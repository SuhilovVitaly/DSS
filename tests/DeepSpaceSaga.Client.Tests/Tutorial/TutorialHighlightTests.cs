using DeepSpaceSaga.Client.Tests.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens.Tutorial;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.Tutorial;

/// <summary>EP-0028-US-0002-TK-0003: the line's anchor is resolved against the current frame layout and gets a pulsing frame + arrow.</summary>
[Collection("InterfaceLog")]
public sealed class TutorialHighlightTests
{
    private static TutorialScript Script(string anchor, string phase = "Move") => new(1, null, new TutorialLimits(),
        [new TutorialStep("lesson", "Tutorial.Journal.Empty", new TutorialCondition(phase, null, null, null, TutorialSignal.None),
            TutorialCompletion.Acknowledged, anchor, 50)]);

    private static async Task<(BattleNavigationTests.Fixture Fixture, TacticalBattleScreen Screen, TutorialSession Session)> Live(
        BattleNavigationTests.Fixture fixture, TutorialScript script)
    {
        await fixture.PressKey(Key.F8);
        var setup = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        var started = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        var skins = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var session = new TutorialSession(script);
        var screen = new TacticalBattleScreen(started.Flow, 1, skins.GetOrClassic("tabletop"))
        { ClockMilliseconds = started.ClockMilliseconds, Tutorial = session };
        fixture.Screens.Replace(screen);
        return (fixture, screen, session);
    }

    private static void Render(TacticalBattleScreen screen)
    {
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
    }

    private static string? Resolve(TacticalBattleScreen screen, TutorialSession session, string? anchor, string? signal = null) =>
        TutorialHighlight.ResolveKey(anchor, screen.Layout, session.LastObservation!, signal);

    private static bool OwnDrone(TacticalBattleSnapshot battle, string id) =>
        battle.Units.Any(u => u.Id == id && u.Side == battle.PlayerSide && u.Kind == TacticalUnitKind.Drone);

    [Fact]
    public async Task ResolvesEachTemplateAgainstLiveTabletopLayout()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var (_, screen, session) = await Live(fixture, Script("pilot:{own}"));
        Render(screen);
        var observation = session.LastObservation!;
        Assert.Null(observation.SelectedDroneId);

        // {own}: first own drone (snapshot order) that has the key.
        string firstOwn = observation.Battle.Units.First(u => OwnDrone(observation.Battle, u.Id) &&
            u.State != TacticalUnitState.Destroyed && screen.Layout.Rects.ContainsKey("pilot:" + u.Id)).Id;
        Assert.Equal("pilot:" + firstOwn, Resolve(screen, session, "pilot:{own}"));
        Assert.True(screen.Layout.Rects.ContainsKey("tutorial:highlight"));
        Assert.True(screen.Layout.Rects.ContainsKey("tutorial:arrow"));
        Assert.Null(Resolve(screen, session, "endpoint:{selected}")); // nothing selected yet
        Assert.Equal("button:primary", Resolve(screen, session, "button:primary"));
        Assert.Equal("minimap", Resolve(screen, session, "minimap"));

        screen.OnKeyDown(Key.Tab);
        screen.OnKeyDown(Key.Tab); // second pilot: {own} now prefers the selected one
        Render(screen);
        string selected = session.LastObservation!.SelectedDroneId!;
        Assert.NotEqual(firstOwn, selected);
        Assert.Equal("pilot:" + selected, Resolve(screen, session, "pilot:{own}"));
        Assert.Equal("endpoint:" + selected, Resolve(screen, session, "endpoint:{selected}"));
        Assert.Equal("reachable", Resolve(screen, session, "reachable"));
        Assert.Equal("pilot:" + firstOwn, Resolve(screen, session, "pilot:{signal}", firstOwn));
        Assert.Null(Resolve(screen, session, "pilot:{signal}", null));
        Assert.Equal(screen.Layout.Rects["pilot:" + selected], TutorialHighlight.Target(screen.Layout, "pilot:" + selected));

