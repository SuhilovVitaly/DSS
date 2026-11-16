using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

namespace DeepSpaceSaga.Client.UI.Screens.Tutorial;

/// <summary>
/// Local per-step tutorial events for one battle (EP-0028-US-0006-TK-0001): <c>&lt;battle report&gt;.tutorial.json</c> next to the
/// <see cref="TacticalBattleMetrics"/> report, rewritten atomically (temp file + move) after every event. Event time is the
/// observation clock; no user names or profile paths are written. A failed write is reported once and never breaks the battle.
/// US-0022-TK-0024: the file is written off the calling (render) thread — events recorded in quick succession coalesce into one
/// write of the latest list — and the first failed write switches disk output off for the rest of the battle (events stay in
/// memory). <see cref="Flush"/> waits for the pending write (a transition, e.g. leaving the battle; never a render frame).
/// </summary>
internal sealed class TutorialMetrics(string battleId, string? directory = null)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private readonly List<Entry> _events = [];
    private readonly string _directory = directory ?? TacticalBattleMetrics.DirectoryPath;
    private readonly object _gate = new();
    private Entry[]? _pending;      // latest unwritten snapshot (guarded by _gate)
    private bool _writing;          // a background write loop is scheduled or running (guarded by _gate)
    private Task _writer = Task.CompletedTask;
    private volatile bool _disabled; // set by the first failed write; nothing touches the disk afterwards
    private bool _warned;

    internal string BattleId { get; } = battleId;
    internal string? ScenarioId { get; init; }
    internal long Seed { get; init; }

    /// <summary>Observation clock of the battle's first frame; event times are written relative to it (no machine clock).</summary>
    internal long OriginMs { get; init; }

    /// <summary>Receives the single "TUTORIAL METRICS: …" line of a failed write (default: Interface.log).</summary>
    internal Action<string> Warning { get; init; } = InterfaceLog.Write;

    /// <summary>Runs the file write off the calling thread (default: the thread pool). Tests may defer it to prove a record does no I/O.</summary>
    internal Func<Action, Task> Background { get; init; } = work => Task.Run(work);

    internal string FilePath => Path.ChangeExtension(TacticalBattleMetrics.ReportPath(BattleId, _directory), ".tutorial.json");

    internal IReadOnlyList<Entry> Events => _events;

    /// <summary>True after a failed write: later events are kept in memory only.</summary>
    internal bool WritesDisabled => _disabled;

    /// <summary>shown | completed | dismissed | expired.</summary>
    internal void Record(TutorialEvent e) =>
        Add(new Entry(e.StepId, e.State.ToString().ToLowerInvariant(), e.Phase, e.Turn, e.AtMs - OriginMs));

    internal void RecordHints(bool enabled, string phase, int turn, long atMs) =>
        Add(new Entry(null, enabled ? "hints-enabled" : "hints-disabled", phase, turn, atMs - OriginMs));

    /// <summary>The player left the unfinished tutorial battle for the main menu (tells an abandoned battle from a crash).</summary>
    internal void RecordAbandoned(string phase, int turn, long atMs) =>
        Add(new Entry(null, "abandoned", phase, turn, atMs - OriginMs));

    /// <summary>Waits until every event recorded so far is on disk (or writing has been switched off).</summary>
    internal void Flush()
    {
        Task writer;
        lock (_gate) writer = _writer;
        writer.Wait();
    }

    private void Add(Entry entry)
    {
        _events.Add(entry);
        if (_disabled) return;
        lock (_gate)
        {
            _pending = _events.ToArray();
            if (_writing) return; // the running loop picks the newer snapshot up
            _writing = true;
            _writer = Background(WritePending);
        }
    }

    private void WritePending()
    {
        while (true)
        {
            Entry[]? snapshot;
            lock (_gate)
            {
                snapshot = _disabled ? null : _pending;
                _pending = null;
                if (snapshot is null)
                {
                    _writing = false;
                    return;
                }
            }
            try { Write(snapshot); }
            catch (Exception ex) // off the render thread nothing would observe it: switch writing off and report once
            {
                _disabled = true;
                Warn(ex.Message);
            }
        }
    }

    private void Write(Entry[] events)
    {
        string? temporary = null;
        try
        {
            Directory.CreateDirectory(_directory);
            string path = FilePath;
            var report = new
            {
                schemaVersion = 1,
                battleId = BattleId,
                scenarioId = ScenarioId,
                seed = Seed,
                updatedUtc = DateTimeOffset.UtcNow,
                events = events.Select(e => new { step = e.Step, kind = e.Kind, phase = e.Phase, turn = e.Turn, atMs = e.AtMs }),
            };
            temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(report, Json));
            File.Move(temporary, path, overwrite: true);
            temporary = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            _disabled = true; // no retry on every later event
            Warn(ex.Message);
        }
        finally
        {
            if (temporary is not null)
            {
                try { File.Delete(temporary); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
        }
    }

    private void Warn(string message)
    {
        if (_warned) return;
        _warned = true;
        try { Warning("TUTORIAL METRICS: " + message); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException) { }
    }

    internal sealed record Entry(string? Step, string Kind, string Phase, int Turn, long AtMs);
}
