using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

public sealed partial class SnapshotBuffer
{
    internal const int PresentationHistoryLimit = 4096;
    private Guid? _presentationSession;
    private readonly HashSet<Guid> _retiredPresentationSessions = [];
    private readonly SortedDictionary<long, CombatPresentationUpdate> _reorderedUpdates = [];
    private sealed record PendingSceneFrame(double MotionTimeMs, AuthoritativeSnapshot State, bool Full);
    private readonly Queue<PendingSceneFrame> _sceneFrames = [];
    private AuthoritativeSnapshot? _displayBaseline;
    private HashSet<string> _displayDynamicObjectIds = [];
    private AuthoritativeSnapshot? _sceneBaseline;
    private HashSet<string> _dynamicObjectIds = [];
    private BufferedSnapshot? _presented;
    private long _contiguousSequence;
    private long _confirmedMotionTime;
    private long _lastPresentationReceipt;
    private long _sceneTick;
    private double _sceneTime;
    private long _sceneDelta;
    private bool _automaticPause;
    private long? _stoppedBoundary;
    private long _stoppedBoundaryReceivedAt;
    private bool _sceneStarved;
    private int _pendingCombatFacts;
    private readonly Dictionary<string, CommandAcceptance> _acceptances = [];
    private readonly Queue<string> _acceptanceOrder = [];
    internal event Action<object>? PresentationDiagnostic;

    internal void ConfirmPresentationControl(SimulationControlAcknowledgement acknowledgement)
    {
        lock (_sync)
            if (_presentationSession is null || _presentationSession == acknowledgement.SessionId)
                _automaticPause = acknowledgement.AutomaticPause;
    }

    internal void RecordAcceptance(CommandAcceptance acceptance)
    {
        lock (_sync)
        {
            if (!_acceptances.ContainsKey(acceptance.CommandId)) _acceptanceOrder.Enqueue(acceptance.CommandId);
            _acceptances[acceptance.CommandId] = acceptance;
            while (_acceptances.Count > PresentationHistoryLimit) _acceptances.Remove(_acceptanceOrder.Dequeue());
        }
    }

    public CommandAcceptance? FindCommandAcceptance(string commandId)
    {
        lock (_sync) return _acceptances.GetValueOrDefault(commandId);
    }

    public BufferedSnapshot? Presented { get { lock (_sync) return _presentationSession is not null ? _presented : _latest; } }
    public bool HasCombatPresentation { get { lock (_sync) return _presentationSession is not null; } }
    public Guid? PresentationSessionId { get { lock (_sync) return _presentationSession; } }
    public bool AutomaticPresentationPause { get { lock (_sync) return _automaticPause; } }
    public long ContiguousPresentationSequence { get { lock (_sync) return _contiguousSequence; } }
    public AuthoritativeSnapshot? AuthoritativeLatest { get { lock (_sync) return _latest?.Snapshot; } }
    public bool PresentationPauseRequested
    {
        get
        {
            lock (_sync) return _presentationSession is not null && !_automaticPause &&
                (_latest?.Snapshot.CurrentSpeed != SimulationSpeed.Speed0 || _reorderedUpdates.Count > 0) &&
                (_timestampProvider() - _lastPresentationReceipt) * 1000.0 / Stopwatch.Frequency > 1000;
        }
    }

