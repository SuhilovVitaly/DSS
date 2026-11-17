using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Scenario;

public sealed record WorldEncounterData(WorldBattleBinding Binding, long Seed, string Phase);
