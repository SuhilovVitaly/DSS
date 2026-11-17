using System.Text.Json;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0033Us0001Tk0002Tests
{
    private static string SourceRoot([System.Runtime.CompilerServices.CallerFilePath] string file = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, "../../../src/DeepSpaceSaga.Client"));

    [Fact]
    public void Content_LoadsAndReferencesResolve()
    {
        using var services = JsonDocument.Parse(File.ReadAllText(Path.Combine(SourceRoot(), "Data/Demo/services.json")));
        using var cluster = JsonDocument.Parse(File.ReadAllText(Path.Combine(SourceRoot(), "Data/Demo/cluster.json")));
        var stations = cluster.RootElement.GetProperty("stationIds").EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Equal(1, services.RootElement.GetProperty("schemaVersion").GetInt32());
        foreach (var station in services.RootElement.GetProperty("stationIds").EnumerateArray())
            Assert.Contains(station.GetString(), stations);
        foreach (var rate in services.RootElement.GetProperty("repairRates").EnumerateObject())
            Assert.True(rate.Value.GetInt64() >= 0);
    }

    [Fact]
    public void DemoLimits_AreSatisfied()
    {
        using var services = JsonDocument.Parse(File.ReadAllText(Path.Combine(SourceRoot(), "Data/Demo/services.json")));
        var stations = services.RootElement.GetProperty("stationIds").EnumerateArray().Select(x => x.GetString()).ToArray();
        Assert.Contains("SPC-0002", stations);
        Assert.True(stations.Distinct().Count() >= 2);
    }
}
