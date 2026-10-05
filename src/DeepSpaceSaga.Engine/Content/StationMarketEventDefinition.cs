using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Content;

internal sealed record StationMarketEventDefinition(
    [property: JsonPropertyName("typeId"), JsonRequired] string TypeId,
    [property: JsonPropertyName("displayNameKey"), JsonRequired] string DisplayNameKey,
    [property: JsonPropertyName("descriptionKey"), JsonRequired] string DescriptionKey,
    [property: JsonPropertyName("effectSummaryKey"), JsonRequired] string EffectSummaryKey,
    [property: JsonPropertyName("priority"), JsonRequired] int Priority,
    [property: JsonPropertyName("chancePermillePerHour"), JsonRequired] int ChancePermillePerHour,
    [property: JsonPropertyName("minDurationHours"), JsonRequired] int MinDurationHours,
    [property: JsonPropertyName("maxDurationHours"), JsonRequired] int MaxDurationHours,
    [property: JsonPropertyName("eligibleMarketProfileIds"), JsonRequired] ImmutableArray<string> EligibleMarketProfileIds,
    [property: JsonPropertyName("itemEffects"), JsonRequired] ImmutableArray<StationMarketEventItemEffectDefinition> ItemEffects,
    [property: JsonPropertyName("routeEffect")] StationMarketEventRouteEffectDefinition? RouteEffect = null) : ITypeDefinition;

internal sealed record StationMarketEventItemEffectDefinition(
    [property: JsonPropertyName("itemTypeId"), JsonRequired] string ItemTypeId,
    [property: JsonPropertyName("productionMultiplierPermille"), JsonRequired] int ProductionMultiplierPermille,
    [property: JsonPropertyName("demandMultiplierPermille"), JsonRequired] int DemandMultiplierPermille,
    [property: JsonPropertyName("priceMultiplierPermille"), JsonRequired] int PriceMultiplierPermille,
    [property: JsonPropertyName("activationStockDelta")] long ActivationStockDelta = 0);

internal sealed record StationMarketEventRouteEffectDefinition(
    [property: JsonPropertyName("availability"), JsonRequired] string Availability,
    [property: JsonPropertyName("maxAffectedIncidentEdges"), JsonRequired] int MaxAffectedIncidentEdges,
    [property: JsonPropertyName("travelTimeMultiplierPermille"), JsonRequired] int TravelTimeMultiplierPermille,
    [property: JsonPropertyName("fuelMultiplierPermille"), JsonRequired] int FuelMultiplierPermille,
    [property: JsonPropertyName("riskProfileId")] string? RiskProfileId = null);

internal static class StationMarketEventIds
{
    public static ImmutableArray<string> All { get; } =
    ["event.reactor-accident", "event.decompression", "event.hydroponics-failure", "event.pirate-blockade",
     "event.cargo-convoy", "event.quarantine", "event.repair-boom", "event.scientific-contract"];
}

