using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Ordered scene revisions, including revisions inside a publication interval. State contains
/// only dynamic/combat objects and scene-facing fields; FullSnapshot supplies the coarse world.
/// A watermark is usable only after every sequence through Sequence has been received.
/// All physical times are milliseconds; SentAtTimestamp is diagnostic monotonic time only.
/// </summary>
public sealed record CombatPresentationUpdate(
    Guid SessionId, long Sequence, long ConfirmedMotionTimeMs,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<CombatPresentationFrame>))]
    ImmutableArray<CombatPresentationFrame> Frames, AuthoritativeSnapshot? FullSnapshot,
    bool AutomaticPause, long SentAtTimestamp);

public sealed record CombatPresentationFrame(double MotionTimeMs, AuthoritativeSnapshot State);

/// <summary>Queue acceptance, independent of the subsequent CommandResult.</summary>
public sealed record CommandAcceptance(string CommandId, bool Accepted);
public sealed record SimulationControlAcknowledgement(Guid SessionId, long MotionTimeMs,
    SimulationSpeed CurrentSpeed, bool AutomaticPause);

/// <summary>Optional modern session transport. Receipts bypass the delayed scene.</summary>
public interface ICombatPresentationConnection
{
    ValueTask<CommandAcceptance> SubmitCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default);
    ValueTask<SimulationControlAcknowledgement> SetPresentationSpeedAsync(SimulationSpeed speed,
        CancellationToken cancellationToken = default);
    IAsyncEnumerable<CombatPresentationUpdate> ReadCombatUpdatesAsync(CancellationToken cancellationToken = default);
    ValueTask ReportPresentationProgressAsync(Guid sessionId, long contiguousSequence, bool pauseRequested,
        CancellationToken cancellationToken = default);
    ValueTask<SimulationControlAcknowledgement> ResumePresentationAsync(SimulationSpeed speed, CancellationToken cancellationToken = default);
}
