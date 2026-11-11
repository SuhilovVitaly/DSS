using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class OverlayShellTests
{
    [Fact]
    public async Task Panel_rectangles_block_map_clicks()
    {
        await using var s = await Ep0019Scenes.Create("1-move-no-hover"); using var b = s.Render();
        var before = s.Screen.BuildMoveRequest().Orders;
        s.Screen.OnMouseDown(1850, 90, MouseButton.Left);
        Assert.Equal(before, s.Screen.BuildMoveRequest().Orders);
        Assert.Equal(1280, s.Screen.View.PixelWidth);
        Assert.Equal(BattleDesign.MapBg, b.GetPixel(1100, 1040));
        Assert.Equal(6, s.Screen.PanelRects.Count);
        var primary = s.Screen.Layout.Rects["button:primary"];
        Assert.Equal(new SKRect(1048, 116, 1266, 144), primary);
        Assert.Equal(ScreenEvent.MoveTacticalBattle, s.Screen.OnMouseDown(primary.MidX * 1.5f, primary.MidY * 1.5f, MouseButton.Left));
        var card = s.Screen.Layout.Rects["build"];
        Assert.Equal(new SKRect(1040, 158, 1274, 458), card);
        s.Screen.OnMouseDown(card.MidX * 1.5f, card.MidY * 1.5f, MouseButton.Left);
        Assert.Equal(before, s.Screen.BuildMoveRequest().Orders);
    }
}
