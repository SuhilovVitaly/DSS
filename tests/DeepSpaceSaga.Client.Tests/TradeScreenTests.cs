using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// The Trade overlay screen itself (opened from StationScreen's `TRADE` button — see
/// StationScreenTests's Trade-button tests). Placeholder shell only — buying/selling
/// mechanics aren't in the Engine yet, so there's no real trade data to assert on, just
/// the open/close mechanics. Structural twin of StationScreenTests/FinanceScreenTests.
/// </summary>
public class TradeScreenTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static void RenderScreen(TradeScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    [Fact]
    public void Escape_returns_CloseTrade()
    {
        var screen = new TradeScreen();
        var result = screen.OnKeyDown(Key.Escape);
        Assert.Equal(ScreenEvent.CloseTrade, result);
    }

    [Fact]
    public void Close_button_click_returns_CloseTrade()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        var hit = TradeLayout.HitTest(
            TradeLayout.PanelLeft(ScreenWidth) + TradeLayout.CloseButtonLocalRect().Left + 1f,
            TradeLayout.PanelTop(ScreenHeight) + TradeLayout.CloseButtonLocalRect().Top + 1f,
            ScreenWidth, ScreenHeight);
        Assert.Equal(TradeButton.Close, hit);

        var (left, top, right, bottom) = TradeLayout.CloseButtonLocalRect();
        float cx = TradeLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = TradeLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        var result = screen.OnMouseDown(cx, cy);
        Assert.Equal(ScreenEvent.CloseTrade, result);
    }

    [Fact]
    public void Click_inside_panel_outside_close_button_returns_None()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        float px = TradeLayout.PanelLeft(ScreenWidth) + TradeLayout.PanelWidth / 2f;
        float py = TradeLayout.PanelTop(ScreenHeight) + TradeLayout.PanelHeight / 2f;

        var result = screen.OnMouseDown(px, py);
        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void Click_outside_panel_returns_CloseTrade()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        // Top-left corner of the screen — well outside the centered panel.
        var result = screen.OnMouseDown(2f, 2f);
        Assert.Equal(ScreenEvent.CloseTrade, result);
    }

    [Fact]
    public void Right_click_outside_panel_does_not_close()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        var result = screen.OnMouseDown(2f, 2f, MouseButton.Right);
        Assert.Equal(ScreenEvent.None, result);
    }
}
