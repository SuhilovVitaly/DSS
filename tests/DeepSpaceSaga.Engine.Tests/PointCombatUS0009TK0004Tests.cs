using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Content;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0004TK0001Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0009TK0004Tests
{
    private static IEnumerable<T> All<T>(TypeRegistry<T> source) where T : ITypeDefinition => Enumerable.Range(0, source.Count).Select(source.GetDefinition);
    private static SimulationEngine Battle(bool engineEnabled = false) => CreateCounter(o => o with
    {
        PositionX = o.ObjectId == Player ? 0 : 600,
        PositionY = 0,
        SpeedMps = 0,
        MovementType = "Stationary",
        Modules = o.Modules!.Select(m => m with
        { PowerState = m.ModuleTypeId == "module.engine.basic" && !engineEnabled ? "Off" : "On" }).ToArray()
    });
    private static ScenarioFile Save(SimulationEngine engine, long time) => engine.CaptureSaveStateForTests(time * 4, SimulationSpeed.Speed0, time);

    [Fact]
    public void Save18_midflight_resume_matches_uninterrupted()
    {
        using var engine = Battle();
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, "MOD-PIRATE-TORPEDO-01", 0, 0, "slow", 0).Disposition);
        engine.ReceiveCommand(Launch(x: 600, profile: "slow")); engine.ReceiveCommand(CounterLaunch(x: 250)); At(engine);
        var save = Save(engine, 100);
        Assert.NotNull(save.GameState.PointCombatState);
        Assert.Equal(3, save.GameState.PointCombatState!.Projectiles.Count);
        string path = Path.Combine(Path.GetTempPath(), "dss-point-save18-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, ScenarioLoader.Serialize(save));
            var loaded = ScenarioLoader.LoadFromFile(path, true, true);
            using var restored = new SimulationEngine(CounterRegistry()); restored.LoadScenario(loaded, true);
            var baseline = At(restored, 100).CombatReports!;
            Assert.Empty(baseline.Reports);
            Assert.Equal(save.GameState.CombatReportsState!.Sequence, baseline.BaselineSequence);
            foreach (long time in new long[] { 100, 313, 999, 1700, 4000, 10000, 13000 })
            {
                var a = At(engine, time); var b = At(restored, time);
                Assert.Equal(JsonSerializer.Serialize(a.Objects), JsonSerializer.Serialize(b.Objects));
                Assert.Equal(JsonSerializer.Serialize(a.InstalledModules), JsonSerializer.Serialize(b.InstalledModules));
                Assert.Equal(a.Encounter, b.Encounter);
                Assert.Equal(engine.CaptureLaunchScatterState(), restored.CaptureLaunchScatterState());
                Assert.Equal(engine.CaptureNpcDodgeRngState(), restored.CaptureNpcDodgeRngState());
                Assert.Equal(ScenarioLoader.Serialize(Save(engine, time)), ScenarioLoader.Serialize(Save(restored, time)));
            }
            foreach (var world in new[] { engine, restored }) world.ReceiveCommand(Launch("next-draw", x: 600, profile: "medium"));
            Assert.Equal(JsonSerializer.Serialize(At(engine, 13000).Objects), JsonSerializer.Serialize(At(restored, 13000).Objects));
            Assert.Equal(engine.CaptureLaunchScatterState(), restored.CaptureLaunchScatterState());
            Assert.Equal(8UL, restored.CaptureLaunchScatterState().Counter);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Save18_preserves_evasion_segment_anchor_and_original_base_course(bool changeSide)
    {
        using var engine = Battle(true);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, "MOD-PIRATE-TORPEDO-01", 0, 0, "slow", 0).Disposition);
        var threat = Assert.Single(At(engine).Objects, o => o.ObservedProjectile is not null).ObjectId;
        var evade = new PlayerCommand("evade", 1, Player, "MOD-PLAYER-ENGINE-01", ManeuverCommandTypes.Evade,
            ThreatProjectileId: threat, EvadeSide: EvadeSide.Left);
        At(engine, 113); engine.ReceiveCommand(evade); At(engine, 113);
        if (changeSide) { At(engine, 313); engine.ReceiveCommand(evade with { CommandId = "side-change", EvadeSide = EvadeSide.Right }); At(engine, 313); }
        var save = Save(engine, 457);
        using var restored = new SimulationEngine(CounterRegistry());
        restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true), true);
        foreach (long time in new long[] { 457, 913, 1801, 6000, 10600, 12000, 25000 })
        {
            Assert.Equal(JsonSerializer.Serialize(At(engine, time).Objects), JsonSerializer.Serialize(At(restored, time).Objects));
            Assert.Equal(ScenarioLoader.Serialize(Save(engine, time)), ScenarioLoader.Serialize(Save(restored, time)));
        }
        Assert.Equal(1, restored.CaptureEvasionEpisodeCount());
        Assert.Null(At(restored, 25000).Objects.Single(o => o.ObjectId == Player).Evasion);
    }

    [Theory]
    [InlineData(1UL, 799)]
    [InlineData(1UL, 800)]
    [InlineData(1UL, 913)]
    [InlineData(2UL, 799)]
    [InlineData(2UL, 800)]
    [InlineData(2UL, 913)]
    public void Npc_dodge_cursor_roll_and_motion_continue_without_reroll(ulong seed, long saveTime)
    {
        using var engine = Create(o => o.ObjectId != Target ? o : o with
        {
            PositionX = 200,
            PositionY = 0,
            DirectionDegrees = 270,
            Ai = new(25, 8, 35),
            Crew = o.Crew!.Append(o.Crew![0] with { CrewId = "npc-pilot", ManeuverabilitySkill = 20 }).ToArray(),
            Modules = o.Modules!.Select(m => m.ModuleId == "MOD-PIRATE-ENGINE-01"
                ? m with { PowerState = "On", OperatorCrewId = "npc-pilot" } : m with { PowerState = "Off" }).ToArray()
        }, scatter: 0, seed: seed);
        engine.ReceiveCommand(Launch()); At(engine);
        var save = Save(engine, saveTime);
        using var restored = new SimulationEngine(Registry(scatter: 0));
        restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true), true);
        foreach (long time in new long[] { saveTime, 800, 913, 1200, 4200 }.Where(t => t >= saveTime).Distinct().Order())
        {
            Assert.Equal(JsonSerializer.Serialize(At(engine, time).Objects), JsonSerializer.Serialize(At(restored, time).Objects));
            Assert.Equal(engine.CaptureNpcDodgeThreats(), restored.CaptureNpcDodgeThreats());
            Assert.Equal(engine.CaptureNpcDodgeRngState(), restored.CaptureNpcDodgeRngState());
            Assert.Equal(ScenarioLoader.Serialize(Save(engine, time)), ScenarioLoader.Serialize(Save(restored, time)));
        }
        Assert.Equal(1UL, restored.CaptureNpcDodgeRngState().Counter);
        Assert.Equal(seed == 1, Assert.Single(restored.CaptureNpcDodgeThreats()).Evaded);
    }

    [Fact]
    public void Lowercase_point_owner_launcher_and_projectile_references_normalize_to_existing_identity()
    {
        using var engine = Battle(true);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, "MOD-PIRATE-TORPEDO-01", 0, 0, "slow", 0).Disposition);
        engine.ReceiveCommand(Launch()); At(engine);
        var save = Save(engine, 100); var point = save.GameState.PointCombatState!;
        save = save with
        {
            GameState = save.GameState with
            {
                PointCombatState = point with
                {
                    Projectiles = point.Projectiles.Select(p => p with
                    {
                        ObjectId = p.ObjectId.ToUpperInvariant(),
                        Flight = p.Flight with
                        { OwnerObjectId = p.Flight.OwnerObjectId.ToLowerInvariant(), LauncherModuleId = p.Flight.LauncherModuleId.ToLowerInvariant() }
                    }).ToArray(),
                    Encounter = point.Encounter with { EnemyIds = point.Encounter.EnemyIds.Select(id => id.ToLowerInvariant()).ToArray() }
                }
            }
        };
        using var restored = new SimulationEngine(CounterRegistry()); restored.LoadScenario(save, true);
        Assert.Equal(JsonSerializer.Serialize(At(engine, 913).Objects), JsonSerializer.Serialize(At(restored, 913).Objects));
    }

    [Fact]
    public async Task Defeated_point_world_cannot_overwrite_an_existing_slot()
    {
        var engine = PointCombatUS0008TK0001Tests.Battle(ai: false, enemyLauncherOn: true);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, "MOD-PIRATE-TORPEDO-01", 0, 0, "fast", 0).Disposition);
        Assert.Equal(SessionOutcomeState.Defeated, At(engine, 2500).EffectiveOutcome.State);
        string directory = Directory.CreateTempSubdirectory("dss-point-defeat-").FullName;
        string path = Path.Combine(directory, "slot.json"); byte[] original = [1, 2, 3, 4]; File.WriteAllBytes(path, original);
        try
        {
            await using (var connection = new LocalGameSessionConnection(engine, directory))
            {
                var error = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.SaveAsync("slot").AsTask());
                Assert.Equal(CommandReasonCodes.SessionDefeated, error.Message);
                Assert.Equal(original, File.ReadAllBytes(path)); Assert.Single(Directory.GetFiles(directory));
            }
        }
        finally { Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Won_and_acknowledged_idle_preserve_statistics_and_orphan_enemy_flight(bool acknowledge)
    {
        using var engine = CreateCounter(o => o with
        {
            PositionX = o.ObjectId == Player ? 0 : 600,
            PositionY = 0,
            SpeedMps = 0,
            MovementType = "Stationary",
            Modules = o.Modules!.Where(m => o.ObjectId != Target || m.ModuleTypeId is "module.engine.basic" or "module.torpedo.launcher.basic")
            .Select(m => m with
            {
                PowerState = m.ModuleTypeId == "module.engine.basic" ? "Off" : "On",
                StructurePoints = o.ObjectId == Target ? 100 : m.StructurePoints,
                ArmorClass = o.ObjectId == Target ? "none" : m.ArmorClass,
                Armor = o.ObjectId == Target ? 0 : m.Armor
            }).ToArray()
        }, scatter: 0, torpedoScatter: 0);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, "MOD-PIRATE-TORPEDO-01", 900, 0, "slow", 0).Disposition);
        engine.ReceiveCommand(Launch(x: 600, profile: "fast")); At(engine);
        Assert.Equal(CombatEncounterState.Won, At(engine, 4000).Encounter!.State);
        if (acknowledge) { engine.ReceiveCommand(new("ack", 1, Player, "", CombatEncounterCommandTypes.AcknowledgeResults)); At(engine, 4000); }
        var save = Save(engine, 4100); Assert.Single(save.GameState.PointCombatState!.Projectiles);
        using var restored = new SimulationEngine(CounterRegistry(scatter: 0, torpedoScatter: 0)); restored.LoadScenario(save, true);
        foreach (long time in new long[] { 4100, 5000, 63749, 64000 })
        {
            Assert.Equal(JsonSerializer.Serialize(At(engine, time).Objects), JsonSerializer.Serialize(At(restored, time).Objects));
            Assert.Equal(At(engine, time).Encounter, At(restored, time).Encounter);
            Assert.Equal(ScenarioLoader.Serialize(Save(engine, time)), ScenarioLoader.Serialize(Save(restored, time)));
        }
        Assert.Equal(1, At(restored, 64000).Encounter!.Stats.EnemiesDestroyed);
    }

    [Fact]
    public void Load_stops_held_turn_and_replayed_receipt_does_not_restart_it()
    {
        using var engine = Battle(true);
        var hold = new PlayerCommand("hold", 1, Player, "MOD-PLAYER-ENGINE-01", ShipEngineCommandTypes.TurnHold,
            TurnDirection: TurnDirection.Right, Active: true);
        engine.ReceiveCommand(hold); Assert.NotNull(At(engine).Objects.Single(o => o.ObjectId == Player).TurnHoldRateDegPerSec);
        var save = Save(engine, 457);
        double direction = save.GameState.SpaceObjects.Single(o => o.ObjectId == Player).DirectionDegrees;
        using var restored = new SimulationEngine(CounterRegistry()); restored.LoadScenario(save, true);
        restored.ReceiveCommand(hold);
        var ship = At(restored, 1457).Objects.Single(o => o.ObjectId == Player);
        Assert.Null(ship.TurnHoldRateDegPerSec); Assert.Equal(direction, ship.Direction);
    }

    [Fact]
    public void Save_during_evasion_restores_suspended_orbit_and_its_command_identity()
    {
        using var engine = CreateCounter(o => o with
        {
            PositionX = o.ObjectId == Player ? 0 : 600,
            PositionY = 0,
            SpeedMps = o.ObjectId == Player ? 2000 : 0,
            Modules = o.Modules!.Select(m => m with { PowerState = "On" }).ToArray()
        });
        engine.ReceiveCommand(new("orbit", 1, Player, "MOD-PLAYER-ENGINE-01", ShipEngineCommandTypes.Orbit, TargetWorldX: 1000, TargetWorldY: 1000));
        Assert.Equal(ShipEngineCommandTypes.Orbit, At(engine).Objects.Single(o => o.ObjectId == Player).ActiveEngineCommandType);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, "MOD-PIRATE-TORPEDO-01", 0, 0, "slow", 0).Disposition);
        string threat = Assert.Single(At(engine).Objects, o => o.ObservedProjectile is not null).ObjectId;
        engine.ReceiveCommand(new("evade", 1, Player, "MOD-PLAYER-ENGINE-01", ManeuverCommandTypes.Evade, ThreatProjectileId: threat, EvadeSide: EvadeSide.Left)); At(engine);
        var save = Save(engine, 1113); Assert.NotNull(Assert.Single(save.GameState.PointCombatState!.Evasions).OrbitCycle);
        using var restored = new SimulationEngine(CounterRegistry()); restored.LoadScenario(save, true);
        foreach (long time in new long[] { 1113, 5000, 12000, 13001, 25000 })
        {
            Assert.Equal(JsonSerializer.Serialize(At(engine, time).Objects), JsonSerializer.Serialize(At(restored, time).Objects));
            Assert.Equal(ScenarioLoader.Serialize(Save(engine, time)), ScenarioLoader.Serialize(Save(restored, time)));
        }
        Assert.Equal("orbit", Save(restored, 25000).GameState.SpaceObjects.Single(o => o.ObjectId == Player)
            .Modules!.Single(m => m.ModuleId == "MOD-PLAYER-ENGINE-01").ActiveCycle!.CommandId);
    }

    [Fact]
    public void Catalog_edit_changes_future_weapons_without_recomputing_captured_flights()
    {
        using var engine = Battle(); engine.ReceiveCommand(Launch(x: 600)); engine.ReceiveCommand(CounterLaunch(x: 250)); At(engine);
        var save = Save(engine, 100); var source = CounterRegistry();
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Select(m => m.CategoryTypeId switch
        {
            "module.torpedo.launcher" => m with { TorpedoDamage = 500, Profiles = [new("slow", 8, 9), new("medium", 10, 5), new("fast", 16, 2.5)] },
            "module.countermeasure.launcher" => m with { Profiles = [new("wide", 20, 1), new("fast", 25, .5)] },
            _ => m
        }), All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents), combatRules: source.CombatRules);
        using var restored = new SimulationEngine(registry); restored.LoadScenario(save, true);
        var loaded = At(restored, 100);
        foreach (var captured in save.GameState.PointCombatState!.Projectiles)
            Assert.Equal(captured.Flight, loaded.Objects.Single(o => o.ObjectId == captured.ObjectId).OwnProjectile);
        var launcher = loaded.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!;
        Assert.Equal(8, launcher.Profiles!.Value[0].SpeedKmS); Assert.Equal(500, launcher.Damage);
        Assert.Equal(20, loaded.InstalledModules.Single(m => m.ModuleId == Defense).Defense!.Profiles!.Value[0].SpeedKmS);
        At(restored, 12500); restored.ReceiveCommand(Launch("new-torpedo", x: 600)); restored.ReceiveCommand(CounterLaunch("new-counter", x: 250));
        var next = At(restored, 12500);
        Assert.Equal(8, next.Objects.Single(o => o.OwnProjectile?.Kind == ProjectileKind.Torpedo).OwnProjectile!.SpeedKmS);
        Assert.Equal(20, next.Objects.Single(o => o.OwnProjectile?.Kind == ProjectileKind.Countermissile).OwnProjectile!.SpeedKmS);
    }
}
