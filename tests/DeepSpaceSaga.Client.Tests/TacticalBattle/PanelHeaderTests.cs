using System.Globalization;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PanelHeaderTests
{
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
        var flight = TacticalBattlePanelHeader.Capture(Frame(loss, TacticalBattlePresentation.SalvoFlight));
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
        var hidden = TacticalBattlePanelHeader.Capture(Frame(final, TacticalBattlePresentation.SalvoFlight));
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
