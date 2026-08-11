using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class TacticalMapDepthRendererTests
{
    private const int CanvasSize = 64;

    [Fact]
    public void Spherical_marker_lights_upper_left_and_shades_lower_right_for_every_palette_color()
    {
        SKColor[] palette =
        [
            SpaceMapColorResolver.FallbackColor,
            SpaceMapColorResolver.PlayerShipColor,
            SpaceMapColorResolver.NpcNeutralColor,
            SpaceMapColorResolver.NpcEnemyColor,
            SpaceMapColorResolver.NpcFriendColor,
            SpaceMapColorResolver.AsteroidColor,
            SpaceMapColorResolver.ContainerColor,
            SpaceMapColorResolver.StationColor,
            SpaceMapColorResolver.PlanetColor,
            SpaceMapColorResolver.SunColor
        ];

        var renderer = new TacticalMapDepthRenderer();

        foreach (SKColor color in palette)
        {
            using var bitmap = new SKBitmap(CanvasSize, CanvasSize);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Black);

            renderer.DrawSphericalMarker(canvas, 32, 32, 20, color);

            SKColor highlight = bitmap.GetPixel(26, 26);
            SKColor shade = bitmap.GetPixel(38, 38);

            Assert.True(
                Luminance(highlight) > Luminance(shade),
                $"Expected upper-left highlight to be brighter for #{color.Red:X2}{color.Green:X2}{color.Blue:X2}; " +
                $"highlight={highlight}, shade={shade}");
        }
    }

    [Fact]
    public void Future_trajectory_is_a_lit_tube_with_upper_left_highlight_and_lower_right_shadow()
    {
        var renderer = new TacticalMapDepthRenderer();
        var camera = new CameraState(focusX: 0, focusY: 0, pixelsPerWorldUnit: 1);
        FutureTrajectoryPoint[] points =
        [
            new(-20, 0),
            new(20, 0)
        ];

        using var bitmap = new SKBitmap(CanvasSize, CanvasSize);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);

        renderer.DrawFutureTrajectory(canvas, points, camera, CanvasSize, CanvasSize);

        SKColor highlight = bitmap.GetPixel(32, 31);
        SKColor shadow = bitmap.GetPixel(32, 35);

        Assert.True(Luminance(highlight) > Luminance(shadow));
        Assert.True(Luminance(shadow) > 0);
    }

    [Fact]
    public void Navigation_trajectory_keeps_a_distinct_warm_3d_profile()
    {
        var renderer = new TacticalMapDepthRenderer();
        var camera = new CameraState(focusX: 0, focusY: 0, pixelsPerWorldUnit: 1);
        FutureTrajectoryPoint[] points =
        [
            new(-20, 0),
            new(20, 0)
        ];

        using var bitmap = new SKBitmap(CanvasSize, CanvasSize);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);

        renderer.DrawNavigationTrajectory(canvas, points, camera, CanvasSize, CanvasSize);

        SKColor highlight = bitmap.GetPixel(32, 31);
        SKColor shadow = bitmap.GetPixel(32, 35);

        Assert.True(Luminance(highlight) > Luminance(shadow));
        Assert.True(highlight.Red > highlight.Blue);
    }

    private static int Luminance(SKColor color)
    {
        return color.Red + color.Green + color.Blue;
    }
}
