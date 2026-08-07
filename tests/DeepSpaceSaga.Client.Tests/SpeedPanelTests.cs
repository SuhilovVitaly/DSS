using System.Collections.Immutable;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class SpeedPanelTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static (SnapshotBuffer, GameSessionScreen) CreateScreen()
    {
        var buffer = new SnapshotBuffer();
        // Simulate the first authoritative snapshot from a scenario (Speed0, GameTime=0)
        buffer.Update(new AuthoritativeSnapshot(0, 0, SimulationSpeed.Speed0,
            System.Collections.Immutable.ImmutableArray<ObjectMotionSnapshot>.Empty));
        var predictor = new LinearMotionPredictor();
        var screen = new GameSessionScreen(buffer, predictor);
        return (buffer, screen);
    }

    private static void Render(GameSessionScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    // ── Start state ─────────────────────────────────────────────

    [Fact]
    public void Start_state_is_Speed0()
    {
        var (buffer, _) = CreateScreen();
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);
    }

    [Fact]
    public void Start_LastNonPauseSpeed_is_Speed1()
    {
        var (_, screen) = CreateScreen();
        Assert.Equal(SimulationSpeed.Speed1, screen.LastNonPauseSpeed);
    }

    // ── Speed buttons ───────────────────────────────────────────

    [Fact]
    public void Click_Speed1_sets_Speed1()
    {
        var (buffer, screen) = CreateScreen();
        Render(screen);

        var btn = screen.SpeedButtonRects[1]; // Speed1
        screen.OnMouseDown(btn.MidX, btn.MidY);

        Assert.Equal(SimulationSpeed.Speed1, buffer.CurrentSpeed);
    }

    [Fact]
    public void Click_Speed4_sets_Speed4()
    {
        var (buffer, screen) = CreateScreen();
        Render(screen);

        var btn = screen.SpeedButtonRects[4]; // Speed4
        screen.OnMouseDown(btn.MidX, btn.MidY);

        Assert.Equal(SimulationSpeed.Speed4, buffer.CurrentSpeed);
    }

    [Fact]
    public void Click_Pause_sets_Speed0()
    {
        var (buffer, screen) = CreateScreen();
        Render(screen);

        // First set Speed1
        screen.OnMouseDown(screen.SpeedButtonRects[1].MidX, screen.SpeedButtonRects[1].MidY);
        Assert.Equal(SimulationSpeed.Speed1, buffer.CurrentSpeed);

        // Then click Pause
        screen.OnMouseDown(screen.SpeedButtonRects[0].MidX, screen.SpeedButtonRects[0].MidY);
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);
    }

    [Fact]
    public void Click_Pause_preserves_LastNonPauseSpeed()
    {
        var (buffer, screen) = CreateScreen();
        Render(screen);

        // Set Speed3
        screen.OnMouseDown(screen.SpeedButtonRects[3].MidX, screen.SpeedButtonRects[3].MidY);
        Assert.Equal(SimulationSpeed.Speed3, screen.LastNonPauseSpeed);

        // Click Pause
        screen.OnMouseDown(screen.SpeedButtonRects[0].MidX, screen.SpeedButtonRects[0].MidY);
        Assert.Equal(SimulationSpeed.Speed3, screen.LastNonPauseSpeed); // unchanged
    }

    // ── Number keys 1–5 ─────────────────────────────────────────

    [Fact]
    public void Key_1_sets_Speed0()
    {
        var (buffer, screen) = CreateScreen();
        Render(screen);

        // Set Speed2 first, then press 1 → pause
        screen.OnKeyDown(Key.Number3);
        Assert.Equal(SimulationSpeed.Speed2, buffer.CurrentSpeed);

        screen.OnKeyDown(Key.Number1);
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);
    }

    [Fact]
    public void Key_2_sets_Speed1()
    {
        var (buffer, screen) = CreateScreen();
        Render(screen);

        screen.OnKeyDown(Key.Number2);

        Assert.Equal(SimulationSpeed.Speed1, buffer.CurrentSpeed);
    }

    [Fact]
    public void Key_3_sets_Speed2()
    {
        var (buffer, screen) = CreateScreen();
        Render(screen);

        screen.OnKeyDown(Key.Number3);

        Assert.Equal(SimulationSpeed.Speed2, buffer.CurrentSpeed);
    }

    [Fact]
    public void Key_4_sets_Speed3()
    {
        var (buffer, screen) = CreateScreen();
        Render(screen);

        screen.OnKeyDown(Key.Number4);

        Assert.Equal(SimulationSpeed.Speed3, buffer.CurrentSpeed);
    }

    [Fact]
    public void Key_5_sets_Speed4()
    {
        var (buffer, screen) = CreateScreen();
        Render(screen);

        screen.OnKeyDown(Key.Number5);

        Assert.Equal(SimulationSpeed.Speed4, buffer.CurrentSpeed);
    }

    [Fact]
    public void Key_2_then_Space_pause_and_resume()
    {
        var (buffer, screen) = CreateScreen();
        Render(screen);

        screen.OnKeyDown(Key.Number2); // Speed1
        Assert.Equal(SimulationSpeed.Speed1, buffer.CurrentSpeed);

        screen.OnKeyDown(Key.Space); // pause
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);
        Assert.Equal(SimulationSpeed.Speed1, screen.LastNonPauseSpeed);

        screen.OnKeyDown(Key.Space); // resume
        Assert.Equal(SimulationSpeed.Speed1, buffer.CurrentSpeed);
    }

    [Theory]
    [InlineData(Key.Number1)]
    [InlineData(Key.Number2)]
    [InlineData(Key.Number3)]
    [InlineData(Key.Number4)]
    [InlineData(Key.Number5)]
    public void Keys_1_to_5_return_ScreenEvent_None(Key key)
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(key));
    }

    // ── Space key ───────────────────────────────────────────────

    [Fact]
    public void Space_on_Speed2_pauses()
    {
        var (buffer, screen) = CreateScreen();
        Render(screen);

        screen.OnMouseDown(screen.SpeedButtonRects[2].MidX, screen.SpeedButtonRects[2].MidY);
        Assert.Equal(SimulationSpeed.Speed2, buffer.CurrentSpeed);

        screen.OnKeyDown(Key.Space);
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);
        Assert.Equal(SimulationSpeed.Speed2, screen.LastNonPauseSpeed);
    }

    [Fact]
    public void Space_on_pause_resumes_to_LastNonPauseSpeed()
    {
        var (buffer, screen) = CreateScreen();
        Render(screen);

        // Set Speed4, then pause
        screen.OnMouseDown(screen.SpeedButtonRects[4].MidX, screen.SpeedButtonRects[4].MidY);
        screen.OnKeyDown(Key.Space); // pause
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);

        // Resume
        screen.OnKeyDown(Key.Space);
        Assert.Equal(SimulationSpeed.Speed4, buffer.CurrentSpeed);
    }

    [Fact]
    public void Space_on_start_pause_resumes_to_Speed1()
    {
        var (buffer, screen) = CreateScreen();
        Render(screen);

        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);
        Assert.Equal(SimulationSpeed.Speed1, screen.LastNonPauseSpeed);

        screen.OnKeyDown(Key.Space);
        Assert.Equal(SimulationSpeed.Speed1, buffer.CurrentSpeed);
    }

    // ── Speed panel layout ──────────────────────────────────────

    [Fact]
    public void Speed_panel_renders_in_top_right()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        var rect = screen.LastSpeedPanelRect;
        Assert.True(rect.Left > ScreenWidth / 2f, "Should be in right half");
        Assert.True(rect.Top < ScreenHeight / 2f, "Should be in top half");
    }

    [Fact]
    public void Speed_panel_has_5_buttons()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        Assert.Equal(5, screen.SpeedButtonRects.Count);
        for (int i = 0; i < 5; i++)
            Assert.True(screen.SpeedButtonRects[i].Width > 0, $"Button {i} has zero width");
    }

    // ── Click on speed panel does not pan ───────────────────────

    [Fact]
    public void Click_on_speed_panel_does_not_pan_camera()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        double fxBefore = screen.CameraFocusX;
        double fyBefore = screen.CameraFocusY;

        // Click Speed1 button
        var btn = screen.SpeedButtonRects[1];
        screen.OnMouseDown(btn.MidX, btn.MidY);

        Assert.Equal(fxBefore, screen.CameraFocusX);
        Assert.Equal(fyBefore, screen.CameraFocusY);
    }

    // ── Escape and Ctrl+I still work ────────────────────────────

    [Fact]
    public void Escape_still_returns_OpenGameMenu()
    {
        var (_, screen) = CreateScreen();
        Assert.Equal(ScreenEvent.OpenGameMenu, screen.OnKeyDown(Key.Escape));
    }

    [Fact]
    public void Ctrl_I_still_opens_info_panel()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        // Hide panel
        screen.OnMouseDown(screen.LastCloseRect.MidX, screen.LastCloseRect.MidY);
        Assert.False(screen.IsPanelVisible);

        // Ctrl+I reopens
        screen.OnKeyDown(Key.I);
        Assert.True(screen.IsPanelVisible);
    }

    // ── Green indicator position ────────────────────────────────

    [Fact]
    public void Active_speed_indicator_under_correct_button()
    {
        var (buffer, screen) = CreateScreen();
        Render(screen);

        // Default: Speed0 → indicator under button 0 (Pause)
        Assert.Equal(SimulationSpeed.Speed0, buffer.CurrentSpeed);

        // Click Speed3
        screen.OnMouseDown(screen.SpeedButtonRects[3].MidX, screen.SpeedButtonRects[3].MidY);
        Assert.Equal(SimulationSpeed.Speed3, buffer.CurrentSpeed);
    }

    [Fact]
    public void Space_returns_ScreenEvent_None()
    {
        var (_, screen) = CreateScreen();
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Space));
    }
}
