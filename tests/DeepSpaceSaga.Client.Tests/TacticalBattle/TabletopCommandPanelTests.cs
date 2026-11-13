using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class TabletopCommandPanelTests
{
    [Fact]
    public async Task Panel_is_offset_from_card_and_captures_map_click_pan_and_zoom()
    {
        await using var scene = await Ep0019Scenes.Create("1-move-no-hover", withoutAnomalies: true);
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var screen = new TacticalBattleScreen(scene.Screen.Flow, 1, catalog.GetOrClassic("tabletop"));
        using var bitmap = new SKBitmap(1280, 720); using var canvas = new SKCanvas(bitmap);
        try
        {
            Assert.Null(screen.CaptureFrame(1280, 720)!.SelectedDroneId);
            screen.OnKeyDown(Key.Tab);
            screen.Render(canvas, 1280, 720);
            string selected = screen.Movement.Selected!;
            var panel = screen.Layout.Rects["pilot-panel:" + selected];
            Assert.Equal(new SKRect(51, 49, 201, 140), panel);
            Assert.Contains(panel, screen.PanelRects);
            var before = screen.View;
            screen.OnMouseDown(200, 138, MouseButton.Left);
            screen.OnMouseUp(200, 138, MouseButton.Left);
            Assert.Empty(screen.BuildMoveRequest().Orders);
            screen.OnMouseDown(200, 138, MouseButton.Right); screen.OnMouseMove(230, 168); screen.OnMouseUp(230, 168, MouseButton.Right);
            Assert.Equal(before, screen.View);
            screen.OnMouseWheel(200, 138, -1);
            Assert.Equal(before, screen.View);
            Assert.Equal(selected, screen.Movement.Selected);
            Assert.True(panel.Contains(screen.CardCommandBounds("speed-")));
            var button = screen.CardCommandBounds("straight");
            int x = (int)button.Left + 5, y = (int)button.Top + 4;
            var normal = bitmap.GetPixel(x, y);
            Assert.True(screen.OnMouseMove(x, y));
            screen.Render(canvas, 1280, 720);
            Assert.NotEqual(normal, bitmap.GetPixel(x, y));
            Assert.False(screen.OnMouseMove(200, 138));
            screen.Render(canvas, 1280, 720);
            Assert.Equal(normal, bitmap.GetPixel(x, y));
            var tongue = bitmap.GetPixel(65, 48);
            Assert.True(tongue.Red > 200 && tongue.Green > 150 && tongue.Blue < 120);
        }
        finally { screen.OnDeactivated(); }
    }
}
