using System.Text.Json;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0006Tk0002Tests
{
    [Fact]
    public void EveryInstalledModuleMapsToSaleItem()
    {
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(AppContext.BaseDirectory + "/Settings.json",
            AppContext.BaseDirectory + "/Scenarios/Demo/scenario.json");
        Assert.Equal(6, DemoCampaignDefinitions.Load(AppContext.BaseDirectory).Loot!.ModuleItems!.Value.Length);
    }

    [Fact]
    public void ItemMassMatchesTacticalDefinition()
    {
        var loot = DemoCampaignDefinitions.Load(AppContext.BaseDirectory).Loot!;
        using var items = JsonDocument.Parse(File.ReadAllText(AppContext.BaseDirectory + "/Data/Items/Good/items-good.json"));
        var folders = new Dictionary<string, string> { ["weapon"] = "Weapons", ["armor"] = "Armor", ["engine"] = "Engines", ["shield"] = "Shields", ["decoy"] = "Decoys" };
        foreach (var map in loot.ModuleItems!.Value)
        {
            using var module = JsonDocument.Parse(File.ReadAllText(AppContext.BaseDirectory + "/Data/TacticalBattle/Modules/" + folders[map.Category] + "/" + map.CatalogId + ".json"));
            var item = items.RootElement.GetProperty("itemTypes").EnumerateArray().Single(i => i.GetProperty("typeId").GetString() == map.ItemTypeId);
            Assert.Equal(module.RootElement.GetProperty("massKg").GetDouble(), item.GetProperty("unitMassKg").GetDouble());
            Assert.Equal(Math.Ceiling(module.RootElement.GetProperty("massKg").GetDouble() * 10), item.GetProperty("basePriceCredits").GetDouble());
        }
    }

    [Fact]
    public void RepeatedSlotsRemainMultipleUnits()
    {
        using var layouts = JsonDocument.Parse(File.ReadAllText(AppContext.BaseDirectory + "/Data/TacticalBattle/layouts.json"));
        var heavy = layouts.RootElement.GetProperty("heavy.sword.shield");
        Assert.Equal(2, heavy.GetProperty("engines").GetArrayLength());
        Assert.Equal(2, heavy.GetProperty("shieldCount").GetInt32());
    }

    [Fact]
    public void TrophyHasFiniteBuyer()
    {
        var loot = DemoCampaignDefinitions.Load(AppContext.BaseDirectory).Loot!;
        using var doc = JsonDocument.Parse(File.ReadAllText(AppContext.BaseDirectory + "/Data/Markets/station-market-profiles.json"));
        var buyer = doc.RootElement.GetProperty("profiles").EnumerateArray().Single(p => p.GetProperty("typeId").GetString() == "market.industrial");
        foreach (var id in loot.ModuleItems!.Value.Select(m => m.ItemTypeId).Append("item.decoy-charge.t1"))
        {
            Assert.Contains(buyer.GetProperty("demandItemTypeIds").EnumerateArray(), x => x.GetString() == id);
            Assert.Equal(50, buyer.GetProperty("economy").GetProperty("stockTargets").EnumerateArray()
                .Single(x => x.GetProperty("itemTypeId").GetString() == id).GetProperty("targetStock").GetInt64());
        }
    }
}
