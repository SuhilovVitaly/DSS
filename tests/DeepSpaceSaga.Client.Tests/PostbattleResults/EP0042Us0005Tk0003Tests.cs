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
        Assert.Equal(1, before.FreePrisonerCabins);
        var available = view.SourceHits.Take(2).Select(hit => hit.Id).ToArray();
        Assert.Equal(2, available.Length);
        foreach (string id in available)
        {
            var card = view.SourceHits.Single(hit => hit.Id == id);
            view.OnMouseDown(card.Bounds.MidX, card.Bounds.MidY, MouseButton.Left);
            Render();
        }
        var actions = view.LootActionHits.Where(hit => hit.Row.Kind == BattleResultsScreen.RowKind.Pilot).ToArray();
        Assert.Equal(2, actions.Length);
        var arrest = actions[0];
        view.OnMouseDown(arrest.Bounds.MidX, arrest.Bounds.MidY, MouseButton.Left);
        Assert.True(await view.LastLootAction!);
        Assert.Equal(before with { FreePrisonerCabins = 0 }, view.Capacity);

        // The previous frame still contains the other pilot's clickable area.
        var previousAction = view.LastLootAction;
        var other = actions[1];
        view.OnMouseDown(other.Bounds.MidX, other.Bounds.MidY, MouseButton.Left);
        Assert.Same(previousAction, view.LastLootAction);
        Render();
        Assert.DoesNotContain(view.LootActionHits, hit => hit.Row.Kind == BattleResultsScreen.RowKind.Pilot);

        var campaign = engine.CaptureSnapshot().DemoCampaign!;
        var loot = campaign.Rewards!.Loot!;
        var rejected = engine.ExecuteDemoCampaign(new ArrestPilotCommand("full", campaign.Revision, loot.BattleId, other.ObjectId));
        Assert.Equal(PrisonerCustodyReasons.NoCabin, rejected.Reason);
        Assert.Single(engine.CaptureSaveState().GameState.DemoCampaign!.Prisoners);

        Assert.Equal(BattleResultsScreen.LootStep.Pilots, view.CurrentStep);
        Assert.Equal(ScreenEvent.None, view.OnKeyDown(Key.Enter));
        Assert.Equal(BattleResultsScreen.LootStep.Components, view.CurrentStep);
        Assert.Equal(ScreenEvent.None, view.OnKeyDown(Key.Enter));
        Assert.Equal(BattleResultsScreen.LootStep.Scrap, view.CurrentStep);
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
