using DeepSpaceSaga.Client.Tests.Demo;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

namespace DeepSpaceSaga.Client.Tests.PostbattleResults;

public sealed class EP0042Us0002Tk0003Tests
{
    [Fact]
    public void BattleResultsRows_ModuleChanceArrestAndHiddenDestroyed()
    {
        using var engine = DemoEp0036Us0007Tk0004Tests.Won();
        var saved = engine.CaptureSaveState().GameState.DemoCampaign!.LootSession!;
        var loot = engine.CaptureSnapshot().DemoCampaign!.Rewards!.Loot!;
        var source = loot.Objects.First(entry => entry.Modules.Length > 0 && entry.Pilot is { Alive: true });
        var module = source.Modules[0];
        var rows = BattleResultsScreen.LootRows(source);
        var row = Assert.Single(rows, r => r.Id == module.InstanceId);
        Assert.Equal(BattleResultsScreen.RowKind.Module, row.Kind);
        Assert.Equal(module.ChanceBasisPoints, row.Chance);
        Assert.Equal(module.EstimatedValueTokens, row.Value);
        Assert.Equal(module.RequiredMassKg, row.Mass);
        Assert.Contains(rows, r => r.Kind == BattleResultsScreen.RowKind.Pilot && r.Name == source.Pilot!.DisplayName);

        foreach (var frozen in saved.Sources)
        {
            var projected = loot.Objects.Single(entry => entry.ObjectId == frozen.ObjectId);
            Assert.DoesNotContain(BattleResultsScreen.LootRows(projected), r =>
                frozen.Modules.Any(item => item.Destroyed && item.InstanceId == r.Id));
        }
    }
}
