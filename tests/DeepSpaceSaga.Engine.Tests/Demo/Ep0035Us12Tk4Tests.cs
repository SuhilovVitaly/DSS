using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

[CollectionDefinition("Demo process debug", DisableParallelization = true)]
public sealed class DemoProcessDebugCollection;

[Collection("Demo process debug")]
public sealed class Ep0035Us12Tk4Tests
{
    [Fact]
    public void ProcessOverride_AffectsOnlyNextNewGame_AndSurvivesLoad()
    {
        int original = SimulationEngine.ConfigurePirateSquadChance().PirateSquadChancePercent;
        try
        {
            SimulationEngine.ConfigurePirateSquadChance(100);
            using var squads = DemoEp0033Us0001Tk0003Tests.Demo();
            var save = squads.CaptureSaveState();
            Assert.All(save.GameState.DemoCampaign!.PirateWorld!.Groups, g => Assert.Equal(2, g.Members.Length));
            SimulationEngine.ConfigurePirateSquadChance(0);
            squads.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true), true);
            Assert.All(squads.CaptureSaveState().GameState.DemoCampaign!.PirateWorld!.Groups, g => Assert.Equal(2, g.Members.Length));
            using var singles = DemoEp0033Us0001Tk0003Tests.Demo();
            Assert.All(singles.CaptureSaveState().GameState.DemoCampaign!.PirateWorld!.Groups, g => Assert.Single(g.Members));
            Assert.Throws<ArgumentOutOfRangeException>(() => SimulationEngine.ConfigurePirateSquadChance(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => SimulationEngine.ConfigurePirateSquadChance(101));
            Assert.Equal(0, SimulationEngine.ConfigurePirateSquadChance().PirateSquadChancePercent);
            Assert.DoesNotContain("PirateSquadChancePercent", ScenarioLoader.Serialize(save), StringComparison.OrdinalIgnoreCase);
        }
        finally { SimulationEngine.ConfigurePirateSquadChance(original); }
    }
}
