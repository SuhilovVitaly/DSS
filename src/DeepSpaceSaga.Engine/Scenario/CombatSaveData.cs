using System.Text.Json.Serialization;
using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Authoritative combat continuation. Presentation effects are deliberately absent.</summary>
public sealed record CombatStateData(
    [property: JsonRequired, JsonPropertyName("version")] int Version,
    [property: JsonRequired, JsonPropertyName("lastProcessedMotionTimeMs")] long LastProcessedMotionTimeMs,
    [property: JsonRequired, JsonPropertyName("projectileSequence")] long ProjectileSequence,
    [property: JsonRequired, JsonPropertyName("impactSequence")] long ImpactSequence,
    [property: JsonRequired, JsonPropertyName("wreckSequence")] long WreckSequence,
    [property: JsonRequired, JsonPropertyName("nextGuidanceMotionTimeMs")] long NextGuidanceMotionTimeMs,
    [property: JsonRequired, JsonPropertyName("launchers")] IReadOnlyList<LauncherSaveData> Launchers,
    [property: JsonRequired, JsonPropertyName("projectiles")] IReadOnlyList<ProjectileSaveData> Projectiles,
    [property: JsonRequired, JsonPropertyName("processedLaunchCommandIds")] IReadOnlyList<string> ProcessedLaunchCommandIds,
    [property: JsonPropertyName("hitLocationRngState")] ulong? HitLocationRngState = null,
    [property: JsonPropertyName("hitLocationRngCounter")] ulong HitLocationRngCounter = 0,
    [property: JsonPropertyName("torpedoHitRngState")] ulong? TorpedoHitRngState = null,
    [property: JsonPropertyName("torpedoHitRngCounter")] ulong TorpedoHitRngCounter = 0,
    [property: JsonPropertyName("launcherBreakdownRngState")] ulong? LauncherBreakdownRngState = null,
    [property: JsonPropertyName("launcherBreakdownRngCounter")] ulong LauncherBreakdownRngCounter = 0,
    [property: JsonPropertyName("processedModeCommandIds")] IReadOnlyList<string>? ProcessedModeCommandIds = null);

public sealed record LauncherSaveData(
    [property: JsonRequired, JsonPropertyName("ownerObjectId")] string OwnerObjectId,
    [property: JsonRequired, JsonPropertyName("moduleId")] string ModuleId,
    [property: JsonRequired, JsonPropertyName("state")] LauncherCombatSnapshot State);

public sealed record ProjectileSaveData(
    [property: JsonRequired, JsonPropertyName("objectId")] string ObjectId,
    [property: JsonRequired, JsonPropertyName("flight")] TorpedoSnapshot Flight,
    [property: JsonRequired, JsonPropertyName("targetLost")] bool TargetLost,
    [property: JsonRequired, JsonPropertyName("targetBaseline")] ObjectMotionSnapshot? TargetBaseline,
    [property: JsonRequired, JsonPropertyName("targetBaselineMotionTimeMs")] long TargetBaselineMotionTimeMs);

