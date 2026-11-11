using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

/// <summary>All information used to draw a battle frame is captured before painting it.</summary>
public sealed record TacticalBattleFrame(TacticalBattleSnapshot Battle, TacticalBattleView View, SKRect Panel,
    float UiScale, bool Pending, string Status, TacticalBattlePresentation Presentation = TacticalBattlePresentation.Current,
    bool EnemyForecast = true, bool EnemySectors = false, string? SelectedDroneId = null, TacticalMovementFrame? Movement = null,
    TacticalFirePlan? FirePlan = null, TacticalSalvoFrame? Salvo = null, bool AllFireSectors = true, string? HoverUnitId = null, SKPoint? HoverPoint = null, string? LastSelectedDroneId = null)
{
    public bool AimingMovement { get; init; }
}

public sealed partial class TacticalBattleScreen : TacticalBattleModalScreen
{
    private readonly TacticalBattleCamera _camera = new();
    private readonly TacticalBattleSkin _skin;
    private readonly ShortcutButton _exitBattle = new("", "Esc");
    private readonly ShortcutButton _phaseAction = new("", "Enter");
    private readonly ShortcutButton _newBattle = new("");
    private readonly ShortcutButton _replaySalvo = new("");
    private readonly Toggle _enemyForecast = new("", true);
    private readonly Toggle _enemySectors = new("", true);
    private readonly Toggle _allFireSectors = new("", true);
    private readonly TacticalBattleOrders _orders = new();
    private readonly TacticalBattleMovePanel _movePanel;
    private readonly TacticalBattleFireOrders _fireOrders = new();
    private readonly TacticalBattleFirePanel _firePanel;
    private readonly TabStrip _tabs = new([new("phase", L("Tab.Phase")), new("sides", L("Tab.Sides")),
        new("log", L("Tab.Log")), new("forecast", L("Tab.Forecast"))]);
    private readonly TacticalBattleSidesTab _sidesTab = new();
    private readonly TacticalBattleLogTab _logTab = new();
    private readonly TacticalBattleForecastTab _forecastTab = new();
    private bool _orderDrag, _orderDragEdited;
    private SKPoint _lastAim;
    private sealed record TurnPresentation(TacticalBattleSnapshot Accepted, TacticalBattleMovementAnimation? Animation,
        TacticalBattleResults? Results = null, ImmutableArray<TacticalMovementPath> PreviousPaths = default);
    private TurnPresentation? _presentation;
    private TacticalBattleSnapshot? _observedOrders;
    internal Func<long> ClockMilliseconds { get; set; } = () => Environment.TickCount64;
    private SKRect _panel;
    private string? _lastSelectedDroneId;
    internal string? HoverUnitId { get; set; }
    internal BattleLayout Layout { get; private set; } = new();
    internal SKRect NewBattleBounds => Raw(_phasePanel.ButtonBounds("new"));
    internal (SKRect Forecast, SKRect Sectors, SKRect AllFire) OverlayControls =>
        (_enemyForecast.Bounds, _enemySectors.Bounds, _allFireSectors.Bounds);
    internal TacticalBattleView View => _camera.View;
    internal string SkinId => _skin.Id;
    internal TacticalMovementFrame Movement => _orders.Capture();
    internal TacticalBattleMovePanel MovePanel => _movePanel;
    internal TacticalBattleFirePanel FirePanel => _firePanel;
    internal bool HasSalvoPresentation => Volatile.Read(ref _presentation)?.Results is not null;
    internal SKRect ReplaySalvoBounds => Raw(_phasePanel.ButtonBounds("replay"));
    internal TabStrip PanelTabs => _tabs;
    internal TacticalBattleSidesTab SidesTab => _sidesTab;
    internal TacticalBattleLogTab LogTab => _logTab;
    internal TacticalBattleForecastTab ForecastTab => _forecastTab;

