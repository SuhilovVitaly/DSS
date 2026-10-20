using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0003TK0003Tests
{
    private static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition =>
        Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);

    private static SimulationEngine Battle(double playerDistanceKm = 0, bool extraEnemy = false, int damage = 200,
        int health = 1000, long reload = 10000, bool dieOnFirstHit = false, int playerSpeedMps = 0, bool extraHasClass = true)
    {
        var source = Registry(scatter: 0);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Select(m => m with
        {
            StructurePointsMax = health,
            TorpedoDamage = m.TorpedoDamage is null ? null : damage,
            ReloadMs = m.IsPointLauncher ? reload : m.ReloadMs
        }), All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes),
            All(source.Dialogues), All(source.Quests), stationMarketProfiles: All(source.StationMarketProfiles),
            shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents),
            combatRules: source.CombatRules! with
            {
                Damage = source.CombatRules.Damage with { HitDisableMs = 8000 },
                ShipDestruction = source.CombatRules.ShipDestruction with
                { DestroyedModuleFraction = dieOnFirstHit ? 0.01m : source.CombatRules.ShipDestruction.DestroyedModuleFraction }
            });
        var scenario = TorpedoImpactTests.Scenario(o => o with
        {
            Ai = null,
            PositionX = o.ObjectId == Player ? playerDistanceKm * 10 : 0,
            PositionY = 0,
            SpeedMps = o.ObjectId == Player ? playerSpeedMps : 0,
            DirectionDegrees = 90,
            MovementType = o.ObjectId == Player && playerSpeedMps > 0 ? "Linear" : "Stationary",
            Modules = o.Modules!.Select(m => m with { StructurePoints = health, Armor = 0, ArmorClass = "none" }).ToArray()
        });
        if (extraEnemy)
        {
            var enemy = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == Target);
            var extra = enemy with
            {
                ObjectId = "enemy-b",
                ShipClassId = extraHasClass ? enemy.ShipClassId : null,
                PositionY = 10,
                Crew = enemy.Crew!.Select(c => c with { CrewId = "extra-" + c.CrewId }).ToArray(),
                Modules = enemy.Modules!.Select(m => m with
                {
                    ModuleId = "extra-" + m.ModuleId,
                    OperatorCrewId = m.OperatorCrewId is null ? null : "extra-" + m.OperatorCrewId
                }).ToArray()
            };
            scenario = scenario with { GameState = scenario.GameState with { SpaceObjects = scenario.GameState.SpaceObjects.Append(extra).ToArray() } };
        }
        var engine = new SimulationEngine(registry);
        engine.LoadScenario(scenario);
        return engine;
    }

    private static AuthoritativeSnapshot At(SimulationEngine engine, long time) => engine.CaptureSnapshotForTests(time * 4, simulationTimeMs: time);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Area_blast_hits_all_ships_and_friends_once(bool extraHasClass)
    {
        using var engine = Battle(extraEnemy: true, extraHasClass: extraHasClass);
        engine.ReceiveCommand(Launch(x: 0));
        var paused = At(engine, 0);
        Assert.DoesNotContain(paused.CombatJournal, e => e.Type == CombatEventType.Hit);
        Assert.Single(paused.Objects, o => o.OwnProjectile is not null);
        var snapshot = At(engine, 1);
        Assert.DoesNotContain(snapshot.Objects, o => o.OwnProjectile is not null);
        var hits = snapshot.CombatJournal.Where(e => e.Type == CombatEventType.Hit).ToArray();
        Assert.Equal(new[] { Player, Target, "enemy-b" }, hits.Select(e => e.TargetObjectId));
        Assert.All(hits, e => Assert.Equal(ProjectileHitResult.Core, e.HitResult));
        var saved = engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0);
        Assert.Equal(3UL, saved.GameState.CombatState!.HitLocationRngCounter);
        foreach (var ship in saved.GameState.SpaceObjects.Where(o => o.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip))
        {
            var damaged = Assert.Single(ship.Modules!, m => m.StructurePoints < 1000);
            Assert.Equal(800, damaged.StructurePoints);
            Assert.Equal(8000, damaged.DisabledUntilMotionTimeMs);
        }
        foreach (var enemy in snapshot.Objects.Where(o => o.ObjectType == SpaceObjectType.NpcShip))
        {
            Assert.Empty(enemy.ModuleHealth);
            Assert.Null(enemy.Defense);
            Assert.True(enemy.EnemyStatus!.Damaged);
        }
        Assert.All(hits.Where(e => e.TargetObjectId != Player), e =>
        {
            Assert.Null(e.Damage); Assert.Null(e.HitModuleId); Assert.Null(e.ArmorBefore); Assert.Null(e.StructureAfter);
        });
        Assert.Equal(3UL, engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0).GameState.CombatState!.HitLocationRngCounter);
    }

    [Theory]
    [InlineData(0, 200)]
    [InlineData(0.75, 200)]
    [InlineData(1.625, 100)]
    [InlineData(2.5, 0)]
    [InlineData(3, 0)]
    [InlineData(2.499, 0)]
    public void Blast_core_edge_and_zero_damage(double distanceKm, int expected)
    {
        // The player fires at (0,0), remaining at a measured offset; fast R=2.5 also damages the owner.
        using var engine = Battle(playerDistanceKm: distanceKm);
        engine.ReceiveCommand(Launch(x: 0, profile: "fast"));
        At(engine, 0);
        long end = (long)Math.Ceiling(distanceKm / 16 * 1000) + 1;
        var snapshot = At(engine, end);
        var ship = engine.CaptureSaveStateForTests(end, SimulationSpeed.Speed0).GameState.SpaceObjects.Single(o => o.ObjectId == Player);
        Assert.Equal(expected, ship.Modules!.Sum(m => 1000 - m.StructurePoints));
        if (expected == 0)
        {
            Assert.All(ship.Modules!, m => Assert.Null(m.DisabledUntilMotionTimeMs));
            Assert.DoesNotContain(snapshot.CombatJournal, e => e.Type == CombatEventType.Hit && e.TargetObjectId == Player);
        }
        else
        {
            var hit = Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.Hit && e.TargetObjectId == Player);
            Assert.Equal(expected, hit.Damage);
            Assert.Equal(expected == 200 ? ProjectileHitResult.Core : ProjectileHitResult.Edge, hit.HitResult);
        }
        // The hostile at the centre always consumes exactly one hit-location draw; player adds one only for positive rounded damage.
        Assert.Equal(expected == 0 ? 1UL : 2UL, engine.CaptureSaveStateForTests(end, SimulationSpeed.Speed0).GameState.CombatState!.HitLocationRngCounter);
    }

    [Fact]
    public void Blast_order_and_partition_are_deterministic()
    {
        using var whole = Battle(playerDistanceKm: 1.625);
        using var split = Battle(playerDistanceKm: 1.625);
        foreach (var engine in new[] { whole, split }) { engine.ReceiveCommand(Launch(x: 0, profile: "fast")); At(engine, 0); }
        var expected = At(whole, 200);
        foreach (long time in new long[] { 1, 7, 33, 100, 101, 150, 199 }) At(split, time);
        var actual = At(split, 200);
        Assert.Equal(JsonSerializer.Serialize(expected.CombatJournal), JsonSerializer.Serialize(actual.CombatJournal));
        Assert.Equal(JsonSerializer.Serialize(expected.Objects), JsonSerializer.Serialize(actual.Objects));
        Assert.Equal(JsonSerializer.Serialize(whole.CaptureSaveStateForTests(200, SimulationSpeed.Speed0).GameState.CombatState),
            JsonSerializer.Serialize(split.CaptureSaveStateForTests(200, SimulationSpeed.Speed0).GameState.CombatState));
        var detonation = Assert.Single(expected.CombatJournal, e => e.Type == CombatEventType.TorpedoDetonated);
        Assert.Equal(101.5625, detonation.MotionTimeMs);
        Assert.Equal(0, detonation.X);
        Assert.Equal(0, detonation.Y);
    }

    [Fact]
    public void Midpoint_damage_rounds_away_from_zero()
    {
        using var engine = Battle(playerDistanceKm: 1.625, damage: 201);
        engine.ReceiveCommand(Launch(x: 0, profile: "fast"));
        At(engine, 0);
        var snapshot = At(engine, 200);
        Assert.Equal(101, Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.Hit && e.TargetObjectId == Player).Damage);
    }

    [Fact]
    public void Simultaneous_torpedoes_resolve_in_ordinal_not_numeric_id_order()
    {
        using var engine = Battle(damage: 1, reload: 0);
        for (int i = 0; i < 12; i++) engine.ReceiveCommand(Launch("shot-" + i, x: 0) with { ClientSequence = (ulong)i + 1 });
        Assert.Equal(12, At(engine, 0).Objects.Count(o => o.OwnProjectile is not null));
        var snapshot = At(engine, 1);
        Assert.Equal(new[] { "torpedo-1", "torpedo-10", "torpedo-11", "torpedo-12", "torpedo-2", "torpedo-3",
            "torpedo-4", "torpedo-5", "torpedo-6", "torpedo-7", "torpedo-8", "torpedo-9" },
            snapshot.CombatJournal.Where(e => e.Type == CombatEventType.TorpedoDetonated).Select(e => e.ProjectileObjectId));
        Assert.Equal(24, snapshot.CombatJournal.Count(e => e.Type == CombatEventType.Hit));
        Assert.Equal(24UL, engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0).GameState.CombatState!.HitLocationRngCounter);
    }

    [Fact]
    public void Simultaneous_ship_deaths_follow_all_area_hits_and_preserve_terminal_player_outcome()
    {
        using var engine = Battle(health: 100, dieOnFirstHit: true);
        engine.ReceiveCommand(Launch(x: 0));
        At(engine, 0);
        var snapshot = At(engine, 1);
        Assert.Equal(SessionOutcomeState.Defeated, snapshot.EffectiveOutcome.State);
        Assert.Equal(0, snapshot.EffectiveOutcome.DeathMotionTimeMs);
        Assert.Equal(2, snapshot.Objects.Count(o => o.ObjectType == SpaceObjectType.Wreck));
        var hits = snapshot.CombatJournal.Where(e => e.Type == CombatEventType.Hit).ToArray();
        var deaths = snapshot.CombatJournal.Where(e => e.Type == CombatEventType.ShipDestroyed).ToArray();
        Assert.Equal(2, hits.Length);
        Assert.Equal(2, deaths.Length);
        Assert.True(hits.Max(e => e.EventId) < deaths.Min(e => e.EventId));
        Assert.Null(deaths.Single(e => e.TargetObjectId == Target).DestroyReason);
        Assert.NotNull(deaths.Single(e => e.TargetObjectId == Player).DestroyReason);
    }

    [Fact]
    public void Moving_ship_is_sampled_at_fractional_detonation_time()
    {
        using var engine = Battle(playerDistanceKm: 1.625, playerSpeedMps: 1000);
        engine.ReceiveCommand(Launch(x: 0, profile: "fast"));
        At(engine, 0);
        var snapshot = At(engine, 200);
        var hit = Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.Hit && e.TargetObjectId == Player);
        Assert.Equal(101.5625, hit.MotionTimeMs);
        // Ship moves 0.1015625km before the captured deadline: distance=1.7265625, rounded edge damage=88.
        Assert.Equal(88, hit.Damage);
        var damaged = Assert.Single(snapshot.Objects.Single(o => o.ObjectId == Player).ModuleHealth, m => m.DisabledUntilMotionTimeMs is not null);
        Assert.Equal(8101.5625, damaged.DisabledUntilMotionTimeMs);
    }
}
