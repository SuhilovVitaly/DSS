using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0032Us0007Tk0003Tests
{
    [Fact]
    public void Save22_RoundTrips()
    {
        using var engine = DemoEp0032Us0007Tk0002Tests.CreateEngine();
        var command = new DemoEp0032Us0007Tk0002Tests.Unknown("durable", 0, 4);
        var original = engine.ExecuteDemoCampaign(command);
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true, true);
        using var restored = DemoEp0032Us0007Tk0002Tests.CreateEngine();
        restored.LoadScenario(save, true);
        Assert.Equal(original, restored.ExecuteDemoCampaign(command));
        Assert.Equal(22, save.SaveFormatVersion);
        Assert.Single(restored.CaptureSaveState().GameState.DemoCampaign!.Receipts);
    }

    [Fact]
    public void CorruptReceipt_RejectsBeforePublish()
    {
        using var engine = DemoEp0032Us0007Tk0002Tests.CreateEngine();
        engine.ExecuteDemoCampaign(new DemoEp0032Us0007Tk0002Tests.Unknown("duplicate", 0, 1));
        var save = engine.CaptureSaveState();
        var data = save.GameState.DemoCampaign!;
        var corrupt = save with { GameState = save.GameState with
        {
            PlayerTokens = 999,
            DemoCampaign = data with { Receipts = data.Receipts.Add(data.Receipts[0]) }
        } };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(corrupt, true));
        Assert.Equal(123, engine.PlayerCredits);
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(corrupt), true, true));
    }

    [Theory]
    [InlineData(20)]
    [InlineData(21)]
    public void ExistingSupportedSaves_StillLoad(int version)
    {
        using var engine = DemoEp0032Us0007Tk0002Tests.CreateEngine();
        var save = engine.CaptureSaveState();
        save = save with { SaveFormatVersion = version, GameState = save.GameState with { DemoCampaign = null } };
        var loaded = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true);
        engine.LoadScenario(loaded, true);
        Assert.Equal(123, engine.PlayerCredits);
        Assert.Null(engine.CaptureSaveState().GameState.SpaceObjects[0].CarrierState);
    }
}