    public TacticalBattleScreen(TacticalBattleFlow flow, float uiScale) : this(flow, uiScale, TacticalBattleSkin.Classic) { }

    internal TacticalBattleScreen(TacticalBattleFlow flow, float uiScale, TacticalBattleSkin skin) : base(flow, isSetup: false, uiScale)
    {
        _skin = skin;
        _movePanel = new(_orders);
        _firePanel = new(_fireOrders);
        _tabs.Changed += _ =>
        {
            CancelOrderGesture();
            _firePanel.ResetInput();
            _logTab.ResetInput();
            _forecastTab.ResetInput();
        };
        if (flow.PresentationState is { } state) _presentation = new(state, null);
    }

    internal bool TurnBusy
    {
        get
        {
            var presentation = Volatile.Read(ref _presentation);
            return Flow.Pending || presentation?.Accepted.Revision != Flow.State?.Revision ||
                presentation?.Animation?.IsActive(ClockMilliseconds()) == true;
        }
    }

    internal bool CanSubmitTurn(ScreenEvent evt) => !TurnBusy && !HasSalvoPresentation && !ConfirmExit &&
        Flow.State?.Phase == (evt == ScreenEvent.MoveTacticalBattle ? TacticalBattlePhase.Move : TacticalBattlePhase.Fire);

    private bool SalvoInFlight => Volatile.Read(ref _presentation)?.Results?.InFlight(ClockMilliseconds()) == true;
    internal bool CanStartNewBattle => !TurnBusy && !SalvoInFlight && !ConfirmExit && Flow.State is not null;
    private bool OutcomeVisible => Flow.State?.Phase == TacticalBattlePhase.Over && !SalvoInFlight;

    internal TacticalBattleFrame? CaptureFrame(int width, int height)
    {
        if (Volatile.Read(ref _presentation) is not { } presentation || width < 2 || height < 1) return null;
        long now = ClockMilliseconds();
        bool moving = presentation.Animation?.IsActive(now) == true;
        var state = moving ? presentation.Animation!.Interpolate(now) : presentation.Accepted;
        if (moving && !presentation.PreviousPaths.IsDefaultOrEmpty)
            state = state with { Paths = presentation.PreviousPaths.AddRange(state.Paths) };
        var salvo = moving ? null : presentation.Results?.Capture(now);
        if (!moving && presentation.Results is { } results) state = results.Project(now);
        bool planning = !moving && salvo is null;
        _viewport = BattleViewport.From(width, height);
        _camera.Update(state, (int)_viewport.Wd, 720);
        if (planning)
        {
            ObserveOrders(state);
            _lastSelectedDroneId = state.Phase == TacticalBattlePhase.Fire ? _fireOrders.Selected : _orders.Selected;
        }
        var firePlan = planning && state.Phase == TacticalBattlePhase.Fire ? _fireOrders.Capture() : null;
        _panel = new(_viewport.Wd - 240, 6, _viewport.Wd - 6, OverlayBottom(state, salvo, firePlan));
        return new(state, _camera.View, _panel, 1, moving || Flow.Pending || presentation.Accepted.Revision != Flow.State?.Revision,
            BlockedNotice is { Length: > 0 } notice ? notice : moving ? L("AnimatingMovement") : Flow.Error,
            Presentation: salvo is null ? TacticalBattlePresentation.Current :
                salvo.InFlight ? TacticalBattlePresentation.SalvoFlight : TacticalBattlePresentation.SalvoResults,
            EnemyForecast: false, EnemySectors: false,
            SelectedDroneId: planning ? state.Phase == TacticalBattlePhase.Fire ? _fireOrders.Selected : _orders.Selected : null,
            Movement: planning && state.Phase == TacticalBattlePhase.Move ? _orders.Capture() : null,
            FirePlan: firePlan,
            Salvo: salvo, AllFireSectors: false, HoverUnitId: HoverUnitId,
            LastSelectedDroneId: _lastSelectedDroneId,
            HoverPoint: _lastPointer is { } pointer && !OverlayHit(pointer) ? pointer : null)
        {
            AimingMovement = _orderDrag && !_camera.Panning && !ConfirmExit && !Flow.Pending
        };
    }

