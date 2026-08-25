using DeepSpaceSaga.Client.UI.Assets;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>Reusable Xenon-style window frame. Interactive rectangles stay owned by the screen layout.</summary>
public static class XenonWindowChrome
{
    internal const string ShellPath = "Images/UI/Themes/Xenon/Window/window-shell.png";
    internal const string ClosePath = "Images/UI/Themes/Xenon/Icons/close.png";
    internal const string CloseActivePath = "Images/UI/Themes/Xenon/Icons/close-active.png";

    private static readonly SKBitmap? Shell = UiAssetLoader.LoadBitmap(ShellPath);
    private static readonly SKBitmap? Close = UiAssetLoader.LoadBitmap(ClosePath);
    private static readonly SKBitmap? CloseActive = UiAssetLoader.LoadBitmap(CloseActivePath);

    internal static bool HasChromeAssets => Shell is not null;

    /// <summary>Forces bitmap decoding before the render loop.</summary>
    public static void Preload() { }

    public static void Draw(SKCanvas canvas, SKRect bounds, SKRect headerRect,
        SKRect footerRect, SKRect closeRect,
        string title, string subtitle, string footerLeft, string footerRight, ButtonState closeState)
    {
        if (HasChromeAssets)
        {
            NinePatch.Draw(canvas, Shell!, bounds, XenonStyle.WindowSliceInset);
        }
        else
        {
            DrawFallbackFrame(canvas, bounds);
        }

        DrawHeaderText(canvas, headerRect, title, subtitle);
        DrawFooter(canvas, footerRect, footerLeft, footerRight);
        DrawClose(canvas, closeRect, closeState);
    }

    private static void DrawFallbackFrame(SKCanvas canvas, SKRect bounds)
    {
        MenuStyle.DrawPanel(canvas, bounds);
    }

    private static void DrawHeaderText(SKCanvas canvas, SKRect rect, string title, string subtitle)
    {
        float titleBaseline = rect.Top + 30f;
        float subtitleBaseline = rect.Top + 57f;
        canvas.DrawText(title, rect.MidX, titleBaseline, XenonStyle.TitleText);
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

    private static void DrawClose(SKCanvas canvas, SKRect rect, ButtonState state)
    {
        var image = state is ButtonState.Hovered or ButtonState.Pressed ? CloseActive : Close;
        if (image is not null)
        {
            var paint = state == ButtonState.Pressed ? XenonStyle.PressedImagePaint : null;
            canvas.DrawBitmap(image, rect, paint);
            return;
        }

        canvas.DrawRect(rect, state is ButtonState.Hovered or ButtonState.Pressed
            ? XenonStyle.BrightCyanStroke
            : XenonStyle.ThinCyanStroke);
        var cross = state is ButtonState.Hovered or ButtonState.Pressed
            ? XenonStyle.BrightCyanStroke
            : XenonStyle.ThinCyanStroke;
        const float inset = 9f;
        canvas.DrawLine(rect.Left + inset, rect.Top + inset, rect.Right - inset, rect.Bottom - inset, cross);
        canvas.DrawLine(rect.Right - inset, rect.Top + inset, rect.Left + inset, rect.Bottom - inset, cross);
    }
}
