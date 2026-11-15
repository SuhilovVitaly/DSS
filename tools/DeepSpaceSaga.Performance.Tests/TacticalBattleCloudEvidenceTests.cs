using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Performance.Tests;

public sealed class TacticalBattleCloudEvidenceTests
{
    [Fact]
    public void BattleWithoutAnomalyClumpsWritesSkippedReport()
    {
        using var report = Measure(Frame(), size => SKSurface.Create(size));
        var entry = Assert.Single(report.RootElement.EnumerateArray());
        Assert.Equal("skipped", entry.GetProperty("status").GetString());
        Assert.Equal("no-anomaly-clumps", entry.GetProperty("reason").GetString());
        Assert.Equal(0, entry.GetProperty("clumps").GetInt32());
    }

    [Fact]
    public void SingleZeroSigmaClumpKeepsAllCloudsZoomFinite()
    {
        using var report = Measure(Frame(new TacticalClumpSnapshot(10, 20, 100, 0, 0, 0)), size => SKSurface.Create(size));
        var scenes = report.RootElement.EnumerateArray().ToArray();
        Assert.Equal(["movement", "cloud-center", "all-clouds"], scenes.Select(s => s.GetProperty("scene").GetString()));
        Assert.All(scenes, s => Assert.Equal("measured", s.GetProperty("status").GetString()));
        double zoom = scenes[2].GetProperty("View").GetProperty("PixelsPerUnit").GetDouble();
        Assert.True(double.IsFinite(zoom) && zoom > 0, $"all-clouds zoom {zoom} must be finite and positive.");
        Assert.Equal(Math.Min(320 / TacticalBattleCloudEvidence.MinimumSpan, 180 / TacticalBattleCloudEvidence.MinimumSpan), zoom);
    }

    [Fact]
    public void UnavailableSurfaceWritesSkippedScenes()
    {
        int calls = 0;
        using var report = Measure(Frame(new TacticalClumpSnapshot(0, 0, 100, 5, 3, 0)), _ => { calls++; return null; });
        var scenes = report.RootElement.EnumerateArray().ToArray();
        Assert.Equal(3, scenes.Length);
        Assert.Equal(3, calls);
        Assert.All(scenes, s =>
        {
            Assert.Equal("skipped", s.GetProperty("status").GetString());
            Assert.Equal("surface-unavailable", s.GetProperty("reason").GetString());
        });
    }

    private static JsonDocument Measure(TacticalBattleFrame frame, Func<SKImageInfo, SKSurface?> createSurface)
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "-clouds.json");
        try
        {
            TacticalBattleCloudEvidence.Measure(frame, path, null, createSurface);
            return JsonDocument.Parse(File.ReadAllText(path));
        }
        finally { File.Delete(path); }
    }

    private static TacticalBattleFrame Frame(params TacticalClumpSnapshot[] clumps)
    {
        var battle = new TacticalBattleSnapshot("cloud-evidence", 1, TacticalBattlePhase.Move, 1, "A", 1, "initial", "Initial",
            [], [], [], [], [], [], [], [], false, new TacticalForecastRules(new TacticalHitRulesSnapshot(0, 0, 0, 0, 0, 0, 0), 0, 0, 1),
            [], null, [])
        {
            Anomalies = clumps.Length == 0 ? [] : [new TacticalAnomalySnapshot("dust", TacticalAnomalyKindCode.Dust, [.. clumps])]
        };
        return new TacticalBattleFrame(battle, new TacticalBattleView(320, 180, 0, 0, 1), SKRect.Empty, 1, false, "");
    }
}