internal static class CombatSaveValidation
{
    internal static GameStateData NormalizeReferences(GameStateData state)
    {
        if (state.CombatState is not { Launchers: not null, Projectiles: not null } saved) return state;
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId, StringComparer.OrdinalIgnoreCase);
        string Canon(string id) => !string.IsNullOrWhiteSpace(id) && objects.TryGetValue(id, out var obj) ? obj.ObjectId : id;
        string Module(string owner, string module) => !string.IsNullOrWhiteSpace(owner) && objects.TryGetValue(owner, out var obj)
            ? obj.Modules?.FirstOrDefault(m => string.Equals(m.ModuleId, module, StringComparison.OrdinalIgnoreCase))?.ModuleId ?? module : module;
        return state with
        {
            CombatState = saved with
            {
                Launchers = saved.Launchers.Select(l => l is null || l.State is null ? l! : l with
                {
                    OwnerObjectId = Canon(l.OwnerObjectId),
                    ModuleId = Module(l.OwnerObjectId, l.ModuleId),
                    State = l.State with
                    {
                        ActiveTorpedoIds = (l.State.ActiveTorpedoIds.IsDefault ? ImmutableArray<string>.Empty : l.State.ActiveTorpedoIds)
                            .Select(Canon).ToImmutableArray()
                    }
                }).ToArray(),
                Projectiles = saved.Projectiles.Select(p => p is null || p.Flight is null ? p! : p with
                {
                    ObjectId = Canon(p.ObjectId),
                    Flight = p.Flight with
                    { OwnerObjectId = Canon(p.Flight.OwnerObjectId), LauncherModuleId = Module(p.Flight.OwnerObjectId, p.Flight.LauncherModuleId), TargetObjectId = Canon(p.Flight.TargetObjectId) },
                    TargetBaseline = p.TargetBaseline is { } target ? target with { ObjectId = Canon(target.ObjectId) } : null
                }).ToArray()
            }
        };
    }

    internal static void Validate(GameStateData state, int format)
    {
        static void Require(bool condition, string reason)
        {
            if (!condition) throw new ScenarioException("Invalid combat save: " + reason);
        }
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId, StringComparer.Ordinal);
        foreach (var obj in objects.Values.Where(o => o.ObjectType == SpaceObjectType.Wreck))
            Require(obj.SpeedMps == 0 && obj.DirectionDegrees == 0 && string.Equals(obj.MovementType, "Stationary", StringComparison.OrdinalIgnoreCase) &&
                (obj.Modules?.Count ?? 0) == 0 && (obj.Crew?.Count ?? 0) == 0 && (obj.Passengers?.Count ?? 0) == 0 && !obj.IsDocked,
                "wreck must be stationary and empty.");
        var combat = state.CombatState;
        if (combat is null)
        {
            Require(format < 10 && !objects.Values.Any(o => o.ObjectType == SpaceObjectType.Missile), "missing combat state.");
            return;
        }
        Require(format >= 17 ? combat.Version == 2 : combat.Version is 1 or 2, "unsupported combat version.");
        Require(combat.LastProcessedMotionTimeMs == state.MotionTimeMs && combat.ProjectileSequence >= 0 &&
            combat.ImpactSequence == 0 && combat.WreckSequence is >= 0 and < long.MaxValue && combat.NextGuidanceMotionTimeMs == long.MaxValue,
            "invalid counters, motion time or retired guidance continuation.");
        Require((format < 17 || combat.HitLocationRngState is not null) &&
            (combat.HitLocationRngState is not null || combat.HitLocationRngCounter == 0), "missing hit-location RNG state.");
        Require(combat.TorpedoHitRngState is null or 0 && combat.TorpedoHitRngCounter == 0 &&
            combat.LauncherBreakdownRngState is null or 0 && combat.LauncherBreakdownRngCounter == 0 &&
            combat.ProcessedModeCommandIds is null or { Count: 0 }, "retired hit/breakdown RNG or mode continuation.");
        Require(combat.Launchers is not null && combat.Projectiles is { Count: 0 } && combat.ProcessedLaunchCommandIds is not null,
            "missing collections or retired guided flights.");
        var launches = new HashSet<string>(StringComparer.Ordinal);
        foreach (string id in combat.ProcessedLaunchCommandIds!)
            Require(!string.IsNullOrWhiteSpace(id) && launches.Add(id), "duplicate or empty launch command ID.");
        var launchers = new Dictionary<(string, string), LauncherCombatSnapshot>();
        foreach (var row in combat.Launchers!)
        {
            Require(row is not null && !string.IsNullOrWhiteSpace(row.OwnerObjectId) && !string.IsNullOrWhiteSpace(row.ModuleId), "invalid launcher row.");
            Require(objects.TryGetValue(row!.OwnerObjectId, out var owner) && owner.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip &&
                owner.Modules?.Any(m => m.ModuleId == row.ModuleId && m.ActiveCycle is null) == true, "orphan or cycling launcher.");
            var l = row.State;
            Require(l is not null && state.PointCombatState is not null && l.Profiles is { IsDefaultOrEmpty: false } &&
                double.IsFinite(l.SpeedKmS) && l.SpeedKmS > 0 && l.TurnRateDegPerSec == 0 && l.Damage > 0 &&
                launchers.TryAdd((row.OwnerObjectId, row.ModuleId), l), "invalid, duplicate or retired launcher state.");
            Require(!l!.ActiveTorpedoIds.IsDefault && l.ActiveTorpedoIds.All(id => !string.IsNullOrWhiteSpace(id)) &&
                l.ActiveTorpedoIds.Distinct(StringComparer.Ordinal).Count() == l.ActiveTorpedoIds.Length &&
                (l.ReadyAtMotionTimeMs is null || double.IsFinite(l.ReadyAtMotionTimeMs.Value) && l.ReadyAtMotionTimeMs >= 0),
                "invalid active flights or reload deadline.");
            Require(PointCombatSaveValidation.ProfilesValid(l.Profiles!.Value) && l.RangeKm is > 0 && double.IsFinite(l.RangeKm.Value) &&
                l.ReloadDurationMs is >= 0 && double.IsFinite(l.ReloadDurationMs.Value) && l.ScatterPercent is >= 0 && double.IsFinite(l.ScatterPercent.Value) &&
                l.Maneuverability is null && !l.IsBrokenUntilDock && l.ModeId == string.Empty &&
                (l.ActiveTorpedoIds.IsEmpty || l.ReadyAtMotionTimeMs is not null), "invalid point capabilities or retired modes.");
            if (l.Operator is { } op)
                Require(op.SkillType == WeaponSkillType.TorpedoAttack && op.BaseRating == 0 && op.Skill is >= 0 and <= 30 &&
                    op.EffectiveRating == op.Skill && owner!.Modules!.Single(m => m.ModuleId == row.ModuleId).OperatorCrewId == op.CrewId &&
                    owner.Crew?.Any(c => c.CrewId == op.CrewId) == true, "invalid point operator membership.");
        }
        Require(objects.Values.Where(o => o.ObjectType == SpaceObjectType.Missile).All(o =>
            state.PointCombatState?.Projectiles.Any(p => p.ObjectId == o.ObjectId && p.Flight.Kind == ProjectileKind.Torpedo) == true), "missile without point flight.");
        foreach (var launcher in launchers)
            foreach (string id in launcher.Value.ActiveTorpedoIds)
                Require(state.PointCombatState?.Projectiles.Any(p => p.ObjectId == id && p.Flight.Kind == ProjectileKind.Torpedo &&
                    p.Flight.OwnerObjectId == launcher.Key.Item1 && p.Flight.LauncherModuleId == launcher.Key.Item2) == true,
                    "busy launcher without matching point flight.");
    }
}
