using Silk.NET.Input;
using SkiaSharp;

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
        using var paint = new SKPaint { IsAntialias = true, StrokeWidth = 3 * uiScale, Color = MenuStyle.ColorButtonBorder };
        canvas.DrawLine(Bounds.Left, Bounds.MidY, Bounds.Right, Bounds.MidY, paint);
        float fraction = Maximum == Minimum ? 0 : (float)((Value - Minimum) / (Maximum - Minimum));
        float x = Bounds.Left + fraction * Bounds.Width;
        paint.Color = Enabled ? XenonStyle.Cyan : XenonStyle.DisabledText;
        canvas.DrawLine(Bounds.Left, Bounds.MidY, x, Bounds.MidY, paint);
        canvas.DrawCircle(x, Bounds.MidY, 6 * uiScale, paint);
        if (HasFocus)
        {
            paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = uiScale;
            canvas.DrawRect(Bounds, paint);
        }
    }
}
