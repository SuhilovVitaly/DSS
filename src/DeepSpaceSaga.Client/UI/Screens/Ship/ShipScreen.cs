using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Ship;

/// <summary>
/// Ship overlay (ТЗ Docs/FirstRelease/Screens/Ship.md). Placeholder shell: the
/// TetrarchClass/CrewAndHabitation/CrewDialogues/IceMining mechanics it will report
/// on are not yet implemented in the Engine, so every section shows a "not
/// available yet" line instead of fabricated data. Opened via the bottom-center
/// Ship panel button or Ctrl+S; closes via the × button, Escape, or a click
/// outside the panel (on the dimmed background). Pause-on-open/resume-on-close
/// is handled generically by SkiaWindow's PushModalAsync/PopModalAsync — this
/// screen has no speed/pause logic of its own.
/// </summary>
public sealed class ShipScreen : IScreen
{
    private int _screenWidth;
    private int _screenHeight;
    private bool _isCloseHovered;

    /// <summary>
    /// Panel background with title bar, at the exact 1400×900 panel size (ТЗ
    /// ScreenCatalog.md standard for gameplay-mechanic windows). Loaded once and
    /// shared by every ShipScreen instance; falls back to MenuStyle.DrawPanel's
    /// plain fill if the file is missing.
    /// </summary>
    private static readonly SKBitmap? BackgroundImage =
        LoadImage("Images/UI/mechanics-window-background-titlebar-1400x900.png");

    private static SKBitmap? LoadImage(string path)
    {
        try { return File.Exists(path) ? SKBitmap.Decode(path) : null; }
        catch { return null; }
    }

    /// <summary>True if the background PNG file was found and decoded at startup.</summary>
    internal static bool HasLoadedBackground => BackgroundImage is not null;

    private static readonly string[] PlaceholderLines =
    {
        "Ship (Tetrarch Class) overview: not available yet",
        "Crew and cabin occupancy: not available yet",
        "Character Communication access: not available yet",
        "Drilling Unit installed: not available yet",
    };

    public void OnActivated() => _isCloseHovered = false;

    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key) =>
        key == Key.Escape ? ScreenEvent.CloseShip : ScreenEvent.None;

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        var hit = ShipLayout.HitTest(x, y, _screenWidth, _screenHeight);
        if (hit == ShipButton.Close)
            return ScreenEvent.CloseShip;

        // Click on the dimmed background outside the panel also closes it.
        if (!ShipLayout.IsInsidePanel(x, y, _screenWidth, _screenHeight))
            return ScreenEvent.CloseShip;

        return ScreenEvent.None;
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call sites/tests.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    public bool OnMouseMove(float x, float y)
    {
        var hit = ShipLayout.HitTest(x, y, _screenWidth, _screenHeight);
        _isCloseHovered = hit == ShipButton.Close;
        return _isCloseHovered;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;

        float pl = ShipLayout.PanelLeft(width);
        float pt = ShipLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + ShipLayout.PanelWidth, pt + ShipLayout.PanelHeight);
        if (BackgroundImage is not null)
            canvas.DrawBitmap(BackgroundImage, panelRect);
        else
            MenuStyle.DrawPanel(canvas, panelRect);

        float cx = pl + ShipLayout.PanelWidth / 2f;
        canvas.DrawText("SHIP", cx, pt + ShipLayout.TitleY, MenuStyle.TextTitle);

        float textY = pt + ShipLayout.BodyStartY;
        foreach (var line in PlaceholderLines)
        {
            canvas.DrawText(line, cx, textY, MenuStyle.TextStatus);
            textY += ShipLayout.BodyLineHeight;
        }

        DrawCloseButton(canvas, pl, pt);
    }

    private void DrawCloseButton(SKCanvas canvas, float panelLeft, float panelTop)
    {
        var (left, top, right, bottom) = ShipLayout.CloseButtonLocalRect();
        var rect = new SKRect(panelLeft + left, panelTop + top, panelLeft + right, panelTop + bottom);

        MenuStyle.DrawButton(canvas, rect, "×", _isCloseHovered ? ButtonState.Hovered : ButtonState.Normal);
    }
}
