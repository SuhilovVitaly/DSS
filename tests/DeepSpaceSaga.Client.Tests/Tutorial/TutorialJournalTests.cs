using DeepSpaceSaga.Client.Tests.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens.Tutorial;
using Silk.NET.Input;
using SkiaSharp;
using static DeepSpaceSaga.Client.Tests.Tutorial.TutorialCommsPanelTests;

namespace DeepSpaceSaga.Client.Tests.Tutorial;

/// <summary>EP-0028-US-0002-TK-0005: H opens the comms log with every line heard in this battle, in order, in the current language.</summary>
[Collection("Battle localization")]
public sealed class TutorialJournalTests
{
    // Existing UI keys stand in for lesson lines (US-0003 adds the real ones).
    private static readonly string[] LineKeys = ["Tutorial.Panel.Title", "Tutorial.Journal.Empty", "Settings.TutorialHints.SaveFailed"];

    /// <summary>A session that has already said the three lines (acknowledged one after another; pacing off).</summary>
    private static TutorialSession HeardThree()
    {
        var session = new TutorialSession(new TutorialScript(1, null, new TutorialLimits(0, int.MaxValue, int.MaxValue),
            [Step("one", LineKeys[0], priority: 90), Step("two", LineKeys[1], priority: 80), Step("three", LineKeys[2], priority: 70)]));
        for (int i = 0; i < LineKeys.Length; i++)
        {
            session.Observe(Move(i));
            session.Director.Acknowledge();
        }
        Assert.Equal(["one", "two", "three"], session.Director.Heard);
        return session;
    }

    private static SKPoint Center(SKRect r) => new(r.MidX, r.MidY);

    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void HTogglesJournalWithLinesHeardInOrder(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        var session = HeardThree();
        Assert.False(session.Journal.IsOpen);
        Assert.False(Draw(session).Rects.ContainsKey("tutorial:journal"));

        Assert.True(session.OnKeyDown(Key.H, KeyModifiers.None, false));
        Assert.True(session.Journal.IsOpen);
        var layout = Draw(session);
        var journal = layout.Rects["tutorial:journal"];
        Assert.Equal(new SKRect(TutorialCommsPanel.Left, TutorialJournal.BottomEdge - TutorialJournal.Height,
            TutorialCommsPanel.Left + TutorialJournal.Width, TutorialJournal.BottomEdge), journal);
        Assert.True(journal.Contains(layout.Rects["tutorial:journal-close"]));
        Assert.Contains(journal, session.Bounds);

        var texts = layout.Texts.Select(t => t.Text).ToList();
        Assert.Contains(Localization.Get("Tutorial.Journal.Title"), texts);
        Assert.Contains(Localization.Get("Tutorial.Journal.Close"), texts);
        int[] order = LineKeys.Select(k => texts.IndexOf(TutorialCommsPanel.Wrap(Localization.Get(k), TutorialJournal.TextWidth)[0])).ToArray();
        Assert.All(order, i => Assert.True(i >= 0));
        Assert.Equal(order.Order(), order); // spoken order, top to bottom

        Assert.False(session.OnKeyDown(Key.H, KeyModifiers.None, true)); // auto-repeat does not flicker the log
        Assert.False(session.OnKeyDown(Key.H, KeyModifiers.Shift, false));
        Assert.False(session.OnKeyDown(Key.Enter, KeyModifiers.None, false)); // other keys keep their battle meaning
        Assert.False(session.OnKeyDown(Key.Tab, KeyModifiers.None, false));
        Assert.True(session.Journal.IsOpen);
        Assert.True(session.OnKeyDown(Key.H, KeyModifiers.None, false));
        Assert.False(session.Journal.IsOpen);
        layout = Draw(session);
        Assert.False(layout.Rects.ContainsKey("tutorial:journal"));
        Assert.DoesNotContain(session.Bounds, b => b == journal);
    }

    [Fact]
    public void EmptyJournalSaysSoAndAScriptWithoutStepsHasNoJournal()
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        var session = new TutorialSession(Script(null, Step("later", phase: "Fire")));
        session.Observe(Move());
        Assert.True(session.OnKeyDown(Key.H, KeyModifiers.None, false));
        Assert.Contains("Сообщений пока нет.", Draw(session).Texts.Select(t => t.Text));

