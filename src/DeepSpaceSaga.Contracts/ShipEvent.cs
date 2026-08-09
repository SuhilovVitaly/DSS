namespace DeepSpaceSaga.Contracts;

/// <summary>
/// One authoritative ship event published in
/// <see cref="AuthoritativeSnapshot.ShipEvents"/> — minimal
/// watch-log channel (requirements §27.2, §56.6). Events are
/// emitted by the engine during cycle completion and
/// cancellation/interruption; the client formats and filters
/// them for display.
/// </summary>
/// <remarks>
/// <para>
/// <c>category</c> and <c>parameters</c> from §27.2 are not
/// included yet — they belong to a future UI log iteration.
/// </para>
/// <para>
/// <see cref="ReasonCode"/> is null on success (completed
/// cycles) and a snake_case code for cancellation/interruption
/// (style per §56.6).
/// </para>
/// </remarks>
public sealed record ShipEvent(
    string EventId,
    string ObjectId,
    string ModuleId,
    string EventType,
    string? ReasonCode,
    long GameTimeMs);

/// <summary>
/// Well-known ship event type codes (snake_case, requirements §27.2).
/// </summary>
public static class ShipEventTypes
{
    /// <summary>An engine command cycle completed successfully.</summary>
    public const string CommandCompleted = "command_completed";

    /// <summary>An active cycle was cancelled by a CancelAll or implicit-cancel command.</summary>
    public const string CycleCancelled = "cycle_cancelled";

    /// <summary>An active cycle was interrupted because the module became unavailable
    /// (power loss, disabled, destroyed, or incompatible state).</summary>
    public const string CycleInterrupted = "cycle_interrupted";
}

/// <summary>
/// Machine-readable reason codes for cancelled and interrupted
/// ship events (snake_case, style per requirements §56.6).
/// Null reason means success (<see cref="ShipEventTypes.CommandCompleted"/>).
/// </summary>
public static class ShipEventReasonCodes
{
    /// <summary>Cycle cancelled by an explicit CancelAll command or by implicit
    /// cancellation when a new one-shot command supersedes an auto-repeat.</summary>
    public const string CancelledByCommand = "cancelled_by_command";

    /// <summary>Module power state is "Off" (requirements §56.6).</summary>
    /// <remarks>Defined but unreachable in the current simulation:
    /// <c>PowerState</c> supports only <c>"On"</c>/<c>"Off"</c>
    /// and there is no energy simulation yet.</remarks>
    public const string PowerOff = "power_off";

    /// <summary>Module has zero energy budget (requirements §56.6).
    /// Defined but unreachable: energy simulation is not implemented yet.</summary>
    public const string NoPower = "no_power";

    /// <summary>Module structure points reached zero (requirements §56.6).</summary>
    public const string ModuleDestroyed = "module_destroyed";

    /// <summary>Module operational state is "Disabled" or "Damaged"
    /// (requirements §56.6).</summary>
    public const string ModuleDisabled = "module_disabled";

    /// <summary>Fallback: module is in an incompatible state not covered by the
    /// specific codes above (e.g. module configuration changed at runtime so the
    /// command type is no longer supported).</summary>
    public const string IncompatibleState = "incompatible_state";
}
