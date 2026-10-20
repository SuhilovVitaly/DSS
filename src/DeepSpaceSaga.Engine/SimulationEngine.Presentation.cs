using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    public const int CombatUpdateIntervalMs = 100;
    private Guid _presentationSession = Guid.NewGuid();
    private long _presentationSequence;
    private readonly List<CombatPresentationFrame> _presentationFrames = [];
    private HashSet<string> _presentationWorldObjectIds = [];
    private bool _collectPresentation;
    private bool _presentationLease;
    private bool _presentationBootstrap;
    private bool _automaticPresentationPause;
    private SimulationSpeed _presentationIntervalSpeed;
    private int _presentationImpactCursor;
    private int _presentationJournalCursor;
    private long _presentationAck;
    private long _presentationProgressAt;
    private Func<long> _presentationTimestamp = Stopwatch.GetTimestamp;

    private void ResetPresentationSession()
    {
        _presentationSession = Guid.NewGuid();
        _presentationSequence = 0;
        _presentationAck = 0;
        _presentationImpactCursor = 0;
        _presentationJournalCursor = 0;
        _presentationFrames.Clear();
        _presentationWorldObjectIds.Clear();
        _automaticPresentationPause = false;
        _presentationBootstrap = true;
        _presentationProgressAt = _presentationTimestamp();
    }

    public void EnablePresentationLease()
    {
        lock (_worldStateLock)
        {
            _presentationLease = true;
            _collectPresentation = true;
            _presentationBootstrap = true;
            _presentationProgressAt = _presentationTimestamp();
        }
    }

    public void ReportPresentationProgress(long sequence, bool pauseRequested, Guid? sessionId = null)
    {
        lock (_worldStateLock)
        {
            if (sessionId is { } identity && identity != _presentationSession) return;
            if (sequence > _presentationSequence || sequence < 0)
                throw new ArgumentOutOfRangeException(nameof(sequence));
            if (sequence > _presentationAck)
            {
                _presentationAck = sequence;
                _presentationProgressAt = _presentationTimestamp();
            }
            if (pauseRequested) PauseForPresentation();
        }
    }

    private void CheckPresentationLease()
    {
        if (_presentationLease && _clock.Speed != SimulationSpeed.Speed0 &&
            (_presentationTimestamp() - _presentationProgressAt) * 1000.0 / Stopwatch.Frequency > 1000)
            PauseForPresentation((long)(_presentationProgressAt * (1000.0 / Stopwatch.Frequency)) + 1000);
    }

    private void PauseForPresentation(long? realDeadlineMs = null)
    {
        if (UsesPointEncounters) AdvancePointClockWorld(realDeadlineMs);
        StopAllTurnHolds();
        _automaticPresentationPause = true;
        // Dialogue/modal restoration must not resume a session paused by the watchdog.
        if (_dialogue.Active is not null) _dialogue.ResumeSpeed = SimulationSpeed.Speed0;
        if (UsesPointEncounters)
        {
            _clock.SetSpeedAtProcessedBoundary(SimulationSpeed.Speed0);
            _clock.ResetRealBaseline();
        }
        else if (realDeadlineMs is { } deadline) _clock.PauseAtRealTime(deadline);
        else _clock.SetSpeed(SimulationSpeed.Speed0);
    }

    public void ResumePresentation(SimulationSpeed speed) => ResumePresentationControl(speed);

    public SimulationControlAcknowledgement ResumePresentationControl(SimulationSpeed speed)
    {
        lock (_worldStateLock)
        {
            _automaticPresentationPause = false;
            _presentationProgressAt = _presentationTimestamp();
            return SetPresentationSpeed(speed);
        }
    }

    public SimulationControlAcknowledgement SetPresentationSpeed(SimulationSpeed speed)
    {
        lock (_worldStateLock)
        {
            var reason = TrySetSpeed(speed);
            return CapturePresentationControl() with { ReasonCode = reason };
        }
    }

    public SimulationControlAcknowledgement CapturePresentationControl()
    {
        lock (_worldStateLock) return new(_presentationSession, _clock.Capture().MotionTimeMs,
            _clock.Speed, _automaticPresentationPause);
    }

    private static bool IsPresentationObject(SpaceObjectRuntime obj) =>
        obj.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip or
            SpaceObjectType.Missile or SpaceObjectType.Countermeasure or SpaceObjectType.Wreck ||
        obj.InitialMotion.SpeedKmS != 0 || obj.InitialMotion.Orbit is not null ||
        obj.ShipClassId is not null || !obj.Modules.IsDefaultOrEmpty || obj.IsDestroyed;

    private void CapturePresentationFrame(double time)
    {
        if (!_collectPresentation) return;
        if (_presentationFrames.Count >= 4096)
        {
            PauseForPresentation();
            throw new InvalidOperationException("Combat presentation history overflow; simulation paused.");
        }
        var clock = _clock.Capture();
        long calendar = clock.GameTimeMs;
        if (_defeatToMotion > _defeatFromMotion && time >= _defeatFromMotion && time <= _defeatToMotion)
        {
            // Reuse the world's calendar/physical mapping, including legacy save offsets
            // and explicit calendar advances. An intermediate scene must not expose the
            // publication interval's later calendar timestamp.
            decimal fraction = ((decimal)time - _defeatFromMotion) / (_defeatToMotion - (decimal)_defeatFromMotion);
            calendar = _defeatFromCalendar + (long)decimal.Floor((_defeatToCalendar - (decimal)_defeatFromCalendar) * fraction);
        }
        var state = BuildSnapshot(new(calendar, clock.Speed, (long)Math.Ceiling(time)),
            presentationOnly: true, prepareWorld: false);
        state = state with { CurrentSpeed = IsDefeated ? SimulationSpeed.Speed0 : UsesPointEncounters ? state.CurrentSpeed : _presentationIntervalSpeed };
        _presentationImpactCursor = _combatImpacts.Count;
        _presentationJournalCursor = _combatJournal.Count;
        AddPresentationFrame(time, state);
    }

    private void AddPresentationFrame(double time, AuthoritativeSnapshot state)
    {
        var worldIds = _objects.Select(o => o.InitialMotion.ObjectId).ToHashSet(StringComparer.Ordinal);
        var removed = _presentationWorldObjectIds.Where(id => !worldIds.Contains(id))
            .Order(StringComparer.Ordinal).ToImmutableArray();
        _presentationWorldObjectIds = worldIds;
        _presentationFrames.Add(new(time, state, removed));
    }

    internal CombatPresentationUpdate CapturePresentationUpdate(bool fullSnapshot = false, bool advanceClock = true)
    {
        lock (_worldStateLock)
        {
            _presentationIntervalSpeed = _clock.Speed;
            CheckPresentationLease();
            var clock = advanceClock ? UsesPointEncounters ? AdvancePointClockWorld() : _clock.UpdateAndCapture() : _clock.Capture();
            var state = BuildSnapshot(clock, presentationOnly: true);
            // Includes commands, terminal outcomes and control acknowledgements after world advancement.
            if (_collectPresentation) AddPresentationFrame(state.MotionTimeMs, state);
            _presentationImpactCursor = _combatImpacts.Count;
            _presentationJournalCursor = _combatJournal.Count;
            AuthoritativeSnapshot? full = fullSnapshot || _presentationBootstrap
                ? BuildSnapshot(new(state.GameTimeMs, state.CurrentSpeed, state.MotionTimeMs), prepareWorld: false) : null;
            _presentationBootstrap = false;
            var result = new CombatPresentationUpdate(_presentationSession, ++_presentationSequence,
                state.MotionTimeMs, _presentationFrames.ToImmutableArray(), full,
                _automaticPresentationPause, _presentationTimestamp());
            _presentationFrames.Clear();
            // Receipts are carried immediately by this update, independently of scene playback.
            if (_collectPresentation || full is not null)
            {
                _commandResults.Clear();
                _shipEvents.Clear();
                _pendingCombatReports.Clear();
            }
            return result;
        }
    }

    public async IAsyncEnumerable<CombatPresentationUpdate> RunCombatUpdatesAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        lock (_worldStateLock) _clock.ResetRealBaseline();
        yield return CapturePresentationUpdate(fullSnapshot: true, advanceClock: false);
        long lastFull = Stopwatch.GetTimestamp();
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(CombatUpdateIntervalMs));
        while (await timer.WaitForNextTickAsync(cancellationToken) && !_disposed)
        {
            bool full = Stopwatch.GetElapsedTime(lastFull).TotalMilliseconds >= SnapshotIntervalMs;
            var update = CapturePresentationUpdate(full);
            if (full) lastFull = Stopwatch.GetTimestamp();
            yield return update;
        }
    }

    internal void SetPresentationTimestampForTests(Func<long> timestamp) => _presentationTimestamp = timestamp;
}
