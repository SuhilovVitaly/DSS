using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens.Tutorial;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.Tutorial;

/// <summary>EP-0028-US-0002-TK-0002: the comms panel shows the officer's current line and drives the director by its own buttons.</summary>
[Collection("Battle localization")]
public sealed class TutorialCommsPanelTests
{
    internal const string OfficerPortrait = "Images/Persons/W4/Portraits/CHR-20260915-104309-12BAB0.png";

    // Existing UI keys stand in for the officer/lesson keys that US-0003 adds; the panel must resolve whatever keys the script names.
    internal static TutorialOfficer Officer(string portrait = OfficerPortrait) =>
        new("Tutorial.Journal.Title", "Tutorial.Panel.Title", portrait, "tutorial.officer");

    internal static TutorialStep Step(string id, string lineKey = "Tutorial.Journal.Empty", string phase = "Move",
        TutorialCompletion complete = TutorialCompletion.Acknowledged, int priority = 50, string? anchor = null) =>
        new(id, lineKey, new TutorialCondition(phase, null, null, null, TutorialSignal.None), complete, anchor, priority);

    internal static TutorialScript Script(TutorialOfficer? officer, params TutorialStep[] steps) =>
        new(1, officer, new TutorialLimits(), [.. steps]);

    internal static TutorialObservation Move(long now = 0, string? selected = null) =>
        new("Move", 1, TutorialTestBattle.Battle(TacticalBattlePhase.Move, 1), selected, null, null, now);

    /// <summary>Draws the session into a 1280x720 Tabletop canvas the way RenderFrame does and returns the recorded layout.</summary>
    internal static BattleLayout Draw(TutorialSession session, float designWidth = 1280, long now = 0)
    {
        using var skins = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        using var scope = TacticalBattleSkinRenderScope.Enter(skins.GetOrClassic("tabletop"));
        using var bitmap = new SKBitmap((int)designWidth, 720);
        using var canvas = new SKCanvas(bitmap);
        var layout = new BattleLayout();
        BattleDesign.Recording = layout;
        try { session.Draw(canvas, layout, designWidth, now); }
        finally { BattleDesign.Recording = null; }
        return layout;
    }

    private static SKPoint Center(SKRect r) => new(r.MidX, r.MidY);

    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void ExpandedPanelShowsOfficerLineAndButtonsInCurrentLanguage(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        var session = new TutorialSession(Script(Officer(), Step("hello")));
        session.Observe(Move());
        var layout = Draw(session);

        var panel = layout.Rects["tutorial:panel"];
        Assert.Equal(new SKRect(TutorialCommsPanel.Left, TutorialCommsPanel.Bottom - TutorialCommsPanel.Height,
            TutorialCommsPanel.Left + TutorialCommsPanel.Width, TutorialCommsPanel.Bottom), panel);
        Assert.Equal([panel], session.Bounds);
        Assert.True(panel.Contains(layout.Rects["tutorial:ack"]));
        Assert.True(panel.Contains(layout.Rects["tutorial:hints"]));
        Assert.True(panel.Contains(layout.Rects["tutorial:portrait"]));
        Assert.False(layout.Rects.ContainsKey("tutorial:chip"));

        var texts = layout.Texts.Select(t => t.Text).ToArray();
        Assert.Contains(ControlText.SafeSymbols(Localization.Get("Tutorial.Journal.Title") + " · " + Localization.Get("Tutorial.Panel.Title")), texts);
        Assert.Contains(Localization.Get("Tutorial.Journal.Empty"), texts);
        Assert.Contains(Localization.Get("Tutorial.Panel.Acknowledge"), texts);
        Assert.Contains(Localization.Get("Tutorial.Panel.HintsOff"), texts);
        Assert.DoesNotContain(texts, t => t.StartsWith("Tutorial.", StringComparison.Ordinal));
        Assert.NotNull(session.Portrait);
        session.Release();
        Assert.Null(session.Portrait);
        Draw(session);
        Assert.NotNull(session.Portrait); // recomposed on the next draw after Release
        session.Release();
    }

    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void WithoutLineThePanelCollapsesToTheCommsChip(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        var session = new TutorialSession(Script(null, Step("later", phase: "Fire")));
        session.Observe(Move());
        var layout = Draw(session);

        var chip = layout.Rects["tutorial:chip"];
        Assert.Equal(new SKRect(TutorialCommsPanel.Left, TutorialCommsPanel.Bottom - 30, TutorialCommsPanel.Left + 190,
            TutorialCommsPanel.Bottom), chip);
        Assert.Equal([chip], session.Bounds);
        Assert.False(layout.Rects.ContainsKey("tutorial:panel"));
        Assert.Contains(ControlText.SafeSymbols(Localization.Get("Tutorial.Panel.Title") + " · " + Localization.Get("Tutorial.Panel.JournalHint")),
            layout.Texts.Select(t => t.Text));
    }

    [Fact]
    public void WrapKeepsLinesInsideTheTextWidthAndTheDrawnLineIsAtMostTwoRows()
    {
        string longText = string.Join(' ', Enumerable.Repeat("Курс прокладывается стрелками, конец пути можно тянуть мышью.", 4));
        var lines = TutorialCommsPanel.Wrap(longText);
        Assert.True(lines.Length > 2);
        using var paint = BattleTypography.TextPaint(TutorialCommsPanel.FontSize, false);
        Assert.All(lines, line => Assert.True(paint.MeasureText(line) <= TutorialCommsPanel.TextWidth, line));

        var rows = TutorialCommsPanel.Rows(longText);
        Assert.Equal(2, rows.Length);
        Assert.Equal(lines[0], rows[0]);
        Assert.EndsWith(ControlText.SafeSymbols("…"), rows[1]);
        Assert.True(paint.MeasureText(rows[1]) <= TutorialCommsPanel.TextWidth);
        Assert.Equal("Коротко.", Assert.Single(TutorialCommsPanel.Rows("Коротко.")));
    }

