using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Content;

public sealed record LootItemMapping(string Category, string CatalogId, string ItemTypeId);
public sealed record DemoLootDefinitions(int SchemaVersion, string ScrapItemTypeId,
    ImmutableDictionary<string, long> DroneScrapQuantityByFrame,
    ImmutableDictionary<string, long> CarrierScrapQuantityByLayout,
    ImmutableArray<LootItemMapping>? ModuleItems = null, ImmutableArray<LootItemMapping> ConsumableItems = default)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string Fingerprint => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
    {
        SchemaVersion,
        ScrapItemTypeId,
        DroneScrapQuantityByFrame = DroneScrapQuantityByFrame.OrderBy(p => p.Key, StringComparer.Ordinal),
        CarrierScrapQuantityByLayout = CarrierScrapQuantityByLayout.OrderBy(p => p.Key, StringComparer.Ordinal),
        ModuleItems = ModuleItems?.OrderBy(m => m.Category, StringComparer.Ordinal).ThenBy(m => m.CatalogId, StringComparer.Ordinal),
        ConsumableItems = ConsumableItems.IsDefault ? null : ConsumableItems.OrderBy(m => m.Category, StringComparer.Ordinal).ThenBy(m => m.CatalogId, StringComparer.Ordinal)
    })));

    internal void ValidateReferences(GameDataRegistry registry, DemoEncountersDefinition? encounters)
    {
        void Require(bool valid) { if (!valid) throw new ScenarioException("Invalid demo loot catalog or mapping."); }
        Require(SchemaVersion == 1 && DroneScrapQuantityByFrame is not null && CarrierScrapQuantityByLayout is not null);
        var catalog = registry.TacticalBattle.Catalog ?? throw new ScenarioException("Loot requires tactical content.");
        ItemTypeDefinition Item(string id)
        {
            Require(!string.IsNullOrWhiteSpace(id) && registry.ItemTypes.Contains(id));
            var item = registry.ItemTypes.GetDefinition(registry.ItemTypes.GetIndex(id));
            Require(item.UnitMassKg > 0 && item.BasePriceCredits is >= 0 && item.StorageKind == ItemStorageKind.Cargo);
            return item;
        }
        Require(Item(ScrapItemTypeId).UnitMassKg == 1);
        Require(DroneScrapQuantityByFrame!.All(p => p.Value > 0 && catalog.Frames.Contains(p.Key)) &&
            CarrierScrapQuantityByLayout!.All(p => p.Value > 0 && catalog.Layouts.Contains(p.Key)));
        if (encounters is null) throw new ScenarioException("Loot requires encounter definitions.");
        foreach (var layoutId in encounters.DroneLayouts)
            Require(DroneScrapQuantityByFrame!.ContainsKey(catalog.Layouts[layoutId].Class));
        foreach (var profile in encounters.Profiles) Require(CarrierScrapQuantityByLayout!.ContainsKey(profile.CarrierLayoutId));
        double ModuleMass(LootItemMapping m) => m.Category switch
        {
            "weapon" when catalog.Weapons.Contains(m.CatalogId) => catalog.Weapons[m.CatalogId].MassKg,
            "armor" when catalog.Armor.Contains(m.CatalogId) => catalog.Armor[m.CatalogId].MassKg,
            "engine" when catalog.Engines.Contains(m.CatalogId) => catalog.Engines[m.CatalogId].MassKg,
            "shield" when catalog.Shields.Contains(m.CatalogId) => catalog.Shields[m.CatalogId].MassKg,
            "decoy" when catalog.Decoys.Contains(m.CatalogId) => catalog.Decoys[m.CatalogId].MassKg,
            _ => throw new ScenarioException("Unknown loot module catalog identity.")
        };
        if (ModuleItems is { } modules)
        {
            Require(!modules.IsDefault && modules.All(m => m is not null) &&
                modules.Select(m => (m.Category, m.CatalogId)).Distinct().Count() == modules.Length);
            foreach (var m in modules) Require(Item(m.ItemTypeId).UnitMassKg == ModuleMass(m));
            void Mapped(string category, string? id) { if (id is not null) Require(modules.Any(m => m.Category == category && m.CatalogId == id)); }
            foreach (var id in encounters.DroneLayouts)
            {
                var layout = catalog.Layouts[id];
                Mapped("weapon", layout.Weapon); Mapped("armor", layout.Armor);
                Mapped("shield", layout.Shield); Mapped("decoy", layout.Module);
                foreach (var engine in layout.Engines) Mapped("engine", engine);
            }
        }
        Require(!ConsumableItems.IsDefault && ConsumableItems.All(m => m is not null) &&
            ConsumableItems.Select(m => (m.Category, m.CatalogId)).Distinct().Count() == ConsumableItems.Length);
        foreach (var m in ConsumableItems)
        {
            Item(m.ItemTypeId);
            Require(m.Category == "ammo" && m.CatalogId == "torpedo.t1" || m.Category == "decoy" && catalog.Decoys.Contains(m.CatalogId));
        }
    }
}
