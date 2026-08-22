using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>
/// The nine-sliced bordered panel (first used by
/// <see cref="ScenarioSelect.ScenarioSelectScreen"/>'s content/action panels, meant for
/// reuse on any screen that wants this panel look) — draws
/// <c>Images/UI/Panels/micro-panel.png</c> via <see cref="NinePatch"/> so one small
/// texture covers any panel size while its rounded, transparent corners stay unscaled and
/// unstretched.
/// </summary>
public static class ImagePanel
{
    private static readonly SKBitmap? Image = LoadImage("Images/UI/Panels/micro-panel.png");

    /// <summary>
    /// Corner/edge-sample size (in Image source pixels) for NinePatch — a 20×20 cut at
    /// each corner, and a 20×20 sample from the middle of each edge for the stretched
    /// borders.
    /// </summary>
    private const float CornerInset = 20f;

    /// <summary>True if the panel PNG file was found and decoded at startup.</summary>
    internal static bool HasLoadedImage => Image is not null;

    private static SKBitmap? LoadImage(string path)
    {
        try { return File.Exists(path) ? SKBitmap.Decode(path) : null; }
        catch { return null; }
    }

    /// <summary>
    /// Draws the panel filling <paramref name="rect"/>. Falls back to
    /// <see cref="MenuStyle.DrawPanel"/>'s plain fill if the texture failed to load.
    /// </summary>
    public static void Draw(SKCanvas canvas, SKRect rect)
    {
        if (Image is not null)
            NinePatch.Draw(canvas, Image, rect, CornerInset);
        else
            MenuStyle.DrawPanel(canvas, rect);
    }
}
