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
                double firstMs = (double)map["firstMs"]!, p95Ms = (double)map["p95Ms"]!;
                Assert.True(firstMs > p95Ms, $"{level}: firstMs {firstMs} must exceed p95 {p95Ms} on the first render after zoom.");
            }
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }
}
