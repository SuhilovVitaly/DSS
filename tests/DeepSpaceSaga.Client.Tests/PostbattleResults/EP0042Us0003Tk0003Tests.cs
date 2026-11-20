using DeepSpaceSaga.Client.Tests.Demo;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.PostbattleResults;

public sealed class EP0042Us0003Tk0003Tests
{
    [Fact]
    public async Task LootOperationRow_ProgressBottomUp_ManualAndCollapse()
    {
        using var engine = DemoEp0036Us0007Tk0004Tests.Won();
        long clock = 1000;
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
            using var bitmap = new SKBitmap(1280, 800);
            using var canvas = new SKCanvas(bitmap);
            view.Render(canvas, bitmap.Width, bitmap.Height);
            var card = view.SourceHits.First(hit => handle.DemoCampaign!.Rewards!.Loot!.Objects
                .Any(source => source.ObjectId == hit.Id && source.Modules.Any(module => module.CanAttempt)));
            view.OnMouseDown(card.Bounds.MidX, card.Bounds.MidY, MouseButton.Left);
            view.Render(canvas, bitmap.Width, bitmap.Height);
            var target = view.LootActionHits.First(hit => hit.Row.Kind == BattleResultsScreen.RowKind.Module);
            view.OnMouseDown(target.Bounds.MidX, target.Bounds.MidY, MouseButton.Left);
            Assert.True(await view.LastLootAction!);

            var operation = Assert.Single(engine.CaptureSnapshot().DemoCampaign!.Rewards!.Loot!.Operations);
            Assert.Equal(new LootRowKey(target.ObjectId, LootRowKind.Module, target.Row.Id), operation.Row);
            Assert.Equal(5000, operation.RemainingMs);
            Assert.Equal(new SKRect(8, 85, 18, 110), BattleResultsScreen.ProgressFill(new(8, 10, 18, 110), .25f));
            Assert.Equal(ScreenEvent.None, view.OnKeyDown(Key.Enter));
            view.OnMouseDown(card.Bounds.MidX, card.Bounds.MidY, MouseButton.Left);
            Assert.False(view.IsExpanded(card.Id));

            Interlocked.Add(ref clock, 5000);
            await DemoEp0036Us0007Tk0004Tests.WaitFor(() =>
                engine.CaptureSnapshot().DemoCampaign!.Rewards!.Loot!.Operations.IsEmpty);
            var source = engine.CaptureSnapshot().DemoCampaign!.Rewards!.Loot!.Objects.Single(entry => entry.ObjectId == target.ObjectId);
            Assert.Contains(source.Modules.Single(module => module.InstanceId == target.Row.Id).State,
                new[] { LootAttemptStates.Collected, LootAttemptStates.Failed });
        }
        finally { view.OnDeactivated(); }
    }
}
