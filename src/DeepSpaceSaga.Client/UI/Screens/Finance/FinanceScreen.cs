using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Finance;

/// <summary>
/// Finance overlay (ТЗ Docs/FirstRelease/Screens/Finance.md). Placeholder shell:
/// the Money/Trading/StationInventory mechanics it will report on are not yet
/// implemented in the Engine, so every section shows a "not available yet" line
/// instead of fabricated numbers. Opened via the bottom-center Finance panel
/// button or Ctrl+F; closes via the × button, Escape, or a click outside the
/// panel (on the dimmed background). Pause-on-open/resume-on-close
/// is handled generically by SkiaWindow's PushModalAsync/PopModalAsync — this
/// screen has no speed/pause logic of its own.
/// </summary>
public sealed class FinanceScreen : IScreen
{
    private readonly SnapshotBuffer? _buffer;

    private int _screenWidth;
    private int _screenHeight;
    private bool _isCloseHovered;

    public FinanceScreen(SnapshotBuffer? buffer = null)
    {
        _buffer = buffer;
    }

    /// <summary>
    /// Panel background with title bar. The PNG asset is a fixed 1400×900 bitmap;
    /// since the panel standard (ТЗ ScreenCatalog.md) is now 1400×800, it is drawn
    /// stretched to <see cref="FinanceLayout.PanelWidth"/>x<see cref="FinanceLayout.PanelHeight"/>
    /// (see DrawBitmap call below) until the asset itself is redrawn at the new size.
    /// Loaded once and shared by every FinanceScreen instance; falls back to
    /// MenuStyle.DrawPanel's plain fill if the file is missing.
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
        "Player Credits balance: not available yet",
        "Station price summary: not available yet",
        "Recent trading results: not available yet",
    };

    public void OnActivated() => _isCloseHovered = false;

    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key) =>
        key == Key.Escape ? ScreenEvent.CloseFinance : ScreenEvent.None;

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        var hit = FinanceLayout.HitTest(x, y, _screenWidth, _screenHeight);
        if (hit == FinanceButton.Close)
            return ScreenEvent.CloseFinance;

        if (IsStationNameHit(x, y))
            return ScreenEvent.NavigateToStation;

        // Click on the dimmed background outside the panel also closes it.
        if (!FinanceLayout.IsInsidePanel(x, y, _screenWidth, _screenHeight))
            return ScreenEvent.CloseFinance;

        return ScreenEvent.None;
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call sites/tests.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    public bool OnMouseMove(float x, float y)
    {
        var hit = FinanceLayout.HitTest(x, y, _screenWidth, _screenHeight);
        _isCloseHovered = hit == FinanceButton.Close;
        return _isCloseHovered || IsStationNameHit(x, y);
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;

        float pl = FinanceLayout.PanelLeft(width);
        float pt = FinanceLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + FinanceLayout.PanelWidth, pt + FinanceLayout.PanelHeight);
        if (BackgroundImage is not null)
            canvas.DrawBitmap(BackgroundImage, panelRect);
        else
            MenuStyle.DrawPanel(canvas, panelRect);

        string? stationName = StationToolbar.ResolveDockedStationName(_buffer?.Latest?.Snapshot);
        StationToolbar.Draw(canvas, pl, pt, stationName, isStationHub: false);

        float cx = pl + FinanceLayout.PanelWidth / 2f;
        canvas.DrawText("FINANCE", cx, pt + FinanceLayout.TitleY, MenuStyle.TextTitle);

        float textY = pt + FinanceLayout.BodyStartY;
        foreach (var line in PlaceholderLines)
        {
            canvas.DrawText(line, cx, textY, MenuStyle.TextStatus);
            textY += FinanceLayout.BodyLineHeight;
        }

        DrawCloseButton(canvas, pl, pt);
    }

    private void DrawCloseButton(SKCanvas canvas, float panelLeft, float panelTop)
    {
        var (left, top, right, bottom) = FinanceLayout.CloseButtonLocalRect();
        var rect = new SKRect(panelLeft + left, panelTop + top, panelLeft + right, panelTop + bottom);

        MenuStyle.DrawButton(canvas, rect, "×", _isCloseHovered ? ButtonState.Hovered : ButtonState.Normal);
    }

    /// <summary>True when (x, y) lands on the toolbar's station-name link (see StationToolbar).</summary>
    private bool IsStationNameHit(float x, float y)
    {
        string? stationName = StationToolbar.ResolveDockedStationName(_buffer?.Latest?.Snapshot);
        if (string.IsNullOrEmpty(stationName))
            return false;

        float pl = FinanceLayout.PanelLeft(_screenWidth);
        float pt = FinanceLayout.PanelTop(_screenHeight);
        var local = StationToolbar.NameLocalRect(stationName);
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }
}
