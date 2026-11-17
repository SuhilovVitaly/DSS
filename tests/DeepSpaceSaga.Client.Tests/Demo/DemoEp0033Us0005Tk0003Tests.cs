using DeepSpaceSaga.Client.UI.Screens.Finance;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0033Us0005Tk0003Tests
{
    [Fact]
    public async Task DebtPaymentUsesSelectedAmountAndPendingBarrier()
    {
        var wait = new TaskCompletionSource<DemoCampaignResponse>();
        var calls = new List<DemoCampaignCommand>();
        var screen = new FinanceScreen(service: (c, _) => { calls.Add(c); return new(wait.Task); });
        screen.PayDebt(20); screen.PayDebt(20);
        Assert.Equal(20, Assert.IsType<PayCrewDebtCommand>(Assert.Single(calls)).Amount);
        wait.SetResult(new("p", true, null, 1, Upkeep: new(100, 20, 80, 86400000, 6, 10, 0, 0)));
        await screen.PendingDebt!;
        Assert.Equal(80, screen.Upkeep!.Outstanding);
        screen.OnActivated(); Assert.Single(calls);
    }
    [Fact]
    public async Task RejectedPaymentDoesNotInventTotals()
    {
        var screen = new FinanceScreen(service: (c, _) => ValueTask.FromResult(new DemoCampaignResponse(c.CommandId, false, "service_insufficient_funds", 0)));
        screen.PayDebt(20); await screen.PendingDebt!;
        Assert.Null(screen.Upkeep);
        Assert.DoesNotContain("service_insufficient_funds", screen.DebtStatus);
    }
}
