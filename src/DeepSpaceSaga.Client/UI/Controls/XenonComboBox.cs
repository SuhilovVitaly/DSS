using DeepSpaceSaga.Client.UI.Assets;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>
/// Xenon Star combo box built from the original Options UI drop-down slices. The screen
/// owns selection, open/closed state, and hit testing; this control only renders them.
/// </summary>
public static class XenonComboBox
{
    internal const string NormalPath = "Images/UI/Themes/Xenon/ComboBox/combo-normal.png";
    internal const string HoverPath = "Images/UI/Themes/Xenon/ComboBox/combo-hover.png";
    internal const string LabelPath = "Images/UI/Themes/Xenon/ComboBox/combo-label.png";
    internal const string ListPath = "Images/UI/Themes/Xenon/ComboBox/combo-list.png";
    internal const string OptionHoverPath = "Images/UI/Themes/Xenon/ComboBox/combo-option-hover.png";

    public const float NativeWidth = 355f;
    public const float LabelHeight = 24f;
    public const float FieldHeight = 34f;
    public const float OptionHeight = 34f;
    public const float OptionHorizontalInset = 13f;

    private static readonly SKBitmap? Normal = UiAssetLoader.LoadBitmap(NormalPath);
    private static readonly SKBitmap? Hover = UiAssetLoader.LoadBitmap(HoverPath);
    private static readonly SKBitmap? Label = UiAssetLoader.LoadBitmap(LabelPath);
    private static readonly SKBitmap? List = UiAssetLoader.LoadBitmap(ListPath);
    private static readonly SKBitmap? OptionHover = UiAssetLoader.LoadBitmap(OptionHoverPath);

    private static readonly SKPaint LabelText = new()
    {
        Color = XenonStyle.Cyan,
        TextSize = 13f,
        IsAntialias = true,
        TextAlign = SKTextAlign.Left,
        Typeface = XenonStyle.TypefaceSemibold
    };

    private static readonly SKPaint ValueText = new()
    {
        Color = XenonStyle.CyanBright,
        TextSize = 15f,
        IsAntialias = true,
        TextAlign = SKTextAlign.Center,
        Typeface = XenonStyle.TypefaceRegular
    };

    private static readonly SKPaint ActiveValueText = new()
    {
        Color = SKColors.White,
        TextSize = 15f,
        IsAntialias = true,
        TextAlign = SKTextAlign.Center,
        Typeface = XenonStyle.TypefaceSemibold
    };

    private static readonly SKPaint FallbackFill = new()
    {
        Color = new SKColor(2, 22, 31),
        Style = SKPaintStyle.Fill
    };

    private static readonly SKPaint FallbackHighlight = new()
    {
        Color = XenonStyle.CyanDim,
        Style = SKPaintStyle.Fill
    };

    internal static bool HasAssets => Normal is not null
        && Hover is not null
        && Label is not null
        && List is not null
        && OptionHover is not null;

    /// <summary>Forces bitmap decoding before the render loop.</summary>
    public static void Preload() { }

    public static void DrawClosed(SKCanvas canvas, SKRect fieldRect, string label, string value, bool highlighted)
    {
        var labelRect = new SKRect(
            fieldRect.Left,
            fieldRect.Top - LabelHeight,
            fieldRect.Right,
            fieldRect.Top);

        if (Label is not null)
            canvas.DrawBitmap(Label, labelRect);
        else
            canvas.DrawRect(labelRect, FallbackFill);

        var fieldImage = highlighted ? Hover : Normal;
        if (fieldImage is not null)
            canvas.DrawBitmap(fieldImage, fieldRect);
        else
        {
            canvas.DrawRect(fieldRect, FallbackFill);
            canvas.DrawRect(fieldRect, XenonStyle.ThinCyanStroke);
        }

        canvas.DrawText(label, labelRect.Left + 14f,
            MenuStyle.VerticalCenterBaseline(labelRect, LabelText), LabelText);

        // The reference image reserves its left side for the illuminated chevron.
        float valueCenterX = fieldRect.Left + 44f + (fieldRect.Width - 56f) / 2f;
        var textPaint = highlighted ? ActiveValueText : ValueText;
        canvas.DrawText(value, valueCenterX,
            MenuStyle.VerticalCenterBaseline(fieldRect, textPaint), textPaint);
    }

    public static void DrawListBackground(SKCanvas canvas, SKRect bounds)
    {
        if (List is not null)
            NinePatch.Draw(canvas, List, bounds, 2f);
        else
        {
            canvas.DrawRect(bounds, FallbackFill);
            canvas.DrawRect(bounds, XenonStyle.ThinCyanStroke);
        }
    }

    public static void DrawOption(SKCanvas canvas, SKRect bounds, string text, bool highlighted)
    {
        if (highlighted)
        {
            var highlightRect = new SKRect(
                bounds.Left + OptionHorizontalInset,
                bounds.Top,
                bounds.Right - OptionHorizontalInset,
                bounds.Bottom);

            if (OptionHover is not null)
                canvas.DrawBitmap(OptionHover, highlightRect);
            else
                canvas.DrawRect(highlightRect, FallbackHighlight);
        }

        var textPaint = highlighted ? ActiveValueText : ValueText;
        canvas.DrawText(text, bounds.MidX,
            MenuStyle.VerticalCenterBaseline(bounds, textPaint), textPaint);
    }
}
