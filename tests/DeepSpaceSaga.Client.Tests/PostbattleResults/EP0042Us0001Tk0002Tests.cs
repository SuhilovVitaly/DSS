using DeepSpaceSaga.Client.Tests.Demo;
using DeepSpaceSaga.Client.Tests.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests.PostbattleResults;

[Collection("InterfaceLog")]
public sealed class EP0042Us0001Tk0002Tests
{
    [Fact]
    public async Task BattleResults_NoMapBypass_DeathRoutesAfterExplicitExit()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create(initialSpeed: SimulationSpeed.Speed0);
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        await fixture.Route(setup.OnKeyDown(Key.Enter));
        var screen = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        var session = fixture.Engine.TacticalBattleSessionForTests!;
        session.Battle.Carriers.Single(carrier => carrier.Side == session.PlayerSide).Hull = 0;
        session.Battle.CheckVictory();
        var response = await screen.Flow.DebugDestroyAsync(new("flush-result", session.Id, session.Revision, session.Battle.Drones[0].Id));
        Assert.Equal(TacticalBattlePhase.Over, response?.State?.Phase);
        screen.ShowConfirmedState();
        await DemoEp0036Us0007Tk0004Tests.WaitFor(() => fixture.Handle.IsPlayerShipDestroyed);

        await fixture.Route(ScreenEvent.CloseTacticalBattle);
        await fixture.Route(ScreenEvent.NewTacticalBattle);
        await fixture.Route(ScreenEvent.QuickLoad);
        Assert.Same(screen, fixture.Screens.Current);

        await fixture.Route(screen.OnKeyDown(Key.Enter));
        var results = Assert.IsType<BattleResultsScreen>(fixture.Screens.Current);
        await fixture.Route(ScreenEvent.AcknowledgeBattleResults);
        await fixture.Route(results.OnKeyDown(Key.Escape));
        await fixture.Route(ScreenEvent.OpenGameMenu);
        Assert.Same(results, fixture.Screens.Current);

        await fixture.Route(results.OnKeyDown(Key.Enter));
        Assert.Equal("ShipDestroyedScreen", fixture.Screens.Current.GetType().Name);
        Assert.Null(fixture.Engine.GetTacticalBattleState());
    }
}
