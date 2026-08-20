using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Ship;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// The Ship overlay screen itself (opened from GameSessionScreen's Mechanics
/// panel — see MechanicsPanelTests.cs). Placeholder shell only —
/// TetrarchClass/CrewAndHabitation/CrewDialogues/IceMining mechanics aren't in
/// the Engine yet, so there's no real ship data to assert on, just the
/// open/close mechanics. Mirrors FinanceScreenTests.cs exactly.
/// </summary>
public class ShipScreenTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static void RenderScreen(ShipScreen screen)
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
        Assert.True(ShipScreen.HasLoadedBackground);
    }

    [Fact]
    public void Escape_returns_CloseShip()
    {
        var screen = new ShipScreen();
        var result = screen.OnKeyDown(Key.Escape);
        Assert.Equal(ScreenEvent.CloseShip, result);
    }

    [Fact]
    public void Close_button_click_returns_CloseShip()
    {
        var screen = new ShipScreen();
        RenderScreen(screen);

        var hit = ShipLayout.HitTest(
            ShipLayout.PanelLeft(ScreenWidth) + ShipLayout.CloseButtonLocalRect().Left + 1f,
            ShipLayout.PanelTop(ScreenHeight) + ShipLayout.CloseButtonLocalRect().Top + 1f,
            ScreenWidth, ScreenHeight);
        Assert.Equal(ShipButton.Close, hit);

        var (left, top, right, bottom) = ShipLayout.CloseButtonLocalRect();
        float cx = ShipLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = ShipLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        var result = screen.OnMouseDown(cx, cy);
        Assert.Equal(ScreenEvent.CloseShip, result);
    }

    [Fact]
    public void Click_inside_panel_outside_close_button_returns_None()
    {
        var screen = new ShipScreen();
        RenderScreen(screen);

        float px = ShipLayout.PanelLeft(ScreenWidth) + ShipLayout.PanelWidth / 2f;
        float py = ShipLayout.PanelTop(ScreenHeight) + ShipLayout.PanelHeight / 2f;

        var result = screen.OnMouseDown(px, py);
        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void Click_outside_panel_returns_CloseShip()
    {
        var screen = new ShipScreen();
        RenderScreen(screen);

        // Top-left corner of the screen — well outside the centered panel.
        var result = screen.OnMouseDown(2f, 2f);
        Assert.Equal(ScreenEvent.CloseShip, result);
    }

    [Fact]
    public void Right_click_outside_panel_does_not_close()
    {
        var screen = new ShipScreen();
        RenderScreen(screen);

        var result = screen.OnMouseDown(2f, 2f, MouseButton.Right);
        Assert.Equal(ScreenEvent.None, result);
    }
}
