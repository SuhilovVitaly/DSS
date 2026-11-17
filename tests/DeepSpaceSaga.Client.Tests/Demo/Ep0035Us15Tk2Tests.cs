using DeepSpaceSaga.Client.Tests.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests.Demo;

[Collection("InterfaceLog")]
public sealed class Ep0035Us15Tk2Tests
{
    [Fact]
    public async Task CampaignCannotAbortAndDrawOrTimeoutAreDisplayedAsDefeat()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = (TacticalBattleModalScreen)fixture.Screens.Current;
        await setup.Flow.OptionsTask!;
        await fixture.Route(setup.OnKeyDown(Key.Enter));
        var state = ((TacticalBattleScreen)fixture.Screens.Current).Flow.State!;
        var flow = new TacticalBattleFlow(fixture.Handle) { CampaignEncounter = new(new(state.BattleId, "encounter:g", "g"), "pending_battle", false) };
        Assert.False(await flow.ExitAsync());
        Assert.True(fixture.Engine.IsTacticalBattleActive);
        var modal = new TacticalBattleModalScreen(flow, false);
        Assert.Equal(ScreenEvent.None, modal.OnKeyDown(Key.Escape));
        Assert.False(modal.ConfirmExit);
        foreach (var kind in new[] { TacticalBattleOutcomeCode.Draw, TacticalBattleOutcomeCode.Timeout })
        {
            var outcome = state with { Outcome = new(kind, [], state.Turn, state.Losses, new(0, 0, 0, 0, 0, 0, 0, 0, 0, null, 0, []), 0) };
            Assert.Equal("Defeat", PhasePanel.OutcomeTitle(outcome, true));
            Assert.Equal(kind.ToString(), PhasePanel.OutcomeTitle(outcome, false));
            Assert.Equal(state.Units, outcome.Units);
        }
    }
}
