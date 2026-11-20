using DeepSpaceSaga.Client.Tests.Demo;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.PostbattleResults;

public sealed class EP0042Us0004Tk0004Tests
{
    [Fact]
    public async Task ManualLoot_CollectOneLeavesRemainingStack()
    {
        using var engine = DemoEp0036Us0007Tk0004Tests.Won();
        long clock = 100;
        engine.LootClockMilliseconds = () => Interlocked.Read(ref clock);
        await using var handle = new GameSessionHandle(new LocalGameSessionConnection(engine));
        await DemoEp0036Us0007Tk0004Tests.WaitFor(() => handle.DemoCampaign?.Rewards?.Loot is not null);
        var flow = new TacticalBattleFlow(handle) { CampaignEncounter = handle.DemoCampaign!.WorldEncounter };
        Assert.True(await flow.StartEncounterAsync());
        Assert.True(await flow.OpenResultsAsync());
        var view = new BattleResultsScreen(flow, 1);
        view.OnActivated();
        try
        {
            using var bitmap = new SKBitmap(1600, 900);
            using var canvas = new SKCanvas(bitmap);
            view.Render(canvas, bitmap.Width, bitmap.Height);
            var before = engine.CaptureSnapshot().DemoCampaign!.Rewards!.Loot!;
            var card = view.SourceHits.First(hit => before.Objects.Any(source => source.ObjectId == hit.Id &&
                source.Consumables.Any(item => item.Remaining > 1 && item.CanCollect)));
            view.OnMouseDown(card.Bounds.MidX, card.Bounds.MidY, MouseButton.Left);
            view.Render(canvas, bitmap.Width, bitmap.Height);
            var action = view.LootActionHits.First(hit => hit.Quantity == 1);
            Assert.Contains(view.LootActionHits, hit => hit.ObjectId == action.ObjectId && hit.Row.Id == action.Row.Id && hit.Quantity is null);
            long remaining = action.Row.Quantity!.Value;
            view.OnMouseDown(action.Bounds.MidX, action.Bounds.MidY, MouseButton.Left);
            Assert.True(await view.LastLootAction!);
            Interlocked.Add(ref clock, 3000);
            await DemoEp0036Us0007Tk0004Tests.WaitFor(() => !engine.HasActiveLootOperations);

            var source = engine.CaptureSnapshot().DemoCampaign!.Rewards!.Loot!.Objects.Single(entry => entry.ObjectId == action.ObjectId);
            Assert.Equal(remaining - 1, source.Consumables.Single(item => item.ItemTypeId == action.Row.Id).Remaining);
        }
        finally { view.OnDeactivated(); }
    }
}
