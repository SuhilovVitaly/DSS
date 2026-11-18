using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0036Us0009Tk0001Tests
{
    [Fact]
    public void OldCompanyPayload_NoPrisoners() => Assert.Null(JsonSerializer.Deserialize<DemoCompanySnapshot>("{}")!.Prisoners);
    [Fact]
    public void PrisonerIdentity_RoundTrips()
    {
        var value = new PrisonerSnapshot("id", "ship", "battle", "drone", "pilot", "Name", "face", 123, CrewGender.Male);
        Assert.Equal(value, JsonSerializer.Deserialize<PrisonerSnapshot>(JsonSerializer.Serialize(value)));
    }
    [Fact]
    public void CapacityDefaults_NoReservedPeople() => Assert.Equal(0,
        JsonSerializer.Deserialize<PassengerCapacitySnapshot>("{}")!.PrisonerCabins);
    [Fact]
    public void ArrestCommand_UsesCampaignIdentity()
    {
        DemoCampaignCommand command = new ArrestPilotCommand("request", 7, "battle", "drone");
        Assert.Equal("request", command.CommandId);
        Assert.Equal(7, command.ExpectedRevision);
        Assert.Equal("battle", ((ILootCampaignCommand)command).BattleId);
    }
}
