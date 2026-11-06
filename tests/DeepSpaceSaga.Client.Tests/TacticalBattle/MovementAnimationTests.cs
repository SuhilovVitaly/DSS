using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class MovementAnimationTests
{
    [Fact]
    public async Task PendingAndMovementPlaybackBlockMouseEnterAndQueuedCommands()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var before = screen.Flow.State!;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Proxy.MoveGate = gate;
        var started = fixture.Route(screen.OnKeyDown(Key.Enter));
        Task? queuedMove = null, queuedFire = null;
        try
        {
            await fixture.Proxy.MoveSeen.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(screen.Flow.Pending);
            var pending = screen.CaptureFrame(1280, 720)!;
            Assert.True(pending.Pending);
            Assert.Same(before, pending.Battle);
            Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Enter));
            Assert.Equal(ScreenEvent.None, screen.OnMouseDown(400, 300, MouseButton.Left));
            Assert.Empty(screen.BuildMoveRequest().Orders);
            queuedMove = fixture.Route(ScreenEvent.MoveTacticalBattle);
            queuedFire = fixture.Route(ScreenEvent.FireTacticalBattle);
        }
        finally { gate.TrySetResult(); }
        await started;
        await queuedMove!;
        await queuedFire!;
        Assert.Equal(1, fixture.Proxy.Moves);
        Assert.Equal(0, fixture.Proxy.Fires);

        var accepted = screen.Flow.State!;
        var path = accepted.Paths.First(p => p.Turn == before.Turn && p.Points.Length > 1);
        var atStart = screen.CaptureFrame(1280, 720)!;
        Assert.True(atStart.Pending);
        Assert.Equal(TacticalBattlePhase.Move, atStart.Battle.Phase);
        Assert.Equal(BattleFormat.Localize("AnimatingMovement"), atStart.Status);
        Assert.Null(atStart.Movement);
        Assert.Equal(path.Points[0], atStart.Battle.Units.Single(u => u.Id == path.DroneId).Pose);

        fixture.AdvanceAnimation(TacticalBattleMovementAnimation.DurationMs / 2);
        var mid = screen.CaptureFrame(1280, 720)!;
        var midpoint = mid.Battle.Units.Single(u => u.Id == path.DroneId).Pose;
        Assert.NotEqual(path.Points[0], midpoint);
        Assert.NotEqual(path.Points[^1], midpoint);
        if (path.Points.Length % 2 != 0) Assert.Equal(path.Points[(path.Points.Length - 1) / 2], midpoint);
        Assert.Same(accepted, screen.Flow.State);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Right));
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Enter));

        fixture.AdvanceAnimation(TacticalBattleMovementAnimation.DurationMs / 2 - 1);
        Assert.True(screen.CaptureFrame(1280, 720)!.Pending);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.KeypadEnter));
        fixture.AdvanceAnimation(1);
        var arrived = screen.CaptureFrame(1280, 720)!;
        Assert.False(arrived.Pending);
        Assert.Same(accepted, arrived.Battle);
        Assert.Equal(accepted.Phase == TacticalBattlePhase.Fire ? ScreenEvent.FireTacticalBattle : ScreenEvent.MoveTacticalBattle,
            screen.OnKeyDown(Key.Enter));
        Assert.Equal(1, fixture.Proxy.Moves);
        Assert.Equal(0, fixture.Proxy.Fires);
    }

    [Fact]
    public async Task AcceptedTerminalMovementShowsOutcomeOnlyAfterPlayback()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        // Keep the real movement and paths; inject a terminal response to exercise this rare UI branch.
        fixture.Proxy.TransformMoveResponse = response =>
        {
            var state = response.State!;
            var outcome = new TacticalOutcomeSnapshot(TacticalBattleOutcomeCode.Draw, [], state.Turn, state.Losses,
                new(0, 0, 0, 0, 0, 0, 0, 0, 0, null, 0, []), 0);
            return response with { State = state with { Phase = TacticalBattlePhase.Over, Outcome = outcome }, CurrentPhase = TacticalBattlePhase.Over };
        };
        await fixture.Route(ScreenEvent.MoveTacticalBattle);
        Assert.Equal(TacticalBattlePhase.Over, screen.Flow.State!.Phase);
        var moving = TacticalBattlePanelHeader.Capture(screen.CaptureFrame(1280, 720)!);
        Assert.Empty(moving.Outcome);
        Assert.False(moving.ShowNew);
        fixture.AdvanceAnimation(TacticalBattleMovementAnimation.DurationMs);
        var final = TacticalBattlePanelHeader.Capture(screen.CaptureFrame(1280, 720)!);
        Assert.Equal(BattleFormat.Localize("Outcome.Draw"), final.Outcome);
        Assert.True(final.ShowNew);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Enter));
        Assert.Equal(0, fixture.Proxy.Fires);
    }

    private static async Task<TacticalBattleScreen> Start(BattleNavigationTests.Fixture fixture)
    {
        fixture.ManualAnimation = true;
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        return Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
    }
}
