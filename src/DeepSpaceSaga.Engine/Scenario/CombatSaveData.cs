using System.Text.Json.Serialization;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

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
    [property: JsonRequired, JsonPropertyName("processedLaunchCommandIds")] IReadOnlyList<string> ProcessedLaunchCommandIds);

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
    internal static void Validate(GameStateData state, int format)
    {
        static void Require(bool condition, string reason)
        {
            if (!condition) throw new ScenarioException("Invalid combat save: " + reason);
        }
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId, StringComparer.Ordinal);
        foreach (var obj in objects.Values)
        {
            if (obj.ShipClassId is not null && format >= 10)
                Require(obj.HullHitPoints is > 0 && obj.HullHitPointsMax is > 0 &&
                    obj.HullHitPoints <= obj.HullHitPointsMax, "classified ship requires current and maximum HP.");
            if (obj.HullHitPointsMax is not null)
                Require(obj.ShipClassId is not null && obj.HullHitPointsMax > 0 &&
                    obj.HullHitPoints <= obj.HullHitPointsMax, "invalid maximum HP.");
            if (obj.ObjectType == SpaceObjectType.Wreck)
                Require(obj.SpeedMps == 0 && obj.DirectionDegrees == 0 &&
                    string.Equals(obj.MovementType, "Stationary", StringComparison.OrdinalIgnoreCase) &&
                    (obj.Modules?.Count ?? 0) == 0 && (obj.Crew?.Count ?? 0) == 0 &&
                    (obj.Passengers?.Count ?? 0) == 0 && !obj.IsDocked, "wreck must be stationary and empty.");
        }
        var combat = state.CombatState;
        if (combat is null)
        {
            Require(format < 10 && !objects.Values.Any(o => o.ObjectType == SpaceObjectType.Missile), "missing combat state.");
            return;
        }
        Require(combat.Version == 1, "unsupported combat version.");
        Require(combat.LastProcessedMotionTimeMs == state.MotionTimeMs && combat.ProjectileSequence >= 0 &&
            combat.ImpactSequence is >= 0 and < long.MaxValue && combat.WreckSequence is >= 0 and < long.MaxValue,
            "invalid counters or motion time.");
        Require(combat.Launchers is not null && combat.Projectiles is not null && combat.ProcessedLaunchCommandIds is not null,
            "missing combat collections.");
        var launches = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in combat.ProcessedLaunchCommandIds!)
            Require(!string.IsNullOrWhiteSpace(id) && launches.Add(id), "duplicate or empty launch command ID.");
        var launchers = new Dictionary<(string, string), LauncherCombatSnapshot>();
        foreach (var row in combat.Launchers!)
        {
            Require(row is not null && !string.IsNullOrWhiteSpace(row.OwnerObjectId) && !string.IsNullOrWhiteSpace(row.ModuleId), "invalid launcher row.");
            Require(objects.TryGetValue(row!.OwnerObjectId, out var owner) &&
                owner.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip &&
                owner.Modules?.Any(m => m.ModuleId == row.ModuleId && m.ActiveCycle is null) == true, "orphan or cycling launcher.");
            Require(row.State is not null && Parameters(row.State.SpeedKmS, row.State.TurnRateDegPerSec, row.State.Damage) &&
                launchers.TryAdd((row.OwnerObjectId, row.ModuleId), row.State), "invalid or duplicate launcher.");
        }
        var projectileIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in combat.Projectiles!)
        {
            Require(row is not null && !string.IsNullOrWhiteSpace(row.ObjectId) && projectileIds.Add(row.ObjectId), "duplicate projectile.");
            Require(objects.TryGetValue(row!.ObjectId, out var obj) && obj.ObjectType == SpaceObjectType.Missile &&
                (obj.Modules?.Count ?? 0) == 0 && !obj.IsDestroyed && Finite(obj.PositionX, obj.PositionY), "orphan projectile or invalid pose.");
            var f = row.Flight;
            Require(f is not null && Parameters(f.SpeedKmS, f.TurnRateDegPerSec, f.Damage), "invalid captured weapon.");
            CountermeasureSaveValidation.ValidateCapturedTorpedoRating(f!, format);
            Require(!string.IsNullOrWhiteSpace(f!.OwnerObjectId) && !string.IsNullOrWhiteSpace(f.LauncherModuleId) &&
                launchers.TryGetValue((f.OwnerObjectId, f.LauncherModuleId), out var launcher) && launcher.ActiveTorpedoObjectId == row.ObjectId &&
                launcher.SpeedKmS == f.SpeedKmS && launcher.TurnRateDegPerSec == f.TurnRateDegPerSec && launcher.Damage == f.Damage,
                "busy/projectile mismatch.");
            Require(!string.IsNullOrWhiteSpace(f.TargetObjectId) && f.TargetObjectId != f.OwnerObjectId && f.TargetObjectId != row.ObjectId &&
                row.TargetLost == (!objects.TryGetValue(f.TargetObjectId, out var targetObject) || targetObject.IsDestroyed),
                "invalid target or target-lost state.");
            Require(f.LaunchMotionTimeMs >= 0 && f.LaunchMotionTimeMs <= state.MotionTimeMs && f.HitChancePercent == 100 &&
                double.IsFinite(f.DistanceTravelledWorldUnits) && f.DistanceTravelledWorldUnits >= 0 && SameSpeed(obj!.SpeedMps / 1000, f.SpeedKmS),
                "invalid flight time, distance or speed.");
            var route = f.Route;
            Require(route is not null && route.PlannerVersion == TorpedoGuidanceMath.PlannerVersion && Enum.IsDefined(route.Phase) &&
                route.StartMotionTimeMs >= f.LaunchMotionTimeMs && route.StartMotionTimeMs <= state.MotionTimeMs &&
                double.IsFinite(route.ElapsedMs) && route.ElapsedMs == state.MotionTimeMs - route.StartMotionTimeMs &&
                !route.Segments.IsDefaultOrEmpty, "invalid or unsupported route.");
            foreach (var segment in route!.Segments) Require(Segment(segment, f), "invalid route segment.");
            for (int i = 1; i < route.Segments.Length; i++)
            {
                var end = TorpedoGuidanceMath.PredictSegment(route.Segments[i - 1], route.Segments[i - 1].DurationMs);
                var next = route.Segments[i];
                Require(Near(end.X, next.X) && Near(end.Y, next.Y) && HeadingNear(end.Direction, next.Direction), "route geometry gap.");
            }
            Require(route.HasIntercept == (f.PredictedImpactMotionTimeMs is not null) && f.PredictedImpactMotionTimeMs is not < 0,
                "invalid intercept time.");
            double duration = route.Segments.Sum(s => s.DurationMs);
            Require(double.IsFinite(duration), "route duration overflow.");
            if (route.HasIntercept)
                Require(duration < long.MaxValue - route.StartMotionTimeMs &&
                    f.PredictedImpactMotionTimeMs == route.StartMotionTimeMs + (long)Math.Ceiling(duration), "intercept/route mismatch.");
            var pose = TorpedoGuidanceMath.Predict(new ObjectMotionSnapshot(row.ObjectId, obj!.PositionX, obj.PositionY,
                f.SpeedKmS, obj.DirectionDegrees, Torpedo: f), 0);
            Require(Near(pose.X, obj.PositionX) && Near(pose.Y, obj.PositionY) && HeadingNear(pose.Direction, obj.DirectionDegrees), "route/pose mismatch.");
            double time = f.LaunchMotionTimeMs, distance = 0;
            TorpedoRouteSegment? previous = null;
            Require(!f.Trail.IsDefault, "missing full trail.");
            foreach (var trail in f.Trail)
            {
                Require(trail is not null && trail.PlannerVersion == TorpedoGuidanceMath.PlannerVersion && Segment(trail.Segment, f) &&
                    Near(trail.StartMotionTimeMs, time), "invalid or discontinuous trail.");
                if (previous is not null)
                {
                    var end = TorpedoGuidanceMath.PredictSegment(previous, previous.DurationMs);
                    Require(Near(end.X, trail!.Segment.X) && Near(end.Y, trail.Segment.Y) && HeadingNear(end.Direction, trail.Segment.Direction), "trail geometry gap.");
                }
                previous = trail!.Segment;
                time += previous.DurationMs;
                distance += previous.DurationMs * previous.SpeedKmS / 100;
            }
            Require(Near(time, state.MotionTimeMs) && Near(distance, f.DistanceTravelledWorldUnits), "incomplete trail or distance mismatch.");
            if (previous is not null)
            {
                var end = TorpedoGuidanceMath.PredictSegment(previous, previous.DurationMs);
                Require(Near(end.X, obj.PositionX) && Near(end.Y, obj.PositionY) && HeadingNear(end.Direction, obj.DirectionDegrees), "trail/pose mismatch.");
            }
            if (row.TargetLost)
                Require(row.TargetBaseline is null && !route.HasIntercept && route.Segments.All(s => s.AngularVelocityDegPerSec == 0), "invalid lost-target continuation.");
            else
                Require(row.TargetBaseline is { } target && target.ObjectId == f.TargetObjectId &&
                    Finite(target.X, target.Y, target.SpeedKmS, target.Direction) && target.SpeedKmS >= 0 &&
                    row.TargetBaselineMotionTimeMs >= f.LaunchMotionTimeMs && row.TargetBaselineMotionTimeMs <= state.MotionTimeMs,
                    "missing target guidance baseline.");
        }
        Require(objects.Values.Where(o => o.ObjectType == SpaceObjectType.Missile).All(o => projectileIds.Contains(o.ObjectId)), "missile without flight payload.");
        foreach (var launcher in launchers)
            if (launcher.Value.ActiveTorpedoObjectId is { } id)
                Require(combat.Projectiles!.Any(p => p.ObjectId == id && p.Flight.OwnerObjectId == launcher.Key.Item1 &&
                    p.Flight.LauncherModuleId == launcher.Key.Item2), "busy launcher without matching flight.");
        if (projectileIds.Count > 0)
            Require(combat.NextGuidanceMotionTimeMs > state.MotionTimeMs &&
                combat.NextGuidanceMotionTimeMs == (state.MotionTimeMs > long.MaxValue - 100 ? long.MaxValue : state.MotionTimeMs - state.MotionTimeMs % 100 + 100),
                "invalid next guidance boundary.");
    }

    private static bool Parameters(double speed, double turn, int damage) =>
        double.IsFinite(speed) && speed > 0 && double.IsFinite(turn) && turn > 0 && damage > 0;
    private static bool Finite(params double[] values) => values.All(double.IsFinite);
    // A km/s -> m/s -> km/s roundtrip can change the low bits of a valid captured speed.
    private static bool SameSpeed(double a, double b) => double.IsFinite(a) && double.IsFinite(b) &&
        Math.Abs(a - b) <= 1e-12 * Math.Max(Math.Abs(a), Math.Abs(b));
    private static bool Near(double a, double b) => double.IsFinite(a) && double.IsFinite(b) && Math.Abs(a - b) <= 1e-5;
    private static bool HeadingNear(double a, double b) => Math.Abs((a - b + 540) % 360 - 180) <= 1e-5;
    private static bool Segment(TorpedoRouteSegment? s, TorpedoSnapshot f) => s is not null &&
        Finite(s.X, s.Y, s.Direction, s.SpeedKmS, s.AngularVelocityDegPerSec, s.DurationMs) && s.DurationMs >= 0 &&
        s.SpeedKmS == f.SpeedKmS && Math.Abs(s.AngularVelocityDegPerSec) <= f.TurnRateDegPerSec;
}
