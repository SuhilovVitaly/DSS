using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0008TK0001Tests
{
    internal const string NpcLauncher = "MOD-PIRATE-TORPEDO-01";
    internal static AuthoritativeSnapshot At(SimulationEngine engine, long time = 0) => engine.CaptureSnapshotForTests(time * 4, simulationTimeMs: time);
    private static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition => Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);
    internal static SimulationEngine Battle(double distanceKm = 40, bool ai = true, bool enemyLauncherOn = false,
        double enemySpeed = 0, bool extraEnemy = false, bool extraHasAi = true)
    {
        var source = Registry(scatter: 0);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes), All(source.ItemTypes),
            All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents),
            combatRules: source.CombatRules! with
            { ShipDestruction = source.CombatRules.ShipDestruction with { DestroyedModuleFraction = .01m } });
        var scenario = TorpedoImpactTests.Scenario(o => o with
        {
            Ai = o.ObjectId == Target && ai ? new(25, 8, 35) : null,
            PositionX = o.ObjectId == Player ? 0 : distanceKm * 10,
            PositionY = 0,
            SpeedMps = o.ObjectId == Player ? 0 : enemySpeed * 1000,
            DirectionDegrees = 270,
            MovementType = "Linear",
            Modules = o.Modules!.Select(m => m with
            {
                StructurePoints = 100,
                Armor = 0,
                ArmorClass = "none",
                PowerState = o.ObjectId == Target && m.ModuleId == NpcLauncher && enemyLauncherOn ? "On" : m.PowerState
            }).ToArray()
        });
        if (extraEnemy)
        {
            var npc = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == Target);
            var extra = npc with
            {
                ObjectId = "other-enemy",
                PositionX = 2000,
                PositionY = 0,
                Ai = extraHasAi ? new(25, 8, 35) : null,
                Crew = npc.Crew!.Select(c => c with { CrewId = "other-" + c.CrewId }).ToArray(),
                Modules = npc.Modules!.Select(m => m with
                {
                    ModuleId = "other-" + m.ModuleId,
                    OperatorCrewId = m.OperatorCrewId is null ? null : "other-" + m.OperatorCrewId,
                    PowerState = m.ModuleId == NpcLauncher ? "On" : m.PowerState
                }).ToArray()
            };
            scenario = scenario with { GameState = scenario.GameState with { SpaceObjects = scenario.GameState.SpaceObjects.Append(extra).ToArray() } };
        }
        var engine = new SimulationEngine(registry);
        engine.LoadScenario(scenario);
        return engine;
    }

    internal static AuthoritativeSnapshot Win(SimulationEngine engine)
    {
        engine.ReceiveCommand(Launch(x: 400, y: 0, profile: "fast")); At(engine);
        return At(engine, 2500);
    }

    [Theory]
    [InlineData(99.9, true, CombatEncounterState.Active)]
    [InlineData(100, true, CombatEncounterState.Active)]
    [InlineData(100.1, true, CombatEncounterState.Idle)]
    [InlineData(40, false, CombatEncounterState.Idle)]
    public void Encounter_enters_active_only_at_ai_range_admission(double distance, bool ai, CombatEncounterState expected)
    {
        using var engine = Battle(distance, ai);
        var snapshot = At(engine);
        Assert.Equal(expected, snapshot.Encounter!.State);
        Assert.Equal(SessionOutcomeState.Running, snapshot.EffectiveOutcome.State);
    }

    [Fact]
    public void First_hostile_launch_starts_encounter_beyond_range_even_while_paused()
    {
        using var engine = Battle(distanceKm: 200, ai: false, enemyLauncherOn: true);
        Assert.Equal(CombatEncounterState.Idle, At(engine).Encounter!.State);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, NpcLauncher, 1700, 0, "slow", 0).Disposition);
        var result = At(engine);
        Assert.Equal(CombatEncounterState.Active, result.Encounter!.State);
        Assert.Equal(0, result.Encounter.StartedAtMotionMs);
        Assert.Equal(SimulationSpeed.Speed0, result.CurrentSpeed);
    }

    [Fact]
    public void Range_admission_time_is_independent_of_snapshot_partitioning()
    {
        using var whole = Battle(100.1, enemySpeed: 1); using var split = Battle(100.1, enemySpeed: 1);
        At(whole); At(split);
        foreach (long time in new long[] { 17, 89, 101, 700 }) At(split, time);
        var a = At(whole, 1000).Encounter!; var b = At(split, 1000).Encounter!;
        Assert.Equal(CombatEncounterState.Active, a.State);
        Assert.Equal(100, a.StartedAtMotionMs);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Encounter_finishes_once_after_all_hostile_ai_ships_die()
    {
        using var engine = Battle();
        var won = Win(engine);
        Assert.Equal(CombatEncounterState.Won, won.Encounter!.State);
        Assert.Equal(2500, won.Encounter.EndedAtMotionMs);
        Assert.Equal(SessionOutcomeState.Running, won.EffectiveOutcome.State);
        Assert.Equal(won.Encounter, At(engine, 4000).Encounter);
        using var more = Battle(extraEnemy: true);
        Assert.Equal(CombatEncounterState.Active, Win(more).Encounter!.State);
    }

    [Fact]
    public void Encounter_summary_ack_is_idempotent_and_does_not_affect_terminal_outcome()
    {
        using var engine = Battle(); Win(engine);
        var command = new PlayerCommand("ack", 1, Player, "", CombatEncounterCommandTypes.AcknowledgeResults);
        engine.ReceiveCommand(command);
        Assert.Equal(CombatEncounterState.Idle, At(engine, 2500).Encounter!.State);
        engine.ReceiveCommand(command);
        var repeat = At(engine, 2500);
        Assert.Equal(CombatEncounterState.Idle, repeat.Encounter!.State);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(repeat.CommandResults).Status);
        Assert.Equal(SessionOutcomeState.Running, repeat.EffectiveOutcome.State);
    }

    [Fact]
    public void Won_timeout_uses_sixty_physical_seconds_and_pause_does_not_advance_it()
    {
        using var engine = Battle(); Win(engine);
        for (int i = 0; i < 4; i++)
            Assert.Equal(CombatEncounterState.Won, engine.CaptureSnapshotForTests(1_000_000 + i, simulationTimeMs: 2500).Encounter!.State);
        Assert.Equal(CombatEncounterState.Won, At(engine, 62499).Encounter!.State);
        Assert.Equal(CombatEncounterState.Idle, At(engine, 62500).Encounter!.State);
    }

    [Fact]
    public void Live_foreign_projectile_continues_after_won()
    {
        using var engine = Battle(ai: false, enemyLauncherOn: true);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, NpcLauncher, 700, 0, "slow", 0).Disposition);
        var won = Win(engine);
        Assert.Equal(CombatEncounterState.Won, won.Encounter!.State);
        Assert.Single(won.Objects, o => o.ObservedProjectile is not null);
        var after = At(engine, 5000);
        Assert.DoesNotContain(after.Objects, o => o.ObservedProjectile is not null);
        Assert.Equal(CombatEncounterState.Won, after.Encounter!.State);
    }

    [Fact]
    public void New_hostile_launch_restarts_after_won_and_late_ack_cannot_dismiss_it()
    {
        using var engine = Battle(extraEnemy: true, extraHasAi: false);
        Assert.Equal(CombatEncounterState.Won, Win(engine).Encounter!.State);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch("other-enemy", "other-" + NpcLauncher, 1700, 0, "slow", 2500).Disposition);
        engine.ReceiveCommand(new("old-ack", 1, Player, "", CombatEncounterCommandTypes.AcknowledgeResults));
        var active = At(engine, 2500);
        Assert.Equal(CombatEncounterState.Active, active.Encounter!.State);
        Assert.Equal(2500, active.Encounter.StartedAtMotionMs);
    }

    [Fact]
    public void Player_death_after_won_overrides_victory_and_preserves_terminal_defeat()
    {
        using var engine = Battle(ai: false, enemyLauncherOn: true);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, NpcLauncher, 0, 0, "slow", 0).Disposition);
        Assert.Equal(CombatEncounterState.Won, Win(engine).Encounter!.State);
        var lost = At(engine, 7000);
        Assert.Equal(CombatEncounterState.Lost, lost.Encounter!.State);
        Assert.Equal(SessionOutcomeState.Defeated, lost.EffectiveOutcome.State);
        Assert.Equal(SimulationSpeed.Speed0, lost.CurrentSpeed);
        engine.ReceiveCommand(new("ack", 1, Player, "", CombatEncounterCommandTypes.AcknowledgeResults));
        var repeat = At(engine, 10000);
        Assert.Equal(lost.Encounter, repeat.Encounter);
        Assert.Equal(CommandReasonCodes.SessionDefeated, Assert.Single(repeat.CommandResults).ReasonCode);
    }

    [Fact]
    public void Simultaneous_final_enemy_and_player_deaths_end_lost()
    {
        using var engine = Battle(distanceKm: 0);
        engine.ReceiveCommand(Launch(x: 0, y: 0, profile: "fast")); At(engine);
        var result = At(engine, 1);
        Assert.Equal(2, result.Objects.Count(o => o.ObjectType == SpaceObjectType.Wreck));
        Assert.Equal(CombatEncounterState.Lost, result.Encounter!.State);
        Assert.Equal(SessionOutcomeState.Defeated, result.EffectiveOutcome.State);
    }
}
