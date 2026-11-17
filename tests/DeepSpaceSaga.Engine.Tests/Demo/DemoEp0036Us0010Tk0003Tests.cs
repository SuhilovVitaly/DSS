using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0036Us0010Tk0003Tests
{
    [Fact]
    public void JettisonFreesMass_DuplicateAndLoadPreserveReceipt()
    {
        using var engine = DemoEp0036Us0007Tk0003Tests.Won();
        var state = engine.CaptureSnapshot().DemoCampaign!;
        var loot = state.Rewards!.Loot!;
        var item = loot.Cargo.First(c => c.Quantity >= c.QuantityStep);
        long credits = engine.PlayerCredits;
        var command = new JettisonCargoCommand("discard", state.Revision, loot.BattleId, item.ItemTypeId, item.QuantityStep);
        Assert.True(engine.ExecuteDemoCampaign(command).Applied);
        Assert.True(engine.ExecuteDemoCampaign(command).Applied);
        var after = engine.CaptureSnapshot().DemoCampaign!.Rewards!.Loot!;
        Assert.Equal(loot.FreeCargoKg + item.UnitMassKg * item.QuantityStep, after.FreeCargoKg);
        Assert.Equal(item.Quantity - item.QuantityStep, after.Cargo.FirstOrDefault(c => c.ItemTypeId == item.ItemTypeId)?.Quantity ?? 0);
        Assert.Equal(credits, engine.PlayerCredits);
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true, true);
        engine.LoadScenario(save, true);
        Assert.Single(engine.CaptureSaveState().GameState.DemoCampaign!.LootSession!.Jettison);
        Assert.True(engine.ExecuteDemoCampaign(command).Applied);
        Assert.Equal(after.FreeCargoKg, engine.CaptureSnapshot().DemoCampaign!.Rewards!.Loot!.FreeCargoKg);
    }
    [Theory]
    [InlineData("fuel", 1, BattleLootReasons.NotCargoItem)]
    [InlineData("passenger", 1, BattleLootReasons.NotCargoItem)]
    [InlineData("item.food-rations", 0, BattleLootReasons.InvalidQuantity)]
    [InlineData("item.food-rations", long.MaxValue, BattleLootReasons.NotEnoughQuantity)]
    public void InvalidCargoNeverMutates(string item, long quantity, string reason)
    {
        using var engine = DemoEp0036Us0007Tk0003Tests.Won();
        var state = engine.CaptureSnapshot().DemoCampaign!;
        var before = engine.CaptureSnapshot().InstalledModules;
        var response = engine.ExecuteDemoCampaign(new JettisonCargoCommand("invalid", state.Revision, state.Rewards!.Loot!.BattleId, item, quantity));
        Assert.Equal(reason, response.Reason);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(before), System.Text.Json.JsonSerializer.Serialize(engine.CaptureSnapshot().InstalledModules));
    }
    [Fact]
    public void UnknownBasisPreserved()
    {
        var result = SimulationEngine.RemoveCargoCost(new(0, 10, null, [CargoAcquisitionSources.LegacyUnknown]), 3);
        Assert.Equal(7, result.Remaining!.Quantity); Assert.Null(result.Remaining.CostBasisCredits);
    }
}