    public void UpdatePresentation(CombatPresentationUpdate update)
    {
        lock (_sync)
        {
            if (update.SessionId == Guid.Empty || update.Sequence <= 0 || update.ConfirmedMotionTimeMs < 0)
                throw new InvalidOperationException("Invalid combat update identity or watermark.");
            long now = _timestampProvider();
            PresentationDiagnostic?.Invoke(new
            {
                Kind = "combat-update-received",
                update.SessionId,
                update.Sequence,
                update.SentAtTimestamp,
                ReceivedAtTimestamp = now,
                FrameSequences = update.Frames.IsDefault ? [] : update.Frames.Select(f => f.State.SnapshotSequence).ToArray(),
                update.ConfirmedMotionTimeMs,
                ExpectedSequence = _contiguousSequence + 1,
                update.AutomaticPause,
                PauseReason = update.AutomaticPause ? "presentation-data-stale" : null
            });
            if (_retiredPresentationSessions.Contains(update.SessionId)) return;
            if (_presentationSession != update.SessionId)
            {
                if (update.FullSnapshot is not { } bootstrap) return;
                if (_presentationSession is { } oldSession) _retiredPresentationSessions.Add(oldSession);
                _presentationSession = update.SessionId;
                _reorderedUpdates.Clear();
                _sceneFrames.Clear();
                _pendingCombatFacts = 0;
                _combatReports = new();
                _events = new();
                _combatImpactReceivedAt.Clear();
                _acceptances.Clear();
                _acceptanceOrder.Clear();
                _contiguousSequence = update.Sequence;
                _confirmedMotionTime = bootstrap.MotionTimeMs;
                _sceneTime = bootstrap.MotionTimeMs;
                _sceneTick = now + Stopwatch.Frequency * 150 / 1000;
                _sceneBaseline = _displayBaseline = TrimSceneHistory(bootstrap);
                _dynamicObjectIds = update.Frames.IsDefaultOrEmpty ? [] :
                    update.Frames[^1].State.Objects.Select(o => o.ObjectId).ToHashSet(StringComparer.Ordinal);
                _displayDynamicObjectIds = new(_dynamicObjectIds, StringComparer.Ordinal);
                _latest = _presented = new(_sceneBaseline, now);
                _currentSpeed = bootstrap.CurrentSpeed;
                _stoppedBoundary = bootstrap.CurrentSpeed == SimulationSpeed.Speed0 ? bootstrap.MotionTimeMs : null;
                _stoppedBoundaryReceivedAt = now;
                _sceneDelta = 0;
                _automaticPause = update.AutomaticPause;
                _lastPresentationReceipt = now;
                _combatReports.Receive(bootstrap.CombatReports);
                _events.Receive(bootstrap);
                return;
            }
            if (update.Sequence <= _contiguousSequence) return;
            if (_reorderedUpdates.TryGetValue(update.Sequence, out var previous))
            {
                if (previous != update && JsonSerializer.Serialize(previous) != JsonSerializer.Serialize(update))
                    throw new InvalidOperationException("Conflicting combat update sequence.");
                return;
            }
            if (_reorderedUpdates.Count >= 128) throw new InvalidOperationException("Combat update reorder limit exceeded.");
            _reorderedUpdates.Add(update.Sequence, update);
            while (_reorderedUpdates.Remove(_contiguousSequence + 1, out var next))
            {
                if (next.ConfirmedMotionTimeMs < _confirmedMotionTime)
                    throw new InvalidOperationException("Combat watermark moved backwards.");
                double previousTime = _confirmedMotionTime;
                foreach (var frame in next.Frames)
                {
                    if (!double.IsFinite(frame.MotionTimeMs) || frame.MotionTimeMs < previousTime ||
                        frame.MotionTimeMs > next.ConfirmedMotionTimeMs)
                        throw new InvalidOperationException("Invalid combat frame boundary.");
                    previousTime = frame.MotionTimeMs;
                    int facts = (frame.State.CombatImpacts.IsDefault ? 0 : frame.State.CombatImpacts.Length) +
                        (frame.State.CombatJournal.IsDefault ? 0 : frame.State.CombatJournal.Length);
                    if (facts > PresentationHistoryLimit - _pendingCombatFacts)
                        throw new InvalidOperationException("Undisplayed combat event history overflow.");
                    _pendingCombatFacts += facts;
                    if (_sceneFrames.Count >= PresentationHistoryLimit)
                        throw new InvalidOperationException("Combat scene history overflow.");
                    _sceneFrames.Enqueue(new(frame.MotionTimeMs, frame.State, false));
                    _events.Receive(frame.State); // Command receipts bypass the scene clock.
                }
                if (!next.Frames.IsDefaultOrEmpty)
                {
                    var tail = next.Frames[^1].State with
                    {
                        CombatImpacts = next.Frames.SelectMany(f => f.State.CombatImpacts.IsDefault ? [] : f.State.CombatImpacts).ToImmutableArray(),
                        CombatJournal = next.Frames.SelectMany(f => f.State.CombatJournal.IsDefault ? [] : f.State.CombatJournal).ToImmutableArray()
                    };
                    _latest = new(MergeScene(tail), now);
                }
                if (next.FullSnapshot is { } fullState)
                {
                    var full = TrimSceneHistory(fullState);
                    _sceneBaseline = full;
                    if (_sceneFrames.Count >= PresentationHistoryLimit)
                        throw new InvalidOperationException("Combat scene history overflow.");
                    _sceneFrames.Enqueue(new(full.MotionTimeMs, full, true));
                    _latest = new(full, now);
                    _events.Receive(full);
                }
                _currentSpeed = _latest?.Snapshot.CurrentSpeed ?? _currentSpeed;
                if (_currentSpeed != SimulationSpeed.Speed0) _stoppedBoundary = null;
                else if (_stoppedBoundary != next.ConfirmedMotionTimeMs)
                {
                    _stoppedBoundary = next.ConfirmedMotionTimeMs;
                    _stoppedBoundaryReceivedAt = now;
                }
                _confirmedMotionTime = next.ConfirmedMotionTimeMs;
                _contiguousSequence = next.Sequence;
                _lastPresentationReceipt = now;
                _automaticPause = next.AutomaticPause;
            }
        }
    }

