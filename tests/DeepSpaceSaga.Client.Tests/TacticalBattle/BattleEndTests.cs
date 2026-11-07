using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class BattleEndTests
{
    [Fact]
    public async Task NewBattleDuringMoveExitsFirstAndPrefillsAcceptedSetupWithoutResumingWorld()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        setup.Render(canvas, 1280, 720);
        var seed = setup.CurrentLayout!.Seed;
        setup.OnMouseDown(seed.MidX, seed.MidY, MouseButton.Left);
        setup.OnKeyDown(Key.Backspace);
        setup.OnTextInput('4');
        setup.OnTextInput('7');
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        var accepted = battle.Flow.LastStart!;
        Assert.Equal(47, accepted.Seed);
        var first = battle.Flow.State!;
        Assert.Equal(TacticalBattlePhase.Move, first.Phase);
        battle.Render(canvas, 1280, 720);
        var rect = battle.NewBattleBounds;
        Assert.True(rect.Top < 250);
        Assert.Equal(ScreenEvent.NewTacticalBattle, battle.OnMouseDown(rect.MidX, rect.MidY, MouseButton.Left));
        await fixture.Route(ScreenEvent.NewTacticalBattle);
        var next = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
        Assert.Same(battle.Flow, next.Flow);
        Assert.False(fixture.Engine.IsTacticalBattleActive);
        Assert.Null(fixture.Engine.GetTacticalBattleState());
        Assert.Equal(1, fixture.Depth);
        Assert.Equal(new[] { SimulationSpeed.Speed0 }, fixture.Proxy.Speeds.ToArray());
        var prefill = next.BuildStartRequest();
        Assert.Equal(accepted.Seed, prefill.Seed);
        Assert.Equal(accepted.ScenarioId, prefill.ScenarioId);
        Assert.Equal(accepted.OwnTacticId, prefill.OwnTacticId);
        Assert.Equal(accepted.EnemyTacticId, prefill.EnemyTacticId);
        Assert.Equal(accepted.Loadout.AsEnumerable(), prefill.Loadout);

        await fixture.Route(ScreenEvent.StartTacticalBattle);
        var restarted = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        Assert.NotEqual(first.BattleId, restarted.Flow.State!.BattleId);
        Assert.Equal(47, restarted.Flow.State.Seed);
        Assert.Equal(ScreenEvent.None, restarted.OnKeyDown(Key.Escape));
        Assert.True(restarted.ConfirmExit);
        Assert.True(fixture.Engine.IsTacticalBattleActive);
        await fixture.Route(restarted.OnKeyDown(Key.Enter));
        Assert.Same(fixture.Game, fixture.Screens.Current);
        Assert.Equal(new[] { SimulationSpeed.Speed0, SimulationSpeed.Speed2 }, fixture.Proxy.Speeds.ToArray());
    }

    [Fact]
    public async Task TerminalEscapeReturnsDirectlyAndDrawCanDescribeBothTypedReasons()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        for (int n = 0; n < 100 && battle.Flow.State!.Phase != TacticalBattlePhase.Over; n++)
        {
            var phase = battle.Flow.State.Phase;
            await fixture.Route(phase == TacticalBattlePhase.Move ? ScreenEvent.MoveTacticalBattle : ScreenEvent.FireTacticalBattle);
        }
        fixture.FinishPresentation();
        var final = battle.CaptureFrame(1280, 720)!;
        Assert.Equal(TacticalBattlePhase.Over, final.Battle.Phase);
        var header = TacticalBattlePanelHeader.Capture(final);
        Assert.NotEmpty(header.Outcome);
        var draw = final.Battle.Outcome! with
        {
            Kind = TacticalBattleOutcomeCode.Draw,
            Reasons = [new(TacticalOutcomeReasonCode.NoDrones, "carrier-one"), new(TacticalOutcomeReasonCode.CarrierDestroyed, "carrier-two")]
        };
        var drawHeader = TacticalBattlePanelHeader.Capture(final with { Battle = final.Battle with { Outcome = draw } });
        Assert.Equal(BattleFormat.Localize("Outcome.Draw"), drawHeader.Outcome);
        Assert.Contains("carrier-one", drawHeader.Reason);
        Assert.Contains("carrier-two", drawHeader.Reason);
        Assert.Equal(ScreenEvent.CloseTacticalBattle, battle.OnKeyDown(Key.Escape));
        Assert.False(battle.ConfirmExit);
        await fixture.Route(ScreenEvent.CloseTacticalBattle);
        Assert.Same(fixture.Game, fixture.Screens.Current);
        Assert.False(fixture.Engine.IsTacticalBattleActive);
    }
}
