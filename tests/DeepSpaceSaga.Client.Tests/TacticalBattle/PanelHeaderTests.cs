using System.Globalization;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PanelHeaderTests
{
    [Fact]
    public async Task VisibleLossAndOutcomeAppearAtWinningImpactNotAtResponseReceipt()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var results = ResultsPhaseTests.ControlledResults(screen.Flow.State!);
        var destroyed = results.Battle.Units[0] with { Hull = 0, State = TacticalUnitState.Destroyed };
        var outcome = new TacticalOutcomeSnapshot(TacticalBattleOutcomeCode.A, [new(TacticalOutcomeReasonCode.NoDrones, "B")],
            1, [new("A", 0), new("B", 1)], new(1, 1, 0, 0, 0, 0, 100, 10, 0, "B", 0, []), 2);
        var shot = results.Shots[0] with { Killed = true, LethalImpact = true };
        var final = results.Battle with
        {
            Phase = TacticalBattlePhase.Over,
            Outcome = outcome,
            Units = [destroyed],
            Shots = [shot],
            FireSteps = [new(0, shot.ShooterId, shot.TargetId, TacticalFireSkipReason.None, shot, [destroyed], [], outcome)]
        };
        results = results with { Battle = final, Shots = [shot] };
        var before = TacticalBattlePanelHeader.Capture(Frame(results.Project(1274), TacticalBattlePresentation.SalvoFlight));
        var impact = TacticalBattlePanelHeader.Capture(Frame(results.Project(1275), TacticalBattlePresentation.SalvoResults));
        Assert.Equal(LossLine(0, 0), before.Losses);
        Assert.Empty(before.Outcome);
        Assert.Equal(LossLine(0, 1), impact.Losses);
        Assert.Equal(BattleFormat.Localize("Outcome.Victory"), impact.Outcome);
    }

    [Fact]
    public async Task RealSalvoLossesStayHiddenUntilArrivalAndOutcomeUsesPlayerPerspective()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        TacticalBattleSnapshot? loss = null;
        for (int i = 0; i < 100 && screen.Flow.State!.Phase != TacticalBattlePhase.Over; i++)
        {
            var before = screen.Flow.State;
            var evt = before.Phase == TacticalBattlePhase.Move ? ScreenEvent.MoveTacticalBattle : ScreenEvent.FireTacticalBattle;
            await fixture.Route(evt);
            var after = screen.Flow.State!;
            if (evt == ScreenEvent.FireTacticalBattle && before.Losses.All(l => l.Count == 0) && after.Losses.Any(l => l.Count > 0))
                loss = after;
        }

        Assert.NotNull(loss);
        Assert.NotEmpty(loss.BeforeSalvoUnits);
        Assert.NotEmpty(loss.Shots);
        var initial = new TacticalBattleResults(loss, loss.Shots, [], 0).Project(0);
        var flight = TacticalBattlePanelHeader.Capture(Frame(initial, TacticalBattlePresentation.SalvoFlight));
        var arrived = TacticalBattlePanelHeader.Capture(Frame(loss, TacticalBattlePresentation.SalvoResults));
        Assert.Equal(LossLine(0, 0), flight.Losses);
        Assert.NotEqual(flight.Losses, arrived.Losses);
        Assert.Equal(flight.Title, arrived.Title);
        Assert.EndsWith(BattleFormat.Number(loss.Shots[0].Turn), flight.Title);
        Assert.Equal(BattleFormat.Localize("Phase.Results"), flight.Phase);
        Assert.Empty(flight.Outcome);
        Assert.False(flight.ShowNew);

        var final = screen.Flow.State!;
        Assert.Equal(TacticalBattlePhase.Over, final.Phase);
        Assert.NotNull(final.Outcome);
        Assert.True(final.Outcome.Kind is TacticalBattleOutcomeCode.A or TacticalBattleOutcomeCode.B);
        string winning = final.Outcome.Kind.ToString();
        var victor = TacticalBattlePanelHeader.Capture(Frame(final with { PlayerSide = winning }));
        var defeated = TacticalBattlePanelHeader.Capture(Frame(final with { PlayerSide = winning == "A" ? "B" : "A" }));
        Assert.Equal(BattleFormat.Localize("Outcome.Victory"), victor.Outcome);
        Assert.Equal(BattleFormat.Localize("Outcome.Defeat"), defeated.Outcome);
        Assert.NotEmpty(victor.Reason);
        Assert.True(victor.ShowNew);
        var beforeImpact = new TacticalBattleResults(final, final.Shots, [], 0).Project(0);
        var hidden = TacticalBattlePanelHeader.Capture(Frame(beforeImpact, TacticalBattlePresentation.SalvoFlight));
        Assert.Empty(hidden.Outcome);
        Assert.False(hidden.ShowNew);
        int winnerLosses = final.Losses.Single(l => l.Side == winning).Count;
        int loserLosses = final.Losses.Single(l => l.Side != winning).Count;
        Assert.Equal(LossLine(winnerLosses, loserLosses), victor.Losses);
        Assert.Equal(LossLine(loserLosses, winnerLosses), defeated.Losses);
    }

    [Fact]
    public async Task ActualEngineRefusalIsShownInPanelWithoutChangingPhaseOrSeed()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create(uiScale: 1.5f);
        var screen = await Start(fixture);
        var before = screen.Flow.State!;
        var refusal = await screen.Flow.MoveAsync(new("header-stale", before.BattleId, 0));
        Assert.False(refusal!.Accepted);
        Assert.Equal(TacticalBattleRejectionCode.StaleRevision, refusal.Code);
        var after = screen.Flow.State!;
        var header = TacticalBattlePanelHeader.Capture(Frame(after) with { Status = screen.Flow.Error });
        Assert.Equal(GameSessionHandle.TacticalBattleRefusalText(refusal.Code), header.Status);
        Assert.Equal(TacticalBattlePhase.Move, after.Phase);
        Assert.Equal(BattleFormat.Localize("Seed") + " 1", header.Seed);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { Length: > 0 } root)
        {
            using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(Path.Combine(root, "battle-panel-refusal.png"));
            png.SaveTo(file);
        }
    }

    [Fact]
    public async Task Skin_styles_outcome_and_confirmation_without_styling_setup()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        using var setupBefore = Draw(setup);
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        var classic = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        var skin = TacticalBattleSkin.Parse("""
            {"schemaVersion":1,"id":"chrome","displayName":"Chrome","roles":{
              "modal.exit":{"fill":"#432E62"},
              "panel.outcome":{"fill":"#71802C"},
              "control.toggle.all-fire-sectors":{"fill":"#3B3045"},
              "control.toggle.enemy-forecast":{"fill":"#6D3B45"},
              "control.toggle.enemy-rear-sectors":{"fill":"#3B5645"}
            }}
            """);
        var styled = new TacticalBattleScreen(classic.Flow, classic.UiScale, skin);
        using var classicFrame = Draw(classic);
        using var styledFrame = Draw(styled);
        Assert.Equal(classic.OverlayControls, styled.OverlayControls);
        Assert.All(new[] { styled.OverlayControls.AllFire, styled.OverlayControls.Forecast, styled.OverlayControls.Sectors }, r => Assert.True(r.IsEmpty));
        Assert.Equal(ScreenEvent.None, styled.OnKeyDown(Key.Escape));
        using var confirm = Draw(styled);
        Assert.True(styled.ConfirmExit);
        Assert.Equal(new SKColor(67, 46, 98), confirm.GetPixel(400, 281));
        Assert.Equal(ScreenEvent.None, styled.OnKeyDown(Key.Escape));
        Assert.False(styled.ConfirmExit);
        using var setupAfter = Draw(setup);
        Assert.Equal(setupBefore.Pixels, setupAfter.Pixels);

        var outcome = new TacticalBattlePanelHeader("Test", "Over", SKColors.Yellow, "", "", "Victory", "", SKColors.Green, "", true);
        using var header = new SKBitmap(400, 200);
        using (var canvas = new SKCanvas(header))
        using (TacticalBattleSkinRenderScope.Enter(skin))
        {
            canvas.Clear(SKColors.Black);
            outcome.Draw(canvas, SKRect.Create(400, 200), 1);
        }
        Assert.Contains(new SKColor(113, 128, 44), header.Pixels);

        static SKBitmap Draw(IScreen screen)
        {
            var bitmap = new SKBitmap(1280, 720);
            using var canvas = new SKCanvas(bitmap);
            screen.Render(canvas, 1280, 720);
            return bitmap;
        }
    }

    private static TacticalBattleFrame Frame(TacticalBattleSnapshot state, TacticalBattlePresentation presentation = TacticalBattlePresentation.Current) =>
        new(state, new(880, 720, 0, 0, 1), new(880, 0, 1280, 720), 1, false, "", presentation);

    private static string LossLine(int own, int enemy) =>
        string.Format(CultureInfo.InvariantCulture, BattleFormat.Localize("LossLine"), own, enemy);

    private static async Task<TacticalBattleScreen> Start(BattleNavigationTests.Fixture fixture)
    {
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        return Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
    }
}
