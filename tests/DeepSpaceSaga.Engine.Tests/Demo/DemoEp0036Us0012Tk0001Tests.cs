using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.Demo;

[Collection("Demo process debug")]
public sealed class DemoEp0036Us0012Tk0001Tests
{
    [Fact]
    public void ForcedKeepAndSuccessRecordedOnce_OverrideNotPersisted()
    {
        try
        {
            using var engine = Ep0035Us15Tk3Tests.Begin();
            int skill = engine.CaptureSnapshot().CarrierOperations!.Crew.Single(c => c.Role == CrewRole.Engineer).SalvageBonusPercentagePoints;
            var revision = engine.CaptureSnapshot().DemoCampaign!.Revision;
            Assert.True(engine.ExecuteDemoCampaign(new SetLootDebugRequest("debug", revision, 15, "keep", "success")).Applied);
            Assert.Equal(.15, engine.GetAssignedEngineerBonusFraction());
            Assert.Equal(skill, engine.CaptureSnapshot().CarrierOperations!.Crew.Single(c => c.Role == CrewRole.Engineer).SalvageBonusPercentagePoints);
            var battle = engine.TacticalBattleSessionForTests!;
            foreach (var drone in battle.Battle.Drones.Where(d => d.Side != battle.PlayerSide)) drone.Status = TacticalDroneStatus.Disabled;
            battle.Battle.CheckVictory(); Assert.True(engine.ExitTacticalBattle(new("loot", battle.Id)).Accepted);
            var state = engine.CaptureSaveState().GameState.DemoCampaign!;
            Assert.All(state.LootSession!.Sources.SelectMany(s => s.Modules), m => Assert.False(m.Destroyed));
            var source = state.LootSession.Sources.First(s => s.Modules.Length > 0);
            var module = source.Modules[0];
            Assert.True(engine.ExecuteDemoCampaign(new AttemptModuleSalvageCommand("attempt", state.Revision, battle.Id, source.ObjectId, module.InstanceId)).Applied);
            var saved = engine.CaptureSaveState();
            Assert.DoesNotContain("DestructionRolls", ScenarioLoader.Serialize(saved));
            SimulationEngine.SetLootDebug(new("reset", 0));
            engine.LoadScenario(saved, true);
            var restored = engine.CaptureSaveState().GameState.DemoCampaign!.LootSession!;
            Assert.Equal(LootAttemptStates.Collected, restored.Sources.Single(s => s.ObjectId == source.ObjectId).Modules.Single(m => m.InstanceId == module.InstanceId).State);
            Assert.All(restored.Sources.SelectMany(s => s.Modules), m => Assert.False(m.Destroyed));
            Assert.Null(engine.CaptureSnapshot().DemoCampaign!.DebugSettings!.EngineerBonusPp);
        }
        finally { SimulationEngine.SetLootDebug(new("reset", 0)); }
    }
    [Fact]
    public void InvalidRequestDoesNotChangeSettings()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var before = engine.CaptureSnapshot().DemoCampaign!;
        Assert.Equal(DebugTuningReasonCodes.ValueOutOfRange, engine.ExecuteDemoCampaign(new SetLootDebugRequest("bad", before.Revision, 9, "invalid", "success")).Reason);
        Assert.Equal(before.DebugSettings, engine.CaptureSnapshot().DemoCampaign!.DebugSettings);
    }
}
