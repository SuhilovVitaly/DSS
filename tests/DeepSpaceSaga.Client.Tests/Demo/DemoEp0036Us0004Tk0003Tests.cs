using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests.Demo;

[Collection("InterfaceLog")]
public sealed class DemoEp0036Us0004Tk0003Tests
{
    [Fact]
    public async Task ActualWindowF5WritesOpenLootWithoutClosingMap()
    {
        string directory = Directory.CreateDirectory("D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/manual-save-" + Guid.NewGuid().ToString("N")).FullName;
        try
        {
            using var source = DemoEp0036Us0007Tk0004Tests.Won();
            await using var fixture = await TacticalBattle.BattleNavigationTests.Fixture.Create(saveDirectory: directory, initialSpeed: SimulationSpeed.Speed0);
            fixture.Engine.LoadScenario(source.CaptureSaveState(), true);
            await DemoEp0036Us0007Tk0004Tests.WaitFor(() => fixture.Handle.DemoCampaign?.Rewards?.Loot?.Phase == LootPhases.Open);
            await fixture.Route(DeepSpaceSaga.Client.UI.Screens.ScreenEvent.OpenTacticalBattle);
            var screen = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
            await fixture.PressKey(Silk.NET.Input.Key.F5);
            string json = File.ReadAllText(Path.Combine(directory, SaveSlotNaming.ToFileName(SaveSlots.Quicksave)));
            var saved = ScenarioLoader.LoadFromJson(json, true, true);
            Assert.Equal(LootPhases.Open, saved.GameState.DemoCampaign!.LootSession!.Phase);
            Assert.Same(screen, fixture.Screens.Current);
            Assert.False(fixture.Engine.IsTacticalBattleActive);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualLoadRouterAdoptsPendingCombatOrFrozenLootWithoutCommand(bool finished)
    {
        using var source = DemoEp0036Us0007Tk0004Tests.Won(finished, finished);
        var campaign = source.CaptureSnapshot().DemoCampaign!;
        var saved = finished ? source.CaptureSaveState() : source.CapturePendingEncounterCheckpoint(
            new("checkpoint", campaign.Revision, campaign.WorldEncounter!.Binding.EncounterId));
        using var engine = DeepSpaceSaga.Engine.Content.EngineContentLoader.CreateEngineFromScenarioFile(
            AppContext.BaseDirectory + "/Settings.json", AppContext.BaseDirectory + "/Scenarios/Demo/scenario.json");
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
            Assert.True(screen.CanUseLootSave);
            Assert.Equal(DeepSpaceSaga.Client.UI.Screens.ScreenEvent.QuickSave,
                screen.OnKeyDown(Silk.NET.Input.Key.F5, DeepSpaceSaga.Client.UI.Screens.KeyModifiers.None, false));
            Assert.Equal(DeepSpaceSaga.Client.UI.Screens.ScreenEvent.QuickLoad,
                screen.OnKeyDown(Silk.NET.Input.Key.F9, DeepSpaceSaga.Client.UI.Screens.KeyModifiers.None, false));
            screen.OnKeyDown(Silk.NET.Input.Key.Escape, DeepSpaceSaga.Client.UI.Screens.KeyModifiers.None, false);
            Assert.False(screen.CanUseLootSave);
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
