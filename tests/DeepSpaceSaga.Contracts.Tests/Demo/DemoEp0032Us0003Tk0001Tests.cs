using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

public sealed class DemoEp0032Us0003Tk0001Tests
{
    [Fact]
    public void TerminalOutcomeRoundtripsWithoutResurrectionAction()
    {
        var outcome = new CampaignOutcomeSnapshot(CampaignOutcomeKind.CarrierDestroyed, "battle", false, "carrier_destroyed");
        Assert.Equal(outcome, JsonSerializer.Deserialize<CampaignOutcomeSnapshot>(JsonSerializer.Serialize(outcome)));
        Assert.Null(new DemoCompanySnapshot().Outcome);
    }
}
