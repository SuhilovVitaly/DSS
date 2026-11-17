using System.Collections.Immutable;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Scenario;

// Exact additive EP-0036 identities; never exempt unknown catalog entries from validation.
internal static class LootCatalogCompatibility
{
    internal static readonly ImmutableHashSet<string> AddedSkus = ImmutableHashSet.Create(StringComparer.Ordinal,
        "item.scrap", "item.trophy.weapon.gun.sword.t1", "item.trophy.weapon.gun.spear.t1",
        "item.trophy.armor.plate.medium", "item.trophy.engine.drive.standard",
        "item.trophy.shield.shield.t1", "item.trophy.decoy.decoy.t1", "item.decoy-charge.t1");

    internal static StationMarketProfileDefinition Previous(StationMarketProfileDefinition profile, bool keepScrap = false) => profile with
    {
        DemandItemTypeIds = profile.DemandItemTypeIds.Where(id => !Removed(id, keepScrap)).ToImmutableArray(),
        InitialInventory = profile.InitialInventory.Where(s => !Removed(s.ItemTypeId, keepScrap)).ToImmutableArray(),
        Economy = profile.Economy is { } economy ? economy with
        {
            StockTargets = economy.StockTargets.Where(s => !Removed(s.ItemTypeId, keepScrap)).ToImmutableArray()
        } : null
    };

    internal static bool IsPrevious(StationMarketProfileDefinition profile, string? fingerprint) =>
        fingerprint is not null && fingerprint != profile.Fingerprint &&
        profile.InitialInventory.Where(s => AddedSkus.Contains(s.ItemTypeId)).All(s => s.Quantity == 0) &&
        (fingerprint == Previous(profile).Fingerprint || fingerprint == Previous(profile, true).Fingerprint);

    internal static bool Removed(string id, bool keepScrap) => AddedSkus.Contains(id) && (!keepScrap || id != "item.scrap");

    internal static bool CanAddMissingStock(StationMarketProfileDefinition profile, string? fingerprint, string itemId) =>
        IsPrevious(profile, fingerprint) && AddedSkus.Contains(itemId) &&
        profile.InitialInventory.Any(s => s.ItemTypeId == itemId && s.Quantity == 0) &&
        (fingerprint == Previous(profile).Fingerprint || Removed(itemId, true));
}
