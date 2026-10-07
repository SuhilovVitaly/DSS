using System.Text.Json;

namespace DeepSpaceSaga.Performance.Tests;

[Collection("Solar map evidence")]
public sealed class ClusterPerformanceReportTests
{
    private static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    [Fact]
    public void ClusterReportContainsBoundaryAndEconomyEvidence()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json"), economy = path + ".economy";
        try
        {
            File.WriteAllText(economy, """{"schemaVersion":1,"commit":"test-identity","status":"incomplete","matrix":{"shipConfigurations":[{"id":"starter"}]},"cases":[{"seed":1,"clusterScenario":"Default"}]}""");
            Assert.Equal(0, SolarMapEvidence.Run([Root, path, "--solar-map", "--clusters", "--seeds", "1:1", "--scenarios", "PlayerShipOnly", "--economy-report", economy]));
            using var json = JsonDocument.Parse(File.ReadAllText(path)); var r = json.RootElement;
            Assert.Equal(8, r.GetProperty("clusterCounts").GetArrayLength());
            Assert.Equal("test-identity", r.GetProperty("economyReportRef").GetProperty("commit").GetString());
            Assert.Equal("incomplete", r.GetProperty("economyReportRef").GetProperty("status").GetString());
            Assert.Equal("not-assessed", r.GetProperty("economyReportRef").GetProperty("balance").GetString());
            Assert.All(r.GetProperty("measurements").EnumerateArray(), m =>
            {
                var counts = m.GetProperty("clusterCounts"); Assert.True(counts.GetProperty("resourceAsteroids").GetInt32() > 0);
                Assert.Equal(counts.GetProperty("stations").GetInt32(), counts.GetProperty("markets").GetInt32());
                Assert.True(m.GetProperty("saveAllocationBytes").GetInt64() > 0);
                Assert.Equal("missing-scenario-seed-evidence", m.GetProperty("economyEvidence").GetString());
            });
        }
        finally { File.Delete(path); File.Delete(economy); }
    }

    [Fact]
    public void RasterDoesNotClaimGpuFps()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            Assert.Equal(0, SolarMapEvidence.Run([Root, path, "--solar-map", "--clusters", "--seeds", "1:1", "--scenarios", "PlayerShipOnly"]));
            using var json = JsonDocument.Parse(File.ReadAllText(path));
            Assert.Equal("not-measured", json.RootElement.GetProperty("presentation").GetProperty("status").GetString());
            Assert.Contains("missing-report", json.RootElement.GetProperty("economicAcceptance").GetString());
            Assert.Equal("CPU/Skia raster", json.RootElement.GetProperty("backend").GetString());
        }
        finally { File.Delete(path); }
    }
}
