using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0010Tk0004Tests
{
    [Fact]
    public async Task UnavailableThenChangedIntentRequiresExplicitOriginalRetry()
    {
        var commands = new List<DemoCampaignCommand>();
        var presentation = new BattleLootPresentation(command =>
        {
            commands.Add(command);
            return Task.FromResult(new DemoCampaignResponse(command.CommandId, commands.Count > 1, commands.Count == 1 ? DemoCampaignReasons.Unavailable : null, 0));
        });
        presentation.Observe(new("battle", LootPhases.Open, [], true) { Cargo = [new("A", 10, 1, 1), new("B", 10, 1, 1)] }, 0);
        var panel = new LootJettisonPanel(presentation);
        panel.Select(presentation.State!.Cargo[0]); panel.RequestConfirmation(); Assert.False(await panel.ConfirmAsync());
        panel.Select(presentation.State.Cargo[1]); panel.RequestConfirmation(); Assert.False(await panel.ConfirmAsync());
        Assert.Single(commands); Assert.Equal("A", Assert.IsType<JettisonCargoCommand>(presentation.RetryCommand).ItemTypeId);
        Assert.True(await presentation.RetryAsync());
        Assert.Equal(commands[0], commands[1]); Assert.Null(presentation.RetryCommand);
    }

    [Fact]
    public async Task ConfirmationAndPendingRequireAuthoritativeCargoUpdate()
    {
        using var engine = DemoEp0036Us0007Tk0004Tests.Won();
        var state = engine.CaptureSnapshot().DemoCampaign!;
        var release = new TaskCompletionSource(); int calls = 0;
        var presentation = new BattleLootPresentation(async command => { calls++; await release.Task; return engine.ExecuteDemoCampaign(command); });
        presentation.Observe(state.Rewards!.Loot, state.Revision);
        var panel = new LootJettisonPanel(presentation);
        var item = state.Rewards.Loot!.Cargo.First(c => c.Quantity >= c.QuantityStep * 2);
        panel.Select(item); panel.Change(1);
        Assert.Equal(item.QuantityStep * 2, panel.Quantity);
        Assert.False(await panel.ConfirmAsync()); Assert.Equal(0, calls);
        panel.RequestConfirmation(); var pending = panel.ConfirmAsync();
        Assert.True(presentation.Pending);
        Assert.False(await panel.ConfirmAsync()); Assert.Equal(1, calls);
        Assert.Equal(item.Quantity, presentation.State!.Cargo.Single(c => c.ItemTypeId == item.ItemTypeId).Quantity);
        release.SetResult(); Assert.True(await pending);
        Assert.Equal(item.Quantity - item.QuantityStep * 2, presentation.State!.Cargo.FirstOrDefault(c => c.ItemTypeId == item.ItemTypeId)?.Quantity ?? 0);
    }
}
