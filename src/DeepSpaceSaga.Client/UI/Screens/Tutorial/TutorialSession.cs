using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Tutorial;

/// <summary>
/// Battle-tutorial companion of <see cref="TacticalBattleScreen"/>: fed with every rendered frame, draws on top of the overlay
/// and gets the first look at input that targets its own areas. Exists only in the tutorial battle. It never calls the Engine
/// and never touches battle orders; input hooks return true only for its own areas (none yet: panel arrives in US-0002).
/// </summary>
internal sealed class TutorialSession(TutorialScript script)
{
    internal TutorialScript Script { get; } = script;
    internal TutorialDirector Director { get; } = new(script);

    /// <summary>Areas that capture input, in design coordinates.</summary>
    internal IEnumerable<SKRect> Bounds => [];

    internal void Observe(TutorialObservation observation) => Director.Observe(observation);

    internal void Draw(SKCanvas canvas, BattleLayout layout, float designWidth, long nowMs) { }

    internal bool OnMouseDown(SKPoint designPoint, MouseButton button) => false;

    internal bool OnKeyDown(Key key, KeyModifiers modifiers, bool repeat) => false;

    internal bool OnMouseWheel(SKPoint designPoint, float delta) => false;

    /// <summary>Releases drawing resources (portrait — US-0002).</summary>
    internal void Release() { }
}
