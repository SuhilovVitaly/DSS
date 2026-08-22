using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>
/// The nine-sliced BACK/PLAY-style button (first used by
/// <see cref="ScenarioSelect.ScenarioSelectScreen"/>'s action panel, meant for reuse on
/// any screen that wants this look) — draws <c>Images/UI/Buttons/button.png</c> via
/// <see cref="NinePatch"/> so one small texture covers any button size. There's only one
/// texture (no separate hover/disabled art): hover recolors the label instead of touching
/// the background (a highlighted background rect read as a plain gray box, not a button
/// state), and disabled dims the whole nine-sliced draw plus the label.
/// </summary>
public static class ImageButton
{
    private static readonly SKBitmap? Image = LoadImage("Images/UI/Buttons/button.png");

    /// <summary>
    /// Corner/edge-sample size (in Image source pixels) for NinePatch — measured extent of
    /// the button's rounded corner is ~11px; 18 leaves a safety margin while staying well
    /// under half of any reasonably sized button rect (e.g. half of a 44px-tall button).
    /// </summary>
    private const float CornerInset = 18f;

    /// <summary>True if the button PNG file was found and decoded at startup.</summary>
    internal static bool HasLoadedImage => Image is not null;

    private static SKBitmap? LoadImage(string path)
    {
        try { return File.Exists(path) ? SKBitmap.Decode(path) : null; }
        catch { return null; }
    }

    private static readonly SKPaint _textNormal = MakeTextPaint(MenuStyle.ColorText, MenuStyle.TypefaceBold, MenuStyle.ButtonFontSize);
    private static readonly SKPaint _textDisabled = MenuStyle.TextButtonDim;
    private static readonly SKPaint _textHover = MakeTextPaint(new SKColor(0xDA, 0x93, 0x07), MenuStyle.TypefaceBold, MenuStyle.ButtonFontSize);

    /// <summary>Text paints for a non-default typeface/size, built lazily and cached per (typeface, size).</summary>
    private static readonly Dictionary<(SKTypeface Typeface, float Size), (SKPaint Normal, SKPaint Hover, SKPaint Disabled)> _customTextPaints = new();

    private static SKPaint MakeTextPaint(SKColor color, SKTypeface typeface, float size) => new()
    {
        Color = color,
        TextSize = size,
        IsAntialias = true,
        TextAlign = SKTextAlign.Center,
        Typeface = typeface
    };

    private static (SKPaint Normal, SKPaint Hover, SKPaint Disabled) GetTextPaints(SKTypeface? typeface, float? fontSize)
    {
        if (typeface is null && fontSize is null)
            return (_textNormal, _textHover, _textDisabled);

        var resolvedTypeface = typeface ?? MenuStyle.TypefaceBold;
        var resolvedSize = fontSize ?? MenuStyle.ButtonFontSize;
        var key = (resolvedTypeface, resolvedSize);

        if (!_customTextPaints.TryGetValue(key, out var paints))
        {
            paints = (
                MakeTextPaint(MenuStyle.ColorText, resolvedTypeface, resolvedSize),
                MakeTextPaint(new SKColor(0xDA, 0x93, 0x07), resolvedTypeface, resolvedSize),
                MakeTextPaint(MenuStyle.ColorTextDim, resolvedTypeface, resolvedSize));
            _customTextPaints[key] = paints;
        }

        return paints;
    }

    /// <summary>Dims the whole nine-sliced draw for a disabled button (no separate disabled art).</summary>
    private static readonly SKPaint _disabledOverlay = new() { Color = new SKColor(255, 255, 255, 110) };

    /// <summary>
    /// Draws the button at <paramref name="rect"/> with <paramref name="text"/> centered
    /// on it. Falls back to <see cref="MenuStyle.DrawButton"/>'s flat style if the texture
    /// failed to load. Pass <paramref name="typeface"/> and/or <paramref name="fontSize"/>
    /// to override the default bold label font/size (e.g. <see cref="MenuStyle.TypefaceHumaroid"/>
    /// for the MainMenu buttons).
    /// </summary>
    public static void Draw(SKCanvas canvas, SKRect rect, string text, ButtonState state, SKTypeface? typeface = null, float? fontSize = null)
    {
        if (Image is null)
        {
            MenuStyle.DrawButton(canvas, rect, text, state);
            return;
        }

        var backgroundPaint = state == ButtonState.Disabled ? _disabledOverlay : null;
        NinePatch.Draw(canvas, Image, rect, CornerInset, backgroundPaint);

        var paints = GetTextPaints(typeface, fontSize);
        var textPaint = state switch
        {
            ButtonState.Disabled => paints.Disabled,
            ButtonState.Hovered => paints.Hover,
            _ => paints.Normal
        };
        canvas.DrawText(text, rect.MidX, MenuStyle.VerticalCenterBaseline(rect, textPaint), textPaint);
    }
}
