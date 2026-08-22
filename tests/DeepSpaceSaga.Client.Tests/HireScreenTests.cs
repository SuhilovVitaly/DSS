using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Hire;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// The Hire overlay screen itself (opened from StationScreen's `HIRE` button — see
/// StationScreenTests's Hire-button tests). Placeholder shell only — passenger
/// contracts aren't in the Engine yet, so there's no real contract data to assert on,
/// just the open/close mechanics. Structural twin of TradeScreenTests/StationScreenTests.
/// </summary>
public class HireScreenTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static void RenderScreen(HireScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    [Fact]
    public void Escape_returns_CloseHire()
    {
        var screen = new HireScreen();
        var result = screen.OnKeyDown(Key.Escape);
        Assert.Equal(ScreenEvent.CloseHire, result);
    }

    [Fact]
    public void Close_button_click_returns_CloseHire()
    {
        var screen = new HireScreen();
        RenderScreen(screen);

        var hit = HireLayout.HitTest(
            HireLayout.PanelLeft(ScreenWidth) + HireLayout.CloseButtonLocalRect().Left + 1f,
            HireLayout.PanelTop(ScreenHeight) + HireLayout.CloseButtonLocalRect().Top + 1f,
            ScreenWidth, ScreenHeight);
        Assert.Equal(HireButton.Close, hit);

        var (left, top, right, bottom) = HireLayout.CloseButtonLocalRect();
        float cx = HireLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = HireLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        var result = screen.OnMouseDown(cx, cy);
        Assert.Equal(ScreenEvent.CloseHire, result);
    }

    [Fact]
    public void Click_inside_panel_outside_close_button_returns_None()
    {
        var screen = new HireScreen();
        RenderScreen(screen);

        float px = HireLayout.PanelLeft(ScreenWidth) + HireLayout.PanelWidth / 2f;
        float py = HireLayout.PanelTop(ScreenHeight) + HireLayout.PanelHeight / 2f;

        var result = screen.OnMouseDown(px, py);
        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void Click_outside_panel_returns_CloseHire()
    {
        var screen = new HireScreen();
        RenderScreen(screen);

        // Top-left corner of the screen — well outside the centered panel.
        var result = screen.OnMouseDown(2f, 2f);
        Assert.Equal(ScreenEvent.CloseHire, result);
    }

    [Fact]
    public void Right_click_outside_panel_does_not_close()
    {
        var screen = new HireScreen();
        RenderScreen(screen);

        var result = screen.OnMouseDown(2f, 2f, MouseButton.Right);
        Assert.Equal(ScreenEvent.None, result);
    }
}
