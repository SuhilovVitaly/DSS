using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Dialogue;

internal static class StationSecuritySystem
{
    public static DialogueProgressState Update(List<SpaceObjectRuntime> objects, string? playerId,
        DialogueProgressState progress, long time)
    {
        int playerIndex = objects.FindIndex(o => o.InitialMotion.ObjectId == playerId);
        if (playerIndex < 0) return progress;
        var incidents = progress.SecurityIncidents.ToBuilder();
        var predictor = new LinearMotionPredictor();
        for (int i = 0; i < incidents.Count; i++)
        {
            var incident = incidents[i];
            var ship = objects[playerIndex];
            if (incident.Completed || ship.IsDestroyed) continue;
            var station = objects.FirstOrDefault(o => o.InitialMotion.ObjectId == incident.StationObjectId);
            if (station?.SecurityZoneRadiusKm is not { } radius) continue;
            long checkTime = Math.Min(time, incident.DeadlineGameTimeMs);
            var playerMotion = predictor.Predict(ship.InitialMotion, Math.Max(0, checkTime - ship.StartGameTimeMs));
            var stationMotion = predictor.Predict(station.InitialMotion, Math.Max(0, checkTime - station.StartGameTimeMs));
            double dx = playerMotion.X - stationMotion.X, dy = playerMotion.Y - stationMotion.Y;
            if (dx * dx + dy * dy > (radius * 10.0) * (radius * 10.0))
                incidents[i] = incident with { Completed = true };
            else if (time >= incident.DeadlineGameTimeMs)
            {
                objects[playerIndex] = ship with
                {
                    InitialMotion = playerMotion with { SpeedKmS = 0 }, StartGameTimeMs = checkTime,
                    IsDestroyed = true, IsDocked = false, DockedStationObjectId = null,
                    Modules = ship.Modules.Select(m => m with { ActiveCycle = null }).ToImmutableArray()
                };
                incidents[i] = incident with { Completed = true };
            }
        }
        return progress with { SecurityIncidents = incidents.ToImmutable() };
    }
}
