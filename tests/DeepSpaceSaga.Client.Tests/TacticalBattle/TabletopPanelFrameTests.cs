using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class TabletopPanelFrameTests
{
    [Fact]
    public void Steel_frame_has_square_corners_reversed_insets_shadow_and_no_glow()
    {
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        using var bitmap = new SKBitmap(800, 600); using var canvas = new SKCanvas(bitmap); canvas.Scale(10);
        var rect = new SKRect(10, 10, 60, 45);
        using (TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic("tabletop")))
        {
            Assert.True(TacticalBattleSkinRenderScope.Steel);
            canvas.Clear(SKColors.Transparent);
            BattleDesign.PanelBox(canvas, rect, selected: true);
            Assert.Equal(SKColor.Parse("#4C5764"), bitmap.GetPixel(300, 110));
            Assert.Equal(SKColor.Parse("#0A0E12"), bitmap.GetPixel(300, 440));
            Assert.Equal(SKColor.Parse("#05070A"), bitmap.GetPixel(300, 100));
            Assert.True(bitmap.GetPixel(100, 100).Alpha > 0);
            Assert.Equal(new SKColor(0, 0, 0, 128), bitmap.GetPixel(610, 470));
            var before = bitmap.Bytes;
            BattleDesign.Round(canvas, rect, SKColors.Red, SKColors.White, 20, 20, "ui.button.glow");
            Assert.Equal(before, bitmap.Bytes);
            canvas.Clear(SKColors.Transparent);
            BattleDesign.Round(canvas, rect, SKColors.Red, SKColors.White);
            Assert.Equal(SKColor.Parse("#0A0E12"), bitmap.GetPixel(300, 110));
            Assert.Equal(SKColor.Parse("#4C5764"), bitmap.GetPixel(300, 440));
            canvas.Clear(SKColors.Transparent);
            BattleDesign.Button(canvas, rect, "", big: true);
            Assert.Equal(SKColor.Parse("#4A3410"), bitmap.GetPixel(300, 100));
        }
        using (TacticalBattleSkinRenderScope.Enter(TacticalBattleSkin.Classic))
        {
            Assert.False(TacticalBattleSkinRenderScope.Steel);
            canvas.Clear(SKColors.Transparent); BattleDesign.PanelBox(canvas, rect);
            Assert.Equal(0, bitmap.GetPixel(100, 100).Alpha);
            Assert.Equal(0, bitmap.GetPixel(610, 470).Alpha);
        }
    }

    [Fact]
    public async Task Phase_window_geometry_is_preserved_and_bevel_is_dark_in_real_frame()
    {
        await using var scene = await Ep0019Scenes.Create("1-move-no-hover");
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var screen = new TacticalBattleScreen(scene.Screen.Flow, 1, catalog.GetOrClassic("tabletop"));
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        try
        {
            scene.Screen.Render(canvas, 1920, 1080); var classicPhase = scene.Screen.Layout.Rects["phase"];
            screen.Render(canvas, 1920, 1080);
            Assert.Equal(classicPhase, screen.Layout.Rects["phase"]);
            var phase = screen.Layout.Rects["phase"];
            var bevel = bitmap.GetPixel((int)(phase.MidX * 1.5f), (int)((phase.Top + 1) * 1.5f));
            Assert.InRange(bevel.Red, 0x4C - 12, 0x4C + 12);
            Assert.InRange(bevel.Green, 0x57 - 12, 0x57 + 12);
            Assert.InRange(bevel.Blue, 0x64 - 12, 0x64 + 12);
            for (int x = (int)(phase.Left * 1.5f); x < phase.Right * 1.5f; x++)
            {
                var edge = bitmap.GetPixel(x, (int)(phase.Top * 1.5f));
                Assert.True(edge.Red <= 128 && edge.Green <= 128 && edge.Blue <= 128);
            }
            if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { Length: > 0 } folder)
            {
                using var crop = new SKBitmap(740, 1440); using var cropCanvas = new SKCanvas(crop);
                cropCanvas.Scale(2); cropCanvas.Translate(-1551, 0);
                screen.Render(cropCanvas, 1920, 1080);
                using var png = crop.Encode(SKEncodedImageFormat.Png, 100);
                using var file = File.Create(Path.Combine(folder, "steel-frames-windows-x2.png")); png.SaveTo(file);
            }
        }
        finally { screen.OnDeactivated(); }
    }
}
