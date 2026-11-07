using System.Text.Json.Nodes;

namespace DeepSpaceSaga.Performance.Tests;

[Collection("Solar map evidence")]
public sealed class V2MapAcceptanceReportTests
{
    internal static string Root
    {
        get { var d = new DirectoryInfo(AppContext.BaseDirectory); while (!File.Exists(Path.Combine(d!.FullName, "DeepSpaceSaga.sln"))) d = d.Parent; return d.FullName; }
    }
    internal static string NewDirectory()
    {
        string path = Path.GetFullPath(Path.Combine(Root, "../artifacts/validation/ep-0018-map-geography-and-scale/report-tests-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(path); return path;
    }
    internal static string[] Arguments(string output) => [Root, output, "--map-v2-acceptance", "--seeds", "42", "--scenarios", "Default", "--viewports", "1280x720", "--ui-scales", "1", "--warmup", "1", "--frames", "2"];
    [Fact]
    public void ReportBindsCameraGeometryAndImagesToSeed()
    {
        string directory = NewDirectory();
        try
        {
            string output = Path.Combine(directory, "report.json"), original = Directory.GetCurrentDirectory();
            Assert.Equal(0, V2MapAcceptance.Run(Arguments(output)));
            Assert.Equal(original, Directory.GetCurrentDirectory());
            Assert.True(V2MapAcceptance.Validate(output, out string error), error);
            var report = JsonNode.Parse(File.ReadAllText(output))!;
            Assert.Equal(4, report["rendering"]!.AsArray().Count);
            Assert.Equal(42UL, report["worlds"]![0]!["seed"]!.GetValue<ulong>());
            Assert.Equal(2, report["worlds"]![0]!["generationVersion"]!.GetValue<int>());
            Assert.True(report["worlds"]![0]!["minimumDays"]!.GetValue<double>() >= 1);
        }
        finally { Directory.Delete(directory, true); }
    }
    [Fact]
    public void MissingEvidenceCannotPass()
    {
        string directory = NewDirectory();
        try
        {
            string output = Path.Combine(directory, "report.json");
            Assert.Equal(0, V2MapAcceptance.Run(Arguments(output)));
            var report = JsonNode.Parse(File.ReadAllText(output))!;
            string original = report.ToJsonString();
            report["rendering"]![0]!["camera"]!["ppu"] = .001;
            File.WriteAllText(output, report.ToJsonString());
            Assert.False(V2MapAcceptance.Validate(output, out _));
            File.WriteAllText(output, original);
            string image = report["rendering"]![0]!["image"]!.GetValue<string>();
            File.Delete(image);
            Assert.False(V2MapAcceptance.Validate(output, out _));
        }
        finally { Directory.Delete(directory, true); }
    }
    [Fact]
    public void UnknownOptionsFailBeforeRendering()
    {
        string directory = NewDirectory();
        try
        {
            string output = Path.Combine(directory, "report.json");
            Assert.Equal(1, V2MapAcceptance.Run([.. Arguments(output), "--imaginary-setting", "true"]));
            Assert.False(V2MapAcceptance.Validate(output, out _));
            Assert.Empty(Directory.GetDirectories(directory));
        }
        finally { Directory.Delete(directory, true); }
    }
}
