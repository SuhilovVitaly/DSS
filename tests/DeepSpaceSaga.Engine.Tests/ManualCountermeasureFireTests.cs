using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public class ManualCountermeasureFireTests
{
    internal const string Defense1 = "MOD-PLAYER-COUNTERMEASURE-01", Defense2 = "MOD-PLAYER-COUNTERMEASURE-02";
    internal static PlayerCommand Manual(string id, string target, string module = Defense1) => new(id, 1, Player, module, DefenseCommandTypes.Fire, target);
    internal static string LaunchEnemy(SimulationEngine engine) => At(engine, 1).Objects.Single(o => o.Torpedo is not null).ObjectId;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Paused_manual_fire_creates_busy_without_time_and_ignores_auto_toggle(bool auto)
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario());
        string target = LaunchEnemy(engine);
        engine.ReceiveCommand(new("toggle", 1, Player, Defense1, auto ? DefenseCommandTypes.Enable : DefenseCommandTypes.Disable));
        engine.ReceiveCommand(Manual("manual", target));
        var snapshot = At(engine, 1);
        var projectile = Assert.Single(snapshot.Objects, o => o.Countermeasure is not null);
        var flight = projectile.Countermeasure!;
        Assert.Equal(LaunchMode.Manual, flight.Mode);
        Assert.Equal(1, flight.LaunchMotionTimeMs);
        Assert.Equal(60001, flight.ExpiresAtMotionTimeMs);
        Assert.Equal(60000, flight.MaxFlightTimeMs);
        Assert.Equal(500, flight.FrozenChanceTenths);
        Assert.Contains(snapshot.Objects, o => o.ObjectId == target && o.Torpedo is not null);
        Assert.Equal(DefenseState.Guiding, snapshot.InstalledModules.Single(m => m.ModuleId == Defense1).Defense!.State);
        Assert.Equal(auto, snapshot.InstalledModules.Single(m => m.ModuleId == Defense1).Defense!.AutoEnabled);
        Assert.Equal(projectile, At(engine, 1, 9000000).Objects.Single(o => o.ObjectId == projectile.ObjectId));
        Assert.Empty(snapshot.CombatJournal.Where(e => e.Roll is not null));
    }

    [Fact]
    public void Two_modules_can_target_same_torpedo_and_same_id_cannot_spawn_twice()
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario());
        string target = LaunchEnemy(engine);
        engine.ReceiveCommand(Manual("first", target));
        engine.ReceiveCommand(Manual("first", target));
        engine.ReceiveCommand(Manual("second", target, Defense2));
        var snapshot = At(engine, 1);
        Assert.Equal(2, snapshot.Objects.Count(o => o.Countermeasure is not null));
        Assert.All(snapshot.InstalledModules.Where(m => m.Defense is not null), m => Assert.Equal(DefenseState.Guiding, m.Defense!.State));
        engine.ReceiveCommand(Manual("busy", target));
        var busy = At(engine, 1);
        Assert.Equal(CommandReasonCodes.NotReady, Assert.Single(busy.CommandResults).ReasonCode);
        Assert.Equal(2, busy.Objects.Count(o => o.Countermeasure is not null));
        Assert.Equal(0ul, engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0).GameState.DefenseState!.RngCounter);
    }

    [Fact]
    public void Zero_chance_and_no_solution_do_not_block_manual_fire()
    {
        var source = AutomaticDefenseTests.WeaponRegistry(0, 100);
        static IEnumerable<T> All<T>(TypeRegistry<T> r) where T : ITypeDefinition => Enumerable.Range(0, r.Count).Select(r.GetDefinition);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Select(m =>
            m.CountermeasureAccuracy is not null ? m with { CountermeasureSpeedKmS = 0.001 } : m),
            All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes),
            All(source.Dialogues), All(source.Quests), stationMarketProfiles: All(source.StationMarketProfiles),
            shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents));
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(HostileTorpedoFireTests.HostileScenario(1500));
        string target = LaunchEnemy(engine);
        engine.ReceiveCommand(Manual("zero", target));
        var flight = Assert.Single(At(engine, 1).Objects, o => o.Countermeasure is not null).Countermeasure!;
        Assert.Equal(0, flight.FrozenChanceTenths);
        Assert.False(flight.Route.HasIntercept);
        Assert.Null(flight.PredictedEncounterMotionTimeMs);
        var after = Assert.Single(At(engine, 1100).Objects, o => o.Countermeasure is not null).Countermeasure!;
        Assert.Equal(60001, after.ExpiresAtMotionTimeMs);
        Assert.Null(after.PredictedEncounterMotionTimeMs);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("ship")]
    [InlineData("own")]
    [InlineData("wrong-owner")]
    [InlineData("wrong-module")]
    public void Invalid_manual_fire_has_no_spawn_attempt_or_rng_side_effect(string invalid)
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario());
        string target = LaunchEnemy(engine);
        if (invalid == "own")
        {
            engine.ReceiveCommand(Fire("own-launch"));
            target = At(engine, 1).Objects.Single(o => o.Torpedo?.OwnerObjectId == Player).ObjectId;
        }
        var command = Manual("invalid", invalid == "missing" ? "absent" : invalid == "ship" ? Target : target);
        if (invalid == "wrong-owner") command = command with { ObjectId = Target };
        if (invalid == "wrong-module") command = command with { ModuleId = Launcher };
        engine.ReceiveCommand(command);
        var result = At(engine, 1);
        Assert.Equal(CommandResultStatus.Rejected, Assert.Single(result.CommandResults).Status);
        Assert.DoesNotContain(result.Objects, o => o.Countermeasure is not null);
        Assert.Contains(result.Objects, o => o.ObjectId == target && o.Torpedo is not null);
        var save = engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0).GameState;
        Assert.Equal(0, save.DefenseState!.ProjectileSequence);
        Assert.Equal(0ul, save.DefenseState.RngCounter);
    }

    [Theory]
    [InlineData(PlayerRelation.Friend)]
    [InlineData(PlayerRelation.Neutral)]
    public void Living_owner_current_relation_overrides_captured_enemy_provenance(string relation)
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario());
        string target = LaunchEnemy(engine);
        var save = engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0);
        save = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o =>
            o.ObjectId == Target ? o with { RelationToPlayer = relation } : o).ToArray()
            }
        };
        engine.LoadScenario(save);
        engine.ReceiveCommand(Manual("friendly", target));
        var snapshot = At(engine, 1);
        Assert.Equal("target_not_hostile", Assert.Single(snapshot.CommandResults).ReasonCode);
        Assert.DoesNotContain(snapshot.Objects, o => o.Countermeasure is not null);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void Manual_range_boundary_is_inclusive(double offset, bool allowed)
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario(2000));
        string target = LaunchEnemy(engine);
        var save = engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0);
        save = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o =>
            o.ObjectId == Player ? o with { PositionX = offset } : o).ToArray()
            }
        };
        engine.LoadScenario(save);
        engine.ReceiveCommand(Manual("range", target));
        var snapshot = At(engine, 1);
        Assert.Equal(allowed ? 1 : 0, snapshot.Objects.Count(o => o.Countermeasure is not null));
        if (!allowed) Assert.Equal("target_out_of_range", Assert.Single(snapshot.CommandResults).ReasonCode);
    }

    [Fact]
    public void Durable_command_identity_survives_receipt_eviction()
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario());
        string target = LaunchEnemy(engine);
        engine.ReceiveCommand(Manual("original", target)); At(engine, 1);
        for (int i = 0; i <= SimulationEngine.CommandReceiptLimit; i++)
            engine.ReceiveCommand(new("invalid-" + i, (ulong)i + 2, Player, "absent", "invalid"));
        At(engine, 1);
        var save = engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0);
        Assert.DoesNotContain(save.GameState.CommandReceipts!, r => r.CommandId == "original");
        Assert.Contains("original", save.GameState.CombatState!.ProcessedLaunchCommandIds);
        engine.ReceiveCommand(Manual("original", target, Defense2));
        var snapshot = At(engine, 1);
        Assert.Single(snapshot.Objects, o => o.Countermeasure is not null);
        Assert.Equal(DefenseState.Ready, snapshot.InstalledModules.Single(m => m.ModuleId == Defense2).Defense!.State);
    }

    [Theory]
    [InlineData("off")]
    [InlineData("broken")]
    [InlineData("operator")]
    public void Unready_module_rejects_without_marking_target(string cause)
    {
        var scenario = HostileTorpedoFireTests.HostileScenario();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o =>
            o.ObjectId != Player ? o : o with
            {
                Modules = o.Modules!.Select(m => m.ModuleId != Defense1 ? m : m with
                {
                    PowerState = cause == "off" ? "Off" : m.PowerState,
                    StructurePoints = cause == "broken" ? 0 : m.StructurePoints,
                    OperatorCrewId = cause == "operator" ? null : m.OperatorCrewId
                }).ToArray()
            }).ToArray()
            }
        };
        using var engine = Create(scenario);
        string target = LaunchEnemy(engine);
        engine.ReceiveCommand(Manual("unready", target));
        var snapshot = At(engine, 1);
        Assert.Equal(CommandResultStatus.Rejected, Assert.Single(snapshot.CommandResults).Status);
        Assert.DoesNotContain(snapshot.Objects, o => o.Countermeasure is not null);
        Assert.Contains(snapshot.Objects, o => o.ObjectId == target && o.Torpedo is not null);
    }

    [Fact]
    public void A_new_manual_attempt_is_allowed_after_miss_and_reload()
    {
        using var engine = new SimulationEngine(AutomaticDefenseTests.WeaponRegistry(0, 100));
        engine.LoadScenario(HostileTorpedoFireTests.HostileScenario());
        string target = LaunchEnemy(engine);
        engine.ReceiveCommand(Manual("first-attempt", target)); At(engine, 1);
        var ready = At(engine, 20000);
        Assert.Contains(ready.CombatJournal, e => e.Type == CombatEventType.Miss && e.ChanceTenths == 0);
        Assert.Equal(DefenseState.Ready, ready.InstalledModules.Single(m => m.ModuleId == Defense1).Defense!.State);
        Assert.Contains(ready.Objects, o => o.ObjectId == target && o.Torpedo is not null);
        engine.ReceiveCommand(Manual("second-attempt", target));
        var second = At(engine, 20000);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(second.CommandResults).Status);
        Assert.Equal(target, Assert.Single(second.Objects, o => o.Countermeasure is not null).Countermeasure!.TargetTorpedoId);
    }

    [Theory]
    [InlineData(0, 100, 0)]
    [InlineData(100, 0, 1000)]
    public void Zero_and_hundred_still_draw(decimal accuracy, decimal maneuverability, int expectedChance)
    {
        using var engine = new SimulationEngine(AutomaticDefenseTests.WeaponRegistry(accuracy, maneuverability));
        engine.LoadScenario(HostileTorpedoFireTests.HostileScenario());
        string target = LaunchEnemy(engine);
        engine.ReceiveCommand(Manual("boundary", target));
        var launched = At(engine, 1);
        Assert.Equal(expectedChance,
            Assert.Single(launched.Objects, o => o.Countermeasure is not null).Countermeasure!.FrozenChanceTenths);
        var resolved = At(engine, 20000);
        var roll = Assert.Single(resolved.CombatJournal, e => e.RatingBreakdown is not null && e.Roll is not null);
        Assert.Equal(expectedChance, roll.ChanceTenths);
        Assert.InRange(roll.Roll!.Value, 1, 1000);
        Assert.Equal(1UL, engine.CaptureSaveState().GameState.DefenseState!.RngCounter);
    }

    [Fact]
    public void Captured_hostility_keeps_orphaned_enemy_torpedo_eligible()
    {
        var scenario = HostileTorpedoFireTests.HostileScenario();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o =>
            o.ObjectId == Target ? FragileForOneHit(o) : o).ToArray()
            }
        };
        using var engine = Create(scenario);
        string target = LaunchEnemy(engine);
        engine.ReceiveCommand(Fire("kill-owner"));
        var own = At(engine, 1).Objects.Single(o => o.Torpedo?.OwnerObjectId == Player).Torpedo!;
        long time = own.PredictedImpactMotionTimeMs!.Value + 1;
        var orphaned = At(engine, time);
        Assert.DoesNotContain(orphaned.Objects, o => o.ObjectId == Target);
        Assert.Equal(PlayerRelation.Enemy, orphaned.Objects.Single(o => o.ObjectId == target).Torpedo!.OwnerPlayerRelationAtLaunch);
        engine.ReceiveCommand(Manual("orphan", target));
        Assert.Single(At(engine, time).Objects, o => o.Countermeasure is not null);
    }

    [Fact]
    public void Enemy_torpedo_aimed_at_another_object_remains_manual_target()
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario());
        string target = LaunchEnemy(engine);
        var save = engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0);
        // A valid custom save may identify a different stationary object at the same aim
        // point. The real launch/curve stays intact; eligibility must not demand a player target.
        var obstacle = Obstacle("other-target", 0, 0);
        save = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Append(obstacle).ToArray(),
                CombatState = save.GameState.CombatState! with
                {
                    Projectiles = save.GameState.CombatState!.Projectiles.Select(p =>
                    p.ObjectId != target ? p : p with
                    { Flight = p.Flight with { TargetObjectId = obstacle.ObjectId }, TargetBaseline = p.TargetBaseline! with { ObjectId = obstacle.ObjectId } }).ToArray()
                }
            }
        };
        engine.LoadScenario(save);
        engine.ReceiveCommand(Manual("other-object", target));
        Assert.Single(At(engine, 1).Objects, o => o.Countermeasure is not null);
    }

    [Fact]
    public void Positive_chance_manual_flight_roundtrips_actual_file_without_duplicate_spawn()
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario());
        string target = LaunchEnemy(engine);
        engine.ReceiveCommand(Manual("saved-manual", target)); At(engine, 1);
        string path = Path.Combine(Path.GetTempPath(), "dss-manual-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(101, SimulationSpeed.Speed0)));
            using var restored = Create(ScenarioLoader.LoadFromFile(path, allowNonZeroGameTime: true));
            restored.ReceiveCommand(Manual("saved-manual", target, Defense2));
            var actual = At(restored, 1101);
            var expected = At(engine, 1101);
            Assert.Single(actual.Objects, o => o.Countermeasure is not null);
            Assert.Equal(JsonSerializer.Serialize(expected.Objects), JsonSerializer.Serialize(actual.Objects));
            Assert.Equal(LaunchMode.Manual, actual.Objects.Single(o => o.Countermeasure is not null).Countermeasure!.Mode);
        }
        finally { File.Delete(path); }
    }
}
