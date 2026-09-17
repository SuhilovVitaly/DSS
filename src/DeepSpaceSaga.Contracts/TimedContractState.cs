using System.Collections.Immutable;

namespace DeepSpaceSaga.Contracts;

/// <summary>Absolute contract timing and payment commitments, independent of UI and wall time.</summary>
public sealed record TimedContractState(
    string ContractId, long DeadlineGameTimeMs, long ExpectedPayout,
    string DestinationStationObjectId, ImmutableArray<string> PassengerIds, bool DeadlineMissed = false);
