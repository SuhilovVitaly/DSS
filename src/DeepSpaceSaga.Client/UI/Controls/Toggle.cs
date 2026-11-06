using Silk.NET.Input;
using SkiaSharp;
using SkinStyle = DeepSpaceSaga.Client.UI.Screens.TacticalBattle.TacticalBattleSkinRenderScope;

namespace DeepSpaceSaga.Client.UI.Controls;

public sealed class Toggle(string label, bool value = false)
{
    public string Label { get; set; } = label;
    public bool Value { get; private set; } = value;
    public SKRect Bounds { get; set; }
    public bool Enabled { get; set; } = true;
    public bool HasFocus { get; set; }
    public event Action<bool>? Changed;
    public bool SetValue(bool value, bool notify = false)
    {
        if (Value == value) return false;
        Value = value;
        if (notify) Changed?.Invoke(value);
        return true;
    }
    public bool OnMouseDown(float x, float y, MouseButton button)
    {
        if (!Enabled || button != MouseButton.Left || !Bounds.Contains(x, y)) return false;
        HasFocus = true;
        SetValue(!Value, notify: true);
        return true;
    }
    public bool OnKeyDown(Key key, bool repeat = false)
    {
        if (!Enabled || !HasFocus || key is not (Key.Space or Key.Enter or Key.KeypadEnter)) return false;
        if (!repeat) SetValue(!Value, notify: true);
        return true;
    }
    public void Draw(SKCanvas canvas, float uiScale = 1)
    {
        if (Bounds.IsEmpty || uiScale <= 0) return;
        float size = Math.Min(Bounds.Height, 18 * uiScale);
        var box = SKRect.Create(Bounds.Left, Bounds.MidY - size / 2, size, size);
        string state = Value ? "checked" : "unchecked";
        if (!Enabled) state += ".disabled";
        else if (HasFocus) state += ".focused";
        var role = SkinStyle.Control("control.toggle", state);
        ControlVisuals.Frame(canvas, box, HasFocus, Enabled, uiScale, role);
        if (Value)
        {
            var marker = SkinStyle.Control("control.toggle", $"marker.{state}");
            float inset = size * 0.15f;
            var imageBounds = new SKRect(box.Left + inset, box.Top + inset, box.Right - inset, box.Bottom - inset);
            if (!SkinStyle.Image(canvas, marker, imageBounds))
            {
                using var paint = new SKPaint { IsAntialias = true };
                using var dash = SkinStyle.Stroke(paint, marker, Enabled ? XenonStyle.Cyan : XenonStyle.DisabledText, 2 * uiScale, uiScale);
                canvas.DrawLine(box.Left + size * 0.2f, box.MidY, box.Left + size * 0.45f, box.Bottom - size * 0.2f, paint);
                canvas.DrawLine(box.Left + size * 0.45f, box.Bottom - size * 0.2f, box.Right - size * 0.15f, box.Top + size * 0.2f, paint);
            }
        }
        ControlVisuals.Label(canvas, Label, new(box.Right + 8 * uiScale, Bounds.Top, Bounds.Right, Bounds.Bottom), Enabled, uiScale, role: role);
    }
}
