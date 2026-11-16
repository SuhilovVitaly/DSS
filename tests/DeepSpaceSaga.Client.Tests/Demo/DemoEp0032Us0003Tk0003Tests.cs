using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.ShipDestroyed;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0032Us0003Tk0003Tests
{
    [Fact]
    public void EmptyWingOffersRecoveryAndTerminalKeepsLoadAndMenu()
    {
        using var locale = Localization.UseLocaleForTests("English");
        Assert.Contains("trade", CampaignOutcomePresentation.Guidance(new(CampaignOutcomeKind.Active, "battle", true, "no_ready_wing")));
        Assert.Contains("destroyed", CampaignOutcomePresentation.Guidance(new(CampaignOutcomeKind.CarrierDestroyed, "battle", false, "carrier_destroyed")));
        var screen = new ShipDestroyedScreen();
        screen.OnActivated();
        Assert.Equal(ScreenEvent.OpenLoadWindow, screen.OnKeyDown(Key.Enter));
        screen.OnKeyDown(Key.Tab);
        Assert.Equal(ScreenEvent.MainMenu, screen.OnKeyDown(Key.Enter));
        screen.OnDeactivated();
        screen.OnActivated();
        Assert.Equal(ScreenEvent.OpenLoadWindow, screen.OnKeyDown(Key.Enter));
    }
}
