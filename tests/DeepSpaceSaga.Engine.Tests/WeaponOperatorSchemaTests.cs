using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public class WeaponOperatorSchemaTests
{
    internal static ScenarioFile Assigned(int? attack = 50, int? defense = 50) => TorpedoImpactTests.Scenario(o => o with
    {
        Crew = [new ShipCrewMemberData("operator-" + o.ObjectId, "Operator", attack, defense)],
        Modules = o.Modules?.Select(m => m with
        {
            OperatorCrewId = m.ModuleId == Launcher ? "operator-" + o.ObjectId : null,
            AutoDefenseEnabled = false
        }).ToArray()
    });

    [Theory]
    [InlineData(0, 100)]
    [InlineData(100, 0)]
    [InlineData(null, null)]
    public void Skills_and_assignments_validate(int? attack, int? defense)
    {
        using var engine = Create(Assigned(attack, defense));
        Assert.NotNull(engine.CaptureSaveState());
    }

    [Theory]
    [InlineData(-1, 50)]
    [InlineData(101, 50)]
    [InlineData(50, -1)]
    [InlineData(50, 101)]
    public void Invalid_skills_reject_atomically(int attack, int defense)
    {
        using var engine = Create(Assigned());
        var before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(Assigned(attack, defense)));
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
    public void Absent_operator_is_not_default_skill50()
    {
        var scenario = TorpedoImpactTests.Scenario(o => o with
        {
            Crew = [new ShipCrewMemberData("legacy", "Legacy")],
            Modules = o.Modules?.Select(m => m with { OperatorCrewId = null, AutoDefenseEnabled = true }).ToArray()
        });
        using var engine = Create(scenario);
        var player = engine.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectId == Player);
        Assert.Null(Assert.Single(player.Crew!).TorpedoSkill);
        Assert.Null(Assert.Single(player.Crew!).CountermeasureSkill);
        Assert.All(player.Modules!, m => { Assert.Null(m.OperatorCrewId); Assert.True(m.AutoDefenseEnabled); });
    }

    [Fact]
    public void Crew_skills_and_assignment_roundtrip()
    {
        using var engine = Create(Assigned(0, 100));
        var saved = engine.CaptureSaveState();
        using var restored = Create(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(saved), true));
        var player = restored.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectId == Player);
        Assert.Equal(0, Assert.Single(player.Crew!).TorpedoSkill);
        Assert.Equal(100, Assert.Single(player.Crew!).CountermeasureSkill);
        Assert.Equal("operator-" + Player, player.Modules!.Single(m => m.ModuleId == Launcher).OperatorCrewId);
        Assert.All(player.Modules!, m => Assert.False(m.AutoDefenseEnabled));
    }
}
