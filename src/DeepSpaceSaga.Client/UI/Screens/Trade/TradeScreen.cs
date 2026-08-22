using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Trade;

/// <summary>
/// Trade overlay (Docs/FirstRelease/Screens/Trade.md). Placeholder shell only — buying,
/// selling and refueling are not implemented yet, so the panel just shows a single
/// "not available yet" line instead of live station inventory/prices. Opened from
/// <see cref="Station.StationScreen"/>'s `TRADE` button (ScreenEvent.OpenTrade) as a
/// nested modal on top of it; closes via the × button, Escape, or a click outside the
/// panel (on the dimmed background), returning to <see cref="Station.StationScreen"/>.
/// Pause-on-open/resume-on-close is handled generically by SkiaWindow's
/// PushModalAsync/PopModalAsync — this screen has no speed/pause logic of its own.
/// Structural twin of <see cref="Finance.FinanceScreen"/>/<see cref="Station.StationScreen"/>.
/// </summary>
public sealed class TradeScreen : IScreen
{
    private int _screenWidth;
    private int _screenHeight;
    private bool _isCloseHovered;

    private static readonly SKColor DimColor = new(0, 0, 0, 160);
    private static readonly SKPaint DimPaint = new() { Color = DimColor, Style = SKPaintStyle.Fill };

    private const string PlaceholderLine = "Trade: not available yet";

    public void OnActivated() => _isCloseHovered = false;

    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key) =>
        key == Key.Escape ? ScreenEvent.CloseTrade : ScreenEvent.None;

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        var hit = TradeLayout.HitTest(x, y, _screenWidth, _screenHeight);
        if (hit == TradeButton.Close)
            return ScreenEvent.CloseTrade;

        // Click on the dimmed background outside the panel also closes it.
        if (!TradeLayout.IsInsidePanel(x, y, _screenWidth, _screenHeight))
            return ScreenEvent.CloseTrade;

        return ScreenEvent.None;
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call-site/test conventions.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    public bool OnMouseMove(float x, float y)
    {
        var hit = TradeLayout.HitTest(x, y, _screenWidth, _screenHeight);
        _isCloseHovered = hit == TradeButton.Close;
        return _isCloseHovered;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;

        // Dim background (overlay effect — underlying screens render behind this)
        canvas.DrawRect(0, 0, width, height, DimPaint);

        float pl = TradeLayout.PanelLeft(width);
        float pt = TradeLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + TradeLayout.PanelWidth, pt + TradeLayout.PanelHeight);
        MenuStyle.DrawPanel(canvas, panelRect);

        float cx = pl + TradeLayout.PanelWidth / 2f;
        canvas.DrawText("TRADE", cx, pt + TradeLayout.TitleY, MenuStyle.TextTitle);
        canvas.DrawText(PlaceholderLine, cx, pt + TradeLayout.BodyStartY, MenuStyle.TextStatus);

        DrawCloseButton(canvas, pl, pt);
    }

    private void DrawCloseButton(SKCanvas canvas, float panelLeft, float panelTop)
    {
        var (left, top, right, bottom) = TradeLayout.CloseButtonLocalRect();
        var rect = new SKRect(panelLeft + left, panelTop + top, panelLeft + right, panelTop + bottom);

        MenuStyle.DrawButton(canvas, rect, "×", _isCloseHovered ? ButtonState.Hovered : ButtonState.Normal);
    }
}