    [Fact]
    public void AcknowledgeAndHintsButtonsDriveTheDirector()
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        var session = new TutorialSession(Script(null,
            Step("first", priority: 60), Step("second", complete: TutorialCompletion.PhaseLeft, priority: 50)));
        session.Observe(Move(0));
        var layout = Draw(session);
        Assert.Equal("first", session.Director.Current?.Step.Id);

        Assert.True(session.OnMouseDown(Center(layout.Rects["tutorial:ack"]), MouseButton.Left));
        Assert.Equal(TutorialStepState.Completed, session.Director.StateOf("first"));
        session.Observe(Move(100));
        layout = Draw(session);
        Assert.Equal("second", session.Director.Current?.Step.Id);

        Assert.True(session.OnMouseDown(Center(layout.Rects["tutorial:ack"]), MouseButton.Left));
        Assert.Equal(TutorialStepState.Dismissed, session.Director.StateOf("second")); // action step: "got it" dismisses it

        var again = new TutorialSession(Script(null, Step("only", complete: TutorialCompletion.PilotSelected)));
        again.Observe(Move());
        layout = Draw(again);
        Assert.True(again.OnMouseDown(Center(layout.Rects["tutorial:hints"]), MouseButton.Left));
        Assert.False(again.Director.Enabled);
        Assert.Equal(TutorialStepState.Dismissed, again.Director.StateOf("only"));
        layout = Draw(again);
        Assert.False(layout.Rects.ContainsKey("tutorial:panel"));
        var chip = layout.Rects["tutorial:chip"];
        Assert.Contains(Localization.Get("Tutorial.Panel.HintsOn"), layout.Texts.Select(t => t.Text));
        Assert.Equal("hints-on", again.Panel.Hit(Center(chip)));

        Assert.True(again.OnMouseDown(Center(chip), MouseButton.Right)); // captured, but only the left button acts
        Assert.False(again.Director.Enabled);
        Assert.True(again.OnMouseDown(Center(chip), MouseButton.Left));
        Assert.True(again.Director.Enabled);
        Assert.False(again.OnMouseDown(new SKPoint(chip.Right + 5, chip.MidY), MouseButton.Left));
    }

    [Fact]
    public void ClicksInsidePanelAreCapturedOutsideAreNot()
    {
        var session = new TutorialSession(Script(null, Step("hello")));
        Assert.Empty(session.Bounds); // nothing drawn yet
        Assert.False(session.OnMouseDown(new SKPoint(300, 680), MouseButton.Left));
        session.Observe(Move());
        var layout = Draw(session);
        var panel = layout.Rects["tutorial:panel"];
        var portrait = layout.Rects["tutorial:portrait"];

        Assert.True(session.OnMouseDown(Center(portrait), MouseButton.Left));
        Assert.True(session.OnMouseDown(Center(panel), MouseButton.Right));
        Assert.Equal("hello", session.Director.Current?.Step.Id); // a click on the body changes nothing
        Assert.False(session.OnMouseDown(new SKPoint(panel.Left - 1, panel.MidY), MouseButton.Left));
        Assert.False(session.OnMouseDown(new SKPoint(panel.MidX, panel.Top - 1), MouseButton.Left));
        Assert.False(session.OnMouseDown(new SKPoint(640, 300), MouseButton.Left));
    }

    [Fact]
    public void MissingPortraitDrawsPlaceholder()
    {
        using var locale = Localization.UseLocaleForTests("English");
        var missing = new TutorialSession(Script(Officer("Images/Persons/W4/Portraits/missing-head.png"), Step("hello")));
        Assert.Null(missing.Portrait);
        missing.Observe(Move());
        var layout = Draw(missing);
        Assert.True(layout.Rects.ContainsKey("tutorial:portrait"));
        Assert.Contains(Localization.Get("Tutorial.Journal.Title")[..1], layout.Texts.Select(t => t.Text));

        var anonymous = new TutorialSession(Script(null, Step("hello")));
        Assert.Null(anonymous.Portrait);
        anonymous.Observe(Move());
        layout = Draw(anonymous);
        Assert.Contains(Localization.Get("Tutorial.Panel.Title")[..1], layout.Texts.Select(t => t.Text));
        Assert.Contains(Localization.Get("Tutorial.Panel.Title"), layout.Texts.Select(t => t.Text));
        missing.Release();
        anonymous.Release();
    }

    [Fact]
    public void NarrowWindowShrinksThePanelBetweenPilotColumnAndRightPanel()
    {
        var session = new TutorialSession(Script(null, Step("hello")));
        session.Observe(Move());
        var wide = Draw(session, 1707);
        Assert.Equal(TutorialCommsPanel.Width, wide.Rects["tutorial:panel"].Width);
        var narrow = Draw(session, 900); // 900 - 246 - 206 = 448
        Assert.Equal(448, narrow.Rects["tutorial:panel"].Width);
        Assert.True(narrow.Rects["tutorial:panel"].Contains(narrow.Rects["tutorial:ack"]));
    }
}
