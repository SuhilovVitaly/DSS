using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed class GameSessionScreen : IScreen
{
    private readonly SnapshotBuffer _buffer;
    private readonly IMotionPredictor _predictor;
    private readonly CameraState _camera;
    private readonly GridRenderer _grid;

    // Object / marker paints
    private readonly SKPaint _objectPaint;
    private readonly SKPaint _centerPaint;
    private readonly SKPaint _markerPaint;

    // Info panel paints
    private readonly SKPaint _panelBgPaint;
    private readonly SKPaint _panelBorderPaint;
    private readonly SKPaint _panelTextPaint;
    private readonly SKPaint _panelLabelPaint;
    private readonly SKPaint _panelClosePaint;

    private int _viewportW;
    private int _viewportH;
    private float _mouseX;
    private float _mouseY;

    // Panel state
    private bool _panelVisible = true;
    private SKRect _lastPanelRect;
    private SKRect _lastCloseRect;

    // Panel layout constants
    private const float PanelPaddingX = 10f;
    private const float PanelPaddingY = 8f;
    private const float PanelLineHeight = 16f;
    private const float PanelFontSize = 12f;
    private const float CloseButtonSize = 14f;
    private const float CloseButtonMargin = 4f;

    // Panel position
    private const float PanelMargin = 8f;

    // ── Test seams ──────────────────────────────────────────────

    /// <summary>Whether the info panel is currently visible.</summary>
    internal bool IsPanelVisible => _panelVisible;

    /// <summary>Current camera focus world X (test seam).</summary>
    internal double CameraFocusX => _camera.FocusX;

    /// <summary>Current camera focus world Y (test seam).</summary>
    internal double CameraFocusY => _camera.FocusY;

    /// <summary>Last panel bounding rectangle (for hit-test verification).</summary>
    internal SKRect LastPanelRect => _lastPanelRect;

    /// <summary>Last close-button bounding rectangle.</summary>
    internal SKRect LastCloseRect => _lastCloseRect;

    // ── Constructor ─────────────────────────────────────────────

    public GameSessionScreen(SnapshotBuffer buffer, IMotionPredictor predictor)
    {
        _buffer = buffer;
        _predictor = predictor;

        _camera = new CameraState(focusX: 10000, focusY: 10000, pixelsPerWorldUnit: 1.0);
        _grid = new GridRenderer();

        _objectPaint = new SKPaint { Color = SKColors.Cyan, Style = SKPaintStyle.Fill, IsAntialias = true };
        _centerPaint = new SKPaint { Color = new SKColor(40, 40, 40), Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        _markerPaint = new SKPaint { Color = new SKColor(220, 220, 60), Style = SKPaintStyle.Fill, IsAntialias = true };

        _panelBgPaint = new SKPaint
        {
            Color = new SKColor(0, 0, 0, 200),
            Style = SKPaintStyle.Fill,
        };

        _panelBorderPaint = new SKPaint
        {
            Color = new SKColor(42, 42, 42),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
        };

        var typeface = SKTypeface.FromFamilyName("Consolas") ?? SKTypeface.Default;

        _panelTextPaint = new SKPaint
        {
            Color = new SKColor(200, 200, 200),
            TextSize = PanelFontSize,
            IsAntialias = true,
            Typeface = typeface,
        };

        _panelLabelPaint = new SKPaint
        {
            Color = new SKColor(140, 140, 140),
            TextSize = PanelFontSize,
            IsAntialias = true,
            Typeface = typeface,
        };

        _panelClosePaint = new SKPaint
        {
            Color = new SKColor(180, 80, 80),
            TextSize = CloseButtonSize,
            IsAntialias = true,
            Typeface = typeface,
            TextAlign = SKTextAlign.Center,
        };
    }

    // ── IScreen ─────────────────────────────────────────────────

    public void OnActivated() { }
    public void OnDeactivated() { }

    public ScreenEvent OnMouseDown(float x, float y)
    {
        // 1. Close button hit → hide panel, consume click
        if (_panelVisible && _lastCloseRect.Contains(x, y))
        {
            _panelVisible = false;
            return ScreenEvent.None;
        }

        // 2. Panel hit (but not close) → consume click, don't move camera
        if (_panelVisible && _lastPanelRect.Contains(x, y))
        {
            return ScreenEvent.None;
        }

        // 3. Map click → pan camera
        var (worldX, worldY) = _camera.ScreenToWorld(x, y, _viewportW, _viewportH);
        _camera.SetFocus(worldX, worldY);
        return ScreenEvent.None;
    }

    public bool OnMouseMove(float x, float y)
    {
        _mouseX = x;
        _mouseY = y;
        return false;
    }

    public ScreenEvent OnKeyDown(Key key)
    {
        // Escape always opens GameMenu
        if (key == Key.Escape)
            return ScreenEvent.OpenGameMenu;

        // Ctrl+I opens the info panel (no-op if already visible)
        if (key == Key.I)
        {
            _panelVisible = true;
            return ScreenEvent.None;
        }

        return ScreenEvent.None;
    }

    // ── Render ──────────────────────────────────────────────────

    public void Render(SKCanvas canvas, int width, int height)
    {
        _viewportW = width;
        _viewportH = height;

        // 1. Draw adaptive world grid (clears background internally)
        _grid.Draw(canvas, _camera, width, height);

        // 2. Static marker at World(10000, 10000)
        var (mx, my) = _camera.WorldToScreen(10000, 10000, width, height);
        canvas.DrawCircle(mx, my, 20, _markerPaint);

        // 3. Crosshair at viewport center
        float cx = width / 2f;
        float cy = height / 2f;
        canvas.DrawLine(cx - 10, cy, cx + 10, cy, _centerPaint);
        canvas.DrawLine(cx, cy - 10, cx, cy + 10, _centerPaint);

        // 4. Engine objects
        var buffered = _buffer.Latest;
        if (buffered is not null)
        {
            long predictionDelta = buffered.PredictionDeltaMs;
            bool isPaused = _buffer.CurrentSpeed == SimulationSpeed.Speed0;
            long effectiveDelta = isPaused ? 0 : predictionDelta;

            foreach (var obj in buffered.Snapshot.Objects)
            {
                var predicted = effectiveDelta > 0
                    ? _predictor.Predict(obj, effectiveDelta)
                    : obj;

                var (sx, sy) = _camera.WorldToScreen(predicted.X, predicted.Y, width, height);
                canvas.DrawCircle(sx, sy, 4, _objectPaint);
            }
        }

        // 5. Info panel — screen-space overlay, drawn last (only when visible)
        if (_panelVisible)
            DrawInfoPanel(canvas, buffered);
    }

    // ── Info panel ──────────────────────────────────────────────

    private void DrawInfoPanel(SKCanvas canvas, BufferedSnapshot? buffered)
    {
        var lines = BuildPanelLines(buffered);

        // Calculate panel size
        float labelWidth = 0;
        float valueWidth = 0;
        foreach (var (label, value) in lines)
        {
            labelWidth = Math.Max(labelWidth, _panelLabelPaint.MeasureText(label));
            valueWidth = Math.Max(valueWidth, _panelTextPaint.MeasureText(value));
        }

        float gap = 8f;
        float extraForClose = CloseButtonSize + CloseButtonMargin;
        float panelW = PanelPaddingX * 2 + labelWidth + gap + valueWidth + extraForClose;
        float panelH = PanelPaddingY * 2 + lines.Count * PanelLineHeight;

        // Position: bottom-left
        float panelX = PanelMargin;
        float panelY = _viewportH - panelH - PanelMargin;

        _lastPanelRect = new SKRect(panelX, panelY, panelX + panelW, panelY + panelH);

        // Close button: right side of panel, vertically centered in the top area
        float closeX = panelX + panelW - CloseButtonMargin - CloseButtonSize / 2f;
        float closeY = panelY + PanelPaddingY + CloseButtonSize - 2f;
        _lastCloseRect = new SKRect(
            closeX - CloseButtonSize / 2f - 2,
            closeY - CloseButtonSize + 2,
            closeX + CloseButtonSize / 2f + 2,
            closeY + 4);

        // Draw background and border
        canvas.DrawRect(_lastPanelRect, _panelBgPaint);
        canvas.DrawRect(_lastPanelRect, _panelBorderPaint);

        // Draw close button
        canvas.DrawText("×", closeX, closeY, _panelClosePaint);

        // Draw text lines
        float textY = panelY + PanelPaddingY + PanelLineHeight - 3f;
        float labelX = panelX + PanelPaddingX;
        float valueX = labelX + labelWidth + gap;

        foreach (var (label, value) in lines)
        {
            canvas.DrawText(label, labelX, textY, _panelLabelPaint);
            canvas.DrawText(value, valueX, textY, _panelTextPaint);
            textY += PanelLineHeight;
        }
    }

    private List<(string Label, string Value)> BuildPanelLines(BufferedSnapshot? buffered)
    {
        var lines = new List<(string Label, string Value)>();

        // Game Time
        if (buffered is not null)
        {
            long ms = buffered.Snapshot.GameTimeMs;
            long sec = ms / 1000;
            string time = $"{sec / 3600:D2}:{(sec % 3600) / 60:D2}:{sec % 60:D2}";
            lines.Add(("Game Time", time));
            lines.Add(("Speed", buffered.Snapshot.CurrentSpeed.ToString()));
        }
        else
        {
            lines.Add(("Game Time", "--:--:--"));
            lines.Add(("Speed", "—"));
        }

        // Cursor window
        lines.Add(("Cursor Window", $"({_mouseX:F0}, {_mouseY:F0})"));

        // Cursor game (world)
        if (_viewportW > 0 && _viewportH > 0)
        {
            var (wx, wy) = _camera.ScreenToWorld(_mouseX, _mouseY, _viewportW, _viewportH);
            lines.Add(("Cursor Game", $"({wx:F0}, {wy:F0})"));
        }
        else
        {
            lines.Add(("Cursor Game", "(—, —)"));
        }

        // Selection / hover (placeholder)
        lines.Add(("Selected Id", "—"));
        lines.Add(("Active Id", "—"));

        // Object count
        int objectCount = buffered?.Snapshot.Objects.Length ?? 0;
        lines.Add(("Celestial objects", objectCount.ToString()));

        return lines;
    }
}
