using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Defeat;

/// <summary>Logical coordinates; the window applies the configured interface scale.</summary>
internal readonly record struct DefeatLayout(SKRect Panel, SKRect Load, SKRect MainMenu, float Scale)
{
    internal static DefeatLayout Create(int width, int height)
    {
        float scale = Math.Max(.01f, Math.Min(1, Math.Min((width - 16) / 500f, (height - 16) / 280f)));
        float left = (width - 500 * scale) / 2, top = (height - 280 * scale) / 2;
        SKRect Rect(float x, float y, float w, float h) =>
            new(left + x * scale, top + y * scale, left + (x + w) * scale, top + (y + h) * scale);
        return new(Rect(0, 0, 500, 280), Rect(40, 110, 420, 56), Rect(40, 182, 420, 56), scale);
    }

    internal ScreenEvent Hit(float x, float y) => Load.Contains(x, y) ? ScreenEvent.OpenLoadWindow :
        MainMenu.Contains(x, y) ? ScreenEvent.MainMenu : ScreenEvent.None;
}
