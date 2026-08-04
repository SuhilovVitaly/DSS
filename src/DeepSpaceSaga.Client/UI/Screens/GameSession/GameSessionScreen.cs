using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using System.Diagnostics;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed class GameSessionScreen : IScreen
{
    private readonly SnapshotBuffer _buffer;
    private readonly IMotionPredictor _predictor;
    private readonly CameraState _camera;
    private readonly GridRenderer _grid;
    private readonly ObjectTrailStore _trailStore;
    private readonly List<ObjectRenderState> _renderStates = new();
    private readonly GameSessionHandle? _handle;

    // Object paints
    private readonly SKPaint _trailPaint;
    private readonly SKPaint _objectPaint;
    private readonly SKPaint _playerShipPaint;
    private readonly SKPaint _centerPaint;

    // Shared UI paints
    private readonly SKPaint _panelBgPaint;
    private readonly SKPaint _panelBorderPaint;
    private readonly SKPaint _panelTextPaint;
    private readonly SKPaint _panelLabelPaint;
    private readonly SKPaint _panelClosePaint;

    // Speed panel paints
    private readonly SKPaint _speedBtnNormalPaint;
    private readonly SKPaint _speedBtnActivePaint;
    private readonly SKPaint _speedBtnTextPaint;
    private readonly SKPaint _speedIndicatorPaint;

    private int _viewportW;
    private int _viewportH;
    private float _mouseX;
    private float _mouseY;
    private bool _shouldBootstrapInitialTrails = true;

    // Info panel state
    private bool _panelVisible = true;
    private SKRect _lastPanelRect;
    private SKRect _lastCloseRect;

    // Speed state
    private SimulationSpeed _lastNonPauseSpeed = SimulationSpeed.Speed1;
    private SKRect _lastSpeedPanelRect;
    private readonly SKRect[] _speedButtonRects = new SKRect[5];

    // Camera state
    private bool _isFocusAttachedToPlayer = true;

    // Layout constants
    private const double ZoomStepFactor = 1.25;
    private const float PanelPaddingX = 10f;
    private const float PanelPaddingY = 8f;
    private const float PanelLineHeight = 16f;
    private const float PanelFontSize = 12f;
    private const float CloseButtonSize = 14f;
    private const float CloseButtonMargin = 4f;
    private const float PanelMargin = 8f;

    // Speed panel layout
    private const float SpeedBtnW = 32f;
    private const float SpeedBtnH = 22f;
    private const float SpeedBtnGap = 2f;
    private const float SpeedPanelPadX = 6f;
    private const float SpeedPanelPadY = 4f;
    private const float SpeedIndicatorSize = 8f;

    private static readonly string[] SpeedLabels = { "II", "1x", "5x", "20x", "100x" };
    private static readonly SimulationSpeed[] SpeedValues =
        { SimulationSpeed.Speed0, SimulationSpeed.Speed1, SimulationSpeed.Speed2, SimulationSpeed.Speed3, SimulationSpeed.Speed4 };

    // ── Test seams ──────────────────────────────────────────────

    internal bool IsPanelVisible => _panelVisible;
    internal double CameraFocusX => _camera.FocusX;
    internal double CameraFocusY => _camera.FocusY;
    internal double CameraPixelsPerWorldUnit => _camera.PixelsPerWorldUnit;
    internal SKRect LastPanelRect => _lastPanelRect;
    internal SKRect LastCloseRect => _lastCloseRect;
    internal SimulationSpeed LastNonPauseSpeed => _lastNonPauseSpeed;
    internal SKRect LastSpeedPanelRect => _lastSpeedPanelRect;
    internal IReadOnlyList<SKRect> SpeedButtonRects => _speedButtonRects;
    internal bool IsFocusAttachedToPlayer => _isFocusAttachedToPlayer;
    internal IReadOnlyList<ObjectTrailPoint> GetObjectTrail(string objectId) => _trailStore.GetTrail(objectId);

    // ── Constructor ─────────────────────────────────────────────

    public GameSessionScreen(
        SnapshotBuffer buffer,
        IMotionPredictor predictor,
        GameSessionHandle? handle = null,
        Func<long>? timestampProvider = null)
    {
        _buffer = buffer;
        _predictor = predictor;
        _handle = handle;

        _camera = new CameraState(focusX: 10000, focusY: 10000, pixelsPerWorldUnit: 1.0);
        _grid = new GridRenderer();
        _trailStore = new ObjectTrailStore(_predictor, timestampProvider ?? Stopwatch.GetTimestamp);

        _trailPaint = new SKPaint { Color = new SKColor(190, 190, 190, 160), Style = SKPaintStyle.Stroke, StrokeWidth = 2f, IsAntialias = true };
        _objectPaint = new SKPaint { Color = SKColors.Cyan, Style = SKPaintStyle.Fill, IsAntialias = true };
        _playerShipPaint = new SKPaint { Color = SKColors.LimeGreen, Style = SKPaintStyle.Fill, IsAntialias = true };
        _centerPaint = new SKPaint { Color = new SKColor(40, 40, 40), Style = SKPaintStyle.Stroke, StrokeWidth = 1 };

        _panelBgPaint = new SKPaint { Color = new SKColor(0, 0, 0, 200), Style = SKPaintStyle.Fill };
        _panelBorderPaint = new SKPaint { Color = new SKColor(42, 42, 42), Style = SKPaintStyle.Stroke, StrokeWidth = 1f };

        var typeface = SKTypeface.FromFamilyName("Consolas") ?? SKTypeface.Default;

        _panelTextPaint = new SKPaint { Color = new SKColor(200, 200, 200), TextSize = PanelFontSize, IsAntialias = true, Typeface = typeface };
        _panelLabelPaint = new SKPaint { Color = new SKColor(140, 140, 140), TextSize = PanelFontSize, IsAntialias = true, Typeface = typeface };
        _panelClosePaint = new SKPaint { Color = new SKColor(180, 80, 80), TextSize = CloseButtonSize, IsAntialias = true, Typeface = typeface, TextAlign = SKTextAlign.Center };

        _speedBtnNormalPaint = new SKPaint { Color = new SKColor(30, 30, 30), Style = SKPaintStyle.Fill };
        _speedBtnActivePaint = new SKPaint { Color = new SKColor(50, 60, 50), Style = SKPaintStyle.Fill };
        _speedBtnTextPaint = new SKPaint { Color = new SKColor(180, 180, 180), TextSize = 11f, IsAntialias = true, Typeface = typeface, TextAlign = SKTextAlign.Center };
        _speedIndicatorPaint = new SKPaint { Color = new SKColor(80, 200, 80), Style = SKPaintStyle.Fill, IsAntialias = true };
    }

    // ── IScreen ─────────────────────────────────────────────────

    public void OnActivated() { }
    public void OnDeactivated() { }

    public ScreenEvent OnMouseDown(float x, float y)
    {
        // 1. Speed panel buttons
        int speedIdx = HitTestSpeedPanel(x, y);
        if (speedIdx >= 0)
        {
            ApplySpeed(SpeedValues[speedIdx]);
            return ScreenEvent.None;
        }

        // 2. Info panel close button
        if (_panelVisible && _lastCloseRect.Contains(x, y))
        {
            _panelVisible = false;
            return ScreenEvent.None;
        }

        // 3. Info panel (consume, don't pan)
        if (_panelVisible && _lastPanelRect.Contains(x, y))
        {
            return ScreenEvent.None;
        }

        // 4. Map click → pan camera
        var (worldX, worldY) = _camera.ScreenToWorld(x, y, _viewportW, _viewportH);
        _isFocusAttachedToPlayer = false;
        _camera.SetFocus(worldX, worldY);
        return ScreenEvent.None;
    }

    public bool OnMouseMove(float x, float y)
    {
        _mouseX = x;
        _mouseY = y;
        return false;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        if (delta == 0 || _viewportW <= 0 || _viewportH <= 0)
            return ScreenEvent.None;

        double factor = delta > 0 ? ZoomStepFactor : 1.0 / ZoomStepFactor;
        _camera.ZoomAt(factor, x, y, _viewportW, _viewportH);
        return ScreenEvent.None;
    }

    public ScreenEvent OnKeyDown(Key key)
    {
        if (key == Key.Escape)
            return ScreenEvent.OpenGameMenu;

        if (key == Key.I)
        {
            _panelVisible = true;
            return ScreenEvent.None;
        }

        if (key == Key.Space)
        {
            TogglePause();
            return ScreenEvent.None;
        }

        return ScreenEvent.None;
    }

    // ── Speed control ───────────────────────────────────────────

    private void TogglePause()
    {
        var current = _buffer.CurrentSpeed;
        if (current == SimulationSpeed.Speed0)
        {
            ApplySpeed(_lastNonPauseSpeed);
        }
        else
        {
            _lastNonPauseSpeed = current;
            ApplySpeed(SimulationSpeed.Speed0);
        }
    }

    private void ApplySpeed(SimulationSpeed speed)
    {
        if (speed != SimulationSpeed.Speed0)
            _lastNonPauseSpeed = speed;

        if (_handle is not null)
            _ = _handle.SetSpeedAsync(speed);
        else
            _buffer.CurrentSpeed = speed; // fallback for tests
    }

    private int HitTestSpeedPanel(float x, float y)
    {
        for (int i = 0; i < _speedButtonRects.Length; i++)
        {
            if (_speedButtonRects[i].Contains(x, y))
                return i;
        }
        return -1;
    }

    // ── Render ──────────────────────────────────────────────────

    public void Render(SKCanvas canvas, int width, int height)
    {
        _viewportW = width;
        _viewportH = height;

        var prediction = _buffer.LatestPrediction;
        var buffered = prediction?.BufferedSnapshot;
        UpdateObjectRenderStates(prediction);

        UpdateCameraFocusFromPlayer(_renderStates);

        // 1. Grid
        _grid.Draw(canvas, _camera, width, height);

        // 2. Crosshair
        float cx = width / 2f;
        float cy = height / 2f;
        canvas.DrawLine(cx - 10, cy, cx + 10, cy, _centerPaint);
        canvas.DrawLine(cx, cy - 10, cx, cy + 10, _centerPaint);

        // 3. Object trails
        if (prediction is not null)
        {
            _trailStore.Update(
                _renderStates,
                prediction.CurrentSpeed,
                GetPredictedGameTimeMs(prediction),
                _shouldBootstrapInitialTrails);
            _shouldBootstrapInitialTrails = false;

            DrawObjectTrails(canvas, width, height);

            // 4. Engine objects
            foreach (var state in _renderStates)
            {
                var (sx, sy) = _camera.WorldToScreen(state.Predicted.X, state.Predicted.Y, width, height);
                var paint = state.IsPlayerShip ? _playerShipPaint : _objectPaint;
                canvas.DrawCircle(sx, sy, 4, paint);
            }
        }

        // 5. Speed panel (top-right)
        DrawSpeedPanel(canvas);

        // 6. Info panel (bottom-left)
        if (_panelVisible)
            DrawInfoPanel(canvas, buffered);
    }

    // ── Speed panel ─────────────────────────────────────────────

    private void UpdateObjectRenderStates(SnapshotPrediction? prediction)
    {
        _renderStates.Clear();

        if (prediction is null)
            return;

        long ed = prediction.EffectivePredictionDeltaMs;
        string? playerShipObjectId = prediction.BufferedSnapshot.Snapshot.PlayerShipObjectId;

        foreach (var obj in prediction.BufferedSnapshot.Snapshot.Objects)
        {
            var predicted = ed > 0 ? _predictor.Predict(obj, ed) : obj;
            _renderStates.Add(new ObjectRenderState(obj, predicted, obj.ObjectId == playerShipObjectId));
        }
    }

    private void UpdateCameraFocusFromPlayer(IReadOnlyList<ObjectRenderState> renderStates)
    {
        if (!_isFocusAttachedToPlayer)
            return;

        for (int i = 0; i < renderStates.Count; i++)
        {
            var state = renderStates[i];
            if (!state.IsPlayerShip)
                continue;

            _camera.SetFocus(state.Predicted.X, state.Predicted.Y);
            return;
        }
    }

    private static long GetPredictedGameTimeMs(SnapshotPrediction prediction)
    {
        return prediction.BufferedSnapshot.Snapshot.GameTimeMs + prediction.EffectivePredictionDeltaMs;
    }

    private void DrawObjectTrails(SKCanvas canvas, int width, int height)
    {
        foreach (var points in _trailStore.Trails)
        {
            if (points.Count < 2)
                continue;

            for (int i = 1; i < points.Count; i++)
            {
                var from = points[i - 1];
                var to = points[i];
                var (fromX, fromY) = _camera.WorldToScreen(from.X, from.Y, width, height);
                var (toX, toY) = _camera.WorldToScreen(to.X, to.Y, width, height);

                float t = (float)i / (points.Count - 1);
                byte alpha = (byte)(40 + 120 * t);
                _trailPaint.Color = new SKColor(190, 190, 190, alpha);
                canvas.DrawLine(fromX, fromY, toX, toY, _trailPaint);
            }
        }
    }

    private void DrawSpeedPanel(SKCanvas canvas)
    {
        int btnCount = SpeedLabels.Length;
        float totalW = SpeedPanelPadX * 2 + btnCount * SpeedBtnW + (btnCount - 1) * SpeedBtnGap;
        float panelH = SpeedPanelPadY * 2 + SpeedBtnH + SpeedIndicatorSize + 2f;

        float panelX = _viewportW - totalW - PanelMargin;
        float panelY = PanelMargin;

        _lastSpeedPanelRect = new SKRect(panelX, panelY, panelX + totalW, panelY + panelH);
        canvas.DrawRect(_lastSpeedPanelRect, _panelBgPaint);
        canvas.DrawRect(_lastSpeedPanelRect, _panelBorderPaint);

        float btnX = panelX + SpeedPanelPadX;
        float btnY = panelY + SpeedPanelPadY;

        var currentSpeed = _buffer.CurrentSpeed;
        int activeIdx = Array.IndexOf(SpeedValues, currentSpeed);
        if (activeIdx < 0) activeIdx = 0;

        for (int i = 0; i < btnCount; i++)
        {
            var btnRect = new SKRect(btnX, btnY, btnX + SpeedBtnW, btnY + SpeedBtnH);
            _speedButtonRects[i] = btnRect;

            bool isActive = (i == activeIdx);
            canvas.DrawRect(btnRect, isActive ? _speedBtnActivePaint : _speedBtnNormalPaint);
            canvas.DrawRect(btnRect, _panelBorderPaint);

            float textY = btnY + SpeedBtnH / 2f + _speedBtnTextPaint.TextSize / 3f;
            canvas.DrawText(SpeedLabels[i], btnX + SpeedBtnW / 2f, textY, _speedBtnTextPaint);

            // Green indicator under active button
            if (isActive)
            {
                float indX = btnX + SpeedBtnW / 2f - SpeedIndicatorSize / 2f;
                float indY = btnY + SpeedBtnH + 1f;
                var path = new SKPath();
                path.MoveTo(indX, indY);
                path.LineTo(indX + SpeedIndicatorSize, indY);
                path.LineTo(indX + SpeedIndicatorSize / 2f, indY + SpeedIndicatorSize);
                path.Close();
                canvas.DrawPath(path, _speedIndicatorPaint);
            }

            btnX += SpeedBtnW + SpeedBtnGap;
        }
    }

    // ── Info panel ──────────────────────────────────────────────

    private void DrawInfoPanel(SKCanvas canvas, BufferedSnapshot? buffered)
    {
        var lines = BuildPanelLines(buffered);

        float labelWidth = 0, valueWidth = 0;
        foreach (var (label, value) in lines)
        {
            labelWidth = Math.Max(labelWidth, _panelLabelPaint.MeasureText(label));
            valueWidth = Math.Max(valueWidth, _panelTextPaint.MeasureText(value));
        }

        float gap = 8f;
        float extraForClose = CloseButtonSize + CloseButtonMargin;
        float panelW = PanelPaddingX * 2 + labelWidth + gap + valueWidth + extraForClose;
        float panelH = PanelPaddingY * 2 + lines.Count * PanelLineHeight;

        float panelX = PanelMargin;
        float panelY = _viewportH - panelH - PanelMargin;

        _lastPanelRect = new SKRect(panelX, panelY, panelX + panelW, panelY + panelH);

        float closeX = panelX + panelW - CloseButtonMargin - CloseButtonSize / 2f;
        float closeY = panelY + PanelPaddingY + CloseButtonSize - 2f;
        _lastCloseRect = new SKRect(closeX - 9, closeY - 16, closeX + 9, closeY + 4);

        canvas.DrawRect(_lastPanelRect, _panelBgPaint);
        canvas.DrawRect(_lastPanelRect, _panelBorderPaint);
        canvas.DrawText("×", closeX, closeY, _panelClosePaint);

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

    internal List<(string Label, string Value)> BuildPanelLines(BufferedSnapshot? buffered)
    {
        var lines = new List<(string Label, string Value)>();

        if (buffered is not null)
        {
            long ms = buffered.Snapshot.GameTimeMs;
            long sec = ms / 1000;
            lines.Add(("Game Time", $"{sec / 3600:D2}:{(sec % 3600) / 60:D2}:{sec % 60:D2}"));
            lines.Add(("Speed", buffered.Snapshot.CurrentSpeed.ToString()));
        }
        else
        {
            lines.Add(("Game Time", "--:--:--"));
            lines.Add(("Speed", "—"));
        }

        lines.Add(("Cursor Window", $"({_mouseX:F0}, {_mouseY:F0})"));
        lines.Add(("Scale", $"{_camera.PixelsPerWorldUnit:0.####} px/unit"));

        if (_viewportW > 0 && _viewportH > 0)
        {
            var (wx, wy) = _camera.ScreenToWorld(_mouseX, _mouseY, _viewportW, _viewportH);
            lines.Add(("Cursor Game", $"({wx:F0}, {wy:F0})"));
        }
        else
        {
            lines.Add(("Cursor Game", "(—, —)"));
        }

        lines.Add(("Selected Id", "—"));
        lines.Add(("Active Id", "—"));

        int objectCount = buffered?.Snapshot.Objects.Length ?? 0;
        lines.Add(("Celestial objects", objectCount.ToString()));

        return lines;
    }

}

internal readonly record struct ObjectRenderState(
    ObjectMotionSnapshot Source,
    ObjectMotionSnapshot Predicted,
    bool IsPlayerShip);
