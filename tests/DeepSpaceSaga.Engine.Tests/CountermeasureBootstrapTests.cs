using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public class CountermeasureBootstrapTests
{
    internal static ScenarioFile RealScenario() => ScenarioLoader.LoadFromFile(Path.Combine(ClientRoot, "Scenarios", "PlayerShipOnly", "scenario.json"));

    [Fact]
    public void Both_ships_start_auto_defense_enabled()
    {
        using var engine = Create(RealScenario());
        var snapshot = At(engine, 0);
        Assert.All(snapshot.Objects, o =>
        {
            Assert.True(o.Defense!.AutoEnabled);
            Assert.Equal(DefenseState.Ready, o.Defense.State);
            Assert.Equal(30m, o.Defense.Operator!.EffectiveRating);
            Assert.Equal(100, o.Defense.RangeKm);
        });
        Assert.Equal(snapshot.Objects.Single(o => o.ObjectId == Player).Defense,
            Assert.Single(snapshot.InstalledModules, m => m.Defense is not null).Defense);
    }

    [Fact]
    public void Missing_operator_gives_no_operator_state()
    {
        var scenario = RealScenario();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o with
                { Modules = o.Modules!.Select(m => m with { OperatorCrewId = null }).ToArray() }).ToArray()
            }
        };
        using var engine = Create(scenario);
        Assert.All(At(engine, 0).Objects, o => { Assert.Equal(DefenseState.NoOperator, o.Defense!.State); Assert.Null(o.Defense.Operator); });
    }

    [Fact]
    public void Npc_defense_does_not_enable_npc_torpedo_fire()
    {
        using var engine = Create(RealScenario());
        engine.ReceiveCommand(new("npc-fire", 1, Target, "MOD-PIRATE-TORPEDO-01", CombatCommandTypes.Fire, Player));
        var snapshot = At(engine, 1000);
        Assert.DoesNotContain(snapshot.Objects, o => o.Torpedo is not null);
        Assert.Equal(CommandResultStatus.Rejected, Assert.Single(snapshot.CommandResults).Status);
    }
}
