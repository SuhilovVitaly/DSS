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
    int? ApproximateDroneCount = null);

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
}
