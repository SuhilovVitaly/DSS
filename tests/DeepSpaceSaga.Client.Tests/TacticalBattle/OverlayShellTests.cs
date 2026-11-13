using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Contracts;
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
        s.Screen.OnMouseDown(1850, 90, MouseButton.Right);
        s.Screen.OnMouseUp(1850, 90, MouseButton.Right);
        Assert.Equal(before, s.Screen.BuildMoveRequest().Orders);
        Assert.Equal(1280, s.Screen.View.PixelWidth);
        Assert.Equal(BattleDesign.MapBg, b.GetPixel(1100, 1040));
        Assert.Equal(7, s.Screen.PanelRects.Count);
        var primary = s.Screen.Layout.Rects["button:primary"];
        Assert.Equal(new SKRect(1048, 242, 1266, 270), primary);
        Assert.Equal(ScreenEvent.MoveTacticalBattle, s.Screen.OnMouseDown(primary.MidX * 1.5f, primary.MidY * 1.5f, MouseButton.Left));
        var card = s.Screen.Layout.Rects["battle-log"];
        Assert.DoesNotContain("build", s.Screen.Layout.Rects.Keys);
        Assert.Contains(BattleLogFormatter.Body(s.Screen.Flow.State!.Log[0]), s.Screen.LogTab.Panel.Text);
        Assert.Equal(new SKRect(1040, 284, 1274, 714), card);
        s.Screen.OnMouseDown(card.MidX * 1.5f, card.MidY * 1.5f, MouseButton.Left);
        Assert.Equal(before, s.Screen.BuildMoveRequest().Orders);
    }

    [Fact]
    public async Task MinimapTracksCameraAndBlocksMapInput()
    {
        await using var scene = await Ep0019Scenes.Create("1-move-no-hover");
        using var before = scene.Render();
        var screen = scene.Screen;
        var map = screen.Layout.Rects["minimap"];
        var viewport = screen.Layout.Rects["minimap:viewport"];
        var view = screen.View;
        Assert.Equal(new SKRect(1040, 6, 1274, 126), map);
        var phase = screen.Layout.Rects["phase"];
        Assert.Equal(map.Width, phase.Width);
        Assert.Equal(map.Right, phase.Right);
        Assert.True(map.Bottom < phase.Top);
        Assert.Contains(screen.PanelRects, rect => rect.Contains(map.MidX * 1.5f, map.MidY * 1.5f));
        Assert.InRange(viewport.Width / viewport.Height, 1.7777f, 1.7779f);

        var orders = screen.BuildMoveRequest().Orders;
        var x = map.MidX * 1.5f;
        var y = map.MidY * 1.5f;
        screen.OnMouseDown(x, y, MouseButton.Right);
        screen.OnMouseMove(x - 50, y - 50);
        screen.OnMouseUp(x - 50, y - 50, MouseButton.Right);
        screen.OnMouseWheel(x, y, -100);
        Assert.Equal(orders, screen.BuildMoveRequest().Orders);
        Assert.Equal(view, screen.View);

        screen.OnMouseWheel(960, 540, -1000000);
        using var zoomed = scene.Render();
        var zoomedViewport = screen.Layout.Rects["minimap:viewport"];
        Assert.True(zoomedViewport.Width < viewport.Width);
        screen.OnMouseDown(960, 540, MouseButton.Right);
        screen.OnMouseMove(1060, 590);
        screen.OnMouseUp(1060, 590, MouseButton.Right);
        using var panned = scene.Render();
        var pannedViewport = screen.Layout.Rects["minimap:viewport"];
        Assert.True(pannedViewport.MidX < zoomedViewport.MidX);
        Assert.True(pannedViewport.MidY < zoomedViewport.MidY);

        using var resized = new SKBitmap(1440, 1080);
        using var canvas = new SKCanvas(resized);
        screen.Render(canvas, resized.Width, resized.Height);
        Assert.Equal(954, screen.Layout.Rects["minimap"].Right);
        var resizedViewport = screen.Layout.Rects["minimap:viewport"];
        Assert.InRange(resizedViewport.Width / resizedViewport.Height, 1.3332f, 1.3335f);

        var unit = screen.Flow.State!.Units.First(u => u.Kind == TacticalUnitKind.Carrier);
        var marker = screen.Layout.Rects["minimap:unit:" + unit.Id];
        var oldZoom = screen.View.PixelsPerUnit;
        screen.OnMouseDown(marker.MidX * 1.5f, marker.MidY * 1.5f, MouseButton.Left);
        screen.OnMouseUp(marker.MidX * 1.5f, marker.MidY * 1.5f, MouseButton.Left);
        Assert.InRange(Math.Abs(screen.View.CenterX - unit.Pose.X), 0, .01);
        Assert.InRange(Math.Abs(screen.View.CenterY - unit.Pose.Y), 0, .01);
        Assert.Equal(oldZoom, screen.View.PixelsPerUnit);
        Assert.Equal(orders, screen.BuildMoveRequest().Orders);
    }

    [Fact]
    public async Task MovementStrokeGetsThinnerWhenZoomingOut()
    {
        await using var scene = await Ep0019Scenes.Create("1-move-no-hover");
        using var bitmap = scene.Render();
        var screen = scene.Screen;
        var before = screen.View;
        screen.OnMouseWheel(960, 540, 1000000);
        Assert.True(screen.View.PixelsPerUnit < before.PixelsPerUnit);
        Assert.InRange(TacticalBattleMapRenderer.MovementStrokeScale(screen.View), .5f,
            TacticalBattleMapRenderer.MovementStrokeScale(before) - .01f);
        screen.OnMouseWheel(960, 540, -1000000);
        Assert.True(TacticalBattleMapRenderer.MovementStrokeScale(screen.View) >
            TacticalBattleMapRenderer.MovementStrokeScale(before));
    }

    [Fact]
    public async Task BattleLogScrollsWithoutZoomingTheMap()
    {
        await using var scene = await Ep0019Scenes.Create("4-salvo-results");
        using var bitmap = scene.Render();
        var screen = scene.Screen;
        var panel = screen.LogTab.Panel;
        var view = screen.View;
        Assert.Equal(panel.MaximumScroll, panel.ScrollOffset);
        Assert.True(panel.ScrollOffset > 0);
        screen.OnMouseWheel(panel.Bounds.MidX * 1.5f, panel.Bounds.MidY * 1.5f, -100);
        Assert.True(panel.ScrollOffset < panel.MaximumScroll);
        Assert.Equal(view, screen.View);
    }

    [Fact]
    public async Task MinimapShowsOnlySurvivingCarriersAndDrones()
    {
        await using var scene = await Ep0019Scenes.Create("1-move-no-hover");
        var frame = scene.Screen.CaptureFrame(1280, 720)!;
        var carrier = frame.Battle.Units.First(unit => unit.Kind == TacticalUnitKind.Carrier);
        var drone = frame.Battle.Units.First(unit => unit.Kind == TacticalUnitKind.Drone);
        frame = frame with
        {
            Battle = frame.Battle with
            {
                Units =
                [
                    carrier with { Pose = new(2000, 500, 0) },
                    drone with { Pose = new(-1500, 0, 0), State = TacticalUnitState.Disabled },
                    drone with { Id = "wreck", State = TacticalUnitState.Destroyed }
                ]
            }
        };
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        var layout = new BattleLayout();
        var bounds = TacticalBattleMinimap.Bounds(frame);
        BattleDesign.Recording = layout;
        try { TacticalBattleMinimap.Draw(canvas, frame, bounds); }
        finally { BattleDesign.Recording = null; }

        Assert.Equal(4, layout.Rects.Count);
        Assert.Empty(layout.Texts);
        var carrierMark = layout.Rects["minimap:unit:" + carrier.Id];
        var droneMark = layout.Rects["minimap:unit:" + drone.Id];
        Assert.True(carrierMark.Width > droneMark.Width);
        Assert.True(carrierMark.MidX > droneMark.MidX);
        Assert.True(carrierMark.MidY > droneMark.MidY);
        Assert.True(bounds.Contains(carrierMark));
        Assert.True(bounds.Contains(droneMark));
    }
}
