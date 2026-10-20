using System.Diagnostics.CodeAnalysis;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Rng;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Point-only save integrity. Uses captured point geometry, not legacy encounter/chance rules.</summary>
internal static class PointCombatContinuationValidation
{
    private static void Require([DoesNotReturnIf(false)] bool condition, string reason)
    {
        if (!condition) throw new ScenarioException("Invalid point combat continuation: " + reason);
    }
    private static bool Near(double a, double b) => double.IsFinite(a) && double.IsFinite(b) &&
        Math.Abs(a - b) <= 1e-7 + 16 * Math.Max(Math.Abs(Math.BitIncrement(a) - a), Math.Abs(Math.BitIncrement(b) - b));
    private static bool Heading(double a, double b) => double.IsFinite(a) && double.IsFinite(b) &&
        Math.Abs((a - b + 540) % 360 - 180) <= 1e-8;
    private static bool Pose(SpaceObjectData obj, ObjectMotionSnapshot pose) => Near(obj.PositionX, pose.X) &&
        Near(obj.PositionY, pose.Y) && Near(obj.SpeedMps / 1000, pose.SpeedKmS) && Heading(obj.DirectionDegrees, pose.Direction);

    internal static void Validate(GameStateData state, int format)
    {
        if (state.PointCombatState is not { } point) return;
        Require(format >= 18 && state.MasterSeed is not null && state.CombatState is not null && state.DefenseState is not null,
            "missing Save18 continuation envelopes.");
        var combat = state.CombatState; var defense = state.DefenseState;
        Require(combat.Launchers is not null && combat.Projectiles is not null && defense.Launchers is not null &&
            defense.Projectiles is not null && defense.Journal is not null, "missing continuation collections.");
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId, StringComparer.Ordinal);
        var launches = defense.Journal.Where(e => e is { Type: CombatEventType.Launch, ProjectileKind: not null }).ToArray();
        Require(launches.All(e => Enum.IsDefined(e.ProjectileKind!.Value) && !string.IsNullOrWhiteSpace(e.ProfileId) &&
            !string.IsNullOrWhiteSpace(e.ProjectileObjectId) && !string.IsNullOrWhiteSpace(e.ActorObjectId) &&
            e.BlastRadiusKm is > 0 && double.IsFinite(e.BlastRadiusKm.Value)) &&
            launches.Select(e => e.ProjectileObjectId).Distinct(StringComparer.OrdinalIgnoreCase).Count() == launches.Length,
            "invalid point launch history.");
        var launchById = launches.ToDictionary(e => e.ProjectileObjectId, StringComparer.OrdinalIgnoreCase);
        Require(point.LaunchScatter.Counter == (ulong)launches.Length * 2,
            "scatter counter does not match accepted launches.");
        ulong seed = RngStreamSeedDerivation.DeriveStreamSeed(state.MasterSeed.Value, RngStreamNames.LaunchScatter);
        Require(point.LaunchScatter.State == unchecked(seed + point.LaunchScatter.Counter * 0x9E3779B97F4A7C15UL),
            "scatter state/counter mismatch.");
        Require(point.NpcDodge.Counter == (ulong)point.DodgeThreats.Count(d => d.RollSucceeded is not null),
            "NPC roll counter does not match evaluated episodes.");
        if (point.NpcDodge.Counter == 0)
            Require(point.NpcDodge.State == RngStreamSeedDerivation.DeriveStreamSeed(state.MasterSeed.Value, RngStreamNames.NpcDodge),
                "NPC initial RNG state mismatch.");
        var legacyIds = combat.Projectiles.Where(p => p is not null).Select(p => p.ObjectId)
            .Concat(defense.Projectiles.Where(p => p is not null).Select(p => p.ObjectId)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var row in point.Projectiles)
        {
            var f = row.Flight;
            Require(!legacyIds.Contains(row.ObjectId) && objects.TryGetValue(row.ObjectId, out var obj),
                "duplicate representation or missing projectile object.");
            obj = objects[row.ObjectId];
            Require(obj.ObjectType == (f.Kind == ProjectileKind.Torpedo ? SpaceObjectType.Missile : SpaceObjectType.Countermeasure) &&
                !obj.IsDestroyed && !obj.IsDocked && (obj.Modules?.Count ?? 0) == 0 && (obj.Crew?.Count ?? 0) == 0 &&
                (obj.Passengers?.Count ?? 0) == 0, "invalid projectile object.");
            Require(CountermeasureSaveValidation.SequenceWithin(row.ObjectId, f.Kind == ProjectileKind.Torpedo ? "torpedo-" : "countermeasure-",
                f.Kind == ProjectileKind.Torpedo ? combat.ProjectileSequence : defense.ProjectileSequence), "projectile counter behind identity.");
            double dx = (f.DetonationX - f.StartX) / 10, dy = (f.DetonationY - f.StartY) / 10;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            Require(double.IsFinite(distance) && Near(f.DetonationMotionTimeMs, BlastMath.DetonationTimeMs(f.LaunchMotionTimeMs, distance, f.SpeedKmS)) &&
                f.DetonationMotionTimeMs <= long.MaxValue, "captured distance/speed/fuse mismatch.");
            double direction = (Math.Atan2(dx, -dy) * 180 / Math.PI + 360) % 360;
            var start = new ObjectMotionSnapshot(row.ObjectId, f.StartX, f.StartY, f.SpeedKmS, direction,
                OwnProjectile: f with { X = f.StartX, Y = f.StartY });
            var pose = CurrentManeuverForecast.Sample(start, state.MotionTimeMs - f.LaunchMotionTimeMs);
            Require(Pose(obj, pose) && Near(f.X, pose.X) && Near(f.Y, pose.Y), "captured position/speed/course mismatch.");
            var launch = launchById.GetValueOrDefault(row.ObjectId);
            Require(launch is not null && launch.ActorObjectId.Equals(f.OwnerObjectId, StringComparison.OrdinalIgnoreCase) &&
                launch.ProjectileKind == f.Kind && launch.ProfileId == f.ProfileId && launch.BlastRadiusKm == f.BlastRadiusKm &&
                launch.MotionTimeMs == f.LaunchMotionTimeMs && Near(launch.X, f.StartX) && Near(launch.Y, f.StartY),
                "captured flight does not match its launch fact.");
            bool own = f.OwnerObjectId == state.PlayerShipObjectId;
            Require(own == (f.OwnerPlayerRelationAtLaunch == PlayerRelation.Self) && (own
                ? f.Reporter?.Role == (f.Kind == ProjectileKind.Torpedo ? BattleReportReporterRole.TorpedoOperator : BattleReportReporterRole.CountermeasureOperator)
                : f.Reporter is null), "invalid captured ownership or reporter.");
            if (objects.TryGetValue(f.OwnerObjectId, out var owner))
                Require(owner.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip && !owner.IsDestroyed,
                    "invalid live projectile owner.");
        }
        foreach (var e in point.Evasions)
        {
            Require(objects.TryGetValue(e.ObjectId, out var owner) && owner.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip &&
                !owner.IsDestroyed && e.IntegrationAnchor is not null &&
                owner.Modules?.Any(m => m.ModuleId == e.EngineModuleId && m.ActiveCycle is null) == true,
                "missing evasion actor/engine/anchor.");
            var anchor = e.IntegrationAnchor;
            Require(anchor.Torpedo is null && anchor.Countermeasure is null && anchor.TurnHoldRateDegPerSec is null && anchor.HostileCombat is null &&
                anchor.Orbit is null && anchor.EngineManeuver is null && anchor.ActiveEngineCommandType is null,
                "evasion anchor contains incompatible controls.");
            ObjectMotionSnapshot pose;
            try { pose = CurrentManeuverForecast.Sample(anchor with { Evasion = e.Motion }, state.MotionTimeMs - e.Motion.SampleMotionTimeMs); }
            catch (ArgumentException ex) { throw new ScenarioException("Invalid evasion point continuation.", ex); }
            Require(Pose(owner!, pose), "evasion anchor/pose mismatch.");
            var launch = launchById.GetValueOrDefault(e.Motion.ThreatProjectileId);
            Require(launch is { ProjectileKind: ProjectileKind.Torpedo } &&
                !launch.ActorObjectId.Equals(e.ObjectId, StringComparison.OrdinalIgnoreCase), "evasion has no historical foreign torpedo.");
            if (e.OrbitCycle is { } cycle)
                Require(cycle.CommandType == ShipEngineCommandTypes.Orbit && cycle.ObjectId == e.ObjectId && cycle.ModuleId == e.EngineModuleId &&
                    !string.IsNullOrWhiteSpace(cycle.CommandId) && !string.IsNullOrWhiteSpace(cycle.CycleId) && cycle.DurationMs >= 0 &&
                    cycle.StartedGameTimeMs >= 0 && cycle.StartedGameTimeMs <= e.StartedAt &&
                    e.Motion.BaseMotion.ActiveEngineCommandType == ShipEngineCommandTypes.Orbit, "invalid suspended orbit identity.");
            else Require(e.Motion.BaseMotion.ActiveEngineCommandType is null, "base course contains an unsaved command.");
        }
        foreach (var d in point.DodgeThreats)
        {
            var launch = launchById.GetValueOrDefault(d.ProjectileId);
            Require(launch is { ProjectileKind: ProjectileKind.Torpedo } &&
                !launch.ActorObjectId.Equals(d.OwnerId, StringComparison.OrdinalIgnoreCase), "NPC dodge has no foreign torpedo.");
            if (objects.TryGetValue(d.OwnerId, out var owner))
                Require(owner.ObjectType == SpaceObjectType.NpcShip && !owner.IsDestroyed && owner.Ai is not null &&
                    owner.Modules?.Any(m => m.ModuleId == d.EngineModuleId) == true, "invalid NPC dodge actor/engine.");
            else Require(point.Encounter.EnemyIds.Contains(d.OwnerId), "unknown historical NPC dodge actor.");
        }
        Require(point.PlayerEvasionEpisodes >= point.Encounter.Stats.PlayerEvasions &&
            (point.Encounter.State == CombatEncounterState.Active || point.Encounter.ResumeSpeed is null) &&
            point.Encounter.EnemyIds.All(id => id != state.PlayerShipObjectId &&
                (!objects.TryGetValue(id, out var obj) || obj.ObjectType == SpaceObjectType.NpcShip)) &&
            point.Encounter.ProjectileIds.All(launchById.ContainsKey),
            "invalid encounter membership, resume speed or evasion count.");
    }
}
