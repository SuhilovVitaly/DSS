using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class TabletopCloudTests
{
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
        new PaintedCloudLayer().Draw(canvas, frame);
        var color = bitmap.GetPixel(160, 120);
        Assert.True(color.Alpha > 20, color.ToString());
        if (kind == TacticalAnomalyKindCode.Dust) Assert.True(color.Blue > color.Red && color.Red > color.Green, color.ToString());
        else Assert.True(color.Red > color.Green && color.Green > color.Blue, color.ToString());
        Assert.True(bitmap.GetPixel(0, 0).Alpha < color.Alpha);
        Assert.True(bitmap.Pixels.Distinct().Count() > 100);
        var first = bitmap.Bytes;
        canvas.Clear(SKColors.Transparent);
        new PaintedCloudLayer().Draw(canvas, frame);
        Assert.Equal(first, bitmap.Bytes);
    }
}
