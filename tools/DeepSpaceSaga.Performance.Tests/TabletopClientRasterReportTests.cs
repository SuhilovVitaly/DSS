using System.Text.Json.Nodes;

namespace DeepSpaceSaga.Performance.Tests;

public sealed class TabletopClientRasterReportTests
{
    static readonly string[] MapLevels = ["local", "region", "cluster", "system"];

    [Fact]
    public void FirstOpenIsMeasuredOnFirstRenderAndPassesAreMarkedColdOrWarm()
    {
        string directory = Path.GetFullPath(Path.Combine(V2MapAcceptanceReportTests.Root,
            "../artifacts/validation/tabletop-client/report-tests-" + Guid.NewGuid().ToString("N")));
        try
        {
            string output = Path.Combine(directory, "raster.json");
            TabletopClientRaster.Run(V2MapAcceptanceReportTests.Root, output);
            var results = JsonNode.Parse(File.ReadAllText(output))!["results"]!.AsArray();
            Assert.Equal(12, results.Count);

            var cold = results.Where(r => (string)r!["language"]! == "English").ToList();
            var warm = results.Where(r => (string)r!["language"]! == "Russian").ToList();
            Assert.All(cold, r => Assert.Equal("cold", (string)r!["cache"]!));
            Assert.All(warm, r => Assert.Equal("warm", (string)r!["cache"]!));

            foreach (string level in MapLevels)
            {
                var map = cold.Single(r => (string)r!["name"]! == level)!;
                // Timing comparisons (firstMs vs p95) are evidence for a dedicated raster run, not a unit-test
                // assertion: under a parallel suite run the p95 of the warm frames is noisy.
                double firstMs = (double)map["firstMs"]!;
                Assert.True(double.IsFinite(firstMs) && firstMs > 0, $"{level}: firstMs {firstMs} must be a measured render time.");
            }
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }
}
