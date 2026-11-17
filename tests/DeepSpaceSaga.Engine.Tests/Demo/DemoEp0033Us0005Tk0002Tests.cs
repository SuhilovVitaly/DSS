using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0033Us0005Tk0002Tests
{
    private const long Day = 86400000;
    private static void Credits(SimulationEngine engine, long credits)
    {
        var save = engine.CaptureSaveState();
        engine.LoadScenario(save with { GameState = save.GameState with { PlayerTokens = credits } }, isSave: true);
    }
    [Fact]
    public void CalendarBoundaryAndLoadDoNotDoubleCharge()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        Credits(engine, 5000);
        int crew = engine.CaptureSnapshot().CarrierOperations!.Crew.Length;
        var first = engine.CaptureSaveStateForTests(Day, SimulationSpeed.Speed0, simulationTimeMs: 0);
        var ledger = first.GameState.DemoCampaign!.Company!.Upkeep!;
        Assert.Equal(crew * 20, ledger.Accrued);
        Assert.Equal(ledger.Accrued, ledger.Paid);
        Assert.Equal(0, ledger.Outstanding);
        var repeat = engine.CaptureSaveStateForTests(Day, SimulationSpeed.Speed0, simulationTimeMs: 0);
        Assert.Equal(first.GameState.PlayerTokens, repeat.GameState.PlayerTokens);
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(first), true, true), isSave: true);
        Assert.Equal(ledger.Accrued, engine.CaptureSnapshot().DemoCampaign!.Company!.Upkeep!.Accrued);
        Assert.Equal(ledger.Accrued, engine.CaptureSnapshot().DemoCampaign!.Company!.Upkeep!.Accrued);
    }
    [Fact]
    public void DebtPaymentIsExplicitAtomicAndIdempotent()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        Credits(engine, 1);
        var save = engine.CaptureSaveStateForTests(Day, SimulationSpeed.Speed0, simulationTimeMs: 0);
        var ledger = save.GameState.DemoCampaign!.Company!.Upkeep!;
        Assert.True(ledger.Outstanding > 20);
        Assert.Equal(1, ledger.Paid);
        engine.LoadScenario(save with { GameState = save.GameState with { PlayerTokens = 100 } }, isSave: true);
        long revision = engine.CaptureSnapshot().DemoCampaign!.Revision;
        var command = new PayCrewDebtCommand("pay", revision, 20);
        Assert.True(engine.ExecuteDemoCampaign(command).Applied);
        Assert.Equal(80, engine.PlayerCredits);
        engine.ExecuteDemoCampaign(command);
        Assert.Equal(80, engine.PlayerCredits);
        Assert.Equal(ledger.Outstanding - 20, engine.CaptureSnapshot().DemoCampaign!.Company!.Upkeep!.Outstanding);
        Assert.False(engine.ExecuteDemoCampaign(new PayCrewDebtCommand("too-much", revision + 1, long.MaxValue)).Applied);
        Assert.Equal(80, engine.PlayerCredits);
        for (int i = 0; i < 1030; i++) engine.ExecuteDemoCampaign(new PayCrewDebtCommand("rejected-" + i, revision + 1, long.MaxValue));
        Assert.Equal(DemoCampaignReasons.AlreadyApplied, engine.ExecuteDemoCampaign(command).Reason);
        Assert.Equal(DemoCampaignReasons.IdentityConflict, engine.ExecuteDemoCampaign(command with { ExpectedRevision = revision + 1 }).Reason);
        Assert.Equal(80, engine.PlayerCredits);
    }
    [Fact]
    public void AbsentCrewStopsAccruingAndOnlyActualPeopleNeedFood()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        DemoEp0033Us0004Tk0003Tests.MakeRoom(engine);
        var before = engine.CaptureSnapshot();
        int count = before.CarrierOperations!.Crew.Length;
        Assert.Equal(count, before.DemoCampaign!.Company!.Upkeep!.PeopleFed);
        var save = engine.CaptureSaveState();
        engine.LoadScenario(save with { GameState = save.GameState with { SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId == save.GameState.PlayerShipObjectId
            ? s with { Passengers = [new ShipPassengerData("passenger", "Passenger")] } : s).ToArray() } }, isSave: true);
        Assert.Equal(count + 1, engine.CaptureSnapshot().DemoCampaign!.Company!.Upkeep!.PeopleFed);
        var after = engine.CaptureSaveStateForTests(Day, SimulationSpeed.Speed0, simulationTimeMs: 0);
        Assert.Equal(count * 20, after.GameState.DemoCampaign!.Company!.Upkeep!.Accrued);
        Assert.True(after.GameState.EconomyTime!.MissingRations >= 0);
    }
}
