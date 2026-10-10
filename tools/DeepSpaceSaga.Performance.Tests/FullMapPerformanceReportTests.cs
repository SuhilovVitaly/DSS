using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Performance.Tests;

[Collection("Solar map evidence")]
public sealed class FullMapPerformanceReportTests
{
    private static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    [Fact]
    public void FullMapReportRequiresLayerAndBackendContext()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            Assert.Equal(1, SolarMapEvidence.Run([Root, path, "--solar-map", "--all-map-layers"]));
            Assert.Equal(0, SolarMapEvidence.Run([Root, path, "--solar-map", "--clusters", "--all-map-layers", "--seeds", "1:1", "--scenarios", "PlayerShipOnly", "--config", "max"]));
            using var json = JsonDocument.Parse(File.ReadAllText(path)); var report = json.RootElement;
            Assert.True(report.GetProperty("allMapLayers").GetBoolean());
            Assert.All(report.GetProperty("layerContent").EnumerateObject(), p => Assert.True(p.Value.GetBoolean()));
            Assert.Equal("CPU/Skia raster", report.GetProperty("backend").GetString());
            Assert.Contains("not-applicable", report.GetProperty("vsync").GetString());
            var row = Assert.Single(report.GetProperty("measurements").EnumerateArray());
            var counts = row.GetProperty("mapCounts");
            Assert.Equal(row.GetProperty("objects").GetInt32(), counts.GetProperty("authoritativeEntities").GetInt32());
            Assert.Equal(4, counts.GetProperty("aiBases").GetInt32()); Assert.Equal(4, counts.GetProperty("territories").GetInt32());
            Assert.Equal(5, counts.GetProperty("fields").GetInt32()); Assert.Equal(2, counts.GetProperty("pointsOfInterest").GetInt32());
            Assert.Equal(0, row.GetProperty("epoch").GetProperty("motionTimeMs").GetInt64());
            Assert.True(row.GetProperty("placement").GetProperty("checkCount").GetInt32() > 0);
            Assert.All(report.GetProperty("rendering").EnumerateArray(), r => Assert.Equal("All", r.GetProperty("layers").GetString()));
            Assert.Equal("not-measured", report.GetProperty("criteria").GetProperty("gpuExecution").GetString());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void MissingGpuOrBaselineNotMarkedPassed()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            Assert.Equal(0, SolarMapEvidence.Run([Root, path, "--solar-map", "--clusters", "--boundary-only", "--seeds", "1:1", "--scenarios", "PlayerShipOnly", "--config", "min"]));
            using var json = JsonDocument.Parse(File.ReadAllText(path)); var report = json.RootElement;
            Assert.Equal("not-measured", report.GetProperty("presentation").GetProperty("status").GetString());
            Assert.Equal("not-measured", report.GetProperty("criteria").GetProperty("nativePresentation").GetString());
            Assert.Contains("missing fresh baseline", report.GetProperty("criteria").GetProperty("improvement").GetString());
            Assert.Equal(JsonValueKind.Null, report.GetProperty("clientFrameReportRef").ValueKind);
            var counts = Assert.Single(report.GetProperty("measurements").EnumerateArray()).GetProperty("mapCounts");
            foreach (string key in new[] { "aiBases", "fields", "pointsOfInterest", "territories" }) Assert.Equal(0, counts.GetProperty(key).GetInt32());
            Assert.Single(report.GetProperty("clusterCounts").EnumerateArray());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void DecorationCountsSeparateFromEntities()
    {
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [new("ship", 0, 0, 0, 0)],
            SolarSystemMap: new(1, 42, 10000, [new("belt", 100, 200, 1, 2048)], [], []),
            AiMap: new(1, [], [], [new("debris", "Debris", .5, "Sun", null, null, 0, 0, 0, 100, 0, 360, 1)], []));
        foreach (int samples in new[] { 0, 2048, 65536 })
        {
            var changed = snapshot with { SolarSystemMap = snapshot.SolarSystemMap! with { Belts = [snapshot.SolarSystemMap!.Belts[0] with { DecorationSamples = samples }] } };
            using var json = JsonDocument.Parse(JsonSerializer.Serialize(SolarMapEvidence.Counts(changed))); var c = json.RootElement;
            Assert.Equal(1, c.GetProperty("authoritativeEntities").GetInt32()); Assert.Equal(samples, c.GetProperty("configuredBeltDecorationSamples").GetInt32());
            Assert.Equal(64, c.GetProperty("configuredDebrisDecorationSamples").GetInt32()); Assert.Equal(1, c.GetProperty("fields").GetInt32());
        }
    }
}
