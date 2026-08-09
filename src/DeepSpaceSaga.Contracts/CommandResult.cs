namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Final disposition of a player command after processing by the authoritative
/// engine (requirements §27.1). Published in the next
/// <see cref="AuthoritativeSnapshot"/> — results of commands processed since the
/// previous snapshot.
/// </summary>
/// <remarks>
/// <para>
/// Push-notifications are not required: the client observes command outcomes
/// through the snapshot stream.
/// </para>
/// <para>
/// <c>deferAttemptCount</c> from §27.1 is intentionally NOT present yet: the
/// engine does not track deferred-attempt limits. It will be added together
/// with the §26.5 DeferredLimitExceeded rule.
/// </para>
/// </remarks>
public enum CommandResultStatus
{
    /// <summary>Command started successfully (or was an idempotent no-op / replacement).</summary>
    Executed,

    /// <summary>Command was rejected — it never entered the pending queue (§26.1).</summary>
    Rejected,

    /// <summary>Command was deferred because the module was busy; it stays pending and is retried.</summary>
    Deferred,

    /// <summary>Command was cancelled (future: §26.5 supersession by a newer command).</summary>
    Cancelled,

    /// <summary>Command failed at execution (future: §56.5 success roll).</summary>
    Failed
}

/// <summary>
/// Machine-readable outcome of one processed player command (requirements §27.1).
/// <see cref="ReasonCode"/> is null on success and a snake_case code on failure
/// (style per §56.6, e.g. <c>power_off</c>, <c>no_power</c>).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CommandResultStatus.Deferred"/> is <b>per-processing</b>: if a
/// command stays deferred for N ticks, the engine publishes one Deferred result
/// per tick it was processed (retried and requeued). The final disposition
/// (Executed, Rejected, or Cancelled) appears exactly once in the snapshot where
/// the command leaves the pending queue.
/// </para>
/// <para>
/// Within a single snapshot, each CommandId appears at most once — the engine
/// deduplicates by CommandId during drain, keeping the last disposition.
/// </para>
/// </remarks>
public sealed record CommandResult(
    string CommandId,
    string ObjectId,
    string ModuleId,
    string CommandType,
    CommandResultStatus Status,
    long EffectiveGameTimeMs,
    string? ReasonCode = null);

/// <summary>
/// Machine-readable reason codes for non-executed command results (snake_case,
/// style per requirements §56.6).
/// </summary>
public static class CommandReasonCodes
{
    /// <summary>Command addressed an object that is not the player ship or does not exist.</summary>
    public const string UnknownObject = "unknown_object";

    /// <summary>Command addressed a module that does not exist on the target object.</summary>
    public const string UnknownModule = "unknown_module";

    /// <summary>Command type is not supported by the target module.</summary>
    public const string UnknownCommandType = "unknown_command_type";

    /// <summary>Module exists but cannot execute commands (power Off / Disabled / no structure points / no inertia).</summary>
    public const string ModuleUnavailable = "module_unavailable";

    /// <summary>Module is busy with an active non-auto-repeat cycle; command deferred.</summary>
    public const string Busy = "busy";

    /// <summary>Match command (e.g. match-target-speed) arrived without the required targetObjectId (§56.9).</summary>
    public const string MissingTarget = "missing_target";

    /// <summary>Match command referenced a targetObjectId that does not exist in the world (§56.9).</summary>
    public const string UnknownTarget = "unknown_target";
}