internal static class StationMarketEventCatalog
{
    internal static TypeRegistry<StationMarketEventDefinition> Create(
        IEnumerable<StationMarketEventDefinition> definitions, TypeRegistry<ItemTypeDefinition> items,
        TypeRegistry<StationMarketProfileDefinition> profiles, bool requireComplete)
    {
        var events = definitions.ToArray();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var definition in events)
        {
            if (definition is null) throw new ContentException("stationMarketEvents contains null.");
            void Reject(string detail) => throw new ContentException($"Event '{definition.TypeId}': {detail}.");
            if (!StationMarketEventIds.All.Contains(definition.TypeId, StringComparer.Ordinal)) Reject("unknown canonical definition ID");
            foreach (var key in new[] { definition.DisplayNameKey, definition.DescriptionKey, definition.EffectSummaryKey })
                if (string.IsNullOrWhiteSpace(key) || !keys.Add(key)) Reject("missing or duplicate localization key");
            if (definition.Priority is < 0 or > 1000 || definition.ChancePermillePerHour is < 0 or > 1000 ||
                definition.MinDurationHours < 1 || definition.MaxDurationHours < definition.MinDurationHours ||
                definition.MaxDurationHours > 168) Reject("priority/chance/duration outside allowed range");
            if (definition.EligibleMarketProfileIds.IsDefaultOrEmpty || definition.ItemEffects.IsDefaultOrEmpty)
                Reject("eligible profiles and item effects must be nonempty");
            var profileIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in definition.EligibleMarketProfileIds)
                if (string.IsNullOrWhiteSpace(id) || !profileIds.Add(id) || !profiles.Contains(id)) Reject("unknown or duplicate profile");
            var eligible = definition.EligibleMarketProfileIds.Select(id => profiles.GetDefinition(profiles.GetIndex(id))).ToArray();
            var itemIds = new HashSet<string>(StringComparer.Ordinal);
            bool changesMarket = false;
            foreach (var effect in definition.ItemEffects)
            {
                if (effect is null || string.IsNullOrWhiteSpace(effect.ItemTypeId) || !itemIds.Add(effect.ItemTypeId) || !items.Contains(effect.ItemTypeId))
                    Reject("unknown, null or duplicate item effect");
                var item = items.GetDefinition(items.GetIndex(effect!.ItemTypeId));
                if (item.StorageKind != ItemStorageKind.Cargo || effect.ItemTypeId == "item.fuel") Reject("Fuel is not a cargo event item");
                if (effect.ProductionMultiplierPermille is < 0 or > 4000 || effect.DemandMultiplierPermille is < 0 or > 4000 ||
                    effect.PriceMultiplierPermille is < 0 or > 4000 || effect.ActivationStockDelta is < -1000000 or > 1000000)
                    Reject("effect multiplier/delta outside allowed range");
                if (effect.ProductionMultiplierPermille != 1000 && !eligible.Any(p => p.SupplyItemTypeIds.Contains(effect.ItemTypeId)))
                    Reject("production effect has no eligible supply flow");
                if (effect.DemandMultiplierPermille != 1000 && !eligible.Any(p => p.DemandItemTypeIds.Contains(effect.ItemTypeId)))
                    Reject("demand effect has no eligible demand flow");
                if (effect.ActivationStockDelta != 0 && !eligible.All(p => p.Economy?.StockTargets.Any(t => t.ItemTypeId == effect.ItemTypeId) == true))
                    Reject("activation stock delta requires a target in every eligible profile");
                changesMarket |= effect.ProductionMultiplierPermille != 1000 || effect.DemandMultiplierPermille != 1000 ||
                    effect.PriceMultiplierPermille != 1000 || effect.ActivationStockDelta != 0;
            }
            if (definition.RouteEffect is { } route)
            {
                if (definition.TypeId is not ("event.pirate-blockade" or "event.quarantine") ||
                    route.Availability is not (StationRouteAvailabilityEffects.Restricted or StationRouteAvailabilityEffects.Unavailable) ||
                    route.MaxAffectedIncidentEdges != 1 || route.TravelTimeMultiplierPermille is < 500 or > 4000 ||
                    route.FuelMultiplierPermille is < 500 or > 4000 || route.RiskProfileId is not null && string.IsNullOrWhiteSpace(route.RiskProfileId))
                    Reject("invalid bounded route effect");
            }
            if (!changesMarket) Reject("event must have a market effect");
        }
        var registry = TypeRegistry<StationMarketEventDefinition>.Create(events, "station market events");
        if (requireComplete && (events.Length != 8 || !events.Select(e => e.TypeId).ToHashSet(StringComparer.Ordinal).SetEquals(StationMarketEventIds.All)))
            throw new ContentException("stationMarketEvents requires exactly eight canonical definitions.");
        return registry;
    }

    internal static string Fingerprint(TypeRegistry<StationMarketEventDefinition> events) =>
        Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(
            Enumerable.Range(0, events.Count).Select(events.GetDefinition).OrderBy(e => e.TypeId, StringComparer.Ordinal)
                .Select(e => e with
                {
                    EligibleMarketProfileIds = e.EligibleMarketProfileIds.Order(StringComparer.Ordinal).ToImmutableArray(),
                    ItemEffects = e.ItemEffects.OrderBy(i => i.ItemTypeId, StringComparer.Ordinal).ToImmutableArray(),
                }))));
}
