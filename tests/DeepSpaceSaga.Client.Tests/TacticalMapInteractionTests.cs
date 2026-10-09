using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class TacticalMapInteractionTests
{
    [Theory]
    [InlineData(.8f)]
    [InlineData(1f)]
    [InlineData(1.2f)]
    [InlineData(1.5f)]
    public void Hover_over_panel_does_not_activate_map_object(float scale)
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, []));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        screen.SetUiScale(scale);
        using var surface = SKSurface.Create(new SKImageInfo(1920, 1080));
        screen.Render(surface.Canvas, 1920, 1080);
        var panel = screen.LastSpeedPanelRect;
        float x = panel.MidX * scale, y = panel.MidY * scale;
        var obj = new ObjectMotionSnapshot("under-panel",
            screen.CameraFocusX + (x - 960) / screen.CameraPixelsPerWorldUnit,
            screen.CameraFocusY + (y - 540) / screen.CameraPixelsPerWorldUnit, 0, 0);
        buffer.Update(new(2, 0, SimulationSpeed.Speed0, [obj]));
        screen.Render(surface.Canvas, 1920, 1080);
        screen.OnMouseMove(x, y);
        Assert.Null(screen.ActiveObjectId);
    }

    [Theory]
    [InlineData(-6, false)]
    [InlineData(-4, true)]
    public void Offscreen_marker_is_not_interactive(float markerX, bool visible)
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0,
            [new("edge", 10000 + markerX - 960, 10000, 0, 0)]));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var surface = SKSurface.Create(new SKImageInfo(1920, 1080));
        screen.Render(surface.Canvas, 1920, 1080);
        screen.OnMouseMove(1, 540);
        Assert.Equal(visible ? "edge" : null, screen.ActiveObjectId);
        screen.OnMouseDown(1, 540);
        Assert.Equal(visible ? "edge" : null, screen.SelectedObjectId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Visible_hit_priority_is_stable(bool reverse)
    {
        ObjectMotionSnapshot[] objects = [new("near", 10000, 10000, 0, 0),
            new("station-b", 10020, 10000, 0, 0, RenderObjectType: SpaceObjectType.Station),
            new("station-a", 10020, 10000, 0, 0, RenderObjectType: SpaceObjectType.Station)];
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0,
            (reverse ? objects.AsEnumerable().Reverse() : objects).ToImmutableArray()));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var surface = SKSurface.Create(new SKImageInfo(1920, 1080));
        screen.Render(surface.Canvas, 1920, 1080);
        screen.OnMouseMove(960, 540);
        Assert.Equal("station-a", screen.ActiveObjectId);
        screen.OnMouseDown(960, 540);
        Assert.Equal(screen.ActiveObjectId, screen.SelectedObjectId);
    }
}
