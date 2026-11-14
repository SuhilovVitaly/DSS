using System.Collections.Immutable;

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
    /// (requirements §59, DB\Documentation\02-FirstRelease\TechnicalTasks\StationEconomyProductionAndSizing.md
    /// "Схема данных"). Null for every module type today — Module buy/sell is explicitly out of
    /// scope for story-20260825-084409 Batch 1 (no authoritative command, no UI); this field only
    /// lays down the data shape a later batch will populate and wire up. See
    /// <c>StationSizeFactors.ModuleNeutralFactor</c> for the accompanying station-size factor
    /// placeholder.
    /// </summary>
    long? BasePriceCredits = null,
    string? CategoryTypeId = null,
    long? FuelEfficiencyKmPerKg = null,
    /// <summary>
    /// Number of drones this module can house when its owning category is a hangar bay
    /// (module.drone-bay). Null for module types that are not hangar bays. Non-negative
    /// when present.
    /// </summary>
    int? DroneCapacity = null,
    /// <summary>Rectangular hangar footprint width in hull cells (positive when present).</summary>
    int? FootprintWidth = null,
    /// <summary>Rectangular hangar footprint height in hull cells (positive when present).</summary>
    int? FootprintHeight = null) : ITypeDefinition;
