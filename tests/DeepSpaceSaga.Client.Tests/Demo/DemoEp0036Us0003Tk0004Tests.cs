using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0003Tk0004Tests
{
    private static LootSessionSnapshot Loot(string battle = "battle") => new(battle, LootPhases.Open,
        [new("a", "DisabledDrone", [new("ammo", 3, true, null)], null, [], null),
         new("b", "DisabledDrone", [new("decoy", 2, true, null)], null, [], null)], true);

    [Fact]
    public void SelectedSourceShowsOnlyItsItems()
    {
        var model = new BattleLootPresentation(_ => throw new InvalidOperationException());
        model.Observe(Loot(), 1); Assert.True(model.Select("a"));
        var panel = new BattleLootConsumablesPanel(model);
        Assert.Equal("ammo", Assert.Single(panel.Rows(id => id)).Id);
        Assert.True(model.Select("b"));
        Assert.Equal("decoy", Assert.Single(panel.Rows(id => id)).Id);
        model.Observe(Loot("other"), 1);
        Assert.Null(model.Selected);
    }

    [Fact]
    public async Task PendingBlocksDuplicate_AndRejectedCollectPreservesRemaining()
    {
        var pending = new TaskCompletionSource<DemoCampaignResponse>();
        int calls = 0;
        var model = new BattleLootPresentation(_ => { calls++; return pending.Task; });
        model.Observe(Loot(), 1); model.Select("a");
        var first = model.CollectAsync("ammo", 3);
        Assert.True(model.Pending);
        Assert.False(await model.CollectAsync("ammo", 3));
        Assert.False(Assert.Single(new BattleLootConsumablesPanel(model).Rows(id => id)).Enabled);
        pending.SetResult(new("id", false, BattleLootReasons.CargoFull, 1));
        Assert.False(await first);
        Assert.Equal(1, calls);
        Assert.Equal(3, Assert.Single(model.Selected!.Consumables).Remaining);
    }

    [Fact]
    public async Task ClosedBattleClearsPanel()
    {
        var model = new BattleLootPresentation(_ => Task.FromResult(new DemoCampaignResponse("id", true, null, 2,
            Rewards: new DemoRewardsSnapshot { Loot = Loot() with { Phase = LootPhases.Closed } })));
        model.Observe(Loot(), 1); model.Select("a");
        Assert.True(await model.CollectAsync("ammo", 3));
        Assert.Null(model.Selected); Assert.Null(model.State);
        Assert.Empty(new BattleLootConsumablesPanel(model).Rows(id => id));
    }
}
