using DeepSpaceSaga.Client.Tests.Demo;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.PostbattleResults;

public sealed class EP0042Us0005Tk0003Tests
{
    [Fact]
    public async Task BattleResultsFooter_CabinsRefreshAndNoJettison()
    {
        using var engine = DemoEp0036Us0007Tk0004Tests.Won();
        await using var handle = new GameSessionHandle(new LocalGameSessionConnection(engine));
        await DemoEp0036Us0007Tk0004Tests.WaitFor(() => handle.DemoCampaign?.Rewards?.Loot is not null);
        var flow = new TacticalBattleFlow(handle) { CampaignEncounter = handle.DemoCampaign!.WorldEncounter };
        Assert.True(await flow.StartEncounterAsync());
        Assert.True(await flow.OpenResultsAsync());
        var view = new BattleResultsScreen(flow, 1);
        using var bitmap = new SKBitmap(1600, 900);
        using var canvas = new SKCanvas(bitmap);
        void Render() => view.Render(canvas, bitmap.Width, bitmap.Height);
        Render();
        var before = view.Capacity!;
        var card = view.SourceHits.First(hit => handle.DemoCampaign!.Rewards!.Loot!.Objects.First(source => source.ObjectId == hit.Id)
            .Pilot is { Alive: true, CaptureReason: null });
        view.OnMouseDown(card.Bounds.MidX, card.Bounds.MidY, MouseButton.Left);
        Render();
        var arrest = view.LootActionHits.First(hit => hit.Row.Kind == BattleResultsScreen.RowKind.Pilot);
        view.OnMouseDown(arrest.Bounds.MidX, arrest.Bounds.MidY, MouseButton.Left);
        Assert.True(await view.LastLootAction!);
        Assert.Equal(before with { FreePrisonerCabins = before.FreePrisonerCabins - 1 }, view.Capacity);

        Assert.Equal(ScreenEvent.None, view.OnKeyDown(Key.Enter));
        Render();
        Assert.True(view.ConfirmDiscard);
        Assert.NotEmpty(view.LeavingRows());
        Assert.DoesNotContain(arrest.Row.Name, view.LeavingRows());
        Assert.Equal(ScreenEvent.None, view.OnKeyDown(Key.Enter));
        Assert.Equal(ScreenEvent.None, view.OnMouseDown(view.StayBounds.MidX, view.StayBounds.MidY, MouseButton.Left));
        Assert.False(view.ConfirmDiscard);
        Assert.Equal(LootPhases.Open, engine.CaptureSnapshot().DemoCampaign!.Rewards!.Loot!.Phase);
        Assert.Equal(ScreenEvent.None, view.OnKeyDown(Key.Enter));
        Assert.Equal(ScreenEvent.AcknowledgeBattleResults, view.OnMouseDown(view.ExitBounds.MidX, view.ExitBounds.MidY, MouseButton.Left));
        Assert.True(view.ConsumeAcknowledgeRequest());
        Assert.True(await flow.AcknowledgeResultsAsync(view.ConfirmDiscard));
        Assert.Equal(LootPhases.Closed, engine.CaptureSnapshot().DemoCampaign!.Rewards!.Loot!.Phase);
    }
}
