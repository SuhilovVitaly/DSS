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
            totalChannelDifference > 0,
            $"Expected the gray-lilac UnknownSpaceObject halo and the cold-gray Asteroid halo to remain " +
            $"visually distinguishable; unknownHalo={unknownHalo}, asteroidHalo={asteroidHalo}, " +
            $"totalChannelDifference={totalChannelDifference}");
    }

    [Fact]
    public void Forecast_is_dashed_blue_and_navigation_solid_blue()
    {
        var renderer = new TacticalMapDepthRenderer();
        var camera = new CameraState(0, 0, 1);
        FutureTrajectoryPoint[] points = [new(-20, 0), new(20, 0)];
        using var future = new SKBitmap(64, 64);
        using var navigation = new SKBitmap(64, 64);
        using var fc = new SKCanvas(future);
        using var nc = new SKCanvas(navigation);
        fc.Clear(SKColors.Transparent); nc.Clear(SKColors.Transparent);
        renderer.DrawFutureTrajectory(fc, points, camera, 64, 64);
        renderer.DrawNavigationTrajectory(nc, points, camera, 64, 64);
        Assert.Contains(Enumerable.Range(14, 35), x => future.GetPixel(x, 32).Alpha == 0);
        Assert.All(Enumerable.Range(14, 35), x => Assert.True(navigation.GetPixel(x, 32).Alpha > 0));
        var pixel = navigation.GetPixel(32, 32);
        Assert.True(pixel.Blue > pixel.Green && pixel.Green > pixel.Red);
        Assert.InRange(pixel.Alpha, 210, 217);
    }

    [Fact]
    public void Engine_flame_draws_visible_pixels_behind_the_ship()
    {
        var renderer = new TacticalMapDepthRenderer();

        using var bitmap = new SKBitmap(CanvasSize, CanvasSize);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);

        // Ship faces "up" on screen (direction 0 degrees => dx=0, dy=-1), so
        // the flame trails behind it, toward +y (downward on screen).
        renderer.DrawEngineFlame(canvas, 32, 32, directionDegrees: 0, radius: 10, uiTimeMs: 0);

        SKColor behind = bitmap.GetPixel(32, 32 + 8);

        Assert.True(
            Luminance(behind) > 0,
            $"Expected visible flame pixels behind the ship; behind={behind}");
    }

    [Fact]
    public void Engine_flame_is_directional_and_does_not_paint_ahead_of_the_ship()
    {
        var renderer = new TacticalMapDepthRenderer();

        using var bitmap = new SKBitmap(CanvasSize, CanvasSize);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);

        renderer.DrawEngineFlame(canvas, 32, 32, directionDegrees: 0, radius: 10, uiTimeMs: 0);

        SKColor ahead = bitmap.GetPixel(32, 32 - 8);

        Assert.Equal(SKColors.Black, ahead);
    }

    [Fact]
    public void Engine_flame_flicker_animates_with_uiTimeMs()
    {
        var renderer = new TacticalMapDepthRenderer();

        // uiTimeMs=45 lands at a quarter of the 180ms period (flicker at its
        // maximum, 1.0); uiTimeMs=135 lands at three-quarters (flicker at its
        // minimum, 0.6) — the two extremes of the sine-driven pulse.
        using var bitmapA = new SKBitmap(CanvasSize, CanvasSize);
        using var canvasA = new SKCanvas(bitmapA);
        canvasA.Clear(SKColors.Black);
        renderer.DrawEngineFlame(canvasA, 32, 32, directionDegrees: 0, radius: 10, uiTimeMs: 45);

        using var bitmapB = new SKBitmap(CanvasSize, CanvasSize);
        using var canvasB = new SKCanvas(bitmapB);
        canvasB.Clear(SKColors.Black);
        renderer.DrawEngineFlame(canvasB, 32, 32, directionDegrees: 0, radius: 10, uiTimeMs: 135);

        // At radius=10, the outer layer's tip sits at ~29px behind center at peak
        // flicker (1.0) and ~20px at the trough (0.6). Sample at 24px — inside the
        // flame at peak, beyond it at the trough — so only the longer extreme
        // reaches this point, making the flicker-driven length change unambiguous.
        SKColor tipA = bitmapA.GetPixel(32, 32 + 24);
        SKColor tipB = bitmapB.GetPixel(32, 32 + 24);

        Assert.NotEqual(Luminance(tipA), Luminance(tipB));
    }

    [Fact]
    public void Focus_indicator_draws_half_size_cross_in_existing_color()
    {
        var renderer = new TacticalMapDepthRenderer();
        using var bitmap = new SKBitmap(128, 128);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        renderer.DrawFocusIndicator(canvas, 64.5f, 64.5f);

        Assert.InRange(bitmap.GetPixel(64, 64).Alpha, 100, 104);
        foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
        {
            var arm = bitmap.GetPixel(64 + dx * 12, 64 + dy * 12);
            Assert.InRange(arm.Red, 149, 153);
            Assert.InRange(arm.Green, 166, 170);
            Assert.InRange(arm.Blue, 182, 186);
            Assert.InRange(arm.Alpha, 100, 104);
            Assert.NotEqual(0, bitmap.GetPixel(64 + dx * 22, 64 + dy * 22).Alpha);
            Assert.Equal(0, bitmap.GetPixel(64 + dx * 23, 64 + dy * 23).Alpha);
        }

        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (Math.Abs(x - 64) > 1 && Math.Abs(y - 64) > 1)
                    Assert.Equal(0, bitmap.GetPixel(x, y).Alpha);
            }
        }
    }

    private static int Luminance(SKColor color)
    {
        return color.Red + color.Green + color.Blue;
    }

    [Fact]
    public void Target_forecast_keeps_visible_dashes_gaps_and_a_bounded_soft_edge()
    {
        using var bitmap = new SKBitmap(200, 80);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        new TacticalMapDepthRenderer().DrawTargetTrajectory(canvas,
            [new(-80, 0), new(80, 0)], new CameraState(0, 0, 1), 200, 80);

        byte[] center = Enumerable.Range(30, 140).Select(x => bitmap.GetPixel(x, 40).Alpha).ToArray();
        Assert.Contains(center, alpha => alpha >= 50);
        Assert.Contains(center, alpha => alpha == 0);
        Assert.All(Enumerable.Range(0, 200), x => Assert.Equal(0, bitmap.GetPixel(x, 44).Alpha));
    }
}