        for (int i = 0; screen.Flow.State!.Phase != TacticalBattlePhase.Fire && i < 30; i++)
        {
            await fixture.Route(screen.OnKeyDown(Key.Enter)); // until the sides are in range
            fixture.FinishPresentation();
            Render(screen);
        }
        Assert.Equal(TacticalBattlePhase.Fire, screen.Flow.State!.Phase);
        screen.OnKeyDown(Key.Tab);
        Render(screen);
        var fire = session.LastObservation!;
        string firstCandidate = fire.Battle.Units.First(u => screen.Layout.Rects.ContainsKey("candidate:" + u.Id)).Id;
        Assert.False(OwnDrone(fire.Battle, firstCandidate));
        Assert.Equal("candidate:" + firstCandidate, Resolve(screen, session, "candidate:{any}"));
        Assert.Equal("card-command:hold", Resolve(screen, session, "card-command:hold"));
    }

    [Fact]
    public async Task HighlightDoesNotCaptureClicks()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var (_, screen, session) = await Live(fixture, Script("pilot:{own}"));
        Render(screen);
        var highlight = screen.Layout.Rects["tutorial:highlight"];
        var arrow = screen.Layout.Rects["tutorial:arrow"];
        Assert.DoesNotContain(screen.PanelRects, r => r == highlight || r == arrow);
        Assert.Equal([session.Panel.Bounds], session.Bounds);

        var card = screen.Layout.Rects[session.Director.Current!.Step.Anchor!.Replace("{own}",
            session.LastObservation!.Battle.Units.First(u => screen.Layout.Rects.ContainsKey("pilot:" + u.Id)).Id)];
        Assert.Null(screen.Movement.Selected);
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(card.MidX, card.MidY, MouseButton.Left));
        screen.OnMouseUp(card.MidX, card.MidY, MouseButton.Left);
        Assert.NotNull(screen.Movement.Selected); // the click under the highlight reached the pilot card
    }

    [Fact]
    public async Task MissingAnchorDrawsNothing()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var (_, screen, session) = await Live(fixture, Script("evade-ring:{selected}"));
        Render(screen);
        Assert.Equal("lesson", session.Director.Current?.Step.Id);
        Assert.False(screen.Layout.Rects.ContainsKey("tutorial:highlight"));
        Assert.False(screen.Layout.Rects.ContainsKey("tutorial:arrow"));
        Assert.True(screen.Layout.Rects.ContainsKey("tutorial:panel"));
        Assert.Null(Resolve(screen, session, "no-such-anchor"));
        Assert.Null(Resolve(screen, session, null));
        Assert.Null(Resolve(screen, session, "pilot:{unknown}"));

        session.Director.Enabled = false; // hints off: no highlight even for a resolvable anchor
        var hinted = await Live(await BattleNavigationTests.Fixture.Create(), Script("button:primary"));
        await using var second = hinted.Fixture;
        Render(hinted.Screen);
        Assert.True(hinted.Screen.Layout.Rects.ContainsKey("tutorial:highlight"));
        hinted.Session.Director.Enabled = false;
        Render(hinted.Screen);
        Assert.False(hinted.Screen.Layout.Rects.ContainsKey("tutorial:highlight"));
    }

    [Fact]
    public void PulseDependsOnlyOnInjectedClock()
    {
        Assert.Equal(0.55f, TutorialHighlight.PulseAlpha(0), 3);
        Assert.Equal(1.00f, TutorialHighlight.PulseAlpha(300), 3);
        Assert.Equal(0.10f, TutorialHighlight.PulseAlpha(900), 3);
        Assert.Equal(TutorialHighlight.PulseAlpha(250), TutorialHighlight.PulseAlpha(250 + 1200 * 7));

        byte[] Pixels(long now, SKRect target, out BattleLayout layout)
        {
            using var bitmap = new SKBitmap(1280, 720);
            using var canvas = new SKCanvas(bitmap);
            layout = new BattleLayout();
            TutorialHighlight.Draw(canvas, layout, target, now);
            return bitmap.Bytes;
        }

        var target = new SKRect(300, 200, 400, 260);
        Assert.Equal(Pixels(300, target, out var high), Pixels(300, target, out _));
        Assert.NotEqual(Pixels(300, target, out _), Pixels(900, target, out _));
        var frame = high.Rects["tutorial:highlight"];
        Assert.Equal(new SKRect(296, 196, 404, 264), frame);
        var arrow = high.Rects["tutorial:arrow"];
        Assert.True(arrow.Bottom <= frame.Top && arrow.Height == 12); // above the target, tip down

        Pixels(0, new SKRect(10, 20, 60, 50), out var top);
        Assert.True(top.Rects["tutorial:arrow"].Top >= top.Rects["tutorial:highlight"].Bottom); // near the top edge: below, tip up
    }
}
