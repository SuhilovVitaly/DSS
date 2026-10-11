namespace DeepSpaceSaga.Contracts;

public enum SessionOutcomeState { Running, Defeated }

/// <summary>Durable session outcome projected on every snapshot, independently of recent
/// command/event buffers and UI modal state. It is not a post-death world save format.
/// DeathMotionTimeMs preserves fractional collision time; calendar time has millisecond resolution.</summary>
public sealed record SessionOutcomeSnapshot(
    SessionOutcomeState State,
    string? PlayerObjectId = null,
    string? WreckObjectId = null,
    long? DeathEventId = null,
    double? DeathMotionTimeMs = null,
    long? DeathGameTimeMs = null)
{
    public static SessionOutcomeSnapshot Running { get; } = new(SessionOutcomeState.Running);
    public bool CanSave => State == SessionOutcomeState.Running;
    public string? SaveDeniedReason => CanSave ? null : CommandReasonCodes.SessionDefeated;
}