    private InteractiveAnomalyField? _interactiveField;

    public override void OnDeactivated()
    {
        _interactiveField?.Dispose();
        _interactiveField = null;
        base.OnDeactivated();
    }

    public override void Render(SKCanvas canvas, int width, int height) => RenderFrame(canvas, width, height, null);

    internal void RenderNative(SKCanvas canvas, int width, int height) =>
        RenderFrame(canvas, width, height, _interactiveField ??= new());

    private void RenderFrame(SKCanvas canvas, int width, int height, InteractiveAnomalyField? interactiveField)
    {
        using var skin = TacticalBattleSkinRenderScope.Enter(_skin);
        if (CaptureFrame(width, height) is not { } frame) { base.Render(canvas, width, height); return; }
        _pilotColumn.Arrange(frame, PilotCardModel.Project(frame, _portraits));
        if (_lastPointer is { } pointer)
        {
            _hoverFrame = frame;
            UpdateHover(pointer);
            frame = frame with { HoverUnitId = HoverUnitId, HoverPoint = OverlayHit(pointer) ? null : pointer };
        }
        Layout = new() { Camera = frame.View };
        BattleDesign.Recording = Layout;
        canvas.Save();
        try { canvas.Scale(_viewport.S); TacticalBattleMapRenderer.Draw(canvas, frame, interactiveField, _pilotColumn.Bounds.Bottom); DrawOverlay(canvas, frame); }
        finally { canvas.Restore(); BattleDesign.Recording = null; }
        if (ConfirmExit) DrawExitConfirmation(canvas, width, height);
    }

    public override ScreenEvent OnKeyDown(Key key, KeyModifiers modifiers, bool repeat)
    {
        if (BlockWorldKey(key, modifiers)) return ScreenEvent.None;
        if (TurnBusy) return ScreenEvent.None;
        if (key == Key.Escape && !repeat && OutcomeVisible) return ScreenEvent.CloseTacticalBattle;


        if (!ConfirmExit && HasSalvoPresentation)
        {
            if (!repeat && key is Key.Enter or Key.KeypadEnter) AdvanceSalvo();
            return key == Key.Escape ? base.OnKeyDown(key, modifiers, repeat) : ScreenEvent.None;
        }
        if (!ConfirmExit && !Flow.Pending && Flow.State is { Phase: TacticalBattlePhase.Move } state)
        {
            ObserveOrders(state);
            if (key == Key.Tab && !repeat && (modifiers & (KeyModifiers.Control | KeyModifiers.Alt)) == 0)
            {
                CancelOrderGesture();
                _orders.Cycle(modifiers.HasFlag(KeyModifiers.Shift));
                return ScreenEvent.None;
            }
            if (modifiers == KeyModifiers.None)
            {
                switch (key)
                {
                    case Key.Left: _orders.Adjust(0, -5); return ScreenEvent.None;
                    case Key.Right: _orders.Adjust(0, 5); return ScreenEvent.None;
                    case Key.Up: _orders.Adjust(2, 0); return ScreenEvent.None;
                    case Key.Down: _orders.Adjust(-2, 0); return ScreenEvent.None;
                }
            }
        }
        if (!ConfirmExit && Flow.State is { Phase: TacticalBattlePhase.Fire } firing)
        {
            ObserveOrders(firing);
            if (!repeat && modifiers == KeyModifiers.None && key == Key.E && _fireOrders.Selected is { } evader)
            {
                _fireOrders.ToggleEvade(evader);
                return ScreenEvent.None;
            }
            if (!repeat && modifiers == KeyModifiers.None && key == Key.D && _fireOrders.Selected is { } decoy)
            {
                _fireOrders.ToggleDecoy(decoy);
                return ScreenEvent.None;
            }
            if (key == Key.Tab && !repeat && (modifiers & (KeyModifiers.Control | KeyModifiers.Alt)) == 0)
            {
                _firePanel.ResetInput();
                _fireOrders.Cycle(modifiers.HasFlag(KeyModifiers.Shift));
                return ScreenEvent.None;
            }

        }
        if (key is Key.Escape or Key.Enter or Key.KeypadEnter) CancelOrderGesture();
        return base.OnKeyDown(key, modifiers, repeat);
    }

