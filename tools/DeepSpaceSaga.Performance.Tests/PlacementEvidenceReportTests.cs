using System.Diagnostics;
using System.Text.Json;

namespace DeepSpaceSaga.Performance.Tests;

[Collection("Solar map evidence")]
public sealed class PlacementEvidenceReportTests
{
    private static string Root => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

    [Fact]
    public void PlacementReportIncludesCriticalEpochs()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            string? previous = null;
            for (int i = 0; i < 2; i++)
            {
                Assert.Equal(0, SolarMapEvidence.Run([Root, path, "--solar-map", "--clusters", "--ai-placement", "--seeds", "42:42", "--scenarios", "PlayerShipOnly", "--config", "min"]));
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var report = doc.RootElement;
                var row = Assert.Single(report.GetProperty("measurements").EnumerateArray());
                var placement = row.GetProperty("placement");
                Assert.Equal(365, placement.GetProperty("horizonDays").GetInt32());
                Assert.True(placement.GetProperty("attempts").GetInt32() > 0);
                Assert.Equal(JsonValueKind.Array, placement.GetProperty("criticalEpochs").ValueKind);
                var checks = placement.GetProperty("placementChecks").EnumerateArray().ToArray();
                foreach (long day in new long[] { 0, 1, 7, 30, 100, 365 })
                    Assert.Contains(checks, c => c.GetProperty("epochGameTimeMs").GetInt64() == day * 86400000);
                Assert.All(placement.GetProperty("components").EnumerateArray(), c => Assert.Equal(1, c.GetProperty("count").GetInt32()));
                Assert.Empty(placement.GetProperty("violations").EnumerateArray());
                Assert.True(placement.GetProperty("minSampledClearanceWorld").GetDouble() > 0);
                Assert.Contains("not a guarantee", placement.GetProperty("scope").GetString());
                if (previous is not null) Assert.Equal(previous, placement.GetRawText());
                previous = placement.GetRawText();
            }
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void FailedPlacementReportHasRepro()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var start = new ProcessStartInfo("dotnet") { RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            foreach (string argument in new[] { typeof(SolarMapEvidence).Assembly.Location, Root, path, "--solar-map", "--clusters", "--ai-placement", "--seeds", "7:7", "--scenarios", "PlayerShipOnly", "--ai-patrol-radius-km", "1000000000000", "--ai-attempts", "2" })
                start.ArgumentList.Add(argument);
            using var process = Process.Start(start)!;
            Assert.True(process.WaitForExit(30000)); Assert.Equal(1, process.ExitCode);
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var report = doc.RootElement;
            Assert.Equal("failed", report.GetProperty("status").GetString());
            Assert.Equal(7UL, report.GetProperty("repro").GetProperty("seed").GetUInt64());
            Assert.Equal("PlayerShipOnly", report.GetProperty("repro").GetProperty("scenario").GetString());
            Assert.Equal(2, report.GetProperty("repro").GetProperty("config").GetProperty("ai").GetProperty("maxPlacementAttempts").GetInt32());
            Assert.Contains("attempts=2", report.GetProperty("error").GetString());
            Assert.Contains("ai_start_network_overlap", report.GetProperty("error").GetString());
        }
        finally { File.Delete(path); }
    }
}
