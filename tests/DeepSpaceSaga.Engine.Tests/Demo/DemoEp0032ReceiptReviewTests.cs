using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0032ReceiptReviewTests
{
    private const string Root = DemoEp0032Us0001Tk0002Tests.ContentRoot;
    private static string Temp()
    {
        var path = "D:/DeepSpaceSaga/artifacts/validation/ep-0032-review/fixtures/" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(path);
        return path;
    }

    [Fact]
    public void RejectionFloodHasBoundedReplayHistoryAndNoGameplayRevision()
    {
        using var engine = DemoEp0032Us0007Tk0002Tests.CreateEngine();
        for (int i = 0; i < 1100; i++) engine.ExecuteDemoCampaign(new DemoEp0032Us0007Tk0002Tests.Unknown("reject-" + i, 0, i));
        Assert.Equal(1024, engine.CaptureSaveState().GameState.DemoCampaign!.Receipts.Length);
        Assert.Equal(0, engine.CaptureSnapshot().DemoCampaign!.Revision);
        Assert.Equal(123, engine.PlayerCredits);
        var last = new DemoEp0032Us0007Tk0002Tests.Unknown("reject-1099", 0, 1099);
        var receipt = engine.ExecuteDemoCampaign(last);
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true, true);
        engine.LoadScenario(save, true);
        Assert.Equal(receipt, engine.ExecuteDemoCampaign(last));
    }
}
