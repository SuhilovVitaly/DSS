using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public class PointCombatUS0020TK0002Tests
{
    [Fact]
    public async Task Player_info_restores_ship_image_and_fields()
    {
        await using var f = await PointCombatUiFixture.Create();
        Assert.Null(f.Screen.ObjectInfoPanel.OwnModuleDiagram);
        var lines = ObjectInfoPanel.BuildLines(new("ship", "Ship", 5, 90, SpaceObjectType.PlayerShip));
        Assert.Contains(lines, l => l.Label == "Name");
        Assert.Contains(lines, l => l.Label == "Speed");
        Assert.True(f.Screen.ObjectInfoPanel.RowBodyRects[0].Height > 0);
    }

    [Fact]
    public void Module_details_are_available_on_left()
    {
        InstalledModuleSnapshot[] modules = Enumerable.Range(0, 4).Select(i => new InstalledModuleSnapshot(
            "m" + i, "type", "Module " + i, i, [], ArmorCurrent: 20, ArmorMax: 40,
            StructureCurrent: i == 3 ? 0 : 50, StructureMax: 100, IsDestroyed: i == 3,
            IsCritical: i == 1, DisabledUntilMotionTimeMs: i == 2 ? 8000 : null)).ToArray();
        var diagram = new ModuleDiagram();
        diagram.Update(null, 1, "ship", [.. modules], [], 0, .25, 0);
        var lines = CommandsPanel.ModuleDetails("Navigation", null, modules, diagram);
        foreach (var module in modules) Assert.Contains(lines, l => l.StartsWith(module.DisplayName));
        Assert.Contains(lines, l => l.Contains(Localization.Combat("Panel.Armor")) && l.Contains("20"));
        Assert.Contains(lines, l => l.Contains(Localization.Combat("Disabled")) && l.Contains("32"));
        Assert.Contains(lines, l => l.Contains(Localization.Combat("Panel.Structure")) && l.Contains("0"));
        Assert.Contains(lines, l => l.EndsWith(" " + Localization.Combat("Panel.CriticalIcon")));
    }

    [Theory]
    [InlineData(1280, 720, 1.5f)]
    [InlineData(1920, 1080, 1f)]
    public async Task Panels_survive_resize_and_selection(int width, int height, float scale)
    {
        await using var f = await PointCombatUiFixture.Create(width, height, scale);
        Assert.NotEmpty(f.Screen.CommandsPanel.OwnModuleDiagram!.Tiles);
        Assert.Contains(f.Screen.CommandsPanel.CommandPanelRows, r => !r.DetailLines.IsDefaultOrEmpty);
        using var bitmap = new SKBitmap(width + 100, height + 100);
        using var canvas = new SKCanvas(bitmap);
        f.Screen.Render(canvas, width + 100, height + 100);
        Assert.Null(f.Screen.ObjectInfoPanel.OwnModuleDiagram);
        Assert.True(f.Screen.CombatReportsBounds.Height > 0);
    }
}
