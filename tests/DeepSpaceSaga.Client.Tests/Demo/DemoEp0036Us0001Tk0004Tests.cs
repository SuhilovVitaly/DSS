using System.Text.Json;
using System.Text.RegularExpressions;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0001Tk0004Tests
{
    [Fact]
    public void RealBountyContent_LoadsAndReferencesExistingGroups()
    {
        var definitions = DemoCampaignDefinitions.Load(AppContext.BaseDirectory);
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(
            Path.Combine(AppContext.BaseDirectory, "Settings.json"),
            Path.Combine(AppContext.BaseDirectory, "Scenarios/Demo/scenario.json"));
        var groups = engine.CaptureSaveState().GameState.DemoCampaign!.PirateWorld!.Groups;
        Assert.Equal(2, definitions.Jobs!.Bounties.Length);
        var campaign = engine.CaptureSnapshot().DemoCampaign!;
        Assert.True(engine.ExecuteDemoCampaign(new DeepSpaceSaga.Contracts.AcceptBountyCommand("track-area",
            campaign.Revision, "bounty:pirate:01")).Applied);
        var screen = new DeepSpaceSaga.Client.UI.Screens.Contracts.ContractsScreen(
            campaign: () => engine.CaptureSnapshot().DemoCampaign);
        var region = Assert.Single(screen.BountyRows).Order.SearchRegion;
        Assert.Equal("demo_pirate_region", region);
        Assert.Contains("bounty." + region, BountyText("English").Keys);
        Assert.Contains("bounty." + region, BountyText("Russian").Keys);
        foreach (var bounty in definitions.Jobs.Bounties)
        {
            Assert.Contains(groups, g => g.GroupId == bounty.TargetGroupId);
            Assert.Contains(bounty.IssuerStationId, definitions.Cluster!.StationIds);
            Assert.True(bounty.RewardTokens > 0);
        }
    }

    private static Dictionary<string, string> BountyText(string locale) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Data/Locale", locale + ".json")))!
            .Where(p => p.Key.StartsWith("bounty.", StringComparison.Ordinal)).ToDictionary(p => p.Key, p => p.Value);

    [Fact]
    public void BountyLocaleKeys_ParityAndPlaceholders()
    {
        var en = BountyText("English");
        var ru = BountyText("Russian");
        Assert.NotEmpty(en);
        Assert.Equal(en.Keys.Order(), ru.Keys.Order());
        foreach (var (key, value) in en)
            Assert.Equal(Regex.Matches(value, @"\{\d+\}").Select(m => m.Value).Order(),
                Regex.Matches(ru[key], @"\{\d+\}").Select(m => m.Value).Order());
    }

    [Fact]
    public void CancellationText_DoesNotClaimRewardForfeiture()
    {
        Assert.Contains("preserves", BountyText("English")["bounty.cancellation_free"]);
        Assert.Contains("сохраняет", BountyText("Russian")["bounty.cancellation_free"]);
    }
}
