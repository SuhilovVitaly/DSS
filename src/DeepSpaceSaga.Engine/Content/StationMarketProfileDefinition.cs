using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;

namespace DeepSpaceSaga.Engine.Content;

internal sealed record StationMarketStockDefinition(string ItemTypeId, long Quantity);

/// <summary>Target stock level for one economy-managed cargo item (US-0002 AC-03/05).</summary>
internal sealed record StationMarketTargetDefinition(string ItemTypeId, long TargetStock);

/// <summary>
/// Which mechanism drives a station's hourly production/consumption (US-0002 A-03/A-04):
/// <see cref="Profile"/> runs one shared hourly batch from <see cref="StationMarketEconomyDefinition"/>'s
/// Hourly* lists; <see cref="Modules"/> defers output entirely to the station's existing
/// producing-module recipes and only uses <see cref="StationMarketEconomyDefinition.HourlyConsumption"/>
/// for demand not covered by those recipes. A profile is never both at once.
/// </summary>
internal enum StationMarketProductionSource { Profile, Modules }

/// <summary>
/// Optional bounded-economy configuration for a <see cref="StationMarketProfileDefinition"/>
/// (US-0002 AC-01). Absent on the owning profile means the station stays US-0001
/// bootstrap-only (no hourly flow, no bounded stock). Runtime interpretation of these rates
/// is TK-0003; this type only carries the validated, immutable schema.
/// </summary>
/// <param name="HourlyInputs">
/// Trade units consumed per game hour when <paramref name="ProductionSource"/> is
/// <see cref="StationMarketProductionSource.Profile"/>. Empty (never populated) when
/// <see cref="StationMarketProductionSource.Modules"/> is used.
/// </param>
/// <param name="HourlyOutputs">
/// Trade units produced per game hour when <paramref name="ProductionSource"/> is
/// <see cref="StationMarketProductionSource.Profile"/>; exactly matches the owning profile's
/// SupplyItemTypeIds. Empty (never populated) when <see cref="StationMarketProductionSource.Modules"/>
/// is used — that source produces only via existing recipes.
/// </param>
/// <param name="HourlyConsumption">
/// Trade units consumed per game hour independent of any recipe/profile output — Transit
/// (no production) and ship-visit demand not covered by <see cref="HourlyInputs"/>.
/// </param>
/// <param name="StockTargets">
/// Steady-state stock level per cargo item this profile manages; the set exactly matches the
/// owning profile's InitialInventory items. MaxStock is always 2×TargetStock (AC-03).
/// </param>
/// <param name="ShortageThresholdPermille">
/// Stock strictly below TargetStock×this/1000 is Shortage. Must be between 0 and 1000
/// (exclusive both ends).
/// </param>
/// <param name="SurplusThresholdPermille">
/// Stock strictly above TargetStock×this/1000 is Surplus. Must be between 1000 and 2000
/// (exclusive both ends).
/// </param>
/// <param name="BudgetRegenerationDivisorPerDay">
/// Trading budget regenerates by floor(maxBudget/this) per game day (AC-05); at least 24 so
/// the per-hour slice of a day's regeneration never exceeds one whole day's worth.
/// </param>
internal sealed record StationMarketEconomyDefinition(
    StationMarketProductionSource ProductionSource,
    ImmutableArray<StationMarketStockDefinition> HourlyInputs,
    ImmutableArray<StationMarketStockDefinition> HourlyOutputs,
    ImmutableArray<StationMarketStockDefinition> HourlyConsumption,
    ImmutableArray<StationMarketTargetDefinition> StockTargets,
    int ShortageThresholdPermille,
    int SurplusThresholdPermille,
    int BudgetRegenerationDivisorPerDay);

internal sealed record StationMarketProfileDefinition(
    string TypeId,
    string DisplayName,
    ImmutableArray<string> SupplyItemTypeIds,
    ImmutableArray<string> DemandItemTypeIds,
    ImmutableArray<StationMarketStockDefinition> InitialInventory,
    long InitialCredits,
    long RefuelStockKg,
    ImmutableDictionary<StationSize, int> SizeFactors,
    /// <summary>
    /// Optional bounded-economy schema (US-0002 AC-01). Null means this profile stays
    /// US-0001 bootstrap-only — never 0/defaulted, and never silently synthesized.
    /// </summary>
    StationMarketEconomyDefinition? Economy = null) : ITypeDefinition
{
    // Compute from this record, including copies made with `with`; presentation is not economic state.
    // When Economy is null this must serialize byte-for-byte identically to the US-0001 payload
    // shape (no "Economy" property at all) so every pre-existing fingerprint stays valid — only a
    // configured Economy participates in the hash (US-0002 A-02).
    public string Fingerprint => Convert.ToHexString(Economy is null
        ? SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            TypeId,
            SupplyItemTypeIds = SupplyItemTypeIds.Order(StringComparer.Ordinal),
            DemandItemTypeIds = DemandItemTypeIds.Order(StringComparer.Ordinal),
            InitialInventory = InitialInventory.OrderBy(stock => stock.ItemTypeId, StringComparer.Ordinal),
            InitialCredits,
            RefuelStockKg,
            SizeFactors = SizeFactors.OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal)
                .Select(pair => new { Size = pair.Key.ToString(), Factor = pair.Value }),
        }))
        : SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            TypeId,
            SupplyItemTypeIds = SupplyItemTypeIds.Order(StringComparer.Ordinal),
            DemandItemTypeIds = DemandItemTypeIds.Order(StringComparer.Ordinal),
            InitialInventory = InitialInventory.OrderBy(stock => stock.ItemTypeId, StringComparer.Ordinal),
            InitialCredits,
            RefuelStockKg,
            SizeFactors = SizeFactors.OrderBy(pair => pair.Key.ToString(), StringComparer.Ordinal)
                .Select(pair => new { Size = pair.Key.ToString(), Factor = pair.Value }),
            Economy = new
            {
                Economy.ProductionSource,
                HourlyInputs = Economy.HourlyInputs.OrderBy(stock => stock.ItemTypeId, StringComparer.Ordinal),
                HourlyOutputs = Economy.HourlyOutputs.OrderBy(stock => stock.ItemTypeId, StringComparer.Ordinal),
                HourlyConsumption = Economy.HourlyConsumption.OrderBy(stock => stock.ItemTypeId, StringComparer.Ordinal),
                StockTargets = Economy.StockTargets.OrderBy(target => target.ItemTypeId, StringComparer.Ordinal),
                Economy.ShortageThresholdPermille,
                Economy.SurplusThresholdPermille,
                Economy.BudgetRegenerationDivisorPerDay,
            },
        })));
}
