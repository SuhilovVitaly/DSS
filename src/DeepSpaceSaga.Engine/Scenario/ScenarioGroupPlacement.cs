using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Scenario;

internal static class ScenarioGroupPlacement
{
    internal static ScenarioFile Translate(ScenarioFile source, double playerX, double playerY)
    {
        if (!double.IsFinite(playerX) || !double.IsFinite(playerY)) throw new ScenarioException("Non-finite scenario translation.");
        var objects = source.GameState.SpaceObjects.ToDictionary(o => o.ObjectId, StringComparer.OrdinalIgnoreCase);
        SpaceObjectData Normalize(SpaceObjectData obj)
        {
            if (!obj.IsDocked) return obj;
            if (obj.DockedStationObjectId is null || !objects.TryGetValue(obj.DockedStationObjectId, out var parent) ||
                parent.ObjectType != SpaceObjectType.Station || parent.IsDocked)
                throw new ScenarioException($"{obj.ObjectId}: invalid docked scenario parent.");
            return obj with { PositionX = parent.PositionX + 1, PositionY = parent.PositionY + 1 };
        }
        var player = Normalize(objects[source.GameState.PlayerShipObjectId]);
        double dx = playerX - player.PositionX, dy = playerY - player.PositionY;
        var translated = source.GameState.SpaceObjects.Select(Normalize).Select(o => o with
        {
            PositionX = o.PositionX + dx,
            PositionY = o.PositionY + dy
        }).ToArray();
        if (translated.Any(o => !double.IsFinite(o.PositionX) || !double.IsFinite(o.PositionY)))
            throw new ScenarioException("Scenario translation exceeds finite coordinates.");
        return source with { GameState = source.GameState with { SpaceObjects = translated } };
    }

    /// <summary>Validate the seeded placement at its original epoch; the live save remains untouched.</summary>
    internal static GameStateData InitialGeometry(GameStateData state)
    {
        if (state.SolarSystem is null) return state;
        return state with
        {
            SpaceObjects = state.SpaceObjects.Select(o =>
        {
            if (o.Orbit is not { } orbit) return o;
            var pose = OrbitalMotionMath.At(new(o.ObjectId, 0, 0, 0, 0, WorldOffsetX: o.WorldOffsetX, WorldOffsetY: o.WorldOffsetY),
                orbit, orbit.EpochSimulationTimeMs);
            return o with { PositionX = pose.X, PositionY = pose.Y, SpeedMps = 0, DirectionDegrees = 0, MovementType = "Stationary" };
        }).ToArray()
        };
    }
}
