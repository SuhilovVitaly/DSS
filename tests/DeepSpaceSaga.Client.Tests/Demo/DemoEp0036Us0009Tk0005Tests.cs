using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Client.UI.Screens.Ship;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0009Tk0005Tests
{
    [Fact]
    public async Task ArrestClickUsesSharedPendingAndAuthoritativeReceipt()
    {
        using var engine = DemoEp0036Us0007Tk0004Tests.Won();
        var state = engine.CaptureSnapshot().DemoCampaign!;
        var release = new TaskCompletionSource();
        int calls = 0;
        var presentation = new BattleLootPresentation(async command => { calls++; await release.Task; return engine.ExecuteDemoCampaign(command); });
        presentation.Observe(state.Rewards!.Loot, state.Revision);
        var window = new BattleLootWindow(presentation);
        window.Select(state.Rewards.Loot!.Objects.First(s => s.Pilot is not null).ObjectId);
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        window.Draw(surface.Canvas, 1280, 720, false);
        var point = new SKPoint(window.ArrestBounds.MidX, window.ArrestBounds.MidY);
        window.Click(point, false); window.Click(point, false);
        Assert.True(presentation.Pending); Assert.False(presentation.Selected!.Pilot!.Captured); Assert.Equal(1, calls);
        release.SetResult(); Assert.True(await window.LastActionTask!);
        Assert.True(presentation.Selected!.Pilot!.Captured);
        window.Draw(surface.Canvas, 1280, 720, false); window.Click(point, false);
        Assert.Equal(1, calls);
        var ship = new ShipScreen(ShipViewModel.From(engine.CaptureSnapshot().CarrierOperations), 0, null,
            campaign: () => engine.CaptureSnapshot().DemoCampaign);
        Assert.Equal(presentation.Selected.Pilot.SourcePilotId, Assert.Single(ship.Prisoners).SourcePilotId);
    }
}
