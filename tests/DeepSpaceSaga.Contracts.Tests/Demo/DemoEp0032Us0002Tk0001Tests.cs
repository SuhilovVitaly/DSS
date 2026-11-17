using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0032Us0002Tk0001Tests
{
    [Fact]
    public void MemorialRoundtripRetainsIdentityAndSkills()
    {
        var pilot = new CrewMemberSnapshot("crew", "Name", CrewGender.Female, "portrait", CrewRole.Pilot, 4, 5, 6);
        var state = new PilotLossSnapshot([new(pilot, "battle", 12)], ["captain"]);
        var restored = JsonSerializer.Deserialize<PilotLossSnapshot>(JsonSerializer.Serialize(state))!;
        Assert.Equal(pilot, Assert.Single(restored.Memorial).Pilot);
        Assert.Equal("captain", Assert.Single(restored.LiveCrewIds));
        Assert.Equal(12, restored.Memorial[0].GameTimeMs);
    }
}
