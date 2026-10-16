using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Motion;
using SkiaSharp;
using static DeepSpaceSaga.Client.Tests.CombatReportScreenFlowTests;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatReportMapViewportTests
{
    [Theory]
    [InlineData(1280, 720, 1f)]
    [InlineData(1280, 720, 1.2f)]
    [InlineData(1280, 720, 1.5f)]
    [InlineData(1920, 1080, 1f)]
    [InlineData(1920, 1080, 1.2f)]
    [InlineData(1920, 1080, 1.5f)]
    public void FreeMapExcludesReportPanel(int width, int height, float scale)
    {
        var buffer = new SnapshotBuffer(); buffer.Update(World(1, 10));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale);
        using var surface = SKSurface.Create(new SKImageInfo(width, height)); Frame(screen, surface.Canvas, width, height);
        var panel = screen.CombatReportsBounds;
        var physical = new SKRect(panel.Left * scale, panel.Top * scale, panel.Right * scale, panel.Bottom * scale);
        Assert.False(screen.AvailableMapRect().IntersectsWith(physical));
        screen.DisposeCombatReports();
    }
}
