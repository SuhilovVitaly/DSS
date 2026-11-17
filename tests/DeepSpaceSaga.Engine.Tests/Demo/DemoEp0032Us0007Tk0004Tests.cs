using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0032Us0007Tk0004Tests
{
    [Fact]
    public async Task AsyncCommand_ReturnsAuthoritativeReceipt()
    {
        using var engine = DemoEp0032Us0007Tk0002Tests.CreateEngine();
        await using var connection = new LocalGameSessionConnection(engine);
        var command = new DemoEp0032Us0007Tk0002Tests.Unknown("async", 0, 2);
        var response = await connection.ExecuteDemoCampaignAsync(command);
        Assert.Equal(engine.ExecuteDemoCampaign(command), response);
        Assert.Single(engine.CaptureSaveState().GameState.DemoCampaign!.Receipts);
    }

    [Fact]
    public async Task FailedCandidateLoad_PreservesLiveSession()
    {
        using var engine = DemoEp0032Us0007Tk0002Tests.CreateEngine();
        await using var connection = new LocalGameSessionConnection(engine);
        var directory = Path.Combine("D:/DeepSpaceSaga/artifacts/validation/ep-0032", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "future.json");
            await File.WriteAllTextAsync(path, """{"saveFormatVersion":999,"gameState":{}}""");
            Assert.Throws<ScenarioException>(() => LocalGameSessionConnection.CreateFromSaveFile("unused-settings.json", path));
            var response = await connection.ExecuteDemoCampaignAsync(new DemoEp0032Us0007Tk0002Tests.Unknown("still-live", 0, 1));
            Assert.Equal("unsupported", response.Reason);
            Assert.Equal(123, engine.PlayerCredits);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task DisposedSession_RejectsMutation()
    {
        using var engine = DemoEp0032Us0007Tk0002Tests.CreateEngine();
        var connection = new LocalGameSessionConnection(engine);
        await connection.DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await connection.ExecuteDemoCampaignAsync(new DemoEp0032Us0007Tk0002Tests.Unknown("disposed", 0, 1)));
    }
}
