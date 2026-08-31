using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;
using Xunit;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public class GameSessionScalePanelTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static (SnapshotBuffer, GameSessionScreen) CreateScreen()
    {
        var buffer = new SnapshotBuffer();
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

    private static string ReadLogLines()
    {
        string path = Path.Combine(Environment.CurrentDirectory, InterfaceLog.FileName);
        if (!File.Exists(path))
            return string.Empty;

        // FileShare.ReadWrite — InterfaceLog writers append concurrently (parallel test classes).
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    // ── InterfaceLog ─────────────────────────────────────────────

    [Fact]
    public void InterfaceLog_writes_timestamped_line()
    {
        string marker = "IFACELOG-TEST-" + Guid.NewGuid().ToString("N");

        InterfaceLog.Write(marker);

        string content = ReadLogLines();
        string? line = content
            .Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .FirstOrDefault(l => l.Contains(marker, StringComparison.Ordinal));

        Assert.NotNull(line);
        // Format: [yyyy-MM-dd HH:mm:ss] message
        Assert.Matches(@"^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\] ", line);
    }

    // ── Layout ───────────────────────────────────────────────────

    [Fact]
    public void Scale_panel_renders_left_of_speed_panel()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        var scaleRect = screen.LastScalePanelRect;
        var speedRect = screen.LastSpeedPanelRect;

        Assert.True(scaleRect.Top > ScreenHeight / 2f, "Scale panel should be in the bottom area");
        Assert.True(scaleRect.MidX < ScreenWidth / 2f, "Scale panel should sit left of the row's horizontal center");
        Assert.True(scaleRect.Right < speedRect.Left,
            $"Scale panel right ({scaleRect.Right}) must be left of speed panel ({speedRect.Left})");
        Assert.Equal(speedRect.Top, scaleRect.Top, precision: 3);
    }

    [Fact]
    public void Scale_panel_has_5_buttons_with_correct_labels()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        Assert.Equal(5, screen.ScaleButtonRects.Count);
        Assert.Equal(new[] { "M0.5", "M1", "M10", "M100", "M1000" }, screen.ScalePanelLabels);

        var panel = screen.LastScalePanelRect;
        for (int i = 0; i < screen.ScaleButtonRects.Count; i++)
        {
            var btn = screen.ScaleButtonRects[i];
            Assert.True(btn.Width > 0, $"Button {i} has zero width");
            Assert.True(btn.Height > 0, $"Button {i} has zero height");
            Assert.True(btn.Left >= panel.Left && btn.Right <= panel.Right,
                $"Button {i} outside panel horizontally");
        }
    }

    // ── Clicks set target PPU ────────────────────────────────────

    [Fact]
    public void Click_M0_5_sets_ppu_to_2()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        // Default PPU is 1.0 — click M0.5 to zoom in to the new maximum (PPU = 2.0).
        screen.OnMouseDown(screen.ScaleButtonRects[0].MidX, screen.ScaleButtonRects[0].MidY);

        Assert.Equal(2.0, screen.CameraPixelsPerWorldUnit);
    }

    [Fact]
    public void Click_M1_sets_ppu_to_1()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        // Zoom out first so the click actually changes the scale.
        screen.OnMouseWheel(ScreenWidth / 2f, ScreenHeight / 2f, -1.0f);
        Assert.True(screen.CameraPixelsPerWorldUnit < 1.0);

        screen.OnMouseDown(screen.ScaleButtonRects[1].MidX, screen.ScaleButtonRects[1].MidY);

        Assert.Equal(1.0, screen.CameraPixelsPerWorldUnit);
    }

    [Fact]
    public void Click_M10_sets_ppu_to_0_1()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        screen.OnMouseDown(screen.ScaleButtonRects[2].MidX, screen.ScaleButtonRects[2].MidY);

        Assert.Equal(0.1, screen.CameraPixelsPerWorldUnit);
    }

    [Fact]
    public void Click_M100_sets_ppu_to_0_01()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        screen.OnMouseDown(screen.ScaleButtonRects[3].MidX, screen.ScaleButtonRects[3].MidY);

        Assert.Equal(0.01, screen.CameraPixelsPerWorldUnit);
    }

    [Fact]
    public void Click_M1000_sets_ppu_to_0_001()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        screen.OnMouseDown(screen.ScaleButtonRects[4].MidX, screen.ScaleButtonRects[4].MidY);

        Assert.Equal(0.001, screen.CameraPixelsPerWorldUnit);
    }

    // ── Indicator position ───────────────────────────────────────

    [Fact]
    public void Indicator_under_M0_5_when_ppu_is_exactly_2()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        screen.OnMouseDown(screen.ScaleButtonRects[0].MidX, screen.ScaleButtonRects[0].MidY);
        Assert.Equal(2.0, screen.CameraPixelsPerWorldUnit);

        Assert.Equal(screen.ScaleButtonRects[0].MidX, screen.ScaleIndicatorCenterX);
    }

    [Fact]
    public void Indicator_under_M1_when_ppu_is_exactly_1()
    {
        var (_, screen) = CreateScreen();
        Render(screen); // default PPU = 1.0

        Assert.Equal(screen.ScaleButtonRects[1].MidX, screen.ScaleIndicatorCenterX);
    }

    [Fact]
    public void Indicator_under_M10_when_ppu_is_exactly_0_1()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        screen.OnMouseDown(screen.ScaleButtonRects[2].MidX, screen.ScaleButtonRects[2].MidY);

        Assert.Equal(screen.ScaleButtonRects[2].MidX, screen.ScaleIndicatorCenterX);
    }

    [Fact]
    public void Indicator_between_buttons_when_ppu_is_intermediate()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        // M10 → PPU = 0.1, then one wheel step out → PPU = 0.08
        screen.OnMouseDown(screen.ScaleButtonRects[2].MidX, screen.ScaleButtonRects[2].MidY);
        screen.OnMouseWheel(ScreenWidth / 2f, ScreenHeight / 2f, -1.0f);
        double ppu = screen.CameraPixelsPerWorldUnit;
        Assert.Equal(0.08, ppu, precision: 12);

        float indX = screen.ScaleIndicatorCenterX;
        float lowerX = screen.ScaleButtonRects[2].MidX;
        float upperX = screen.ScaleButtonRects[3].MidX;

        // Piecewise-log position ≈ 2.09691 → interpolate between buttons 2 and 3 (M10, M100).
        Assert.True(indX > lowerX && indX < upperX,
            $"Indicator X={indX} should sit between {lowerX} and {upperX}");

        // frac inside the [M10, M100] decade (×10 step → log span of 1).
        double frac = (Math.Log10(0.1) - Math.Log10(ppu)) / (Math.Log10(0.1) - Math.Log10(0.01));
        float expected = lowerX + (upperX - lowerX) * (float)frac;
        Assert.Equal(expected, indX, precision: 1);
    }

    // ── Logging ──────────────────────────────────────────────────

    [Fact]
    public void Wheel_zoom_logs_to_Interface_log()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        // Zoom out first so PPU is not at the upper boundary (M0.5 = 2.0),
        // otherwise zoom-in won't change PPU and won't log.
        screen.OnMouseWheel(ScreenWidth / 2f, ScreenHeight / 2f, -1.0f);

        // Read only the tail appended after the action to isolate from parallel test writers.
        string logPath = Path.Combine(Environment.CurrentDirectory, InterfaceLog.FileName);
        long lengthBefore = File.Exists(logPath) ? new FileInfo(logPath).Length : 0;

        screen.OnMouseWheel(ScreenWidth / 2f, ScreenHeight / 2f, 1.0f);

        string tail = LogTailReader.ReadTail(lengthBefore);

        Assert.True(tail.Contains("Scale → PPU=", StringComparison.Ordinal),
            "Wheel zoom should append a 'Scale → PPU=' line to Interface.log");
    }

    // ── Isolation ────────────────────────────────────────────────

    [Fact]
    public void Scale_panel_click_does_not_affect_speed()
    {
        var (buffer, screen) = CreateScreen();
        Render(screen);

        // Set Speed2 via speed panel
        screen.OnMouseDown(screen.SpeedButtonRects[2].MidX, screen.SpeedButtonRects[2].MidY);
        Assert.Equal(SimulationSpeed.Speed2, buffer.CurrentSpeed);

        // Click scale button (M10) — speed must stay untouched
        screen.OnMouseDown(screen.ScaleButtonRects[2].MidX, screen.ScaleButtonRects[2].MidY);

        Assert.Equal(SimulationSpeed.Speed2, buffer.CurrentSpeed);
        Assert.Equal(0.1, screen.CameraPixelsPerWorldUnit);
    }

    [Fact]
    public void Scale_buttons_consume_click_and_do_not_pan_map()
    {
        var (_, screen) = CreateScreen();
        Render(screen);

        double fxBefore = screen.CameraFocusX;
        double fyBefore = screen.CameraFocusY;

        screen.OnMouseDown(screen.ScaleButtonRects[3].MidX, screen.ScaleButtonRects[3].MidY);

        Assert.Equal(fxBefore, screen.CameraFocusX);
        Assert.Equal(fyBefore, screen.CameraFocusY);
        Assert.Equal(0.01, screen.CameraPixelsPerWorldUnit);
    }
}