    public override MoveTacticalBattleRequest BuildMoveRequest()
    {
        if (Flow.State is not { } state) return base.BuildMoveRequest();
        ObserveOrders(state);
        return new(TacticalBattleFlow.NewIdentity("move"), state.BattleId, state.Revision, _orders.EditedOrders());
    }

    public override FireTacticalBattleRequest BuildFireRequest()
    {
        if (Flow.State is not { } state) return base.BuildFireRequest();
        ObserveOrders(state);
        return new(TacticalBattleFlow.NewIdentity("fire"), state.BattleId, state.Revision, _fireOrders.BuildTargets());
    }

    public override void OnBattleUpdated(TacticalBattleSnapshot? before, TacticalBattleResponse response)
    {
        if (Flow.PresentationState is not { } state) return;
        if (response.State is null) return;
        var current = Volatile.Read(ref _presentation);
        if (current?.Accepted.BattleId == state.BattleId && current.Accepted.Revision >= state.Revision) return;
        if (!response.Accepted)
        {
            Volatile.Write(ref _presentation, new(state, null));
            return;
        }
        long now = ClockMilliseconds();
        var projectedResponse = response with { State = state };
        var animation = TacticalBattleMovementAnimation.Create(before, projectedResponse, now);
        var results = TacticalBattleResults.Create(before, projectedResponse, now + (animation?.PlaybackDurationMs ?? 0));
        var previousPaths = animation is not null && before is not null && !before.Paths.IsDefaultOrEmpty
            ? before.Paths.Where(path => path.Turn == before.Turn - 1).ToImmutableArray()
            : [];
        Volatile.Write(ref _presentation, new(state, animation, results, previousPaths));
    }

    private void ObserveOrders(TacticalBattleSnapshot state)
    {
        if (ReferenceEquals(_observedOrders, state)) return;
        var before = _observedOrders;
        _observedOrders = state;
        var previouslyFiring = _fireOrders.Selected;
        _fireOrders.Observe(state, _orders.Selected);
        _orders.Observe(state);
        if (state.Turn != before?.Turn || state.Phase != before?.Phase)
        {
            if (state.Phase == TacticalBattlePhase.Move && previouslyFiring is not null) _orders.Select(previouslyFiring);
            CancelOrderGesture();
            _movePanel.ResetScroll();
            _firePanel.ResetInput();
            _firePanel.ResetScroll();
        }
    }

