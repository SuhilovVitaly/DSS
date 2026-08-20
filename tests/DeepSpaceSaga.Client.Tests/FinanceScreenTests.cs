using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Finance;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// The Finance overlay screen itself (opened from GameSessionScreen's Mechanics
/// panel — see MechanicsPanelTests.cs). Placeholder shell only —
/// Money/Trading/StationInventory mechanics aren't in the Engine yet, so there's
/// no real financial data to assert on, just the open/close mechanics.
/// </summary>
public class FinanceScreenTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static void RenderScreen(FinanceScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    [Fact]
    public void Background_image_is_loaded()
    {
        // Regression: the mechanics-window-background-titlebar-1700x1200.png asset must
        // resolve at the client's working directory and be registered in the .csproj
        // with CopyToOutputDirectory, or the panel silently falls back to a plain fill.
        Assert.True(FinanceScreen.HasLoadedBackground);
    }

    [Fact]
    public void Escape_returns_CloseFinance()
    {
        var screen = new FinanceScreen();
        var result = screen.OnKeyDown(Key.Escape);
        Assert.Equal(ScreenEvent.CloseFinance, result);
    }

    [Fact]
    public void Close_button_click_returns_CloseFinance()
    {
        var screen = new FinanceScreen();
        RenderScreen(screen);

        var hit = FinanceLayout.HitTest(
            FinanceLayout.PanelLeft(ScreenWidth) + FinanceLayout.CloseButtonLocalRect().Left + 1f,
            FinanceLayout.PanelTop(ScreenHeight) + FinanceLayout.CloseButtonLocalRect().Top + 1f,
            ScreenWidth, ScreenHeight);
        Assert.Equal(FinanceButton.Close, hit);

        var (left, top, right, bottom) = FinanceLayout.CloseButtonLocalRect();
        float cx = FinanceLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = FinanceLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        var result = screen.OnMouseDown(cx, cy);
        Assert.Equal(ScreenEvent.CloseFinance, result);
    }

    [Fact]
    public void Click_inside_panel_outside_close_button_returns_None()
    {
        var screen = new FinanceScreen();
        RenderScreen(screen);

        float px = FinanceLayout.PanelLeft(ScreenWidth) + FinanceLayout.PanelWidth / 2f;
        float py = FinanceLayout.PanelTop(ScreenHeight) + FinanceLayout.PanelHeight / 2f;

        var result = screen.OnMouseDown(px, py);
        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void Click_outside_panel_returns_CloseFinance()
    {
        var screen = new FinanceScreen();
        RenderScreen(screen);

        // Top-left corner of the screen — well outside the centered panel.
        var result = screen.OnMouseDown(2f, 2f);
        Assert.Equal(ScreenEvent.CloseFinance, result);
    }

    [Fact]
    public void Right_click_outside_panel_does_not_close()
    {
        var screen = new FinanceScreen();
        RenderScreen(screen);

        var result = screen.OnMouseDown(2f, 2f, MouseButton.Right);
        Assert.Equal(ScreenEvent.None, result);
    }
}
