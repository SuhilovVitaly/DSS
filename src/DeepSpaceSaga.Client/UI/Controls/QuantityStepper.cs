using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>Which zone of a <see cref="QuantityStepper"/> a hit-test point falls in.</summary>
public enum QuantityStepperButton { None, Minus, Plus }

/// <summary>
/// [-] [ value ] [+] quantity control (Trade screen's Buy/Sell quantity picker). Stateless —
/// the owning screen holds the current value and min/max, this only draws and hit-tests.
/// Minus/plus buttons reuse Generic Type A's compact marker-free look; the value box mirrors
/// SettingsScreen's flat dark combo-box style (see SettingsScreen.DrawComboBox).
/// </summary>
public static class QuantityStepper
{
    public const float ButtonWidth = 44f;
    public const float Height = 44f;

    /// <summary>Value box background — same flat dark fill as SettingsScreen's combo-box.</summary>
    private static readonly SKPaint _valueBoxFill = new() { Color = new SKColor(0x2D, 0x2D, 0x2D), Style = SKPaintStyle.Fill };

    /// <summary>Value box border — same as SettingsScreen's combo-box border.</summary>
    private static readonly SKPaint _valueBoxBorder = new() { Color = new SKColor(0x50, 0x50, 0x50), Style = SKPaintStyle.Stroke, StrokeWidth = 1f };

    private static readonly SKPaint _valueText = new()
    {
        Color = MenuStyle.ColorText,
        TextSize = MenuStyle.ButtonFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Center,
        Typeface = MenuStyle.TypefaceHumaroid
    };

    public static SKRect MinusButtonRect(SKRect rect) =>
        new(rect.Left, rect.Top, rect.Left + ButtonWidth, rect.Top + Height);

    public static SKRect PlusButtonRect(SKRect rect) =>
        new(rect.Right - ButtonWidth, rect.Top, rect.Right, rect.Top + Height);

    public static SKRect ValueBoxRect(SKRect rect) =>
        new(MinusButtonRect(rect).Right, rect.Top, PlusButtonRect(rect).Left, rect.Top + Height);

    public static QuantityStepperButton HitTest(SKRect rect, float x, float y)
    {
        if (MinusButtonRect(rect).Contains(x, y)) return QuantityStepperButton.Minus;
        if (PlusButtonRect(rect).Contains(x, y)) return QuantityStepperButton.Plus;
        return QuantityStepperButton.None;
    }

    /// <summary>
    /// Draws the minus button, value box, and plus button filling <paramref name="rect"/>.
    /// Clamping <paramref name="value"/> against min/max is the owning screen's job — this
    /// only renders whatever value it's given, including negative/zero.
    /// </summary>
    public static void Draw(
        SKCanvas canvas, SKRect rect, long value,
        QuantityStepperButton hovered = QuantityStepperButton.None,
        bool canDecrement = true, bool canIncrement = true)
    {
        var minusState = !canDecrement
            ? ButtonState.Disabled
            : hovered == QuantityStepperButton.Minus ? ButtonState.Hovered : ButtonState.Normal;
        var plusState = !canIncrement
            ? ButtonState.Disabled
            : hovered == QuantityStepperButton.Plus ? ButtonState.Hovered : ButtonState.Normal;

        GenericButtonTypeA.Draw(canvas, MinusButtonRect(rect), "−", minusState, showMarker: false);
        GenericButtonTypeA.Draw(canvas, PlusButtonRect(rect), "+", plusState, showMarker: false);

        var valueBoxRect = ValueBoxRect(rect);
        canvas.DrawRect(valueBoxRect, _valueBoxFill);
        canvas.DrawRect(valueBoxRect, _valueBoxBorder);
        canvas.DrawText(value.ToString(), valueBoxRect.MidX, MenuStyle.VerticalCenterBaseline(valueBoxRect, _valueText), _valueText);
    }
}
