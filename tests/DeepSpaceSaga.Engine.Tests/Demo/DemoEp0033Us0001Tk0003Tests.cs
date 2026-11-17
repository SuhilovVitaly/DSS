using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0033Us0001Tk0003Tests
{
    internal static string Root([System.Runtime.CompilerServices.CallerFilePath] string file = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, "../../../src/DeepSpaceSaga.Client"));
    internal static SimulationEngine Demo() => EngineContentLoader.CreateEngineFromScenarioFile(
        Root() + "/Settings.json", Root() + "/Scenarios/Demo/scenario.json");

    internal static string Damage(SimulationEngine engine, long credits = 2000)
    {
        var save = engine.CaptureSaveState();
        string shipId = save.GameState.PlayerShipObjectId;
        var ship = save.GameState.SpaceObjects.Single(s => s.ObjectId == shipId);
        var drones = ship.CarrierState!.OwnedDrones!.ToArray();
        drones[0] = drones[0] with { CurrentHull = drones[0].MaxHull - 2, Armor = drones[0].MaxArmor - 1, Jammed = true };
        var changed = ship with { CarrierState = ship.CarrierState with { OwnedDrones = drones } };
        engine.LoadScenario(save with { GameState = save.GameState with { PlayerTokens = credits,
            SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId == shipId ? changed : s).ToArray() } }, isSave: true);
        return drones[0].DroneId;
    }

    internal static RepairQuote Quote(SimulationEngine engine, string target, string id = "quote")
    {
        var response = engine.ExecuteDemoCampaign(new RepairQuoteRequest(id, engine.CaptureSnapshot().DemoCampaign!.Revision, target));
        Assert.True(response.Applied, response.Reason);
        return response.StationRepair!.Quote!;
    }

    [Fact]
    public void HappyPath_MatchesStory()
    {
        using var engine = Demo();
        string target = Damage(engine);
        var before = engine.CaptureSnapshot();
        var quote = Quote(engine, target);
        Assert.Equal(75, quote.Total);
        var response = engine.ExecuteDemoCampaign(new RepairCommand("repair", quote.Revision, quote.Id, target));
        Assert.True(response.Applied, response.Reason);
        var after = engine.CaptureSnapshot();
        Assert.Equal(before.PlayerCredits - 75, after.PlayerCredits);
        var repaired = after.CarrierOperations!.OwnedDrones.Single(d => d.Id == target);
        Assert.Equal(repaired.MaxHull, repaired.CurrentHull);
        Assert.Equal(repaired.MaxArmor, repaired.Armor);
        Assert.False(repaired.Jammed);
        Assert.Equal(before.CarrierOperations!.OwnedDrones[0].AmmoCurrent, repaired.AmmoCurrent);
        Assert.Equal(before.CarrierOperations.OwnedDrones[0].DecoyCharges, repaired.DecoyCharges);
        Assert.Single(response.StationRepair!.Postings);
    }

    [Fact]
    public void InvalidOrDuplicate_NoPartialMutation()
    {
        using var engine = Demo();
        string target = Damage(engine, 1);
        var quote = Quote(engine, target);
        var rejected = engine.ExecuteDemoCampaign(new RepairCommand("poor", quote.Revision, quote.Id, target));
        Assert.Equal(CompanyServiceReasons.InsufficientFunds, rejected.Reason);
        Assert.Equal(1, engine.PlayerCredits);
        Assert.True(engine.CaptureSnapshot().CarrierOperations!.OwnedDrones[0].Jammed);
        target = Damage(engine);
        quote = Quote(engine, target, "quote2");
        var request = new RepairCommand("repair", quote.Revision, quote.Id, target);
        var applied = engine.ExecuteDemoCampaign(request);
        var saved = ScenarioLoader.Serialize(engine.CaptureSaveState());
        Assert.Equal(applied, engine.ExecuteDemoCampaign(request));
        Assert.Equal(saved, ScenarioLoader.Serialize(engine.CaptureSaveState()));
        Assert.Equal(DemoCampaignReasons.IdentityConflict, engine.ExecuteDemoCampaign(request with { TargetId = "other" }).Reason);
    }

    [Fact]
    public void SaveLoad_PreservesOutcome()
    {
        using var engine = Demo();
        string target = Damage(engine);
        var quote = Quote(engine, target);
        var request = new RepairCommand("repair", quote.Revision, quote.Id, target);
        Assert.True(engine.ExecuteDemoCampaign(request).Applied);
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true, true);
        engine.LoadScenario(save, isSave: true);
        long balance = engine.PlayerCredits;
        Assert.True(engine.ExecuteDemoCampaign(request).Applied);
        Assert.Equal(balance, engine.PlayerCredits);
        Assert.Single(engine.CaptureSnapshot().DemoCampaign!.Company!.StationRepair!.Postings);
    }

    [Fact]
    public void LoadInvalidatesQuoteAndLegacyWorldReceivesNoServices()
    {
        using var engine = Demo();
        string target = Damage(engine);
        var quote = Quote(engine, target);
        engine.LoadScenario(engine.CaptureSaveState(), isSave: true);
        Assert.Equal(CompanyServiceReasons.InvalidQuote,
            engine.ExecuteDemoCampaign(new RepairCommand("old", quote.Revision, quote.Id, target)).Reason);
        using var legacy = DemoEp0032Us0001Tk0002Tests.Tutorial();
        Assert.False(legacy.ExecuteDemoCampaign(new RepairQuoteRequest("legacy", 0, "SPC-0001")).Applied);
    }
}
