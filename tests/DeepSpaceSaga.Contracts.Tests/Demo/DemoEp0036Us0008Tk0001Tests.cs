using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0036Us0008Tk0001Tests
{
    [Fact]
    public void OldCrewPayload_DefaultBonusZero() => Assert.Equal(0,
        JsonSerializer.Deserialize<CrewMemberSnapshot>("""{"Id":"crew","DisplayName":"Alex","Gender":1}""")!.SalvageBonusPercentagePoints);

    [Fact]
    public void EngineerRole_PreservesExistingEnumValues()
    {
        Assert.Equal(0, (int)CrewRole.Captain);
        Assert.Equal(1, (int)CrewRole.Officer);
        Assert.Equal(2, (int)CrewRole.Pilot);
        Assert.Equal(3, (int)CrewRole.Engineer);
    }

    [Fact]
    public void EngineerBonus_RoundTrips()
    {
        var crew = new CrewMemberSnapshot("engineer", "Alex", CrewGender.Male, Role: CrewRole.Engineer, SalvageBonusPercentagePoints: 13);
        Assert.Equal(crew, JsonSerializer.Deserialize<CrewMemberSnapshot>(JsonSerializer.Serialize(crew)));
    }
}
