using DeepSpaceSaga.Client.UI.Screens.Ship;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0033Us0001Tk0004Tests
{
    [Fact]
    public async Task ConfirmedSnapshot_RendersOutcome()
    {
        var wait = new TaskCompletionSource<DemoCampaignResponse>();
        int calls = 0;
        var screen = new ShipScreen(ShipViewModel.From(null), 0, null,
            service: (_, _) => { calls++; return new(wait.Task); });
        screen.RequestRepair("carrier");
        screen.RequestRepair("carrier");
        Assert.Equal(1, calls);
        wait.SetResult(new("q", true, null, 1, new(new("q", "s", "carrier", 1, 100, 5,
            [new("hull", 1, 5, 5)], new(9, 0, 0, false)))));
        await screen.PendingService!;
        Assert.NotEmpty(screen.ServiceStatus);
    }
    [Fact]
    public async Task RejectedCommand_ShowsReason()
    {
        var screen = new ShipScreen(ShipViewModel.From(null), 0, null,
            service: (_, _) => ValueTask.FromResult(new DemoCampaignResponse("q", false, "service_not_docked", 0)));
        screen.RequestRepair("carrier");
        await screen.PendingService!;
        Assert.DoesNotContain("service_not_docked", screen.ServiceStatus);
        screen.ConfirmRepair();
    }
    [Fact]
    public void Reopen_DoesNotResubmit()
    {
        int calls = 0;
        var screen = new ShipScreen(ShipViewModel.From(null), 0, null,
            service: (_, _) => { calls++; return ValueTask.FromResult(new DemoCampaignResponse("q", false, "unavailable", 0)); });
        screen.OnActivated(); screen.OnDeactivated(); screen.OnActivated();
        Assert.Equal(0, calls);
    }
}
