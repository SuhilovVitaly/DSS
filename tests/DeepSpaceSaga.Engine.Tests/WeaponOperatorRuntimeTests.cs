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
        var scenario = TorpedoImpactTests.Scenario(o => o with
        {
            Crew = o.Crew?.Select(c => c with { TorpedoSkill = 80, CountermeasureSkill = 10 }).ToArray()
        });
        using var engine = Create(scenario);
        var modules = At(engine, 0).InstalledModules;
        var attack = modules.Single(m => m.ModuleId == Launcher).Operator!;
        var defense = modules.Single(m => m.ModuleTypeId == "module.countermeasure.launcher.basic").Operator!;
        Assert.NotEqual(attack.CrewId, defense.CrewId);
        Assert.Equal(WeaponSkillType.TorpedoAttack, attack.SkillType);
        Assert.Equal(WeaponSkillType.CountermeasureDefense, defense.SkillType);
        Assert.Equal(48m, attack.EffectiveRating);
        Assert.Equal(6m, defense.EffectiveRating);
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
                DefenseState = null,
                SpaceObjects = saved.GameState.SpaceObjects.Select(o => o with
                {
                    Crew = o.Crew?.Select(c => c with { TorpedoSkill = null, CountermeasureSkill = null }).ToArray(),
                    Modules = o.Modules?.Select(m => m with { OperatorCrewId = null }).ToArray()
                }).ToArray(),
                CombatState = combat with
                {
                    Launchers = combat.Launchers.Select(l => l with { State = l.State with { Operator = null } }).ToArray(),
                    Projectiles = combat.Projectiles.Select(p => p with
                    { Flight = p.Flight with { TorpedoRating = null, RatingBreakdown = null } }).ToArray()
                }
            }
        };
        using var restored = Create(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(saved), true));
        var flight = Assert.Single(At(restored, 0).Objects, o => o.Torpedo is not null).Torpedo!;
        Assert.Equal(30m, flight.TorpedoRating);
        Assert.True(flight.RatingMigratedFromLegacySave);
        Assert.Null(flight.RatingBreakdown);
        using var twice = Create(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(restored.CaptureSaveState()), true));
        Assert.True(Assert.Single(At(twice, 0).Objects, o => o.Torpedo is not null).Torpedo!.RatingMigratedFromLegacySave);
        Assert.Equal(Target, Assert.Single(At(twice, 5000).CombatImpacts).HitObjectId);
        twice.ReceiveCommand(Fire("new-launch"));
        Assert.Equal("no_weapon_operator", At(twice, 5000).CommandResults.Single(r => r.CommandId == "new-launch").ReasonCode);
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
        using var engine = Create(WeaponOperatorSchemaTests.Assigned(77, 50));
        engine.ReceiveCommand(Fire("capture"));
        var launched = Assert.Single(At(engine, 0).Objects, o => o.Torpedo is not null).Torpedo!;
        Assert.Equal(46.2m, launched.TorpedoRating);
        Assert.Equal(77, launched.RatingBreakdown!.Skill);
        var saved = engine.CaptureSaveState();
        saved = saved with
        {
            GameState = saved.GameState with
            {
                SpaceObjects = saved.GameState.SpaceObjects.Select(o => o with
                {
                    Crew = o.Crew?.Select(c => c with { TorpedoSkill = 1, DisplayName = "Changed" }).ToArray()
                }).ToArray()
            }
        };
        string path = Path.Combine(Path.GetTempPath(), "operator-flight-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            File.WriteAllText(path, ScenarioLoader.Serialize(saved));
            using var restored = EngineContentLoader.CreateEngineFromSaveFile(Path.Combine(ClientRoot, "Settings.json"), path);
            var snapshot = At(restored, 100);
            var flight = Assert.Single(snapshot.Objects, o => o.Torpedo is not null).Torpedo!;
            Assert.Equal(46.2m, flight.TorpedoRating);
            Assert.Equal(launched.RatingBreakdown, flight.RatingBreakdown);
            Assert.Equal(0.6m, snapshot.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.Operator!.EffectiveRating);
            Assert.Equal(Target, Assert.Single(At(restored, 5000).CombatImpacts).HitObjectId);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(50, 30)]
    [InlineData(100, 60)]
    public void Zero_skill_operator_has_zero_rating(int skill, int expected)
    {
        using var engine = Create(WeaponOperatorSchemaTests.Assigned(skill));
        engine.ReceiveCommand(Fire("skill"));
        var flight = Assert.Single(At(engine, 0).Objects, o => o.Torpedo is not null).Torpedo!;
        Assert.Equal((decimal)expected, flight.TorpedoRating);
        Assert.NotNull(flight.RatingBreakdown);
    }

    [Fact]
    public void Operator_projection_is_authoritative()
    {
        var source = WeaponOperatorSchemaTests.Assigned(80, 10);
        source = source with
        {
            GameState = source.GameState with
            {
                SpaceObjects = source.GameState.SpaceObjects.Select(o => o.ObjectId == Target ? o with { IsKnown = false } : o).ToArray()
            }
        };
        using var engine = Create(source);
        var snapshot = At(engine, 0);
        var weaponOperator = snapshot.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.Operator!;
        Assert.Equal("operator-" + Player, weaponOperator.CrewId);
        Assert.Equal(WeaponSkillType.TorpedoAttack, weaponOperator.SkillType);
        Assert.Equal(80, weaponOperator.Skill);
        Assert.Equal(30m, weaponOperator.BaseRating);
        Assert.Equal(48m, weaponOperator.EffectiveRating);
        var unknown = snapshot.Objects.Single(o => o.ObjectId == Target);
        Assert.Null(unknown.HullCombat);
        Assert.Null(unknown.Defense);
        Assert.DoesNotContain("operator-" + Target, JsonSerializer.Serialize(snapshot));
    }
}
