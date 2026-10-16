using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public class HostileTorpedoFireTests
{
    internal static ScenarioFile HostileScenario(double distance = 1000) => TorpedoImpactTests.Scenario(o => o with
    {
        PositionY = o.ObjectId == Player ? 0 : -distance,
        RelationToPlayer = o.ObjectId == Target ? PlayerRelation.Enemy : null,
        Modules = o.Modules!.Select(m => m with { AutoDefenseEnabled = false, PowerState = m.ModuleTypeId == "module.engine.basic" ? "Off" : "On" }).ToArray()
    });

    [Fact]
    public void All_enemy_ships_fire_neutrals_do_not()
    {
        var source = HostileScenario();
        var pirate = source.GameState.SpaceObjects.Single(o => o.ObjectId == Target);
        SpaceObjectData Clone(string id, string relation, double x) => pirate with
        {
            ObjectId = id,
            PositionX = x,
            PositionY = 0,
            RelationToPlayer = relation,
            Crew = pirate.Crew!.Select(c => c with { CrewId = id + c.CrewId }).ToArray(),
            Modules = pirate.Modules!.Select(m => m with
            {
                ModuleId = id + m.ModuleId,
                OperatorCrewId = m.OperatorCrewId is null ? null : id + m.OperatorCrewId
            }).ToArray()
        };
        using var engine = Create(source with
        {
            GameState = source.GameState with
            {
                SpaceObjects = source.GameState.SpaceObjects.Concat(new[] { Clone("enemy-b", PlayerRelation.Enemy, 1000),
                Clone("neutral", PlayerRelation.Neutral, 500), Clone("friend", PlayerRelation.Friend, -500) }).ToArray()
            }
        });
        var snapshot = At(engine, 1251);
        var flights = snapshot.Objects.Where(o => o.Torpedo is not null).Select(o => o.Torpedo!).ToArray();
        Assert.Equal(new[] { Target, "enemy-b" }.Order(), flights.Select(f => f.OwnerObjectId).Order());
        Assert.All(flights, f => { Assert.Equal(Player, f.TargetObjectId); Assert.InRange(f.LaunchMotionTimeMs, 501, 1251); });
        Assert.Empty(snapshot.CommandResults);
        Assert.Empty(engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0).GameState.CombatState!.ProcessedLaunchCommandIds);
    }

    [Fact]
    public void Pause_has_no_auto_fire()
    {
        using var engine = Create(HostileScenario());
        for (int i = 0; i < 5; i++) Assert.DoesNotContain(At(engine, 0, i * 1000).Objects, o => o.Torpedo is not null);
        Assert.Single(At(engine, 1251, 5000).Objects, o => o.Torpedo is not null);
    }

    [Fact]
    public void Range_entry_inside_large_interval_waits_for_reaction()
    {
        var scenario = HostileScenario(2100);
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o.ObjectId == Target
                    ? o with { SpeedMps = 1000, DirectionDegrees = 180, MovementType = "Linear" } : o).ToArray()
            }
        };
        using var engine = Create(scenario);
        Assert.DoesNotContain(At(engine, 9999).Objects, o => o.Torpedo is not null);
        var flight = Assert.Single(At(engine, 12000).Objects, o => o.Torpedo is not null).Torpedo!;
        Assert.InRange(flight.LaunchMotionTimeMs, 10500, 11250);
    }

    [Theory]
    [InlineData("off")]
    [InlineData("broken")]
    [InlineData("operator")]
    public void Unavailable_hostile_weapon_does_not_launch(string cause)
    {
        var scenario = HostileScenario();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o.ObjectId != Target ? o : o with
                {
                    Modules = o.Modules!.Select(m => m.ModuleTypeId != "module.torpedo.launcher.basic" ? m : m with
                    {
                        PowerState = cause == "off" ? "Off" : m.PowerState,
                        StructurePoints = cause == "broken" ? 0 : m.StructurePoints,
                        OperatorCrewId = cause == "operator" ? null : m.OperatorCrewId
                    }).ToArray()
                }).ToArray()
            }
        };
        using var engine = Create(scenario);
        Assert.DoesNotContain(At(engine, 100000).Objects, o => o.Torpedo is not null);
        Assert.DoesNotContain(At(engine, 100000).CombatJournal, e => e.Type == CombatEventType.Launch);
    }

    [Fact]
    public void Finishing_torpedo_allows_next_shot()
    {
        using var engine = Create(HostileScenario(100));
        var first = Assert.Single(At(engine, 1251).Objects, o => o.Torpedo is not null).Torpedo!;
        long after = first.PredictedImpactMotionTimeMs!.Value + 2;
        var snapshot = At(engine, after);
        var impact = Assert.Single(snapshot.CombatImpacts);
        Assert.DoesNotContain(snapshot.Objects, o => o.Torpedo is not null);
        var second = Assert.Single(At(engine, first.LaunchMotionTimeMs + 16251).Objects, o => o.Torpedo is not null).Torpedo!;
        Assert.InRange(second.LaunchMotionTimeMs - first.LaunchMotionTimeMs, 15500, 16250);
        Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.ModuleDestroyed && e.TargetObjectId == Player);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Large_interval_matches_partitioned_fire(double distance)
    {
        using var whole = Create(HostileScenario(distance));
        using var split = Create(HostileScenario(distance));
        var expected = At(whole, 150000);
        foreach (long time in new long[] { 1, 2, 7, 101, 1234, 3100, 10000, 99999, 150000 }) At(split, time);
        var actual = At(split, 150000);
        Assert.Equal(JsonSerializer.Serialize(expected.CombatJournal), JsonSerializer.Serialize(actual.CombatJournal));
        Assert.True(expected.CombatJournal.Count(e => e.Type == CombatEventType.Launch && e.Result == "torpedo") >= 3);
        Assert.Equal(expected.Objects.Select(o => o.ObjectId), actual.Objects.Select(o => o.ObjectId));
    }

    [Fact]
    public void Exhausted_projectile_ids_do_not_fault_or_spin_scheduler()
    {
        var scenario = HostileScenario();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Append(Obstacle("TORPEDO-" + long.MaxValue, 10000, 10000)).ToArray()
            }
        };
        using var engine = Create(scenario);
        var save = engine.CaptureSaveState();
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            { CombatState = save.GameState.CombatState! with { ProjectileSequence = long.MaxValue - 1 } }
        }, isSave: true);
        var snapshot = At(engine, 1000);
        Assert.DoesNotContain(snapshot.Objects, o => o.Torpedo is not null);
        Assert.DoesNotContain(snapshot.CombatJournal, e => e.Type == CombatEventType.Launch);
    }

    [Fact]
    public void Player_defense_responds_to_real_hostile_launch()
    {
        var scenario = HostileScenario(600);
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o.ObjectId != Player ? o : o with
                {
                    Modules = o.Modules!.Select(m => m with { AutoDefenseEnabled = true }).ToArray()
                }).ToArray()
            }
        };
        using var engine = Create(scenario);
        var snapshot = At(engine, 2502);
        var hostile = Assert.Single(snapshot.Objects, o => o.Torpedo is not null).Torpedo!;
        var defense = Assert.Single(snapshot.Objects, o => o.Countermeasure is not null).Countermeasure!;
        Assert.Equal(Target, hostile.OwnerObjectId);
        Assert.Equal(Player, defense.OwnerObjectId);
        Assert.Equal(500, defense.FrozenChanceTenths);
        Assert.Equal(2, snapshot.InstalledModules.Count(m => m.Defense is not null));
        Assert.Single(snapshot.InstalledModules, m => m.Defense?.State == DefenseState.Guiding);
        Assert.Single(snapshot.InstalledModules, m => m.Defense?.State == DefenseState.Ready);
    }

    [Fact]
    public void Actual_hostile_flight_file_roundtrip_preserves_next_shots()
    {
        using var engine = Create(HostileScenario());
        At(engine, 10000);
        var save = engine.CaptureSaveStateForTests(10000, SimulationSpeed.Speed0);
        Assert.Single(save.GameState.CombatState!.Projectiles);
        string path = Path.Combine(Path.GetTempPath(), $"hostile-fire-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, ScenarioLoader.Serialize(save));
            using var loaded = new SimulationEngine(Registry.Value);
            loaded.LoadScenario(ScenarioLoader.LoadFromFile(path, allowNonZeroGameTime: true, isSave: true), isSave: true);
            Assert.Equal(JsonSerializer.Serialize(At(engine, 150000).CombatJournal), JsonSerializer.Serialize(At(loaded, 150000).CombatJournal));
        }
        finally { File.Delete(path); }
    }
}
