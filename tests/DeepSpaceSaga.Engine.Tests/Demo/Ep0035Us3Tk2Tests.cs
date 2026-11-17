using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class Ep0035Us3Tk2Tests
{
    internal static ScenarioFile CapturedScenario(SimulationEngine engine)
    {
        var save = DemoEp0035Us0010Tk0002Tests.ContactScenario(engine);
        var campaign = save.GameState.DemoCampaign!;
        var world = campaign.PirateWorld!;
        return save with { GameState = save.GameState with { DemoCampaign = campaign with
        { PirateWorld = world with { Groups = world.Groups.SetItem(0, world.Groups[0] with
        { Phase = "captured", CapturedMotionTimeMs = world.LastMotionTimeMs, MotionPlan = null }) } } } };
    }

    [Fact]
    public void CaptureRequiresReportAcknowledgementThenStartsExactOwnedBattleAndRejectsEscape()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        engine.LoadScenario(CapturedScenario(engine), true);
        var campaign = engine.CaptureSnapshot().DemoCampaign!;
        var encounter = campaign.WorldEncounter!;
        Assert.False(encounter.CanContinue);
        Assert.False(engine.ExecuteDemoCampaign(new BeginEncounterBattleCommand("early", campaign.Revision, encounter.Binding.EncounterId)).Applied);
        Assert.Equal("encounter_captured", engine.TrySetSpeed(SimulationSpeed.Speed1));
        Assert.True(engine.ExecuteDemoCampaign(new DismissSensorContactReportRequest("ack", campaign.Revision, campaign.PendingSensorReport!.BatchId)).Applied);
        var ready = engine.CaptureSnapshot().DemoCampaign!;
        Assert.True(ready.WorldEncounter!.CanContinue);
        var save = engine.CaptureSaveState();
        using var loaded = DemoEp0033Us0001Tk0003Tests.Demo();
        loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true), true);
        var command = new BeginEncounterBattleCommand("begin", ready.Revision, encounter.Binding.EncounterId);
        var accepted = loaded.ExecuteDemoCampaign(command);
        Assert.True(accepted.Applied);
        Assert.Equal(accepted, loaded.ExecuteDemoCampaign(command));
        var battle = loaded.GetTacticalBattleState()!;
        Assert.Equal(encounter.Binding.BattleId, battle.BattleId);
        var expected = save.GameState.DemoCampaign!.PirateCompositions.Where(c => c.GroupId == encounter.Binding.GroupId)
            .SelectMany(c => c.Drones).Select(d => d.DroneId).Order().ToArray();
        Assert.Equal(expected, battle.Units.Where(u => u.Side == "B" && u.Kind == TacticalUnitKind.Drone).Select(u => u.Id).Order());
        Assert.Equal(TacticalBattleRejectionCode.WrongPhase, loaded.ExitTacticalBattle(new("escape", battle.BattleId)).Code);
        Assert.Throws<InvalidOperationException>(() => loaded.LoadScenario(save, true));
    }
}

