using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static class TacticalBattleMetrics
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    internal static string DirectoryPath => Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { Length: > 0 } root
        && Path.IsPathFullyQualified(root)
        ? Path.Combine(root, "battle-metrics")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeepSpaceSaga", "BattleMetrics");

    internal static string Write(TacticalBattleSnapshot state, bool exited = false, string? directory = null)
    {
        directory ??= DirectoryPath;
        var units = state.Units.ToDictionary(unit => unit.Id, StringComparer.Ordinal);
        var shots = state.Log.Where(entry => entry.Shot is not null).Select(entry =>
        {
            units.TryGetValue(entry.ActorId ?? "", out var shooter);
            var shot = entry.Shot!.ResolvedShot;
            return new Shot(entry.Sequence, entry.Turn, entry.ActorId, entry.TargetId, shooter?.Side,
                shooter?.Build?.WeaponId, entry.Kind, entry.Shot.Chance, entry.Shot.Decoyed, shot);
        }).ToArray();
        var skips = state.Log.Where(entry => entry.Kind == TacticalBattleLogKind.FireSkipped)
            .GroupBy(entry => entry.SkipReason).Select(group => new { reason = group.Key, count = group.Count() }).ToArray();
        var report = new
        {
            schemaVersion = 1,
            state.BattleId, state.Seed, state.ScenarioId, state.PlayerSide, state.Revision, state.Turn, state.Tactics,
            updatedUtc = DateTimeOffset.UtcNow,
            status = state.Outcome is not null ? "completed" : exited ? "exited" : "active",
            outcome = state.Outcome?.Kind,
            totals = Summarize(shots),
            sides = state.Tactics.Select(tactic => new
            {
                tactic.Side, tactic.TacticId, totals = Summarize(shots.Where(shot => shot.Side == tactic.Side).ToArray())
            }).ToArray(),
            skips, shots
        };
        Directory.CreateDirectory(directory);
        string path = ReportPath(state.BattleId, directory);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(report, Json));
            File.Move(temporary, path, overwrite: true);
        }
        finally { File.Delete(temporary); }
        return path;
    }

    internal static string ReportPath(string battleId, string directory)
    {
        string id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(battleId)));
        return Path.Combine(directory, id + ".json");
    }

    private static object Summarize(Shot[] shots)
    {
        int hits = shots.Count(shot => shot.Outcome is TacticalBattleLogKind.ShotHit or TacticalBattleLogKind.RearKill);
        int misses = shots.Count(shot => shot.Outcome == TacticalBattleLogKind.ShotMissed);
        int absorbed = shots.Count(shot => shot.Outcome == TacticalBattleLogKind.ShotAbsorbed);
        return new
        {
            shots = shots.Length, hits, misses, absorbed,
            hitPercent = shots.Length == 0 ? (double?)null : 100.0 * hits / shots.Length,
            expectedHitPercent = shots.Length == 0 ? (double?)null : 100 * shots.Average(shot => shot.Chance),
            expectedHits = shots.Sum(shot => shot.Chance),
            decoyed = shots.Count(shot => shot.Decoyed)
        };
    }

    private sealed record Shot(ulong Sequence, int Turn, string? ShooterId, string? TargetId, string? Side,
        string? WeaponId, TacticalBattleLogKind Outcome, double Chance, bool Decoyed, TacticalShotSnapshot? Resolved);
}
