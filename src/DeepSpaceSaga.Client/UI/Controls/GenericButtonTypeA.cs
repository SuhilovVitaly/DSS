using DeepSpaceSaga.Client.UI.Assets;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>
/// Generic Type A action button. Draws a scalable cyan/navy shell, a diamond marker,
/// centered caller-provided text, and all standard button states. It owns no hit testing
/// or action semantics; those remain with the consuming screen.
/// </summary>
public static class GenericButtonTypeA
{
    internal const string NormalPath = "Images/UI/Themes/Xenon/Buttons/menu-button.png";
    internal const string HoverPath = "Images/UI/Themes/Xenon/Buttons/menu-button-hover.png";
    internal const string MarkerPath = "Images/UI/Themes/Xenon/Icons/diamond-marker.png";
    internal const string MarkerActivePath = "Images/UI/Themes/Xenon/Icons/diamond-marker-active.png";
    internal const string MarkerDisabledPath = "Images/UI/Themes/Xenon/Icons/diamond-marker-disabled.png";

    private static readonly SKBitmap? Normal = UiAssetLoader.LoadBitmap(NormalPath);
    private static readonly SKBitmap? Hover = UiAssetLoader.LoadBitmap(HoverPath);
    private static readonly SKBitmap? Marker = UiAssetLoader.LoadBitmap(MarkerPath);
    private static readonly SKBitmap? MarkerActive = UiAssetLoader.LoadBitmap(MarkerActivePath);
    private static readonly SKBitmap? MarkerDisabled = UiAssetLoader.LoadBitmap(MarkerDisabledPath);

    internal static bool HasAssets => Normal is not null && Hover is not null;

    /// <summary>Forces bitmap decoding before the render loop.</summary>
    public static void Preload() { }

    public static void Draw(
        SKCanvas canvas, SKRect bounds, string text, ButtonState state,
        SKColor? textColorOverride = null, bool showMarker = true)
    {
        if (HasAssets)
        {
            var image = state is ButtonState.Hovered or ButtonState.Pressed ? Hover! : Normal!;
            var imagePaint = state switch
            {
                ButtonState.Disabled => XenonStyle.DisabledImagePaint,
                ButtonState.Pressed => XenonStyle.PressedImagePaint,
                _ => null
            };
            NinePatch.Draw(canvas, image, bounds, XenonStyle.ButtonSliceInset, imagePaint);
        }
        else
        {
            MenuStyle.DrawButton(canvas, bounds, text, state);
        }

        if (showMarker)
            DrawMarker(canvas, bounds, state);
        if (!HasAssets)
            return;

        var textPaint = state switch
        {
            ButtonState.Hovered => XenonStyle.ButtonTextHover,
            ButtonState.Pressed => XenonStyle.ButtonTextPressed,
            ButtonState.Disabled => XenonStyle.ButtonTextDisabled,
            _ => XenonStyle.ButtonText
        };

        // Keep long labels clear of the fixed left marker while preserving the centered
        // Type A composition. Most labels retain the theme font size; only labels that
        // would enter the marker's breathing room are scaled down.
        float maxTextWidth;
        if (showMarker)
        {
            float markerSize = MarkerSize(bounds);
            float markerLeftInset = MarkerLeftInset(bounds);
            float markerClearanceX = bounds.Left + markerLeftInset + markerSize + 8f;
            maxTextWidth = Math.Max(1f, 2f * (bounds.MidX - markerClearanceX));
        }
        else
        {
            maxTextWidth = Math.Max(1f, bounds.Width - 16f);
        }
        float textWidth = textPaint.MeasureText(text);

        if (textWidth > maxTextWidth || textColorOverride.HasValue)
        {
            using var fittedPaint = new SKPaint
            {
                Color = textColorOverride ?? textPaint.Color,
                TextSize = textWidth > maxTextWidth
                    ? textPaint.TextSize * maxTextWidth / textWidth
                    : textPaint.TextSize,
                IsAntialias = textPaint.IsAntialias,
                TextAlign = textPaint.TextAlign,
                Typeface = textPaint.Typeface
            };
            canvas.DrawText(text, bounds.MidX,
                MenuStyle.VerticalCenterBaseline(bounds, fittedPaint), fittedPaint);
            return;
        }

        canvas.DrawText(text, bounds.MidX,
            MenuStyle.VerticalCenterBaseline(bounds, textPaint), textPaint);
    }

    private static void DrawMarker(SKCanvas canvas, SKRect bounds, ButtonState state)
    {
        var marker = state switch
        {
            ButtonState.Hovered or ButtonState.Pressed => MarkerActive,
            ButtonState.Disabled => MarkerDisabled,
            _ => Marker
        };

        float markerSize = MarkerSize(bounds);
        float leftInset = MarkerLeftInset(bounds);
        var rect = new SKRect(
            bounds.Left + leftInset,
            bounds.MidY - markerSize / 2f,
            bounds.Left + leftInset + markerSize,
            bounds.MidY + markerSize / 2f);

        if (marker is not null)
        {
            var paint = state == ButtonState.Pressed ? XenonStyle.PressedImagePaint : null;
            canvas.DrawBitmap(marker, rect, paint);
            return;
        }

        var stroke = state == ButtonState.Disabled
            ? XenonStyle.DimCyanStroke
            : state is ButtonState.Hovered or ButtonState.Pressed
                ? XenonStyle.BrightCyanStroke
                : XenonStyle.ThinCyanStroke;
        canvas.DrawLine(rect.MidX, rect.Top, rect.Right, rect.MidY, stroke);
        canvas.DrawLine(rect.Right, rect.MidY, rect.MidX, rect.Bottom, stroke);
        canvas.DrawLine(rect.MidX, rect.Bottom, rect.Left, rect.MidY, stroke);
        canvas.DrawLine(rect.Left, rect.MidY, rect.MidX, rect.Top, stroke);
    }

    private static float MarkerSize(SKRect bounds) =>
        Math.Min(24f, Math.Max(8f, bounds.Height - 16f));

    private static float MarkerLeftInset(SKRect bounds) =>
        Math.Min(24f, Math.Max(8f, bounds.Width * 0.08f));
}
