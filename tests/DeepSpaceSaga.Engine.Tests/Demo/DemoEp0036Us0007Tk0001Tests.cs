using System.Collections.Immutable;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0036Us0007Tk0001Tests
{
    [Fact]
    public void UnknownItemAndIncompleteModuleCoverage_Reject()
    {
        string root = DemoEp0033Us0001Tk0003Tests.Root();
        var registry = EngineContentLoader.LoadFromSettingsFile(root + "/Settings.json").Registry;
        var definitions = DemoCampaignDefinitions.Load(root);
        var loot = new DemoLootDefinitions(1, "missing", new Dictionary<string, long> { ["light"] = 20, ["heavy"] = 60 }.ToImmutableDictionary(),
            new Dictionary<string, long> { ["carrier.standard"] = 250 }.ToImmutableDictionary(), ConsumableItems: []);
        Assert.Throws<ScenarioException>(() => loot.ValidateReferences(registry, definitions.Encounters));
        loot = loot with { ScrapItemTypeId = "item.steel", ModuleItems = [] };
        Assert.Throws<ScenarioException>(() => loot.ValidateReferences(registry, definitions.Encounters));
    }

    [Fact]
    public void LootFingerprint_ChangesWithoutChangingCluster()
    {
        var loot = new DemoLootDefinitions(1, "item.scrap", ImmutableDictionary<string, long>.Empty,
            ImmutableDictionary<string, long>.Empty, ConsumableItems: []);
        Assert.NotEqual(loot.Fingerprint, (loot with { ScrapItemTypeId = "item.other" }).Fingerprint);
        var definitions = new DemoCampaignDefinitions(null, "cluster") { Loot = loot };
        Assert.Equal("cluster", (definitions with { Loot = loot with { ScrapItemTypeId = "item.other" } }).Fingerprint);
    }
}
