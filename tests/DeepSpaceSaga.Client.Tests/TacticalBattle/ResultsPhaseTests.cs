using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class ResultsPhaseTests
{
    [Fact]
    public async Task ActualSalvoWaitsForArrivalAndSkipReplayContinueNeverSendAnotherShot()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        for (int i = 0; i < 30 && screen.Flow.State?.Phase == TacticalBattlePhase.Move; i++)
            await fixture.Route(ScreenEvent.MoveTacticalBattle);
        Assert.Equal(TacticalBattlePhase.Fire, screen.Flow.State?.Phase);
        fixture.AdvanceAnimation(TacticalBattleMovementAnimation.FireTransitionDurationMs - 1);
        Assert.True(screen.TurnBusy);
        Assert.Null(screen.CaptureFrame(1280, 720)!.FirePlan);
        fixture.AdvanceAnimation(1);
        Assert.False(screen.TurnBusy);
        Assert.NotNull(screen.CaptureFrame(1280, 720)!.FirePlan);
        fixture.ManualAnimation = true;
        await fixture.Route(ScreenEvent.FireTacticalBattle);
        var state = screen.Flow.State!;
        Assert.NotEmpty(state.Shots);
        Assert.True(screen.HasSalvoPresentation);
        var first = screen.CaptureFrame(1280, 720)!;
        Assert.Equal(TacticalBattlePresentation.SalvoFlight, first.Presentation);
        Assert.Null(first.FirePlan);
        Assert.Null(first.Movement);
        Assert.All(first.Salvo!.Flights, shot => Assert.False(shot.Flying));
        Assert.Empty(first.Salvo.Damage);

        fixture.AdvanceAnimation((long)TacticalBattleResults.InitialDelayMs - 1);
        Assert.All(screen.CaptureFrame(1280, 720)!.Salvo!.Flights, shot => Assert.False(shot.Flying));
        fixture.AdvanceAnimation(1);
        var launch = screen.CaptureFrame(1280, 720)!.Salvo!;
        Assert.True(launch.Flights[0].Flying);
        Assert.Equal(0, launch.Flights[0].Progress);
        fixture.AdvanceAnimation(200);
        Save(screen, "salvo-in-flight.png");
        await fixture.Route(ScreenEvent.FireTacticalBattle);
        await fixture.Route(ScreenEvent.MoveTacticalBattle);
        Assert.Equal(1, fixture.Proxy.Fires);
        Assert.Same(state, screen.Flow.State);

        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Enter));
        var skipped = screen.CaptureFrame(1280, 720)!;
        Assert.Equal(TacticalBattlePresentation.SalvoResults, skipped.Presentation);
        Assert.Equal(state.Shots.Count(s => s.Hit), skipped.Salvo!.Hits);
        Save(screen, "salvo-resolved.png");
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        var replay = screen.ReplaySalvoBounds;
        Assert.False(replay.IsEmpty);
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(replay.MidX, replay.MidY, MouseButton.Left));
        Assert.Equal(TacticalBattlePresentation.SalvoFlight, screen.CaptureFrame(1280, 720)!.Presentation);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Enter));
        Assert.True(screen.HasSalvoPresentation);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Enter));
        Assert.False(screen.HasSalvoPresentation);
        Assert.Equal(TacticalBattlePresentation.Current, screen.CaptureFrame(1280, 720)!.Presentation);
        Assert.Equal(1, fixture.Proxy.Fires);
        Assert.Same(state, screen.Flow.State);
    }

    [Fact]
    public async Task EmptyAutomaticSalvoSkipsResultsAndKeepsMovementDelay()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        fixture.ManualAnimation = true;
        await fixture.Route(ScreenEvent.MoveTacticalBattle);
        Assert.True(screen.Flow.State!.FireSkipped);
        Assert.Empty(screen.Flow.State.Shots);
        Assert.False(screen.HasSalvoPresentation);
        Assert.True(screen.CaptureFrame(1280, 720)!.Pending);
        fixture.AdvanceAnimation(TacticalBattleMovementAnimation.DurationMs);
        var ready = screen.CaptureFrame(1280, 720)!;
        Assert.False(ready.Pending);
        Assert.Null(ready.Salvo);
        Assert.Equal(TacticalBattlePresentation.Current, ready.Presentation);
    }

    [Fact]
    public async Task TimelineCapsLongAutomaticSalvoAndUsesAlternatingMissOffsets()
    {
        Assert.Equal(new SKPoint(128, 13), TacticalBattleResults.Endpoint(new(0, 0), new(100, 0), false, 0, 1));
        Assert.Equal(new SKPoint(128, -13), TacticalBattleResults.Endpoint(new(0, 0), new(100, 0), false, 1, 1));
        Assert.Equal(new SKPoint(100, 0), TacticalBattleResults.Endpoint(new(0, 0), new(100, 0), true, 1, 1));
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        for (int i = 0; i < 30 && screen.Flow.State?.Phase == TacticalBattlePhase.Move; i++)
            await fixture.Route(ScreenEvent.MoveTacticalBattle);
        var before = screen.Flow.State!;
        await fixture.Route(ScreenEvent.FireTacticalBattle);
        var after = screen.Flow.State!;
        Assert.NotEmpty(after.Shots);
        // Repeat a genuine captured shot to drive the cap independently of roster size.
        var captured = after with { FireSkipped = true, Shots = Enumerable.Repeat(after.Shots[0], 12).ToImmutableArray() };
        var response = new TacticalBattleResponse("fixture", true, TacticalBattleRejectionCode.None, captured.Phase, captured, []);
        var results = TacticalBattleResults.Create(before with { Phase = TacticalBattlePhase.Move }, response, 900)!;
        Assert.Equal(TacticalBattleResults.CapMs, results.EndMs, 8);
        Assert.True(results.Capture(900).AutoFire);
        Assert.True(results.Capture(900 + (long)TacticalBattleResults.CapMs - 1).InFlight);
        Assert.False(results.Capture(900 + (long)TacticalBattleResults.CapMs).InFlight);
        Assert.True(results.Capture(900 + (long)TacticalBattleResults.CapMs).Flights[^1].Flash);
        Assert.False(results.Capture(900 + (long)(TacticalBattleResults.CapMs + TacticalBattleResults.FlashMs)).Flights[^1].Flash);
    }

    [Fact]
    public async Task Skin_changes_salvo_visuals_without_changing_arrival_or_replay()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        for (int i = 0; i < 30 && screen.Flow.State?.Phase == TacticalBattlePhase.Move; i++)
            await fixture.Route(ScreenEvent.MoveTacticalBattle);
        fixture.AdvanceAnimation(TacticalBattleMovementAnimation.FireTransitionDurationMs);
        fixture.ManualAnimation = true;
        await fixture.Route(ScreenEvent.FireTacticalBattle);
        fixture.AdvanceAnimation((long)TacticalBattleResults.InitialDelayMs + 200);
        var frame = screen.CaptureFrame(1280, 720)!;
        Assert.Equal(TacticalBattlePresentation.SalvoFlight, frame.Presentation);
        var flight = Assert.Single(frame.Salvo!.Flights.Where(flight => flight.Flying));
        var shot = flight.Shot;
        var from = frame.View.Project(shot.From);
        var to = TacticalBattleResults.Endpoint(from, frame.View.Project(shot.To), shot.Hit, flight.Index, 1);
        var head = new SKPoint(from.X + (to.X - from.X) * (float)flight.Progress,
            from.Y + (to.Y - from.Y) * (float)flight.Progress);
        var skin = TacticalBattleSkin.Parse("""
            {"schemaVersion":1,"id":"salvo","displayName":"Salvo","roles":{
              "shot.projectile":{"fill":"#FF00FF","size":12,"shape":"diamond"},
              "shot.flash":{"fill":"#00FFEE","metrics":{"flash-scale":2}}
            }}
            """);
        using var before = BattleMapRenderTests.Raster(frame);
        using var after = BattleMapRenderTests.Raster(frame, skin);
        Assert.NotEqual(before.GetPixel((int)head.X, (int)head.Y), after.GetPixel((int)head.X, (int)head.Y));
        Assert.Equal(new SKColor(255, 0, 255), after.GetPixel((int)head.X, (int)head.Y));

        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Enter));
        Assert.Equal(TacticalBattlePresentation.SalvoResults, screen.CaptureFrame(1280, 720)!.Presentation);
        Assert.Equal(1, fixture.Proxy.Fires);
        Assert.Same(frame.Battle, screen.Flow.State);
    }

    private static async Task<TacticalBattleScreen> Start(BattleNavigationTests.Fixture fixture)
    {
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        return Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
    }

    private static void Save(TacticalBattleScreen screen, string name)
    {
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is not { Length: > 0 } root) return;
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var file = File.Create(Path.Combine(root, name));
        png.SaveTo(file);
    }
}
