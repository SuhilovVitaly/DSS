using System.Collections.Immutable;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>
/// Catalog-compatibility allowlist for EP-0029 (carrier + finite torpedoes). Adding the new torpedo
/// SKU changed the economic catalog fingerprint, so older saves carry the pre-carrier digest. That
/// digest is accepted only when it is the explicitly known previous fingerprint and the pre-existing
/// SKUs are structurally unchanged — never via a generic bypass, and never by substituting an
/// arbitrary fingerprint.
/// </summary>
internal static class CarrierCatalogCompatibility
{
    // Item SKUs added by EP-0029. Excluded when recomputing the pre-carrier digest.
    private static readonly ImmutableHashSet<string> AddedItemSkus = ImmutableHashSet.Create(
        StringComparer.Ordinal, "item.drone-torpedo.t1");

    /// <summary>Catalog identity carried by a structurally unchanged pre-carrier world.</summary>
    internal static CatalogCompatibilityData Previous(GameDataRegistry registry)
    {
        var previous = Enumerable.Range(0, registry.ItemTypes.Count)
            .Select(registry.ItemTypes.GetDefinition)
            .Where(item => !AddedItemSkus.Contains(item.TypeId));
        return new CatalogCompatibilityData(
            registry.CatalogVersion,
            EconomyTimeData.CurrentRulesVersion,
            GameDataRegistry.EconomicCatalogFingerprint(previous));
    }

    /// <summary>
    /// True when the saved identity is either the current catalog or the allowlisted previous one.
    /// A mismatch in catalog/rules version always rejects; a fingerprint that is neither the current
    /// digest nor the recomputed previous digest (which structurally proves the pre-existing SKUs
    /// are unchanged) is rejected before any session is published.
    /// </summary>
    internal static bool Accepts(GameDataRegistry registry, CatalogCompatibilityData saved)
    {
        if (saved == registry.CatalogCompatibility)
            return true;
        if (saved.CatalogVersion != registry.CatalogVersion ||
            saved.RulesVersion != EconomyTimeData.CurrentRulesVersion)
            return false;
        return string.Equals(saved.Fingerprint, Previous(registry).Fingerprint, StringComparison.Ordinal);
    }
}
