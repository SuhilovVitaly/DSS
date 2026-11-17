using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class Ep0035Us0017Tk0001Tests
{
    private sealed record UnsupportedCommand(string CommandId, long ExpectedRevision)
        : DemoCampaignCommand(CommandId, ExpectedRevision);

    [Fact]
    public void UnknownCommandAndStaleRepairDoNotMutateGameplay()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        string target = DemoEp0033Us0001Tk0003Tests.Damage(engine);
        var before = engine.CaptureSnapshot();
        var unknown = new UnsupportedCommand("unknown", before.DemoCampaign!.Revision);
        var rejection = engine.ExecuteDemoCampaign(unknown);
        Assert.False(rejection.Applied);
        Assert.Equal(DemoCampaignReasons.Unsupported, rejection.Reason);
        Assert.Equal(rejection, engine.ExecuteDemoCampaign(unknown));
        var stale = engine.ExecuteDemoCampaign(new RepairQuoteRequest("stale", -1, target));
        Assert.Equal(DemoCampaignReasons.StaleRevision, stale.Reason);
        var after = engine.CaptureSnapshot();
        Assert.Equal(before.PlayerCredits, after.PlayerCredits);
        Assert.Equal(before.DemoCampaign.Revision, after.DemoCampaign!.Revision);
        Assert.Equal(before.CarrierOperations!.OwnedDrones, after.CarrierOperations!.OwnedDrones);
    }

    [Fact]
    public void RepairCommitAndCalendarPayrollSurviveLoadWithoutReapplication()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        string target = DemoEp0033Us0001Tk0003Tests.Damage(engine, 5000);
        var quote = DemoEp0033Us0001Tk0003Tests.Quote(engine, target);
        var command = new RepairCommand("repair-once", quote.Revision, quote.Id, target);
        var accepted = engine.ExecuteDemoCampaign(command);
        Assert.True(accepted.Applied);
        Assert.Equal(4925, engine.PlayerCredits);
        Assert.Equal(accepted, engine.ExecuteDemoCampaign(command));
        var save = engine.CaptureSaveStateForTests(86400000, SimulationSpeed.Speed0, simulationTimeMs: 0);
        var ledger = save.GameState.DemoCampaign!.Company!.Upkeep!;
        Assert.Equal(engine.CaptureSnapshot().CarrierOperations!.Crew.Length * 20, ledger.Accrued);
        Assert.Equal(ledger.Accrued, ledger.Paid);
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true), isSave: true);
        long credits = engine.PlayerCredits;
        Assert.Equal(accepted, engine.ExecuteDemoCampaign(command));
        var repeated = engine.CaptureSaveStateForTests(86400000, SimulationSpeed.Speed0, simulationTimeMs: 0);
        Assert.Equal(credits, repeated.GameState.PlayerTokens);
        Assert.Equal(ledger.Accrued, repeated.GameState.DemoCampaign!.Company!.Upkeep!.Accrued);
    }
}
