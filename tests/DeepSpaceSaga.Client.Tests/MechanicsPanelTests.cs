using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Bottom-center Mechanics panel on GameSessionScreen: the "F" button opens Finance
/// (Docs/FirstRelease/Screens/Finance.md) and the "S" button opens Ship
/// (Docs/FirstRelease/Screens/Ship.md). Pause-on-open/resume-on-close is generic
/// SkiaWindow modal behavior (already covered by ModalPauseTests / ModalTransitionTests
/// for Settings/Save/Load) — not re-tested here. The FinanceScreen/ShipScreen overlays
/// themselves are covered in FinanceScreenTests.cs / ShipScreenTests.cs.
/// </summary>
public class MechanicsPanelTests
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
        Assert.True(screen.LastMechanicsPanelRect.Contains(screen.LastFinanceButtonRect));
    }

    [Fact]
    public void Ship_button_rect_is_populated_after_render()
    {
        var screen = CreateScreen();

        Assert.True(screen.LastShipButtonRect.Width == 0);

        RenderScreen(screen);

        Assert.True(screen.LastShipButtonRect.Width > 0);
        Assert.True(screen.LastShipButtonRect.Height > 0);
        Assert.True(screen.LastMechanicsPanelRect.Contains(screen.LastShipButtonRect));
    }

    [Fact]
    public void Finance_and_ship_buttons_sit_side_by_side_without_overlapping()
    {
        var screen = CreateScreen();
        RenderScreen(screen);

        var finance = screen.LastFinanceButtonRect;
        var ship = screen.LastShipButtonRect;

        Assert.Equal(finance.Top, ship.Top);
        Assert.Equal(finance.Bottom, ship.Bottom);
        Assert.True(finance.Right <= ship.Left, "F button should be to the left of the S button");
    }

    [Fact]
    public void Mechanics_panel_is_centered_at_the_bottom_of_the_screen()
    {
        var screen = CreateScreen();
        RenderScreen(screen);

        var rect = screen.LastMechanicsPanelRect;
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

    // ── Click opens Ship ─────────────────────────────────────────

    [Fact]
    public void Click_on_ship_button_returns_OpenShip()
    {
        var screen = CreateScreen();
        RenderScreen(screen);

        float bx = screen.LastShipButtonRect.MidX;
        float by = screen.LastShipButtonRect.MidY;

        var result = screen.OnMouseDown(bx, by);
        Assert.Equal(ScreenEvent.OpenShip, result);
    }

    [Fact]
    public void Click_on_ship_button_does_not_pan_camera()
    {
        var screen = CreateScreen();
        RenderScreen(screen);

        double focusXBefore = screen.CameraFocusX;
        double focusYBefore = screen.CameraFocusY;

        float bx = screen.LastShipButtonRect.MidX;
        float by = screen.LastShipButtonRect.MidY;
        screen.OnMouseDown(bx, by);

        Assert.Equal(focusXBefore, screen.CameraFocusX);
        Assert.Equal(focusYBefore, screen.CameraFocusY);
    }

    // ── Ctrl+F opens Finance / Ctrl+S opens Ship ──────────────────

    [Fact]
    public void Ctrl_F_key_returns_OpenFinance()
    {
        var screen = CreateScreen();

        var result = screen.OnKeyDown(Key.F);
        Assert.Equal(ScreenEvent.OpenFinance, result);
    }

    [Fact]
    public void Ctrl_S_key_returns_OpenShip()
    {
        var screen = CreateScreen();

        var result = screen.OnKeyDown(Key.S);
        Assert.Equal(ScreenEvent.OpenShip, result);
    }

    // ── Escape still works alongside the new buttons ─────────────

    [Fact]
    public void Escape_still_returns_OpenGameMenu_with_mechanics_panel_present()
    {
        var screen = CreateScreen();
        RenderScreen(screen);

        var result = screen.OnKeyDown(Key.Escape);
        Assert.Equal(ScreenEvent.OpenGameMenu, result);
    }
}
