using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0032Us0002Tk0002Tests
{
    [Fact]
    public void DestroyedPilotLeavesLiveRosterAndFreesOneCabin()
    {
        using var engine = DemoEp0032Us0001Tk0002Tests.Tutorial();
        var before = engine.CaptureSnapshot().CarrierOperations!;
        var start = DemoEp0032Us0001Tk0002Tests.Start(engine, "loss");
        var session = engine.TacticalBattleSessionForTests!;
        var drone = session.Battle.Drones.First(d => d.Side == session.PlayerSide);
        var owned = before.OwnedDrones.Single(d => d.Id == drone.Id);
        var pilot = before.Crew.Single(p => p.Id == owned.CrewId);
        drone.Status = TacticalDroneStatus.Destroyed;
        var request = new ExitTacticalBattleRequest("exit", start.State!.BattleId);
        engine.ExitTacticalBattle(request);
        engine.ExitTacticalBattle(request);
        var snapshot = engine.CaptureSnapshot();
        var memorial = Assert.Single(snapshot.DemoCampaign!.Company!.PilotLoss!.Memorial);
        Assert.Equal(pilot, memorial.Pilot);
        Assert.Equal(start.State.BattleId, memorial.BattleId);
        Assert.DoesNotContain(snapshot.CarrierOperations!.Crew, p => p.Id == pilot.Id);
        Assert.Equal(before.CabinReservation!.OccupiedCabins - 1, snapshot.CarrierOperations.CabinReservation!.OccupiedCabins);
        Assert.Equal("", snapshot.CarrierOperations.OwnedDrones.Single(d => d.Id == drone.Id).CrewId);

        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true, true);
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(DemoEp0032Us0001Tk0002Tests.ContentRoot + "/Settings.json", out _, out _);
        using var restored = new SimulationEngine(registry);
        restored.LoadScenario(save, isSave: true);
        Assert.Equal(pilot, Assert.Single(restored.CaptureSnapshot().DemoCampaign!.Company!.PilotLoss!.Memorial).Pilot);
        Assert.DoesNotContain(restored.CaptureSnapshot().CarrierOperations!.Crew, p => p.Id == pilot.Id);

        var corrupted = save with { GameState = save.GameState with
        { DemoCampaign = save.GameState.DemoCampaign! with { PilotMemorial = save.GameState.DemoCampaign.PilotMemorial.Add(memorial) } } };
        var stable = ScenarioLoader.Serialize(restored.CaptureSaveState());
        Assert.Throws<ScenarioException>(() => restored.LoadScenario(corrupted, isSave: true));
        Assert.Equal(stable, ScenarioLoader.Serialize(restored.CaptureSaveState()));
    }

    [Fact]
    public void DisabledPilotIsNotKilled()
    {
        using var engine = DemoEp0032Us0001Tk0002Tests.Tutorial();
        var start = DemoEp0032Us0001Tk0002Tests.Start(engine, "disabled");
        var session = engine.TacticalBattleSessionForTests!;
        session.Battle.Drones.First(d => d.Side == session.PlayerSide).Status = TacticalDroneStatus.Disabled;
        engine.ExitTacticalBattle(new("exit", start.State!.BattleId));
        Assert.Empty(engine.CaptureSnapshot().DemoCampaign!.Company!.PilotLoss!.Memorial);
        Assert.Equal(6, engine.CaptureSnapshot().CarrierOperations!.Crew.Length);
    }
}
