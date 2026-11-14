using DeepSpaceSaga.Client.UI.Portraits;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Tutorial;

/// <summary>
/// Battle-tutorial companion of <see cref="TacticalBattleScreen"/>: fed with every rendered frame, draws on top of the overlay
/// and gets the first look at input that targets its own areas. Exists only in the tutorial battle. It never calls the Engine
/// and never touches battle orders; input hooks return true only for its own areas (the comms panel).
/// </summary>
internal sealed class TutorialSession
{
    private bool _portraitComposed;
    private SKBitmap? _portrait;

    internal TutorialSession(TutorialScript script)
    {
        Script = script;
        Director = new(script);
        ComposePortrait(); // window transition, not a render frame
    }

    internal TutorialScript Script { get; }
    internal TutorialDirector Director { get; }
    internal TutorialCommsPanel Panel { get; } = new();
    internal TutorialJournal Journal { get; } = new();

    /// <summary>Officer portrait (300x300) or null: no officer, no head asset, or released.</summary>
    internal SKBitmap? Portrait => _portrait;

    /// <summary>Areas that capture input, in design coordinates (the panel or chip as last drawn, and the open journal).</summary>
    internal IEnumerable<SKRect> Bounds
    {
        get
        {
            if (!Panel.Bounds.IsEmpty) yield return Panel.Bounds;
            if (Journal.IsOpen) yield return Journal.Bounds;
        }
    }

    /// <summary>The last frame observation (anchor templates are resolved against it).</summary>
    internal TutorialObservation? LastObservation { get; private set; }

    /// <summary>Steps heard in this battle, in spoken order (journal source).</summary>
    internal IReadOnlyList<TutorialStep> HeardSteps =>
        Director.Heard.Select(id => Script.Steps.First(s => s.Id == id)).ToArray();

    internal void Observe(TutorialObservation observation)
    {
        LastObservation = observation;
        Director.Observe(observation);
    }

    internal void Draw(SKCanvas canvas, BattleLayout layout, float designWidth, long nowMs)
    {
        if (Script.Steps.IsDefaultOrEmpty) return; // no lessons, no comms panel
        if (!_portraitComposed) ComposePortrait();
        var line = Director.Collapsed ? null : Director.Current; // a collapsed line shows as the chip, without a highlight
        if (Director.Enabled && line is { Step.Anchor: { } anchor } && LastObservation is { } observation &&
            TutorialHighlight.Target(layout, TutorialHighlight.ResolveKey(anchor, layout, observation, line.SignalUnitId)) is { } target)
            TutorialHighlight.Draw(canvas, layout, target, nowMs); // under the panel; not part of Bounds
        Panel.Draw(canvas, layout, designWidth, line, Director.Enabled, _portrait, Script.Officer);
        Journal.Draw(canvas, layout, HeardSteps);
    }

    internal bool OnMouseDown(SKPoint designPoint, MouseButton button)
    {
        if (Journal.OnMouseDown(designPoint)) return true;
        if (!Panel.Bounds.Contains(designPoint)) return false;
        if (button != MouseButton.Left) return true;
        switch (Panel.Hit(designPoint))
        {
            case "ack": Director.Acknowledge(); break;
            case "hints-off": Director.Enabled = false; break;
            case "hints-on": Director.Enabled = true; break;
            case null when Director.Collapsed && Panel.ChipBounds.Contains(designPoint): Director.Expand(); break;
        }
        return true;
    }

    /// <summary>H toggles the comms log (also with hints off); Esc closes an open log first. Everything else is the battle's.</summary>
    internal bool OnKeyDown(Key key, KeyModifiers modifiers, bool repeat)
    {
        if (Script.Steps.IsDefaultOrEmpty || repeat || modifiers != KeyModifiers.None) return false;
        if (key == Key.H) { Journal.Toggle(); return true; }
        if (key == Key.Escape && Journal.IsOpen) { Journal.Close(); return true; }
        return false;
    }

    internal bool OnMouseWheel(SKPoint designPoint, float delta) => Journal.OnMouseWheel(designPoint, delta);

    /// <summary>Releases the portrait bitmap; the next <see cref="Draw"/> composes it again.</summary>
    internal void Release()
    {
        _portrait?.Dispose();
        _portrait = null;
        _portraitComposed = false;
    }

    private void ComposePortrait()
    {
        _portraitComposed = true;
        if (Script.Officer is { } officer)
            _portrait = DialoguePortraitComposer.Compose(officer.Portrait, PersonSex.Female, officer.PortraitKey);
    }
}
