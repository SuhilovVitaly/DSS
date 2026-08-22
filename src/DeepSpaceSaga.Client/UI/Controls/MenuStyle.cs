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
    // TEMP DIAG — startup timing investigation, remove once resolved. Must be the
    // first field so it's initialized before anything it measures below.
    private static readonly System.Diagnostics.Stopwatch DiagStopwatch = System.Diagnostics.Stopwatch.StartNew();

    // --- Fonts (Verdana, matching reference) ---
    public static readonly SKTypeface TypefaceRegular = LoadTypefaceDiag("Regular", SKFontStyleWeight.Normal);
    public static readonly SKTypeface TypefaceBold = LoadTypefaceDiag("Bold", SKFontStyleWeight.Bold);

    // TEMP DIAG — startup timing investigation, remove once resolved.
    private static SKTypeface LoadTypefaceDiag(string label, SKFontStyleWeight weight)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var typeface =
            SKTypeface.FromFamilyName("Verdana", weight, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
            ?? SKTypeface.Default;
        DeepSpaceSaga.Client.UI.InterfaceLog.Write(
            $"STARTUP DIAG: MenuStyle SKTypeface.FromFamilyName(Verdana {label}) took {sw.ElapsedMilliseconds} ms");
        return typeface;
    }

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
    public static readonly SKColor ColorDimOverlay = new(0, 0, 0, 160);

    // --- Font sizes (visual property, not layout geometry) ---
    public const float ButtonFontSize = 14f;      // 10.8pt Bold
    public const float TitleFontSize = 32f;       // 24pt Bold
    public const float VersionFontSize = 13f;     // 10pt Regular
    public const float StatusFontSize = 11f;      // 8pt Regular

    // --- Border widths (visual property) ---
    public const float PanelBorderWidth = 2f;
    public const float ButtonBorderWidth = 1f;

    // --- Pre-built paints (created once, reused across frames) ---

    public static SKPaint PanelFill { get; } = new() { Color = ColorPanelBg, Style = SKPaintStyle.Fill };
    public static SKPaint PanelBorder { get; } = new() { Color = ColorPanelBorder, Style = SKPaintStyle.Stroke, StrokeWidth = PanelBorderWidth };
    public static SKPaint BackgroundFill { get; } = new() { Color = ColorBackground, Style = SKPaintStyle.Fill };
    public static SKPaint DimOverlayFill { get; } = new() { Color = ColorDimOverlay, Style = SKPaintStyle.Fill };

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
        const float cr = 0f; // sharp corners, matching FlatStyle.Flat

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

    /// <summary>
    /// Exact baseline Y that vertically centers text in <paramref name="rect"/> using the
    /// paint's real font metrics (Ascent/Descent) — more precise than a TextSize/3 rule of
    /// thumb, which matters for a shallow rect (e.g. a title bar).
    /// </summary>
    public static float VerticalCenterBaseline(SKRect rect, SKPaint paint)
    {
        var metrics = paint.FontMetrics;
        return rect.MidY - (metrics.Ascent + metrics.Descent) / 2f;
    }

    /// <summary>Draw fullscreen black background.</summary>
    public static void DrawBackground(SKCanvas canvas, int width, int height)
    {
        canvas.DrawRect(0, 0, width, height, BackgroundFill);
    }

    /// <summary>
    /// Draw the fullscreen dim overlay used behind a modal panel. Drawn exactly once
    /// by SkiaWindow.OnRender between the interactive root screen and the bottom-most
    /// overlay — individual overlay screens (Station, Trade, GameMenu, Settings, ...)
    /// must not draw their own copy, or stacking overlays (e.g. Trade opened from
    /// Station) would compound the dimming and look darker than a single overlay.
    /// </summary>
    public static void DrawDimOverlay(SKCanvas canvas, int width, int height)
    {
        canvas.DrawRect(0, 0, width, height, DimOverlayFill);
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

    // TEMP DIAG — startup timing investigation, remove once resolved. Runs after
    // every static field above has been initialized (fonts + all SKPaint objects).
    static MenuStyle()
    {
        DeepSpaceSaga.Client.UI.InterfaceLog.Write(
            $"STARTUP DIAG: MenuStyle static init complete (fonts+paints) — {DiagStopwatch.ElapsedMilliseconds} ms total");
    }
}
