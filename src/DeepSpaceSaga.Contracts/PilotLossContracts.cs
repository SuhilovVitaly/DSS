using System.Collections.Immutable;

namespace DeepSpaceSaga.Contracts;

/// <summary>Permanent loss record. The captured crew member is never an available recruit.</summary>
public sealed record PilotMemorialEntry(CrewMemberSnapshot Pilot, string BattleId, long GameTimeMs);

public sealed record PilotLossSnapshot(ImmutableArray<PilotMemorialEntry> Memorial, ImmutableArray<string> LiveCrewIds);
