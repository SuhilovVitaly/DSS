using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Tutorial;
using Silk.NET.Input;
using SkiaSharp;
using static DeepSpaceSaga.Client.Tests.Tutorial.TutorialCommsPanelTests;

namespace DeepSpaceSaga.Client.Tests.Tutorial;

/// <summary>EP-0028-US-0004-TK-0003: the in-battle hints button and chip start from the saved preference and save every change.</summary>
[Collection("Battle localization")]
public sealed class TutorialHintsToggleTests
{
    [Fact]
    public void ToggleInBattleCallsPersistWithNewValue()
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        var saved = new List<bool>();
        var session = new TutorialSession(Script(null, Step("only", complete: TutorialCompletion.PilotSelected)))
        {
            PersistHints = value =>
            {
                saved.Add(value);
                return true;
            },
        };
        Assert.True(session.HintsEnabled);
        session.Observe(Move());
        var layout = Draw(session);

        Assert.True(session.OnMouseDown(Center(layout.Rects["tutorial:hints"]), MouseButton.Left));
        Assert.False(session.HintsEnabled);
        Assert.False(session.Director.Enabled);
        Assert.Equal(new[] { false }, saved);

        layout = Draw(session);
        var chip = layout.Rects["tutorial:chip"];
        Assert.False(session.OnMouseDown(Center(chip), MouseButton.Right)); // passes through, nothing saved
        Assert.Equal(new[] { false }, saved);
        Assert.True(session.OnMouseDown(Center(chip), MouseButton.Left));
        Assert.True(session.HintsEnabled);
        Assert.Equal(new[] { false, true }, saved);

        // Acknowledge and clicks outside never save.
        session.Observe(Move(now: 100_000));
        layout = Draw(session, now: 100_000);
        if (layout.Rects.TryGetValue("tutorial:ack", out var ack)) session.OnMouseDown(Center(ack), MouseButton.Left);
        Assert.False(session.OnMouseDown(new SKPoint(1270, 10), MouseButton.Left));
        Assert.Equal(new[] { false, true }, saved);
    }

    /// <summary>
    /// US-0006-TK-0004: with hints on but no line shown (nothing yet, or a line folded into the chip), the chip still offers
    /// «Hints: off»; the switch is saved through the same PersistHints path as the panel button.
    /// </summary>
    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void ChipOffersHintsOffWhileNoLineIsShown(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        var saved = new List<bool>();
        var session = new TutorialSession(Script(null, Step("later", phase: "Fire")))
        {
            PersistHints = value =>
            {
                saved.Add(value);
                return true;
            },
        };
        session.Observe(Move());
        var layout = Draw(session);
        Assert.Null(session.Director.Current);
        var chip = layout.Rects["tutorial:chip"];
        var off = layout.Rects["tutorial:hints"];
        Assert.Contains(Localization.Get("Tutorial.Panel.HintsOff"), layout.Texts.Select(t => t.Text));
        Assert.True(off.Right <= 1280 - 246, off.ToString()); // stays left of the right panel
        Assert.Equal("hints-off", session.Panel.Hit(Center(off)));
        Assert.Null(session.Panel.Hit(Center(chip)));

        Assert.True(session.OnMouseDown(Center(off), MouseButton.Left));
        Assert.False(session.HintsEnabled);
        Assert.Equal(new[] { false }, saved);
        layout = Draw(session);
        Assert.False(layout.Rects.ContainsKey("tutorial:hints")); // off: the chip itself is «Hints: on»
        Assert.Equal("hints-on", session.Panel.Hit(Center(layout.Rects["tutorial:chip"])));

        // A collapsed line: the chip again offers «Hints: off», and the chip itself still unfolds the line.
        var collapsed = new TutorialSession(Script(null, Step("only", complete: TutorialCompletion.PilotSelected)))
        {
            PersistHints = value =>
            {
                saved.Add(value);
                return true;
            },
        };
        collapsed.Observe(Move());
        collapsed.Observe(Move(now: new TutorialLimits().AutoCollapseMs));
        Assert.True(collapsed.Director.Collapsed);
        layout = Draw(collapsed);
        Assert.Contains(Localization.Get("Tutorial.Panel.HintsOff"), layout.Texts.Select(t => t.Text));
        Assert.True(collapsed.OnMouseDown(Center(layout.Rects["tutorial:hints"]), MouseButton.Left));
        Assert.False(collapsed.HintsEnabled);
        Assert.Equal(TutorialStepState.Dismissed, collapsed.Director.StateOf("only"));
        Assert.Equal(new[] { false, false }, saved);
    }

    [Fact]
    public void StartsSilentWhenPreferenceIsOff()
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        var session = new TutorialSession(Script(Officer(), Step("hello"))) { HintsEnabled = false };
        Assert.False(session.Director.Enabled);
        session.Observe(Move());
        var layout = Draw(session);

        Assert.Null(session.Director.Current); // no line is shown
        Assert.Empty(session.Director.Heard);
        Assert.False(layout.Rects.ContainsKey("tutorial:panel"));
        var chip = layout.Rects["tutorial:chip"];
        Assert.Contains(Localization.Get("Tutorial.Panel.HintsOn"), layout.Texts.Select(t => t.Text));
        Assert.Equal("hints-on", session.Panel.Hit(Center(chip)));
        Assert.True(session.OnKeyDown(Key.H, KeyModifiers.None, false)); // the journal still works
        Assert.True(session.Journal.IsOpen);

        var defaults = new TutorialSession(Script(Officer(), Step("hello")));
        Assert.True(defaults.HintsEnabled);
        Assert.True(defaults.Director.Enabled);
    }

    [Fact]
    public void PersistFailureKeepsBattleRunning()
    {
        using var locale = Localization.UseLocaleForTests("English");
        int calls = 0;
        var session = new TutorialSession(Script(null, Step("only", complete: TutorialCompletion.PilotSelected)))
        {
            PersistHints = _ =>
            {
                calls++;
                return false;
            },
        };
        session.Observe(Move());
        var layout = Draw(session);
        Assert.True(session.OnMouseDown(Center(layout.Rects["tutorial:hints"]), MouseButton.Left));
        Assert.False(session.HintsEnabled); // the player wants silence now, saved or not
        layout = Draw(session);
        Assert.True(session.OnMouseDown(Center(layout.Rects["tutorial:chip"]), MouseButton.Left));
        Assert.True(session.HintsEnabled);
        Assert.Equal(2, calls);

        var throwing = new TutorialSession(Script(null, Step("only", complete: TutorialCompletion.PilotSelected)))
        {
            PersistHints = _ => throw new IOException("disk is gone"),
        };
        throwing.Observe(Move());
        layout = Draw(throwing);
        Assert.True(throwing.OnMouseDown(Center(layout.Rects["tutorial:hints"]), MouseButton.Left));
        Assert.False(throwing.HintsEnabled);
        Draw(throwing); // still draws
    }

    private static SKPoint Center(SKRect r) => new(r.MidX, r.MidY);
}
