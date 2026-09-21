using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;

namespace DeepSpaceSaga.Engine.Content;

internal sealed record StationMarketStockDefinition(string ItemTypeId, long Quantity);

internal sealed record StationMarketProfileDefinition(
    string TypeId,
    string DisplayName,
    ImmutableArray<string> SupplyItemTypeIds,
    ImmutableArray<string> DemandItemTypeIds,
    ImmutableArray<StationMarketStockDefinition> InitialInventory,
    long InitialCredits,
    long RefuelStockKg,
    ImmutableDictionary<StationSize, int> SizeFactors) : ITypeDefinition
{
    // Compute from this record, including copies made with `with`; presentation is not economic state.
    public string Fingerprint => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
    {
        TypeId,
        SupplyItemTypeIds = SupplyItemTypeIds.Order(StringComparer.Ordinal),
        DemandItemTypeIds = DemandItemTypeIds.Order(StringComparer.Ordinal),
        InitialInventory = InitialInventory.OrderBy(stock => stock.ItemTypeId, StringComparer.Ordinal),
        InitialCredits,
        RefuelStockKg,
        SizeFactors = SizeFactors.OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal)
            .Select(pair => new { Size = pair.Key.ToString(), Factor = pair.Value }),
    })));
}
