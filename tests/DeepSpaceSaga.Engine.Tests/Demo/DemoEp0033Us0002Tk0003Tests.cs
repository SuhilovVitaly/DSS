using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.Demo.DemoEp0033Us0001Tk0003Tests;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0033Us0002Tk0003Tests
{
    private static string EmptyDecoys(SimulationEngine engine, long credits = 2000)
    {
        string id = Damage(engine, credits);
        var save = engine.CaptureSaveState();
        var ship = save.GameState.SpaceObjects.Single(s => s.ObjectId == save.GameState.PlayerShipObjectId);
        var drones = ship.CarrierState!.OwnedDrones!.Select(d => d.DroneId == id
            ? d with { DecoyId = "decoy.t1", DecoyMaxCharges = 2, DecoyCharges = 0 } : d).ToArray();
        engine.LoadScenario(save with { GameState = save.GameState with { SpaceObjects = save.GameState.SpaceObjects.Select(s =>
            s.ObjectId == ship.ObjectId ? s with { CarrierState = ship.CarrierState with { OwnedDrones = drones } } : s).ToArray() } }, isSave: true);
        return id;
    }
    private static DecoySupplyQuote Quote(SimulationEngine engine, string id)
    {
        var response = engine.ExecuteDemoCampaign(new DecoyQuoteRequest("quote", engine.CaptureSnapshot().DemoCampaign!.Revision, id, 2));
        Assert.True(response.Applied, response.Reason);
        return response.Supply!.Quote!;
    }
    [Fact]
    public void PaidSupply_ReplayAndLoadDoNotMintCharges()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        string id = EmptyDecoys(engine);
        var before = engine.CaptureSnapshot().DemoCampaign!.Company!.Supply!;
        var quote = Quote(engine, id);
        Assert.Equal(50, quote.Total);
        var command = new ResupplyDecoyCommand("supply", quote.Revision, id, quote.Id, 2);
        Assert.True(engine.ExecuteDemoCampaign(command).Applied);
        Assert.Equal(1950, engine.PlayerCredits);
        Assert.Equal(2, engine.CaptureSnapshot().CarrierOperations!.OwnedDrones.Single(d => d.Id == id).DecoyCharges);
        Assert.Equal(before.MaximumShots, engine.CaptureSnapshot().DemoCampaign!.Company!.Supply!.MaximumShots);
        var save = ScenarioLoader.Serialize(engine.CaptureSaveState());
        engine.ExecuteDemoCampaign(command);
        Assert.Equal(save, ScenarioLoader.Serialize(engine.CaptureSaveState()));
        engine.LoadScenario(ScenarioLoader.LoadFromJson(save, true, true), isSave: true);
        engine.ExecuteDemoCampaign(command);
        Assert.Equal(1950, engine.PlayerCredits);
        Assert.Equal(2, engine.CaptureSnapshot().CarrierOperations!.OwnedDrones.Single(d => d.Id == id).DecoyCharges);
    }
    [Fact]
    public void InsufficientFundsAndLoadInvalidatedQuote_DoNotMutateInventory()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        string id = EmptyDecoys(engine, 1);
        var quote = Quote(engine, id);
        var response = engine.ExecuteDemoCampaign(new ResupplyDecoyCommand("poor", quote.Revision, id, quote.Id, 2));
        Assert.Equal(CompanyServiceReasons.InsufficientFunds, response.Reason);
        Assert.Equal(1, engine.PlayerCredits);
        Assert.Equal(0, engine.CaptureSnapshot().CarrierOperations!.OwnedDrones.Single(d => d.Id == id).DecoyCharges);
        engine.LoadScenario(engine.CaptureSaveState(), isSave: true);
        Assert.Equal(CompanyServiceReasons.InvalidQuote, engine.ExecuteDemoCampaign(
            new ResupplyDecoyCommand("after-load", quote.Revision, id, quote.Id, 2)).Reason);
    }
    [Fact]
    public void CapacityAndUndockedService_AreRejected()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        Assert.Equal(CompanyServiceReasons.NotDocked, engine.ExecuteDemoCampaign(new DecoyQuoteRequest("undocked", 0, "unknown", 1)).Reason);
        string id = EmptyDecoys(engine);
        Assert.Equal(CompanyServiceReasons.Unavailable, engine.ExecuteDemoCampaign(new DecoyQuoteRequest("too-many", 0, id, 3)).Reason);
    }
}

