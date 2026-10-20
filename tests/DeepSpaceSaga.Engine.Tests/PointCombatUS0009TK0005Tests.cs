using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.Content;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0004TK0001Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0009TK0005Tests
{
    private static IEnumerable<T> All<T>(TypeRegistry<T> source) where T : ITypeDefinition => Enumerable.Range(0, source.Count).Select(source.GetDefinition);
    private static SimulationEngine Battle()
    {
        var engine = CreateCounter(o => o with
        {
            PositionX = o.ObjectId == Player ? 0 : 600,
            PositionY = 0,
            SpeedMps = 0,
            Ai = o.ObjectId == Target ? new(25, 8, 35) : null,
            Modules = o.Modules!.Select(m => m with { PowerState = "On" }).ToArray()
        });
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, "MOD-PIRATE-TORPEDO-01", 0, 0, "slow", 0).Disposition);
        engine.ReceiveCommand(Launch(x: 600)); engine.ReceiveCommand(CounterLaunch(x: 250));
        string threat = Assert.Single(At(engine).Objects, o => o.ObservedProjectile is not null).ObjectId;
        engine.ReceiveCommand(new("evade", 1, Player, "MOD-PLAYER-ENGINE-01", ManeuverCommandTypes.Evade,
            ThreatProjectileId: threat, EvadeSide: EvadeSide.Left)); At(engine);
        return engine;
    }
    private static ScenarioFile Save(SimulationEngine engine) => engine.CaptureSaveStateForTests(400, SimulationSpeed.Speed0, 100);

    [Theory]
    [InlineData("position")]
    [InlineData("deadline")]
    [InlineData("speed")]
    [InlineData("evasion-anchor")]
    [InlineData("projectile-sequence")]
    [InlineData("scatter-counter")]
    [InlineData("npc-counter")]
    [InlineData("npc-engine")]
    [InlineData("counter-reload")]
    [InlineData("profile")]
    [InlineData("reporter")]
    [InlineData("self-relation")]
    [InlineData("missing-point-state")]
    [InlineData("missing-point-flight")]
    [InlineData("evasion-unsaved-command")]
    [InlineData("npc-owner")]
    [InlineData("npc-unknown-owner")]
    [InlineData("encounter-player-as-enemy")]
    [InlineData("encounter-unknown-projectile")]
    [InlineData("point-defense-legacy-rating")]
    [InlineData("point-defense-profile")]
    public void Invalid_point_continuation_is_rejected_without_world_or_file_mutation(string defect)
    {
        using var engine = Battle(); var before = Save(engine); var point = before.GameState.PointCombatState!;
        var own = point.Projectiles.Single(p => p.Flight.OwnerObjectId == Player && p.Flight.Kind == ProjectileKind.Torpedo);
        var changed = defect switch
        {
            "position" => own with { Flight = own.Flight with { X = own.Flight.X + 1 } },
            "deadline" => own with { Flight = own.Flight with { DetonationMotionTimeMs = own.Flight.DetonationMotionTimeMs + 1000 } },
            "speed" => own with { Flight = own.Flight with { SpeedKmS = own.Flight.SpeedKmS + 1 } },
            "profile" => own with { Flight = own.Flight with { ProfileId = "" } },
            "reporter" => own with { Flight = own.Flight with { Reporter = null } },
            "self-relation" => own with { Flight = own.Flight with { OwnerPlayerRelationAtLaunch = PlayerRelation.Neutral } },
            _ => own
        };
        point = point with { Projectiles = point.Projectiles.Select(p => p.ObjectId == own.ObjectId ? changed : p).ToArray() };
        if (defect == "evasion-anchor") point = point with
        {
            Evasions = point.Evasions.Select(e => e with
            { IntegrationAnchor = e.IntegrationAnchor! with { X = e.IntegrationAnchor!.X + 1 } }).ToArray()
        };
        if (defect == "scatter-counter") point = point with { LaunchScatter = point.LaunchScatter with { Counter = 0 } };
        if (defect == "npc-counter") point = point with { NpcDodge = point.NpcDodge with { Counter = 1 } };
        if (defect == "npc-engine") point = point with { DodgeThreats = point.DodgeThreats.Select(d => d with { EngineModuleId = "MOD-PIRATE-TORPEDO-01" }).ToArray() };
        if (defect == "npc-owner") point = point with { DodgeThreats = point.DodgeThreats.Select(d => d with { OwnerId = Player, EngineModuleId = "MOD-PLAYER-ENGINE-01" }).ToArray() };
        if (defect == "npc-unknown-owner") point = point with { DodgeThreats = point.DodgeThreats.Select(d => d with { OwnerId = "unknown-actor" }).ToArray() };
        if (defect == "missing-point-flight") point = point with { Projectiles = point.Projectiles.Where(p => p.ObjectId != own.ObjectId).ToArray() };
        if (defect == "evasion-unsaved-command") point = point with
        {
            Evasions = point.Evasions.Select(e => e with
            { Motion = e.Motion with { BaseMotion = e.Motion.BaseMotion with { ActiveEngineCommandType = ShipEngineCommandTypes.Orbit } } }).ToArray()
        };
        if (defect == "encounter-player-as-enemy") point = point with { Encounter = point.Encounter with { EnemyIds = [Player] } };
        if (defect == "encounter-unknown-projectile") point = point with { Encounter = point.Encounter with { ProjectileIds = ["unknown-flight"] } };
        var state = before.GameState with { PointCombatState = point };
        if (defect == "missing-point-state") state = state with { PointCombatState = null };
        if (defect == "projectile-sequence") state = state with { CombatState = state.CombatState! with { ProjectileSequence = 0 } };
        if (defect == "counter-reload") state = state with
        {
            DefenseState = state.DefenseState! with
            {
                Launchers = state.DefenseState.Launchers.Select(l => l.OwnerObjectId == Player && l.ModuleId == Defense
                ? l with { State = l.State with { ReadyAtMotionTimeMs = 0 } } : l).ToArray()
            }
        };
        if (defect is "point-defense-legacy-rating" or "point-defense-profile") state = state with
        {
            DefenseState = state.DefenseState! with
            {
                Launchers = state.DefenseState.Launchers.Select(l => l.OwnerObjectId == Player && l.ModuleId == Defense
                ? l with
                {
                    State = defect == "point-defense-legacy-rating" ? l.State with { Accuracy = 50 }
                    : l.State with { Profiles = [new("wide", 0, 1)] }
                } : l).ToArray()
            }
        };
        string path = Path.Combine(Path.GetTempPath(), "dss-corrupt-point-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, ScenarioLoader.Serialize(before with { GameState = state })); byte[] bytes = File.ReadAllBytes(path);
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(ScenarioLoader.LoadFromFile(path, true, true), true));
            Assert.Equal(bytes, File.ReadAllBytes(path)); Assert.Equal(ScenarioLoader.Serialize(before), ScenarioLoader.Serialize(Save(engine)));
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(300)]
    public void Zero_reload_point_defense_is_immediately_ready_and_roundtrips_overlapping_flights(double x)
    {
        var source = CounterRegistry(scatter: 0, torpedoScatter: 0);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Select(m => m.CategoryTypeId == "module.countermeasure.launcher"
            ? m with { ReloadMs = 0 } : m), All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents), combatRules: source.CombatRules);
        using var engine = new SimulationEngine(registry); engine.LoadScenario(TorpedoImpactTests.Scenario(o => o with { Ai = null }));
        engine.ReceiveCommand(CounterLaunch("zero-1", x)); engine.ReceiveCommand(CounterLaunch("zero-2", x)); var launched = At(engine);
        var defense = launched.InstalledModules.Single(m => m.ModuleId == Defense).Defense!;
        Assert.Equal(DefenseState.Ready, defense.State); Assert.Null(defense.ReloadDueMotionTimeMs); Assert.Equal(0, defense.ReadyAtMotionTimeMs);
        Assert.Equal(2, defense.ActiveProjectileIds!.Value.Length);
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0);
        Assert.Equal(0UL, save.GameState.DefenseState!.RngCounter); Assert.Equal(0UL, save.GameState.CombatState!.TorpedoHitRngCounter);
        Assert.Equal(4UL, save.GameState.PointCombatState!.LaunchScatter.Counter);
        using var restored = new SimulationEngine(registry); restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true), true);
        Assert.Equal(ScenarioLoader.Serialize(save), ScenarioLoader.Serialize(restored.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0)));
        foreach (long time in new long[] { 1, 2000, 2500 })
        {
            At(engine, time); At(restored, time);
            Assert.Equal(ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(time * 4, SimulationSpeed.Speed0, time)),
                ScenarioLoader.Serialize(restored.CaptureSaveStateForTests(time * 4, SimulationSpeed.Speed0, time)));
        }
    }

    [Fact]
    public void Point_validation_uses_no_legacy_interception_or_guidance_helper()
    {
        string root = Path.GetFullPath(Path.Combine(TorpedoImpactTests.ClientRoot, "..", "DeepSpaceSaga.Engine"));
        foreach (string relative in new[] { "Scenario/PointCombatSaveData.cs", "Scenario/PointCombatContinuationValidation.cs", "SimulationEngine.PointCombatPersistence.cs" })
        {
            string source = File.ReadAllText(Path.Combine(root, relative));
            Assert.DoesNotContain("InterceptionMath", source); Assert.DoesNotContain("TorpedoGuidanceMath", source);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Point_catalog_rejects_legacy_launcher_state_before_replacing_world(bool defense)
    {
        using var engine = CreateCounter(); var before = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0);
        using var historical = TorpedoImpactTests.Create(); var legacy = historical.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0);
        var wrong = before with
        {
            GameState = defense
            ? before.GameState with { DefenseState = legacy.GameState.DefenseState }
            : before.GameState with { CombatState = legacy.GameState.CombatState }
        };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(wrong, true));
        Assert.Equal(ScenarioLoader.Serialize(before), ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0)));
    }
}
