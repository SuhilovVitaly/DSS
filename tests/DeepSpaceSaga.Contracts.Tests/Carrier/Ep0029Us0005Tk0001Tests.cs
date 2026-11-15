using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

/// <summary>
/// EP-0029 US-0005 TK-0001 — owned sortie request identity.
/// </summary>
public class Ep0029Us0005Tk0001Tests
{
    [Fact]
    public void OwnedStartSerializesIdentity()
    {
        var request = new StartTacticalBattleRequest("cmd", 7, "initial")
        {
            SourceShipId = "SPC-0001",
            ExpectedCarrierRevision = 42,
        };
        var copy = JsonSerializer.Deserialize<StartTacticalBattleRequest>(JsonSerializer.Serialize(request))!;
        Assert.Equal("SPC-0001", copy.SourceShipId);
        Assert.Equal(42, copy.ExpectedCarrierRevision);
        Assert.Equal("initial", copy.ScenarioId);
    }

    [Fact]
    public void OldRequestsStillDeserialize()
    {
        const string oldJson = """{"CommandId":"cmd","Seed":7,"ScenarioId":"initial"}""";
        var legacy = JsonSerializer.Deserialize<StartTacticalBattleRequest>(oldJson)!;
        Assert.Null(legacy.SourceShipId);
        Assert.Equal(0, legacy.ExpectedCarrierRevision); // 0 = no owned entry, not a wildcard

        // Appended rejection codes keep old numeric identities.
        Assert.Equal(26, (int)TacticalBattleRejectionCode.NoAmmo);
        Assert.Equal(27, (int)TacticalBattleRejectionCode.WrongShip);
        Assert.Equal(30, (int)TacticalBattleRejectionCode.StaleCarrierState);
    }
}
