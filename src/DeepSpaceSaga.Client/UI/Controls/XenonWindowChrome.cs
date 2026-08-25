using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>Reusable Xenon-style window frame. Interactive rectangles stay owned by the screen layout.</summary>
public static class XenonWindowChrome
{
    internal static bool HasChromeAssets => GenericWindowTypeA.HasAssets;

    /// <summary>Forces bitmap decoding before the render loop.</summary>
    public static void Preload() => GenericWindowTypeA.Preload();

    public static void Draw(SKCanvas canvas, SKRect bounds, SKRect footerRect,
        string title, string version, string footerLeft)
    {
        GenericWindowTypeA.Draw(canvas, bounds);

        GenericWindowTypeA.DrawTitle(canvas, bounds, title, XenonStyle.TitleText);
        GenericWindowTypeA.DrawVersion(canvas, bounds, version);
        DrawFooter(canvas, footerRect, footerLeft);
    }

    private static void DrawFooter(SKCanvas canvas, SKRect rect, string left)
    {
        canvas.DrawLine(rect.Left, rect.Top, rect.Right, rect.Top, XenonStyle.DimCyanStroke);
        float baseline = MenuStyle.VerticalCenterBaseline(rect, XenonStyle.FooterLeftText) + 2f;
        canvas.DrawText(left, rect.Left + 8f, baseline, XenonStyle.FooterLeftText);
    }

}
