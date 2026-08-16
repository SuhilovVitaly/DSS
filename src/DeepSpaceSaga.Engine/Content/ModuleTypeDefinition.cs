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
    int? CabinesCount = null) : ITypeDefinition;
