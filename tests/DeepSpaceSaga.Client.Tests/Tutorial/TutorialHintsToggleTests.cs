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
        Assert.True(session.OnMouseDown(Center(chip), MouseButton.Right)); // captured, nothing saved
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
