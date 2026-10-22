using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Private authoritative point-combat continuation; never a presentation DTO.</summary>
public sealed record PointCombatStateData(
    [property: JsonRequired] int Version,
    [property: JsonRequired] IReadOnlyList<PointProjectileSaveData> Projectiles,
    [property: JsonRequired] IReadOnlyList<EvasionSaveData> Evasions,
    [property: JsonRequired] NamedCombatRngSaveData LaunchScatter,
    [property: JsonRequired] NamedCombatRngSaveData NpcDodge,
    [property: JsonRequired] IReadOnlyList<NpcDodgeSaveData> DodgeThreats,
    [property: JsonRequired] EncounterSaveData Encounter,
    [property: JsonRequired] long PlayerEvasionEpisodes)
{
    public const int CurrentVersion = 1;
}

public sealed record NamedCombatRngSaveData([property: JsonRequired] int AlgorithmVersion,
    [property: JsonRequired] ulong State, [property: JsonRequired] ulong Counter);
public sealed record PointProjectileSaveData([property: JsonRequired] string ObjectId,
    [property: JsonRequired] OwnProjectileSnapshot Flight,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? LauncherModuleTypeId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] OwnProjectileSnapshot? OriginalFlight = null);
public sealed record EvasionSaveData([property: JsonRequired] string ObjectId,
    [property: JsonRequired] string EngineModuleId, [property: JsonRequired] long StartedAt,
    [property: JsonRequired] EvasionSnapshot Motion, [property: JsonRequired] ActiveCycleData? OrbitCycle,
    [property: JsonRequired] long? LastTurnTime,
    [property: JsonRequired] ObjectMotionSnapshot? IntegrationAnchor = null);
public sealed record NpcDodgeSaveData([property: JsonRequired] string OwnerId,
    [property: JsonRequired] string EngineModuleId, [property: JsonRequired] string ProjectileId,
    [property: JsonRequired] long DetectedAt, [property: JsonRequired] long DueAt,
    [property: JsonRequired] bool Evaluated, [property: JsonRequired] bool? RollSucceeded,
    [property: JsonRequired] bool Evaded);
public sealed record EncounterSaveData([property: JsonRequired] CombatEncounterState State,
    [property: JsonRequired] long StartedAt, [property: JsonRequired] long? EndedAt,
    [property: JsonRequired] long QuietSince, [property: JsonRequired] IReadOnlyList<string> EnemyIds,
    [property: JsonRequired] IReadOnlyList<string> ProjectileIds,
    [property: JsonRequired] CombatEncounterStats Stats, [property: JsonRequired] SimulationSpeed? ResumeSpeed);

internal static class PointCombatSaveValidation
{
    internal static bool ProfilesValid(IEnumerable<LaunchProfileSnapshot>? profiles)
    {
        var list = profiles?.ToArray();
        return list is { Length: > 0 } && list.All(p => p is not null && !string.IsNullOrWhiteSpace(p.ProfileId) &&
            p.ProfileId == p.ProfileId.Trim() && double.IsFinite(p.SpeedKmS) && p.SpeedKmS > 0 &&
            double.IsFinite(p.BlastRadiusKm) && p.BlastRadiusKm > 0) &&
            list.Select(p => p.ProfileId).Distinct(StringComparer.Ordinal).Count() == list.Length;
    }

