using System.Collections.Immutable;
using System.Text.Json.Serialization;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Content;

internal sealed record ModuleTypeDefinition(
    string TypeId,
    string DisplayName,
    int SlotSize,
    long MassKg,
    int StructurePointsMax,
    long PowerConsumptionW,
    ImmutableArray<string> CommandTypeIds,
    long? CargoCapacityKg = null,
    int? MaxSpeedMps = null,
    int? TurnStepDegrees = null,
    int? LinearInertiaMps2 = null,
    int? AngularInertiaDegPerSec = null,
    long BaseCycleTimeMs = 0,
    long? FuelCapacityKg = null,
    int BaseSuccessChancePercent = 100,
    /// <summary>
    /// Number of crew cabins provided by this module type (e.g. living quarters).
    /// Null for module types that do not house crew.
    /// </summary>
    int? CabinesCount = null,
    /// <summary>
    /// Base Credits price of this module type at a station-side Module trade/service catalog
    /// (requirements §59, Documentation\02-FirstRelease\TechnicalTasks\StationEconomyProductionAndSizing.md
    /// "Схема данных"). Null for every module type today — Module buy/sell is explicitly out of
    /// scope for story-20260825-084409 Batch 1 (no authoritative command, no UI); this field only
    /// lays down the data shape a later batch will populate and wire up. See
    /// <c>StationSizeFactors.ModuleNeutralFactor</c> for the accompanying station-size factor
    /// placeholder.
    /// </summary>
    long? BasePriceCredits = null,
    /// <summary>Hull damage per contact in integer hit points; null for non-launchers.</summary>
    int? TorpedoDamage = null,
    /// <summary>Projectile's own constant speed in km/s, without carrier velocity.</summary>
    double? TorpedoSpeedKmS = null,
    /// <summary>Positive maximum projectile turn rate in degrees per physical second.</summary>
    double? TorpedoTurnRateDegPerSec = null,
    double? CountermeasureSpeedKmS = null,
    double? CountermeasureTurnRateDegPerSec = null,
    double? CountermeasureRangeKm = null,
    long? CountermeasureReloadMs = null,
    string? CategoryTypeId = null,
    long? FuelEfficiencyKmPerKg = null,
    decimal? TorpedoManeuverability = null,
    double? TorpedoRangeKm = null,
    decimal? CountermeasureAccuracy = null,
    double? CountermeasureManualRangeKm = null,
    long? CountermeasureMaxFlightTimeMs = null,
    decimal? Accuracy = null,
    decimal? ArmorPiercingPercent = null,
    decimal? ArmorDamagePercent = null,
    long? LauncherReloadMs = null,
    long? TorpedoMaxFlightTimeMs = null,
    int? Barrels = null,
    long? BurstIntervalMs = null,
    decimal? Maneuverability = null,
    string? CombatRole = null,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<LaunchProfileSnapshot>))]
    ImmutableArray<LaunchProfileSnapshot> Profiles = default,
    double? RangeKm = null,
    long? ReloadMs = null,
    double? ScatterPercent = null) : ITypeDefinition
{
    internal bool IsPointLauncher => !Profiles.IsDefault || RangeKm.HasValue || ReloadMs.HasValue || ScatterPercent.HasValue;
}
