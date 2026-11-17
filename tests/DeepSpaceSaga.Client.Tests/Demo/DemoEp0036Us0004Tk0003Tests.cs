using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0004Tk0003Tests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualLoadRouterAdoptsPendingCombatOrFrozenLootWithoutCommand(bool finished)
    {
        using var engine = DemoEp0036Us0007Tk0004Tests.Won(finished, finished);
        var campaign = engine.CaptureSnapshot().DemoCampaign!;
        var saved = finished ? engine.CaptureSaveState() : engine.CapturePendingEncounterCheckpoint(
            new("checkpoint", campaign.Revision, campaign.WorldEncounter!.Binding.EncounterId));
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(saved), true, true), true);
        await using var connection = new LocalGameSessionConnection(engine);
        await using var handle = new GameSessionHandle(connection);
        await DemoEp0036Us0007Tk0004Tests.WaitFor(() => handle.DemoCampaign is not null);
        long revision = handle.DemoCampaign!.Revision;
        var flow = await SkiaWindow.RecoverBattleFlowAsync(handle);
        Assert.NotNull(flow);
        Assert.Equal(finished ? TacticalBattlePhase.Over : engine.GetTacticalBattleState()!.Phase, flow.State!.Phase);
        Assert.Equal(revision, engine.CaptureSnapshot().DemoCampaign!.Revision);
        Assert.Equal(!finished, engine.IsTacticalBattleActive);
        if (finished)
        {
            var screen = new TacticalBattleScreen(flow, 1);
            Assert.Equal(TacticalBattlePhase.Over, screen.CaptureFrame(1280, 720)!.Battle.Phase);
            screen.OnKeyDown(Silk.NET.Input.Key.Escape, DeepSpaceSaga.Client.UI.Screens.KeyModifiers.None, false);
            Assert.NotNull(screen.LootWindow);
            Assert.False(screen.LootWindow.RequestExit());
            flow.ConfirmLootDiscard = true;
            Assert.True(await flow.ExitAsync());
            await DemoEp0036Us0007Tk0004Tests.WaitFor(() => handle.DemoCampaign?.Rewards?.Loot?.Phase == LootPhases.Closed);
            Assert.Null(await SkiaWindow.RecoverBattleFlowAsync(handle));
            Assert.False(engine.IsTacticalBattleActive);
        }
    }
}
