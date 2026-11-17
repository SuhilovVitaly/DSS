using System.Text.Json;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0033Us0002Tk0002Tests
{
    [Fact]
    public void ServiceMarkets_HaveFiniteReplenishedTorpedoes()
    {
        var root = Root();
        using var doc = JsonDocument.Parse(File.ReadAllText(root + "/Data/Markets/station-market-profiles.json"));
        foreach (string id in new[] { "market.transit", "market.industrial" })
        {
            var market = doc.RootElement.GetProperty("profiles").EnumerateArray().Single(x => x.GetProperty("typeId").GetString() == id);
            Assert.Equal(24, market.GetProperty("initialInventory").EnumerateArray().Single(IsTorpedo).GetProperty("quantity").GetInt32());
            Assert.Equal(2, market.GetProperty("economy").GetProperty("hourlyOutputs").EnumerateArray().Single(IsTorpedo).GetProperty("quantity").GetInt32());
        }
    }
    private static bool IsTorpedo(JsonElement x) => x.GetProperty("itemTypeId").GetString() == "item.drone-torpedo.t1";
    private static string Root([System.Runtime.CompilerServices.CallerFilePath] string file = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, "../../../src/DeepSpaceSaga.Client"));
}
