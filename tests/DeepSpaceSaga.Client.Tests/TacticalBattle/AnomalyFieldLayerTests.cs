using System.Collections.Immutable;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class AnomalyFieldLayerTests
{
    // Independent Python scalar formula, actual seed1 clumps. Six strongest centers,
    // four rotated one-sigma points each and two background points:32, exceeding AC's20.
    private static readonly (int X, int Y, byte R, byte G, byte B)[] Reference =
    [
        (828, 368, 90, 43, 54),
        (822, 382, 218, 80, 107),
        (834, 354, 94, 44, 56),
        (817, 363, 210, 78, 103),
        (839, 372, 88, 42, 53),
        (788, 517, 202, 76, 100),
        (806, 533, 93, 44, 55),
        (770, 502, 255, 105, 141),
        (778, 530, 67, 36, 45),
        (799, 505, 78, 39, 49),
        (828, 416, 68, 36, 45),
        (829, 435, 80, 39, 50),
        (826, 398, 76, 39, 49),
        (817, 417, 102, 46, 59),
        (839, 415, 68, 36, 45),
        (447, 617, 169, 121, 230),
        (460, 639, 74, 59, 102),
        (435, 594, 193, 137, 255),
        (437, 622, 99, 75, 136),
        (458, 611, 74, 59, 102),
        (532, 482, 74, 59, 102),
        (513, 502, 74, 59, 102),
        (550, 463, 74, 59, 102),
        (522, 473, 154, 111, 210),
        (542, 492, 74, 59, 102),
        (511, 603, 85, 66, 117),
        (503, 617, 210, 148, 255),
        (519, 590, 74, 59, 102),
        (495, 594, 188, 134, 255),
        (526, 612, 74, 59, 102),
        (3, 3, 16, 21, 24),
        (1916, 1076, 16, 21, 24)
    ];
    private static readonly TacticalBattleView Camera = new(1280, 720, 700, 200, 1);
    private static readonly SKSizeI Size = new(1920, 1080);

    [Fact]
    public async Task Control_pixels_match_formula()
    {
        await using var scene = await Ep0019Scenes.Create("1-move");
        var battle = scene.Screen.Flow.State!;
        using var layer = new AnomalyFieldLayer();
        var image = layer.Render(battle.Anomalies, battle.Rules.Anomalies, Camera, Size, 1.5f)!;
        using var bitmap = new SKBitmap(Size.Width, Size.Height);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(BattleDesign.MapBg);
            using var paint = new SKPaint { BlendMode = SKBlendMode.Screen };
            canvas.DrawImage(image, 0, 0, paint);
        }
        foreach (var p in Reference)
        {
            var actual = bitmap.GetPixel(p.X, p.Y);
            Assert.InRange(Math.Abs(actual.Red - p.R), 0, 6);
            Assert.InRange(Math.Abs(actual.Green - p.G), 0, 6);
            Assert.InRange(Math.Abs(actual.Blue - p.B), 0, 6);
        }
        Assert.Equal(32, Reference.Length);
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { Length: > 0 } root)
        {
            using var capture = SKImage.FromBitmap(bitmap); using var data = capture.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(Path.Combine(root, "anomaly-layer.png"), data.ToArray());
        }
    }

    [Fact]
    public async Task Field_is_screen_blended_again_inside_selected_sector()
    {
        await using var scene = await Ep0019Scenes.Create("1-move"); var frame = scene.Screen.CaptureFrame(1920, 1080)!;
        var strongest = frame.Battle.Anomalies.Where(a => a.Kind == TacticalAnomalyKindCode.Heat)
            .SelectMany(a => a.Clumps).OrderByDescending(c => c.Amplitude).First();
        var own = frame.Battle.Units.First(u => u.Side == frame.Battle.PlayerSide && u.Kind == TacticalUnitKind.Drone);
        own = own with { Pose = new(strongest.X - 50, strongest.Y, 0), Build = own.Build! with { Range = 200, ArcDegrees = 90 } };
        var battle = frame.Battle with { Units = [], Paths = [], Phase = TacticalBattlePhase.Fire };
        frame = frame with
        {
            Battle = battle,
            View = Camera,
            Movement = null,
            FirePlan = new([new(own, new(own.Id, [], null), null)], own.Id),
            SelectedDroneId = own.Id,
            HoverUnitId = null
        };
        int x = Reference[0].X, y = Reference[0].Y;
        // Keep the sample away from26-unit grid strokes and the sector edge.
        while (x / 1.5 % 26 < 2 || x / 1.5 % 26 > 24) x++;
        while (y / 1.5 % 26 < 2 || y / 1.5 % 26 > 24) y++;
        using var layer = new AnomalyFieldLayer();
        using var field = SKBitmap.FromImage(layer.Render(battle.Anomalies, battle.Rules.Anomalies, Camera, Size, 1.5f)!);
        using var expected = new SKBitmap(1, 1);
        using (var canvas = new SKCanvas(expected))
        {
            canvas.Clear(BattleDesign.MapBg); canvas.DrawColor(field.GetPixel(x, y), SKBlendMode.Screen);
            canvas.DrawColor(BattleDesign.ArcFill, SKBlendMode.SrcOver); canvas.DrawColor(field.GetPixel(x, y), SKBlendMode.Screen);
        }
        using var actual = new SKBitmap(Size.Width, Size.Height);
        using (var canvas = new SKCanvas(actual)) { canvas.Scale(1.5f); TacticalBattleMapRenderer.Draw(canvas, frame); }
        var a = actual.GetPixel(x, y); var e = expected.GetPixel(0, 0);
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { Length: > 0 } root)
        { using var capture = SKImage.FromBitmap(actual); using var data = capture.Encode(SKEncodedImageFormat.Png, 100); File.WriteAllBytes(Path.Combine(root, "sector-layer.png"), data.ToArray()); }
        Assert.True(Math.Abs(a.Red - e.Red) <= 1, $"pixel{x},{y} actual{a} expected{e} field{field.GetPixel(x, y)} center{strongest.X},{strongest.Y} selected{frame.FirePlan.Selected?.Drone.Pose}");
        Assert.InRange(Math.Abs(a.Red - e.Red), 0, 1); Assert.InRange(Math.Abs(a.Green - e.Green), 0, 1); Assert.InRange(Math.Abs(a.Blue - e.Blue), 0, 1);
    }

    [Fact]
    public async Task Texture_is_world_anchored()
    {
        await using var scene = await Ep0019Scenes.Create("1-move"); var battle = scene.Screen.Flow.State!;
        using var layer = new AnomalyFieldLayer();
        using var before = SKBitmap.FromImage(layer.Render(battle.Anomalies, battle.Rules.Anomalies, Camera, Size, 1.5f)!);
        using var after = SKBitmap.FromImage(layer.Render(battle.Anomalies, battle.Rules.Anomalies, Camera with { CenterX = 704 }, Size, 1.5f)!);
        foreach (var p in Reference.Where(p => p.X >= 6)) Assert.Equal(before.GetPixel(p.X, p.Y), after.GetPixel(p.X - 6, p.Y));
        Assert.Equal(2, layer.BuildCount);
    }

    [Fact]
    public async Task Layer_is_cached_per_camera()
    {
        await using var scene = await Ep0019Scenes.Create("1-move"); var battle = scene.Screen.Flow.State!;
        using var layer = new AnomalyFieldLayer(); var view = new TacticalBattleView(320, 180, 700, 200, 1); var size = new SKSizeI(320, 180);
        var first = layer.Render(battle.Anomalies, battle.Rules.Anomalies, view, size, 1);
        var copy = battle.Anomalies.Select(a => a with { Clumps = a.Clumps.Select(c => c with { }).ToImmutableArray() }).ToImmutableArray();
        Assert.Same(first, layer.Render(copy, battle.Rules.Anomalies! with { }, view, size, 1)); Assert.Equal(1, layer.BuildCount);
        Assert.NotSame(first, layer.Render(copy, battle.Rules.Anomalies, view with { CenterX = 701 }, size, 1)); Assert.Equal(2, layer.BuildCount);
        layer.Render(copy, battle.Rules.Anomalies, view with { PixelWidth = 321 }, new(321, 180), 1); Assert.Equal(3, layer.BuildCount);
        layer.Render(copy, battle.Rules.Anomalies, view, new(640, 360), 2); Assert.Equal(4, layer.BuildCount);
        var changed = copy.SetItem(0, copy[0] with { Clumps = copy[0].Clumps.SetItem(0, copy[0].Clumps[0] with { Amplitude = 1 }) });
        layer.Render(changed, battle.Rules.Anomalies, view, new(640, 360), 2); Assert.Equal(5, layer.BuildCount);
    }

    [Fact]
    public async Task No_anomalies_leaves_map_unchanged()
    {
        await using var scene = await Ep0019Scenes.Create("1-move"); var frame = scene.Screen.CaptureFrame(1920, 1080)!;
        using var layer = new AnomalyFieldLayer();
        layer.Render(frame.Battle.Anomalies, frame.Battle.Rules.Anomalies, Camera, new(320, 180), 1);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap); canvas.Clear(new SKColor(37, 51, 61)); var before = bitmap.Bytes;
        var empty = frame with { Battle = frame.Battle with { Anomalies = [] } };
        layer.Draw(canvas, empty); Assert.Equal(before, bitmap.Bytes);
        TacticalBattleMapRenderer.Draw(canvas, empty); var withRules = bitmap.Bytes;
        canvas.Clear(new SKColor(37, 51, 61));
        TacticalBattleMapRenderer.Draw(canvas, empty with { Battle = empty.Battle with { Rules = empty.Battle.Rules with { Anomalies = null } } });
        Assert.Equal(withRules, bitmap.Bytes);
    }
}
