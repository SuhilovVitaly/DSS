using DeepSpaceSaga.Client.Tests.Demo;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Engine.LocalClient;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.PostbattleResults;

public sealed class EP0042Us0001Tk0001Tests
{
    [Fact]
    public async Task TerminalOutcome_OpensResultsWithoutExiting()
    {
        using var engine = DemoEp0036Us0007Tk0004Tests.Won();
        await using var handle = new GameSessionHandle(new LocalGameSessionConnection(engine));
        await DemoEp0036Us0007Tk0004Tests.WaitFor(() => handle.DemoCampaign?.WorldEncounter is not null);
        var flow = new TacticalBattleFlow(handle)
        {
            CampaignEncounter = handle.DemoCampaign!.WorldEncounter,
            RequireBattleResults = true
        };
        Assert.True(await flow.StartEncounterAsync());

        var screen = new TacticalBattleScreen(flow, 1);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);

        Assert.False(screen.ResultsBounds.IsEmpty);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Escape, KeyModifiers.None, false));
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(400, 400, MouseButton.Left));
        Assert.Null(screen.LootWindow);
        Assert.Equal(ScreenEvent.OpenBattleResults, screen.OnKeyDown(Key.Enter, KeyModifiers.None, false));
        Assert.Equal(ScreenEvent.OpenBattleResults,
            screen.OnMouseDown(screen.ResultsBounds.MidX, screen.ResultsBounds.MidY, MouseButton.Left));
        Assert.NotNull(engine.GetTacticalBattleState());
    }
}
