using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0035Us0009Tk0001Tests
{
    [Fact]
    public void Roundtrip_PreservesIdentity()
    {
        var source = new DemoCampaignSnapshot(1, 2,
            SensorOperator: new("sensor", "crew", null, 2400, 2640, 240));
        Assert.Equal(source, JsonSerializer.Deserialize<DemoCampaignSnapshot>(JsonSerializer.Serialize(source)));
        var crew = new CrewMemberSnapshot("crew", "Irina", CrewGender.Female, SensorRangeBonusPercent: 10);
        Assert.Equal(crew, JsonSerializer.Deserialize<CrewMemberSnapshot>(JsonSerializer.Serialize(crew)));
    }

    [Fact]
    public void MissingAssignment_IsExplicit()
    {
        var legacy = JsonSerializer.Deserialize<DemoCampaignSnapshot>("{\"Revision\":1,\"CommitSequence\":2}");
        Assert.Null(legacy!.SensorOperator);
        var script = new SensorOperatorSnapshot("sensor", null, SensorOperatorReasons.Unassigned, 2400, 1920, -480);
        Assert.Equal(script, JsonSerializer.Deserialize<SensorOperatorSnapshot>(JsonSerializer.Serialize(script)));
        Assert.NotEqual(SensorOperatorReasons.CrewMissing, SensorOperatorReasons.ModuleUnavailable);
        var crew = JsonSerializer.Deserialize<CrewMemberSnapshot>("{\"Id\":\"crew\",\"DisplayName\":\"Irina\",\"Gender\":0}");
        Assert.Equal(0, crew!.SensorRangeBonusPercent);
    }
}
