using DeepSpaceSaga.Client.UI.Assets;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>
/// Tabletop combo box used in Settings. The screen
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
        Color = TabletopStyle.Secondary,
        TextSize = 13f,
        IsAntialias = true,
        TextAlign = SKTextAlign.Left,
        Typeface = XenonStyle.TypefaceSemibold
    };

    private static readonly SKPaint ValueText = new()
    {
        Color = TabletopStyle.Ink,
        TextSize = 15f,
        IsAntialias = true,
        TextAlign = SKTextAlign.Center,
        Typeface = XenonStyle.TypefaceRegular
    };

    private static readonly SKPaint ActiveValueText = new()
    {
        Color = TabletopStyle.Gold,
        TextSize = 15f,
        IsAntialias = true,
        TextAlign = SKTextAlign.Center,
        Typeface = XenonStyle.TypefaceSemibold
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
        var labelRect = new SKRect(fieldRect.Left + 10, fieldRect.Top - LabelHeight, fieldRect.Right - 10, fieldRect.Top);
        ControlVisuals.DrawLabel(canvas, ControlText.Ellipsize(label, labelRect.Width, LabelText), labelRect, LabelText);
        TabletopStyle.Field(canvas, fieldRect, highlighted);
        var valueRect = new SKRect(fieldRect.Left + 10, fieldRect.Top, fieldRect.Right - 28, fieldRect.Bottom);
        TabletopStyle.Label(canvas, valueRect, value, highlighted ? ActiveValueText : ValueText);

        using var chevron = new SKPaint { Color = highlighted ? TabletopStyle.Gold : TabletopStyle.Secondary, StrokeWidth = 2, IsAntialias = true };
        float x = fieldRect.Right - 14;
        float y = fieldRect.MidY;
        canvas.DrawLine(x - 4, y - 2, x, y + 2, chevron);
        canvas.DrawLine(x, y + 2, x + 4, y - 2, chevron);
    }

    public static void DrawListBackground(SKCanvas canvas, SKRect bounds) => TabletopStyle.Panel(canvas, bounds);

    public static void DrawOption(SKCanvas canvas, SKRect bounds, string text, bool highlighted)
    {
        bounds.Inflate(-OptionHorizontalInset, 0);
        if (highlighted)
            TabletopStyle.Field(canvas, bounds, focused: true);
        bounds.Inflate(-8, 0);
        TabletopStyle.Label(canvas, bounds, text, highlighted ? ActiveValueText : ValueText);
    }
}
