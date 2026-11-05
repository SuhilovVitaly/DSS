using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

public sealed class ShortcutButton(string label, string keyHint = "", string tooltip = "")
{
    private long _hoverAt;
    private SKPoint _pointer;
    public string Label { get; set; } = label;
    public string KeyHint { get; set; } = keyHint;
    public string Tooltip { get; set; } = tooltip;
    public SKRect Bounds { get; set; }
    public bool Enabled { get; set; } = true;
    public bool HasFocus { get; set; }
    public bool Hovered { get; private set; }
    public event Action? Clicked;
    public bool OnMouseMove(float x, float y, long? nowMs = null)
    {
        bool hover = Bounds.Contains(x, y);
        if (hover && !Hovered) _hoverAt = nowMs ?? Environment.TickCount64;
        Hovered = hover; _pointer = new(x, y);
        return hover;
    }
    public void ResetInput() => Hovered = false;
    public bool OnMouseDown(float x, float y, MouseButton button)
    {
        if (!Enabled || button != MouseButton.Left || !Bounds.Contains(x, y)) return false;
        HasFocus = true; Clicked?.Invoke(); return true;
    }
    public bool OnKeyDown(Key key, bool repeat = false)
    {
        if (!Enabled || !HasFocus || key is not (Key.Enter or Key.KeypadEnter or Key.Space)) return false;
        if (!repeat) Clicked?.Invoke();
        return true;
    }
    public void Draw(SKCanvas canvas, float uiScale = 1)
    {
        if (Bounds.IsEmpty || uiScale <= 0) return;
        canvas.Save(); canvas.Translate(Bounds.Left, Bounds.Top); canvas.Scale(uiScale);
        GenericButtonTypeA.Draw(canvas, SKRect.Create(0, 0, Bounds.Width / uiScale, Bounds.Height / uiScale),
            ControlText.SafeSymbols(Label + (KeyHint.Length == 0 ? "" : " [" + KeyHint + "]")), !Enabled ? ButtonState.Disabled : Hovered || HasFocus ? ButtonState.Hovered : ButtonState.Normal);
        canvas.Restore();
    }
    public bool TooltipReady(long nowMs) => Hovered && Tooltip.Length > 0 && nowMs - _hoverAt >= MenuStyle.TooltipHoverDelaySeconds * 1000;
    public void DrawTooltip(SKCanvas canvas, SKRect viewport, float uiScale = 1, long? nowMs = null)
    {
        if (TooltipReady(nowMs ?? Environment.TickCount64)) ControlText.Tooltip(canvas, Tooltip, _pointer, viewport, uiScale);
    }
}
