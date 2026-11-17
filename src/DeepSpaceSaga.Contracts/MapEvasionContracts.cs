namespace DeepSpaceSaga.Contracts;

/// <summary>Observed relative motion only. Private last-known targets and patrol waypoints stay in Engine.</summary>
public sealed record PursuitSnapshot(string GroupId, string EventId, string ObservedStatus,
    double RangeWorldUnits, double ClosingSpeedWorldUnitsPerSecond, long ObservedMotionTimeMs,
    string? EndReason = null);

public static class PursuitStatuses
{
    public const string Approaching = "approaching";
    public const string Receding = "receding";
    public const string Captured = "captured";
    public const string Escaped = "escaped";
}
