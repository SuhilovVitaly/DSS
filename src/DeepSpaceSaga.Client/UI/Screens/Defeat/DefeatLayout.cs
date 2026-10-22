using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Defeat;

/// <summary>Logical coordinates; the window applies the configured interface scale.</summary>
internal readonly record struct DefeatLayout(SKRect Panel, SKRect Load, SKRect MainMenu, float Scale, SKRect Stats = default)
{
    internal static DefeatLayout Create(int width, int height, bool hasStats = false)
    {
        float panelHeight = hasStats ? 440 : 280;
        float scale = Math.Max(.01f, Math.Min(1, Math.Min((width - 16) / 500f, (height - 16) / panelHeight)));
        float left = (width - 500 * scale) / 2, top = (height - panelHeight * scale) / 2;
        SKRect Rect(float x, float y, float w, float h) =>
            new(left + x * scale, top + y * scale, left + (x + w) * scale, top + (y + h) * scale);
        return new(Rect(0, 0, 500, panelHeight), Rect(40, hasStats ? 260 : 110, 420, 56),
            Rect(40, hasStats ? 332 : 182, 420, 56), scale, hasStats ? Rect(40, 98, 420, 140) : SKRect.Empty);
    }

    internal ScreenEvent Hit(float x, float y) => Load.Contains(x, y) ? ScreenEvent.OpenLoadWindow :
        MainMenu.Contains(x, y) ? ScreenEvent.MainMenu : ScreenEvent.None;
}
