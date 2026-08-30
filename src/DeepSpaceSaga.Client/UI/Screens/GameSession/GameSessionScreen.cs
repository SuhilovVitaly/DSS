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
    private readonly TacticalMapDepthRenderer _depthRenderer;
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

    /// <summary>
    /// UI-only scale factor applied to the GameSession overlay panels (top-left
    /// Commands Panel, top-right scale/speed panels, bottom info panels). Never
    /// affects the tactical map, camera, or hit-testing against map objects — only
    /// the UI-pass canvas transform and the UI-space mouse coordinates derived
    /// from it.
    /// </summary>
    private float _uiScale = 1.0f;
    private static readonly float[] AllowedUiScales = { 0.8f, 1.0f, 1.2f, 1.5f };

    // Object paints
    private readonly SKPaint _trailPaint;
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

    private int _viewportW;
    private int _viewportH;
    private float _uiViewportW;
    private float _uiViewportH;
    private int _lastViewportW;
    private int _lastViewportH;
    private float _mouseX;
    private float _mouseY;
    private float _uiMouseX;
    private float _uiMouseY;
    private bool _isPanningMap;
    private float _panLastScreenX;
    private float _panLastScreenY;
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

    // Mechanics panel (bottom-center) state — holds the Finance/Ship/... buttons
    // (Docs/FirstRelease/Screens/Finance.md, Docs/FirstRelease/Screens/Ship.md)
    private SKRect _lastMechanicsPanelRect;
    private SKRect _lastFinanceButtonRect;
    private SKRect _lastShipButtonRect;
    private bool _isFinanceButtonHovered;
    private bool _isShipButtonHovered;

    // Commands Panel (top-left) — ТЗ подзадача 1 skeleton + ТЗ-04 data-driven buttons
    private readonly CommandsPanel _commandsPanel;

    // Mechanics panel paints (bottom-center)
    private readonly SKPaint _mechanicsBtnNormalPaint;
    private readonly SKPaint _mechanicsBtnHoverPaint;
    private readonly SKPaint _mechanicsBtnTextPaint;

    // Camera state
    private bool _isFocusAttachedToPlayer = true;

    // Navigation (Ctrl+Click) state — ТЗ: Navigation Waypoints
    private bool _isCtrlLeftDown;
    private bool _isCtrlRightDown;
    private bool IsCtrlDown => _isCtrlLeftDown || _isCtrlRightDown;

    // Object interaction state — ТЗ: ActiveObject and SelectedObject
    private string? _activeObjectId;
    private string? _selectedObjectId;

    /// <summary>Last snapshot sequence <see cref="ConsumePendingAutoTransition"/> already
    /// checked for a freshly-Executed navigation.dock CommandResult — see that method.</summary>
    private ulong? _lastAutoTransitionCheckedSnapshotSequence;
    /// <summary>
    /// True once a real OnMouseMove has reported a position on THIS activation of the
    /// screen. False before the first-ever move (avoids treating the (0,0) field default
    /// as a real cursor position) and reset on every OnActivated/OnDeactivated so a modal
    /// round-trip (e.g. GameMenu closing) never reuses a stale pre-modal position — the
    /// panel/Engine correctly show no ActiveObjectId until a fresh move arrives.
    /// </summary>
    private bool _hasMousePosition;


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

    // Mechanics panel (bottom-center) layout — one button per gameplay-mechanic
    // window (Finance, Ship, ...), all sharing the same button size.
    private const float MechanicsPanelPadding = 6f;
    private const float MechanicsButtonWidth = 56f;
    private const float MechanicsButtonHeight = 32f;
    private const float MechanicsButtonGap = 6f;

    private const string PlayerEngineModuleId = "MOD-PLAYER-ENGINE-01";

    /// <summary>
    /// Fixed screen-space hit-test radius for ActiveObjectId/SelectedObjectId (ТЗ §54):
    /// exactly 30 px from the drawn marker center (ObjectRenderState.Predicted),
    /// independent of zoom, marker size, and uiScale. Border-inclusive (&lt;=).
    /// </summary>
    private const float ObjectHitTestRadiusPx = 30f;

    private static readonly string[] SpeedLabels = { "II", "1x", "5x", "20x", "100x" };
    private static readonly SimulationSpeed[] SpeedValues =
        { SimulationSpeed.Speed0, SimulationSpeed.Speed1, SimulationSpeed.Speed2, SimulationSpeed.Speed3, SimulationSpeed.Speed4 };
    private static readonly string[] ScaleLabels = { "M0.5", "M1", "M10", "M100", "M1000" };
    private static readonly double[] ScaleTargets = { 2.0, 1.0, 0.1, 0.01, 0.001 };
    private static readonly double WheelMinPpu = ScaleTargets.Min();
    private static readonly double WheelMaxPpu = ScaleTargets.Max();

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
    internal SKRect LastMechanicsPanelRect => _lastMechanicsPanelRect;
    internal SKRect LastFinanceButtonRect => _lastFinanceButtonRect;
    internal SKRect LastShipButtonRect => _lastShipButtonRect;
    internal IReadOnlyList<string> ScalePanelLabels => ScaleLabels;
    internal float ScaleIndicatorCenterX => ComputeScaleIndicatorPosition();
    internal CommandsPanel CommandsPanel => _commandsPanel;
    internal float UiScale => _uiScale;

    /// <summary>Current frame's render list (scale-filtered, client-side).</summary>
    internal IReadOnlyList<ObjectRenderState> RenderStates => _renderStates;
    internal bool IsFocusAttachedToPlayer => _isFocusAttachedToPlayer;
    internal IReadOnlyList<ObjectTrailPoint> GetObjectTrail(string objectId) => _trailStore.GetTrail(objectId);
    internal string? ActiveObjectId => _activeObjectId;
    internal string? SelectedObjectId => _selectedObjectId;

    // ── Constructor ─────────────────────────────────────────────

    public GameSessionScreen(
        SnapshotBuffer buffer,
        IMotionPredictor predictor,
        GameSessionHandle? handle = null,
        Func<long>? timestampProvider = null,
        bool showTrajectoryPrediction = true,
        float uiScale = 1.0f)
    {
        _buffer = buffer;
        _predictor = predictor;
        _handle = handle;
        _timestampProvider = timestampProvider ?? Stopwatch.GetTimestamp;
        _showTrajectoryPrediction = showTrajectoryPrediction;
        _uiScale = ValidateUiScale(uiScale);
        _uiTimeStartTimestamp = _timestampProvider();

        _camera = new CameraState(focusX: 10000, focusY: 10000, pixelsPerWorldUnit: 1.0);
        _grid = new GridRenderer();
        _trailStore = new ObjectTrailStore(_predictor, _timestampProvider);
        _futureTrajectoryProjector = new FutureTrajectoryProjector(_predictor);
        _navigationTrajectoryProjector = new NavigationTrajectoryProjector();
        _labelRenderer = new ObjectLabelRenderer();
        _depthRenderer = new TacticalMapDepthRenderer();

        _trailPaint = new SKPaint { Color = new SKColor(190, 190, 190, 160), Style = SKPaintStyle.Stroke, StrokeWidth = 2f, IsAntialias = true };

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

        _mechanicsBtnNormalPaint = new SKPaint { Color = new SKColor(30, 30, 30), Style = SKPaintStyle.Fill };
        _mechanicsBtnHoverPaint = new SKPaint { Color = new SKColor(55, 55, 55), Style = SKPaintStyle.Fill };
        _mechanicsBtnTextPaint = new SKPaint { Color = new SKColor(200, 200, 200), TextSize = 13f, IsAntialias = true, Typeface = typeface, TextAlign = SKTextAlign.Center };

        _commandsPanel = new CommandsPanel(IsModuleCommandEnabled, SendCommandFromPanel);
    }

    /// <summary>
    /// Falls back to 1.0 (100%) whenever the requested scale is outside the allowed
    /// set — an invalid uiScale must never break rendering.
    /// </summary>
    private static float ValidateUiScale(float scale) =>
        Array.Exists(AllowedUiScales, v => Math.Abs(v - scale) < 0.001f) ? scale : 1.0f;

    /// <summary>
    /// Applies a new UI scale immediately to this already-open session screen
    /// (called from Settings while the game session is running). The map, camera,
    /// and simulation state are never touched by this call.
    /// </summary>
    internal void SetUiScale(float scale) => _uiScale = ValidateUiScale(scale);

    // ── IScreen ─────────────────────────────────────────────────

    public void OnActivated()
    {
        // Wait for a fresh MouseMove before hit-testing again — the position we had
        // before this screen was last deactivated (e.g. under a since-closed modal) is
        // stale and must not silently reactivate an object under it.
        _hasMousePosition = false;
    }

    public void OnDeactivated()
    {
        _isCtrlLeftDown = false;
        _isCtrlRightDown = false;
        _hasMousePosition = false;
        SetActiveObjectId(null);
    }

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        // UI panels are laid out and hit-tested in logical (unscaled) space — the
        // raw window coordinates must be converted before testing against them.
        // The map (below) always uses the raw x, y.
        float uiX = x / _uiScale;
        float uiY = y / _uiScale;

        if (button == MouseButton.Right)
        {
            // Right-click on a UI panel is not a map click — it must not reset the
            // selection (ТЗ §54). Elsewhere on the map, right-click always clears
            // SelectedObjectId (regardless of hitting an object or empty space),
            // never touches ActiveObjectId, and never moves the camera or sends a
            // navigation command.
            if (!IsClickOnUiPanel(uiX, uiY))
                SetSelectedObjectId(null);

            return ScreenEvent.None;
        }

        if (button != MouseButton.Left)
            return ScreenEvent.None;

        // 0. Scale panel buttons (left of speed panel — check first)
        int scaleIdx = HitTestScalePanel(uiX, uiY);
        if (scaleIdx >= 0)
        {
            ApplyScale(ScaleTargets[scaleIdx]);
            return ScreenEvent.None;
        }

        // 1. Speed panel buttons
        int speedIdx = HitTestSpeedPanel(uiX, uiY);
        if (speedIdx >= 0)
        {
            ApplySpeed(SpeedValues[speedIdx]);
            return ScreenEvent.None;
        }

        // 1.5. Mechanics panel buttons (bottom-center)
        if (_lastFinanceButtonRect.Contains(uiX, uiY))
            return ScreenEvent.OpenFinance;
        if (_lastShipButtonRect.Contains(uiX, uiY))
            return ScreenEvent.OpenShip;

        // 2. Commands Panel (top-left) — consume clicks, don't pan (ТЗ подзадача 1)
        if (_commandsPanel.OnMouseDown(uiX, uiY))
            return ScreenEvent.None;

        // 3. Info panel close button
        if (_panelVisible && _lastCloseRect.Contains(uiX, uiY))
        {
            _panelVisible = false;
            return ScreenEvent.None;
        }

        // 4. Info panel (consume, don't pan)
        if (_panelVisible && _lastPanelRect.Contains(uiX, uiY))
        {
            return ScreenEvent.None;
        }

        // 5. Player ship info panel (consume, don't pan)
        if (_lastPlayerShipPanelRect.Contains(uiX, uiY))
        {
            return ScreenEvent.None;
        }

        // 5.5. Object selection takes priority over both plain pan and Ctrl+Click
        // navigation (ТЗ §54, TacticalMapSpecification.md line 79: "клик поглощается,
        // камера не двигается, navigation command не отправляется"): a left click
        // within the 30 px hit radius of a visible object selects it — the camera
        // does not move and no navigation command is sent, whether or not Ctrl is
        // held. Selecting the player ship itself is the one exception: it reattaches
        // camera focus to the player (existing, still-wanted behavior), same as
        // Ctrl+C. Selecting any OTHER object leaves camera state completely
        // untouched (UX change, story-20260827-083137.md: selecting an object no
        // longer makes the camera follow/re-center on it).
        string? hitObjectId = FindNearestObjectId(x, y);
        if (hitObjectId is not null)
        {
            SetSelectedObjectId(hitObjectId);

            if (hitObjectId == _buffer.Latest?.Snapshot.PlayerShipObjectId)
            {
                _isFocusAttachedToPlayer = true;
            }

            // While docked, a successful navigation.dock physically snaps the ship onto
            // the station with only a (1, 1) world-unit offset (requirements Docking.md)
            // — at any normal zoom the two markers sit within 1-2 screen px of each other,
            // so FindNearestObjectId's nearest/tie-break result between them is effectively
            // unpredictable pixel-by-pixel. Clicking either the docked station OR the
            // player ship itself (authoritative IsDocked/DockedStationObjectId from the
            // snapshot) therefore (re)opens the Station screen — clicking "the ship" and
            // clicking "the station" are the same physical spot once docked. Docking
            // itself is unaffected by this click either way.
            var snapshot = _buffer.Latest?.Snapshot;
            var playerShip = snapshot?.Objects.FirstOrDefault(o => o.ObjectId == snapshot.PlayerShipObjectId);
            if (playerShip is { IsDocked: true } dockedShip &&
                (hitObjectId == dockedShip.DockedStationObjectId || hitObjectId == dockedShip.ObjectId))
            {
                return ScreenEvent.OpenStation;
            }

            return ScreenEvent.None;
        }

        // 5.6. Ctrl+Click navigation: free map area only (object case handled above)
        // — send exactly one engine.orbit command with world coordinates.
        // The camera focus is NOT changed.
        if (IsCtrlDown)
        {
            var (navWorldX, navWorldY) = _camera.ScreenToWorld(x, y, _viewportW, _viewportH);
            SendNavigationCommand(navWorldX, navWorldY);
            return ScreenEvent.None;
        }

        // 6. Map click -> start a potential drag-pan. The camera no longer jumps/
        // re-centers on a plain click by itself (disabled per user feedback: the
        // jump fought with dragging, making it feel broken) — only actual mouse
        // movement while held pans the camera, in OnMouseMove below.
        _isFocusAttachedToPlayer = false;
        _isPanningMap = true;
        _panLastScreenX = x;
        _panLastScreenY = y;
        return ScreenEvent.None;
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call sites/tests.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    public bool OnMouseMove(float x, float y)
    {
        // _mouseX/_mouseY stay raw (displayed as "Cursor Window" and used to compute
        // "Cursor Game" via the camera); _uiMouseX/_uiMouseY are the logical-space
        // coordinates UI panels hover-test against.
        _mouseX = x;
        _mouseY = y;
        _uiMouseX = x / _uiScale;
        _uiMouseY = y / _uiScale;
        _hasMousePosition = true;

        if (_isPanningMap)
        {
            float dx = x - _panLastScreenX;
            float dy = y - _panLastScreenY;
            if (dx != 0f || dy != 0f)
            {
                double ppu = _camera.PixelsPerWorldUnit;
                _camera.SetFocus(_camera.FocusX - dx / ppu, _camera.FocusY - dy / ppu);
            }
            _panLastScreenX = x;
            _panLastScreenY = y;
        }

        RecomputeActiveObjectId();
        _isFinanceButtonHovered = _lastFinanceButtonRect.Contains(_uiMouseX, _uiMouseY);
        _isShipButtonHovered = _lastShipButtonRect.Contains(_uiMouseX, _uiMouseY);
        return _commandsPanel.OnMouseMove(_uiMouseX, _uiMouseY) || _isFinanceButtonHovered || _isShipButtonHovered;
    }

    public void OnMouseUp(float x, float y)
    {
        _mouseX = x;
        _mouseY = y;
        _uiMouseX = x / _uiScale;
        _uiMouseY = y / _uiScale;
        _isPanningMap = false;
        _commandsPanel.OnMouseUp(_uiMouseX, _uiMouseY);
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

        if (key == Key.C && IsCtrlDown)
        {
            // Reattach camera focus to the player ship. Object selection no longer
            // makes the camera follow another object (story-20260827-083137.md UX
            // change), so there is no "detach from a followed non-player object"
            // case to handle here anymore — Ctrl+C always reattaches to the player.
            _isFocusAttachedToPlayer = true;
            return ScreenEvent.None;
        }

        if (key == Key.Escape)
            return ScreenEvent.OpenGameMenu;

        if (key == Key.I)
        {
            _panelVisible = true;
            return ScreenEvent.None;
        }

        // Only ever arrives as Ctrl+F — KeyboardEdgeTracker gates the F edge on Ctrl,
        // mirroring Ctrl+I above.
        if (key == Key.F)
            return ScreenEvent.OpenFinance;

        // Only ever arrives as Ctrl+S — same gating as Ctrl+F/Ctrl+I above.
        if (key == Key.S)
            return ScreenEvent.OpenShip;

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

    private void SendEngineCommand(string commandType)
    {
        var playerShipObjectId = _buffer.LatestPrediction?.BufferedSnapshot.Snapshot.PlayerShipObjectId;
        if (_handle is null || string.IsNullOrWhiteSpace(playerShipObjectId))
            return;

        _ = _handle.SendEngineCommandAsync(playerShipObjectId, PlayerEngineModuleId, commandType);
    }

    // ── Commands Panel (top-left) hooks — ТЗ-04 ─────────────────

    /// <summary>
    /// Data-driven enablement for Commands Panel buttons: the target requirement
    /// comes from the snapshot's per-module command metadata. "point" commands are
    /// never enabled from the panel (Ctrl+Click map navigation is the only path);
    /// "object" commands (match/scanner) require SelectedObjectId; engine commands
    /// additionally respect the engine command panel rules. A button is also
    /// disabled when no installed module currently exposes the commandType, or when
    /// its target metadata is missing (never send a command blind). navigation.stationsList
    /// has no station-list screen yet, so it stays visible but always disabled.
    /// </summary>
    private bool IsModuleCommandEnabled(string commandType)
    {
        if (commandType == NavigationComputerCommandTypes.StationsList)
            return false; // no station-list screen yet — visible, always disabled this pass

        if (ResolveModuleId(commandType) is null)
            return false; // no installed module exposes this commandType

        string? target = FindCommandTarget(commandType);
        switch (target)
        {
            case "point":
                return false;
            case "object":
                return _selectedObjectId is not null;
            case "none":
                return commandType.StartsWith("engine.", StringComparison.Ordinal)
                    ? CanSendEngineCommand(commandType, _buffer.Latest?.Snapshot)
                    : true;
            default:
                return false; // metadata missing — never send a command blind
        }
    }

    /// <summary>
    /// Send a command from a Commands Panel button. Fire-and-forget like the engine
    /// panel — the authoritative engine validates the command and its target. The
    /// panel groups commands by gameplay meaning, so the addressed installed module
    /// is resolved here by matching CommandTypeIds (first module by Position).
    /// </summary>
    private void SendCommandFromPanel(string commandType)
    {
        string? moduleId = ResolveModuleId(commandType);
        if (moduleId is null)
            return; // defensive — IsModuleCommandEnabled already gates this

        var playerShipObjectId = _buffer.LatestPrediction?.BufferedSnapshot.Snapshot.PlayerShipObjectId;
        if (_handle is null || string.IsNullOrWhiteSpace(playerShipObjectId))
            return;

        string? targetObjectId = FindCommandTarget(commandType) == "object" ? _selectedObjectId : null;
        _ = _handle.SendCommandAsync(playerShipObjectId, moduleId, commandType, targetObjectId);
    }

    /// <summary>
    /// Resolves the installed module that should receive <paramref name="commandType"/>:
    /// the first module (ordered by Position) whose CommandTypeIds contains it.
    /// </summary>
    private string? ResolveModuleId(string commandType)
    {
        var modules = _buffer.Latest?.Snapshot.InstalledModules;
        if (modules is null || modules.Value.IsDefaultOrEmpty)
            return null; // no snapshot yet, or InstalledModules left at its default (uninitialized) value

        return modules.Value
            .Where(m => m.CommandTypeIds.Contains(commandType))
            .OrderBy(m => m.Position)
            .Select(m => m.ModuleId)
            .FirstOrDefault();
    }

    /// <summary>Target requirement ("none"/"point"/"object") for a command type, from the buffered snapshot's Commands metadata.</summary>
    private string? FindCommandTarget(string commandType)
    {
        var snapshot = _buffer.Latest?.Snapshot;
        if (snapshot is null)
            return null;

        foreach (var module in snapshot.InstalledModules)
        {
            if (module.Commands.IsDefaultOrEmpty)
                continue;

            foreach (var command in module.Commands)
            {
                if (command.CommandTypeId == commandType)
                    return command.Target;
            }
        }

        return null;
    }

    /// <summary>
    /// Ctrl+Click navigation: send exactly one engine.orbit command with
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
            return;
        }

        _ = _handle.SendEngineCommandAsync(
            playerShipObjectId, PlayerEngineModuleId, ShipEngineCommandTypes.Orbit, worldX, worldY);
    }

    /// <summary>
    /// Click-selection priority when several objects fall within the hit-test radius at
    /// once (lower number wins): Station, then the player's own ship, then any other ship
    /// (NPC), then everything else (asteroids, planets, the sun, unknown objects, ...).
    /// Distance is only the tiebreaker within the same tier — see
    /// <see cref="FindNearestObjectId"/>. IsPlayerShip (identity), not RenderObjectType, is
    /// what marks the player's own ship — RenderObjectType for it is
    /// <see cref="SpaceObjectType.PlayerShip"/> too, but IsPlayerShip is the authoritative
    /// signal used everywhere else in this file for the same distinction.
    /// </summary>
    private static int GetClickPriority(ObjectRenderState state)
    {
        if (state.Predicted.RenderObjectType == SpaceObjectType.Station)
            return 0;
        if (state.IsPlayerShip)
            return 1;
        if (state.Predicted.RenderObjectType == SpaceObjectType.NpcShip)
            return 2;
        return 3;
    }

    /// <summary>
    /// Highest-priority (see <see cref="GetClickPriority"/>), then nearest, visible object
    /// within <see cref="ObjectHitTestRadiusPx"/> screen pixels of (x, y), or null if none
    /// qualifies (ТЗ §54). Only objects currently in <see cref="_renderStates"/> (i.e.
    /// passing the scale visibility filter) participate. Distance comparisons use squared
    /// distance (no sqrt); ties (equal priority AND equal squared distance) break on the
    /// lexicographically smaller ObjectId (Ordinal) so the result never depends on
    /// iteration/snapshot order.
    /// </summary>
    private string? FindNearestObjectId(float x, float y)
    {
        string? bestId = null;
        int bestPriority = int.MaxValue;
        double bestDistanceSq = double.MaxValue;
        const double radiusSq = (double)ObjectHitTestRadiusPx * ObjectHitTestRadiusPx;

        for (int i = 0; i < _renderStates.Count; i++)
        {
            var state = _renderStates[i];
            var (sx, sy) = _camera.WorldToScreen(state.Predicted.X, state.Predicted.Y, _viewportW, _viewportH);
            double dx = x - sx;
            double dy = y - sy;
            double distanceSq = dx * dx + dy * dy;
            if (distanceSq > radiusSq)
                continue;

            int priority = GetClickPriority(state);

            if (bestId is null ||
                priority < bestPriority ||
                (priority == bestPriority &&
                 (distanceSq < bestDistanceSq ||
                  (distanceSq == bestDistanceSq &&
                   string.CompareOrdinal(state.Predicted.ObjectId, bestId) < 0))))
            {
                bestId = state.Predicted.ObjectId;
                bestPriority = priority;
                bestDistanceSq = distanceSq;
            }
        }

        return bestId;
    }

    /// <summary>
    /// Recompute ActiveObjectId from the current cursor position against the current
    /// <see cref="_renderStates"/> (ТЗ §54). Called on every OnMouseMove and again every
    /// Render (after render states/camera are refreshed) so that camera pan/zoom, object
    /// motion under a stationary cursor, and objects appearing/disappearing all keep it
    /// current without waiting on an extra input event.
    /// Requires a real position: before the first-ever OnMouseMove (or right after
    /// reactivation, before a fresh one arrives) <see cref="_hasMousePosition"/> is
    /// false, so the (0,0) field default never masquerades as "cursor at the top-left
    /// corner". Silk.NET/SkiaWindow have no mouse-leave signal, so a position outside
    /// the current viewport (possible once the OS cursor has actually left the window
    /// and no further OnMouseMove arrives) is also treated as "no cursor" rather than
    /// hit-testing against a stale in-window position.
    /// </summary>
    private void RecomputeActiveObjectId()
    {
        bool cursorInViewport = _hasMousePosition &&
            _viewportW > 0 && _viewportH > 0 &&
            _mouseX >= 0 && _mouseX <= _viewportW &&
            _mouseY >= 0 && _mouseY <= _viewportH;

        string? candidate = cursorInViewport ? FindNearestObjectId(_mouseX, _mouseY) : null;
        SetActiveObjectId(candidate);
    }

    private void SetActiveObjectId(string? objectId)
    {
        if (_activeObjectId == objectId)
            return;

        _activeObjectId = objectId;
        NotifyInteractionStateChanged();
    }

    private void SetSelectedObjectId(string? objectId)
    {
        if (_selectedObjectId == objectId)
            return;

        _selectedObjectId = objectId;
        NotifyInteractionStateChanged();
    }

    /// <summary>
    /// Push the full (ActiveObjectId, SelectedObjectId) pair to the engine — only when
    /// running with a real session (_handle is null in tests that construct the screen
    /// directly against a SnapshotBuffer). Non-blocking: GameSessionHandle queues and
    /// sends in the background (ТЗ §54 "Ограничение частоты").
    /// </summary>
    private void NotifyInteractionStateChanged()
    {
        _handle?.UpdateObjectInteractionState(_activeObjectId, _selectedObjectId);
    }

    /// <summary>
    /// True when (uiX, uiY) lands on any currently-drawn UI panel/button — used to keep
    /// a right-click on a panel from being treated as a map click (ТЗ §54: "Правый клик
    /// по UI-панели не считается кликом по карте и не должен сбрасывать выбор"). Mirrors
    /// every panel hit-test the left-click handler already consumes clicks on, using the
    /// same rects computed by the last Render.
    /// </summary>
    private bool IsClickOnUiPanel(float uiX, float uiY)
    {
        if (HitTestScalePanel(uiX, uiY) >= 0)
            return true;
        if (HitTestSpeedPanel(uiX, uiY) >= 0)
            return true;
        if (_lastFinanceButtonRect.Contains(uiX, uiY))
            return true;
        if (_lastShipButtonRect.Contains(uiX, uiY))
            return true;
        if (_commandsPanel.CaptionRect.Contains(uiX, uiY) || _commandsPanel.BodyRect.Contains(uiX, uiY))
            return true;
        if (_panelVisible && (_lastCloseRect.Contains(uiX, uiY) || _lastPanelRect.Contains(uiX, uiY)))
            return true;
        if (_lastPlayerShipPanelRect.Contains(uiX, uiY))
            return true;

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

        // Recompute after render states + camera focus are current for this frame —
        // covers pan/zoom, camera-focus changes, and an object moving under a
        // stationary cursor (ТЗ §54). OnMouseMove already recomputes eagerly on input;
        // this catches every other trigger that isn't a mouse-move event.
        RecomputeActiveObjectId();

        if (_diagInterestingFrame && PauseResumeDiagnostics.Enabled)
        {
            PauseResumeDiagnostics.Write(
                $"CAMERA focusX={_camera.FocusX:F3} focusY={_camera.FocusY:F3} attached={_isFocusAttachedToPlayer}");
            _diagInterestingFrame = false;
        }

        // 1. Grid
        _grid.Draw(canvas, _camera, width, height);

        // 2. Camera focus indicator
        float cx = width / 2f;
        float cy = height / 2f;
        _depthRenderer.DrawFocusIndicator(canvas, cx, cy);

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

                // Selection takes visual priority when the same object is also active;
                // orange is reserved for hovered objects that are not selected.
                if (state.Predicted.ObjectId == _selectedObjectId)
                    _depthRenderer.DrawSelectionReticle(canvas, sx, sy, r, uiTimeMs);
                else if (state.Predicted.ObjectId == _activeObjectId)
                    _depthRenderer.DrawActiveObjectReticle(canvas, sx, sy, r, uiTimeMs);

                if (state.IsPlayerShip)
                {
                    if (state.Predicted.ActiveEngineCommandType == ShipEngineCommandTypes.Accelerate)
                    {
                        _depthRenderer.DrawEngineFlame(canvas, sx, sy, state.Predicted.Direction, r, uiTimeMs);
                    }
                    DrawPlayerShipGlyph(canvas, sx, sy, state.Predicted.Direction, r);
                }
                else
                {
                    var markerColor = SpaceMapColorResolver.GetColor(
                        state.Predicted.RenderObjectType, state.Predicted.RelationToPlayer);
                    if (TacticalMapMarkerPolicy.UsesGlintMarker(state.Predicted.RenderObjectType))
                    {
                        _depthRenderer.DrawGlintMarker(canvas, sx, sy, r, markerColor);
                    }
                    else
                    {
                        _depthRenderer.DrawSphericalMarker(canvas, sx, sy, r, markerColor);
                    }
                }
            }

            // 4.5. Object label plaques (on top of objects, before UI panels)
            _labelRenderer.DrawPlaques(canvas, _renderStates, uiTimeMs, _buffer.CurrentSpeed, width, height, _camera);
        }

        // UI overlay pass — everything from here on is a GameSession UI panel, never
        // the tactical map. Panels are laid out in logical (unscaled) coordinates;
        // the canvas transform is the only thing that grows them to _uiScale. Text
        // scales vectorially as part of this transform — base font sizes are never
        // multiplied, and the transform is never combined with a TextSize change.
        _uiViewportW = width / _uiScale;
        _uiViewportH = height / _uiScale;

        canvas.Save();
        canvas.Scale(_uiScale);

        // 4.75. Scale panel (top-right, left of speed panel)
        DrawScalePanel(canvas);

        // 5. Speed panel (top-right)
        DrawSpeedPanel(canvas);

        // 6. Commands Panel (top-left)
        _commandsPanel.Render(canvas,
            buffered?.Snapshot.InstalledModules ?? ImmutableArray<InstalledModuleSnapshot>.Empty);

        // 7. Info panel (bottom-left)
        if (_panelVisible)
            DrawInfoPanel(canvas, buffered);

        // 8. Player ship info panel (bottom-right)
        var playerShip = FindPlayerShip(_renderStates);
        DrawPlayerShipInfoPanel(canvas, playerShip);

        // 9. Mechanics panel (bottom-center) — Finance/Ship buttons
        DrawMechanicsPanel(canvas);

        canvas.Restore();
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

        // SelectedObjectId survives a scale-filter change (it isn't recomputed from
        // _renderStates like ActiveObjectId) but must still be cleared the moment the
        // object no longer exists in the authoritative world at all — checked against
        // every object in this snapshot, not just the scale-filtered _renderStates
        // (ТЗ §54 "Жизненный цикл объекта").
        if (_selectedObjectId is not null && !_currentVisualObjectIds.Contains(_selectedObjectId))
            SetSelectedObjectId(null);

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
            // The player ship always gets its own ballistic preview (subject to the
            // Approach exclusion below). A non-player object only gets one when it is
            // the current SelectedObjectId — e.g. the target of an active
            // navigation.approach — so the player can visually cross-check where that
            // object will actually be against the ship's own planned curve, without
            // drawing a preview for every object in the scene.
            bool isSelectedTarget = !state.IsPlayerShip &&
                _selectedObjectId is not null &&
                state.Predicted.ObjectId == _selectedObjectId;

            if (!state.IsPlayerShip && !isSelectedTarget)
                continue;

            if (!FutureTrajectoryProjector.ShouldDraw(state.Predicted))
                continue;

            // navigation.approach already gets its own single-color path from
            // DrawNavigationTrajectories (same pursuit math, run to actual arrival).
            // Drawing this generic straight-line projection on top of it produced a
            // visible two-color trajectory for the same course. Only applies to the
            // player ship — a selected target never has its own Approach command.
            if (state.IsPlayerShip && state.Predicted.ActiveEngineCommandType == NavigationComputerCommandTypes.Approach)
                continue;

            var points = _futureTrajectoryProjector.Project(state.Predicted);
            if (points.Count < 2)
                continue;

            _depthRenderer.DrawFutureTrajectory(canvas, points, _camera, width, height);
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

        foreach (var state in _renderStates)
        {
            if (!state.IsPlayerShip)
                continue;

            var predicted = state.Predicted;
            if (predicted.NavigationTargetX is not null)
            {
                // isConfirmedIntercept comes from the projector's OWN resolution, not
                // from re-checking predicted.NavigationPhase here: during the single
                // transient FlyThroughPendingPhase frame right after the command starts,
                // the projector already independently resolves (and draws) the confirmed
                // rendezvous curve one frame before the engine bakes that confirmation
                // into the authoritative NavigationPhase string — gating on the phase
                // string here would miss that first frame and read as "no intercept" even
                // though the curve shown IS already the confirmed one.
                var points = _navigationTrajectoryProjector.Project(
                    predicted, out bool isConfirmedIntercept, out var interceptPoint);
                if (points.Count >= 2)
                    _depthRenderer.DrawNavigationTrajectory(canvas, points, _camera, width, height);

                DrawNavigationTargetMarker(canvas, predicted.NavigationTargetX.Value, predicted.NavigationTargetY!.Value, width, height);

                // A confirmed intercept-solve curve (story-20260829-210641.md) is flown
                // to a fixed rendezvous pose baked once when the curve was built — unlike
                // NavigationTargetX/Y above, which is re-baked to the target's LIVE
                // position every cycle while the curve is still being flown (client-facing
                // metadata only) and so drifts away from where the curve actually ends.
                // interceptPoint is computed analytically from the target's live pose and
                // constant velocity (see NavigationTrajectoryProjector.ProjectFlyThrough),
                // NOT read off the drawn curve's own discretized tracked endpoint — that
                // endpoint accumulates enough per-cycle turn quantization over a long curve
                // to visibly miss the target's own drawn straight-line trajectory.
                if (isConfirmedIntercept)
                {
                    var (ix, iy) = _camera.WorldToScreen(interceptPoint.X, interceptPoint.Y, width, height);
                    _depthRenderer.DrawInterceptPoint(canvas, ix, iy);
                }
            }
        }
    }

    private void DrawNavigationTargetMarker(SKCanvas canvas, double worldX, double worldY, int width, int height)
    {
        var (x, y) = _camera.WorldToScreen(worldX, worldY, width, height);
        _depthRenderer.DrawNavigationTarget(canvas, x, y);
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
        float panelX = _uiViewportW - totalW - PanelMargin;
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

        _depthRenderer.DrawPlayerShipGlyph(
            canvas,
            _playerShipGlyphPath,
            radius,
            SpaceMapColorResolver.PlayerShipColor);
    }

    /// <summary>
    /// Polled once per frame by SkiaWindow's render loop (unlike every other ScreenEvent
    /// here, this one isn't produced by a direct input handler — a successful Dock is an
    /// authoritative outcome the client only learns about from the next snapshot, not
    /// synchronously when the button is clicked). Requirements Docking.md: "После
    /// успешного Dock экран станции открывается автоматически" — this is that wiring.
    /// Edge-triggered on SnapshotSequence so it fires at most once per completed Dock
    /// command, not on every frame the same snapshot stays "Latest" (which would also
    /// fight a player who deliberately closes the Station screen right after auto-open).
    /// </summary>
    internal ScreenEvent ConsumePendingAutoTransition()
    {
        var snapshot = _buffer.Latest?.Snapshot;
        if (snapshot is null || snapshot.SnapshotSequence == _lastAutoTransitionCheckedSnapshotSequence)
            return ScreenEvent.None;

        _lastAutoTransitionCheckedSnapshotSequence = snapshot.SnapshotSequence;

        bool justDocked = !snapshot.CommandResults.IsDefaultOrEmpty && snapshot.CommandResults.Any(r =>
            r.CommandType == NavigationComputerCommandTypes.Dock && r.Status == CommandResultStatus.Executed);

        return justDocked ? ScreenEvent.OpenStation : ScreenEvent.None;
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

    private static bool IsCyclicEngineCommand(string commandType)
    {
        return commandType == ShipEngineCommandTypes.Accelerate ||
               commandType == ShipEngineCommandTypes.Brake ||
               commandType == ShipEngineCommandTypes.TurnLeftUntilCancel ||
               commandType == ShipEngineCommandTypes.TurnRightUntilCancel;
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
        float panelY = _uiViewportH - panelH - PanelMargin;

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

        lines.Add(("Selected Id", _selectedObjectId ?? "—"));
        lines.Add(("Active Id", _activeObjectId ?? "—"));

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

        float panelX = _uiViewportW - panelW - PanelMargin;
        float panelY = _uiViewportH - panelH - PanelMargin;

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

    /// <summary>
    /// Bottom-center panel holding one button per gameplay-mechanic window — "F"
    /// opens Finance (Docs/FirstRelease/Screens/Finance.md), "S" opens Ship
    /// (Docs/FirstRelease/Screens/Ship.md). Ctrl+F / Ctrl+S open the same overlays
    /// without needing the buttons (see OnKeyDown). Clicking pushes
    /// ScreenEvent.OpenFinance / OpenShip, handled by SkiaWindow's generic
    /// PushModalAsync — pause-on-open/resume-on-close needs no logic here.
    /// </summary>
    private void DrawMechanicsPanel(SKCanvas canvas)
    {
        const int buttonCount = 2;
        float panelW = MechanicsButtonWidth * buttonCount + MechanicsButtonGap * (buttonCount - 1) + MechanicsPanelPadding * 2;
        float panelH = MechanicsButtonHeight + MechanicsPanelPadding * 2;
        float panelX = (_uiViewportW - panelW) / 2f;
        float panelY = _uiViewportH - panelH - PanelMargin;

        _lastMechanicsPanelRect = new SKRect(panelX, panelY, panelX + panelW, panelY + panelH);
        canvas.DrawRect(_lastMechanicsPanelRect, _panelBgPaint);
        canvas.DrawRect(_lastMechanicsPanelRect, _panelBorderPaint);

        float btnX = panelX + MechanicsPanelPadding;
        float btnY = panelY + MechanicsPanelPadding;

        _lastFinanceButtonRect = new SKRect(btnX, btnY, btnX + MechanicsButtonWidth, btnY + MechanicsButtonHeight);
        DrawMechanicsButton(canvas, _lastFinanceButtonRect, "F", _isFinanceButtonHovered);

        btnX += MechanicsButtonWidth + MechanicsButtonGap;
        _lastShipButtonRect = new SKRect(btnX, btnY, btnX + MechanicsButtonWidth, btnY + MechanicsButtonHeight);
        DrawMechanicsButton(canvas, _lastShipButtonRect, "S", _isShipButtonHovered);
    }

    private void DrawMechanicsButton(SKCanvas canvas, SKRect rect, string label, bool hovered)
    {
        canvas.DrawRect(rect, hovered ? _mechanicsBtnHoverPaint : _mechanicsBtnNormalPaint);
        canvas.DrawRect(rect, _panelBorderPaint);

        float textY = rect.MidY + _mechanicsBtnTextPaint.TextSize / 3f;
        canvas.DrawText(label, rect.MidX, textY, _mechanicsBtnTextPaint);
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
