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
    private readonly FutureTrajectoryProjector _futureTrajectoryProjector;
    private readonly ObjectLabelRenderer _labelRenderer;
    private readonly List<ObjectRenderState> _renderStates = new();
    private readonly Dictionary<string, ObjectMotionSnapshot> _pausedVisualAnchors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, VisualCorrection> _visualCorrections = new(StringComparer.Ordinal);
    private readonly HashSet<string> _currentVisualObjectIds = new(StringComparer.Ordinal);
    private readonly List<string> _visualObjectIdsToRemove = new();
    private readonly HashSet<string> _initialTrailBootstrapObjectIds = new(StringComparer.Ordinal);
    private readonly GameSessionHandle? _handle;
    private readonly Func<long> _timestampProvider;

    // Object paints
    private readonly SKPaint _trailPaint;
    private readonly SKPaint _futureTrajectoryPaint;
    private readonly SKPaint _objectPaint;
    private readonly SKPaint _playerShipPaint;
    private readonly SKPaint _playerShipOutlinePaint;
    private readonly SKPaint _centerPaint;
    private readonly SKPath _playerShipGlyphPath = new();

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

    // Scale panel paints
    private readonly SKPaint _scaleBtnNormalPaint;
    private readonly SKPaint _scaleBtnActivePaint;
    private readonly SKPaint _scaleBtnTextPaint;
    private readonly SKPaint _scaleIndicatorPaint;

    // Ship command panel paints
    private readonly SKPaint _commandBtnNormalPaint;
    private readonly SKPaint _commandBtnHoverPaint;
    private readonly SKPaint _commandBtnPressedPaint;
    private readonly SKPaint _commandBtnDisabledPaint;
    private readonly SKPaint _commandBtnTextPaint;

    private int _viewportW;
    private int _viewportH;
    private int _lastViewportW;
    private int _lastViewportH;
    private float _mouseX;
    private float _mouseY;
    private bool _shouldBootstrapInitialTrails = true;
    private bool _capturedInitialTrailBootstrapObjects;
    private long _lastFrameTimestamp;
    private bool _hasLastFrameTimestamp;
    private SimulationSpeed _previousRenderSpeed = SimulationSpeed.Speed1;

    /// <summary>Monotonic start reference for UI-time (status square blink).</summary>
    private readonly long _uiTimeStartTimestamp;

    // Info panel state
    private bool _panelVisible = true;
    private SKRect _lastPanelRect;
    private SKRect _lastCloseRect;

    // Player ship info panel state
    private SKRect _lastPlayerShipPanelRect;

    // Speed state
    private SimulationSpeed _lastNonPauseSpeed = SimulationSpeed.Speed1;
    private SKRect _lastSpeedPanelRect;
    private readonly SKRect[] _speedButtonRects = new SKRect[5];

    // Scale state
    private SKRect _lastScalePanelRect;
    private readonly SKRect[] _scaleButtonRects = new SKRect[4];

    private SKRect _lastCommandPanelRect;
    private readonly SKRect[] _engineCommandButtonRects = new SKRect[EngineCommandButtons.Length];
    private int _pressedEngineCommandButtonIndex = -1;

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
    private const double VisualReconciliationDurationSeconds = 0.3;

    // Speed panel layout
    private const float SpeedBtnW = 32f;
    private const float SpeedBtnH = 22f;
    private const float SpeedBtnGap = 2f;
    private const float SpeedPanelPadX = 6f;
    private const float SpeedPanelPadY = 4f;
    private const float SpeedIndicatorSize = 8f;

    // Scale panel layout
    private const float ScaleBtnW = 32f;
    private const float ScaleBtnH = 22f;
    private const float ScaleBtnGap = 2f;
    private const float ScalePanelPadX = 6f;
    private const float ScalePanelPadY = 4f;
    private const float ScaleIndicatorSize = 8f;
    private const float ScalePanelGapFromSpeed = 4f;
    private const double ScaleSnapTolerance = 0.05;

    // Ship command panel layout
    private const string PlayerEngineModuleId = "MOD-PLAYER-ENGINE-01";
    private const float CommandBtnSize = 32f;
    private const float CommandBtnGap = 4f;
    private const float CommandPanelPadX = 6f;
    private const float CommandPanelPadY = 6f;
    private const float CommandPanelBottomMargin = 10f;
    private const float CommandIndicatorSize = 8f;

    private static readonly string[] SpeedLabels = { "II", "1x", "5x", "20x", "100x" };
    private static readonly SimulationSpeed[] SpeedValues =
        { SimulationSpeed.Speed0, SimulationSpeed.Speed1, SimulationSpeed.Speed2, SimulationSpeed.Speed3, SimulationSpeed.Speed4 };
    private static readonly string[] ScaleLabels = { "M1", "M10", "M100", "M1000" };
    private static readonly double[] ScaleTargets = { 1.0, 0.1, 0.01, 0.001 };
    private static readonly EngineCommandButton[] EngineCommandButtons =
    [
        new("^", ShipEngineCommandTypes.Accelerate),
        new("_", ShipEngineCommandTypes.Brake),
        new(">", ShipEngineCommandTypes.TurnRightStep),
        new("<", ShipEngineCommandTypes.TurnLeftStep),
        new(">>", ShipEngineCommandTypes.TurnRightUntilCancel),
        new("<<", ShipEngineCommandTypes.TurnLeftUntilCancel),
        new("X", ShipEngineCommandTypes.CancelAll),
    ];

    // ── Test seams ──────────────────────────────────────────────

    internal bool IsPanelVisible => _panelVisible;
    internal double CameraFocusX => _camera.FocusX;
    internal double CameraFocusY => _camera.FocusY;
    internal double CameraPixelsPerWorldUnit => _camera.PixelsPerWorldUnit;
    internal SKRect LastPanelRect => _lastPanelRect;
    internal SKRect LastCloseRect => _lastCloseRect;
    internal SKRect LastPlayerShipPanelRect => _lastPlayerShipPanelRect;
    internal SimulationSpeed LastNonPauseSpeed => _lastNonPauseSpeed;
    internal SKRect LastSpeedPanelRect => _lastSpeedPanelRect;
    internal IReadOnlyList<SKRect> SpeedButtonRects => _speedButtonRects;
    internal SKRect LastScalePanelRect => _lastScalePanelRect;
    internal IReadOnlyList<SKRect> ScaleButtonRects => _scaleButtonRects;
    internal IReadOnlyList<string> ScalePanelLabels => ScaleLabels;
    internal float ScaleIndicatorCenterX => ComputeScaleIndicatorPosition();
    internal SKRect LastCommandPanelRect => _lastCommandPanelRect;
    internal IReadOnlyList<SKRect> EngineCommandButtonRects => _engineCommandButtonRects;
    internal int PressedEngineCommandButtonIndex => _pressedEngineCommandButtonIndex;
    internal int ActiveEngineCommandButtonIndex { get; private set; } = -1;
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
        _timestampProvider = timestampProvider ?? Stopwatch.GetTimestamp;
        _uiTimeStartTimestamp = _timestampProvider();

        _camera = new CameraState(focusX: 10000, focusY: 10000, pixelsPerWorldUnit: 1.0);
        _grid = new GridRenderer();
        _trailStore = new ObjectTrailStore(_predictor, _timestampProvider);
        _futureTrajectoryProjector = new FutureTrajectoryProjector(_predictor);
        _labelRenderer = new ObjectLabelRenderer();

        _trailPaint = new SKPaint { Color = new SKColor(190, 190, 190, 160), Style = SKPaintStyle.Stroke, StrokeWidth = 2f, IsAntialias = true };
        _futureTrajectoryPaint = new SKPaint
        {
            Color = new SKColor(30, 30, 30, 140),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new float[] { 8f, 6f }, 0f)
        };
        _objectPaint = new SKPaint { Style = SKPaintStyle.Fill, IsAntialias = true };
        _playerShipPaint = new SKPaint { Color = new SKColor(85, 107, 47), Style = SKPaintStyle.Fill, IsAntialias = true };
        _playerShipOutlinePaint = new SKPaint { Color = new SKColor(100, 122, 62), Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f, IsAntialias = true };
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

        _scaleBtnNormalPaint = new SKPaint { Color = new SKColor(30, 30, 30), Style = SKPaintStyle.Fill };
        _scaleBtnActivePaint = new SKPaint { Color = new SKColor(50, 60, 50), Style = SKPaintStyle.Fill };
        _scaleBtnTextPaint = new SKPaint { Color = new SKColor(180, 180, 180), TextSize = 11f, IsAntialias = true, Typeface = typeface, TextAlign = SKTextAlign.Center };
        _scaleIndicatorPaint = new SKPaint { Color = new SKColor(80, 200, 80), Style = SKPaintStyle.Fill, IsAntialias = true };

        _commandBtnNormalPaint = new SKPaint { Color = new SKColor(26, 28, 31), Style = SKPaintStyle.Fill };
        _commandBtnHoverPaint = new SKPaint { Color = new SKColor(39, 45, 51), Style = SKPaintStyle.Fill };
        _commandBtnPressedPaint = new SKPaint { Color = new SKColor(58, 75, 67), Style = SKPaintStyle.Fill };
        _commandBtnDisabledPaint = new SKPaint { Color = new SKColor(18, 18, 18, 190), Style = SKPaintStyle.Fill };
        _commandBtnTextPaint = new SKPaint { Color = new SKColor(210, 218, 214), TextSize = 11f, IsAntialias = true, Typeface = typeface, TextAlign = SKTextAlign.Center };
    }

    // ── IScreen ─────────────────────────────────────────────────

    public void OnActivated() { }
    public void OnDeactivated() { }

    public ScreenEvent OnMouseDown(float x, float y)
    {
        // 0. Scale panel buttons (left of speed panel — check first)
        int scaleIdx = HitTestScalePanel(x, y);
        if (scaleIdx >= 0)
        {
            ApplyScale(ScaleTargets[scaleIdx]);
            return ScreenEvent.None;
        }

        // 1. Speed panel buttons
        int speedIdx = HitTestSpeedPanel(x, y);
        if (speedIdx >= 0)
        {
            ApplySpeed(SpeedValues[speedIdx]);
            return ScreenEvent.None;
        }

        // 2. Ship command panel buttons
        int commandIdx = HitTestEngineCommandPanel(x, y);
        if (commandIdx >= 0 && CanSendEngineCommand(
                EngineCommandButtons[commandIdx].CommandType,
                _buffer.Latest?.Snapshot))
        {
            _pressedEngineCommandButtonIndex = commandIdx;
            SendEngineCommand(EngineCommandButtons[commandIdx].CommandType);
            return ScreenEvent.None;
        }

        if (_lastCommandPanelRect.Contains(x, y))
        {
            return ScreenEvent.None;
        }

        // 3. Info panel close button
        if (_panelVisible && _lastCloseRect.Contains(x, y))
        {
            _panelVisible = false;
            return ScreenEvent.None;
        }

        // 4. Info panel (consume, don't pan)
        if (_panelVisible && _lastPanelRect.Contains(x, y))
        {
            return ScreenEvent.None;
        }

        // 5. Player ship info panel (consume, don't pan)
        if (_lastPlayerShipPanelRect.Contains(x, y))
        {
            return ScreenEvent.None;
        }

        // 6. Map click -> pan camera
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

    public void OnMouseUp(float x, float y)
    {
        _mouseX = x;
        _mouseY = y;
        _pressedEngineCommandButtonIndex = -1;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        if (delta == 0 || _viewportW <= 0 || _viewportH <= 0)
            return ScreenEvent.None;

        double factor = delta > 0 ? ZoomStepFactor : 1.0 / ZoomStepFactor;
        double oldPpu = _camera.PixelsPerWorldUnit;
        _camera.ZoomAt(factor, x, y, _viewportW, _viewportH);
        if (_camera.PixelsPerWorldUnit != oldPpu)
            InterfaceLog.Write($"Scale → PPU={_camera.PixelsPerWorldUnit:F4}");
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

        // Number keys 1..5 → Speed0..Speed4 (index in SpeedValues)
        if (key >= Key.Number1 && key <= Key.Number5)
        {
            ApplySpeed(SpeedValues[(int)(key - Key.Number1)]);
            return ScreenEvent.None;
        }

        string? commandType = key switch
        {
            Key.Up => ShipEngineCommandTypes.Accelerate,
            Key.Down => ShipEngineCommandTypes.Brake,
            Key.Left => ShipEngineCommandTypes.TurnLeftStep,
            Key.Right => ShipEngineCommandTypes.TurnRightStep,
            _ => null
        };

        if (commandType is not null && CanSendEngineCommand(commandType, _buffer.Latest?.Snapshot))
        {
            SendEngineCommand(commandType);
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

    private int HitTestEngineCommandPanel(float x, float y)
    {
        for (int i = 0; i < _engineCommandButtonRects.Length; i++)
        {
            if (_engineCommandButtonRects[i].Contains(x, y))
                return i;
        }

        return -1;
    }

    private void SendEngineCommand(string commandType)
    {
        var playerShipObjectId = _buffer.LatestPrediction?.BufferedSnapshot.Snapshot.PlayerShipObjectId;
        if (_handle is null || string.IsNullOrWhiteSpace(playerShipObjectId))
            return;

        _ = _handle.SendEngineCommandAsync(playerShipObjectId, PlayerEngineModuleId, commandType);
    }

    // ── Render ──────────────────────────────────────────────────

    public void Render(SKCanvas canvas, int width, int height)
    {
        _viewportW = width;
        _viewportH = height;

        // Frame timing for label smoothing.
        long now = _timestampProvider();
        double deltaSeconds = 0.02; // reasonable default (~50 fps)
        if (_hasLastFrameTimestamp)
        {
            long deltaTicks = now - _lastFrameTimestamp;
            deltaSeconds = (double)deltaTicks / Stopwatch.Frequency;
            if (deltaSeconds <= 0) deltaSeconds = 0.016; // floor at ~60 fps
            if (deltaSeconds > 0.5) deltaSeconds = 0.5;   // cap at 500 ms
        }
        _lastFrameTimestamp = now;
        _hasLastFrameTimestamp = true;

        // UI time in milliseconds — monotonic, independent of simulation
        // speed and game time. Drives the status square blink.
        long uiTimeMs = (long)((now - _uiTimeStartTimestamp) * 1000.0 / Stopwatch.Frequency);
        if (uiTimeMs < 0)
            uiTimeMs = 0;

        bool viewportResized = width != _lastViewportW || height != _lastViewportH;
        _lastViewportW = width;
        _lastViewportH = height;

        var prediction = _buffer.LatestPrediction;
        var buffered = prediction?.BufferedSnapshot;
        UpdateObjectRenderStates(prediction, deltaSeconds);

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
            CaptureInitialTrailBootstrapObjects();
            long predictedGameTimeMs = GetPredictedGameTimeMs(prediction);
            bool shouldBootstrapInitialTrails = _shouldBootstrapInitialTrails;

            _trailStore.Update(
                _renderStates,
                prediction.CurrentSpeed,
                predictedGameTimeMs,
                shouldBootstrapInitialTrails,
                _initialTrailBootstrapObjectIds,
                prediction.BufferedSnapshot.Snapshot.SnapshotSequence);
            if (shouldBootstrapInitialTrails)
            {
                _shouldBootstrapInitialTrails = false;
                _initialTrailBootstrapObjectIds.Clear();
            }

            DrawObjectTrails(canvas, width, height);

            // 3.5. Future trajectory (before objects, after historical trails)
            DrawFutureTrajectories(canvas, width, height);

            // Compute smoothed label geometries once per frame so both
            // DrawLeaders and DrawPlaques see the same positions.
            bool resetSmoothing = viewportResized;
            _labelRenderer.ComputeGeometries(_renderStates, deltaSeconds, width, height, _camera, resetSmoothing);

            // 3.75. Label leader lines (behind objects)
            _labelRenderer.DrawLeaders(canvas, _renderStates, width, height, _camera);

            // 4. Engine objects
            foreach (var state in _renderStates)
            {
                var (sx, sy) = _camera.WorldToScreen(state.Predicted.X, state.Predicted.Y, width, height);
                if (state.IsPlayerShip)
                {
                    DrawPlayerShipGlyph(canvas, sx, sy, state.Predicted.Direction);
                }
                else
                {
                    _objectPaint.Color = SpaceMapColorResolver.GetColor(
                        state.Predicted.ObjectType, state.Predicted.RelationToPlayer);
                    canvas.DrawCircle(sx, sy, 4, _objectPaint);
                }
            }

            // 4.5. Object label plaques (on top of objects, before UI panels)
            _labelRenderer.DrawPlaques(canvas, _renderStates, uiTimeMs, _buffer.CurrentSpeed, width, height, _camera);
        }

        // 4.75. Scale panel (top-right, left of speed panel)
        DrawScalePanel(canvas);

        // 5. Speed panel (top-right)
        DrawSpeedPanel(canvas);

        // 6. Ship command panel (bottom-center)
        DrawEngineCommandPanel(canvas, buffered);

        // 7. Info panel (bottom-left)
        if (_panelVisible)
            DrawInfoPanel(canvas, buffered);

        // 8. Player ship info panel (bottom-right)
        var playerShip = FindPlayerShip(_renderStates);
        DrawPlayerShipInfoPanel(canvas, playerShip);
    }

    // ── Speed panel ─────────────────────────────────────────────

    private void UpdateObjectRenderStates(SnapshotPrediction? prediction, double deltaSeconds)
    {
        if (prediction is null)
        {
            _renderStates.Clear();
            return;
        }

        bool isPaused = prediction.CurrentSpeed == SimulationSpeed.Speed0;
        bool enteringPause = isPaused && _previousRenderSpeed != SimulationSpeed.Speed0;
        bool resuming = !isPaused && _previousRenderSpeed == SimulationSpeed.Speed0;

        if (enteringPause)
        {
            _pausedVisualAnchors.Clear();
            for (int i = 0; i < _renderStates.Count; i++)
            {
                var state = _renderStates[i];
                _pausedVisualAnchors[state.Predicted.ObjectId] = state.Predicted;
            }

            _visualCorrections.Clear();
        }

        _renderStates.Clear();
        _currentVisualObjectIds.Clear();

        long ed = prediction.EffectivePredictionDeltaMs;
        string? playerShipObjectId = prediction.BufferedSnapshot.Snapshot.PlayerShipObjectId;

        foreach (var obj in prediction.BufferedSnapshot.Snapshot.Objects)
        {
            var predicted = ed > 0 ? _predictor.Predict(obj, ed) : obj;
            _currentVisualObjectIds.Add(obj.ObjectId);

            if (isPaused)
            {
                if (!_pausedVisualAnchors.TryGetValue(obj.ObjectId, out var anchor))
                {
                    anchor = predicted;
                    _pausedVisualAnchors[obj.ObjectId] = anchor;
                }

                predicted = ApplyVisualPose(predicted, anchor);
            }
            else
            {
                bool correctionCreated = false;
                if (resuming && _pausedVisualAnchors.TryGetValue(obj.ObjectId, out var anchor))
                {
                    var newCorrection = CreateVisualCorrection(anchor, predicted);
                    if (newCorrection.HasOffset)
                    {
                        _visualCorrections[obj.ObjectId] = newCorrection;
                        correctionCreated = true;
                    }
                }

                if (_visualCorrections.TryGetValue(obj.ObjectId, out var correction))
                {
                    if (!correctionCreated)
                        correction = correction with { ElapsedSeconds = correction.ElapsedSeconds + deltaSeconds };

                    predicted = ApplyVisualCorrection(predicted, correction);
                    if (correction.ElapsedSeconds >= VisualReconciliationDurationSeconds)
                        _visualCorrections.Remove(obj.ObjectId);
                    else
                        _visualCorrections[obj.ObjectId] = correction;
                }
            }

            _renderStates.Add(new ObjectRenderState(obj, predicted, obj.ObjectId == playerShipObjectId));
        }

        RemoveMissingVisualStates(_pausedVisualAnchors);
        RemoveMissingVisualStates(_visualCorrections);

        if (resuming)
            _pausedVisualAnchors.Clear();

        _previousRenderSpeed = prediction.CurrentSpeed;
    }

    private static ObjectMotionSnapshot ApplyVisualPose(
        ObjectMotionSnapshot target,
        ObjectMotionSnapshot visualPose)
    {
        return target with
        {
            X = visualPose.X,
            Y = visualPose.Y,
            Direction = visualPose.Direction
        };
    }

    private static VisualCorrection CreateVisualCorrection(
        ObjectMotionSnapshot visualPose,
        ObjectMotionSnapshot target)
    {
        return new VisualCorrection(
            visualPose.X - target.X,
            visualPose.Y - target.Y,
            ShortestDirectionDelta(visualPose.Direction, target.Direction),
            ElapsedSeconds: 0);
    }

    private static ObjectMotionSnapshot ApplyVisualCorrection(
        ObjectMotionSnapshot target,
        VisualCorrection correction)
    {
        double progress = Math.Clamp(
            correction.ElapsedSeconds / VisualReconciliationDurationSeconds,
            0,
            1);
        double smoothProgress = progress * progress * (3 - 2 * progress);
        double remaining = 1 - smoothProgress;

        return target with
        {
            X = target.X + correction.OffsetX * remaining,
            Y = target.Y + correction.OffsetY * remaining,
            Direction = NormalizeDirection(target.Direction + correction.DirectionOffset * remaining)
        };
    }

    private static double ShortestDirectionDelta(double visualDirection, double targetDirection)
    {
        double delta = (visualDirection - targetDirection) % 360;
        if (delta > 180)
            delta -= 360;
        else if (delta < -180)
            delta += 360;

        return delta;
    }

    private static double NormalizeDirection(double direction)
    {
        double normalized = direction % 360;
        return normalized < 0 ? normalized + 360 : normalized;
    }

    private void RemoveMissingVisualStates<T>(Dictionary<string, T> states)
    {
        _visualObjectIdsToRemove.Clear();
        foreach (string objectId in states.Keys)
        {
            if (!_currentVisualObjectIds.Contains(objectId))
                _visualObjectIdsToRemove.Add(objectId);
        }

        for (int i = 0; i < _visualObjectIdsToRemove.Count; i++)
            states.Remove(_visualObjectIdsToRemove[i]);
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

    private void CaptureInitialTrailBootstrapObjects()
    {
        if (_capturedInitialTrailBootstrapObjects)
            return;

        for (int i = 0; i < _renderStates.Count; i++)
            _initialTrailBootstrapObjectIds.Add(_renderStates[i].Predicted.ObjectId);

        _capturedInitialTrailBootstrapObjects = true;
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

    // ── Future trajectory ────────────────────────────────────────

    private void DrawFutureTrajectories(SKCanvas canvas, int width, int height)
    {
        foreach (var state in _renderStates)
        {
            if (!state.IsPlayerShip)
                continue;

            if (!FutureTrajectoryProjector.ShouldDraw(state.Predicted))
                continue;

            var points = _futureTrajectoryProjector.Project(state.Predicted);
            if (points.Count < 2)
                continue;

            for (int i = 1; i < points.Count; i++)
            {
                var from = points[i - 1];
                var to = points[i];
                var (fromX, fromY) = _camera.WorldToScreen(from.X, from.Y, width, height);
                var (toX, toY) = _camera.WorldToScreen(to.X, to.Y, width, height);

                canvas.DrawLine(fromX, fromY, toX, toY, _futureTrajectoryPaint);
            }
        }
    }

    // ── Test seams for future trajectory ─────────────────────────

    internal IReadOnlyList<FutureTrajectoryPoint> GetFutureTrajectory(string objectId)
    {
        foreach (var state in _renderStates)
        {
            if (state.Predicted.ObjectId == objectId)
            {
                if (!state.IsPlayerShip)
                    return Array.Empty<FutureTrajectoryPoint>();

                return FutureTrajectoryProjector.ShouldDraw(state.Predicted)
                    ? _futureTrajectoryProjector.Project(state.Predicted)
                    : Array.Empty<FutureTrajectoryPoint>();
            }
        }

        return Array.Empty<FutureTrajectoryPoint>();
    }

    /// <summary>
    /// Compute the speed panel rect from current viewport — used both by
    /// DrawSpeedPanel and DrawScalePanel (scale panel sits left of it).
    /// </summary>
    private SKRect ComputeSpeedPanelRect()
    {
        int btnCount = SpeedLabels.Length;
        float totalW = SpeedPanelPadX * 2 + btnCount * SpeedBtnW + (btnCount - 1) * SpeedBtnGap;
        float panelH = SpeedPanelPadY * 2 + SpeedBtnH + SpeedIndicatorSize + 2f;
        float panelX = _viewportW - totalW - PanelMargin;
        float panelY = PanelMargin;
        return new SKRect(panelX, panelY, panelX + totalW, panelY + panelH);
    }

    private void DrawSpeedPanel(SKCanvas canvas)
    {
        int btnCount = SpeedLabels.Length;
        _lastSpeedPanelRect = ComputeSpeedPanelRect();
        canvas.DrawRect(_lastSpeedPanelRect, _panelBgPaint);
        canvas.DrawRect(_lastSpeedPanelRect, _panelBorderPaint);

        float btnX = _lastSpeedPanelRect.Left + SpeedPanelPadX;
        float btnY = _lastSpeedPanelRect.Top + SpeedPanelPadY;

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

    // ── Scale panel ─────────────────────────────────────────────

    private void DrawScalePanel(SKCanvas canvas)
    {
        int btnCount = ScaleLabels.Length;
        float totalW = ScalePanelPadX * 2 + btnCount * ScaleBtnW + (btnCount - 1) * ScaleBtnGap;
        float panelH = ScalePanelPadY * 2 + ScaleBtnH + ScaleIndicatorSize + 2f;

        float panelX = ComputeSpeedPanelRect().Left - ScalePanelGapFromSpeed - totalW;
        float panelY = PanelMargin;

        _lastScalePanelRect = new SKRect(panelX, panelY, panelX + totalW, panelY + panelH);
        canvas.DrawRect(_lastScalePanelRect, _panelBgPaint);
        canvas.DrawRect(_lastScalePanelRect, _panelBorderPaint);

        float btnX = panelX + ScalePanelPadX;
        float btnY = panelY + ScalePanelPadY;

        int activeIdx = GetClosestScaleIndex();

        for (int i = 0; i < btnCount; i++)
        {
            var btnRect = new SKRect(btnX, btnY, btnX + ScaleBtnW, btnY + ScaleBtnH);
            _scaleButtonRects[i] = btnRect;

            bool isActive = (i == activeIdx);
            canvas.DrawRect(btnRect, isActive ? _scaleBtnActivePaint : _scaleBtnNormalPaint);
            canvas.DrawRect(btnRect, _panelBorderPaint);

            float textY = btnY + ScaleBtnH / 2f + _scaleBtnTextPaint.TextSize / 3f;
            canvas.DrawText(ScaleLabels[i], btnX + ScaleBtnW / 2f, textY, _scaleBtnTextPaint);

            btnX += ScaleBtnW + ScaleBtnGap;
        }

        // Continuous indicator — under the active button or between buttons.
        float indX = ComputeScaleIndicatorPosition() - ScaleIndicatorSize / 2f;
        float indY = panelY + ScalePanelPadY + ScaleBtnH + 1f;
        var path = new SKPath();
        path.MoveTo(indX, indY);
        path.LineTo(indX + ScaleIndicatorSize, indY);
        path.LineTo(indX + ScaleIndicatorSize / 2f, indY + ScaleIndicatorSize);
        path.Close();
        canvas.DrawPath(path, _scaleIndicatorPaint);
    }

    /// <summary>
    /// Index of the scale button nearest to the current PPU (active highlight).
    /// </summary>
    private int GetClosestScaleIndex()
    {
        double position = ScalePosition();
        return (int)Math.Round(position);
    }

    /// <summary>
    /// Continuous scale position in button space: 0 = M1 (PPU 1.0),
    /// 1 = M10 (PPU 0.1), 2 = M100 (PPU 0.01), 3 = M1000 (PPU 0.001).
    /// Computed from log10(PPU), clamped to [0, 3].
    /// </summary>
    private double ScalePosition()
    {
        // PPU is always clamped to a positive range by CameraState, so log10 is defined.
        double logPpu = Math.Log10(_camera.PixelsPerWorldUnit);
        double position = -logPpu;
        return Math.Clamp(position, 0, 3);
    }

    private float ComputeScaleIndicatorPosition()
    {
        double position = ScalePosition();
        int nearest = (int)Math.Round(position);
        if (Math.Abs(position - nearest) < ScaleSnapTolerance)
            return _scaleButtonRects[nearest].MidX;

        int lower = (int)Math.Floor(position);
        int upper = (int)Math.Ceiling(position);
        if (lower == upper)
            return _scaleButtonRects[lower].MidX;

        float frac = (float)(position - lower);
        float lowerX = _scaleButtonRects[lower].MidX;
        float upperX = _scaleButtonRects[upper].MidX;
        return lowerX + (upperX - lowerX) * frac;
    }

    private void ApplyScale(double targetPpu)
    {
        _camera.SetZoom(targetPpu);
        InterfaceLog.Write($"Scale → PPU={targetPpu:F4}");
    }

    private int HitTestScalePanel(float x, float y)
    {
        for (int i = 0; i < _scaleButtonRects.Length; i++)
        {
            if (_scaleButtonRects[i].Contains(x, y))
                return i;
        }
        return -1;
    }

    private void DrawPlayerShipGlyph(SKCanvas canvas, float sx, float sy, double directionDegrees)
    {
        double radians = directionDegrees * Math.PI / 180.0;
        float dx = (float)Math.Sin(radians);
        float dy = -(float)Math.Cos(radians);
        float rx = dy;
        float ry = -dx;

        var nose = new SKPoint(sx + dx * 7f, sy + dy * 7f);
        var left = new SKPoint(sx - dx * 5f - rx * 5f, sy - dy * 5f - ry * 5f);
        var right = new SKPoint(sx - dx * 5f + rx * 5f, sy - dy * 5f + ry * 5f);

        _playerShipGlyphPath.Reset();
        _playerShipGlyphPath.MoveTo(nose);
        _playerShipGlyphPath.LineTo(left);
        _playerShipGlyphPath.LineTo(right);
        _playerShipGlyphPath.Close();

        canvas.DrawPath(_playerShipGlyphPath, _playerShipPaint);
        canvas.DrawPath(_playerShipGlyphPath, _playerShipOutlinePaint);
    }

    private void DrawEngineCommandPanel(SKCanvas canvas, BufferedSnapshot? buffered)
    {
        int btnCount = EngineCommandButtons.Length;
        float totalW = CommandPanelPadX * 2 + btnCount * CommandBtnSize + (btnCount - 1) * CommandBtnGap;
        float totalH = CommandPanelPadY * 2 + CommandBtnSize + CommandIndicatorSize + 2f;
        float panelX = (_viewportW - totalW) / 2f;
        float panelY = _viewportH - totalH - CommandPanelBottomMargin;

        _lastCommandPanelRect = new SKRect(panelX, panelY, panelX + totalW, panelY + totalH);
        canvas.DrawRect(_lastCommandPanelRect, _panelBgPaint);
        canvas.DrawRect(_lastCommandPanelRect, _panelBorderPaint);

        float btnX = panelX + CommandPanelPadX;
        float btnY = panelY + CommandPanelPadY;

        bool panelEnabled = _handle is not null &&
                            !string.IsNullOrWhiteSpace(buffered?.Snapshot.PlayerShipObjectId);
        string? activeCommandType = GetActiveEngineCommandType(buffered?.Snapshot);
        ActiveEngineCommandButtonIndex = -1;

        for (int i = 0; i < btnCount; i++)
        {
            var rect = new SKRect(btnX, btnY, btnX + CommandBtnSize, btnY + CommandBtnSize);
            _engineCommandButtonRects[i] = rect;

            bool buttonEnabled = panelEnabled &&
                                 CanSendEngineCommand(EngineCommandButtons[i].CommandType, buffered?.Snapshot);
            bool isHover = rect.Contains(_mouseX, _mouseY);
            bool isPressed = i == _pressedEngineCommandButtonIndex && isHover;
            var paint = buttonEnabled
                ? isPressed ? _commandBtnPressedPaint : isHover ? _commandBtnHoverPaint : _commandBtnNormalPaint
                : _commandBtnDisabledPaint;

            canvas.DrawRect(rect, paint);
            canvas.DrawRect(rect, _panelBorderPaint);

            _commandBtnTextPaint.Color = buttonEnabled
                ? new SKColor(210, 218, 214)
                : new SKColor(96, 96, 96);
            float textY = rect.MidY + _commandBtnTextPaint.TextSize / 3f;
            canvas.DrawText(EngineCommandButtons[i].Label, rect.MidX, textY, _commandBtnTextPaint);

            if (IsCyclicEngineCommand(EngineCommandButtons[i].CommandType) &&
                EngineCommandButtons[i].CommandType == activeCommandType)
            {
                ActiveEngineCommandButtonIndex = i;
                DrawCommandIndicator(canvas, rect);
            }

            btnX += CommandBtnSize + CommandBtnGap;
        }
    }

    private bool CanSendEngineCommand(string commandType, AuthoritativeSnapshot? snapshot)
    {
        if (_handle is null || string.IsNullOrWhiteSpace(snapshot?.PlayerShipObjectId))
            return false;

        string? activeCommand = GetActiveEngineCommandType(snapshot);
        if (activeCommand is null)
            return true;

        // Same cyclic command — idempotent, no need to send again.
        if (commandType == activeCommand)
            return false;

        // CancelAll is always allowed — it explicitly cancels the active cycle.
        if (commandType == ShipEngineCommandTypes.CancelAll)
            return true;

        // One-shot turns are rejected by the engine when an auto-repeat cycle is active.
        if (!IsCyclicEngineCommand(commandType))
            return false;

        // Different cyclic command while another is active.
        // Only TurnLeftUntilCancel ↔ TurnRightUntilCancel mutual replacement is allowed.
        return IsUntilCancelTurnCommand(activeCommand) && IsUntilCancelTurnCommand(commandType);
    }

    private static string? GetActiveEngineCommandType(AuthoritativeSnapshot? snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot?.PlayerShipObjectId))
            return null;

        foreach (var obj in snapshot.Objects)
        {
            if (obj.ObjectId == snapshot.PlayerShipObjectId)
                return obj.ActiveEngineCommandType;
        }

        return null;
    }

    private static bool IsCyclicEngineCommand(string commandType)
    {
        return commandType == ShipEngineCommandTypes.Accelerate ||
               commandType == ShipEngineCommandTypes.Brake ||
               commandType == ShipEngineCommandTypes.TurnLeftUntilCancel ||
               commandType == ShipEngineCommandTypes.TurnRightUntilCancel;
    }

    private static bool IsUntilCancelTurnCommand(string commandType)
    {
        return commandType == ShipEngineCommandTypes.TurnLeftUntilCancel ||
               commandType == ShipEngineCommandTypes.TurnRightUntilCancel;
    }

    private void DrawCommandIndicator(SKCanvas canvas, SKRect buttonRect)
    {
        float indicatorX = buttonRect.MidX - CommandIndicatorSize / 2f;
        float indicatorY = buttonRect.Bottom + 1f;
        _playerShipGlyphPath.Reset();
        _playerShipGlyphPath.MoveTo(indicatorX, indicatorY);
        _playerShipGlyphPath.LineTo(indicatorX + CommandIndicatorSize, indicatorY);
        _playerShipGlyphPath.LineTo(indicatorX + CommandIndicatorSize / 2f, indicatorY + CommandIndicatorSize);
        _playerShipGlyphPath.Close();
        canvas.DrawPath(_playerShipGlyphPath, _speedIndicatorPaint);
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

    // ── Player ship info panel ───────────────────────────────────

    private static ObjectRenderState? FindPlayerShip(IReadOnlyList<ObjectRenderState> renderStates)
    {
        for (int i = 0; i < renderStates.Count; i++)
        {
            if (renderStates[i].IsPlayerShip)
                return renderStates[i];
        }
        return null;
    }

    private static string? GetActiveEngineCommandDisplayName(string? commandType)
    {
        return commandType switch
        {
            ShipEngineCommandTypes.Accelerate => "Accelerate",
            ShipEngineCommandTypes.Brake => "Brake",
            ShipEngineCommandTypes.TurnLeftUntilCancel => "Turn Left Until Cancel",
            ShipEngineCommandTypes.TurnRightUntilCancel => "Turn Right Until Cancel",
            _ => null
        };
    }

    internal List<(string Label, string Value)> BuildPlayerShipPanelLines(ObjectRenderState? playerShip)
    {
        var lines = new List<(string Label, string Value)>();

        if (playerShip is not null)
        {
            var p = playerShip.Value.Predicted;
            lines.Add(("Speed", $"{p.SpeedKmS:0.###} km/s"));
            lines.Add(("Course", $"{p.Direction:F0}°"));
            lines.Add(("Location", $"({p.X:F0}, {p.Y:F0})"));

            string engineDisplay = GetActiveEngineCommandDisplayName(p.ActiveEngineCommandType) ?? "—";
            lines.Add(("Engine", engineDisplay));
        }
        else
        {
            lines.Add(("Speed", "—"));
            lines.Add(("Course", "—"));
            lines.Add(("Location", "(—, —)"));
            lines.Add(("Engine", "—"));
        }

        return lines;
    }

    private void DrawPlayerShipInfoPanel(SKCanvas canvas, ObjectRenderState? playerShip)
    {
        var lines = BuildPlayerShipPanelLines(playerShip);

        float labelWidth = 0, valueWidth = 0;
        foreach (var (label, value) in lines)
        {
            labelWidth = Math.Max(labelWidth, _panelLabelPaint.MeasureText(label));
            valueWidth = Math.Max(valueWidth, _panelTextPaint.MeasureText(value));
        }

        float gap = 8f;
        float panelW = PanelPaddingX * 2 + labelWidth + gap + valueWidth;
        float panelH = PanelPaddingY * 2 + lines.Count * PanelLineHeight;

        float panelX = _viewportW - panelW - PanelMargin;
        float panelY = _viewportH - panelH - PanelMargin;

        _lastPlayerShipPanelRect = new SKRect(panelX, panelY, panelX + panelW, panelY + panelH);

        canvas.DrawRect(_lastPlayerShipPanelRect, _panelBgPaint);
        canvas.DrawRect(_lastPlayerShipPanelRect, _panelBorderPaint);

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

    private readonly record struct VisualCorrection(
        double OffsetX,
        double OffsetY,
        double DirectionOffset,
        double ElapsedSeconds)
    {
        internal bool HasOffset => OffsetX != 0 || OffsetY != 0 || DirectionOffset != 0;
    }

}

internal readonly record struct ObjectRenderState(
    ObjectMotionSnapshot Source,
    ObjectMotionSnapshot Predicted,
    bool IsPlayerShip);

internal readonly record struct EngineCommandButton(string Label, string CommandType);
