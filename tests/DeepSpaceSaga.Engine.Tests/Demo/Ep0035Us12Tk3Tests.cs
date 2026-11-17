using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class Ep0035Us12Tk3Tests
{
    [Fact]
    public void ShippedSquadProfile_IsExactlyTwoLairsAtTenPercent()
    {
        var definitions = DemoCampaignDefinitions.Load(DemoEp0033Us0001Tk0003Tests.Root()).Encounters!;
        Assert.Equal(10, definitions.SquadWeight);
        Assert.Equal(new[] { "lair", "lair" }, definitions.SquadMembers);
        Assert.Equal(12, definitions.SquadMembers.Sum(id => definitions.Profiles.Single(p => p.SignatureTypeId == id).DroneCount));
        Assert.Throws<ScenarioException>(() => (definitions with { SquadMembers = ["lair", "tancred"] }).Validate());
        Assert.Throws<ScenarioException>(() => (definitions with { SquadMembers = ["lair", "lair", "lair"] }).Validate());
    }
}
