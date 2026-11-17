using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0033Us0003Tk0001Tests
{
    [Fact]
    public void Payload_RoundTrips()
    {
        var command = new PurchaseDroneCommand("c", 1, "offer", "hangar", 0);
        Assert.Equal(command, JsonSerializer.Deserialize<PurchaseDroneCommand>(JsonSerializer.Serialize(command)));
        var equip = new EquipDroneCommand("e", 2, "drone", "weapon");
        Assert.Equal(equip, JsonSerializer.Deserialize<EquipDroneCommand>(JsonSerializer.Serialize(equip)));
    }
    [Fact]
    public void LegacyPayload_UsesSafeDefaults() => Assert.Null(JsonSerializer.Deserialize<DemoCompanySnapshot>("{}")!.Replacement);
}
