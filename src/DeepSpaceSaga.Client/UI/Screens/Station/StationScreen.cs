using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Station;

/// <summary>
/// Station overlay (Docs/FirstRelease/Screens/Station.md). Placeholder shell:
/// Trade/Finance/Hire/Representatives/Install Drilling Unit/Undock are not yet
/// implemented, so the panel shows a "not available yet" line instead of live
/// station data. Opened from GameSessionScreen by left-clicking the station the
/// player ship is currently docked to (ScreenEvent.OpenStation); closes via the
/// × button, Escape, or a click outside the panel (on the dimmed background),
/// returning to GameSessionScreen while the docked state itself is untouched —
/// clicking the station again reopens this screen. Pause-on-open/resume-on-close
/// is handled generically by SkiaWindow's PushModalAsync/PopModalAsync — this
/// screen has no speed/pause logic of its own. Structural twin of
/// <see cref="Finance.FinanceScreen"/>.
/// </summary>
public sealed class StationScreen : IScreen
{
    private int _screenWidth;
    private int _screenHeight;
    private bool _isCloseHovered;

    private static readonly SKColor DimColor = new(0, 0, 0, 160);
    private static readonly SKPaint DimPaint = new() { Color = DimColor, Style = SKPaintStyle.Fill };

    private static readonly string[] PlaceholderLines =
    {
        "Trade: not available yet",
        "Finance: not available yet",
        "Representatives: not available yet",
        "Install Drilling Unit: not available yet",
        "Hire: not available yet",
        "Undock: not available yet",
    };

    public void OnActivated() => _isCloseHovered = false;

    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key) =>
        key == Key.Escape ? ScreenEvent.CloseStation : ScreenEvent.None;

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        var hit = StationLayout.HitTest(x, y, _screenWidth, _screenHeight);
        if (hit == StationButton.Close)
            return ScreenEvent.CloseStation;

        // Click on the dimmed background outside the panel also closes it.
        if (!StationLayout.IsInsidePanel(x, y, _screenWidth, _screenHeight))
            return ScreenEvent.CloseStation;

        return ScreenEvent.None;
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call-site/test conventions.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    public bool OnMouseMove(float x, float y)
    {
        var hit = StationLayout.HitTest(x, y, _screenWidth, _screenHeight);
        _isCloseHovered = hit == StationButton.Close;
        return _isCloseHovered;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;

        // Dim background (overlay effect — underlying GameSessionScreen renders behind this)
        canvas.DrawRect(0, 0, width, height, DimPaint);

        float pl = StationLayout.PanelLeft(width);
        float pt = StationLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + StationLayout.PanelWidth, pt + StationLayout.PanelHeight);
        MenuStyle.DrawPanel(canvas, panelRect);

        float cx = pl + StationLayout.PanelWidth / 2f;
        canvas.DrawText("STATION", cx, pt + StationLayout.TitleY, MenuStyle.TextTitle);

        float textY = pt + StationLayout.BodyStartY;
        foreach (var line in PlaceholderLines)
        {
            canvas.DrawText(line, cx, textY, MenuStyle.TextStatus);
            textY += StationLayout.BodyLineHeight;
        }

        DrawCloseButton(canvas, pl, pt);
    }

    private void DrawCloseButton(SKCanvas canvas, float panelLeft, float panelTop)
    {
        var (left, top, right, bottom) = StationLayout.CloseButtonLocalRect();
        var rect = new SKRect(panelLeft + left, panelTop + top, panelLeft + right, panelTop + bottom);

        MenuStyle.DrawButton(canvas, rect, "×", _isCloseHovered ? ButtonState.Hovered : ButtonState.Normal);
    }
}