    public override ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (ConfirmExit) return base.OnMouseDown(x, y, button);
        if (TurnBusy) return ScreenEvent.None;
        var p = DesignPoint(x, y);
        if (OverlayHit(p)) return OverlayClick(p, button);
        if (button == MouseButton.Right) { _camera.BeginPan(p.X, p.Y); return ScreenEvent.None; }
        if (button != MouseButton.Left || HasSalvoPresentation) return ScreenEvent.None;
        if (Flow.State is { Phase: TacticalBattlePhase.Move } state)
        {
            ObserveOrders(state);
            if (_orders.Hit(View, p, 7, true) is { } handle)
            { _orders.Select(handle); _orderDrag = true; _orderDragEdited = false; _lastAim = p; }
            else if (_orders.Hit(View, p, 12, false) is { } id) _orders.ToggleSelection(id);
            else { _orderDrag = _orders.Selected is not null; if (_orderDrag) AimOrder(p); }
        }
        else if (Flow.State is { Phase: TacticalBattlePhase.Fire } fire)
        { ObserveOrders(fire); _fireOrders.ChooseAt(View, p, 1); }
        return ScreenEvent.None;
    }
    public override bool OnMouseMove(float x, float y)
    {
        if (ConfirmExit) return base.OnMouseMove(x, y);
        var p = DesignPoint(x, y);
        _lastPointer = p;
        if (_camera.PanTo(p.X, p.Y)) return true;
        if (TurnBusy) return false;
        if (OverlayDrag(p)) return true;
        if (_orderDrag) { if (p != _lastAim) AimOrder(p); return true; }
        UpdateHover(p);
        return OverlayHit(p);
    }
    public override void OnMouseUp(float x, float y, MouseButton button)
    {
        if (button == MouseButton.Right) _camera.EndPan();
        if (button != MouseButton.Left) return;
        EndOverlayDrag();
        if (_orderDrag && !TurnBusy && !ConfirmExit)
        { var p = DesignPoint(x, y); if (p != _lastAim) AimOrder(p); if (_orderDragEdited) _orders.AdvanceToNearestUntouched(); }
        _orderDrag = _orderDragEdited = false;
    }
    public override ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        var p = DesignPoint(x, y);
        if (!ConfirmExit && !TurnBusy && !HasSalvoPresentation && _pilotColumn.ScrollTargets(SelectedPlanningDrone, p, delta))
            return ScreenEvent.None;
        if (!ConfirmExit && (_pilotColumn.Scroll(p, delta) || _phasePanel.ScrollTargets(p, delta) || _phasePanel.ScrollQueue(p, delta) || _hoverCard.ScrollResults(p, delta))) return ScreenEvent.None;
        if (!ConfirmExit && !TurnBusy && !_orderDrag && !OverlayHit(p)) _camera.Zoom(p.X, p.Y, delta);
        return ScreenEvent.None;
    }
    public override void OnInputReset()
    {
        base.OnInputReset();
        EndOverlayDrag();
        HoverUnitId = null;
        _lastPointer = null;
        _camera.EndPan();
        CancelOrderGesture();
        _firePanel.ResetInput();
        _logTab.ResetInput();
        _forecastTab.ResetInput();
        _exitBattle.ResetInput(); _newBattle.ResetInput(); _phaseAction.ResetInput(); _replaySalvo.ResetInput();
        _enemyForecast.HasFocus = _enemySectors.HasFocus = _allFireSectors.HasFocus = false;
    }

    private void AdvanceSalvo()
    {
        var current = Volatile.Read(ref _presentation);
        if (current?.Results is not { } salvo) return;
        long now = ClockMilliseconds();
        var next = salvo.InFlight(now) ? salvo with { Skipped = true, SkippedAtMs = now } : null;
        Interlocked.CompareExchange(ref _presentation, current with { Results = next }, current);
    }

    private ScreenEvent ExitAction()
    {
        if (OutcomeVisible) return ScreenEvent.CloseTacticalBattle;
        ConfirmExit = true;
        return ScreenEvent.None;
    }

    private void ReplaySalvo()
    {
        var current = Volatile.Read(ref _presentation);
        long now = ClockMilliseconds();
        if (current?.Results is not { } salvo || salvo.InFlight(now)) return;
        Interlocked.CompareExchange(ref _presentation, current with { Results = salvo with { StartedMs = now, Skipped = false, SkippedAtMs = null } }, current);
    }

    private void AimOrder(SKPoint p)
    {
        var (x, y) = View.Unproject(p.X, p.Y);
        _orderDragEdited |= _orders.AimAt(x, y);
        _lastAim = p;
    }

    private void CancelOrderGesture()
    {
        _orderDrag = _orderDragEdited = false;
        _movePanel.ResetInput();
        EndOverlayDrag();
    }
}
