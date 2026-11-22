using DeepSpaceSaga.Client.Tests.Demo;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Engine.LocalClient;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.PostbattleResults;

public sealed class EP0042Us0002Tk0002Tests
{
    [Fact]
    public async Task BattleResultsAccordion_IndependentExpansionClippedInputs()
    {
        using var engine = DemoEp0036Us0007Tk0004Tests.Won();
        await using var handle = new GameSessionHandle(new LocalGameSessionConnection(engine));
        await DemoEp0036Us0007Tk0004Tests.WaitFor(() => handle.DemoCampaign?.WorldEncounter is not null);
        var flow = new TacticalBattleFlow(handle) { CampaignEncounter = handle.DemoCampaign!.WorldEncounter };
        Assert.True(await flow.StartEncounterAsync());
        Assert.True(await flow.OpenResultsAsync());
        var view = new BattleResultsScreen(flow, 1);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        void Render() => view.Render(canvas, 1280, 720);
        Render();
        var first = view.SourceHits[0];
        var second = view.SourceHits[1];
        view.OnMouseDown(first.Bounds.MidX, first.Bounds.MidY, MouseButton.Left);
        Render();
        var moved = view.SourceHits[1];
        Assert.True(moved.Bounds.Top > second.Bounds.Top);
        view.OnMouseDown(moved.Bounds.MidX, moved.Bounds.MidY, MouseButton.Left);
        Assert.True(view.IsExpanded(first.Id));
        Assert.True(view.IsExpanded(second.Id));

        var footer = view.ExitBounds;
        view.OnMouseWheel(view.Contents.MidX, view.Contents.MidY, 10000);
        Render();
        Assert.Equal(footer, view.ExitBounds);
        Assert.All(view.SourceHits, hit => Assert.True(view.Contents.Contains(hit.Bounds)));
        view.OnMouseDown(view.Contents.Left + 15, view.Contents.Top - 5, MouseButton.Left);
        Assert.True(view.IsExpanded(first.Id));
        Assert.True(view.IsExpanded(second.Id));
    }
}
