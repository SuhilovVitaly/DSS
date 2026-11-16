using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Client.UI.Screens.Ship;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0032Us0001Tk0003Tests
{
    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public void ConfirmedSnapshot_RendersOutcome(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        var state = new CampaignRoundtripSnapshot(new("battle", "ship", 1, false, ["lost"], 7, SortieExitKind.Aborted));
        var snapshot = new DemoCampaignSnapshot(1, 1, new(state));
        var screen = new ShipScreen(ShipViewModel.From(null), 0, null, campaign: () => snapshot);
        Assert.Contains("7", screen.CampaignSummary);
        Assert.Contains("1", screen.CampaignSummary);
        Assert.DoesNotContain("Demo.", screen.CampaignSummary);
        screen.OnActivated();
        screen.OnDeactivated();
        screen.OnActivated();
        Assert.Equal(CampaignRoundtripPresentation.Summary(state), screen.CampaignSummary);
    }

    [Fact]
    public void RejectedCommand_ShowsReason()
    {
        using var locale = Localization.UseLocaleForTests("English");
        Assert.Contains("changed", CampaignRoundtripPresentation.RearmReason("stale_revision"));
        Assert.DoesNotContain("unknown_code", CampaignRoundtripPresentation.RearmReason("unknown_code"));
    }
}
