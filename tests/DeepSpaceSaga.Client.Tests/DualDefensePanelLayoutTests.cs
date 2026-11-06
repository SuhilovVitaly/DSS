using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class DualDefensePanelLayoutTests
{
    [Fact]
    public void World_command_panel_preserves_all_four_template_groups()
    {
        var panel = new CommandsPanel();
        using var bitmap = new SKBitmap(400, 2000);
        using var canvas = new SKCanvas(bitmap);
        panel.Render(canvas, ImmutableArray<InstalledModuleSnapshot>.Empty, 2000, "player");
        Assert.Equal(new[] { "Navigation", "Maneuver", "Engine", "Space Control" }, panel.CommandPanelRows.Select(r => r.Name));
    }
}
