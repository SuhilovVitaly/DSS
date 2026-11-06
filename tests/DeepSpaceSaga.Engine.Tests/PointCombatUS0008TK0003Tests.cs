using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0004TK0001Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0008TK0003Tests
{
    private const string NpcLauncher = "MOD-PIRATE-TORPEDO-01", Engine = "MOD-PLAYER-ENGINE-01";
    private static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition => Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);
    private static SimulationEngine Battle(double distance = 40, bool ai = true, bool enemyFire = false,
        bool lethal = false, SimulationClock? clock = null)
    {
        var source = CounterRegistry(scatter: 0, torpedoScatter: 0);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Select(m => m with { StructurePointsMax = 1000 }),
            All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents),
            combatRules: source.CombatRules! with
            { ShipDestruction = source.CombatRules.ShipDestruction with { DestroyedModuleFraction = lethal ? .01m : .3333m } });
        var scenario = TorpedoImpactTests.Scenario(o => o with
        {
            PositionX = o.ObjectId == Player ? 0 : distance * 10,
            PositionY = 0,
            DirectionDegrees = 0,
            Ai = o.ObjectId == Target && ai ? new(25, 8, 35) : null,
            Modules = o.Modules!.Select(m => m with
            {
                StructurePoints = lethal && o.ObjectId == Target ? 100 : 1000,
                ArmorClass = o.ObjectId == Player ? "light" : "none",
                Armor = o.ObjectId == Player ? 60 : 0,
                PowerState = o.ObjectId == Target && m.ModuleId == NpcLauncher && enemyFire ? "On" : m.PowerState
            }).ToArray()
        });
        var engine = new SimulationEngine(registry, clock: clock); engine.LoadScenario(scenario); return engine;
    }

    [Fact]
    public void Encounter_stats_count_authoritative_launch_hit_and_death_once()
    {
        using var engine = PointCombatUS0008TK0001Tests.Battle();
        var won = PointCombatUS0008TK0001Tests.Win(engine);
        var expected = new CombatEncounterStats(TorpedoesLaunched: 1, EnemyHitsDealt: 1, EnemiesDestroyed: 1);
        Assert.Equal(expected, won.Encounter!.Stats);
        engine.ReceiveCommand(Launch(x: 400, y: 0, profile: "fast")); // Same CommandId: receipt replay.
        Assert.Equal(expected, At(engine, 2500).Encounter!.Stats);
        Assert.Equal(expected, At(engine, 3000).Encounter!.Stats);
        engine.CapturePresentationUpdate(fullSnapshot: true, advanceClock: false);
        Assert.Equal(expected, At(engine, 3000).Encounter!.Stats);
    }

    [Fact]
    public void Enemy_launch_and_actual_own_armor_structure_losses_are_counted_once()
    {
        using var engine = Battle(distance: 30, ai: false, enemyFire: true);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, NpcLauncher, 0, 0, "slow", 0).Disposition);
        var hit = At(engine, 5000);
        // 200 damage / AP20: 60 armor absorbs 60 of the remaining160; structure loses40+100=140.
        var expected = new CombatEncounterStats(EnemyTorpedoesLaunched: 1, PlayerHitsReceived: 1, ArmorDamageTaken: 60, StructureDamageTaken: 140);
        Assert.Equal(expected, hit.Encounter!.Stats);
        Assert.Single(hit.CombatJournal, e => e.Type == CombatEventType.ModuleHit && e.TargetObjectId == Player);
        Assert.Equal(expected, At(engine, 6000).Encounter!.Stats);
        Assert.All(hit.Objects.Where(o => o.ObjectId == Target), o => Assert.True(o.ModuleHealth.IsDefaultOrEmpty));
    }

    [Fact]
    public void Player_counter_counts_only_hostile_torpedoes_among_multiple_shot_down_flights()
    {
        using var engine = Battle(distance: 31, ai: false, enemyFire: true);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, NpcLauncher, 0, 0, "fast", 0).Disposition);
        engine.ReceiveCommand(Launch(x: 160, y: 0, profile: "fast"));
        engine.ReceiveCommand(CounterLaunch(x: 150)); At(engine);
        var result = At(engine, 1000);
        Assert.Equal(2, result.CombatJournal.Count(e => e.Type == CombatEventType.ShotDown));
        var expected = new CombatEncounterStats(TorpedoesLaunched: 1, CountermissilesLaunched: 1, EnemyTorpedoesLaunched: 1, TorpedoesIntercepted: 1);
        Assert.Equal(expected, result.Encounter!.Stats);
        engine.ReceiveCommand(CounterLaunch(x: 150));
        Assert.Equal(expected, At(engine, 1000).Encounter!.Stats);
    }

    [Fact]
    public void Player_evasion_counts_new_episode_but_not_continuation_or_replay()
    {
        using var engine = Battle(distance: 10, ai: false, enemyFire: true);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, NpcLauncher, -600, 0, "slow", 0).Disposition);
        var threat = Assert.Single(At(engine).Objects, o => o.ObservedProjectile is not null).ObjectId;
        var first = new PlayerCommand("evade", 1, Player, Engine, ManeuverCommandTypes.Evade, ThreatProjectileId: threat, EvadeSide: EvadeSide.Left);
        engine.ReceiveCommand(first); At(engine);
        engine.ReceiveCommand(first); At(engine, 100);
        engine.ReceiveCommand(first with { CommandId = "continue", EvadeSide = EvadeSide.Right });
        var continued = At(engine, 500);
        Assert.Equal(new CombatEncounterStats(EnemyTorpedoesLaunched: 1, PlayerEvasions: 1), continued.Encounter!.Stats);
        Assert.Equal(1, engine.CaptureEvasionEpisodeCount());
    }

    [Fact]
    public void Rejected_launch_does_not_increment_any_counter()
    {
        using var engine = Battle(); At(engine);
        engine.ReceiveCommand(Launch(profile: "missing"));
        var rejected = At(engine);
        Assert.Equal(CommandResultStatus.Rejected, Assert.Single(rejected.CommandResults).Status);
        Assert.Equal(new CombatEncounterStats(), rejected.Encounter!.Stats);
        engine.ReceiveCommand(Launch("accepted")); At(engine);
        engine.ReceiveCommand(Launch("while-reloading"));
        var reloading = At(engine);
        Assert.Equal(CommandReasonCodes.LauncherReloading, Assert.Single(reloading.CommandResults).ReasonCode);
        Assert.Equal(new CombatEncounterStats(TorpedoesLaunched: 1), reloading.Encounter!.Stats);
    }

    [Fact]
    public void New_encounter_resets_all_counters()
    {
        using var engine = PointCombatUS0008TK0001Tests.Battle(extraEnemy: true, extraHasAi: false);
        Assert.Equal(1, PointCombatUS0008TK0001Tests.Win(engine).Encounter!.Stats.EnemiesDestroyed);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch("other-enemy", "other-" + NpcLauncher, 1700, 0, "slow", 2500).Disposition);
        Assert.Equal(new CombatEncounterStats(EnemyTorpedoesLaunched: 1), At(engine, 2500).Encounter!.Stats);
    }

    [Fact]
    public void Completed_encounter_flights_and_defense_still_update_stats_after_ack()
    {
        using var engine = Battle(ai: false, enemyFire: true, lethal: true);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, NpcLauncher, 0, 0, "slow", 0).Disposition);
        engine.ReceiveCommand(Launch(x: 400, y: 0, profile: "fast")); At(engine);
        Assert.Equal(CombatEncounterState.Won, At(engine, 2500).Encounter!.State);
        engine.ReceiveCommand(new("ack", 1, Player, "", CombatEncounterCommandTypes.AcknowledgeResults));
        Assert.Equal(CombatEncounterState.Idle, At(engine, 2500).Encounter!.State);
        engine.ReceiveCommand(CounterLaunch(x: 2500.0 / 14)); At(engine, 2500);
        var after = At(engine, 3700);
        Assert.Equal(new CombatEncounterStats(TorpedoesLaunched: 1, CountermissilesLaunched: 1, EnemyTorpedoesLaunched: 1,
            TorpedoesIntercepted: 1, EnemyHitsDealt: 1, EnemiesDestroyed: 1), after.Encounter!.Stats);
        Assert.DoesNotContain(after.Objects, o => o.ObservedProjectile is not null);
    }

    [Fact]
    public void Duration_uses_motion_time_and_excludes_pause()
    {
        long real = 0;
        var clock = new SimulationClock(SimulationSpeed.Speed0, () => real);
        using var engine = Battle(lethal: true, clock: clock);
        engine.CaptureSnapshot();
        Assert.Null(engine.TrySetSpeed(SimulationSpeed.Speed1));
        engine.ReceiveCommand(Launch(x: 400, y: 0, profile: "fast")); engine.CaptureSnapshot();
        real = 4000;
        Assert.Equal(1, engine.CaptureSnapshot(advanceClock: true).Encounter!.DurationSeconds);
        Assert.Null(engine.TrySetSpeed(SimulationSpeed.Speed0));
        real = 100000;
        Assert.Equal(1, engine.CaptureSnapshot(advanceClock: true).Encounter!.DurationSeconds);
        Assert.Null(engine.TrySetSpeed(SimulationSpeed.Speed1));
        real = 106000;
        var won = engine.CaptureSnapshot(advanceClock: true).Encounter!;
        Assert.Equal(CombatEncounterState.Won, won.State);
        Assert.Equal(2.5, won.DurationSeconds);
        Assert.Equal(10, won.DurationSeconds * 4);
        real += 100000;
        Assert.Equal(won, engine.CaptureSnapshot(advanceClock: true).Encounter);
    }
}
