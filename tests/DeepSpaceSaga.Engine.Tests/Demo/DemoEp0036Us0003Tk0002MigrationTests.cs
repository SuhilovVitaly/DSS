using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0036Us0003Tk0002MigrationTests
{
    [Fact]
    public void IncompleteOrAliasedLootMappings_RejectBeforeGameplay()
    {
        var root = DemoEp0033Us0001Tk0003Tests.Root();
        var registry = EngineContentLoader.LoadFromSettingsFile(root + "/Settings.json").Registry;
        var definitions = DemoCampaignDefinitions.Load(root);
        var loot = definitions.Loot!;
        Assert.Throws<ScenarioException>(() => (loot with { ConsumableItems = [] }).ValidateReferences(registry, definitions.Encounters));
        Assert.Throws<ScenarioException>(() => (loot with { ModuleItems = null }).ValidateReferences(registry, definitions.Encounters));
        var duplicate = loot.ConsumableItems.SetItem(1, loot.ConsumableItems[1] with { ItemTypeId = loot.ConsumableItems[0].ItemTypeId });
        Assert.Throws<ScenarioException>(() => (loot with { ConsumableItems = duplicate }).ValidateReferences(registry, definitions.Encounters));
        var profile = registry.StationMarketProfiles.GetDefinition(registry.StationMarketProfiles.GetIndex("market.industrial"));
        string previous = LootCatalogCompatibility.Previous(profile).Fingerprint;
        Assert.False(LootCatalogCompatibility.IsPrevious(profile with { InitialCredits = profile.InitialCredits + 1 }, previous));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreLootSave_PreservesEconomicsAndAddsZeroStock(bool keepScrap)
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var registry = EngineContentLoader.LoadFromSettingsFile(DemoEp0033Us0001Tk0003Tests.Root() + "/Settings.json").Registry;
        var save = engine.CaptureSaveState();
        var old = save.GameState with
        {
            CatalogCompatibility = CarrierCatalogCompatibility.AcceptedPredecessors(registry).ElementAt(keepScrap ? 1 : 2),
            SpaceObjects = save.GameState.SpaceObjects.Select(o => o.MarketProfileId is { } id ? o with
            {
                MarketProfileFingerprint = LootCatalogCompatibility.Previous(registry.StationMarketProfiles.GetDefinition(
                    registry.StationMarketProfiles.GetIndex(id)), keepScrap).Fingerprint,
                Inventory = o.Inventory!.Where(i => !LootCatalogCompatibility.Removed(i.ItemTypeId, keepScrap)).ToArray()
            } : o).ToArray()
        };
        old = old with
        {
            TradingEconomyContinuation = old.TradingEconomyContinuation! with
            { ConfigurationFingerprint = TradingEconomySaveMigration.ConfigurationFingerprint(old) }
        };
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save with { GameState = old }), true, true), true);
        var current = engine.CaptureSaveState();
        foreach (var station in old.SpaceObjects.Where(o => o.MarketProfileId is not null))
        {
            var restored = current.GameState.SpaceObjects.Single(o => o.ObjectId == station.ObjectId);
            Assert.Equal(station.Credits, restored.Credits);
            Assert.Equal(station.MarketBudgetCredits, restored.MarketBudgetCredits);
            Assert.Equal(station.MarketRevision, restored.MarketRevision);
            Assert.Equal(station.Inventory!.OrderBy(i => i.ItemTypeId), restored.Inventory!
                .Where(i => !LootCatalogCompatibility.Removed(i.ItemTypeId, keepScrap)).OrderBy(i => i.ItemTypeId));
            Assert.All(restored.Inventory!.Where(i => LootCatalogCompatibility.AddedSkus.Contains(i.ItemTypeId)), i => Assert.Equal(0, i.Quantity));
        }
        engine.LoadScenario(current, true);
        var corrupt = old with
        {
            SpaceObjects = old.SpaceObjects.Select(o => o.MarketProfileId is null ? o :
            o with { MarketProfileFingerprint = new string('0', 64) }).ToArray()
        };
        corrupt = corrupt with
        {
            TradingEconomyContinuation = corrupt.TradingEconomyContinuation! with
            { ConfigurationFingerprint = TradingEconomySaveMigration.ConfigurationFingerprint(corrupt) }
        };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with { GameState = corrupt }, true));
        string before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        var stationId = old.SpaceObjects.First(o => o.MarketProfileId is not null).MarketProfileId!;
        var profile = registry.StationMarketProfiles.GetDefinition(registry.StationMarketProfiles.GetIndex(stationId));
        var foreign = profile with { InitialCredits = profile.InitialCredits + 1 };
        foreach (var tampered in new[]
        {
            old with { MarketEventCatalogFingerprint = new string('0', 64) },
            old with { CatalogCompatibility = old.CatalogCompatibility! with { Fingerprint = new string('0', 64) } },
            old with { SpaceObjects = old.SpaceObjects.Select(o => o.MarketProfileId == stationId ? o with
                { MarketProfileFingerprint = LootCatalogCompatibility.Previous(foreign, keepScrap).Fingerprint } : o).ToArray() },
            old with { SpaceObjects = old.SpaceObjects.Select(o => o.MarketProfileId is not null ? o with
                { Inventory = o.Inventory!.Skip(1).ToArray() } : o).ToArray() }
        })
        {
            var consistent = tampered with
            {
                TradingEconomyContinuation = tampered.TradingEconomyContinuation! with
                { ConfigurationFingerprint = TradingEconomySaveMigration.ConfigurationFingerprint(tampered) }
            };
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with { GameState = consistent }, true));
            Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
        }
    }

    [Fact]
    public void CurrentProfileCannotLoseNewOrOldStock()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var save = engine.CaptureSaveState();
        var station = save.GameState.SpaceObjects.First(o => o.Inventory?.Any(i => i.ItemTypeId == "item.scrap") == true);
        foreach (var item in new[] { "item.scrap", "item.fuel" })
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with
            {
                GameState = save.GameState with
                {
                    SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId == station.ObjectId ? o with
                    { Inventory = o.Inventory!.Where(i => i.ItemTypeId != item).ToArray() } : o).ToArray()
                }
            }, true));
    }
}
