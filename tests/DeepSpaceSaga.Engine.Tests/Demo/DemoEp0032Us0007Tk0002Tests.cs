using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0032Us0007Tk0002Tests
{
    internal static SimulationEngine CreateEngine()
    {
        var engine = new SimulationEngine();
        engine.LoadScenario(new(new("test", "Test"), new(0, "Speed0", "player", null,
            [new("player", "PlayerShip", "Permanent", "Player", 0, 0, 0, 0, "Stationary")],
            MasterSeed: 42, PlayerTokens: 123)));
        return engine;
    }

    public sealed record Unknown(string CommandId, long ExpectedRevision, int Amount)
        : DemoCampaignCommand(CommandId, ExpectedRevision);

    [Fact]
    public void DuplicateChangedPayload_IsRejected()
    {
        using var engine = CreateEngine();
        var command = new Unknown("identity", 0, 1);
        var receipt = engine.ExecuteDemoCampaign(command);
        Assert.Equal("unsupported", receipt.Reason);
        Assert.Equal(receipt, engine.ExecuteDemoCampaign(command));
        Assert.Equal("identity_conflict", engine.ExecuteDemoCampaign(command with { Amount = 2 }).Reason);
        Assert.Equal(123, engine.PlayerCredits);
        Assert.Single(engine.CaptureSaveState().GameState.DemoCampaign!.Receipts);
    }

    [Fact]
    public void Save21Migration_DoesNotGrantStarter()
    {
        using var engine = CreateEngine();
        var save = engine.CaptureSaveState();
        engine.LoadScenario(save with { SaveFormatVersion = 21, GameState = save.GameState with { DemoCampaign = null } }, true);
        var restored = engine.CaptureSaveState();
        Assert.Equal(22, restored.SaveFormatVersion);
        Assert.Equal(123, restored.GameState.PlayerTokens);
        Assert.Null(restored.GameState.SpaceObjects[0].CarrierState);
        Assert.Empty(restored.GameState.DemoCampaign!.Receipts);
    }

    [Fact]
    public void CaptureAndTick_HaveSameCommit()
    {
        using var snapshotEngine = CreateEngine();
        using var saveEngine = CreateEngine();
        snapshotEngine.CaptureSnapshotForTests(300, SimulationSpeed.Speed0, 1);
        var first = snapshotEngine.CaptureSaveStateForTests(300, SimulationSpeed.Speed0, 1);
        var second = saveEngine.CaptureSaveStateForTests(300, SimulationSpeed.Speed0, 1);
        Assert.Equal(first.GameState.DemoCampaign, second.GameState.DemoCampaign);
        Assert.Equal(300, second.GameState.DemoCampaign!.LastTickGameTimeMs);
        Assert.Equal(0, second.GameState.DemoCampaign.CommitSequence);
    }
}
