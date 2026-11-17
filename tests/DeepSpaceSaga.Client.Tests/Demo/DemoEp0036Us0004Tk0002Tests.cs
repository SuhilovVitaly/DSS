using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0004Tk0002Tests
{
    [Fact]
    public void ReopenAndLoadReportHasNoCommandsOrMutation()
    {
        using var engine = DemoEp0036Us0007Tk0004Tests.Won();
        var before = engine.CaptureSaveState();
        var report = RewardReportPresentation.Lines(engine.CaptureSnapshot().DemoCampaign);
        Assert.Equal(report, RewardReportPresentation.Lines(engine.CaptureSnapshot().DemoCampaign));
        engine.LoadScenario(DeepSpaceSaga.Engine.Scenario.ScenarioLoader.LoadFromJson(
            DeepSpaceSaga.Engine.Scenario.ScenarioLoader.Serialize(before), true, true), true);
        Assert.Equal(report, RewardReportPresentation.Lines(engine.CaptureSnapshot().DemoCampaign));
        Assert.Equal(before.GameState.DemoCampaign!.Revision, engine.CaptureSnapshot().DemoCampaign!.Revision);
        Assert.Equal(before.GameState.PlayerTokens, engine.PlayerCredits);
    }

    [Fact]
    public void ReportSeparatesReceiptsEstimatesLossesAndUnknownProfit()
    {
        var loot = new LootSessionSnapshot("battle", LootPhases.Closed,
            [new("drone", "DisabledDrone", [new("item.torpedo", 2, false, null) { OriginalQuantity = 5 }], null,
                [new("m", "engine", "e", "item.loot.engine", "Engine", "Intact", 100, 2500, LootAttemptStates.Failed, false, null)],
                new("pilot", "Jane", null, true, null, true))], false);
        var campaign = new DemoCampaignSnapshot(1, 1,
            Company: new(Roundtrip: new(new("battle", "ship", 1, false, ["lost-drone"], 7, SortieExitKind.Completed))),
            Rewards: new() { Loot = loot, BountyPayment = new([new("order", "station", "group", "battle", BountyPaymentStates.Claimable, 1500, 0, null, null, null)]) });
        string text = string.Join('\n', RewardReportPresentation.Lines(campaign));
        Assert.Contains("1500", text);
        Assert.Contains("lost-drone", text);
        Assert.Contains("Jane", text);
        Assert.Contains("100", text);
        // Projection remains unchanged; due payment never becomes cash or a computed profit.
        Assert.Equal(0, campaign.Rewards!.BountyPayment!.Entries[0].PaidTokens);
        Assert.Equal(LootAttemptStates.Failed, loot.Objects[0].Modules[0].State);
        Assert.Contains(Localization.Get("reward_report.notice"), text);
    }
}
