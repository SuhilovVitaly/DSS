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
    public void Glint_marker_has_a_bright_core_that_fades_toward_the_halo_edge()
    {
        var renderer = new TacticalMapDepthRenderer();

        using var bitmap = new SKBitmap(CanvasSize, CanvasSize);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);

        renderer.DrawGlintMarker(canvas, 32, 32, 10, SpaceMapColorResolver.AsteroidColor);

        SKColor core = bitmap.GetPixel(32, 32);
        SKColor haloEdge = bitmap.GetPixel(32, 32 - 21); // near outer halo radius (10 * 2.2 = 22)

        Assert.True(
            Luminance(core) > Luminance(haloEdge),
            $"Expected bright core to fade toward the halo edge; core={core}, haloEdge={haloEdge}");
    }

    [Fact]
    public void Glint_marker_is_direction_symmetric_with_no_directional_streak()
    {
        var renderer = new TacticalMapDepthRenderer();

        using var bitmap = new SKBitmap(CanvasSize, CanvasSize);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);

        renderer.DrawGlintMarker(canvas, 32, 32, 10, SpaceMapColorResolver.AsteroidColor);

        // Two points equidistant from center in different directions, mirroring the
        // offsets the spherical-marker test uses to detect directional lighting.
        SKColor left = bitmap.GetPixel(32 - 6, 32);
        SKColor upperRight = bitmap.GetPixel(32 + 4, 32 - 4);

        Assert.True(
            Math.Abs(Luminance(left) - Luminance(upperRight)) <= 6,
            $"Expected direction-symmetric glow (no directional streak); left={left}, upperRight={upperRight}");
    }

    [Fact]
    public void Glint_marker_halo_keeps_the_base_color_visually_distinguishable()
    {
        var renderer = new TacticalMapDepthRenderer();

        using var bitmap = new SKBitmap(CanvasSize, CanvasSize);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);

        const float radius = 10;
        renderer.DrawGlintMarker(canvas, 16, 32, radius, SpaceMapColorResolver.FallbackColor);
        renderer.DrawGlintMarker(canvas, 48, 32, radius, SpaceMapColorResolver.AsteroidColor);

        // Sample within the halo band (radius * 1.5) rather than the core or the
        // outermost faint edge.
        SKColor unknownHalo = bitmap.GetPixel(16, 32 - (int)(radius * 1.5f));
        SKColor asteroidHalo = bitmap.GetPixel(48, 32 - (int)(radius * 1.5f));

        int totalChannelDifference =
            Math.Abs(unknownHalo.Red - asteroidHalo.Red) +
            Math.Abs(unknownHalo.Green - asteroidHalo.Green) +
            Math.Abs(unknownHalo.Blue - asteroidHalo.Blue);

        Assert.True(
            totalChannelDifference > 70,
            $"Expected the dark-navy UnknownSpaceObject halo and the near-white Asteroid halo to remain " +
            $"visually distinguishable; unknownHalo={unknownHalo}, asteroidHalo={asteroidHalo}, " +
            $"totalChannelDifference={totalChannelDifference}");
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
