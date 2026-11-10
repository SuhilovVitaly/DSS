using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class WeaponHudControlsTests
{
    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void Visible_contacts_outside_largest_free_rectangle_never_get_edge_markers(float scale)
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0,
            [new("player", 0, 0, 0, 0), new("enemy", -100, 0, 0, 0, RenderObjectType: SpaceObjectType.NpcShip)],
            "player", SelectedObjectId: "enemy"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale, timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        var free = screen.AvailableMapRect();
        var world = World(screen, Math.Max(10, free.Left - 5), 540);
        buffer.Update(buffer.Latest!.Snapshot with
        {
            SnapshotSequence = 2,
            Objects = [new("player", 0, 0, 0, 0), new("enemy", world.X, world.Y, 0, 0, RenderObjectType: SpaceObjectType.NpcShip)]
        });
        screen.OnDeactivated();
        screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale, timestampProvider: () => 0);
        screen.Render(canvas, 1920, 1080);
        Assert.Empty(screen.OffscreenMarkerIds);
        var outside = World(screen, -100, 540);
        buffer.Update(buffer.Latest!.Snapshot with
        {
            SnapshotSequence = 3,
            Objects = [new("player", 0, 0, 0, 0), new("enemy", outside.X, outside.Y, 0, 0, RenderObjectType: SpaceObjectType.NpcShip)]
        });
        screen.OnDeactivated();
        screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale, timestampProvider: () => 0);
        screen.Render(canvas, 1920, 1080);
        Assert.Equal("enemy", Assert.Single(screen.OffscreenMarkerIds));
        screen.OnDeactivated();
    }
    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void Player_ship_edge_arrow_tracks_offscreen_pose_and_clears_on_return(float scale)
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [new("player", 0, 0, 0, 0)], "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale, timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        try
        {
            foreach (var (dx, dy) in new[] { (-1100, 0), (1100, 0), (0, -680), (0, 680), (1100, -680) })
            {
                screen.OnKeyDown(Key.Home);
                screen.Render(canvas, 1920, 1080);
                Assert.Empty(screen.OffscreenMarkerIds);
                var free = screen.AvailableMapRect();
                float startX = free.MidX, startY = free.Top + 50;
                screen.OnMouseDown(startX, startY);
                screen.OnMouseMove(startX + dx, startY + dy);
                screen.OnMouseUp(startX + dx, startY + dy);
                screen.Render(canvas, 1920, 1080);
                Assert.Equal("player", Assert.Single(screen.OffscreenMarkerIds));

                free.Inflate(-18, -18);
                float toX = 960 + dx - free.MidX, toY = 540 + dy - free.MidY;
                float edge = Math.Min(free.Width / 2 / Math.Max(.001f, Math.Abs(toX)), free.Height / 2 / Math.Max(.001f, Math.Abs(toY)));
                float x = free.MidX + edge * toX, y = free.MidY + edge * toY;
                float length = MathF.Sqrt(toX * toX + toY * toY);
                Assert.True(IsArrowPixel(x, y));
                Assert.True(IsArrowPixel(x + 5 * toX / length, y + 5 * toY / length));
                Assert.False(IsArrowPixel(x - 7 * toX / length, y - 7 * toY / length));
            }

            var oldZoom = screen.CameraPixelsPerWorldUnit;
            screen.OnKeyDown(Key.Minus);
            screen.Render(canvas, 1920, 1080);
            Assert.True(screen.CameraPixelsPerWorldUnit < oldZoom);
            Assert.Equal("player", Assert.Single(screen.OffscreenMarkerIds));

            var latest = buffer.Latest!.Snapshot;
            var target = World(screen, -160, 360);
            buffer.Update(latest with
            {
                SnapshotSequence = 2,
                Objects = [latest.Objects[0] with { NavigationTargetX = target.X, NavigationTargetY = target.Y }]
            });
            screen.Render(canvas, 1920, 1080);
            Assert.Equal(new[] { "player", "navigation-point" }, screen.OffscreenMarkerIds);

            screen.Render(canvas, 1600, 900);
            Assert.Contains("player", screen.OffscreenMarkerIds);

            screen.OnKeyDown(Key.Home);
            screen.Render(canvas, 1600, 900);
            Assert.DoesNotContain("player", screen.OffscreenMarkerIds);
            buffer.Update(latest with { SnapshotSequence = 3, Objects = [] });
            screen.Render(canvas, 1600, 900);
            Assert.Empty(screen.OffscreenMarkerIds);
        }
        finally
        {
            screen.OnDeactivated();
        }

        bool IsArrowPixel(float x, float y)
        {
            var color = bitmap.GetPixel((int)x, (int)y);
            return color.Red > 180 && color.Green > 130 && color.Red > color.Green && color.Blue < 165;
        }
    }

    private static (double X, double Y) World(GameSessionScreen screen, float x, float y) =>
        (screen.CameraFocusX + (x - 960) / screen.CameraPixelsPerWorldUnit,
         screen.CameraFocusY + (y - 540) / screen.CameraPixelsPerWorldUnit);

}
