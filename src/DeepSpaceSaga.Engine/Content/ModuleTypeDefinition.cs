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
    /// (requirements §59, Docs\FirstRelease\TechnicalTasks\StationEconomyProductionAndSizing.md
    /// "Схема данных"). Null for every module type today — Module buy/sell is explicitly out of
    /// scope for story-20260825-084409 Batch 1 (no authoritative command, no UI); this field only
    /// lays down the data shape a later batch will populate and wire up. See
    /// <c>StationSizeFactors.ModuleNeutralFactor</c> for the accompanying station-size factor
    /// placeholder.
    /// </summary>
    long? BasePriceCredits = null) : ITypeDefinition;
