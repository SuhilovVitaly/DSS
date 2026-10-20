using System.Text.Json.Serialization;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Physical AI decision cursor and captured per-actor motion/encounter state.</summary>
public sealed record HostileCombatSaveData(
    [property: JsonRequired] int Version,
    [property: JsonRequired] long NextDecisionMotionTimeMs,
    [property: JsonRequired] IReadOnlyList<HostileCombatActorSaveData> Actors)
{
    public const int CurrentVersion = 3;
}

public sealed record HostileCombatActorSaveData(
    [property: JsonRequired] string ObjectId,
    [property: JsonRequired] HostileCombatSnapshot State,
    [property: JsonRequired] bool ActiveMotion);

internal static class HostileCombatSaveValidation
{
    internal static GameStateData ValidateAndNormalize(GameStateData state, int format)
    {
        static void Require(bool condition, string reason)
        {
            if (!condition) throw new ScenarioException("Invalid hostile combat save: " + reason);
        }
        var saved = state.HostileCombatState;
        if (saved is null) { Require(format < 16, "missing state"); return state; }
        Require((format >= 18 ? saved.Version == HostileCombatSaveData.CurrentVersion : format >= 17 ? saved.Version == 2 : saved.Version is 1 or 2) && saved.Actors is not null &&
            saved.NextDecisionMotionTimeMs >= 0, "invalid version, cursor or actors");
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId, StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<HostileCombatActorSaveData>();
        foreach (var row in saved.Actors!)
        {
            Require(row is not null && !string.IsNullOrWhiteSpace(row.ObjectId) && row.State is not null && seen.Add(row.ObjectId), "invalid or duplicate actor");
            Require(objects.TryGetValue(row!.ObjectId, out var owner) && owner.ObjectType == SpaceObjectType.NpcShip &&
                !owner.IsDestroyed && owner.RelationToPlayer == PlayerRelation.Enemy, "invalid owner");
            var h = row.State!;
            Require(h.Version == 1 && Enum.IsDefined(h.Phase) && h.Segment is not null &&
                double.IsFinite(h.DesiredRadiusKm) && h.DesiredRadiusKm > 0 &&
                string.Equals(h.TargetObjectId, state.PlayerShipObjectId, StringComparison.OrdinalIgnoreCase), "invalid phase or target");
            var engine = owner!.Modules?.FirstOrDefault(m => string.Equals(m.ModuleId, h.EngineModuleId, StringComparison.OrdinalIgnoreCase));
            var weapon = state.CombatState?.Launchers?.FirstOrDefault(l =>
                string.Equals(l.OwnerObjectId, owner.ObjectId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(l.ModuleId, h.WeaponModuleId, StringComparison.OrdinalIgnoreCase));
            Require(engine is not null && weapon?.State is not null &&
                h.DesiredRadiusKm == (owner.Ai?.StandoffKm ?? weapon.State.RangeKm / 2),
                "missing modules or radius mismatch");
            var s = h.Segment!;
            Require(s.Version == 1 && s.EpochMs >= 0 && s.EpochMs <= state.MotionTimeMs &&
                s.DurationMs == HostileOrbitMath.DecisionIntervalMs && s.ElapsedMs >= 0 && s.ElapsedMs <= s.DurationMs &&
                new[] { s.StartX, s.StartY, s.StartSpeedKmS, s.StartDirectionDegrees, s.AccelerationKmS2, s.TurnRateDegPerSec }.All(double.IsFinite) &&
                s.StartSpeedKmS >= 0 && s.StartSpeedKmS + s.AccelerationKmS2 * s.DurationMs / 1000 >= -1e-9, "invalid segment");
            if (row.ActiveMotion)
            {
                Require(!owner.IsDocked && engine!.PowerState == "On" && engine.OperationalState == "Ready" && engine.ActiveCycle is null &&
                    s.ElapsedMs == state.MotionTimeMs - s.EpochMs && s.EpochMs % HostileOrbitMath.DecisionIntervalMs == 0 &&
                    s.EpochMs <= long.MaxValue - s.DurationMs && saved.NextDecisionMotionTimeMs == s.EpochMs + s.DurationMs &&
                    saved.NextDecisionMotionTimeMs > state.MotionTimeMs, "divergent motion cursor");
                var pose = HostileOrbitMath.Sample(s, s.ElapsedMs);
                Require(Near(pose.X, owner.PositionX) && Near(pose.Y, owner.PositionY) &&
                    Near(pose.SpeedKmS, owner.SpeedMps / 1000) && Near((pose.Direction - owner.DirectionDegrees + 540) % 360, 180), "pose mismatch");
            }
            rows.Add(row with
            {
                ObjectId = owner.ObjectId,
                State = h with
                { TargetObjectId = state.PlayerShipObjectId, EngineModuleId = engine!.ModuleId, WeaponModuleId = weapon!.ModuleId }
            });
        }
        return state with { HostileCombatState = saved with { Actors = rows } };
    }

    private static bool Near(double a, double b) => double.IsFinite(a) && double.IsFinite(b) && Math.Abs(a - b) <= 1e-5;
}
