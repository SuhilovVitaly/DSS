using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0036Us0003Tk0002MigrationTests
{
    [Fact]
    public void PreLootSave_PreservesEconomicsAndAddsZeroStock()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var registry = EngineContentLoader.LoadFromSettingsFile(DemoEp0033Us0001Tk0003Tests.Root() + "/Settings.json").Registry;
        var save = engine.CaptureSaveState();
        var old = save.GameState with
        {
            CatalogCompatibility = CarrierCatalogCompatibility.AcceptedPredecessors(registry).ElementAt(1),
            SpaceObjects = save.GameState.SpaceObjects.Select(o => o.MarketProfileId is { } id ? o with
            {
                MarketProfileFingerprint = LootCatalogCompatibility.Previous(registry.StationMarketProfiles.GetDefinition(
                    registry.StationMarketProfiles.GetIndex(id))).Fingerprint,
                Inventory = o.Inventory!.Where(i => !LootCatalogCompatibility.AddedSkus.Contains(i.ItemTypeId)).ToArray()
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
                .Where(i => !LootCatalogCompatibility.AddedSkus.Contains(i.ItemTypeId)).OrderBy(i => i.ItemTypeId));
            Assert.All(restored.Inventory!.Where(i => LootCatalogCompatibility.AddedSkus.Contains(i.ItemTypeId)), i => Assert.Equal(0, i.Quantity));
        }
        engine.LoadScenario(current, true);
        var corrupt = old with
        {
            SpaceObjects = old.SpaceObjects.Select(o => o.MarketProfileId is null ? o :
            o with { MarketProfileFingerprint = new string('0', 64) }).ToArray()
        };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with { GameState = corrupt }, true));
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
