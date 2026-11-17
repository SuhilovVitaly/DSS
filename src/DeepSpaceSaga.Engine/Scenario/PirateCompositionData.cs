using System.Collections.Immutable;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Private, materialized exactly once; never included in a public world observation.</summary>
public sealed record PirateDroneData(string DroneId, string LayoutId);
public sealed record PirateCompositionData(string GroupId, string CarrierId, string SignatureTypeId,
    int GenerationVersion, ImmutableArray<PirateDroneData> Drones);
