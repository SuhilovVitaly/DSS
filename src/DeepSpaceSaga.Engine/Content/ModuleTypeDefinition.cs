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
    long BaseCycleTimeMs = 0,
    long? FuelCapacityKg = null) : ITypeDefinition;
