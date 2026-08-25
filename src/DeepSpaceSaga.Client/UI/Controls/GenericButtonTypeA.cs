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

    public static void Draw(SKCanvas canvas, SKRect bounds, string text, ButtonState state)
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
        canvas.DrawText(text, bounds.MidX, MenuStyle.VerticalCenterBaseline(bounds, textPaint), textPaint);
    }

    private static void DrawMarker(SKCanvas canvas, SKRect bounds, ButtonState state)
    {
        var marker = state switch
        {
            ButtonState.Hovered or ButtonState.Pressed => MarkerActive,
            ButtonState.Disabled => MarkerDisabled,
            _ => Marker
        };

        float markerSize = Math.Min(24f, Math.Max(8f, bounds.Height - 16f));
        float leftInset = Math.Min(24f, Math.Max(8f, bounds.Width * 0.08f));
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
}
