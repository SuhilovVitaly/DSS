using DeepSpaceSaga.Client.UI.Assets;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>
/// Generic Type A window shell. Draws only the scalable background and cyan frame with
/// clipped corners; titles, buttons, dividers, footers, and all interaction belong to
/// the consuming screen.
/// </summary>
public static class GenericWindowTypeA
{
    internal const string ShellPath = "Images/UI/Themes/Xenon/Window/window-shell.png";

    /// <summary>
    /// Distance from the window's top edge to the typographic top of every Type A title.
    /// The baseline itself depends on the selected font, so it must not be shared directly.
    /// </summary>
    public const float TitleTopInset = 34f;
    public const float VersionBaselineInset = 102f;
    public const float VersionFontSize = 15f;

    private static readonly SKPaint VersionTextPaint = new()
    {
        Color = MenuStyle.ColorText,
        TextSize = VersionFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Center,
        Typeface = MenuStyle.TypefaceHumaroid
    };

    private static readonly SKBitmap? Shell = UiAssetLoader.LoadBitmap(ShellPath);
    private static readonly SKPaint OpaqueBackgroundFill = new()
    {
        Color = new SKColor(2, 12, 22),
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };

    internal static bool HasAssets => Shell is not null;

    /// <summary>Forces bitmap decoding before the render loop.</summary>
    public static void Preload() { }

    /// <summary>Draws a Type A shell into any positive-size destination rectangle.</summary>
    public static void Draw(SKCanvas canvas, SKRect bounds)
    {
        if (Shell is not null)
        {
            NinePatch.Draw(canvas, Shell, bounds, XenonStyle.WindowSliceInset);
            return;
        }

        MenuStyle.DrawPanel(canvas, bounds);
    }

    /// <summary>
    /// Draws an opaque clipped-corner background before the ordinary Type A shell.
    /// Use for modal windows whose underlying screen must not show through the shell's
    /// intentionally translucent center texture.
    /// </summary>
    public static void DrawOpaque(SKCanvas canvas, SKRect bounds)
    {
        float corner = Math.Min(20f, Math.Min(bounds.Width, bounds.Height) / 2f);
        using var path = new SKPath();
        path.MoveTo(bounds.Left + corner, bounds.Top);
        path.LineTo(bounds.Right - corner, bounds.Top);
        path.LineTo(bounds.Right, bounds.Top + corner);
        path.LineTo(bounds.Right, bounds.Bottom - corner);
        path.LineTo(bounds.Right - corner, bounds.Bottom);
        path.LineTo(bounds.Left + corner, bounds.Bottom);
        path.LineTo(bounds.Left, bounds.Bottom - corner);
        path.LineTo(bounds.Left, bounds.Top + corner);
        path.Close();
        canvas.DrawPath(path, OpaqueBackgroundFill);
        Draw(canvas, bounds);
    }

    /// <summary>
    /// Returns the common Type A title anchor: horizontally centered in the window with
    /// its font ascent starting at <see cref="TitleTopInset"/> from the top edge.
    /// </summary>
    public static SKPoint TitlePosition(SKRect bounds, SKPaint paint)
    {
        var metrics = paint.FontMetrics;
        return new SKPoint(bounds.MidX, bounds.Top + TitleTopInset - metrics.Ascent);
    }

    /// <summary>Draws a title at the shared Type A title position.</summary>
    public static void DrawTitle(SKCanvas canvas, SKRect bounds, string title, SKPaint paint)
    {
        var position = TitlePosition(bounds, paint);
        canvas.DrawText(title, position.X, position.Y, paint);
    }

    /// <summary>Draws the shared Type A version label below the title.</summary>
    public static void DrawVersion(SKCanvas canvas, SKRect bounds, string version) =>
        canvas.DrawText(version, bounds.MidX, bounds.Top + VersionBaselineInset, VersionTextPaint);
}
