using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0036Us0009Tk0002Tests
{
    [Fact]
    public void DisabledEnemyPilot_ArrestedOnce_RepeatAndLoad_NoDuplicate()
    {
        using var engine = DemoEp0036Us0007Tk0003Tests.Won();
        var state = engine.CaptureSnapshot().DemoCampaign!;
        var loot = state.Rewards!.Loot!;
        var source = loot.Objects.First(s => s.Pilot is { Alive: true });
        var command = new ArrestPilotCommand("arrest", state.Revision, loot.BattleId, source.ObjectId);
        Assert.True(engine.ExecuteDemoCampaign(command).Applied);
        Assert.True(engine.ExecuteDemoCampaign(command).Applied);
        Assert.Single(engine.CaptureSnapshot().DemoCampaign!.Company!.Prisoners!.Prisoners);
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true, true);
        engine.LoadScenario(save, true);
        state = engine.CaptureSnapshot().DemoCampaign!;
        Assert.True(state.Rewards!.Loot!.Objects.Single(s => s.ObjectId == source.ObjectId).Pilot!.Captured);
        Assert.Equal(PrisonerCustodyReasons.AlreadyCaptured, engine.ExecuteDemoCampaign(command with { CommandId = "again", ExpectedRevision = state.Revision }).Reason);
        Assert.Single(state.Company!.Prisoners!.Prisoners);
        Assert.True(engine.ExecuteDemoCampaign(new FinishBattleLootCommand("finish", state.Revision, loot.BattleId, true)).Applied);
        Assert.Equal(BattleLootReasons.LootClosed, engine.ExecuteDemoCampaign(command with { CommandId = "closed", ExpectedRevision = state.Revision + 1 }).Reason);
    }
    [Fact]
    public void NoFreeCabin_NoMutation()
    {
        using var engine = DemoEp0036Us0007Tk0003Tests.Won();
        var save = engine.CaptureSaveState();
        save = save with { GameState = save.GameState with { SpaceObjects = save.GameState.SpaceObjects.Select(s =>
            s.ObjectId == save.GameState.PlayerShipObjectId ? s with { Passengers = Enumerable.Range(0, 50).Select(i => new ShipPassengerData("p" + i, "Guest")).ToArray() } : s).ToArray() } };
        engine.LoadScenario(save, true);
        var state = engine.CaptureSnapshot().DemoCampaign!;
        var loot = state.Rewards!.Loot!;
        var response = engine.ExecuteDemoCampaign(new ArrestPilotCommand("full", state.Revision, loot.BattleId, loot.Objects.First(s => s.Pilot is not null).ObjectId));
        Assert.Equal(PrisonerCustodyReasons.NoCabin, response.Reason);
        Assert.Empty(engine.CaptureSnapshot().DemoCampaign!.Company!.Prisoners!.Prisoners);
    }
    [Fact]
    public void DestroyedOrFriendlyPilot_Rejected()
    {
        using var engine = DemoEp0036Us0007Tk0003Tests.Won();
        var state = engine.CaptureSnapshot().DemoCampaign!;
        var loot = state.Rewards!.Loot!;
        foreach (var id in new[] { "friendly", loot.Objects.First(s => s.Kind == "DestroyedDrone").ObjectId })
            Assert.Equal(PrisonerCustodyReasons.NotCapturable, engine.ExecuteDemoCampaign(new ArrestPilotCommand(id, state.Revision, loot.BattleId, id)).Reason);
    }
    [Fact]
    public void ForgedPrisonerProvenance_Rejected()
    {
        using var engine = DemoEp0036Us0007Tk0003Tests.Won();
        var save = engine.CaptureSaveState();
        var campaign = save.GameState.DemoCampaign!;
        Assert.Throws<ScenarioException>(() => DemoCampaignData.Validate(campaign with { Prisoners = [new("prisoner:x", "ship", "battle", "drone", "x", "Fake", null, 0)] }, save.GameState.GameTimeMs));
    }
}
