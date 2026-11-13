using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

internal static class TabletopStyle
{
    internal static readonly SKColor Background = new(0x11, 0x16, 0x1C);
    internal static readonly SKColor Steel = new(0x2C, 0x34, 0x3E);
    internal static readonly SKColor Inset = new(0x16, 0x1C, 0x23);
    internal static readonly SKColor Edge = new(0x05, 0x07, 0x0A);
    internal static readonly SKColor Ink = new(0xE4, 0xEC, 0xF4);
    internal static readonly SKColor Secondary = new(0x9A, 0xAA, 0xBB);
    internal static readonly SKColor Gold = new(0xFF, 0xD2, 0x4A);
    internal static readonly SKColor PrimaryInk = new(0x2A, 0x1A, 0x08);
    internal static readonly SKColor Danger = new(0xFF, 0x8A, 0x6A);
    internal static readonly SKColor Success = new(0xA8, 0xE0, 0x8A);

    private static readonly SKColor BevelLight = new(0x4C, 0x57, 0x64);
    private static readonly SKColor BevelDark = new(0x0A, 0x0E, 0x12);
    private static readonly SKColor Primary = new(0xE2, 0xBB, 0x62);
    private static readonly SKColor Disabled = new(0x25, 0x2B, 0x33);

    internal static void Panel(SKCanvas canvas, SKRect rect) => Draw(canvas, rect, Steel);

    internal static void Field(SKCanvas canvas, SKRect rect, bool focused = false, float scale = 1) =>
        Draw(canvas, rect, Inset, recessed: true, highlighted: focused, scale: scale);

    internal static void Button(SKCanvas canvas, SKRect rect, ButtonState state, bool primary = false)
    {
        SKColor fill = state switch
        {
            ButtonState.Disabled => Disabled,
            ButtonState.Pressed => primary ? new(0xC7, 0x9A, 0x3C) : Inset,
            ButtonState.Hovered => primary ? new(0xF3, 0xD8, 0x8A) : new(0x3A, 0x44, 0x50),
            _ => primary ? Primary : new(0x33, 0x3D, 0x49)
        };
        Draw(canvas, rect, fill, recessed: state == ButtonState.Pressed, highlighted: state == ButtonState.Hovered);
    }

    internal static SKColor ButtonInk(ButtonState state, bool primary = false) =>
        state == ButtonState.Disabled ? Secondary : primary ? PrimaryInk : Ink;

    internal static void Draw(SKCanvas canvas, SKRect rect, SKColor fill,
        bool recessed = false, bool highlighted = false, float scale = 1)
    {
        if (rect.IsEmpty || scale <= 0)
            return;

        using var paint = new SKPaint { Color = highlighted ? Gold : Edge };
        canvas.DrawRect(rect, paint);

        float line = Math.Min(scale, Math.Min(rect.Width, rect.Height) / 4f);
        rect.Inflate(-line, -line);
        paint.Color = recessed ? BevelDark : BevelLight;
        canvas.DrawRect(rect, paint);
        paint.Color = recessed ? BevelLight : BevelDark;
        canvas.DrawRect(rect.Left, rect.Bottom - line, rect.Width, line, paint);
        canvas.DrawRect(rect.Right - line, rect.Top, line, rect.Height, paint);
        rect.Inflate(-line, -line);
        paint.Color = fill;
        canvas.DrawRect(rect, paint);
    }

    internal static void Label(SKCanvas canvas, SKRect rect, string text, SKPaint paint)
    {
        if (rect.IsEmpty)
            return;

        string fitted = ControlText.Ellipsize(text, rect.Width, paint);
        ControlVisuals.DrawLabel(canvas, fitted, rect, paint, center: true);
    }
}
