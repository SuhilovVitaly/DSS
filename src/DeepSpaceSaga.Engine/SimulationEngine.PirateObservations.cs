using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private void RememberPirateObservations(long? committedMotionTime = null)
    {
        if (_demoCampaign.PirateWorld is not { } world) return;
        var player = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (player is null) return;
        long time = committedMotionTime ?? _processedSimulationTimeMs;
        var pose = PredictMotion(player, Math.Max(0, time - player.StartGameTimeMs));
        double radius = EvaluateCarrierSensor(player)?.RadiusWorldUnits ?? 0;
        var groups = world.Groups;
        for (int i = 0; i < groups.Length; i++)
        {
            var group = groups[i];
            // Refresh existing memory only at the shared controller boundary. Arbitrary
            // snapshot/save partitions must not change the final last-known observation.
            if (group.PlayerObservation is not null &&
                (committedMotionTime is null || group.MotionPlan?.EndMs != time)) continue;
            if (group.Retired || !SensorCovers(radius, pose.X, pose.Y, group.X, group.Y)) continue;
            double angle = group.Direction * Math.PI / 180;
            var observed = new PirateObservationData(group.X, group.Y, Math.Sin(angle) * group.SpeedKmS * 10,
                -Math.Cos(angle) * group.SpeedKmS * 10, time, ObservePursuit(group, pose, time));
            if (group.PlayerObservation != observed)
                groups = groups.SetItem(i, group with { PlayerObservation = observed });
        }
        if (groups != world.Groups) _demoCampaign = _demoCampaign with { PirateWorld = world with { Groups = groups } };
    }
}
