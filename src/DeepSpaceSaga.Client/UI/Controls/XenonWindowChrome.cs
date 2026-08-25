using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>Reusable Xenon-style window frame. Interactive rectangles stay owned by the screen layout.</summary>
public static class XenonWindowChrome
{
    internal static bool HasChromeAssets => GenericWindowTypeA.HasAssets;

    /// <summary>Forces bitmap decoding before the render loop.</summary>
    public static void Preload() => GenericWindowTypeA.Preload();

    public static void Draw(SKCanvas canvas, SKRect bounds, SKRect headerRect,
        SKRect footerRect, string title, string subtitle, string footerLeft, string footerRight)
    {
        GenericWindowTypeA.Draw(canvas, bounds);

        GenericWindowTypeA.DrawTitle(canvas, bounds, title, XenonStyle.TitleText);
        DrawHeaderSubtitle(canvas, headerRect, subtitle);
        DrawFooter(canvas, footerRect, footerLeft, footerRight);
    }

    private static void DrawHeaderSubtitle(SKCanvas canvas, SKRect rect, string subtitle)
    {
        float subtitleBaseline = rect.Top + 57f;
        float subtitleWidth = XenonStyle.SubtitleText.MeasureText(subtitle);
        float dotX = rect.MidX - subtitleWidth / 2f - 11f;
        canvas.DrawCircle(dotX, subtitleBaseline - 4f, 7f, XenonStyle.StatusGlow);
        canvas.DrawCircle(dotX, subtitleBaseline - 4f, 2f, XenonStyle.StatusDot);
        canvas.DrawText(subtitle, rect.MidX, subtitleBaseline, XenonStyle.SubtitleText);
    }

    private static void DrawFooter(SKCanvas canvas, SKRect rect, string left, string right)
    {
        canvas.DrawLine(rect.Left, rect.Top, rect.Right, rect.Top, XenonStyle.DimCyanStroke);
        float baseline = MenuStyle.VerticalCenterBaseline(rect, XenonStyle.FooterLeftText) + 2f;
        canvas.DrawText(left, rect.Left + 8f, baseline, XenonStyle.FooterLeftText);
        canvas.DrawText(right, rect.Right - 8f, baseline, XenonStyle.FooterRightText);
    }

}