    internal static GameStateData NormalizeReferences(GameStateData state)
    {
        if (state.PointCombatState is not { } saved) return state;
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId, StringComparer.OrdinalIgnoreCase);
        var ids = objects.Keys.ToDictionary(id => id, id => id, StringComparer.OrdinalIgnoreCase);
        foreach (string id in (saved.Encounter?.EnemyIds ?? []).Concat(saved.Projectiles?.Where(p => p?.Flight is not null).Select(p => p.Flight.OwnerObjectId) ?? []))
            if (!string.IsNullOrWhiteSpace(id)) ids.TryAdd(id, id);
        string Canon(string id) => id is not null && ids.TryGetValue(id, out var value) ? value : id!;
        string Module(string owner, string module) => owner is not null && objects.TryGetValue(owner, out var o)
            ? o.Modules?.FirstOrDefault(m => string.Equals(m.ModuleId, module, StringComparison.OrdinalIgnoreCase))?.ModuleId ?? module : module;
        ObjectMotionSnapshot? Motion(ObjectMotionSnapshot? motion) => motion is null ? null : motion with
        { ObjectId = Canon(motion.ObjectId), NavigationTargetObjectId = motion.NavigationTargetObjectId is { } target ? Canon(target) : null };
        return state with
        {
            PointCombatState = saved with
            {
                Projectiles = saved.Projectiles?.Select(p => p?.Flight is not { } f ? p! : p with
                {
                    ObjectId = Canon(p.ObjectId),
                    Flight = f with { OwnerObjectId = Canon(f.OwnerObjectId), LauncherModuleId = Module(f.OwnerObjectId, f.LauncherModuleId) },
                    OriginalFlight = p.OriginalFlight is not { } original ? null : original with
                    { OwnerObjectId = Canon(original.OwnerObjectId), LauncherModuleId = Module(original.OwnerObjectId, original.LauncherModuleId) }
                }).ToArray()!,
                Evasions = saved.Evasions?.Select(e => e?.Motion is not { } m ? e! : e with
                {
                    ObjectId = Canon(e.ObjectId),
                    EngineModuleId = Module(e.ObjectId, e.EngineModuleId),
                    Motion = m with { ThreatProjectileId = Canon(m.ThreatProjectileId), BaseMotion = Motion(m.BaseMotion)! },
                    IntegrationAnchor = Motion(e.IntegrationAnchor),
                    OrbitCycle = e.OrbitCycle is not { } c ? null : c with
                    {
                        ObjectId = c.ObjectId is { } owner ? Canon(owner) : null,
                        ModuleId = c.ModuleId is { } module ? Module(e.ObjectId, module) : null,
                        TargetObjectId = c.TargetObjectId is { } target ? Canon(target) : null
                    }
                }).ToArray()!,
                DodgeThreats = saved.DodgeThreats?.Select(d => d is null ? d! : d with
                { OwnerId = Canon(d.OwnerId), EngineModuleId = Module(d.OwnerId, d.EngineModuleId), ProjectileId = Canon(d.ProjectileId) }).ToArray()!,
                Encounter = saved.Encounter is not { } encounter ? null! : encounter with
                { EnemyIds = encounter.EnemyIds?.Select(Canon).ToArray()!, ProjectileIds = encounter.ProjectileIds?.Select(Canon).ToArray()! }
            }
        };
    }

    internal static void Validate(PointCombatStateData? saved, long time)
    {
        // Absent on new-game and the transitional legacy combat path. Point runtime requires it on restore.
        if (saved is null) return;
        static void Require([DoesNotReturnIf(false)] bool condition, string reason)
        {
            if (!condition) throw new ScenarioException("Invalid point combat save: " + reason);
        }
        static bool Id(string? id) => !string.IsNullOrWhiteSpace(id) && id == id.Trim();
        static bool Unique(IEnumerable<string>? ids) => ids is not null && ids.All(Id) &&
            ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() == ids.Count();
        static bool Finite(params double[] values) => values.All(double.IsFinite);
        Require(saved.Version == PointCombatStateData.CurrentVersion && time >= 0, "unsupported version or time.");
        Require(saved.LaunchScatter is { AlgorithmVersion: 1 } && saved.LaunchScatter.Counter % 2 == 0 &&
            saved.NpcDodge is { AlgorithmVersion: 1 }, "invalid named RNG.");
        Require(saved.PlayerEvasionEpisodes >= 0 && saved.Projectiles is not null && saved.Evasions is not null &&
            saved.DodgeThreats is not null && saved.Encounter is not null, "missing collections or counters.");
        Require(saved.Projectiles!.All(p => p is not null && Id(p.ObjectId) && p.Flight is not null) &&
            Unique(saved.Projectiles.Select(p => p.ObjectId)), "invalid or duplicate projectile.");
        foreach (var row in saved.Projectiles)
        {
            var f = row.Flight;
            Require(Enum.IsDefined(f.Kind) && Id(f.OwnerObjectId) && Id(f.LauncherModuleId) && Id(f.ProfileId) &&
                f.OwnerObjectId != row.ObjectId && Finite(f.X, f.Y, f.StartX, f.StartY, f.DetonationX, f.DetonationY,
                    f.DetonationMotionTimeMs, f.SpeedKmS, f.BlastRadiusKm, f.CoreFraction) && f.SpeedKmS > 0 &&
                f.BlastRadiusKm > 0 && f.CoreFraction is > 0 and < 1, "invalid captured profile or geometry.");
            Require(f.LaunchMotionTimeMs >= 0 && f.LaunchMotionTimeMs <= time &&
                f.DetonationMotionTimeMs >= f.LaunchMotionTimeMs && f.DetonationMotionTimeMs >= time,
                "invalid deadline.");
            Require(f.Kind == ProjectileKind.Torpedo ? f.Damage > 0 : f.Damage == 0,
                "invalid captured damage.");
            Require(f.ArmorPiercingPercent is >= 0 and <= 100 && f.ArmorDamagePercent > 0 &&
                f.OwnerPlayerRelationAtLaunch is PlayerRelation.Self or PlayerRelation.Enemy or PlayerRelation.Friend or PlayerRelation.Neutral,
                "invalid captured armor or launch relation.");
            Require(f.Reporter is null || Enum.IsDefined(f.Reporter.Role) &&
                (f.Reporter.CrewId is null && f.Reporter.DisplayName is null || Id(f.Reporter.CrewId) &&
                !string.IsNullOrWhiteSpace(f.Reporter.DisplayName)), "invalid captured reporter.");
        }
        Require(saved.Evasions!.All(e => e is not null && Id(e.ObjectId)) && Unique(saved.Evasions.Select(e => e.ObjectId)),
            "invalid or duplicate evasion.");
        foreach (var e in saved.Evasions)
        {
            Require(Id(e.EngineModuleId) && e.StartedAt >= 0 && e.StartedAt <= time && e.Motion is not null &&
                e.LastTurnTime is null or >= 0, "invalid evasion owner or start.");
            var m = e.Motion!;
            Require(Finite(m.SampleMotionTimeMs, m.EndsAtMotionTimeMs) && m.SampleMotionTimeMs >= e.StartedAt &&
                m.SampleMotionTimeMs <= time && Math.Ceiling(DeepSpaceSaga.Motion.EvasionMotionMath.CompletionTime(m)) > time && Enum.IsDefined(m.Side) && Id(m.ThreatProjectileId),
                "invalid evasion schedule.");
            Require(m.Steering is null || m.Steering is { } steering && Finite(steering.Direction, steering.MaxSpeedKmS, steering.TurnRateDegPerSec,
                steering.AccelerationKmS2, steering.ReturnsAtMotionTimeMs) && steering.MaxSpeedKmS > 0 && steering.TurnRateDegPerSec > 0 &&
                steering.AccelerationKmS2 > 0 && steering.ReturnsAtMotionTimeMs >= m.EndsAtMotionTimeMs && steering.ReturnsAtMotionTimeMs < long.MaxValue,
                "invalid evasion steering.");
            Require(m.BaseMotion is { Evasion: null, OwnProjectile: null, ObservedProjectile: null } basis &&
                basis.ObjectId == e.ObjectId && Finite(basis.X, basis.Y, basis.SpeedKmS, basis.Direction) && basis.SpeedKmS >= 0,
                "invalid evasion base course.");
            Require(e.IntegrationAnchor is null || e.IntegrationAnchor is { Evasion: null, OwnProjectile: null, ObservedProjectile: null } anchor &&
                anchor.ObjectId == e.ObjectId && Finite(anchor.X, anchor.Y, anchor.SpeedKmS, anchor.Direction) && anchor.SpeedKmS >= 0,
                "invalid evasion integration anchor.");
        }
        var threats = new HashSet<(string, string)>();
        foreach (var d in saved.DodgeThreats!)
        {
            Require(d is not null && Id(d.OwnerId) && Id(d.EngineModuleId) && Id(d.ProjectileId) &&
                threats.Add((d.OwnerId.ToUpperInvariant(), d.ProjectileId.ToUpperInvariant())) &&
                d.DetectedAt >= 0 && d.DetectedAt <= time && d.DueAt >= d.DetectedAt &&
                (d.Evaluated ? d.DueAt <= time : d.DueAt >= time) && (d.RollSucceeded is null || d.Evaluated) &&
                (!d.Evaded || d.RollSucceeded == true), "invalid NPC dodge episode.");
        }
        var encounter = saved.Encounter!;
        Require(Enum.IsDefined(encounter.State) && encounter.StartedAt >= 0 && encounter.StartedAt <= time &&
            encounter.QuietSince >= 0 && encounter.QuietSince <= time && encounter.EndedAt is null or >= 0 &&
            (encounter.EndedAt is null || encounter.EndedAt >= encounter.StartedAt && encounter.EndedAt <= time) &&
            (encounter.State is CombatEncounterState.Won or CombatEncounterState.Lost ? encounter.EndedAt is not null :
                encounter.State != CombatEncounterState.Active || encounter.EndedAt is null) &&
            (encounter.ResumeSpeed is null || Enum.IsDefined(encounter.ResumeSpeed.Value)), "invalid encounter timeline.");
        Require(Unique(encounter.EnemyIds) && Unique(encounter.ProjectileIds) && encounter.Stats is not null,
            "invalid encounter identities.");
        var s = encounter.Stats!;
        Require(new[] { s.TorpedoesLaunched, s.CountermissilesLaunched, s.EnemyTorpedoesLaunched, s.TorpedoesIntercepted,
            s.PlayerHitsReceived, s.EnemyHitsDealt, s.PlayerEvasions, s.EnemiesDestroyed, s.ArmorDamageTaken,
            s.StructureDamageTaken }.All(value => value >= 0), "invalid encounter statistics.");
    }
}
