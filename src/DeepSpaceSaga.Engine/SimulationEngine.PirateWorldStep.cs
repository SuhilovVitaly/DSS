using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private SimulationClockState? _piratePauseClock;
    private bool PirateWorldMustRemainPaused => _demoCampaign.PirateWorld is not null &&
        (_demoCampaign.PendingSensorReport is not null || HasCapturedEncounter || CombatOrLootOpen || _encounterStoragePending);

    private bool PreparePirateWorldStep()
    {
        _piratePauseClock = null;
        if (!PirateWorldMustRemainPaused)
        {
            CaptureCurrentPirateContact();
            ObservePirateGroups(_processedWorldTimeMs);
            PublishCapturedEncounter();
        }
        if (!PirateWorldMustRemainPaused) return false;
        var current = _clock.Capture();
        // No new movement is committed here; retain the saved fractional phase.
        _piratePauseClock = new(_processedWorldTimeMs, SimulationSpeed.Speed0, _processedSimulationTimeMs,
            current.MotionTimeMs == _processedSimulationTimeMs ? current.MotionQuarterRemainder : 0);
        return FinishPirateWorldStep();
    }

    private void CaptureCurrentPirateContact()
    {
        if (_demoCampaign.PirateWorld is not { } world || world.Groups.Any(g => !g.Retired && g.Phase == "captured")) return;
        var player = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId && !o.IsDestroyed);
        if (player is null) return;
        var pose = PredictMotion(player, _processedSimulationTimeMs - player.StartGameTimeMs);
        var start = _objects.Single(o => o.InitialMotion.ObjectId == _demoDefinitions.Cluster!.StartStationId).InitialMotion;
        if (Distance(pose.X, pose.Y, start.X, start.Y) <= 8000) return;
        var hit = world.Groups.Where(g => !g.Retired && PirateGroupCapabilities(g, world).SensorRadius >= 200 &&
            Distance(pose.X, pose.Y, g.X, g.Y) <= 200).OrderBy(g => g.GroupId, StringComparer.Ordinal).FirstOrDefault();
        if (hit is null) return;
        int index = world.Groups.IndexOf(hit);
        _demoCampaign = _demoCampaign with
        {
            PirateWorld = world with
            {
                Groups = world.Groups.SetItem(index,
            hit with { Phase = "captured", CapturedMotionTimeMs = _processedSimulationTimeMs, MotionPlan = null })
            }
        };
    }

    private bool FinishPirateWorldStep()
    {
        if (_piratePauseClock is not { } stopped) return false;
        _processedWorldTimeMs = stopped.GameTimeMs;
        _processedSimulationTimeMs = _appliedMotionTimeMs = stopped.MotionTimeMs;
        SynchronizeOrbitalBindings(_objects, stopped.MotionTimeMs);
        UpdateVoyageForMotion(stopped.MotionTimeMs);
        _clock.Reset(stopped.GameTimeMs, SimulationSpeed.Speed0, stopped.MotionTimeMs, stopped.MotionQuarterRemainder);
        PublishCapturedEncounter();
        return true;
    }
}
