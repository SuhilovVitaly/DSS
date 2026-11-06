using Silk.NET.Input;
using SkiaSharp;
using SkinStyle = DeepSpaceSaga.Client.UI.Screens.TacticalBattle.TacticalBattleSkinRenderScope;

namespace DeepSpaceSaga.Client.UI.Controls;

public sealed class Slider
{
    public double Minimum { get; private set; }
    public double Maximum { get; private set; }
    public double Step { get; }
    public double Value { get; private set; }
    public SKRect Bounds { get; set; }
    public bool Enabled { get; set; } = true;
    public bool HasFocus { get; set; }
    public bool IsDragging { get; private set; }
    public event Action<double>? Changed;

    public Slider(double minimum, double maximum, double value, double step = 0.5)
    {
        if (!double.IsFinite(step) || step <= 0) throw new ArgumentOutOfRangeException(nameof(step));
        Step = step;
        SetRange(minimum, maximum);
        SetValue(value);
    }
    public void SetRange(double minimum, double maximum)
    {
        if (!double.IsFinite(minimum) || !double.IsFinite(maximum) || minimum > maximum)
            throw new ArgumentOutOfRangeException(nameof(minimum));
        Minimum = minimum; Maximum = maximum;
        Value = Math.Clamp(Value, minimum, maximum);
    }
    /// <summary>Programmatic suggestions retain full precision. Only manual gestures request snapping.</summary>
    public bool SetValue(double value, bool notify = false, bool snap = false)
    {
        if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
        value = Math.Clamp(value, Minimum, Maximum);
        if (snap && value != Minimum && value != Maximum)
            value = Math.Clamp(Minimum + Math.Round((value - Minimum) / Step, MidpointRounding.ToEven) * Step, Minimum, Maximum);
        if (value == Value) return false;
        Value = value;
        if (notify) Changed?.Invoke(value);
        return true;
    }
    public bool OnMouseDown(float x, float y, MouseButton button)
    {
        if (!Enabled || button != MouseButton.Left || !Bounds.Contains(x, y)) return false;
        HasFocus = true; IsDragging = true;
        FromPointer(x);
        return true;
    }
    public bool OnMouseMove(float x, float y)
    {
        if (!Enabled) { IsDragging = false; return false; }
        if (IsDragging && float.IsFinite(x)) FromPointer(x);
        return IsDragging || Bounds.Contains(x, y);
    }
    public void OnMouseUp(MouseButton button) { if (button == MouseButton.Left) IsDragging = false; }
    public void ResetInput() => IsDragging = false;
    private void FromPointer(float x)
    {
        if (Bounds.Width <= 0 || !float.IsFinite(x)) return;
        double fraction = Math.Clamp((x - Bounds.Left) / (double)Bounds.Width, 0, 1);
        SetValue(Minimum + (Maximum - Minimum) * fraction, notify: true, snap: true);
    }
    public bool OnKeyDown(Key key)
    {
        if (!Enabled || !HasFocus) return false;
        double next;
        switch (key)
        {
            case Key.Left: case Key.Down: next = Value - Step; break;
            case Key.Right: case Key.Up: next = Value + Step; break;
            case Key.Home: next = Minimum; break;
            case Key.End: next = Maximum; break;
            default: return false;
        }
        SetValue(next, notify: true, snap: true);
        return true;
    }
    public void Draw(SKCanvas canvas, float uiScale = 1)
    {
        if (Bounds.IsEmpty || uiScale <= 0) return;
        using var paint = new SKPaint { IsAntialias = true };
        using (SkinStyle.Stroke(paint, SkinStyle.Control("control.slider", "track"), MenuStyle.ColorButtonBorder, 3 * uiScale, uiScale))
            canvas.DrawLine(Bounds.Left, Bounds.MidY, Bounds.Right, Bounds.MidY, paint);
        float fraction = Maximum == Minimum ? 0 : (float)((Value - Minimum) / (Maximum - Minimum));
        float x = Bounds.Left + fraction * Bounds.Width;
        string state = !Enabled ? "disabled" : IsDragging ? "active" : HasFocus ? "focused" : "normal";
        var color = Enabled ? XenonStyle.Cyan : XenonStyle.DisabledText;
        using (SkinStyle.Stroke(paint, SkinStyle.Control("control.slider", $"track.value.{state}"), color, 3 * uiScale, uiScale))
            canvas.DrawLine(Bounds.Left, Bounds.MidY, x, Bounds.MidY, paint);
        paint.PathEffect = null;
        var thumb = SkinStyle.Control("control.slider", $"thumb.{state}");
        float radius = Math.Clamp(SkinStyle.Number(thumb, "size", 12) / 2, 1, 10) * uiScale;
        if (!SkinStyle.Image(canvas, thumb, SKRect.Create(x - radius, Bounds.MidY - radius, radius * 2, radius * 2)))
        {
            paint.Color = SkinStyle.Color(thumb, "fill", color);
            canvas.DrawCircle(x, Bounds.MidY, radius, paint);
        }
        if (HasFocus)
        {
            paint.Style = SKPaintStyle.Stroke;
            using var stroke = SkinStyle.Stroke(paint, SkinStyle.Control("control.slider", "focus"), color, uiScale, uiScale);
            canvas.DrawRect(Bounds, paint);
        }
    }
}
