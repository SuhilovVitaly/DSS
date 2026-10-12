using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Scenario;
using Xunit.Abstractions;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ModuleCombatEndToEndTests
{
    private readonly ITestOutputHelper _output;

    public ModuleCombatEndToEndTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData("CombatMukha")]
    [InlineData("CombatShmel")]
    public void Real_scenario_runs_hostile_ai_player_fire_and_module_damage(string scenarioName)
    {
        var result = Run(scenarioName, 1);
        _output.WriteLine(result.ToString());
        Assert.True(result.HostileLaunches > 0, "Hostile AI must launch from the shipped scenario.");
        Assert.True(result.PlayerLaunches > 0, "Player fire must execute through the command path.");
        Assert.True(result.HitRolls > 0, "Real torpedo contacts must use the hit RNG stream.");
        Assert.True(result.ModuleHits > 0, "Contacts must reach module armor/structure.");
        Assert.True(result.Finished, "Combat must reach a terminal outcome within the documented horizon.");
    }

    internal static CombatResult Run(string scenarioName, ulong seed)
    {
        string path = Path.Combine(ClientRoot, "Scenarios", scenarioName, "scenario.json");
        var source = ScenarioLoader.LoadFromFile(path);
        string playerId = source.GameState.PlayerShipObjectId!;
        string[] hostileIds = source.GameState.SpaceObjects.Where(o => o.ObjectType == SpaceObjectType.NpcShip)
            .Select(o => o.ObjectId).ToArray();
        using var engine = new SimulationEngine(Registry.Value);
        engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed } });
        ulong commandSequence = 0;
        AuthoritativeSnapshot snapshot = At(engine, 0);
        for (long time = 0; time <= 600_000; time += 5_000)
        {
            snapshot = At(engine, time);
            if (snapshot.EffectiveOutcome.State == SessionOutcomeState.Defeated ||
                hostileIds.All(id => snapshot.Objects.All(o => o.ObjectId != id)))
                break;
            string? target = hostileIds.FirstOrDefault(id => snapshot.Objects.Any(o => o.ObjectId == id));
            if (target is null) break;
            foreach (var module in snapshot.InstalledModules.Where(m => m.LauncherCombat is not null))
            {
                var launcher = module.LauncherCombat!;
                if (launcher.IsBrokenUntilDock || (launcher.ReadyAtMotionTimeMs ?? 0) > snapshot.MotionTimeMs)
                    continue;
                engine.ReceiveCommand(new PlayerCommand($"balance-{seed}-{++commandSequence}",
                    commandSequence, playerId, module.ModuleId, CombatCommandTypes.Fire, target));
                snapshot = At(engine, time);
            }
        }
        var journal = snapshot.CombatJournal;
        bool defeated = snapshot.EffectiveOutcome.State == SessionOutcomeState.Defeated;
        bool victory = !defeated && hostileIds.All(id => snapshot.Objects.All(o => o.ObjectId != id));
        return new CombatResult(scenarioName, seed, victory, defeated, snapshot.MotionTimeMs,
            journal.Count(e => e.Type == CombatEventType.Launch && e.ActorObjectId == playerId),
            journal.Count(e => e.Type == CombatEventType.Launch && e.ActorObjectId != playerId),
            journal.Count(e => e.Type == CombatEventType.TorpedoHitRoll),
            journal.Count(e => e.Type == CombatEventType.ModuleHit),
            journal.Count(e => e.Type == CombatEventType.Hit && e.TargetObjectId == playerId),
            journal.Count(e => e.Type == CombatEventType.Launch && e.TargetObjectId == playerId && e.ActorObjectId != playerId),
            journal.Count(e => e.Type == CombatEventType.Launch && e.ActorObjectId == playerId && e.LauncherModuleId?.Contains("COUNTERMEASURE", StringComparison.Ordinal) == true));
    }

    internal sealed record CombatResult(string Scenario, ulong Seed, bool Victory, bool Defeat, long DurationMs,
        int PlayerLaunches, int HostileLaunches, int HitRolls, int ModuleHits,
        int PlayerHits, int EnemyTorpedoes, int PlayerCountermeasureLaunches)
    {
        public bool Finished => Victory || Defeat;
    }
}
