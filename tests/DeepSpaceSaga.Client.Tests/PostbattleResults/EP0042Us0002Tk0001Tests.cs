using DeepSpaceSaga.Client.Tests.Demo;
using DeepSpaceSaga.Client.UI.Screens.Station;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Engine.LocalClient;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.PostbattleResults;

public sealed class EP0042Us0002Tk0001Tests
{
    [Theory]
    [InlineData(1920, 1080, 1)]
    [InlineData(1536, 720, 1.25f)]
    public async Task BattleResultsBounds_MatchStation(int width, int height, float uiScale)
    {
        using var engine = DemoEp0036Us0007Tk0004Tests.Won();
        await using var handle = new GameSessionHandle(new LocalGameSessionConnection(engine));
        var view = new BattleResultsScreen(new TacticalBattleFlow(handle), uiScale);
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);

        view.Render(canvas, width, height);

        Assert.Equal(StationLayout.PanelLeft(width), view.PanelBounds.Left);
        Assert.Equal(StationLayout.PanelTop(height), view.PanelBounds.Top);
        Assert.Equal(StationLayout.PanelWidth, view.PanelBounds.Width);
        Assert.Equal(StationLayout.PanelHeight, view.PanelBounds.Height);
        Assert.True(view.Contents.Bottom < view.ExitBounds.Top);
        Assert.True(new SKRect(0, 0, width, height).Contains(view.ExitBounds));
    }
}
