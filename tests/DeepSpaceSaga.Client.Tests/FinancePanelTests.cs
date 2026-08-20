using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Finance;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Bottom-center Finance panel button on GameSessionScreen, and the FinanceScreen
/// overlay it opens (Docs/FirstRelease/Screens/Finance.md). Pause-on-open/resume-on-close
/// is generic SkiaWindow modal behavior (already covered by ModalPauseTests /
/// ModalTransitionTests for Settings/Save/Load) — not re-tested here.
/// </summary>
public class FinancePanelTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static GameSessionScreen CreateScreen()
    {
        var buffer = new SnapshotBuffer();
        var predictor = new LinearMotionPredictor();
        return new GameSessionScreen(buffer, predictor);
    }

    private static void RenderScreen(GameSessionScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    // ── Layout ───────────────────────────────────────────────────

    [Fact]
    public void Finance_button_rect_is_populated_after_render()
    {
        var screen = CreateScreen();

        Assert.True(screen.LastFinanceButtonRect.Width == 0);

        RenderScreen(screen);

        Assert.True(screen.LastFinanceButtonRect.Width > 0);
        Assert.True(screen.LastFinanceButtonRect.Height > 0);
        Assert.True(screen.LastFinancePanelRect.Contains(screen.LastFinanceButtonRect));
    }

    [Fact]
    public void Finance_panel_is_centered_at_the_bottom_of_the_screen()
    {
        var screen = CreateScreen();
        RenderScreen(screen);

        var rect = screen.LastFinancePanelRect;
        float expectedCenterX = ScreenWidth / 2f;

        Assert.InRange(rect.MidX, expectedCenterX - 1f, expectedCenterX + 1f);
        Assert.True(rect.Bottom < ScreenHeight, "Panel must sit above the screen's bottom edge, not touch it");
        Assert.True(rect.Bottom > ScreenHeight - 60f, "Panel should hug the bottom of the screen");
    }

    // ── Click opens Finance ──────────────────────────────────────

    [Fact]
    public void Click_on_finance_button_returns_OpenFinance()
    {
        var screen = CreateScreen();
        RenderScreen(screen);

        float bx = screen.LastFinanceButtonRect.MidX;
        float by = screen.LastFinanceButtonRect.MidY;

        var result = screen.OnMouseDown(bx, by);
        Assert.Equal(ScreenEvent.OpenFinance, result);
    }

    [Fact]
    public void Click_on_finance_button_does_not_pan_camera()
    {
        var screen = CreateScreen();
        RenderScreen(screen);

        double focusXBefore = screen.CameraFocusX;
        double focusYBefore = screen.CameraFocusY;

        float bx = screen.LastFinanceButtonRect.MidX;
        float by = screen.LastFinanceButtonRect.MidY;
        screen.OnMouseDown(bx, by);

        Assert.Equal(focusXBefore, screen.CameraFocusX);
        Assert.Equal(focusYBefore, screen.CameraFocusY);
    }

    // ── Ctrl+F opens Finance ─────────────────────────────────────

    [Fact]
    public void Ctrl_F_key_returns_OpenFinance()
    {
        var screen = CreateScreen();

        var result = screen.OnKeyDown(Key.F);
        Assert.Equal(ScreenEvent.OpenFinance, result);
    }

    // ── Escape still works alongside the new button ─────────────

    [Fact]
    public void Escape_still_returns_OpenGameMenu_with_finance_panel_present()
    {
        var screen = CreateScreen();
        RenderScreen(screen);

        var result = screen.OnKeyDown(Key.Escape);
        Assert.Equal(ScreenEvent.OpenGameMenu, result);
    }
}

/// <summary>
/// The Finance overlay screen itself (opened by GameSessionScreen above). Placeholder
/// shell only — Money/Trading/StationInventory mechanics aren't in the Engine yet, so
/// there's no real financial data to assert on, just the open/close mechanics.
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
    public void Click_outside_close_button_returns_None()
    {
        var screen = new FinanceScreen();
        RenderScreen(screen);

        float px = FinanceLayout.PanelLeft(ScreenWidth) + FinanceLayout.PanelWidth / 2f;
        float py = FinanceLayout.PanelTop(ScreenHeight) + FinanceLayout.PanelHeight / 2f;

        var result = screen.OnMouseDown(px, py);
        Assert.Equal(ScreenEvent.None, result);
    }
}