        var none = new TutorialSession(TutorialScript.Empty);
        Assert.False(none.OnKeyDown(Key.H, KeyModifiers.None, false));
        Assert.False(none.Journal.IsOpen);
    }

    [Fact]
    public void JournalCapturesOnlyItsOwnClicksAndWheel()
    {
        using var locale = Localization.UseLocaleForTests("English");
        var session = HeardThree();
        Assert.False(session.OnMouseWheel(new SKPoint(400, 450), 1)); // closed: the wheel belongs to the battle
        session.OnKeyDown(Key.H, KeyModifiers.None, false);
        var layout = Draw(session);
        var journal = layout.Rects["tutorial:journal"];

        Assert.True(session.OnMouseDown(new SKPoint(journal.MidX, journal.MidY), MouseButton.Left));
        Assert.True(session.OnMouseDown(new SKPoint(journal.MidX, journal.MidY), MouseButton.Right));
        Assert.True(session.Journal.IsOpen);
        Assert.True(session.OnMouseWheel(new SKPoint(journal.MidX, journal.MidY), -1));
        Assert.False(session.OnMouseWheel(new SKPoint(journal.Right + 10, journal.MidY), -1));
        Assert.False(session.OnMouseDown(new SKPoint(journal.Right + 10, journal.MidY), MouseButton.Left));
        Assert.False(session.OnMouseDown(new SKPoint(journal.MidX, journal.Top - 10), MouseButton.Left));

        Assert.True(session.OnMouseDown(Center(layout.Rects["tutorial:journal-close"]), MouseButton.Left));
        Assert.False(session.Journal.IsOpen);
        Assert.False(session.OnMouseWheel(new SKPoint(journal.MidX, journal.MidY), -1));
    }

    [Fact]
    public void LongJournalScrollsAndOpensAtTheLatestLine()
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        var steps = Enumerable.Range(0, 20).Select(i => Step("s" + i, "Settings.TutorialHints.SaveFailed", priority: 100 - i)).ToArray();
        var session = new TutorialSession(new TutorialScript(1, null, new TutorialLimits(0, int.MaxValue, int.MaxValue), [.. steps]));
        for (int i = 0; i < steps.Length; i++) { session.Observe(Move(i)); session.Director.Acknowledge(); }
        Assert.Equal(20, session.Director.Heard.Count);

        session.OnKeyDown(Key.H, KeyModifiers.None, false);
        Draw(session);
        float end = session.Journal.Scroll;
        Assert.True(end > 0); // opened at the latest lines
        Assert.Equal(session.Journal.MaxScroll, end);
        var journal = session.Journal.Bounds;
        session.OnMouseWheel(new SKPoint(journal.MidX, journal.MidY), 1); // wheel up: earlier lines
        Draw(session);
        Assert.True(session.Journal.Scroll < end);
        for (int i = 0; i < 50; i++) session.OnMouseWheel(new SKPoint(journal.MidX, journal.MidY), 1);
        Draw(session);
        Assert.Equal(0, session.Journal.Scroll);
        for (int i = 0; i < 50; i++) session.OnMouseWheel(new SKPoint(journal.MidX, journal.MidY), -1);
        Draw(session);
        Assert.Equal(end, session.Journal.Scroll);
    }

    [Fact]
    public void JournalWorksWithHintsDisabled()
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        var session = HeardThree();
        session.Director.Enabled = false;
        Assert.True(session.OnKeyDown(Key.H, KeyModifiers.None, false));
        var layout = Draw(session);
        Assert.True(layout.Rects.ContainsKey("tutorial:journal"));
        Assert.True(layout.Rects.ContainsKey("tutorial:chip")); // the «Hints: on» chip stays available below
        var texts = layout.Texts.Select(t => t.Text).ToArray();
        Assert.Contains(Localization.Get("Tutorial.Panel.HintsOn"), texts);
        Assert.Contains(TutorialCommsPanel.Wrap(Localization.Get(LineKeys[2]), TutorialJournal.TextWidth)[0], texts);
    }
}

/// <summary>EP-0028-US-0002-TK-0005: the journal inside the live battle screen (Esc route, capture areas).</summary>
[Collection("InterfaceLog")]
public sealed class TutorialJournalScreenTests
{
    private static void Render(TacticalBattleScreen screen)
    {
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
    }

    [Fact]
    public async Task EscClosesOpenJournalOnlyAndOtherwiseKeepsExitConfirmation()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        await fixture.PressKey(Key.F8);
        var setup = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        var started = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        using var skins = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var session = new TutorialSession(Script(null, Step("hello")));
        var screen = new TacticalBattleScreen(started.Flow, 1, skins.GetOrClassic("tabletop"))
        { ClockMilliseconds = started.ClockMilliseconds, Tutorial = session };
        fixture.Screens.Replace(screen);
        Render(screen);

        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.H));
        Assert.True(session.Journal.IsOpen);
        Render(screen);
        var journal = screen.Layout.Rects["tutorial:journal"];
        Assert.Contains(journal, screen.PanelRects);

        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Tab)); // battle keys still work with the log open
        Assert.NotNull(screen.Movement.Selected);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Escape));
        Assert.False(session.Journal.IsOpen);
        Assert.False(screen.ConfirmExit);

        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Escape)); // closed log: the usual exit confirmation
        Assert.True(screen.ConfirmExit);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.H)); // not during the exit confirmation
        Assert.False(session.Journal.IsOpen);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Escape));
        Assert.False(screen.ConfirmExit);
        Render(screen);
        Assert.DoesNotContain(journal, screen.PanelRects);
    }
}
