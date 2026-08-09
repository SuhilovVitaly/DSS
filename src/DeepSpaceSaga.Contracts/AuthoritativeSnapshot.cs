using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Authoritative world snapshot produced by the engine.
/// Immutable — safe to share between threads and over the network.
/// SnapshotSequence is monotonically increasing.
/// </summary>
public sealed record AuthoritativeSnapshot(
    ulong SnapshotSequence,
    long GameTimeMs,
    SimulationSpeed CurrentSpeed,
    ImmutableArray<ObjectMotionSnapshot> Objects,
    string? PlayerShipObjectId = null,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<CommandResult>))]
    ImmutableArray<CommandResult> CommandResults = default,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<ShipEvent>))]
    ImmutableArray<ShipEvent> ShipEvents = default);
