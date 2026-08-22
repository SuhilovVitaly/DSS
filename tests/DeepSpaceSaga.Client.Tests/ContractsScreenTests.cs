using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// The Contracts overlay screen itself (opened from StationScreen's `CONTRACTS` button —
/// see StationScreenTests's Contracts-button tests). Split out of the original `Hire`
/// screen (passenger contracts vs. crew hiring — see HireScreenTests). Placeholder shell
/// only — passenger contracts aren't in the Engine yet, so there's no real contract data
/// to assert on, just the open/close mechanics. Structural twin of HireScreenTests.
/// </summary>
public class ContractsScreenTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static void RenderScreen(ContractsScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    [Fact]
    public void Escape_returns_CloseContracts()
    {
        var screen = new ContractsScreen();
        var result = screen.OnKeyDown(Key.Escape);
        Assert.Equal(ScreenEvent.CloseContracts, result);
    }

    [Fact]
    public void Close_button_click_returns_CloseContracts()
    {
        var screen = new ContractsScreen();
        RenderScreen(screen);

        var hit = ContractsLayout.HitTest(
            ContractsLayout.PanelLeft(ScreenWidth) + ContractsLayout.CloseButtonLocalRect().Left + 1f,
            ContractsLayout.PanelTop(ScreenHeight) + ContractsLayout.CloseButtonLocalRect().Top + 1f,
            ScreenWidth, ScreenHeight);
        Assert.Equal(ContractsButton.Close, hit);

        var (left, top, right, bottom) = ContractsLayout.CloseButtonLocalRect();
        float cx = ContractsLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = ContractsLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        var result = screen.OnMouseDown(cx, cy);
        Assert.Equal(ScreenEvent.CloseContracts, result);
    }

    [Fact]
    public void Click_inside_panel_outside_close_button_returns_None()
    {
        var screen = new ContractsScreen();
        RenderScreen(screen);

        float px = ContractsLayout.PanelLeft(ScreenWidth) + ContractsLayout.PanelWidth / 2f;
        float py = ContractsLayout.PanelTop(ScreenHeight) + ContractsLayout.PanelHeight / 2f;

        var result = screen.OnMouseDown(px, py);
        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void Click_outside_panel_returns_CloseContracts()
    {
        var screen = new ContractsScreen();
        RenderScreen(screen);

        // Top-left corner of the screen — well outside the centered panel.
        var result = screen.OnMouseDown(2f, 2f);
        Assert.Equal(ScreenEvent.CloseContracts, result);
    }

    [Fact]
    public void Right_click_outside_panel_does_not_close()
    {
        var screen = new ContractsScreen();
        RenderScreen(screen);

        var result = screen.OnMouseDown(2f, 2f, MouseButton.Right);
        Assert.Equal(ScreenEvent.None, result);
    }
}
