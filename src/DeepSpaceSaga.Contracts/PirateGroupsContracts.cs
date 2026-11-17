using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>Observation only: no private patrol orders, drone inventories or random generator state.</summary>
public sealed record PirateGroupSnapshot(
    string Id,
    double X,
    double Y,
    double VelocityXWorldUnitsPerSecond,
    double VelocityYWorldUnitsPerSecond,
    long ObservedMotionTimeMs,
    bool IsDetected,
    string Status,
    int? ApproximateDroneCount = null,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<PirateSignatureObservation>))]
    ImmutableArray<PirateSignatureObservation> Signatures = default,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<PirateMemberSnapshot>))]
    ImmutableArray<PirateMemberSnapshot> Members = default,
    PursuitSnapshot? Pursuit = null,
    long ObservationAgeMotionMs = 0)
{
    public bool IsDebugVisible => !IsDetected && Status == PirateGroupStatuses.DebugVisible;
}

/// <summary>Stable member carrier identity within a single observed world group.</summary>
public sealed record PirateMemberSnapshot(string CarrierId, string SignatureTypeId);

public sealed partial record DemoEncountersSnapshot
{
    [JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<PirateGroupSnapshot>))]
    public ImmutableArray<PirateGroupSnapshot> Groups { get; init; } = [];
}

public static class PirateGroupStatuses
{
    public const string Observed = "observed";
    public const string DebugVisible = "debug_visible";
    public const string Captured = "captured";
    public const string LastKnown = "last_known";
}
