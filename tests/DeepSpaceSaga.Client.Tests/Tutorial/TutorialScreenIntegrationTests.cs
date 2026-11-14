using System.Text.Json;
using DeepSpaceSaga.Client.Tests.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens.Tutorial;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.Tutorial;

/// <summary>EP-0028-US-0001-TK-0004: the optional tutorial session observes rendered frames and never changes battle input.</summary>
[Collection("InterfaceLog")]
public sealed class TutorialScreenIntegrationTests
{
    private static TutorialScript SelectPilotScript() => new(1, null, new TutorialLimits(),
        [new TutorialStep("move.select-pilot", "Tutorial.Line.MoveSelectPilot",
            new TutorialCondition("Move", 1, null, null, TutorialSignal.None), TutorialCompletion.PilotSelected, "pilot:{own}", 90)]);

    private static async Task<TacticalBattleScreen> StartF8(BattleNavigationTests.Fixture fixture)
    {
        await fixture.PressKey(Key.F8);
        var setup = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        var battle = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        Assert.Equal(TacticalBattlePhase.Move, battle.Flow.State!.Phase);
        return battle;
    }

    private static void Render(TacticalBattleScreen screen)
    {
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
    }

    [Fact]
    public async Task RenderedFramesFeedTheDirectorAndPilotSelectionCompletesTheStep()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var started = await StartF8(fixture);
        using var skins = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var session = new TutorialSession(SelectPilotScript());
        var screen = new TacticalBattleScreen(started.Flow, 1, skins.GetOrClassic("tabletop"))
        { ClockMilliseconds = started.ClockMilliseconds, Tutorial = session };
        Assert.Equal("tabletop", screen.SkinId);

