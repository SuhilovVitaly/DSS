using System.Text.Json;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0003Tk0002Tests
{
    [Fact]
    public void EveryDemoFrameHasFixedScrap()
    {
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(AppContext.BaseDirectory + "/Settings.json",
            AppContext.BaseDirectory + "/Scenarios/Demo/scenario.json");
        var loot = DemoCampaignDefinitions.Load(AppContext.BaseDirectory).Loot!;
        Assert.Equal(20, loot.DroneScrapQuantityByFrame["light"]);
        Assert.Equal(60, loot.DroneScrapQuantityByFrame["heavy"]);
        Assert.Equal(250, loot.CarrierScrapQuantityByLayout["carrier.standard"]);
    }

    [Fact]
    public void ScrapHasFiniteBuyer()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(AppContext.BaseDirectory + "/Data/Markets/station-market-profiles.json"));
        var buyer = doc.RootElement.GetProperty("profiles").EnumerateArray().Single(p => p.GetProperty("typeId").GetString() == "market.industrial");
        Assert.Contains(buyer.GetProperty("demandItemTypeIds").EnumerateArray(), x => x.GetString() == "item.scrap");
        Assert.True(buyer.GetProperty("initialCredits").GetInt64() > 0);
        Assert.Equal(5000, buyer.GetProperty("economy").GetProperty("stockTargets").EnumerateArray()
            .Single(x => x.GetProperty("itemTypeId").GetString() == "item.scrap").GetProperty("targetStock").GetInt64());
    }

    [Fact]
    public void TorpedoMappingUsesExistingItem() => Assert.Contains(DemoCampaignDefinitions.Load(AppContext.BaseDirectory).Loot!.ConsumableItems,
        m => m.Category == "ammo" && m.CatalogId == "torpedo.t1" && m.ItemTypeId == "item.drone-torpedo.t1");
}
