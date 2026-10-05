using System.Diagnostics;
using System.Text.Json;

namespace DeepSpaceSaga.Performance.Tests;

public sealed class SystemPerformanceReportTests
{
    private static string Root
    {
        get { var dir = new DirectoryInfo(AppContext.BaseDirectory); while (!File.Exists(Path.Combine(dir!.FullName, "DeepSpaceSaga.sln"))) dir = dir.Parent; return dir.FullName; }
    }
    [Fact]
    public void ReportHasReproductionAndBackend()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            string originalDirectory = Directory.GetCurrentDirectory();
            Assert.Equal(0, SolarMapEvidence.Run([Root, path, "--solar-map", "--seeds", "1:1", "--scenarios", "PlayerShipOnly", "--config", "min"]));
            using var json = JsonDocument.Parse(File.ReadAllText(path));
            var report = json.RootElement;
            Assert.Equal(originalDirectory, Directory.GetCurrentDirectory());
            Assert.Equal(Path.Combine(Root, "src/DeepSpaceSaga.Client"), report.GetProperty("assetRoot").GetString());
            Assert.Equal(1, report.GetProperty("schemaVersion").GetInt32());
            Assert.Equal("CPU/Skia raster", report.GetProperty("backend").GetString());
            Assert.Equal("not-measured", report.GetProperty("presentation").GetProperty("status").GetString());
            Assert.False(string.IsNullOrWhiteSpace(report.GetProperty("commit").GetString()));
            Assert.True(report.GetProperty("machine").GetProperty("logicalProcessors").GetInt32() > 0);
            Assert.Single(report.GetProperty("measurements").EnumerateArray());
            Assert.All(report.GetProperty("rendering").EnumerateArray(), row =>
            {
                Assert.Equal(600, row.GetProperty("measuredFrames").GetInt32());
                Assert.True(row.GetProperty("p99Ms").GetDouble() > 0);
            });
        }
        finally { File.Delete(path); }
    }
    [Fact]
    public void InvalidConfigReturnsFailure()
    {
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var start = new ProcessStartInfo("dotnet") { RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            foreach (string argument in new[] { typeof(SolarMapEvidence).Assembly.Location, Root, path, "--solar-map", "--config", "invalid" })
                start.ArgumentList.Add(argument);
            using var process = Process.Start(start)!;
            Assert.True(process.WaitForExit(30000));
            Assert.Equal(1, process.ExitCode);
            using var json = JsonDocument.Parse(File.ReadAllText(path));
            Assert.Equal("failed", json.RootElement.GetProperty("status").GetString());
        }
        finally { File.Delete(path); }
    }
}