    private AuthoritativeSnapshot MergeScene(AuthoritativeSnapshot state, bool display = false)
    {
        var baseline = (display ? _displayBaseline : _sceneBaseline)!;
        var oldIds = display ? _displayDynamicObjectIds : _dynamicObjectIds;
        var nextIds = state.Objects.Select(o => o.ObjectId).ToHashSet(StringComparer.Ordinal);
        var objects = baseline.Objects.Where(o => !oldIds.Contains(o.ObjectId) && !nextIds.Contains(o.ObjectId))
            .Concat(state.Objects).ToImmutableArray();
        if (display) _displayDynamicObjectIds = nextIds;
        else _dynamicObjectIds = nextIds;
        var merged = baseline with
        {
            SnapshotSequence = state.SnapshotSequence,
            GameTimeMs = state.GameTimeMs,
            SimulationTimeMs = state.SimulationTimeMs,
            CurrentSpeed = state.CurrentSpeed,
            Objects = objects,
            PlayerCredits = state.PlayerCredits,
            PlayerCrewCount = state.PlayerCrewCount,
            CurrentStationDistrict = state.CurrentStationDistrict,
            PortFees = state.PortFees,
            MissingRations = state.MissingRations,
            InstalledModules = state.InstalledModules,
            CombatImpacts = Recent(baseline.CombatImpacts, state.CombatImpacts),
            CombatJournal = Recent(baseline.CombatJournal, state.CombatJournal),
            CombatReports = state.CombatReports,
            Outcome = state.Outcome,
            CommandResults = state.CommandResults,
            ShipEvents = state.ShipEvents,
            ActiveDialogue = state.ActiveDialogue,
            DialogueEvents = state.DialogueEvents,
            ActiveObjectId = state.ActiveObjectId,
            SelectedObjectId = state.SelectedObjectId
        };
        if (display) _displayBaseline = merged;
        else _sceneBaseline = merged;
        return merged;
    }

    private static AuthoritativeSnapshot TrimSceneHistory(AuthoritativeSnapshot state) => state with
    {
        CombatImpacts = state.CombatImpacts.IsDefault ? [] : state.CombatImpacts.TakeLast(PresentationHistoryLimit).ToImmutableArray(),
        CombatJournal = state.CombatJournal.IsDefault ? [] : state.CombatJournal.TakeLast(PresentationHistoryLimit).ToImmutableArray()
    };

    private static ImmutableArray<T> Recent<T>(ImmutableArray<T> previous, ImmutableArray<T> delta) =>
        (previous.IsDefault ? [] : previous).Concat(delta.IsDefault ? [] : delta)
            .TakeLast(PresentationHistoryLimit).ToImmutableArray();

    /// <summary>Latch one coherent scene for every UI consumer in the prepared frame.</summary>
    public void PreparePresentation()
    {
        lock (_sync)
        {
            if (_presentationSession is null || _presented is null) return;
            long now = _timestampProvider();
            if (now < _sceneTick) return;
            var speed = _presented.Snapshot.CurrentSpeed;
            double desired = Math.Min(_confirmedMotionTime,
                _sceneTime + (now - _sceneTick) * (1000.0 / Stopwatch.Frequency) * speed.MotionTimeMultiplier());
            // Recover a short stall by consuming every intervening revision in order.
            // The 150 ms reserve is one offset, rather than a new delay per packet.
            if (_currentSpeed != SimulationSpeed.Speed0)
            {
                double target = _confirmedMotionTime +
                    ((now - _lastPresentationReceipt) * (1000.0 / Stopwatch.Frequency) - 150) * _currentSpeed.MotionTimeMultiplier();
                desired = Math.Max(desired, Math.Min(_confirmedMotionTime, target));
            }
            else if (_stoppedBoundary is not null &&
                (now - _stoppedBoundaryReceivedAt) * (1000.0 / Stopwatch.Frequency) >= 150)
                desired = _confirmedMotionTime;
            bool starved = desired >= _confirmedMotionTime && _currentSpeed != SimulationSpeed.Speed0;
            if (starved != _sceneStarved)
                PresentationDiagnostic?.Invoke(new
                {
                    Kind = "combat-scene-buffer",
                    Timestamp = now,
                    Stalled = starved,
                    Reason = _reorderedUpdates.Count > 0 ? "sequence-gap" : "confirmed-boundary"
                });
            _sceneStarved = starved;
            while (_sceneFrames.TryPeek(out var revision) && revision.MotionTimeMs <= desired)
            {
                _sceneFrames.Dequeue();
                if (!revision.Full)
                    _pendingCombatFacts -= (revision.State.CombatImpacts.IsDefault ? 0 : revision.State.CombatImpacts.Length) +
                        (revision.State.CombatJournal.IsDefault ? 0 : revision.State.CombatJournal.Length);
                if (revision.Full) _displayBaseline = revision.State;
                var frame = revision.Full ? revision.State : MergeScene(revision.State, display: true);
                _presented = new(frame, now);
                PresentationDiagnostic?.Invoke(new
                {
                    Kind = "combat-frame-presented",
                    Timestamp = now,
                    frame.SnapshotSequence,
                    frame.MotionTimeMs,
                    _contiguousSequence,
                    _confirmedMotionTime,
                    ImpactEventIds = revision.State.CombatImpacts.IsDefault ? [] : revision.State.CombatImpacts.Select(i => i.EventId).ToArray()
                });
                _combatReports.Receive(frame.CombatReports);

            }
            _sceneTime = desired;
            _sceneTick = now;
            _sceneDelta = Math.Max(0, (long)desired - _presented.Snapshot.MotionTimeMs);
        }
    }
}
