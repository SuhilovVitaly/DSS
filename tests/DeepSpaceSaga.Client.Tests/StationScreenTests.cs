using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Station;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// The Station overlay screen itself (opened from GameSessionScreen by left-clicking
/// the station the player ship is docked to — see
/// GameSessionObjectInteractionTests's docked-station-click tests). Placeholder shell:
/// Representatives/Install Drilling Unit/Undock aren't in the Engine yet, so there's no
/// real station data to assert on for those; `Trade`, `Hire`, `Finance` and `Contracts`
/// are real buttons — `Trade`/`Hire`/`Contracts` open their own stub screens
/// (TradeScreenTests/HireScreenTests/ContractsScreenTests), `Finance` opens the
/// pre-existing FinanceScreen (FinanceScreenTests). Structural twin of FinanceScreenTests.
/// </summary>
public class StationScreenTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static void RenderScreen(StationScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    [Fact]
    public void Escape_returns_CloseStation()
    {
        var screen = new StationScreen();
        var result = screen.OnKeyDown(Key.Escape);
        Assert.Equal(ScreenEvent.CloseStation, result);
    }

    [Fact]
    public void Exit_button_click_returns_CloseStation()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var local = StationToolbar.ExitButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = StationLayout.PanelTop(ScreenHeight) + local.MidY;

        var result = screen.OnMouseDown(cx, cy);
        Assert.Equal(ScreenEvent.CloseStation, result);
    }

    [Fact]
    public void Hovering_the_exit_button_reports_interactive()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var local = StationToolbar.ExitButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = StationLayout.PanelTop(ScreenHeight) + local.MidY;

        Assert.True(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Trade_button_click_returns_OpenTrade()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var hit = StationLayout.HitTest(
            StationLayout.PanelLeft(ScreenWidth) + StationLayout.TradeButtonLocalRect().Left + 1f,
            StationLayout.PanelTop(ScreenHeight) + StationLayout.TradeButtonLocalRect().Top + 1f,
            ScreenWidth, ScreenHeight);
        Assert.Equal(StationButton.Trade, hit);

        var (left, top, right, bottom) = StationLayout.TradeButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        var result = screen.OnMouseDown(cx, cy);
        Assert.Equal(ScreenEvent.OpenTrade, result);
    }

    [Fact]
    public void Trade_button_hover_is_reported_interactive()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var (left, top, right, bottom) = StationLayout.TradeButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        Assert.True(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Hire_button_click_returns_OpenHire()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var hit = StationLayout.HitTest(
            StationLayout.PanelLeft(ScreenWidth) + StationLayout.HireButtonLocalRect().Left + 1f,
            StationLayout.PanelTop(ScreenHeight) + StationLayout.HireButtonLocalRect().Top + 1f,
            ScreenWidth, ScreenHeight);
        Assert.Equal(StationButton.Hire, hit);

        var (left, top, right, bottom) = StationLayout.HireButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        var result = screen.OnMouseDown(cx, cy);
        Assert.Equal(ScreenEvent.OpenHire, result);
    }

    [Fact]
    public void Hire_button_hover_is_reported_interactive()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var (left, top, right, bottom) = StationLayout.HireButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        Assert.True(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Finance_button_click_returns_OpenFinance()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var hit = StationLayout.HitTest(
            StationLayout.PanelLeft(ScreenWidth) + StationLayout.FinanceButtonLocalRect().Left + 1f,
            StationLayout.PanelTop(ScreenHeight) + StationLayout.FinanceButtonLocalRect().Top + 1f,
            ScreenWidth, ScreenHeight);
        Assert.Equal(StationButton.Finance, hit);

        var (left, top, right, bottom) = StationLayout.FinanceButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        var result = screen.OnMouseDown(cx, cy);
        Assert.Equal(ScreenEvent.OpenFinance, result);
    }

    [Fact]
    public void Finance_button_hover_is_reported_interactive()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var (left, top, right, bottom) = StationLayout.FinanceButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        Assert.True(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Contracts_button_click_returns_OpenContracts()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var hit = StationLayout.HitTest(
            StationLayout.PanelLeft(ScreenWidth) + StationLayout.ContractsButtonLocalRect().Left + 1f,
            StationLayout.PanelTop(ScreenHeight) + StationLayout.ContractsButtonLocalRect().Top + 1f,
            ScreenWidth, ScreenHeight);
        Assert.Equal(StationButton.Contracts, hit);

        var (left, top, right, bottom) = StationLayout.ContractsButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        var result = screen.OnMouseDown(cx, cy);
        Assert.Equal(ScreenEvent.OpenContracts, result);
    }

    [Fact]
    public void Contracts_button_hover_is_reported_interactive()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var (left, top, right, bottom) = StationLayout.ContractsButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        Assert.True(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Trade_Hire_Finance_and_Contracts_buttons_do_not_overlap()
    {
        var buttons = new[]
        {
            StationLayout.TradeButtonLocalRect(),
            StationLayout.HireButtonLocalRect(),
            StationLayout.FinanceButtonLocalRect(),
            StationLayout.ContractsButtonLocalRect(),
        };

        for (int i = 0; i < buttons.Length; i++)
        {
            for (int j = i + 1; j < buttons.Length; j++)
            {
                var a = buttons[i];
                var b = buttons[j];
                Assert.True(a.Bottom <= b.Top || b.Bottom <= a.Top);
            }
        }
    }

    [Fact]
    public void Click_inside_panel_outside_close_button_returns_None()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        float px = StationLayout.PanelLeft(ScreenWidth) + StationLayout.PanelWidth / 2f;
        float py = StationLayout.PanelTop(ScreenHeight) + StationLayout.PanelHeight / 2f;

        var result = screen.OnMouseDown(px, py);
        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void Click_outside_panel_returns_CloseStation()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        // Top-left corner of the screen — well outside the centered panel.
        var result = screen.OnMouseDown(2f, 2f);
        Assert.Equal(ScreenEvent.CloseStation, result);
    }

    [Fact]
    public void Right_click_outside_panel_does_not_close()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var result = screen.OnMouseDown(2f, 2f, MouseButton.Right);
        Assert.Equal(ScreenEvent.None, result);
    }
}
