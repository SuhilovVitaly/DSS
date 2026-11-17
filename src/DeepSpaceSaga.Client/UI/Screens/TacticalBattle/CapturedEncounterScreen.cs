using DeepSpaceSaga.Client.UI.Controls;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

/// <summary>Explicit acknowledgement of an authoritative capture, without editable battle settings.</summary>
public sealed class CapturedEncounterScreen(TacticalBattleFlow flow, float uiScale = 1)
    : TacticalBattleModalScreen(flow, true, uiScale)
{
    private readonly ScrollTextPanel _text = new() { AutoScroll = false };
    private readonly ShortcutButton _continue = new(Localization.Get("Demo.Encounter.Continue"), "Enter");
    internal override bool CanStart => !Flow.Pending && Flow.CampaignEncounter is not null;
    public override void Render(SKCanvas canvas, int width, int height)
    {
        canvas.Clear(new SKColor(2, 12, 22));
        float s = UiScale;
        ControlVisuals.Label(canvas, Localization.Get("Demo.Encounter.Title"), new(24 * s, 12 * s, width - 24 * s, 55 * s), true, s);
        _text.Layout(new(24 * s, 72 * s, width - 24 * s, Math.Max(100 * s, height - 90 * s)), s);
        _text.SetText(Localization.Get("Demo.Encounter.Warning") + "\n\n" + Flow.Error);
        _text.Draw(canvas);
        _continue.Bounds = new(24 * s, height - 64 * s, Math.Min(width - 24 * s, 390 * s), height - 20 * s);
        _continue.Label = Localization.Get(Flow.Pending ? "Demo.Encounter.Pending" : "Demo.Encounter.Continue");
        _continue.Enabled = CanStart;
        _continue.Draw(canvas, s);
    }
    public override ScreenEvent OnMouseDown(float x, float y, MouseButton button) =>
        CanStart && _continue.OnMouseDown(x, y, button) ? ScreenEvent.StartTacticalBattle : ScreenEvent.None;
    public override bool OnMouseMove(float x, float y) => _continue.OnMouseMove(x, y) | _text.OnMouseMove(x, y);
    public override ScreenEvent OnMouseWheel(float x, float y, float delta)
    { _text.OnMouseWheel(x, y, delta); return ScreenEvent.None; }
    public override ScreenEvent OnKeyDown(Key key, KeyModifiers modifiers, bool repeat) =>
        !repeat && CanStart && key is Key.Enter or Key.KeypadEnter ? ScreenEvent.StartTacticalBattle : ScreenEvent.None;
    public override void OnInputReset() { _continue.ResetInput(); _text.ResetInput(); }
}
