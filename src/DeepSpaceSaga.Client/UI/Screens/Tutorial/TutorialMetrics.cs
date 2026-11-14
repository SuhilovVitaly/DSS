using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

namespace DeepSpaceSaga.Client.UI.Screens.Tutorial;

/// <summary>
/// Local per-step tutorial events for one battle (EP-0028-US-0006-TK-0001): <c>&lt;battle report&gt;.tutorial.json</c> next to the
/// <see cref="TacticalBattleMetrics"/> report, rewritten atomically (temp file + move) after every event. Event time is the
/// observation clock; no user names or profile paths are written. A failed write is reported once and never breaks the battle.
/// </summary>
internal sealed class TutorialMetrics(string battleId, string? directory = null)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private readonly List<Entry> _events = [];
    private readonly string _directory = directory ?? TacticalBattleMetrics.DirectoryPath;
    private bool _warned;

    internal string BattleId { get; } = battleId;
    internal string? ScenarioId { get; init; }
    internal long Seed { get; init; }

    /// <summary>Observation clock of the battle's first frame; event times are written relative to it (no machine clock).</summary>
    internal long OriginMs { get; init; }

    /// <summary>Receives the single "TUTORIAL METRICS: …" line of a failed write (default: Interface.log).</summary>
    internal Action<string> Warning { get; init; } = InterfaceLog.Write;

    internal string FilePath => Path.ChangeExtension(TacticalBattleMetrics.ReportPath(BattleId, _directory), ".tutorial.json");

    internal IReadOnlyList<Entry> Events => _events;

    /// <summary>shown | completed | dismissed | expired.</summary>
    internal void Record(TutorialEvent e) =>
        Add(new Entry(e.StepId, e.State.ToString().ToLowerInvariant(), e.Phase, e.Turn, e.AtMs - OriginMs));

    internal void RecordHints(bool enabled, string phase, int turn, long atMs) =>
        Add(new Entry(null, enabled ? "hints-enabled" : "hints-disabled", phase, turn, atMs - OriginMs));

    private void Add(Entry entry)
    {
        _events.Add(entry);
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
                events = _events.Select(e => new { step = e.Step, kind = e.Kind, phase = e.Phase, turn = e.Turn, atMs = e.AtMs }),
            };
            temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(report, Json));
            File.Move(temporary, path, overwrite: true);
            temporary = null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
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
