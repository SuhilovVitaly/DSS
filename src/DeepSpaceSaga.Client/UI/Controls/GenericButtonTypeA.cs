using DeepSpaceSaga.Client.UI.Assets;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>
/// Shared client action button. The consuming screen owns hit testing and action semantics.
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
        SKColor? textColorOverride = null, bool showMarker = true, bool primary = false)
    {
        if (bounds.IsEmpty)
            return;

        TabletopStyle.Button(canvas, bounds, state, primary);
        var textBounds = bounds;
        textBounds.Inflate(-8, 0);
        if (showMarker && bounds.Width >= 80 && bounds.Height >= 20)
        {
            using var marker = new SKPaint
            {
                Color = TabletopStyle.ButtonInk(state, primary),
                IsAntialias = true
            };
            float x = bounds.Left + 16;
            float y = bounds.MidY;
            using var path = new SKPath();
            path.MoveTo(x, y - 4);
            path.LineTo(x + 4, y);
            path.LineTo(x, y + 4);
            path.LineTo(x - 4, y);
            path.Close();
            canvas.DrawPath(path, marker);
            textBounds.Inflate(-16, 0);
        }

        using var paint = new SKPaint
        {
            Color = textColorOverride ?? TabletopStyle.ButtonInk(state, primary),
            TextSize = XenonStyle.ButtonFontSize,
            Typeface = MenuStyle.TypefaceBold,
            IsAntialias = true
        };
        TabletopStyle.Label(canvas, textBounds, text, paint);
    }
}
