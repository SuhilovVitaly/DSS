using DeepSpaceSaga.Client.UI.Screens.Ship;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0033Us0002Tk0004Tests
{
    [Fact]
    public async Task ResupplyResponseDoesNotHideLaterAmmunitionSnapshot()
    {
        var supply = new SupplySnapshot(10, [new("d", 0, 6, 1, 2)], 10, 25);
        var campaign = new DemoCampaignSnapshot(1, 1, new(Supply: supply));
        var carrier = new CarrierOperationsSnapshot("ship", "class", "Ship", 1, 10, 10, false,
            OwnedDrones: [new("d", "ship", "bay", 0, "pilot", "light", "gun", "armor", AmmoCapacity: 6)]);
        var screen = new ShipScreen(ShipViewModel.From(carrier), 10, null, campaign: () => campaign,
            service: (c, _) => ValueTask.FromResult(new DemoCampaignResponse(c.CommandId, true, null, 1,
                Supply: supply with { Quote = c is DecoyQuoteRequest ? new("q", "s", "d", 1, 1, 100, 1, 0, 25) : null })));
        screen.RequestDecoys("d"); await screen.PendingService!;
        screen.ConfirmDecoys(); await screen.PendingService!;
        var before = RenderSupply(screen);
        campaign = campaign with { Revision = 2, Company = new(Supply: new(4, [new("d", 6, 6, 1, 2)], 10, 25)) };
        var after = RenderSupply(screen);
        Assert.False(before.SequenceEqual(after), "Supply must redraw after a later authoritative ammunition change.");
    }
    private static byte[] RenderSupply(ShipScreen screen)
    {
        using var bitmap = new SkiaSharp.SKBitmap(1920, 1080);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        return bitmap.Bytes;
    }
    [Fact]
    public async Task PendingBarrierAndExplicitConfirmation()
    {
        var wait = new TaskCompletionSource<DemoCampaignResponse>();
        var calls = new List<DemoCampaignCommand>();
        var screen = new ShipScreen(ShipViewModel.From(null), 0, null,
            service: (c, _) => { calls.Add(c); return calls.Count == 1 ? new(wait.Task) : ValueTask.FromResult(new DemoCampaignResponse(c.CommandId, true, null, 2, Supply: new(4, [], 4))); });
        screen.RequestDecoys("d"); screen.RequestDecoys("d"); screen.ConfirmDecoys();
        Assert.Single(calls);
        wait.SetResult(new("q", true, null, 1, Supply: new(4, [], 4, 25, new("q", "s", "d", 1, 0, 100, 1, 0, 25))));
        await screen.PendingService!;
        screen.ConfirmDecoys(); await screen.PendingService!;
        Assert.IsType<ResupplyDecoyCommand>(calls[1]);
        Assert.NotEmpty(screen.SupplyStatus);
        screen.OnActivated(); screen.ConfirmDecoys();
        Assert.Equal(2, calls.Count);
    }
    [Fact]
    public async Task RejectionClearsQuoteAndLocalizesReason()
    {
        int calls = 0;
        var screen = new ShipScreen(ShipViewModel.From(null), 0, null, service: (c, _) =>
        { calls++; return ValueTask.FromResult(new DemoCampaignResponse(c.CommandId, false, "service_not_docked", 0)); });
        screen.RequestDecoys("d"); await screen.PendingService!;
        screen.ConfirmDecoys();
        Assert.Equal(1, calls);
        Assert.DoesNotContain("service_not_docked", screen.SupplyStatus);
    }
}

