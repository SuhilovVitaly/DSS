using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Motion;
using SkiaSharp;
using static DeepSpaceSaga.Client.Tests.CombatReportScreenFlowTests;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatReportHoverTests
{
    [Fact]
    public void ReportHoverClearsCommandsToggleHover()
    {
        var buffer = new SnapshotBuffer(); buffer.Update(World(1, 10));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720)); Frame(screen, surface.Canvas);
        var toggle = screen.CommandsPanel.HideShowButtonRect;
        screen.OnMouseMove(toggle.MidX, toggle.MidY); Assert.Equal(0, screen.CommandsPanel.HoveredButtonIndex);
        var reports = screen.CombatReportsBounds;
        Assert.True(screen.OnMouseMove(reports.MidX, reports.Top + 10));
        Assert.Equal(-1, screen.CommandsPanel.HoveredButtonIndex);
        screen.DisposeCombatReports();
    }
}
