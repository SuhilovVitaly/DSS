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
    long? CargoCapacityKg,
    int? MaxSpeedMps,
    int? TurnStepDegrees,
    int? LinearInertiaMps2,
    long BaseCycleTimeMs = 0) : ITypeDefinition;
