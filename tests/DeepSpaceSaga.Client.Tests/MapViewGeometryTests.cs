using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;
using Xunit.Abstractions;

namespace DeepSpaceSaga.Client.Tests;

public class MapViewGeometryTests(ITestOutputHelper output)
{
    [Fact]
    public void Repeated_layout_reuses_free_viewport_including_empty()
    {
        using var screen = new GameSessionScreen(new SnapshotBuffer(), new LinearMotionPredictor());
        SKRect[] obstacles = [new(0, 0, 100, 100)];
        Assert.Equal(SKRect.Empty, screen.ResolveFreeViewport(100, 100, obstacles));
        long builds = screen.FreeViewportBuilds;
        for (int i = 0; i < 20; i++) Assert.Equal(SKRect.Empty, screen.ResolveFreeViewport(100, 100, obstacles.ToArray()));
        Assert.Equal(builds, screen.FreeViewportBuilds);
        Assert.False(screen.ResolveFreeViewport(200, 100, obstacles).IsEmpty);
        Assert.Equal(builds + 1, screen.FreeViewportBuilds);
        obstacles[0] = new(0, 0, 200, 100);
        Assert.Equal(SKRect.Empty, screen.ResolveFreeViewport(200, 100, obstacles));
        Assert.Equal(builds + 2, screen.FreeViewportBuilds);
    }

    [Fact]
    public void Fully_occluded_labels_do_not_fall_back_under_panels()
    {
        using var renderer = new ObjectLabelRenderer();
        var ship = new ObjectMotionSnapshot("player", 0, 0, 0, 0);
        renderer.ComputeGeometries([new(ship, ship, true)], 0, 100, 100, new(0, 0, 1), availableMap: SKRect.Empty);
        Assert.Empty(renderer.Geometries);
    }

    [Fact]
    public void Fully_occluded_viewport_is_empty() =>
        Assert.Equal(SKRect.Empty, MapViewGeometry.FreeViewport(100, 100, [new(0, 0, 100, 100)]));

    [Fact]
    public void Free_viewport_matches_exhaustive_oracle()
    {
        var random = new Random(51215);
        for (int fixture = 0; fixture < 200; fixture++)
        {
            var obstacles = Enumerable.Range(0, fixture % 5).Select(_ =>
            {
                int x = random.Next(-2, 12), y = random.Next(-2, 10);
                return new SKRect(x, y, x + random.Next(1, 8), y + random.Next(1, 7));
            }).ToArray();
            Assert.Equal(Oracle(12, 10, obstacles), MapViewGeometry.FreeViewport(12, 10, obstacles));
            if (fixture % 10 == 0)
            {
                var expected = Oracle(12, 10, obstacles);
                var scaled = obstacles.AsEnumerable().Reverse().Select(r => new SKRect(r.Left * 1.5f, r.Top * 1.5f, r.Right * 1.5f, r.Bottom * 1.5f)).ToArray();
                Assert.Equal(new SKRect(expected.Left * 1.5f, expected.Top * 1.5f, expected.Right * 1.5f, expected.Bottom * 1.5f),
                    MapViewGeometry.FreeViewport(18, 15, scaled));
            }
        }
    }

    [Fact]
    public void Empty_free_viewport_does_not_move_fit_camera()
    {
        var camera = new CameraState(12, 34, .5);
        MapWorldBounds bounds = new();
        bounds.Include(1000, 2000);
        MapViewGeometry.Fit(camera, bounds, SKRect.Empty, 100, 100, new TacticalMapSettings());
        Assert.Equal((12d, 34d, .5), (camera.FocusX, camera.FocusY, camera.PixelsPerWorldUnit));
        Assert.Equal(SKRect.Empty, MapViewGeometry.FreeViewport(0, 100, []));
        Assert.Equal(SKRect.Empty, MapViewGeometry.FreeViewport(-1, 100, []));
    }

    private static SKRect Oracle(int width, int height, SKRect[] obstacles)
    {
        // Independent integer-grid exhaustive search, not the production edge algorithm.
        var candidates = new List<SKRect>();
        for (int top = 0; top < height; top++)
            for (int left = 0; left < width; left++)
                for (int bottom = top + 1; bottom <= height; bottom++)
                    for (int right = left + 1; right <= width; right++)
                    {
                        var r = new SKRect(left, top, right, bottom);
                        if (!obstacles.Any(o => r.IntersectsWith(o))) candidates.Add(r);
                    }
        return candidates.OrderByDescending(r => r.Width * r.Height).ThenBy(r => r.Top).ThenBy(r => r.Left)
            .ThenBy(r => r.Bottom).ThenBy(r => r.Right).FirstOrDefault();
    }

    [Fact]
    public void Free_viewport_cost_probe()
    {
        var rows = new List<object>();
        foreach (int count in new[] { 0, 4, 8, 16, 32 })
        {
            var random = new Random(51215 + count);
            var obstacles = Enumerable.Range(0, count).Select(_ =>
            {
                int x = random.Next(1900), y = random.Next(1060);
                return new SKRect(x, y, x + random.Next(10, 150), y + random.Next(10, 150));
            }).ToArray();
            for (int i = 0; i < 3; i++) MapViewGeometry.FreeViewport(1920, 1080, obstacles);
            var times = new double[101];
            for (int i = 0; i < times.Length; i++)
            {
                long start = Stopwatch.GetTimestamp();
                MapViewGeometry.FreeViewport(1920 + i % 2, 1080 + i % 3, obstacles);
                times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            }
            Array.Sort(times);
            MapViewGeometry.FreeViewport(1920, 1080, obstacles, out int candidates, out int obstacleChecks);
            rows.Add(new
            {
                obstacles = count,
                samples = times.Length,
                candidates,
                obstacleChecks,
                p50_ms = times[50],
                p95_ms = times[95],
                p99_ms = times[99]
            });
        }
        string json = JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
        output.WriteLine(json);
        if (Environment.GetEnvironmentVariable("DSS_VIEWPORT_BENCHMARK_REPORT") is { Length: > 0 } path)
            File.WriteAllText(path, json);
    }
}
