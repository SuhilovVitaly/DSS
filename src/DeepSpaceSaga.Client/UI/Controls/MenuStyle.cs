using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>
/// Reusable menu visual style — panel, button, and text paints matching the reference design.
/// Use in any menu screen (MainMenu, PauseMenu, Settings, Load Game, etc.) for consistent visuals.
/// </summary>
public enum ButtonState
{
    Normal,
    Hovered,
    Pressed,
    Disabled
}

public static class MenuStyle
{
    // --- Fonts (Verdana, matching reference) ---
    public static readonly SKTypeface TypefaceRegular =
        SKTypeface.FromFamilyName("Verdana", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        ?? SKTypeface.Default;

    public static readonly SKTypeface TypefaceBold =
        SKTypeface.FromFamilyName("Verdana", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        ?? SKTypeface.Default;

    // --- Colors (exact from reference) ---
    public static readonly SKColor ColorText = new(220, 220, 220);
    public static readonly SKColor ColorTextDim = new(90, 90, 90);
    public static readonly SKColor ColorPanelBg = new(0, 0, 0);
    public static readonly SKColor ColorPanelBorder = new(42, 42, 42);
    public static readonly SKColor ColorButtonBg = new(18, 18, 18);
    public static readonly SKColor ColorButtonBorder = new(42, 42, 42);
    public static readonly SKColor ColorButtonHover = new(58, 58, 58);
    public static readonly SKColor ColorButtonPressed = new(78, 78, 78);
    public static readonly SKColor ColorBackground = SKColors.Black;

    // --- Dimensions (reference: 188×58) ---
    public const float ButtonWidth = 188f;
    public const float ButtonHeight = 58f;
    public const float ButtonCornerRadius = 0f;
    public const float ButtonFontSize = 14f;      // 10.8pt ≈ 14px
    public const float TitleFontSize = 32f;       // 24pt ≈ 32px
    public const float VersionFontSize = 13f;     // 10pt ≈ 13px
    public const float StatusFontSize = 11f;      // 8pt ≈ 11px

    public const float PanelBorderWidth = 2f;
    public const float ButtonBorderWidth = 1f;

    // --- Pre-built paints (created once, reused across frames) ---

    public static SKPaint PanelFill { get; } = new() { Color = ColorPanelBg, Style = SKPaintStyle.Fill };
    public static SKPaint PanelBorder { get; } = new() { Color = ColorPanelBorder, Style = SKPaintStyle.Stroke, StrokeWidth = PanelBorderWidth };
    public static SKPaint BackgroundFill { get; } = new() { Color = ColorBackground, Style = SKPaintStyle.Fill };

    public static SKPaint ButtonFillNormal { get; } = new() { Color = ColorButtonBg, Style = SKPaintStyle.Fill };
    public static SKPaint ButtonFillHover { get; } = new() { Color = ColorButtonHover, Style = SKPaintStyle.Fill };
    public static SKPaint ButtonFillPressed { get; } = new() { Color = ColorButtonPressed, Style = SKPaintStyle.Fill };
    public static SKPaint ButtonBorder { get; } = new() { Color = ColorButtonBorder, Style = SKPaintStyle.Stroke, StrokeWidth = ButtonBorderWidth };

    public static SKPaint TextTitle { get; } = MakeText(TitleFontSize, bold: true);
    public static SKPaint TextVersion { get; } = MakeText(VersionFontSize, bold: false);
    public static SKPaint TextButton { get; } = MakeText(ButtonFontSize, bold: true);
    public static SKPaint TextButtonDim { get; } = MakeText(ButtonFontSize, bold: true, dim: true);
    public static SKPaint TextStatus { get; } = MakeText(StatusFontSize, bold: false);

    // --- Methods ---

    /// <summary>Draw a menu-style button at the given rectangle.</summary>
    public static void DrawButton(SKCanvas canvas, SKRect rect, string text, ButtonState state)
    {
        float cr = ButtonCornerRadius;

        var fill = state switch
        {
            ButtonState.Hovered => ButtonFillHover,
            ButtonState.Pressed => ButtonFillPressed,
            _ => ButtonFillNormal
        };
        canvas.DrawRoundRect(rect, cr, cr, fill);

        canvas.DrawRoundRect(rect, cr, cr, ButtonBorder);

        var textPaint = state == ButtonState.Disabled ? TextButtonDim : TextButton;
        float textY = rect.MidY + textPaint.TextSize / 3f;
        canvas.DrawText(text, rect.MidX, textY, textPaint);
    }

    /// <summary>Draw a centered menu panel with border at the given rect.</summary>
    public static void DrawPanel(SKCanvas canvas, SKRect rect)
    {
        canvas.DrawRect(rect, PanelFill);
        canvas.DrawRect(rect, PanelBorder);
    }

    /// <summary>Draw fullscreen black background.</summary>
    public static void DrawBackground(SKCanvas canvas, int width, int height)
    {
        canvas.DrawRect(0, 0, width, height, BackgroundFill);
    }

    private static SKPaint MakeText(float size, bool bold, bool dim = false)
    {
        return new SKPaint
        {
            Color = dim ? ColorTextDim : ColorText,
            TextSize = size,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = bold ? TypefaceBold : TypefaceRegular
        };
    }
}
