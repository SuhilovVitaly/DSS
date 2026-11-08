using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed partial class ResultsPhaseTests
{
    [Fact]
    public async Task Enemy_evasion_is_revealed_only_after_salvo()
    {
        await using var scene = await View.Ep0019Scenes.Create("2-fire");
        using var locale = Localization.UseLocaleForTests("Russian");
        var enemy = scene.Fixture.Engine.TacticalBattleSessionForTests!.Battle.Drones.First(d => d.Side == "B");
        enemy.Hull = enemy.MaxHull / 2 - 1;
        var before = scene.Screen.CaptureFrame(1920, 1080)!;
        Assert.False(TacticalBattleMapRenderer.IsEvading(before, enemy.Id));
        Assert.Empty(before.Battle.FireSteps);
        scene.Fixture.ManualAnimation = true;
        await scene.Fixture.Route(ScreenEvent.FireTacticalBattle);
        var flight = scene.Screen.CaptureFrame(1920, 1080)!;
        Assert.True(flight.Salvo!.InFlight);
        Assert.True(TacticalBattleMapRenderer.IsEvading(flight, enemy.Id));
        scene.Screen.HoverUnitId = enemy.Id;
        using (var bitmap = scene.Render()) { }
        Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == "Уклонение");
        scene.Screen.OnKeyDown(Key.Enter);
        Assert.True(TacticalBattleMapRenderer.IsEvading(scene.Screen.CaptureFrame(1920, 1080)!, enemy.Id));
        scene.Screen.OnKeyDown(Key.Enter);
        Assert.False(TacticalBattleMapRenderer.IsEvading(scene.Screen.CaptureFrame(1920, 1080)!, enemy.Id));
        Assert.Equal(1, scene.Fixture.Proxy.Fires);
    }

    [Fact]
    public async Task Own_evaded_drone_card_shows_evasion()
    {
        await using var scene = await View.Ep0019Scenes.Create("2-fire");
        using var locale = Localization.UseLocaleForTests("Russian");
        string id = scene.Screen.CaptureFrame(1920, 1080)!.FirePlan!.SelectedDroneId!;
        scene.Screen.OnKeyDown(Key.E);
        scene.Fixture.ManualAnimation = true;
        await scene.Fixture.Route(ScreenEvent.FireTacticalBattle);
        scene.Screen.OnKeyDown(Key.Enter);
        var frame = scene.Screen.CaptureFrame(1920, 1080)!;
        var card = PilotCardModel.Project(frame).Single(c => c.UnitId == id);
        Assert.Equal("уклонение", card.Chip);
        Assert.True(card.Evading);
        Assert.Contains(frame.Battle.Log, e => e.ActorId == id && e.SkipReason == TacticalFireSkipReason.Evading);
    }

    [Fact]
    public async Task Zero_shot_evasion_has_results_without_fake_projectiles_or_miss()
    {
        await using var scene = await View.Ep0019Scenes.Create("2-fire");
        using var locale = Localization.UseLocaleForTests("Russian");
        var battle = scene.Fixture.Engine.TacticalBattleSessionForTests!.Battle;
        foreach (var drone in battle.Drones.Where(d => d.Side == "B")) drone.Jammed = true;
        for (int i = 0; i < 4; i++) { scene.Screen.OnKeyDown(Key.E); scene.Screen.OnKeyDown(Key.Tab); }
        scene.Fixture.ManualAnimation = true;
        await scene.Fixture.Route(ScreenEvent.FireTacticalBattle);
        var frame = scene.Screen.CaptureFrame(1920, 1080)!;
        Assert.False(frame.Salvo!.InFlight);
        Assert.Empty(frame.Salvo.Flights);
        Assert.Equal(4, frame.Salvo.Evading.Count);
        Assert.Equal(3, frame.Salvo.Turn);
        using (var bitmap = scene.Render()) { }
        Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == "огонь не ведётся");
        Assert.DoesNotContain(scene.Screen.Layout.Texts, t => t.Text == "ПРОМАХ");
        Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == "ХОД 3  •");
        scene.Screen.OnKeyDown(Key.Enter);
        Assert.Null(scene.Screen.CaptureFrame(1920, 1080)!.Salvo);
        Assert.Equal(1, scene.Fixture.Proxy.Fires);
    }

    [Fact]
    public async Task SequentialPlaybackAppliesDamageAtEachImpactBeforeNextLaunch()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var results = ControlledResults(screen.Flow.State!);
        Assert.Equal(100, results.Project(1274).Units[0].Hull);
        Assert.Equal(95, results.Project(1275).Units[0].Hull);
        Assert.Equal(95, results.Project(1276).Units[0].Hull);
        Assert.Equal(80, results.Project(2250).Units[0].Hull);
        Assert.Equal(10, results.Project(1274).Units[0].Armor);
        Assert.Equal(0, results.Project(1275).Units[0].Armor);
        Assert.Empty(results.Project(1274).Log);
        Assert.Single(results.Project(1275).Log);
        Assert.Equal(2, results.Project(2250).Log.Length);
        Assert.Single(results.Capture(1274).Flights.Where(f => f.Flying));
        var impact = results.Capture(1275);
        Assert.True(impact.Flights[0].Arrived);
        Assert.True(impact.Flights[1].Flying);
        Assert.Equal(0, impact.Flights[1].Progress);
        Assert.Equal(5, Assert.Single(impact.Damage).Hull);
        Assert.Equal(1, impact.Hits);
        Assert.Equal(0, fixture.Proxy.Fires);
    }

    [Fact]
    public async Task SkipReplayAndLargeClockJumpPreserveExactStepStatesAndNoExtraRequest()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var authoritative = screen.Flow.State!;
        var results = ControlledResults(authoritative);
        var final = results.Project(10000);
        Assert.Same(results.Battle, final);
        Assert.Same(final, (results with { Skipped = true }).Project(0));
        var replay = results with { StartedMs = 10000, Skipped = false };
        Assert.Equal(100, replay.Project(10000).Units[0].Hull);
        Assert.Empty(replay.Project(10000).Log);
        Assert.Equal(95, replay.Project(11275).Units[0].Hull);
        Assert.Same(final, replay.Project(20000));
        Assert.Equal(0, fixture.Proxy.Fires);
        Assert.Same(authoritative, screen.Flow.State);
    }

    [Fact]
    public async Task ZeroShotAutoFireAndImmediateVictoryHaveCoherentPresentation()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var results = ControlledResults(screen.Flow.State!);
        var zero = results with { Shots = [] };
        Assert.Equal(0, zero.EndMs);
        Assert.False(zero.Capture(0).InFlight);
        Assert.Empty(zero.Capture(0).Flights);
        var outcome = new TacticalOutcomeSnapshot(TacticalBattleOutcomeCode.A, [new(TacticalOutcomeReasonCode.NoDrones, "carrier-b")],
            1, [], new(1, 1, 0, 0, 0, 0, 40, 0, 0, "B", 0, []), 2);
        var final = results.Battle with { Phase = TacticalBattlePhase.Over, Outcome = outcome };
        var step = final.FireSteps[0] with { Outcome = outcome };
        var won = results with { Battle = final with { FireSteps = [step] }, Shots = [results.Shots[0]] };
        Assert.Null(won.Project(1274).Outcome);
        Assert.Same(outcome, won.Project(1275).Outcome);
    }

    internal static TacticalBattleResults ControlledResults(TacticalBattleSnapshot source)
    {
        var target = source.Units.First(u => u.Kind == TacticalUnitKind.Drone && u.Side != source.PlayerSide)
            with
        { Hull = 100, MaxHull = 100, Armor = 10, MaxArmor = 10, State = TacticalUnitState.Air };
        var first = target with { Hull = 95, Armor = 0 };
        var last = target with { Hull = 80, Armor = 0 };
        var shot = new TacticalShotSnapshot(1, "first", target.Id, new(0, 0, 0), target.Pose, 1, true, 15, false, false, false);
        var event1 = new TacticalBattleLogEvent(1, 1, TacticalBattlePhase.Fire, TacticalBattleLogKind.DamageApplied,
            TargetId: target.Id, Damage: new(5, 10, 1, 95))
        { FireStepIndex = 0 };
        var event2 = event1 with { Sequence = 2, Damage = new(15, 0, 1, 80), FireStepIndex = 1 };
        var battle = source with
        {
            Units = [last],
            BeforeSalvoUnits = [target],
            Log = [event1, event2],
            Shots = [shot, shot with { ShooterId = "second" }],
            FireQueue = [new(0, "first", 80), new(1, "second", 60)],
            FireSteps = [new(0, "first", target.Id, TacticalFireSkipReason.None, shot, [first], [event1], null),
                new(1, "second", target.Id, TacticalFireSkipReason.None, shot with { ShooterId = "second" }, [last], [event2], null)]
        };
        return new(battle, battle.Shots, [], 0);
    }

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
        var captured = after with
        {
            FireSkipped = true,
            Shots = Enumerable.Repeat(after.Shots[0], 12).ToImmutableArray(),
            FireSteps = Enumerable.Range(0, 12).Select(i => new TacticalFireStepSnapshot(i, after.Shots[0].ShooterId,
                after.Shots[0].TargetId, TacticalFireSkipReason.None, after.Shots[0], after.Units, [], null)).ToImmutableArray()
        };
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
        var to = frame.View.Project(shot.To);
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
        Assert.NotSame(frame.Battle, screen.Flow.State);
        Assert.Equal(frame.Battle.BattleId, screen.Flow.State!.BattleId);
        Assert.Equal(frame.Battle.Revision, screen.Flow.State.Revision);
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
