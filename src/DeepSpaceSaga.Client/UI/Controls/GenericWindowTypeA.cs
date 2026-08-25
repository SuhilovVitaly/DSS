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

    private static readonly SKBitmap? Shell = UiAssetLoader.LoadBitmap(ShellPath);

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
}
