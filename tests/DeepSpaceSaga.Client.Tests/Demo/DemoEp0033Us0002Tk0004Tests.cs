using DeepSpaceSaga.Client.UI.Screens.Ship;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0033Us0002Tk0004Tests
{
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
        Assert.Single(new[] { calls }.Where(n => n == 1));
        Assert.DoesNotContain("service_not_docked", screen.SupplyStatus);
    }
}
