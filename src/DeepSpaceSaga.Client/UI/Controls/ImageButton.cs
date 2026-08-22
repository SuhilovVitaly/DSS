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

    private static readonly SKPaint _textNormal = MakeTextPaint(MenuStyle.ColorText);
    private static readonly SKPaint _textDisabled = MenuStyle.TextButtonDim;
    private static readonly SKPaint _textHover = MakeTextPaint(new SKColor(0xDA, 0x93, 0x07));

    private static SKPaint MakeTextPaint(SKColor color) => new()
    {
        Color = color,
        TextSize = MenuStyle.ButtonFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Center,
        Typeface = MenuStyle.TypefaceBold
    };

    /// <summary>Dims the whole nine-sliced draw for a disabled button (no separate disabled art).</summary>
    private static readonly SKPaint _disabledOverlay = new() { Color = new SKColor(255, 255, 255, 110) };

    /// <summary>
    /// Draws the button at <paramref name="rect"/> with <paramref name="text"/> centered
    /// on it. Falls back to <see cref="MenuStyle.DrawButton"/>'s flat style if the texture
    /// failed to load.
    /// </summary>
    public static void Draw(SKCanvas canvas, SKRect rect, string text, ButtonState state)
    {
        if (Image is null)
        {
            MenuStyle.DrawButton(canvas, rect, text, state);
            return;
        }

        var backgroundPaint = state == ButtonState.Disabled ? _disabledOverlay : null;
        NinePatch.Draw(canvas, Image, rect, CornerInset, backgroundPaint);

        var textPaint = state switch
        {
            ButtonState.Disabled => _textDisabled,
            ButtonState.Hovered => _textHover,
            _ => _textNormal
        };
        canvas.DrawText(text, rect.MidX, MenuStyle.VerticalCenterBaseline(rect, textPaint), textPaint);
    }
}
