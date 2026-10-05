using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Client.UI;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PresentedFrameEvidenceTests
{
    [Theory]
    [InlineData(false, 144)]
    [InlineData(false, 60)]
    [InlineData(true, 144)]
    public void FrameEvidenceAggregatesIntervals(bool dropped, int refresh)
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var evidence = new MapFrameEvidence(path);
            var context = new MapFrameContext(1920, 1080, true, refresh, "test GPU", "test GL", 1, "Speed1", 42, 1, 7, 5, 200, .001, "station");
            long ticks = Stopwatch.Frequency;
            for (int i = 0; i < 720; i++)
            {
                ticks += (long)(Stopwatch.Frequency * (dropped && i >= 708 ? .1 : .0125));
                evidence.Record(ticks, 2, context);
            }
            Assert.True(evidence.Completed);
            using var json = JsonDocument.Parse(File.ReadAllText(path));
            Assert.Equal(600, json.RootElement.GetProperty("measuredFrames").GetInt32());
            var stats = json.RootElement.GetProperty("frameIntervals");
            Assert.Equal(dropped ? 100 : 12.5, stats.GetProperty("P99Ms").GetDouble(), 4);
            if (!dropped) Assert.Equal(80, stats.GetProperty("MeanFps").GetDouble(), 4);
            Assert.Equal(refresh < 80 ? "display-limited" : dropped ? "failed" : "passed", json.RootElement.GetProperty("targetVerdict").GetString());
            Assert.Equal(2, json.RootElement.GetProperty("cpuSubmit").GetProperty("MeanMs").GetDouble());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void DisabledDiagnosticsHasNoOutput()
    {
        Assert.Null(MapFrameEvidence.Create(null));
        Assert.Null(MapFrameEvidence.Create(""));
        // Disabled construction has no path, collector or writer to schedule.
    }
}
