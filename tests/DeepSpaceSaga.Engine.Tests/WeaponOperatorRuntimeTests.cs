using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public class WeaponOperatorRuntimeTests
{
    [Fact]
    public void Both_weapon_families_project_their_own_operator_skill()
    {
        var scenario = TorpedoImpactTests.Scenario();
        using var engine = Create(scenario);
        var modules = At(engine, 0).InstalledModules;
        var attack = modules.Single(m => m.ModuleId == Launcher).Operator!;
        var defense = modules.Single(m => m.ModuleId == "MOD-PLAYER-COUNTERMEASURE-01").Operator!;
        Assert.NotEqual(attack.CrewId, defense.CrewId);
        Assert.Equal(WeaponSkillType.TorpedoAttack, attack.SkillType);
        Assert.Equal(WeaponSkillType.CountermeasureDefense, defense.SkillType);
        Assert.Equal(5m, attack.EffectiveRating);
        Assert.Equal(55m, defense.EffectiveRating);
        Assert.Null(modules.Single(m => m.ModuleId == "MOD-PLAYER-ENGINE-01").Operator);
    }
    [Fact]
    public void Legacy_active_torpedo_migrates_rating_without_assigning_operator()
    {
        using var engine = Create(WeaponOperatorSchemaTests.Assigned());
        engine.ReceiveCommand(Fire("old-launch"));
        At(engine, 0);
        var saved = engine.CaptureSaveState();
        var combat = saved.GameState.CombatState!;
        saved = saved with
        {
            SaveFormatVersion = 10,
            GameState = saved.GameState with
            {
                TradingEconomyContinuation = null,
                MarketKnowledge = null,
                DefenseState = null,
                HostileCombatState = saved.GameState.HostileCombatState! with { Version = 2 },
                SpaceObjects = saved.GameState.SpaceObjects.Select(o => o with
                {
                    Modules = o.Modules?.Select(m => m with { OperatorCrewId = null }).ToArray()
                }).ToArray(),
                CombatState = combat with
                {
                    Launchers = combat.Launchers.Select(l => l with { State = l.State with { Operator = null } }).ToArray(),
                    Projectiles = combat.Projectiles.Select(p => p with
                    {
                        Flight = p.Flight with
                        {
                            TorpedoRating = null,
                            RatingBreakdown = null,
                            CapturedManeuverability = null,
                            OwnerPlayerRelationAtLaunch = null
                        }
                    }).ToArray()
                }
            }
        };
        using var restored = Create(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(saved), true));
        var flight = Assert.Single(At(restored, 0).Objects, o => o.Torpedo is not null).Torpedo!;
        Assert.Equal(30m, flight.TorpedoRating);
        Assert.True(flight.RatingMigratedFromLegacySave);
        Assert.Null(flight.RatingBreakdown);
        var migrated = restored.CaptureSaveState();
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(migrated), true, isSave: true));
        // Historical internal fixture still exercises migration; it cannot become a current save.
        using var twice = Create(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(migrated with
        {
            SaveFormatVersion = 15,
            GameState = migrated.GameState with { HostileCombatState = migrated.GameState.HostileCombatState! with { Version = 2 } }
        }), true));
        Assert.True(Assert.Single(At(twice, 0).Objects, o => o.Torpedo is not null).Torpedo!.RatingMigratedFromLegacySave);
        Assert.Equal(Target, Assert.Single(At(twice, 5000).CombatImpacts).HitObjectId);
        At(twice, 15000);
        twice.ReceiveCommand(Fire("new-launch"));
        Assert.Equal("no_weapon_operator", At(twice, 15000).CommandResults.Single(r => r.CommandId == "new-launch").ReasonCode);
    }

    [Fact]
    public void Missing_operator_blocks_fire_without_side_effects()
    {
        using var engine = Create(TorpedoImpactTests.Scenario(o => o with
        {
            Modules = o.Modules?.Select(m => m with { OperatorCrewId = null }).ToArray()
        }));
        var before = engine.CaptureSaveState();
        engine.ReceiveCommand(Fire("no-operator"));
        var snapshot = At(engine, 0);
        Assert.DoesNotContain(snapshot.Objects, o => o.Torpedo is not null);
        Assert.Equal("no_weapon_operator", Assert.Single(snapshot.CommandResults).ReasonCode);
        var after = engine.CaptureSaveState();
        Assert.Equal(JsonSerializer.Serialize(before.GameState.CombatState), JsonSerializer.Serialize(after.GameState.CombatState));
        Assert.Equal(JsonSerializer.Serialize(before.GameState.SpaceObjects), JsonSerializer.Serialize(after.GameState.SpaceObjects));
        Assert.Equal(JsonSerializer.Serialize(before.GameState.DialogueState), JsonSerializer.Serialize(after.GameState.DialogueState));
        Assert.Equal(JsonSerializer.Serialize(before.GameState.TradingMap?.RngStreams), JsonSerializer.Serialize(after.GameState.TradingMap?.RngStreams));
    }

    [Fact]
    public void Torpedo_rating_is_captured_at_launch()
    {
        var scenario = WeaponOperatorSchemaTests.Assigned();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o with
                { Crew = o.Crew?.Select(c => c with { ManeuverabilitySkill = 20 }).ToArray() }).ToArray()
            }
        };
        using var engine = Create(scenario);
        engine.ReceiveCommand(Fire("capture"));
        var launched = Assert.Single(At(engine, 0).Objects, o => o.Torpedo is not null).Torpedo!;
        Assert.Equal(25m, launched.TorpedoRating);
        Assert.Equal(20, launched.RatingBreakdown!.Skill);
        var saved = engine.CaptureSaveState();
        saved = saved with
        {
            GameState = saved.GameState with
            {
                SpaceObjects = saved.GameState.SpaceObjects.Select(o => o with
                {
                    Crew = o.Crew?.Select(c => c with { ManeuverabilitySkill = 1, DisplayName = "Changed" }).ToArray()
                }).ToArray()
            }
        };
        string path = Path.Combine(Path.GetTempPath(), "operator-flight-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, ScenarioLoader.Serialize(saved));
            using var restored = EngineContentLoader.CreateEngineFromSaveFile(global::DeepSpaceSaga.TestSupport.LegacyCombatCatalog.Settings(Path.Combine(ClientRoot, "Settings.json")), path);
            var snapshot = At(restored, 100);
            var flight = Assert.Single(snapshot.Objects, o => o.Torpedo is not null).Torpedo!;
            Assert.Equal(25m, flight.TorpedoRating);
            Assert.Equal(launched.RatingBreakdown, flight.RatingBreakdown);
            Assert.Equal(51m, snapshot.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.Operator!.EffectiveRating);
            Assert.Equal(Target, Assert.Single(At(restored, 5000).CombatImpacts).HitObjectId);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Zero_new_skills_keep_base_weapon_chance()
    {
        using var engine = Create(AutomaticDefenseTests.DefenseScenario());
        engine.ReceiveCommand(Fire("skill"));
        var flight = Assert.Single(At(engine, 0).Objects, o => o.Torpedo is not null).Torpedo!;
        Assert.Equal(5m, flight.TorpedoRating);
        Assert.NotNull(flight.RatingBreakdown);
        Assert.Equal(5m, flight.CapturedManeuverability);
        Assert.Equal(500, DeepSpaceSaga.Engine.Combat.InterceptionMath.ChanceTenths(55, flight.CapturedManeuverability!.Value));
        var interception = Assert.Single(At(engine, 1000).Objects, o => o.Countermeasure is not null).Countermeasure!;
        Assert.Equal(55m, interception.CapturedAccuracy);
        Assert.Equal(500, interception.FrozenChanceTenths);
        Assert.Equal(0, interception.RatingBreakdown.DefenseOperator.Skill);
    }

    [Fact]
    public void Frozen_values_survive_catalog_changes()
    {
        using var engine = Create(AutomaticDefenseTests.DefenseScenario());
        engine.ReceiveCommand(Fire("frozen"));
        At(engine, 0);
        var snapshot = At(engine, 1000);
        var original = Assert.Single(snapshot.Objects, o => o.Countermeasure is not null).Countermeasure!;
        Assert.Equal(55m, original.CapturedAccuracy);
        Assert.Equal(5m, original.CapturedTargetManeuverability);
        Assert.Equal(500, original.FrozenChanceTenths);
        string path = Path.Combine(Path.GetTempPath(), $"frozen-weapons-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(1000, SimulationSpeed.Speed0)));
            using var loaded = new SimulationEngine(AutomaticDefenseTests.WeaponRegistry(20, 90));
            loaded.LoadScenario(ScenarioLoader.LoadFromFile(global::DeepSpaceSaga.TestSupport.LegacyCombatCatalog.Scenario(path), allowNonZeroGameTime: true), isSave: true);
            var restored = Assert.Single(At(loaded, 1000).Objects, o => o.Countermeasure is not null).Countermeasure!;
            Assert.Equal(original.CapturedAccuracy, restored.CapturedAccuracy);
            Assert.Equal(original.CapturedTargetManeuverability, restored.CapturedTargetManeuverability);
            Assert.Equal(original.FrozenChanceTenths, restored.FrozenChanceTenths);
            Assert.Equal(5m, Assert.Single(At(loaded, 1000).Objects, o => o.Torpedo is not null).Torpedo!.CapturedManeuverability);
            var result = Assert.Single(At(loaded, 5000).CombatJournal, e => e.Roll is not null);
            Assert.Equal(500, result.ChanceTenths);
            Assert.Equal(801, result.Roll);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Operator_projection_is_authoritative()
    {
        var source = WeaponOperatorSchemaTests.Assigned();
        source = source with
        {
            GameState = source.GameState with
            {
                SpaceObjects = source.GameState.SpaceObjects.Select(o => o.ObjectId == Target ? o with { IsKnown = false } : o with
                { Crew = o.Crew?.Select(c => c with { ManeuverabilitySkill = 20 }).ToArray() }).ToArray()
            }
        };
        using var engine = Create(source);
        var snapshot = At(engine, 0);
        var weaponOperator = snapshot.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.Operator!;
        Assert.Equal("operator-" + Player, weaponOperator.CrewId);
        Assert.Equal(WeaponSkillType.TorpedoAttack, weaponOperator.SkillType);
        Assert.Equal(20, weaponOperator.Skill);
        Assert.Equal(5m, weaponOperator.BaseRating);
        Assert.Equal(25m, weaponOperator.EffectiveRating);
        var unknown = snapshot.Objects.Single(o => o.ObjectId == Target);
        Assert.Null(unknown.HullCombat);
        Assert.Null(unknown.Defense);
        Assert.DoesNotContain("operator-" + Target, JsonSerializer.Serialize(snapshot));
    }
}
