using DeepSpaceSaga.Contracts;
using System.Collections.Immutable;

namespace DeepSpaceSaga.Engine.Scenario;

public sealed record WorldEncounterData(WorldBattleBinding Binding, long Seed, string Phase);
public sealed record WorldEncounterResultData(WorldBattleBinding Binding, bool PlayerWon,
    TacticalOutcomeSnapshot Outcome, ImmutableArray<TacticalUnitSnapshot> Units);
