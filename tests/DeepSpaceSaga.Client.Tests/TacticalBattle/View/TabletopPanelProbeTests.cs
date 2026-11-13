using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle.View;

[Collection("InterfaceLog")]
public sealed class TabletopPanelProbeTests
{
    [Theory]
    [InlineData("1-move-no-hover", 1f)]
    [InlineData("1-move-no-hover", 1.5f)]
    [InlineData("2-fire-hover-own", 1f)]
    [InlineData("2-fire-hover-own", 1.5f)]
    [InlineData("4-salvo-results", 1f)]
    [InlineData("4-salvo-results", 1.5f)]
    public async Task Real_screen_satisfies_panel_geometry_colour_and_input_probes(string name, float scale)
    {
        using var profile = new TabletopTestProfile();
        using var locale = Localization.UseLocaleForTests("Russian");
        await using var scene = await Ep0019Scenes.Create(name, settingsPath: profile.SettingsPath);
        using var bitmap = new SKBitmap((int)(1280 * scale), (int)(720 * scale));
        using var canvas = new SKCanvas(bitmap);
        scene.Screen.Render(canvas, bitmap.Width, bitmap.Height);
        var layout = scene.Screen.Layout;
        Assert.Equal("tabletop", scene.Screen.SkinId);
        if (name == "1-move-no-hover")
        {
            Assert.Equal(new SKRect(6, 6, 156, 46), layout.Rects["pilot:Вера"]);
            Assert.Equal(new SKRect(51, 49, 201, 140), layout.Rects["pilot-panel:Вера"]);
            Assert.Equal(146, layout.Rects["pilot:Юн"].Top);
            var color = Pixel(112, 12);
            int steel = Math.Min(Distance(color, SKColor.Parse("#3A4450")), Distance(color, SKColor.Parse("#212831")));
            Assert.True(steel < Distance(color, SKColor.Parse("#0E1B1F")));
        }
        else if (name == "2-fire-hover-own")
        {
            var panel = layout.Rects["pilot-panel:Юн"];
            foreach (string command in new[] { "target:Таг-5", "target:Таг-6", "decoy", "evade", "hold" })
                Assert.True(panel.Contains(layout.Rects["card-command:" + command]));
            var target = layout.Rects["card-command:target:Таг-5"];
            scene.Screen.OnMouseDown(target.MidX * scale, target.MidY * scale, MouseButton.Left);
            Assert.Equal("Таг-5", scene.Screen.BuildFireRequest().Targets.Single(t => t.DroneId == "Юн").TargetId);
        }
        else
        {
            var armor = layout.Rects["pilot-damage-armor:Юн"];
            var gold = SKColor.Parse("#FFE27A");
            // A 1.4px stroke centred on an integer edge covers 70% of the pixel at S1.
            var background = Pixel(armor.MidX, armor.Top - 3);
            float coverage = scale == 1 ? .7f : 1;
            var expected = new SKColor((byte)(gold.Red * coverage + background.Red * (1 - coverage)),
                (byte)(gold.Green * coverage + background.Green * (1 - coverage)),
                (byte)(gold.Blue * coverage + background.Blue * (1 - coverage)));
            Assert.InRange(Distance(Pixel(armor.MidX, armor.Top), expected), 0, 36);
            Assert.Contains(layout.Texts, text => text.Text is "ПОПАДАНИЕ" or "ПРОМАХ" or "ПОГЛОЩЁН" or "УКЛОНЕНИЕ");
        }
        var phase = layout.Rects["phase"];
        var minimap = layout.Rects["minimap"];
        if (name == "1-move-no-hover")
        {
            var log = scene.Screen.LogTab.Panel.Bounds;
            Assert.Equal(Pixel(log.MidX, log.Bottom - 9), Pixel(minimap.MidX, minimap.Top + 6));
        }
        var bevel = Pixel(phase.MidX, phase.Top + 1);
        Assert.InRange(Distance(bevel, Pixel(minimap.MidX, minimap.Top + 1)), 0, 6);
        Assert.InRange(bevel.Red, 0x4C - 12, 0x4C + 12);
        Assert.InRange(bevel.Green, 0x57 - 12, 0x57 + 12);
        Assert.InRange(bevel.Blue, 0x64 - 12, 0x64 + 12);
        int left = (int)(phase.Left * scale), right = (int)(phase.Right * scale) - 1;
        int top = (int)(phase.Top * scale), bottom = (int)(phase.Bottom * scale) - 1;
        for (int x = left; x <= right; x++) { Dark(bitmap.GetPixel(x, top)); Dark(bitmap.GetPixel(x, bottom)); }
        for (int y = top; y <= bottom; y++) { Dark(bitmap.GetPixel(left, y)); Dark(bitmap.GetPixel(right, y)); }

        SKColor Pixel(float x, float y) => bitmap.GetPixel((int)(x * scale), (int)(y * scale));
        static void Dark(SKColor color) => Assert.True(color.Red <= 128 && color.Green <= 128 && color.Blue <= 128, color.ToString());
    }

    private static int Distance(SKColor one, SKColor two) => Math.Abs(one.Red - two.Red) + Math.Abs(one.Green - two.Green) + Math.Abs(one.Blue - two.Blue);
}
