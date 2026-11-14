using DeepSpaceSaga.Client.UI.Portraits;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Tutorial;

/// <summary>
/// Battle-tutorial companion of <see cref="TacticalBattleScreen"/>: fed with every rendered frame, draws on top of the overlay
/// and gets the first look at input that targets its own areas. Exists only in the tutorial battle. It never calls the Engine
/// and never touches battle orders; input hooks return true only for its own areas (left button on the comms panel and log).
/// </summary>
internal sealed class TutorialSession
{
    private bool _portraitComposed;
    private SKBitmap? _portrait;
    private TutorialStep[] _heardSteps = [];

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

    /// <summary>
    /// Hints on/off for this battle: initialised from <c>gameSettings.tutorialHints</c> (default on) and switched by the panel's
    /// «Hints: off» button and «Hints: on» chip. Mirrors <see cref="TutorialDirector.Enabled"/>.
    /// </summary>
    internal bool HintsEnabled
    {
        get => Director.Enabled;
        init => Director.Enabled = value;
    }

    /// <summary>Saves a hints switch made in battle; false or an I/O error leaves the switch in effect for this battle.</summary>
    internal Func<bool, bool>? PersistHints { get; init; }

    /// <summary>
    /// Folder for the step events file (<see cref="TutorialMetrics"/>, next to the battle report); null records nothing.
    /// The window passes <see cref="TacticalBattleMetrics.DirectoryPath"/>.
    /// </summary>
    internal string? MetricsDirectory { get; init; }

    /// <summary>Receives a failed metrics write (default: Interface.log).</summary>
    internal Action<string>? MetricsWarning { get; init; }

    /// <summary>Step events of this battle; created from the first observation's battle when <see cref="MetricsDirectory"/> is set.</summary>
    internal TutorialMetrics? Metrics { get; private set; }

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

    /// <summary>
    /// Steps heard in this battle, in spoken order (journal source). Cached: rebuilt only when <see cref="TutorialDirector.Heard"/>
    /// grows (it is append-only), so a render frame allocates nothing here.
    /// </summary>
    internal IReadOnlyList<TutorialStep> HeardSteps
    {
        get
        {
            var heard = Director.Heard;
            if (_heardSteps.Length != heard.Count)
                _heardSteps = heard.Select(id => Script.Steps.First(s => s.Id == id)).ToArray();
            return _heardSteps;
        }
    }

    internal void Observe(TutorialObservation observation)
    {
        LastObservation = observation;
        if (Metrics is null && MetricsDirectory is { } directory)
            Metrics = new(observation.Battle.BattleId, directory)
            {
                ScenarioId = observation.Battle.ScenarioId,
                Seed = observation.Battle.Seed,
                OriginMs = observation.NowMs,
                Warning = MetricsWarning ?? InterfaceLog.Write,
            };
        Director.Observe(observation);
        RecordEvents();
    }

    /// <summary>Moves the director's new step events into the metrics file; without metrics they stay in the director (bounded by the steps).</summary>
    private void RecordEvents()
    {
        if (Metrics is not { } metrics) return;
        foreach (var e in Director.DrainEvents()) metrics.Record(e);
    }

    internal void Draw(SKCanvas canvas, BattleLayout layout, float designWidth, long nowMs)
    {
        if (Script.Steps.IsDefaultOrEmpty) return; // no lessons, no comms panel
        var line = Director.Collapsed ? null : Director.Current; // a collapsed line shows as the chip, without a highlight
        if (Director.Enabled && line is { Step.Anchor: { } anchor } && LastObservation is { } observation &&
            TutorialHighlight.Target(layout, TutorialHighlight.ResolveKey(anchor, layout, observation, line.SignalUnitId)) is { } target)
            TutorialHighlight.Draw(canvas, layout, target, nowMs); // under the panel; not part of Bounds
        Panel.Draw(canvas, layout, designWidth, line, Director.Enabled, _portrait, Script.Officer);
        Journal.Draw(canvas, layout, HeardSteps);
    }

    internal bool OnMouseDown(SKPoint designPoint, MouseButton button)
    {
        if (button != MouseButton.Left) return false; // right/middle pass through: the battle pans the camera from here too
        if (Journal.OnMouseDown(designPoint)) return true;
        if (!Panel.Bounds.Contains(designPoint)) return false;
        switch (Panel.Hit(designPoint))
        {
            case "ack": Director.Acknowledge(); break;
            case "hints-off": SwitchHints(false); break;
            case "hints-on": SwitchHints(true); break;
            case null when Director.Collapsed && Panel.ChipBounds.Contains(designPoint): Director.Expand(); break;
        }
        RecordEvents();
        return true;
    }

    private void SwitchHints(bool enabled)
    {
        bool changed = Director.Enabled != enabled;
        Director.Enabled = enabled; // applies now, whether or not the save below succeeds
        RecordEvents(); // a line dismissed by "off" comes before the switch itself
        if (changed && LastObservation is { } at) Metrics?.RecordHints(enabled, at.Phase, at.Turn, at.NowMs);
        try { PersistHints?.Invoke(enabled); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { } // the battle goes on
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

    /// <summary>
    /// Composes the portrait again after <see cref="Release"/>; called on screen activation (a transition), never from a render
    /// frame. Keeps an already composed bitmap.
    /// </summary>
    internal void Prepare()
    {
        if (!_portraitComposed) ComposePortrait();
    }

    /// <summary>Releases the portrait bitmap; <see cref="Draw"/> shows the placeholder until <see cref="Prepare"/>.</summary>
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
