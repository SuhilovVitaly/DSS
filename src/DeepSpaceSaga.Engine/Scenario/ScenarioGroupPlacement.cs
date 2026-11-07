using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Scenario;

internal static class ScenarioGroupPlacement
{
    /// <summary>Move source-local objects to the relocated anchor; station orbits remain authoritative.</summary>
    internal static ScenarioFile BindV2Start(ScenarioFile original, ClusterGenerationResult candidate, double desiredRadius)
    {
        var old = original.GameState;
        var state = candidate.World.GameState;
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId, StringComparer.Ordinal);
        var oldPlayer = old.SpaceObjects.Single(o => o.ObjectId == old.PlayerShipObjectId);
        string? anchorId = old.TradingMapGeneration?.StartStationObjectId ?? old.TradingMap?.Rules.StartStationObjectId ??
            oldPlayer.DockedStationObjectId ?? old.SpaceObjects.Where(o => o.ObjectType == SpaceObjectType.Station)
                .OrderBy(o => double.Hypot(o.PositionX - oldPlayer.PositionX, o.PositionY - oldPlayer.PositionY))
                .ThenBy(o => o.ObjectId, StringComparer.Ordinal).FirstOrDefault()?.ObjectId;
        // With no source station, SolarSystemGenerator has already placed the actual
        // player/local group. New station identities must not become a teleport anchor.
        if (anchorId is null || !old.SpaceObjects.Any(o => o.ObjectId == anchorId && o.ObjectType == SpaceObjectType.Station))
            return candidate.World;
        var oldAnchor = old.SpaceObjects.Single(o => o.ObjectId == anchorId);
        if (!objects.TryGetValue(anchorId, out var anchor)) throw new ScenarioException("geography/v2: missing start anchor.");
        double vx = oldPlayer.IsDocked ? 1 : oldPlayer.PositionX - oldAnchor.PositionX;
        double vy = oldPlayer.IsDocked ? 1 : oldPlayer.PositionY - oldAnchor.PositionY;
        double radius = double.Hypot(anchor.PositionX, anchor.PositionY);
        double ux = anchor.PositionX / radius, uy = anchor.PositionY / radius;
        double projection = ux * vx + uy * vy;
        double discriminant = desiredRadius * desiredRadius - vx * vx - vy * vy + projection * projection;
        double correctedRadius = -projection + Math.Sqrt(discriminant);
        if (!double.IsFinite(correctedRadius) || correctedRadius <= 0)
            throw new ScenarioException("geography/v2: local start vector cannot fit target solar distance.");
        var home = candidate.Map.Clusters.Single(c => c.Id == candidate.Map.StartClusterId);
        foreach (string id in home.StationIds)
        {
            var station = objects[id];
            objects[id] = station with
            {
                PositionX = station.PositionX * correctedRadius / radius,
                PositionY = station.PositionY * correctedRadius / radius,
                Orbit = station.Orbit! with { SemiMajorAxis = correctedRadius, SemiMinorAxis = correctedRadius }
            };
        }
        anchor = objects[anchorId];
        foreach (var obj in old.SpaceObjects.Where(o => o.ObjectType is not (SpaceObjectType.Station or SpaceObjectType.Sun or SpaceObjectType.Planet)))
        {
            var current = objects[obj.ObjectId];
            if (obj.IsDocked)
            {
                if (obj.DockedStationObjectId is null || !objects.TryGetValue(obj.DockedStationObjectId, out var parent))
                    throw new ScenarioException("geography/v2: missing docking parent.");
                objects[obj.ObjectId] = current with { PositionX = parent.PositionX + 1, PositionY = parent.PositionY + 1 };
            }
            else objects[obj.ObjectId] = current with
            { PositionX = anchor.PositionX + obj.PositionX - oldAnchor.PositionX, PositionY = anchor.PositionY + obj.PositionY - oldAnchor.PositionY };
        }
        return candidate.World with
        {
            GameState = state with
            {
                SpaceObjects = [.. objects.Values.OrderBy(o => o.ObjectId, StringComparer.Ordinal)],
                SolarSystem = state.SolarSystem! with
                { Orbits = [.. objects.Values.Where(o => o.Orbit is not null).OrderBy(o => o.ObjectId, StringComparer.Ordinal).Select(o => new OrbitMapData(o.ObjectId, o.Orbit!))] }
            }
        };
    }

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
