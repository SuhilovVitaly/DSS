using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatSaveLoadTests : IDisposable
{
    private static void MovePlayerWithinRange(SimulationEngine engine, string targetId)
    {
        var save = engine.CaptureSaveState();
        var target = save.GameState.SpaceObjects.Single(o => o.ObjectId == targetId);
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId == Player ? o with
                {
                    PositionX = target.PositionX,
                    PositionY = target.PositionY + 1500,
                    SpeedMps = 0,
                    MovementType = "Stationary"
                } : o with
                {
                    Modules = o.Modules?.Select(m => o.ObjectType == SpaceObjectType.NpcShip && m.ModuleTypeId is "module.torpedo.launcher.basic" or "module.engine.basic"
                    ? m with { PowerState = "Off" } : m).ToArray()
                }).ToArray()
            }
        }, isSave: true);
    }

    [Fact]
    public void Case_variant_existing_id_does_not_break_save_reload()
    {
        using var engine = Create(TorpedoImpactTests.Scenario(null, Obstacle("TORPEDO-1", 1000, 1000)));
        engine.ReceiveCommand(Fire("case-collision"));
        var launched = At(engine, 0);
        Assert.Equal("torpedo-2", Flight(launched).ObjectId);
        using var restored = Roundtrip(engine.CaptureSaveState());
        var resumed = At(restored, 0);
        Assert.Contains(resumed.Objects, o => o.ObjectId == "TORPEDO-1");
        Assert.Equal("torpedo-2", Flight(resumed).ObjectId);
        SameWorld(launched, resumed);
        Assert.Equal(Target, Assert.Single(At(restored, 5000).CombatImpacts).HitObjectId);
    }
    [Fact]
    public void Real_combat_with_save_mid_second_flight_matches_continuous_run()
    {
        using var continuous = new BasicCombatEndToEndTests.CombatRun();
        var resumed = new BasicCombatEndToEndTests.CombatRun();
        string path = Path.Combine(_directory, "real-combat.json");
        try
        {
            void Reload()
            {
                var before = resumed.Snapshot();
                var save = resumed.Engine.CaptureSaveState();
                File.WriteAllText(path, ScenarioLoader.Serialize(save));
                resumed.Dispose();
                resumed = new BasicCombatEndToEndTests.CombatRun(path);
                BasicCombatEndToEndTests.SameWorld(before, resumed.Snapshot());
                Assert.Empty(resumed.Snapshot().CombatImpacts);
                var restored = resumed.Engine.CaptureSaveState();
                Assert.Equal(JsonSerializer.Serialize(save.GameState.CombatState), JsonSerializer.Serialize(restored.GameState.CombatState));
                Assert.Equal(save.GameState.MasterSeed, restored.GameState.MasterSeed);
                Assert.Equal(JsonSerializer.Serialize(save.GameState.DialogueState), JsonSerializer.Serialize(restored.GameState.DialogueState));
                Assert.Equal(JsonSerializer.Serialize(save.GameState.TradingMap?.RngStreams), JsonSerializer.Serialize(restored.GameState.TradingMap?.RngStreams));
                Assert.Equal(JsonSerializer.Serialize(save.GameState.StationResourceFields?.RngStreams), JsonSerializer.Serialize(restored.GameState.StationResourceFields?.RngStreams));
                Assert.Equal(JsonSerializer.Serialize(save.GameState.CommandReceipts), JsonSerializer.Serialize(restored.GameState.CommandReceipts));
            }

            for (int shot = 1; shot <= 3; shot++)
            {
                var launch = continuous.Launch(shot);
                BasicCombatEndToEndTests.SameWorld(launch, resumed.Launch(shot));
                long end = BasicCombatEndToEndTests.EndTime(launch);
                if (shot == 2)
                {
                    long mid = launch.MotionTimeMs + (end - launch.MotionTimeMs) / 2;
                    BasicCombatEndToEndTests.SameWorld(continuous.AdvanceTo(mid), resumed.AdvanceTo(mid, SimulationSpeed.Speed4, partition: true));
                    Assert.NotEmpty(BasicCombatEndToEndTests.Flight(resumed.Snapshot()).Torpedo!.Trail);
                    Reload();
                    resumed.Engine.ReceiveCommand(Fire("proof-2"));
                    Assert.Single(resumed.Snapshot().Objects, o => o.Torpedo is not null);
                }
                var expected = continuous.AdvanceTo(end);
                var actual = resumed.AdvanceTo(end, SimulationSpeed.Speed3, partition: true);
                BasicCombatEndToEndTests.SameWorld(expected, actual);
                BasicCombatEndToEndTests.AssertHit(actual, shot);
                BasicCombatEndToEndTests.SameImpact(expected.CombatImpacts[^1], Assert.Single(actual.CombatImpacts));
                // Reload after every hit; earlier effects must not replay, nor may a prior
                // accepted launch identity create another projectile in the new engine.
                Reload();
                resumed.Engine.ReceiveCommand(Fire("proof-" + shot));
                BasicCombatEndToEndTests.SameWorld(expected, resumed.Snapshot());
                Assert.Empty(resumed.Snapshot().CombatImpacts);
            }
        }
        finally { resumed.Dispose(); }
    }

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "dss-combat-" + Guid.NewGuid().ToString("N"));
    private static string Settings => Path.Combine(ClientRoot, "Settings.json");
    public CombatSaveLoadTests() => Directory.CreateDirectory(_directory);
    public void Dispose() => Directory.Delete(_directory, true);

    [Fact]
    public void Format17_roundtrips_module_durability_ai_and_combat_continuation()
    {
        var scenario = TorpedoImpactTests.Scenario(o => o.ObjectId switch
        {
            Target => o with { Ai = new(50, 10, 35) },
            Player => o with
            {
                Modules = o.Modules!.Select((m, i) => i == 0 ? m with
                { StructurePoints = 10, DisabledUntilMotionTimeMs = 5000 } : m).ToArray()
            },
            _ => o
        });
        using var engine = Create(scenario);
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        Assert.Equal(17, save.SaveFormatVersion);
        Assert.Equal(3, save.GameState.DefenseState!.Version);
        Assert.Equal(2, save.GameState.CombatState!.Version);
        Assert.Equal(2, save.GameState.HostileCombatState!.Version);
        Assert.NotNull(save.GameState.CombatState.HitLocationRngState);
        Assert.NotNull(save.GameState.CombatState.TorpedoHitRngState);
        Assert.NotNull(save.GameState.CombatState.LauncherBreakdownRngState);
        Assert.DoesNotContain("AttemptedTorpedoIds", JsonSerializer.Serialize(save.GameState.DefenseState));
        using var restored = Roundtrip(save);
        var actual = restored.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        Assert.Equal(JsonSerializer.Serialize(save.GameState.DefenseState), JsonSerializer.Serialize(actual.GameState.DefenseState));
        Assert.Equal(JsonSerializer.Serialize(save.GameState.CombatState), JsonSerializer.Serialize(actual.GameState.CombatState));
        Assert.Equal(JsonSerializer.Serialize(save.GameState.HostileCombatState), JsonSerializer.Serialize(actual.GameState.HostileCombatState));
        Assert.Equal(JsonSerializer.Serialize(save.GameState.SpaceObjects.Single(o => o.ObjectId == Player).Modules),
            JsonSerializer.Serialize(actual.GameState.SpaceObjects.Single(o => o.ObjectId == Player).Modules));
        Assert.Equal(save.GameState.SpaceObjects.Single(o => o.ObjectId == Target).Ai,
            actual.GameState.SpaceObjects.Single(o => o.ObjectId == Target).Ai);
    }

    [Fact]
    public void Format17_accepts_a_bounded_captured_hit_chance_instead_of_only_100()
    {
        using var engine = Create();
        engine.ReceiveCommand(Fire("range-check"));
        At(engine, 0);
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        var combat = save.GameState.CombatState!;
        var flight = Assert.Single(combat.Projectiles);
        var changed = save with
        {
            GameState = save.GameState with
            {
                CombatState = combat with { Projectiles = [flight with { Flight = flight.Flight with { HitChancePercent = 35 } }] }
            }
        };
        using var restored = Roundtrip(changed);
        Assert.Equal(35, Assert.Single(restored.CaptureSaveStateForTests(0, SimulationSpeed.Speed0)
            .GameState.CombatState!.Projectiles).Flight.HitChancePercent);
    }

    [Theory]
    [InlineData("defense")]
    [InlineData("combat")]
    [InlineData("hostile")]
    [InlineData("rng")]
    [InlineData("armor")]
    public void Invalid_format17_continuation_is_rejected_before_replacing_world(string defect)
    {
        using var engine = Create();
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        string before = ScenarioLoader.Serialize(save);
        var state = save.GameState;
        var bad = defect switch
        {
            "defense" => save with { GameState = state with { DefenseState = state.DefenseState! with { Version = 2 } } },
            "combat" => save with { GameState = state with { CombatState = state.CombatState! with { Version = 1 } } },
            "hostile" => save with { GameState = state with { HostileCombatState = state.HostileCombatState! with { Version = 1 } } },
            "rng" => save with { GameState = state with { CombatState = state.CombatState! with { TorpedoHitRngState = null } } },
            "armor" => save with
            {
                GameState = state with
                {
                    SpaceObjects = state.SpaceObjects.Select(o => o.ObjectId == Player
                ? o with { Modules = o.Modules!.Select((m, i) => i == 0 ? m with { ArmorClass = "light", Armor = null } : m).ToArray() } : o).ToArray()
                }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(defect))
        };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(bad, isSave: true));
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0)));
    }

    [Theory]
    [InlineData("attempted")]
    [InlineData("singular-flight")]
    public void Format17_rejects_removed_legacy_combat_fields(string field)
    {
        using var engine = Create();
        var json = JsonNode.Parse(ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0)))!;
        if (field == "attempted")
            json["gameState"]!["defenseState"]!["AttemptedTorpedoIds"] = new JsonArray();
        else
            json["gameState"]!["combatState"]!["launchers"]![0]!["state"]!["ActiveTorpedoObjectId"] = "torpedo-1";
        Assert.Contains("Invalid scenario JSON", Assert.Throws<ScenarioException>(() =>
            ScenarioLoader.LoadFromJson(json.ToJsonString(), true, isSave: true)).Message);
    }
    private SimulationEngine Roundtrip(ScenarioFile state, string? settings = null)
    {
        string path = Path.Combine(_directory, "save.json");
        File.WriteAllText(path, ScenarioLoader.Serialize(state));
        if (settings is not null)
            return EngineContentLoader.CreateEngineFromSaveFile(settings, path);
        var restored = new SimulationEngine(Registry.Value);
        restored.LoadScenario(ScenarioLoader.LoadFromFile(path, allowNonZeroGameTime: true, isSave: true), isSave: true);
        return restored;
    }
    private static ScenarioFile RealScenario()
    {
        var scenario = InRangePlayerAttackScenario(ScenarioLoader.LoadFromFile(Path.Combine(ClientRoot, "Scenarios", "PlayerShipOnly", "scenario.json")));
        return scenario with
        {
            GameState = scenario.GameState with
            {
                MasterSeed = 42,
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o.ObjectId == Target ? o with
                { Modules = o.Modules!.Select(m => m with { AutoDefenseEnabled = false }).ToArray() } : o).ToArray()
            }
        };
    }
    private static ObjectMotionSnapshot Flight(AuthoritativeSnapshot snapshot) => Assert.Single(snapshot.Objects.Where(o => o.Torpedo is not null));
    private static void SameWorld(AuthoritativeSnapshot expected, AuthoritativeSnapshot actual)
    {
        Assert.Equal(expected.Objects.Select(o => o.ObjectId), actual.Objects.Select(o => o.ObjectId));
        foreach (var pair in expected.Objects.Zip(actual.Objects))
        {
            Assert.Equal(pair.First.X, pair.Second.X, 6);
            Assert.Equal(pair.First.Y, pair.Second.Y, 6);
            Assert.Equal(pair.First.Direction, pair.Second.Direction, 6);
            Assert.Equal(pair.First.HullCombat, pair.Second.HullCombat);
            if (pair.First.Torpedo is { } flight)
            {
                Assert.NotNull(pair.Second.Torpedo);
                Assert.Equal(flight.DistanceTravelledWorldUnits, pair.Second.Torpedo.DistanceTravelledWorldUnits, 6);
                Assert.Equal(flight.TargetObjectId, pair.Second.Torpedo.TargetObjectId);
            }
        }
        Assert.Equal(JsonSerializer.Serialize(expected.InstalledModules.Select(m => m.LauncherCombat)),
            JsonSerializer.Serialize(actual.InstalledModules.Select(m => m.LauncherCombat)));
    }

    [Fact]
    public void Fractional_content_speed_survives_file_roundtrip_and_rejects_corrupt_speed()
    {
        const double speedKmS = 1.0244;
        var settings = JsonNode.Parse(File.ReadAllText(Settings))!;
        foreach (var property in settings["typeData"]!.AsObject().ToArray())
            settings["typeData"]![property.Key] = Path.GetFullPath(Path.Combine(ClientRoot, property.Value!.GetValue<string>()));
        settings["defaultScenario"] = Path.Combine(ClientRoot, "Scenarios", "PlayerShipOnly", "scenario.json");
        string modulesPath = Path.Combine(_directory, "Modules");
        string sourceModules = Path.Combine(ClientRoot, "Data", "Modules");
        foreach (string source in Directory.GetFiles(sourceModules, "*.json", SearchOption.AllDirectories))
        {
            string destination = Path.Combine(modulesPath, Path.GetRelativePath(sourceModules, source));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            var content = JsonNode.Parse(File.ReadAllText(source))!;
            foreach (var module in content["moduleImplementations"]!.AsArray())
                if (module!["typeId"]!.GetValue<string>() == "module.torpedo.launcher.basic")
                    module["torpedoSpeedKmS"] = speedKmS;
            File.WriteAllText(destination, content.ToJsonString());
        }
        settings["typeData"]!["moduleImplementations"] = modulesPath;
        string settingsPath = Path.Combine(_directory, "Settings.json");
        File.WriteAllText(settingsPath, settings.ToJsonString());
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(settingsPath);
        MovePlayerWithinRange(engine, Target);
        engine.ReceiveCommand(Fire("fractional-speed"));
        At(engine, 0);
        var save = engine.CaptureSaveStateForTests(1234, SimulationSpeed.Speed0);
        var missile = Assert.Single(save.GameState.SpaceObjects, o => o.ObjectType == SpaceObjectType.Missile);
        Assert.Equal(speedKmS, Assert.Single(save.GameState.CombatState!.Projectiles).Flight.SpeedKmS);
        Assert.NotEqual(speedKmS, missile.SpeedMps / 1000);
        using var loaded = Roundtrip(save, settingsPath);
        Assert.Equal(speedKmS, Flight(loaded.CaptureSnapshot()).Torpedo!.SpeedKmS);
        SameWorld(At(engine, 2345), At(loaded, 2345));

        var corrupt = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId == missile.ObjectId
                    ? o with { SpeedMps = o.SpeedMps * 1.001 } : o).ToArray()
            }
        };
        Assert.Throws<ScenarioException>(() => Roundtrip(corrupt, settingsPath));
    }

    [Theory]
    [InlineData("stationary")]
    [InlineData("sTaTiOnArY")]
    public void Wreck_movement_type_preserves_case_insensitive_file_compatibility(string movementType)
    {
        using var engine = Create(RealScenario());
        long time = 0;
        for (int shot = 1; shot <= 3; shot++)
        {
            engine.ReceiveCommand(Fire("wreck-case-" + shot));
            var launch = Flight(At(engine, time));
            time = launch.Torpedo!.PredictedImpactMotionTimeMs!.Value + 1000;
            At(engine, time);
        }
        var save = engine.CaptureSaveStateForTests(time, SimulationSpeed.Speed0);
        var wreck = Assert.Single(save.GameState.SpaceObjects, o => o.ObjectType == SpaceObjectType.Wreck);
        save = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId == wreck.ObjectId
                    ? o with { MovementType = movementType } : o).ToArray()
            }
        };
        using var loaded = Roundtrip(save);
        var restored = Assert.Single(loaded.CaptureSnapshot().Objects.Where(o => o.ObjectType == SpaceObjectType.Wreck));
        Assert.Equal(wreck.ObjectId, restored.ObjectId);
        Assert.Equal(wreck.PositionX, restored.X);
        Assert.Equal(wreck.PositionY, restored.Y);
        Assert.Equal(0, restored.SpeedKmS);
        Assert.Equal(0, restored.Direction);
        Assert.Empty(loaded.CaptureSnapshot().CombatImpacts);
    }

    [Theory]
    [InlineData(SimulationSpeed.Speed0, false)]
    [InlineData(SimulationSpeed.Speed1, false)]
    [InlineData(SimulationSpeed.Speed2, true)]
    [InlineData(SimulationSpeed.Speed3, false)]
    [InlineData(SimulationSpeed.Speed4, true)]
    public void File_roundtrip_midflight_matches_continuous_three_hit_run(SimulationSpeed speed, bool split)
    {
        using var continuous = Create(RealScenario());
        var resumed = Create(RealScenario());
        long time = 0;
        try
        {
            for (int shot = 1; shot <= 3; shot++)
            {
                continuous.ReceiveCommand(Fire("shot-" + shot));
                resumed.ReceiveCommand(Fire("shot-" + shot));
                var launch = At(continuous, time);
                At(resumed, time);
                long end = Flight(launch).Torpedo!.PredictedImpactMotionTimeMs!.Value + 1000;
                long mid = time + (end - time) / 2;
                At(continuous, mid, mid * 10);
                if (split) At(resumed, time + 37, (time + 37) * 10);
                At(resumed, mid, mid * 10);
                var saved = resumed.CaptureSaveStateForTests(mid * 10, speed, mid);
                var old = resumed;
                resumed = Roundtrip(saved);
                old.Dispose();
                SameWorld(At(continuous, mid, mid * 10), resumed.CaptureSnapshot());
                Assert.Empty(resumed.CaptureSnapshot().CombatImpacts);
                if (split) At(resumed, mid + 47, (mid + 47) * 10);
                var expected = At(continuous, end, end * 10);
                var actual = At(resumed, end, end * 10);
                SameWorld(expected, actual);
                var hit = Assert.Single(actual.CombatImpacts);
                Assert.Equal(shot, hit.EventId);
                Assert.Equal(150, hit.DamageApplied);
                Assert.Equal(expected.CombatImpacts[^1].MotionTimeMs, hit.MotionTimeMs, 4);
                // Persist after each hit as well as mid-flight. Replay old launch IDs.
                old = resumed;
                resumed = Roundtrip(resumed.CaptureSaveStateForTests(end * 10, speed, end));
                old.Dispose();
                resumed.ReceiveCommand(Fire("shot-" + shot));
                SameWorld(expected, At(resumed, end, end * 10));
                Assert.Empty(At(resumed, end, end * 10).CombatImpacts);
                if (shot < 3)
                {
                    Assert.Contains(actual.Objects, o => o.ObjectId == Target && o.HullCombat is null);
                    Assert.Equal(shot, actual.CombatJournal.Count(e => e.Type == CombatEventType.ModuleDestroyed));
                }
                else Assert.Single(actual.Objects.Where(o => o.ObjectType == SpaceObjectType.Wreck));
                time = end;
            }
        }
        finally { resumed.Dispose(); }
    }

    [Fact]
    public void Save_after_enqueue_preserves_exactly_one_launch()
    {
        using var engine = Create(RealScenario());
        engine.ReceiveCommand(Fire("queued"));
        using var loaded = Roundtrip(engine.CaptureSaveState());
        loaded.ReceiveCommand(Fire("queued"));
        var snapshot = loaded.CaptureSnapshot();
        Assert.Equal("torpedo-1", Flight(snapshot).ObjectId);
        Assert.Equal(SimulationSpeed.Speed0, snapshot.CurrentSpeed);
        Assert.Equal("torpedo-1", Assert.Single(snapshot.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.ActiveTorpedoIds));
    }

    [Theory]
    [InlineData(3166)]
    [InlineData(3167)]
    public void Save_at_contact_does_not_duplicate_damage_or_wreck(long time)
    {
        using var engine = Create();
        engine.ReceiveCommand(Fire("contact"));
        At(engine, 0);
        using var loaded = Roundtrip(engine.CaptureSaveStateForTests(time, SimulationSpeed.Speed0));
        loaded.ReceiveCommand(Fire("contact"));
        var snapshot = At(loaded, 4000);
        Assert.Null(snapshot.Objects.Single(o => o.ObjectId == Target).HullCombat);
        Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.ModuleDestroyed);
        Assert.Empty(snapshot.Objects.Where(o => o.ObjectType == SpaceObjectType.Wreck));
        Assert.Equal(time == 3166 ? 1 : 0, snapshot.CombatImpacts.Length);
        Assert.Equal(1, loaded.CaptureSaveStateForTests(4000, SimulationSpeed.Speed0).GameState.CombatState!.ImpactSequence);
    }

    [Fact]
    public void Saved_module_and_weapon_values_survive_content_edit()
    {
        using var engine = Create(RealScenario());
        engine.ReceiveCommand(Fire("captured"));
        At(engine, 0);
        var save = engine.CaptureSaveStateForTests(2000, SimulationSpeed.Speed0);
        var settings = JsonNode.Parse(File.ReadAllText(Settings))!;
        foreach (var property in settings["typeData"]!.AsObject().ToArray())
            settings["typeData"]![property.Key] = Path.GetFullPath(Path.Combine(ClientRoot, property.Value!.GetValue<string>()));
        settings["defaultScenario"] = Path.Combine(ClientRoot, "Scenarios", "PlayerShipOnly", "scenario.json");
        string classesPath = Path.Combine(_directory, "classes.json");
        File.WriteAllText(classesPath, "{\"shipClasses\":[{\"typeId\":\"ship.tetrarch\"},{\"typeId\":\"ship.mukha\"}]}");
        settings["typeData"]!["shipClasses"] = classesPath;
        string modulesPath = Path.Combine(_directory, "Modules");
        foreach (string source in Directory.GetFiles(Path.Combine(ClientRoot, "Data", "Modules"), "*.json", SearchOption.AllDirectories))
        {
            string destination = Path.Combine(modulesPath, Path.GetRelativePath(Path.Combine(ClientRoot, "Data", "Modules"), source));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            string content = File.ReadAllText(source);
            if (source.Contains("TorpedoLauncher", StringComparison.Ordinal))
                content = content.Replace("\"torpedoDamage\": 150", "\"torpedoDamage\": 50")
                    .Replace("\"torpedoSpeedKmS\": 10", "\"torpedoSpeedKmS\": 6")
                    .Replace("\"torpedoTurnRateDegPerSec\": 90", "\"torpedoTurnRateDegPerSec\": 45");
            File.WriteAllText(destination, content);
        }
        settings["typeData"]!["moduleImplementations"] = modulesPath;
        string settingsPath = Path.Combine(_directory, "Settings.json");
        File.WriteAllText(settingsPath, settings.ToJsonString());
        using var loaded = Roundtrip(save, settingsPath);
        var snapshot = loaded.CaptureSnapshot();
        Assert.Null(snapshot.Objects.Single(o => o.ObjectId == Player).HullCombat);
        Assert.Equal(150, Flight(snapshot).Torpedo!.Damage);
        Assert.Equal(3, Flight(snapshot).Torpedo!.SpeedKmS);
        Assert.Equal(90, Flight(snapshot).Torpedo!.TurnRateDegPerSec);
        using var fresh = EngineContentLoader.CreateEngineFromSettingsFile(settingsPath);
        var newSnapshot = fresh.CaptureSnapshot();
        Assert.Null(newSnapshot.Objects.Single(o => o.ObjectId == Player).HullCombat);
        Assert.Equal(50, newSnapshot.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.Damage);
        Assert.Equal(6, newSnapshot.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.SpeedKmS);
    }

    [Fact]
    public void Busy_and_full_trail_restore_without_new_rng_draw()
    {
        using var engine = Create(RealScenario());
        engine.ReceiveCommand(Fire("trail"));
        At(engine, 0);
        var save = engine.CaptureSaveStateForTests(12345, SimulationSpeed.Speed0);
        using var loaded = Roundtrip(save);
        var recaptured = loaded.CaptureSaveState();
        Assert.Equal(JsonSerializer.Serialize(save.GameState.CombatState), JsonSerializer.Serialize(recaptured.GameState.CombatState));
        Assert.Equal(save.GameState.MasterSeed, recaptured.GameState.MasterSeed);
        Assert.Equal(JsonSerializer.Serialize(save.GameState.DialogueState), JsonSerializer.Serialize(recaptured.GameState.DialogueState));
        Assert.Equal(JsonSerializer.Serialize(save.GameState.CommandReceipts), JsonSerializer.Serialize(recaptured.GameState.CommandReceipts));
        loaded.ReceiveCommand(Fire("busy"));
        var snapshot = loaded.CaptureSnapshot();
        Assert.Single(snapshot.Objects.Where(o => o.Torpedo is not null));
        Assert.Contains(snapshot.CommandResults, r => r.CommandId == "busy" && r.ReasonCode == "launcher_reloading");
        Assert.NotEmpty(Flight(snapshot).Torpedo!.Trail);
    }

    [Fact]
    public void Combat_restore_preserves_named_world_rng_states_and_next_draws()
    {
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(Settings);
        var initial = engine.CaptureSaveState();
        var target = initial.GameState.SpaceObjects.Last(o => o.ObjectType == SpaceObjectType.Station);
        MovePlayerWithinRange(engine, target.ObjectId);
        engine.ReceiveCommand(Fire("rng-continuation", target.ObjectId));
        At(engine, 0);
        var save = engine.CaptureSaveStateForTests(1234, SimulationSpeed.Speed0);
        Assert.Single(save.GameState.CombatState!.Projectiles);
        using var loaded = Roundtrip(save);
        var restored = loaded.CaptureSaveState();
        Assert.NotEmpty(initial.GameState.TradingMap!.RngStreams);
        Assert.NotEmpty(initial.GameState.StationResourceFields!.RngStreams);
        Assert.Equal(JsonSerializer.Serialize(initial.GameState.TradingMap.RngStreams), JsonSerializer.Serialize(restored.GameState.TradingMap!.RngStreams));
        Assert.Equal(JsonSerializer.Serialize(initial.GameState.StationResourceFields.RngStreams),
            JsonSerializer.Serialize(restored.GameState.StationResourceFields!.RngStreams));
        foreach (var stream in save.GameState.StationResourceFields!.RngStreams)
        {
            var expected = new ResourceFieldRandom(stream);
            var actual = new ResourceFieldRandom(restored.GameState.StationResourceFields.RngStreams.Single(s => s.Name == stream.Name));
            Assert.Equal(expected.NextDouble(), actual.NextDouble());
            Assert.Equal(expected.Capture(), actual.Capture());
        }
    }

    [Fact]
    public void Launch_identity_survives_receipt_eviction_and_file_reload()
    {
        using var engine = Create();
        engine.ReceiveCommand(Fire("original"));
        At(engine, 0);
        At(engine, 4000);
        for (int i = 0; i <= SimulationEngine.CommandReceiptLimit; i++)
            engine.ReceiveCommand(new("invalid-" + i, (ulong)i + 2, Player, "missing", "invalid"));
        At(engine, 4000);
        var save = engine.CaptureSaveStateForTests(4000, SimulationSpeed.Speed0);
        Assert.DoesNotContain(save.GameState.CommandReceipts!, r => r.CommandId == "original");
        using var restored = Roundtrip(save);
        restored.ReceiveCommand(Fire("original"));
        Assert.DoesNotContain(At(restored, 5000).Objects, o => o.Torpedo is not null);
        Assert.Single(At(restored, 5000).CombatJournal, e => e.Type == CombatEventType.ModuleDestroyed);
    }

    [Fact]
    public void Unreachable_replanning_flight_resumes_between_guidance_boundaries()
    {
        using var engine = Create(TorpedoImpactTests.Scenario(o => o.ObjectId == Target ? o with
        { PositionX = 1000, PositionY = -1000, SpeedMps = 4000, DirectionDegrees = 90, MovementType = "Linear" } : o));
        engine.ReceiveCommand(Fire("pursuit"));
        At(engine, 0);
        var save = engine.CaptureSaveStateForTests(1234, SimulationSpeed.Speed0);
        using var restored = Roundtrip(save);
        Assert.False(Flight(restored.CaptureSnapshot()).Torpedo!.Route.HasIntercept);
        SameWorld(At(engine, 1299), At(restored, 1299));
        SameWorld(At(engine, 1300), At(restored, 1300));
        SameWorld(At(engine, 5678), At(restored, 5678));
        Assert.Equal(JsonSerializer.Serialize(Flight(At(engine, 5678)).Torpedo), JsonSerializer.Serialize(Flight(At(restored, 5678)).Torpedo));
    }
}
