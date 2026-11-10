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
        Assert.True(f.Screen.ObjectInfoPanel.RowBodyRects[0].Height > 0);
        var lines = ObjectInfoPanel.BuildLines(new("ship", "Ship", 5, 90, SpaceObjectType.PlayerShip));
        Assert.Contains(lines, l => l.Label == "Name");
        Assert.Contains(lines, l => l.Label == "Speed");
        Assert.True(f.Screen.ObjectInfoPanel.RowBodyRects[0].Height > 0);
    }

    [Fact]
    public void Module_details_are_available_on_left()
    {
        InstalledModuleSnapshot[] modules = Enumerable.Range(0, 4).Select(i => new InstalledModuleSnapshot(
            "m" + i, "type", "Module " + i, i, [],
            StructureCurrent: i == 3 ? 0 : 50, StructureMax: 100, IsDestroyed: i == 3)).ToArray();
        var diagram = new ModuleDiagram();
        diagram.Update([.. modules]);
        var lines = CommandsPanel.ModuleDetails("Navigation", modules, diagram);
        foreach (var module in modules) Assert.Contains(lines, l => l.StartsWith(module.DisplayName));
        Assert.Contains(lines, l => l.Contains(Localization.Get("World.Panel.Structure")) && l.Contains("0"));
    }

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public void Structure_bar_segments_share_vertical_bounds(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        using var typeface = SKTypeface.FromFamilyName("Consolas");
        using var paint = new SKPaint { TextSize = 12, Typeface = typeface };
        string prefix = Localization.Get("World.Panel.Structure") + " " + Localization.Get("World.Panel.BarLeft");
        float segmentWidth = paint.MeasureText(Localization.Get("World.Panel.BarFull"));

        var full = RenderBar(150);
        foreach (var (value, filled) in new[] { (0, 0), (60, 2), (80, 3), (150, 6) })
        {
            var pixels = RenderBar(value);
            for (int segment = 0; segment < 6; segment++)
            {
                Assert.Equal(full[segment].Top, pixels[segment].Top);
                Assert.Equal(full[segment].Bottom, pixels[segment].Bottom);
                Assert.Equal(segment < filled, full[segment].Count == pixels[segment].Count);
            }
        }

        (int Top, int Bottom, int Count)[] RenderBar(int value)
        {
            InstalledModuleSnapshot[] modules = [new("m", "type", "Module", 0, [], StructureCurrent: value, StructureMax: 150)];
            var diagram = new ModuleDiagram();
            diagram.Update([.. modules]);
            var panel = new CommandsPanel { OwnModuleDiagram = diagram };
            using var bitmap = new SKBitmap(450, 900);
            using var canvas = new SKCanvas(bitmap);
            try
            {
                panel.Render(canvas, modules, bitmap.Height);
                var row = Assert.Single(panel.CommandPanelRows, row => row.Name == "Navigation");
                Assert.Contains(row.DetailLines, line => line.StartsWith(prefix, StringComparison.Ordinal) && line.EndsWith($" {value}", StringComparison.Ordinal));
                int baseline = (int)Math.Max(row.BodyRect.Top + 16, row.Buttons.IsDefaultOrEmpty ? 0 : row.Buttons.Max(b => b.Rect.Bottom) + 16) + 16;
                float left = row.BodyRect.Left + 10 + paint.MeasureText(prefix);
                return Enumerable.Range(0, 6).Select(segment =>
                {
                    var rows = new List<int>();
                    int solid = 0;
                    for (int y = baseline - 14; y < baseline + 9; y++)
                    {
                        for (int x = (int)Math.Ceiling(left + segment * segmentWidth); x < (int)Math.Floor(left + (segment + 1) * segmentWidth); x++)
                        {
                            byte green = bitmap.GetPixel(x, y).Green;
                            if (green > 120) rows.Add(y);
                            if (green > 225) solid++;
                        }
                    }
                    Assert.NotEmpty(rows);
                    return (rows.Min() - baseline, rows.Max() - baseline, solid);
                }).ToArray();
            }
            finally
            {
                panel.ReleasePresentationCache();
            }
        }
    }

    [Theory]
    [InlineData(1280, 720, 1.5f)]
    [InlineData(1920, 1080, 1f)]
    public async Task Panels_survive_resize_and_selection(int width, int height, float scale)
    {
        await using var f = await PointCombatUiFixture.Create(width, height, scale);
        Assert.Contains(f.Screen.CommandsPanel.CommandPanelRows, row => row.Name == "Engine");
        Assert.Contains(f.Screen.CommandsPanel.CommandPanelRows, r => !r.DetailLines.IsDefaultOrEmpty);
        using var bitmap = new SKBitmap(width + 100, height + 100);
        using var canvas = new SKCanvas(bitmap);
        f.Screen.Render(canvas, width + 100, height + 100);
        Assert.True(f.Screen.ObjectInfoPanel.RowBodyRects[0].Height > 0);
        Assert.Contains(f.Screen.CommandsPanel.CommandPanelRows, row => row.Name == "Navigation");
    }
}
