using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>Palette, fonts, paints, and slice metrics for the Xenon-inspired DSS theme.</summary>
public static class XenonStyle
{
    public const float WindowSliceInset = 28f;
    public const float ButtonSliceInset = 12f;
    public const byte DisabledAlpha = 97;

    public const float TitleFontSize = 23f;
    public const float ButtonFontSize = 18f;
    public const float FooterFontSize = 13f;

    public static readonly SKColor Cyan = new(54, 194, 232);
    public static readonly SKColor CyanBright = new(166, 238, 255);
    public static readonly SKColor CyanDim = new(35, 104, 132);
    public static readonly SKColor DisabledText = new(91, 119, 135);

    public static readonly SKTypeface TypefaceRegular =
        LoadTypeface("UI/Fonts/FiraSans-Regular.ttf", SKFontStyleWeight.Normal);
    public static readonly SKTypeface TypefaceSemibold =
        LoadTypeface("UI/Fonts/FiraSans-SemiBold.ttf", SKFontStyleWeight.SemiBold);

    public static readonly SKPaint HeaderFallbackFill = MakePaint(new SKColor(3, 24, 35, 150));
    public static readonly SKPaint ThinCyanStroke = MakePaint(Cyan, SKPaintStyle.Stroke, 1f);
    public static readonly SKPaint BrightCyanStroke = MakePaint(CyanBright, SKPaintStyle.Stroke, 2f);
    public static readonly SKPaint DimCyanStroke = MakePaint(CyanDim, SKPaintStyle.Stroke, 1f);
    public static readonly SKPaint DisabledImagePaint = MakePaint(new SKColor(255, 255, 255, DisabledAlpha));
    public static readonly SKPaint PressedImagePaint = MakePaint(new SKColor(220, 235, 240, 205));

    public static readonly SKPaint TitleText = MakeText(TitleFontSize, CyanBright, TypefaceSemibold);
    public static readonly SKPaint ButtonText = MakeText(ButtonFontSize, Cyan, TypefaceSemibold);
    public static readonly SKPaint ButtonTextHover = MakeText(ButtonFontSize, SKColors.White, TypefaceSemibold);
    public static readonly SKPaint ButtonTextPressed = MakeText(ButtonFontSize, new SKColor(204, 232, 239), TypefaceSemibold);
    public static readonly SKPaint ButtonTextDisabled = MakeText(ButtonFontSize, DisabledText, TypefaceSemibold);
    public static readonly SKPaint FooterLeftText = MakeText(FooterFontSize, Cyan, TypefaceRegular, SKTextAlign.Left);

    /// <summary>Forces optional typeface and paint initialization before the render loop.</summary>
    public static void Preload() { }

    private static SKTypeface LoadTypeface(string relativePath, SKFontStyleWeight fallbackWeight)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory,
                relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path))
                return SKTypeface.FromFile(path) ?? FallbackTypeface(fallbackWeight);
        }
        catch
        {
            // Optional typeface: use the Cyrillic-capable system fallback below.
        }

        return FallbackTypeface(fallbackWeight);
    }

    private static SKTypeface FallbackTypeface(SKFontStyleWeight weight) =>
        SKTypeface.FromFamilyName("Verdana", weight, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)
        ?? SKTypeface.Default;

    private static SKPaint MakePaint(SKColor color, SKPaintStyle style = SKPaintStyle.Fill, float width = 1f) =>
        new() { Color = color, Style = style, StrokeWidth = width, IsAntialias = true };

    private static SKPaint MakeText(float size, SKColor color, SKTypeface typeface,
        SKTextAlign align = SKTextAlign.Center) => new()
    {
        Color = color,
        TextSize = size,
        IsAntialias = true,
        TextAlign = align,
        Typeface = typeface
    };
}