        Render(screen);
        Assert.Null(screen.Movement.Selected); // Tabletop Move starts without a selected pilot
        Assert.Equal(TutorialStepState.Shown, session.Director.StateOf("move.select-pilot"));
        Assert.Equal("move.select-pilot", session.Director.Current?.Step.Id);

        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Tab));
        Assert.Equal(TutorialStepState.Shown, session.Director.StateOf("move.select-pilot")); // only rendered frames feed it
        Render(screen);

        Assert.Equal(TutorialStepState.Completed, session.Director.StateOf("move.select-pilot"));
        Assert.Null(session.Director.Current);
        Assert.Equal(
            [TutorialStepState.Shown, TutorialStepState.Completed],
            session.Director.DrainEvents().Select(e => e.State));
    }

    [Fact]
    public async Task SessionDoesNotChangeBattleInputOutcome()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var started = await StartF8(fixture);
        using var skins = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var skin = skins.GetOrClassic("tabletop");
        var plain = new TacticalBattleScreen(started.Flow, 1, skin) { ClockMilliseconds = started.ClockMilliseconds };
        var tutored = new TacticalBattleScreen(started.Flow, 1, skin)
        { ClockMilliseconds = started.ClockMilliseconds, Tutorial = new TutorialSession(SelectPilotScript()) };
        Render(plain);
        Render(tutored);
        // The comms panel (US-0002) only adds its own capture area and tutorial:* layout keys.
        Assert.Equal(plain.PanelRects.Concat(tutored.Tutorial!.Bounds), tutored.PanelRects);
        Assert.Equal(plain.Layout.Rects.OrderBy(r => r.Key, StringComparer.Ordinal),
            tutored.Layout.Rects.Where(r => !r.Key.StartsWith("tutorial:", StringComparison.Ordinal)).OrderBy(r => r.Key, StringComparer.Ordinal));

        Key[] keys = [Key.Tab, Key.Right, Key.Up, Key.Tab, Key.Left, Key.Down, Key.Down];
        foreach (var key in keys)
        {
            Assert.Equal(plain.OnKeyDown(key), tutored.OnKeyDown(key));
            Render(plain);
            Render(tutored);
            Assert.Equal(Json(plain.Movement), Json(tutored.Movement));
        }

        var card = plain.Layout.Rects.First(r => r.Key.StartsWith("pilot:", StringComparison.Ordinal)).Value;
        var endpoint = plain.Layout.Rects.FirstOrDefault(r => r.Key.StartsWith("endpoint:", StringComparison.Ordinal)).Value;
        foreach (var point in new[] { new SKPoint(card.MidX, card.MidY), new SKPoint(640, 400), new SKPoint(endpoint.MidX, endpoint.MidY) })
        {
            Assert.DoesNotContain(tutored.Tutorial!.Bounds, b => b.Contains(point)); // clicks outside the panel behave as without a session
            Assert.Equal(plain.OnMouseDown(point.X, point.Y, MouseButton.Left), tutored.OnMouseDown(point.X, point.Y, MouseButton.Left));
            plain.OnMouseUp(point.X, point.Y, MouseButton.Left);
            tutored.OnMouseUp(point.X, point.Y, MouseButton.Left);
            Assert.Equal(plain.OnMouseWheel(point.X, point.Y, 1), tutored.OnMouseWheel(point.X, point.Y, 1));
            Render(plain);
            Render(tutored);
            Assert.Equal(Json(plain.Movement), Json(tutored.Movement));
            Assert.Equal(plain.View, tutored.View);
        }

        Assert.Equal(Json(plain.BuildMoveRequest().Orders), Json(tutored.BuildMoveRequest().Orders));
        var submit = plain.OnKeyDown(Key.Enter);
        Assert.Equal(ScreenEvent.MoveTacticalBattle, submit);
        Assert.Equal(submit, tutored.OnKeyDown(Key.Enter));
    }

    [Fact]
    public void EmptySessionCapturesNoInput()
    {
        var session = new TutorialSession(TutorialScript.Empty);
        Assert.Same(TutorialScript.Empty, session.Script);
        Assert.Empty(session.Bounds);
        Assert.False(session.OnMouseDown(new SKPoint(10, 10), MouseButton.Left));
        Assert.False(session.OnKeyDown(Key.H, KeyModifiers.None, false));
        Assert.False(session.OnMouseWheel(new SKPoint(10, 10), 1));
        session.Release();
    }


    private static TutorialScript EveryPhaseScript() => new(1, null, new TutorialLimits(),
    [
        .. new[] { "Move", "Fire", "Salvo", "Results", "Outcome" }.Select(phase => new TutorialStep("say." + phase,
            "Tutorial.Journal.Empty", new TutorialCondition(phase, null, null, null, TutorialSignal.None),
            TutorialCompletion.PhaseLeft, null, 50))
    ]);

    private static readonly string[] Protected = ["pilot:", "pilot-panel:", "phase", "minimap", "hover", "action", "salvo-report", "queue"];

    private static void AssertPanelClear(TacticalBattleScreen screen, ISet<string> phases, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, width, height);
        var tutorial = screen.Tutorial!;
        var bounds = Assert.Single(tutorial.Bounds);
        if (tutorial.Director.Current is { } line) phases.Add(line.Step.When.Phase);
        foreach (var (key, rect) in screen.Layout.Rects)
        {
            if (!Protected.Any(p => key == p || (p.EndsWith(':') && key.StartsWith(p, StringComparison.Ordinal)))) continue;
            Assert.False(SKRect.Intersect(bounds, rect) is { IsEmpty: false },
                $"{width}x{height} {tutorial.Director.Current?.Step.When.Phase}: tutorial panel {bounds} overlaps {key} {rect}");
        }
    }

    [Fact]
    public async Task PanelNeverOverlapsPilotCardsPhasePanelOrMinimap()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var started = await StartF8(fixture);
        using var skins = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var screen = new TacticalBattleScreen(started.Flow, 1, skins.GetOrClassic("tabletop"))
        { ClockMilliseconds = started.ClockMilliseconds, Tutorial = new TutorialSession(EveryPhaseScript()) };
        fixture.Screens.Replace(screen);
        var phases = new HashSet<string>(StringComparer.Ordinal);

        void Check(bool wide = true)
        {
            AssertPanelClear(screen, phases, 1280, 720);
            if (wide) AssertPanelClear(screen, phases, 2560, 1080); // design width ~1707
        }

        Check();
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Tab)); // a selected pilot opens its card panel
        Check();
        for (int i = 0; screen.Flow.State!.Phase != TacticalBattlePhase.Over && i < 100; i++)
        {
            bool wide = i < 4;
            await fixture.Route(screen.OnKeyDown(Key.Enter));
            Check(wide);
            if (screen.HasSalvoPresentation)
            {
                fixture.AdvanceAnimation(TacticalBattleMovementAnimation.FireTransitionDurationMs);
                Check(wide); // salvo in flight
                screen.OnKeyDown(Key.Enter);
                Check(wide); // salvo results
            }
            fixture.FinishPresentation();
            Check(wide);
            if (screen.Flow.State!.Phase == TacticalBattlePhase.Fire) { screen.OnKeyDown(Key.Tab); Check(wide); }
        }
        Assert.Equal(TacticalBattlePhase.Over, screen.Flow.State!.Phase);
        Check();
        Assert.Superset(new HashSet<string> { "Move", "Fire", "Salvo", "Results", "Outcome" }, phases);
    }

    [Fact]
    public async Task ClicksInsideLivePanelAreCapturedOutsideAreNot()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var started = await StartF8(fixture);
        using var skins = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var session = new TutorialSession(new TutorialScript(1, null, new TutorialLimits(),
            [new TutorialStep("hello", "Tutorial.Journal.Empty", new TutorialCondition("Move", null, null, null, TutorialSignal.None),
                TutorialCompletion.Acknowledged, null, 50)]));
        var screen = new TacticalBattleScreen(started.Flow, 1, skins.GetOrClassic("tabletop"))
        { ClockMilliseconds = started.ClockMilliseconds, Tutorial = session };
        Render(screen);
        screen.OnKeyDown(Key.Tab);
        Render(screen);
        var panel = screen.Layout.Rects["tutorial:panel"];
        Assert.Contains(panel, screen.PanelRects);
        string before = Json(screen.Movement);
        var view = screen.View;

        var body = screen.Layout.Rects["tutorial:portrait"];
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(body.MidX, body.MidY, MouseButton.Left));
        screen.OnMouseUp(body.MidX, body.MidY, MouseButton.Left);
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(body.MidX, body.MidY, MouseButton.Right));
        screen.OnMouseMove(body.MidX + 200, body.MidY - 200);
        screen.OnMouseUp(body.MidX + 200, body.MidY - 200, MouseButton.Right);
        Render(screen);
        Assert.Equal(before, Json(screen.Movement)); // no path aiming under the panel
        Assert.Equal(view, screen.View);            // no camera pan started on the panel
        Assert.Equal("hello", session.Director.Current?.Step.Id);

        var ack = screen.Layout.Rects["tutorial:ack"];
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(ack.MidX, ack.MidY, MouseButton.Left));
        Assert.Equal(TutorialStepState.Completed, session.Director.StateOf("hello"));
        Render(screen);
        Assert.False(screen.Layout.Rects.ContainsKey("tutorial:panel"));
        var chip = screen.Layout.Rects["tutorial:chip"];
        Assert.Contains(chip, screen.PanelRects);
        Assert.DoesNotContain(panel, screen.PanelRects);
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
}
