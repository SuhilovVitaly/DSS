using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class TabletopCloudTests
{
    [Theory]
    [InlineData(1.4f, -.25f, 0, 0)]
    [InlineData(1.4f, -.25f, 64, 26)]
    [InlineData(1.4f, -.25f, 128, 115)]
    [InlineData(1.4f, -.25f, 255, 255)]
    [InlineData(1.6f, -.35f, 64, 13)]
    [InlineData(1.6f, -.35f, 128, 116)]
    [InlineData(.45f, -.1f, 64, 3)]
    [InlineData(.45f, -.1f, 128, 32)]
    [InlineData(.45f, -.1f, 255, 89)]
    public void Noise_alpha_matches_normalized_spec_including_bias(float gain, float bias, byte red, int alpha)
    {
        using var bitmap = new SKBitmap(1, 1); using var canvas = new SKCanvas(bitmap);
        using var filter = PaintedCloudLayer.NoiseAlpha(gain, bias);
        using var paint = new SKPaint { Color = new SKColor(red, 0, 0), ColorFilter = filter, BlendMode = SKBlendMode.Src };
        canvas.DrawPaint(paint);
        Assert.InRange(bitmap.GetPixel(0, 0).Alpha, Math.Max(0, alpha - 1), Math.Min(255, alpha + 1));
    }

    [Fact]
    public async Task Screen_owns_and_releases_cloud_cache_on_exit()
    {
        await using var scene = await Ep0019Scenes.Create("1-move-no-hover");
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var screen = new TacticalBattleScreen(scene.Screen.Flow, 1, catalog.GetOrClassic("tabletop"));
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        var field = typeof(TacticalBattleScreen).GetField("_paintedClouds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        try
        {
            screen.Render(canvas, 1280, 720);
            var clouds = Assert.IsType<PaintedCloudLayer>(field.GetValue(screen));
            screen.Render(canvas, 1280, 720);
            Assert.Equal(1, clouds.BuildCount);
            screen.OnDeactivated();
            Assert.Null(field.GetValue(screen));
            Assert.False(clouds.HasImage);
        }
        finally { screen.OnDeactivated(); }
    }

    [Fact]
    public async Task Cache_tracks_view_scale_and_clumps_and_disposes_its_image()
    {
        await using var scene = await Ep0019Scenes.Create("1-move-no-hover");
        var frame = scene.Frame with { View = new(320, 240, 0, 0, 1) };
        using var bitmap = new SKBitmap(640, 480);
        using var canvas = new SKCanvas(bitmap);
        using var clouds = new PaintedCloudLayer();
        clouds.Draw(canvas, frame);
        clouds.Draw(canvas, frame);
        Assert.Equal(1, clouds.BuildCount);
        frame = frame with { View = frame.View with { CenterX = 1 } };
        clouds.Draw(canvas, frame); Assert.Equal(2, clouds.BuildCount);
        canvas.Scale(2);
        clouds.Draw(canvas, frame); Assert.Equal(3, clouds.BuildCount);
        var anomaly = frame.Battle.Anomalies[0];
        var changed = anomaly with { Clumps = anomaly.Clumps.SetItem(0, anomaly.Clumps[0] with { Amplitude = anomaly.Clumps[0].Amplitude + 1 }) };
        frame = frame with { Battle = frame.Battle with { Anomalies = frame.Battle.Anomalies.SetItem(0, changed) } };
        clouds.Draw(canvas, frame); Assert.Equal(4, clouds.BuildCount);
        clouds.Draw(canvas, frame); Assert.Equal(4, clouds.BuildCount);
        Assert.True(clouds.HasImage);
        clouds.Dispose(); Assert.False(clouds.HasImage);
        clouds.Draw(canvas, frame); Assert.Equal(5, clouds.BuildCount);
        clouds.Draw(canvas, frame with { Battle = frame.Battle with { Anomalies = [] } });
        Assert.False(clouds.HasImage);
    }

    [Fact]
    public async Task Raster_rebuild_timing_is_recorded_separately_from_native_gpu_acceptance()
    {
        await using var scene = await Ep0019Scenes.Create("2-anomaly-absorption");
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        canvas.Scale(1.5f);
        using var clouds = new PaintedCloudLayer();
        clouds.Draw(canvas, scene.Frame);
        var values = new List<double>();
        for (int i = 1; i <= 3; i++)
        {
            var frame = scene.Frame with { View = scene.Frame.View with { CenterX = scene.Frame.View.CenterX + i } };
            var watch = System.Diagnostics.Stopwatch.StartNew();
            clouds.Draw(canvas, frame);
            values.Add(watch.Elapsed.TotalMilliseconds);
        }
        Assert.Equal(4, clouds.BuildCount);
        var folder = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT");
        if (!string.IsNullOrEmpty(folder)) File.WriteAllText(Path.Combine(folder, "cloud-raster-timing.json"),
            System.Text.Json.JsonSerializer.Serialize(new { backend = "CPU raster", width = 1920, height = 1080, samplesMs = values, budgetMs = 12, nativeGpu = "NOT RUN" }));
    }

    [Theory]
    [InlineData("2-anomaly-absorption")]
    [InlineData("1-anomaly-move-dust")]
    public async Task Actual_scenes_use_painted_clouds_without_starting_legacy_refinement(string name)
    {
        await using var scene = await Ep0019Scenes.Create(name);
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        using var scope = TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic("tabletop"));
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        using var legacy = new InteractiveAnomalyField();
        canvas.Scale(1.5f);
        TacticalBattleMapRenderer.Draw(canvas, scene.Frame, legacy);
        if (name == "2-anomaly-absorption")
        {
            // The global strongest clump can lie beyond this battle camera. Probe the
            // strongest visible clump, as required by UiSpec's actual-scene calibration.
            var clump = scene.Frame.Battle.Anomalies.Where(a => a.Kind == TacticalAnomalyKindCode.Heat)
                .SelectMany(a => a.Clumps).Where(c => new SKRect(20, 20, 1260, 700).Contains(scene.Frame.View.Project(c.X, c.Y)))
                .MaxBy(c => c.Amplitude)!;
            var point = scene.Frame.View.Project(clump.X, clump.Y);
            var color = bitmap.GetPixel((int)(point.X * 1.5f), (int)(point.Y * 1.5f));
            static double Distance(SKColor a, SKColor b) => Math.Pow(a.Red - b.Red, 2) + Math.Pow(a.Green - b.Green, 2) + Math.Pow(a.Blue - b.Blue, 2);
            Assert.True(Distance(color, SKColor.Parse("#D0684A")) < Distance(color, SKColor.Parse("#141C28")), color.ToString());
        }
        Assert.Null(typeof(InteractiveAnomalyField).GetField("_pending", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(legacy));
        Assert.Null(typeof(InteractiveAnomalyField).GetField("_displayed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(legacy));
        var folder = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT");
        if (!string.IsNullOrEmpty(folder))
        {
            using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            using var output = File.Create(Path.Combine(folder, "cloud-" + name + ".png"));
            png.SaveTo(output);
        }
    }

    [Theory]
    [InlineData(TacticalAnomalyKindCode.Dust)]
    [InlineData(TacticalAnomalyKindCode.Heat)]
    public async Task Cloud_center_has_expected_tone_and_transparent_surroundings(TacticalAnomalyKindCode kind)
    {
        await using var scene = await Ep0019Scenes.Create("1-move-no-hover");
        var frame = scene.Frame;
        var anomaly = frame.Battle.Anomalies.First(a => a.Kind == kind);
        var clump = anomaly.Clumps.OrderByDescending(c => c.Amplitude).First();
        frame = frame with
        {
            Battle = frame.Battle with { Anomalies = [anomaly with { Clumps = [clump] }] },
            View = new(320, 240, clump.X, clump.Y, 2)
        };
        using var bitmap = new SKBitmap(320, 240);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        using var clouds = new PaintedCloudLayer();
        clouds.Draw(canvas, frame);
        var color = bitmap.GetPixel(160, 120);
        Assert.True(color.Alpha > 20, color.ToString());
        if (kind == TacticalAnomalyKindCode.Dust) Assert.True(color.Blue > color.Red && color.Red > color.Green, color.ToString());
        else Assert.True(color.Red > color.Green && color.Green > color.Blue, color.ToString());
        Assert.True(bitmap.GetPixel(0, 0).Alpha < color.Alpha);
        Assert.True(bitmap.Pixels.Distinct().Count() > 100);
        var first = bitmap.Bytes;
        canvas.Clear(SKColors.Transparent);
        clouds.Draw(canvas, frame);
        Assert.Equal(first, bitmap.Bytes);
    }
}
