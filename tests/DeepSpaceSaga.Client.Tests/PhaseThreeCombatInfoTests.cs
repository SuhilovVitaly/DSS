using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
namespace DeepSpaceSaga.Client.Tests;

public class PhaseThreeCombatInfoTests
{
    private static InstalledModuleSnapshot Module(string id, decimal unusedAccuracy, int unusedChance) => new(id,
        "module.countermeasure.launcher.basic", "Countermissile", id == "defense-1" ? 2 : 3, [DefenseCommandTypes.Launch],
        "On", "Ready", 60, Commands: [new(DefenseCommandTypes.Launch, "Launch", "point")],
        Defense: new(false, null, DefenseState.Ready, Profiles: [new("wide", 15, 1), new("fast", 25, .5)], RangeKm: 30));
    [Theory]
    [InlineData(200)]
    [InlineData(480)]
    public void Long_details_wrap_and_remain_scrollable(int height)
    {
        string detail = string.Join(" ", Enumerable.Repeat("captured accuracy and maneuverability", 8));
        var panel = new CommandsPanel(weaponDetails: _ => [detail], weaponCommandEnabled: (_, _) => true);
        using var bitmap = new SKBitmap(400, height);
        using var canvas = new SKCanvas(bitmap);
        var modules = new[] { Module("defense-1", 55, 500), Module("defense-2", 75, 700) };
        panel.Render(canvas, modules, height, "player");
        var row = panel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == "defense-2");
        panel.OnMouseDown(row.CaptionRect.MidX, row.CaptionRect.MidY);
        panel.Render(canvas, modules, height, "player");
        row = panel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == "defense-2");
        Assert.True(row.Opened);
        Assert.True(row.DetailLines.Length > 2);
        using var paint = new SKPaint { TextSize = 12, IsAntialias = true };
        Assert.All(row.DetailLines, l => Assert.True(paint.MeasureText(l) <= CommandsPanel.PanelWidth - 20));
        Assert.True(panel.OnMouseWheel(100, 100, -100));
        panel.Render(canvas, modules, height, "player");
        Assert.InRange(panel.CommandPanelRows.Single(r => r.Weapon?.ModuleId == "defense-2").StatusBarRect.Bottom, 40, height);
    }
}
