using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public class WeaponOperatorSchemaTests
{
    [Fact]
    public void Duplicate_operator_assignment_rejected()
    {
        using var engine = Create(CountermeasureBootstrapTests.RealScenario());
        string before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        var scenario = CountermeasureBootstrapTests.RealScenario();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o.ObjectId == Player ? o with
                {
                    Modules = o.Modules!.Select(m => m.ModuleId == "MOD-PLAYER-COUNTERMEASURE-02"
                        ? m with { OperatorCrewId = "CHR-PLAYER-DEFENSE" } : m).ToArray()
                } : o).ToArray()
            }
        };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(scenario));
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    internal static ScenarioFile Assigned(int accuracy = 0, int maneuverability = 0) => TorpedoImpactTests.Scenario(o => o with
    {
        Crew = [new ShipCrewMemberData("operator-" + o.ObjectId, "Operator", accuracy, maneuverability)],
        Modules = o.Modules?.Select(m => m with
        {
            OperatorCrewId = m.ModuleId == Launcher ? "operator-" + o.ObjectId : null,
            AutoDefenseEnabled = false
        }).ToArray()
    });

    [Theory]
    [InlineData(0, 30)]
    [InlineData(30, 0)]
    [InlineData(0, 0)]
    public void Skills_and_assignments_validate(int accuracy, int maneuverability)
    {
        using var engine = Create(Assigned(accuracy, maneuverability));
        Assert.NotNull(engine.CaptureSaveState());
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(31, 0)]
    [InlineData(0, -1)]
    [InlineData(0, 31)]
    public void Invalid_skills_reject_atomically(int accuracy, int maneuverability)
    {
        using var engine = Create(Assigned());
        var before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(Assigned(accuracy, maneuverability)));
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    [Fact]
    public void Crew_reference_cannot_cross_ships()
    {
        var scenario = Assigned();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o.ObjectId != Player ? o : o with
                {
                    Modules = o.Modules!.Select(m => m with { OperatorCrewId = "operator-" + Target }).ToArray()
                }).ToArray()
            }
        };
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(scenario)));
    }

    [Fact]
    public void Duplicate_assignment_rejected()
    {
        var scenario = Assigned();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o.ObjectId != Player ? o : o with
                {
                    Modules = o.Modules!.Select(m => m with { OperatorCrewId = "operator-" + Player }).ToArray()
                }).ToArray()
            }
        };
        Assert.Throws<ScenarioException>(() => Create(scenario));
    }

    [Fact]
    public void Absent_operator_does_not_gain_a_skill()
    {
        var scenario = TorpedoImpactTests.Scenario(o => o with
        {
            Crew = [new ShipCrewMemberData("legacy", "Legacy")],
            Modules = o.Modules?.Select(m => m with { OperatorCrewId = null, AutoDefenseEnabled = true }).ToArray()
        });
        using var engine = Create(scenario);
        var player = engine.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectId == Player);
        Assert.Equal(0, Assert.Single(player.Crew!).AccuracySkill);
        Assert.Equal(0, Assert.Single(player.Crew!).ManeuverabilitySkill);
        Assert.All(player.Modules!, m => { Assert.Null(m.OperatorCrewId); Assert.True(m.AutoDefenseEnabled); });
    }

    [Fact]
    public void Crew_skills_and_assignment_roundtrip()
    {
        using var engine = Create(Assigned(0, 30));
        var saved = engine.CaptureSaveState();
        using var restored = Create(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(saved), true));
        var player = restored.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectId == Player);
        Assert.Equal(0, Assert.Single(player.Crew!).AccuracySkill);
        Assert.Equal(30, Assert.Single(player.Crew!).ManeuverabilitySkill);
        Assert.Equal("operator-" + Player, player.Modules!.Single(m => m.ModuleId == Launcher).OperatorCrewId);
        Assert.All(player.Modules!, m => Assert.False(m.AutoDefenseEnabled));
    }

    [Fact]
    public void Obsolete_crew_skill_fields_are_rejected_by_json_schema()
    {
        var json = ScenarioLoader.Serialize(Assigned());
        Assert.DoesNotContain("\"torpedoSkill\"", json);
        Assert.DoesNotContain("\"countermeasureSkill\"", json);
        var withOldSkill = json.Replace("\"accuracySkill\": 0", "\"torpedoSkill\": 50, \"accuracySkill\": 0", StringComparison.Ordinal);
        Assert.NotEqual(json, withOldSkill);
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(withOldSkill));
    }

    [Theory]
    [InlineData(31, 0, "accuracySkill")]
    [InlineData(-1, 0, "accuracySkill")]
    [InlineData(0, 31, "maneuverabilitySkill")]
    [InlineData(0, -1, "maneuverabilitySkill")]
    public void Skill_31_rejected(int accuracy, int maneuverability, string field)
    {
        var scenario = Assigned() with
        {
            GameState = Assigned().GameState with
            {
                SpaceObjects = Assigned().GameState.SpaceObjects.Select(o => o.ObjectId != Player ? o : o with
                {
                    Crew = o.Crew!.Select(c => c with
                    {
                        AccuracySkill = accuracy,
                        ManeuverabilitySkill = maneuverability
                    }).ToArray()
                }).ToArray()
            }
        };
        using var engine = Create(Assigned());
        string before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        var error = Assert.Throws<ScenarioException>(() => engine.LoadScenario(scenario));
        Assert.Contains(field, error.Message, StringComparison.Ordinal);
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    [Fact]
    public void New_skills_roundtrip_and_legacy_scenario_defaults()
    {
        var legacy = Assigned();
        using var legacyEngine = Create(legacy);
        var legacyCrew = legacyEngine.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectId == Player).Crew!.Single();
        Assert.Equal(0, legacyCrew.AccuracySkill);
        Assert.Equal(0, legacyCrew.ManeuverabilitySkill);

        var skilled = legacy with
        {
            GameState = legacy.GameState with
            {
                SpaceObjects = legacy.GameState.SpaceObjects.Select(o => o.ObjectId != Player ? o : o with
                {
                    Crew = o.Crew!.Select(c => c with { AccuracySkill = 20, ManeuverabilitySkill = 30 }).ToArray()
                }).ToArray()
            }
        };
        using var engine = Create(skilled);
        using var restored = Create(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true));
        var crew = restored.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectId == Player).Crew!.Single();
        Assert.Equal(20, crew.AccuracySkill);
        Assert.Equal(30, crew.ManeuverabilitySkill);
    }
}
