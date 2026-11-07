using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
namespace DeepSpaceSaga.Client.Tests;

public class PhaseThreeCombatInfoTests
{
    [Theory]
    [InlineData(200)]
    [InlineData(480)]
    public void Long_details_wrap_and_remain_scrollable(int height)
    {
        string detail = string.Join(" ", Enumerable.Repeat("available scanning and propulsion controls", 24));
        var panel = new CommandsPanel(groupDetails: name => name == "Engine" ? [detail] : []);
        using var bitmap = new SKBitmap(400, height);
        using var canvas = new SKCanvas(bitmap);
        var modules = Array.Empty<InstalledModuleSnapshot>();
        panel.Render(canvas, modules, height, "player");
        var row = panel.CommandPanelRows.Single(r => r.Name == "Engine");
        panel.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY);
        panel.Render(canvas, modules, height, "player");
        row = panel.CommandPanelRows.Single(r => r.Name == "Engine");
        Assert.True(row.Opened);
        Assert.True(row.DetailLines.Length > 2);
        using var paint = new SKPaint { TextSize = 12, IsAntialias = true };
        Assert.All(row.DetailLines, l => Assert.True(paint.MeasureText(l) <= CommandsPanel.PanelWidth - 20));
        Assert.True(panel.OnMouseWheel(100, 100, -100));
        panel.Render(canvas, modules, height, "player");
        Assert.InRange(panel.CommandPanelRows.Single(r => r.Name == "Engine").StatusBarRect.Bottom, 40, height);
    }
}
