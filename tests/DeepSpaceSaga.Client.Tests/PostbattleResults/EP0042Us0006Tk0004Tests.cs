using DeepSpaceSaga.Client.Tests.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests.PostbattleResults;

[Collection("InterfaceLog")]
public sealed class EP0042Us0006Tk0004Tests
{
    [Fact]
    public async Task BattleResultsLoad_BlockNavigationAfterFailedAcknowledgement()
    {
        string root = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ??
            "D:/DeepSpaceSaga/artifacts/validation/ep-0042/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff");
        string directory = Path.Combine(root, "close-retry-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            await using var fixture = await BattleNavigationTests.Fixture.Create(saveDirectory: directory, initialSpeed: SimulationSpeed.Speed0);
            await fixture.Route(ScreenEvent.OpenTacticalBattle);
            var setup = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
            await setup.Flow.OptionsTask!;
            await fixture.Route(setup.OnKeyDown(Key.Enter));
            var screen = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
            var session = fixture.Engine.TacticalBattleSessionForTests!;
            session.Battle.EndWithTimeout(8);
            await screen.Flow.DebugDestroyAsync(new("observe-timeout", session.Id, session.Revision, session.Battle.Drones[0].Id));
            screen.ShowConfirmedState();
            await fixture.Route(screen.OnKeyDown(Key.Enter));
            var results = Assert.IsType<BattleResultsScreen>(fixture.Screens.Current);
            Assert.True(results.CanSaveLoad);

            var connection = Assert.IsType<LocalGameSessionConnection>(fixture.Proxy.Inner);
            connection.SaveStageForTests = stage =>
            {
                if (stage == "before_replace") throw new IOException("Injected acknowledgement write failure");
            };
            await fixture.Route(results.OnKeyDown(Key.Enter));
            Assert.Same(results, fixture.Screens.Current);
            Assert.True(results.Flow.ResultsCloseCheckpointPending);
            Assert.Equal(ScreenEvent.None, results.OnKeyDown(Key.F5));
            Assert.Equal(ScreenEvent.None, results.OnKeyDown(Key.F9));
            Assert.NotNull(fixture.Engine.TrySetSpeed(SimulationSpeed.Speed1));

            connection.SaveStageForTests = null;
            await fixture.Route(results.OnKeyDown(Key.Enter));
            Assert.Same(fixture.Game, fixture.Screens.Current);
            Assert.False(results.Flow.ResultsCloseCheckpointPending);
            Assert.Null(fixture.Engine.TrySetSpeed(SimulationSpeed.Speed1));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
