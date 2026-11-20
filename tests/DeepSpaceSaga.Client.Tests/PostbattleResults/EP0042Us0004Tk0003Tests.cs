using DeepSpaceSaga.Client.Tests.Demo;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Engine.LocalClient;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.PostbattleResults;

public sealed class EP0042Us0004Tk0003Tests
{
    [Fact]
    public async Task BulkLoot_AllCardsExpand_ArrestRemainsManual()
    {
        using var engine = DemoEp0036Us0007Tk0004Tests.Won();
        await using var handle = new GameSessionHandle(new LocalGameSessionConnection(engine));
        await DemoEp0036Us0007Tk0004Tests.WaitFor(() => handle.DemoCampaign?.Rewards?.Loot is not null);
        var flow = new TacticalBattleFlow(handle) { CampaignEncounter = handle.DemoCampaign!.WorldEncounter };
        Assert.True(await flow.StartEncounterAsync());
        Assert.True(await flow.OpenResultsAsync());
        var view = new BattleResultsScreen(flow, 1);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        view.Render(canvas, bitmap.Width, bitmap.Height);

        var before = handle.DemoCampaign!.Rewards!.Loot!;
        Assert.All(before.Objects, source => Assert.False(view.IsExpanded(source.ObjectId)));
        view.OnMouseDown(view.CollectAllBounds.MidX, view.CollectAllBounds.MidY, MouseButton.Left);
        Assert.True(await view.LastLootAction!);

        var state = engine.CaptureSnapshot().DemoCampaign!.Rewards!.Loot!;
        var sourceIds = state.Operations.Select(operation => operation.Row.ObjectId).Distinct().ToArray();
        Assert.True(sourceIds.Length > 1);
        Assert.All(sourceIds, id => Assert.True(view.IsExpanded(id)));
        Assert.DoesNotContain(state.Objects, source => source.Pilot?.Captured == true);
    }
}
