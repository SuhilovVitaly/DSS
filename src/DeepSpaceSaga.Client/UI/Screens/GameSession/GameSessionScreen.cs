using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
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
    private readonly NavigationTrajectoryProjector _navigationTrajectoryProjector;
    private readonly ObjectLabelRenderer _labelRenderer;
    private readonly List<ObjectRenderState> _renderStates = new();
    private readonly Dictionary<string, ObjectMotionSnapshot> _pausedVisualAnchors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, VisualCorrection> _visualCorrections = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ObjectMotionSnapshot> _lastSnapshotBaselineObjects = new(StringComparer.Ordinal);
    private ulong _lastSnapshotBaselineSequence;
    private long _lastSnapshotBaselineGameTimeMs;
    private bool _hasSnapshotBaseline;
    private bool _diagInterestingFrame;
    private readonly HashSet<string> _currentVisualObjectIds = new(StringComparer.Ordinal);
    private readonly List<string> _visualObjectIdsToRemove = new();
    private readonly HashSet<string> _initialTrailBootstrapObjectIds = new(StringComparer.Ordinal);
    private readonly GameSessionHandle? _handle;
    private readonly Func<long> _timestampProvider;
    private readonly bool _showTrajectoryPrediction;

    // Object paints
    private readonly SKPaint _trailPaint;
    private readonly SKPaint _futureTrajectoryPaint;
    private readonly SKPaint _navigationTrajectoryPaint;
    private readonly SKPaint _navigationTargetPaint;
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
    private readonly SKPaint _commandBtnIconPaint;
    private readonly SKPaint _commandBtnHoverBorderPaint;
    private readonly SKPaint _commandBtnPressedBorderPaint;
    private readonly SKBitmap?[] _engineCommandButtonIcons;

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
    private readonly SKRect[] _scaleButtonRects = new SKRect[ScaleLabels.Length];

    private SKRect _lastCommandPanelRect;
    private readonly SKRect[] _engineCommandButtonRects = new SKRect[EngineCommandButtons.Length];
    private int _pressedEngineCommandButtonIndex = -1;

    // Commands Panel (top-left) — skeleton, ТЗ подзадача 1 (CommandPanelPlan.md)
    private readonly CommandsPanel _commandsPanel = new();

    // Camera state
    private bool _isFocusAttachedToPlayer = true;

    // Navigation (Ctrl+Click) state — ТЗ: Navigation Waypoints
    private bool _isCtrlLeftDown;
    private bool _isCtrlRightDown;
    private bool IsCtrlDown => _isCtrlLeftDown || _isCtrlRightDown;
    private (double X, double Y)? _pendingNavigationTarget;

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
    private const double ReconciliationCorrectionToleranceWorldUnitsSq = 0.25; // 0.5 world unit (~50 m)
    private const double ReconciliationCorrectionToleranceDegrees = 0.25;

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
    private const float CommandBtnSize = 64f;

    /// <summary>Approved click slack for Ctrl+Click object hit-test (+4 px, ТЗ 4.6).</summary>
    private const float HitTestSlackPx = 4f;
    private const float CommandBtnGap = 4f;
    private const float CommandPanelPadX = 6f;
    private const float CommandPanelPadY = 6f;
    private const float CommandPanelBottomMargin = 10f;
    private const float CommandIndicatorSize = 8f;

    private static readonly string[] SpeedLabels = { "II", "1x", "5x", "20x", "100x" };
    private static readonly SimulationSpeed[] SpeedValues =
        { SimulationSpeed.Speed0, SimulationSpeed.Speed1, SimulationSpeed.Speed2, SimulationSpeed.Speed3, SimulationSpeed.Speed4 };
    private static readonly string[] ScaleLabels = { "M0.5", "M1", "M10", "M100", "M1000" };
    private static readonly double[] ScaleTargets = { 2.0, 1.0, 0.1, 0.01, 0.001 };
    private static readonly double WheelMinPpu = ScaleTargets.Min();
    private static readonly double WheelMaxPpu = ScaleTargets.Max();
    private static readonly EngineCommandButton[] EngineCommandButtons =
    [
        new("^", ShipEngineCommandTypes.Accelerate, "button_accelerate.png"),
        new("_", ShipEngineCommandTypes.Brake, "button_brake.png"),
        new("=", ShipEngineCommandTypes.MaintainSpeed, "button_maintain_speed.png"),
        new(">", ShipEngineCommandTypes.TurnRightStep, "button_turn_right_step.png"),
        new("<", ShipEngineCommandTypes.TurnLeftStep, "button_turn_left_step.png"),
        new(">>", ShipEngineCommandTypes.TurnRightUntilCancel, "button_turn_right_until_cancel.png"),
        new("<<", ShipEngineCommandTypes.TurnLeftUntilCancel, "button_turn_left_until_cancel.png"),
        new("°", ShipEngineCommandTypes.MaintainCourse, "button_maintain_course.png"),
        new("X", ShipEngineCommandTypes.CancelAll, "button_cancel_all.png"),
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
    internal CommandsPanel CommandsPanel => _commandsPanel;

    /// <summary>Current frame's render list (scale-filtered, client-side).</summary>
    internal IReadOnlyList<ObjectRenderState> RenderStates => _renderStates;
    internal int ActiveEngineCommandButtonIndex { get; private set; } = -1;
    internal bool IsFocusAttachedToPlayer => _isFocusAttachedToPlayer;
    internal IReadOnlyList<ObjectTrailPoint> GetObjectTrail(string objectId) => _trailStore.GetTrail(objectId);

    // ── Constructor ─────────────────────────────────────────────

    public GameSessionScreen(
        SnapshotBuffer buffer,
        IMotionPredictor predictor,
        GameSessionHandle? handle = null,
        Func<long>? timestampProvider = null,
        bool showTrajectoryPrediction = true)
    {
        _buffer = buffer;
        _predictor = predictor;
        _handle = handle;
        _timestampProvider = timestampProvider ?? Stopwatch.GetTimestamp;
        _showTrajectoryPrediction = showTrajectoryPrediction;
        _uiTimeStartTimestamp = _timestampProvider();

        _camera = new CameraState(focusX: 10000, focusY: 10000, pixelsPerWorldUnit: 1.0);
        _grid = new GridRenderer();
        _trailStore = new ObjectTrailStore(_predictor, _timestampProvider);
        _futureTrajectoryProjector = new FutureTrajectoryProjector(_predictor);
        _navigationTrajectoryProjector = new NavigationTrajectoryProjector();
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
        // Navigation trajectory: same gray dashed style as future trajectory.
        _navigationTrajectoryPaint = new SKPaint
        {
            Color = new SKColor(30, 30, 30, 140),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f,
            IsAntialias = true,
            PathEffect = SKPathEffect.CreateDash(new float[] { 10f, 8f }, 0f)
        };
        _navigationTargetPaint = new SKPaint
        {
            Color = new SKColor(30, 30, 30, 200),
            Style = SKPaintStyle.Fill,
            IsAntialias = true
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
        _commandBtnIconPaint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
        _commandBtnHoverBorderPaint = new SKPaint { Color = new SKColor(90, 100, 96), Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        _commandBtnPressedBorderPaint = new SKPaint { Color = new SKColor(120, 160, 140), Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        _engineCommandButtonIcons = LoadEngineCommandIcons();
    }

    // ── IScreen ─────────────────────────────────────────────────

    public void OnActivated() { }
    public void OnDeactivated()
    {
        _isCtrlLeftDown = false;
        _isCtrlRightDown = false;
    }

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

        // 2.5. Commands Panel (top-left) — consume clicks, don't pan (ТЗ подзадача 1)
        if (_commandsPanel.OnMouseDown(x, y))
            return ScreenEvent.None;

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

        // 5.5. Ctrl+Click navigation: on free map area (no object under the cursor)
        // send exactly one engine.navigate-to-point command with world coordinates.
        // The camera focus is NOT changed and no command is sent when the click lands
        // on an object (hit-test includes a +4 px slack) — panels were already consumed
        // above. AC2, AC1 preserved.
        if (IsCtrlDown)
        {
            var (navWorldX, navWorldY) = _camera.ScreenToWorld(x, y, _viewportW, _viewportH);
            if (!HitTestObject(x, y))
                SendNavigationCommand(navWorldX, navWorldY);
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
        return _commandsPanel.OnMouseMove(x, y);
    }

    public void OnMouseUp(float x, float y)
    {
        _mouseX = x;
        _mouseY = y;
        _pressedEngineCommandButtonIndex = -1;
        _commandsPanel.OnMouseUp(x, y);
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        if (delta == 0 || _viewportW <= 0 || _viewportH <= 0)
            return ScreenEvent.None;

        double factor = delta > 0 ? ZoomStepFactor : 1.0 / ZoomStepFactor;
        double oldPpu = _camera.PixelsPerWorldUnit;
        _camera.ZoomAt(factor, x, y, _viewportW, _viewportH, minPpu: WheelMinPpu, maxPpu: WheelMaxPpu);
        if (_camera.PixelsPerWorldUnit != oldPpu)
            InterfaceLog.Write($"Scale → PPU={_camera.PixelsPerWorldUnit:F4}");
        return ScreenEvent.None;
    }

    public ScreenEvent OnKeyDown(Key key)
    {
        if (key == Key.ControlLeft)
        {
            _isCtrlLeftDown = true;
            return ScreenEvent.None;
        }

        if (key == Key.ControlRight)
        {
            _isCtrlRightDown = true;
            return ScreenEvent.None;
        }

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

        if (key == Key.F5)
            return ScreenEvent.QuickSave;

        if (key == Key.F9)
            return ScreenEvent.QuickLoad;

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

    public void OnKeyUp(Key key)
    {
        if (key == Key.ControlLeft)
            _isCtrlLeftDown = false;
        else if (key == Key.ControlRight)
            _isCtrlRightDown = false;
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

    /// <summary>
    /// Ctrl+Click navigation: send exactly one engine.navigate-to-point command with
    /// the clicked world coordinates and remember the pending target for the AC3
    /// preview line (drawn until the authoritative NavigationTarget* appears in a
    /// snapshot — or the command is rejected).
    /// </summary>
    private void SendNavigationCommand(double worldX, double worldY)
    {
        var buffer = _buffer.LatestPrediction;
        if (_handle is null || buffer is null)
            return;

        var playerShipObjectId = buffer.BufferedSnapshot.Snapshot.PlayerShipObjectId;
        if (string.IsNullOrWhiteSpace(playerShipObjectId))
            return;

        // Client-side precheck: predict current ship state and validate target safety.
        // Avoids sending a command the engine would reject, and prevents drawing a
        // looping pending trajectory.
        var motion = _predictor.Predict(
            FindPlayerShipMotion(buffer.BufferedSnapshot.Snapshot) ?? throw new InvalidOperationException("Player ship missing"),
            buffer.EffectivePredictionDeltaMs);

        // Use the active navigation angular inertia if present; otherwise fall back
        // to the engine module's known value (4 deg/sec). Without an active cycle
        // the snapshot carries 0, but the safety check needs the real value.
        int angularInertia = motion.NavigationAngularInertiaDegPerSec > 0
            ? motion.NavigationAngularInertiaDegPerSec
            : 4;

        if (!NavigationWaypointMath.IsTargetSafe(
                motion.X, motion.Y, motion.Direction, motion.SpeedKmS,
                worldX, worldY,
                angularInertia))
        {
            _pendingNavigationTarget = null;
            return;
        }

        _ = _handle.SendEngineCommandAsync(
            playerShipObjectId, PlayerEngineModuleId, ShipEngineCommandTypes.NavigateToPoint, worldX, worldY);
        _pendingNavigationTarget = (worldX, worldY);
    }

    /// <summary>
    /// Hit-test an object under the cursor using the SAME marker radii the renderer
    /// draws, plus the approved +4 px slack (ТЗ 4.6). Ctrl+Click on an object sends
    /// no navigation command.
    /// </summary>
    private bool HitTestObject(float x, float y)
    {
        for (int i = 0; i < _renderStates.Count; i++)
        {
            var state = _renderStates[i];
            string renderType = state.IsPlayerShip
                ? SpaceObjectType.PlayerShip
                : (state.Predicted.RenderObjectType ?? SpaceObjectType.UnknownSpaceObject);
            float radius = TacticalMapMarkerPolicy.GetMarkerRadiusPx(renderType) + HitTestSlackPx;
            var (sx, sy) = _camera.WorldToScreen(state.Predicted.X, state.Predicted.Y, _viewportW, _viewportH);
            float dx = x - sx;
            float dy = y - sy;
            if (dx * dx + dy * dy <= radius * radius)
                return true;
        }

        return false;
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

        if (_diagInterestingFrame && PauseResumeDiagnostics.Enabled)
        {
            PauseResumeDiagnostics.Write(
                $"CAMERA focusX={_camera.FocusX:F3} focusY={_camera.FocusY:F3} attached={_isFocusAttachedToPlayer}");
            _diagInterestingFrame = false;
        }

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

            // 3.55. Navigation trajectory (Ctrl+Click) — after future trajectory,
            // visually distinct (golden dash vs dark dash)
            DrawNavigationTrajectories(canvas, width, height);

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
                // Marker radius from the shared policy (screen-space, zoom-independent).
                // The player ship's render type comes from identity (IsPlayerShip), not
                // the payload — legacy payloads without RenderObjectType still draw as a ship.
                float r = TacticalMapMarkerPolicy.GetMarkerRadiusPx(
                    state.IsPlayerShip ? SpaceObjectType.PlayerShip : state.Predicted.RenderObjectType);
                if (state.IsPlayerShip)
                {
                    DrawPlayerShipGlyph(canvas, sx, sy, state.Predicted.Direction, r);
                }
                else
                {
                    _objectPaint.Color = SpaceMapColorResolver.GetColor(
                        state.Predicted.RenderObjectType, state.Predicted.RelationToPlayer);
                    canvas.DrawCircle(sx, sy, r, _objectPaint);
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

        // 6.5. Commands Panel (top-left)
        _commandsPanel.Render(canvas,
            buffered?.Snapshot.InstalledModules ?? ImmutableArray<InstalledModuleSnapshot>.Empty);

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
        var snapshot = prediction.BufferedSnapshot.Snapshot;
        string? playerShipObjectId = snapshot.PlayerShipObjectId;

        // A fresh authoritative snapshot can reveal that the object's real trajectory
        // (velocity/heading) differed from what the client had been extrapolating from
        // the PREVIOUS snapshot — e.g. an engine command or turn cycle progressed while
        // paused/off-screen, or the engine's and client's clocks simply disagree by a few
        // ms (amplified hugely at Speed4). Either way, "what the client was already
        // showing, carried forward to the same target time" is the previous baseline
        // object extrapolated to now — NOT the new snapshot's own object (which is the
        // discontinuity itself, not a continuity reference). Must apply exactly once (the
        // first frame that observes this snapshot as latest) and smooth like a resume
        // correction, otherwise it snaps instantly on whichever frame receives it — not
        // necessarily the pause/resume transition frame at all.
        bool newSnapshotArrived = _hasSnapshotBaseline && snapshot.SnapshotSequence != _lastSnapshotBaselineSequence;
        long targetGameTimeMs = snapshot.GameTimeMs + ed;

        foreach (var obj in snapshot.Objects)
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
                else if (newSnapshotArrived &&
                         _lastSnapshotBaselineObjects.TryGetValue(obj.ObjectId, out var prevBaseline))
                {
                    long elapsedFromPrevBaseline = targetGameTimeMs - _lastSnapshotBaselineGameTimeMs;
                    var continuityExpected = elapsedFromPrevBaseline > 0
                        ? _predictor.Predict(prevBaseline, elapsedFromPrevBaseline)
                        : prevBaseline;

                    var newCorrection = CreateVisualCorrection(continuityExpected, predicted);
                    if (IsMeaningfulCorrection(newCorrection))
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

            if (PauseResumeDiagnostics.Enabled && obj.ObjectId == playerShipObjectId &&
                (enteringPause || resuming || newSnapshotArrived ||
                 _visualCorrections.ContainsKey(obj.ObjectId) || isPaused))
            {
                _diagInterestingFrame = true;
                PauseResumeDiagnostics.Write(
                    $"OBJECT id={obj.ObjectId} isPaused={isPaused} enteringPause={enteringPause} resuming={resuming} " +
                    $"newSnapshotArrived={newSnapshotArrived} " +
                    $"snapSeq={snapshot.SnapshotSequence} snapGameTimeMs={snapshot.GameTimeMs} ed={ed} " +
                    $"authX={obj.X:F3} authY={obj.Y:F3} authDir={obj.Direction:F3} " +
                    $"visualX={predicted.X:F3} visualY={predicted.Y:F3} visualDir={predicted.Direction:F3} " +
                    $"turnStepDeg={obj.TurnStepDegrees} turnStepRemainingMs={obj.TurnStepRemainingMs} " +
                    $"correctionActive={_visualCorrections.ContainsKey(obj.ObjectId)}");
            }

            _lastSnapshotBaselineObjects[obj.ObjectId] = obj;

            // ТЗ-10 scale visibility filter — client-side only: hidden objects
            // remain in the snapshot/buffer, only the render list is filtered.
            // The player ship resolves via identity so legacy payloads without
            // RenderObjectType stay visible at every scale.
            string renderType = obj.ObjectId == playerShipObjectId
                ? SpaceObjectType.PlayerShip
                : (obj.RenderObjectType ?? SpaceObjectType.UnknownSpaceObject);
            if (!TacticalMapMarkerPolicy.ShouldRenderAtScale(renderType, _camera.PixelsPerWorldUnit))
                continue;

            _renderStates.Add(new ObjectRenderState(obj, predicted, obj.ObjectId == playerShipObjectId));
        }

        RemoveMissingVisualStates(_pausedVisualAnchors);
        RemoveMissingVisualStates(_visualCorrections);
        RemoveMissingVisualStates(_lastSnapshotBaselineObjects);

        if (resuming)
            _pausedVisualAnchors.Clear();

        _lastSnapshotBaselineGameTimeMs = snapshot.GameTimeMs;
        _lastSnapshotBaselineSequence = snapshot.SnapshotSequence;
        _hasSnapshotBaseline = true;

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

    private static bool IsMeaningfulCorrection(VisualCorrection correction)
    {
        double distanceSq = correction.OffsetX * correction.OffsetX + correction.OffsetY * correction.OffsetY;
        return distanceSq > ReconciliationCorrectionToleranceWorldUnitsSq ||
               Math.Abs(correction.DirectionOffset) > ReconciliationCorrectionToleranceDegrees;
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
        var shipIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var state in _renderStates)
        {
            if (state.IsPlayerShip)
                shipIds.Add(state.Predicted.ObjectId);
        }

        foreach (var kvp in _trailStore.Trails)
        {
            var points = kvp.Value;
            if (points.Count < 2)
                continue;

            bool isShip = shipIds.Contains(kvp.Key);

            for (int i = 1; i < points.Count; i++)
            {
                var from = points[i - 1];
                var to = points[i];
                var (fromX, fromY) = _camera.WorldToScreen(from.X, from.Y, width, height);
                var (toX, toY) = _camera.WorldToScreen(to.X, to.Y, width, height);

                float t = (float)i / (points.Count - 1);
                _trailPaint.Color = GetTrailSegmentColor(t, isShip);
                canvas.DrawLine(fromX, fromY, toX, toY, _trailPaint);
            }
        }
    }

    /// <summary>
    /// Trail color by position fraction t (0 = tail's far/oldest end, 1 = at the object).
    /// For ships, the final third renders as fiery red (hot exhaust).
    /// Non-ship objects render plain gray throughout.
    /// Alpha fades from 40 at the tail to 160 at the object.
    /// </summary>
    internal static SKColor GetTrailSegmentColor(float t, bool isShip)
    {
        byte alpha = (byte)(40 + 120 * t);
        return isShip && t > 2.0f / 3.0f
            ? new SKColor(220, 30, 20, alpha)
            : new SKColor(190, 190, 190, alpha);
    }

    // ── Future trajectory ────────────────────────────────────────

    private void DrawFutureTrajectories(SKCanvas canvas, int width, int height)
    {
        if (!_showTrajectoryPrediction)
            return;

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

    // ── Navigation trajectory ────────────────────────────────────

    /// <summary>
    /// Draw the navigation trajectory (AC3/AC4/AC10): the authoritative-projected
    /// path when the snapshot carries an active navigate cycle (same
    /// NavigationWaypointMath as the engine), otherwise the pending preview — a plain
    /// straight line to the last Ctrl+Click target, drawn strictly client-side until
    /// the authoritative target arrives (≤1 s) or the command is rejected. Unconfirmed
    /// commands never affect motion prediction (architectural invariant).
    /// </summary>
    private void DrawNavigationTrajectories(SKCanvas canvas, int width, int height)
    {
        if (!_showTrajectoryPrediction)
            return;

        var snapshot = _buffer.Latest?.Snapshot;
        if (snapshot is not null)
        {
            var playerShip = FindPlayerShipMotion(snapshot);
            bool confirmed = playerShip?.NavigationTargetX is not null;
            bool rejected = !snapshot.CommandResults.IsDefaultOrEmpty &&
                            snapshot.CommandResults.Any(r =>
                                r.CommandType == ShipEngineCommandTypes.NavigateToPoint &&
                                r.Status == CommandResultStatus.Rejected);
            if (confirmed || rejected)
                _pendingNavigationTarget = null;
        }

        foreach (var state in _renderStates)
        {
            if (!state.IsPlayerShip)
                continue;

            var predicted = state.Predicted;
            if (predicted.NavigationTargetX is not null)
            {
                var points = _navigationTrajectoryProjector.Project(predicted);
                if (points.Count >= 2)
                {
                    for (int i = 1; i < points.Count; i++)
                    {
                        var from = points[i - 1];
                        var to = points[i];
                        var (fromX, fromY) = _camera.WorldToScreen(from.X, from.Y, width, height);
                        var (toX, toY) = _camera.WorldToScreen(to.X, to.Y, width, height);
                        canvas.DrawLine(fromX, fromY, toX, toY, _navigationTrajectoryPaint);
                    }
                }

                DrawNavigationTargetMarker(canvas, predicted.NavigationTargetX.Value, predicted.NavigationTargetY!.Value, width, height);
            }
            else if (_pendingNavigationTarget is { } pending)
            {
                var (sx, sy) = _camera.WorldToScreen(predicted.X, predicted.Y, width, height);
                var (tx, ty) = _camera.WorldToScreen(pending.X, pending.Y, width, height);
                canvas.DrawLine(sx, sy, tx, ty, _navigationTrajectoryPaint);
                DrawNavigationTargetMarker(canvas, pending.X, pending.Y, width, height);
            }
        }
    }

    private void DrawNavigationTargetMarker(SKCanvas canvas, double worldX, double worldY, int width, int height)
    {
        var (x, y) = _camera.WorldToScreen(worldX, worldY, width, height);
        canvas.DrawCircle(x, y, 1.5f, _navigationTargetPaint);
    }

    // ── Test seams for future trajectory ─────────────────────────

    /// <summary>
    /// Test seam: navigation trajectory of the given object (player ship only).
    /// Empty when the object has no authoritative navigation target in the snapshot.
    /// </summary>
    internal IReadOnlyList<FutureTrajectoryPoint> GetNavigationTrajectory(string objectId)
    {
        foreach (var state in _renderStates)
        {
            if (state.Predicted.ObjectId == objectId)
            {
                if (!state.IsPlayerShip || state.Predicted.NavigationTargetX is null)
                    return Array.Empty<FutureTrajectoryPoint>();

                return _navigationTrajectoryProjector.Project(state.Predicted);
            }
        }

        return Array.Empty<FutureTrajectoryPoint>();
    }

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
    /// Continuous scale position in button space: 0 = M0.5 (PPU 2.0),
    /// 1 = M1 (PPU 1.0), 2 = M10 (PPU 0.1), 3 = M100 (PPU 0.01),
    /// 4 = M1000 (PPU 0.001). Piecewise-log interpolation between
    /// neighboring buttons (ScaleTargets sorted by descending PPU),
    /// clamped to [0, ScaleTargets.Length - 1]. Exact button values
    /// yield integer positions, so the indicator sits exactly under
    /// the active button.
    /// </summary>
    private double ScalePosition()
    {
        // PPU is always clamped to a positive range by CameraState, so log10 is defined.
        double logPpu = Math.Log10(_camera.PixelsPerWorldUnit);

        for (int i = 0; i < ScaleTargets.Length - 1; i++)
        {
            double logHigh = Math.Log10(ScaleTargets[i]);
            double logLow = Math.Log10(ScaleTargets[i + 1]);
            if (logPpu <= logHigh && logPpu >= logLow)
            {
                double position = i + (logHigh - logPpu) / (logHigh - logLow);
                return Math.Clamp(position, 0, ScaleTargets.Length - 1);
            }
        }

        return logPpu > Math.Log10(ScaleTargets[0]) ? 0 : ScaleTargets.Length - 1;
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

    /// <summary>
    /// Draw the player ship glyph scaled to the policy marker radius
    /// (10 px diameter at default scale): nose = radius, half-width =
    /// radius * 5/7 — the whole glyph fits inside the 10 px marker.
    /// </summary>
    private void DrawPlayerShipGlyph(SKCanvas canvas, float sx, float sy, double directionDegrees, float radius)
    {
        double radians = directionDegrees * Math.PI / 180.0;
        float dx = (float)Math.Sin(radians);
        float dy = -(float)Math.Cos(radians);
        float rx = dy;
        float ry = -dx;
        float halfWidth = radius * 5f / 7f;

        var nose = new SKPoint(sx + dx * radius, sy + dy * radius);
        var left = new SKPoint(sx - dx * halfWidth - rx * halfWidth, sy - dy * halfWidth - ry * halfWidth);
        var right = new SKPoint(sx - dx * halfWidth + rx * halfWidth, sy - dy * halfWidth + ry * halfWidth);

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

            var icon = _engineCommandButtonIcons[i];
            if (icon is not null)
            {
                byte iconAlpha = buttonEnabled ? (byte)255 : (byte)110;
                _commandBtnIconPaint.Color = new SKColor(255, 255, 255, iconAlpha);
                canvas.DrawBitmap(icon, rect, _commandBtnIconPaint);

                var borderPaint = isPressed
                    ? _commandBtnPressedBorderPaint
                    : isHover ? _commandBtnHoverBorderPaint : _panelBorderPaint;
                canvas.DrawRect(rect, borderPaint);
            }
            else
            {
                canvas.DrawRect(rect, _panelBorderPaint);
                _commandBtnTextPaint.Color = buttonEnabled
                    ? new SKColor(210, 218, 214)
                    : new SKColor(96, 96, 96);
                float textY = rect.MidY + _commandBtnTextPaint.TextSize / 3f;
                canvas.DrawText(EngineCommandButtons[i].Label, rect.MidX, textY, _commandBtnTextPaint);
            }

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

        var ship = FindPlayerShipMotion(snapshot);
        if (ship is null)
            return false;

        // Rule 1: only the button matching the currently active periodic
        // (cyclic) command is disabled — every other button stays active.
        if (IsCyclicEngineCommand(commandType) && commandType == ship.ActiveEngineCommandType)
            return false;

        // Rule 2: Brake is disabled at zero speed.
        if (commandType == ShipEngineCommandTypes.Brake && ship.SpeedKmS <= 0)
            return false;

        // Rule 3: Accelerate is disabled at max speed.
        if (commandType == ShipEngineCommandTypes.Accelerate &&
            ship.MaxSpeedKmS is { } max && ship.SpeedKmS >= max)
            return false;

        return true;
    }

    private static ObjectMotionSnapshot? FindPlayerShipMotion(AuthoritativeSnapshot? snapshot)
    {
        if (string.IsNullOrWhiteSpace(snapshot?.PlayerShipObjectId))
            return null;

        foreach (var obj in snapshot.Objects)
        {
            if (obj.ObjectId == snapshot.PlayerShipObjectId)
                return obj;
        }

        return null;
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

    // ── Icon loading ────────────────────────────────────────────

    private static SKBitmap?[] LoadEngineCommandIcons()
    {
        var icons = new SKBitmap?[EngineCommandButtons.Length];
        for (int i = 0; i < EngineCommandButtons.Length; i++)
        {
            icons[i] = LoadButtonIcon(
                $"Images/UI/GameSessionScreenUI/navigation-panel/{EngineCommandButtons[i].IconFileName}");
        }
        return icons;
    }

    private static SKBitmap? LoadButtonIcon(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            using var bitmap = SKBitmap.Decode(path);
            if (bitmap is null)
                return null;

            return bitmap.Resize(
                new SKSizeI((int)CommandBtnSize, (int)CommandBtnSize),
                SKFilterQuality.High);
        }
        catch (IOException)
        {
            return null;
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

internal readonly record struct EngineCommandButton(string Label, string CommandType, string IconFileName);
