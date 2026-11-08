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
    TacticalFirePlan? FirePlan = null, TacticalSalvoFrame? Salvo = null, bool AllFireSectors = true);

public sealed partial class TacticalBattleScreen : TacticalBattleModalScreen
{
    private readonly TacticalBattleCamera _camera = new();
    private readonly TacticalBattleSkin _skin;
    private readonly ShortcutButton _fit = new("", "0");
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
        TacticalBattleResults? Results = null);
    private TurnPresentation? _presentation;
    private TacticalBattleSnapshot? _observedOrders;
    internal Func<long> ClockMilliseconds { get; set; } = () => Environment.TickCount64;
    private SKRect _panel;
    internal SKRect NewBattleBounds => _newBattle.Bounds;
    internal (SKRect Forecast, SKRect Sectors, SKRect AllFire) OverlayControls =>
        (_enemyForecast.Bounds, _enemySectors.Bounds, _allFireSectors.Bounds);
    internal TacticalBattleView View => _camera.View;
    internal string SkinId => _skin.Id;
    internal TacticalMovementFrame Movement => _orders.Capture();
    internal TacticalBattleMovePanel MovePanel => _movePanel;
    internal TacticalBattleFirePanel FirePanel => _firePanel;
    internal bool HasSalvoPresentation => Volatile.Read(ref _presentation)?.Results is not null;
    internal SKRect ReplaySalvoBounds => _replaySalvo.Bounds;
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
        if (flow.State is { } state) _presentation = new(state, null);
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
        var salvo = moving ? null : presentation.Results?.Capture(now);
        if (!moving && presentation.Results is { } results) state = results.Project(now);
        bool planning = !moving && salvo is null;
        int panelWidth = (int)PyMath.Min(400 * UiScale, width * 0.60);
        int mapWidth = (int)PyMath.Max(1, width - panelWidth);
        _panel = new(mapWidth, 0, width, height);
        _camera.Update(state, mapWidth, height);
        if (planning) ObserveOrders(state);
        return new(state, _camera.View, _panel, UiScale, moving || Flow.Pending || presentation.Accepted.Revision != Flow.State?.Revision,
            BlockedNotice is { Length: > 0 } notice ? notice : moving ? L("AnimatingMovement") : Flow.Error,
            Presentation: salvo is null ? TacticalBattlePresentation.Current :
                salvo.InFlight ? TacticalBattlePresentation.SalvoFlight : TacticalBattlePresentation.SalvoResults,
            EnemyForecast: planning && _enemyForecast.Value, EnemySectors: planning && _enemySectors.Value,
            SelectedDroneId: planning ? state.Phase == TacticalBattlePhase.Fire ? _fireOrders.Selected : _orders.Selected : null,
            Movement: planning && state.Phase == TacticalBattlePhase.Move ? _orders.Capture() : null,
            FirePlan: planning && state.Phase == TacticalBattlePhase.Fire ? _fireOrders.Capture() : null,
            Salvo: salvo, AllFireSectors: _allFireSectors.Value);
    }

    public override void Render(SKCanvas canvas, int width, int height)
    {
        using var skin = TacticalBattleSkinRenderScope.Enter(_skin);
        if (CaptureFrame(width, height) is not { } frame)
        {
            base.Render(canvas, width, height);
            return;
        }
        canvas.Clear(TacticalBattleMapRenderer.Background);
        canvas.Save();
        canvas.ClipRect(SKRect.Create(frame.View.PixelWidth, frame.View.PixelHeight));
        TacticalBattleMapRenderer.Draw(canvas, frame);
        canvas.Restore();

        canvas.Save();
        canvas.ClipRect(frame.Panel);
        DrawPanel(canvas, frame);
        canvas.Restore();
        if (ConfirmExit) DrawExitConfirmation(canvas, width, height);
        else
        {
            var viewport = SKRect.Create(width, height);
            _fit.DrawTooltip(canvas, viewport, frame.UiScale);
            if (_tabs.SelectedId == "phase")
            {
                if (frame.Movement is not null) _movePanel.DrawTooltip(canvas, viewport);
                if (frame.FirePlan is not null) _firePanel.DrawOverlay(canvas, viewport);
            }
        }
    }

    private void DrawPanel(SKCanvas canvas, TacticalBattleFrame frame)
    {
        var panel = frame.Panel;
        float s = frame.UiScale;
        TacticalBattleSkinRenderScope.Box(canvas, "panel.background", panel, new(10, 24, 36), null, s, antialias: false);
        using var divider = new SKPaint { Color = TacticalBattleSkinRenderScope.Color("panel.divider", "stroke", new(30, 71, 92)) };
        canvas.DrawRect(panel.Left, panel.Top, (float)PyMath.Min(panel.Width,
            TacticalBattleSkinRenderScope.Number("panel.divider", "strokeWidth", 1) * s), panel.Height, divider);
        var content = new SKRect(panel.Left + 12 * s, panel.Top, panel.Right - 12 * s, panel.Bottom);
        var battle = frame.Battle;
        var header = TacticalBattlePanelHeader.Capture(frame);
        float y = header.Draw(canvas, content, s);

        float col = (float)PyMath.Max(0, (content.Width - 8 * s) * 0.5);
        _fit.Bounds = SKRect.Create(content.Left, y, col, 34 * s);
        _exitBattle.Bounds = SKRect.Create(content.Right - col, y, col, 34 * s);
        bool results = frame.Salvo is not null;
        _newBattle.Bounds = SKRect.Create(content.Left, y + 41 * s, content.Width, 30 * s);
        bool showVisibility = !results && !header.ShowNew;
        _allFireSectors.Bounds = showVisibility ? SKRect.Create(content.Left, y + 99 * s, content.Width, 22 * s) : SKRect.Empty;
        _enemySectors.Bounds = showVisibility ? SKRect.Create(content.Left, y + 122 * s, content.Width, 22 * s) : SKRect.Empty;
        _enemyForecast.Bounds = showVisibility ? SKRect.Create(content.Left, y + 145 * s, content.Width, 22 * s) : SKRect.Empty;
        _phaseAction.Bounds = !results && header.ShowNew ? SKRect.Empty :
            SKRect.Create(content.Left, y + (results ? 78 : 175) * s, content.Width, 36 * s);
        _fit.Label = L("FitField");
        _fit.Tooltip = L("FitFieldHelp");
        _enemyForecast.Label = L("EnemyForecast");
        _enemySectors.Label = L("EnemySectors");
        _allFireSectors.Label = L("AllFireSectors");
        if (showVisibility)
            ControlVisuals.Label(canvas, L("VisibilitySettings"), SKRect.Create(content.Left, y + 76 * s, content.Width, 20 * s), true, s);
        using (TacticalBattleSkinRenderScope.EnterRole("control.toggle.all-fire-sectors"))
            _allFireSectors.Draw(canvas, s);
        using (TacticalBattleSkinRenderScope.EnterRole("control.toggle.enemy-forecast"))
            _enemyForecast.Draw(canvas, s);
        using (TacticalBattleSkinRenderScope.EnterRole("control.toggle.enemy-rear-sectors"))
            _enemySectors.Draw(canvas, s);
        _exitBattle.Label = L("ToMap"); _exitBattle.Enabled = !frame.Pending;
        _phaseAction.Label = L(frame.Salvo is { } playback
            ? playback.InFlight ? "SalvoSkip" : battle.Phase == TacticalBattlePhase.Over ? "SalvoToOutcome" : "SalvoToMove"
            : battle.Phase == TacticalBattlePhase.Fire ? "Fire" : "Move");
        _phaseAction.Enabled = !frame.Pending && (results || battle.Phase != TacticalBattlePhase.Over);
        _newBattle.Label = L("NewBattle"); _newBattle.Enabled = !frame.Pending && frame.Salvo?.InFlight != true;
        _fit.Draw(canvas, s); _exitBattle.Draw(canvas, s);
        _newBattle.Draw(canvas, s);
        _phaseAction.Draw(canvas, s);
        var body = new SKRect(content.Left, (float)PyMath.Max(_phaseAction.Bounds.Bottom, _newBattle.Bounds.Bottom) + 8 * s,
            content.Right, panel.Bottom - 8 * s);
        _replaySalvo.Bounds = SKRect.Empty;
        if (frame.Salvo is { InFlight: false })
        {
            _replaySalvo.Bounds = SKRect.Create(body.Left, body.Top, body.Width, 32 * s);
            _replaySalvo.Label = L("SalvoReplay");
            _replaySalvo.Draw(canvas, s);
            body.Top += 39 * s;
        }
        _tabs.Bounds = SKRect.Create(body.Left, body.Top, body.Width, 27 * s);
        _tabs.Draw(canvas, s);
        body.Top += 34 * s;
        if (_tabs.SelectedId == "sides") { _sidesTab.Draw(canvas, frame, body); return; }
        if (_tabs.SelectedId == "log") { _logTab.Draw(canvas, frame, body); return; }
        if (_tabs.SelectedId == "forecast") { _forecastTab.Draw(canvas, frame, body); return; }
        if (frame.Salvo is { } salvo)
        {
            using var paint = ControlText.Paint(s);
            float top = body.Top;
            string summary = salvo.InFlight ? L("SalvoFlying") : string.Format(
                System.Globalization.CultureInfo.InvariantCulture, L("SalvoSummary"),
                BattleFormat.Number(salvo.Flights.Length), BattleFormat.Number(salvo.Hits));
            if (salvo.AutoFire) summary += "\n" + L("SalvoAutoFire");
            foreach (var line in ControlText.Wrap(summary, body.Width, paint))
            {
                ControlVisuals.Label(canvas, line, SKRect.Create(body.Left, top, body.Width, 20 * s), true, s);
                top += 19 * s;
            }
        }
        if (frame.Movement is { } movement) _movePanel.Draw(canvas, movement, body, s, !frame.Pending);
        if (frame.FirePlan is { } fire) _firePanel.Draw(canvas, fire, battle, body,
            SKRect.Create(panel.Right, panel.Bottom), s, !frame.Pending);
    }

    private void Fit()
    {
        if (Flow.State is { } state) _camera.Fit(state);
    }

    public override ScreenEvent OnKeyDown(Key key, KeyModifiers modifiers, bool repeat)
    {
        if (BlockWorldKey(key, modifiers)) return ScreenEvent.None;
        if (TurnBusy) return ScreenEvent.None;
        if (key == Key.Escape && !repeat && OutcomeVisible) return ScreenEvent.CloseTacticalBattle;
        if (!ConfirmExit && _tabs.SelectedId == "log" && _logTab.OnKeyDown(key)) return ScreenEvent.None;
        if (!ConfirmExit && _tabs.SelectedId == "forecast" && _forecastTab.OnKeyDown(key)) return ScreenEvent.None;
        if (!ConfirmExit && !repeat && modifiers == KeyModifiers.None && key is Key.Number0 or Key.Keypad0)
        {
            Fit();
            return ScreenEvent.None;
        }
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
            if (key == Key.Tab && !repeat && (modifiers & (KeyModifiers.Control | KeyModifiers.Alt)) == 0)
            {
                _firePanel.ResetInput();
                _fireOrders.Cycle(modifiers.HasFlag(KeyModifiers.Shift));
                return ScreenEvent.None;
            }
            if (!repeat && _firePanel.OnKeyDown(key)) return ScreenEvent.None;
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
        if (Flow.State is not { } state) return;
        if (response.State is null) return;
        var current = Volatile.Read(ref _presentation);
        if (current?.Accepted.BattleId == state.BattleId && current.Accepted.Revision >= state.Revision) return;
        if (!response.Accepted)
        {
            Volatile.Write(ref _presentation, new(state, null));
            return;
        }
        long now = ClockMilliseconds();
        var animation = TacticalBattleMovementAnimation.Create(before, response, now);
        var results = TacticalBattleResults.Create(before, response, now + (animation?.PlaybackDurationMs ?? 0));
        Volatile.Write(ref _presentation, new(state, animation, results));
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
        bool firing = Flow.State?.Phase == TacticalBattlePhase.Fire;
        if (firing && _tabs.SelectedId == "phase" && _firePanel.Targets.IsOpen)
        {
            _firePanel.OnMouseDown(x, y, button);
            return ScreenEvent.None;
        }
        if (_tabs.OnMouseDown(x, y, button)) return ScreenEvent.None;
        if (HasSalvoPresentation)
        {
            if (button == MouseButton.Right) _camera.BeginPan(x, y);
            else if (_fit.OnMouseDown(x, y, button)) Fit();
            else if (_exitBattle.OnMouseDown(x, y, button)) return ExitAction();
            else if (CanStartNewBattle && _newBattle.OnMouseDown(x, y, button)) return ScreenEvent.NewTacticalBattle;
            else if (_phaseAction.OnMouseDown(x, y, button)) AdvanceSalvo();
            else if (_replaySalvo.OnMouseDown(x, y, button)) ReplaySalvo();
            else if (_tabs.SelectedId == "log") _logTab.OnMouseDown(x, y, button);
            else if (_tabs.SelectedId == "forecast") _forecastTab.OnMouseDown(x, y, button);
            return ScreenEvent.None;
        }
        if (button == MouseButton.Right)
        {
            if (!_orderDrag) _camera.BeginPan(x, y);
            return ScreenEvent.None;
        }
        if (!_panel.Contains(x, y))
        {
            if (button == MouseButton.Left && !_camera.Panning && _camera.Contains(x, y) &&
                Flow.State is { Phase: TacticalBattlePhase.Move } state)
            {
                ObserveOrders(state);
                var p = new SKPoint(x, y);
                if (_orders.Hit(View, p, 12 * UiScale, endpoint: true) is { } handle)
                {
                    _orders.Select(handle);
                    _orderDrag = true; _orderDragEdited = false; _lastAim = p;
                }
                else if (_orders.Hit(View, p, 12 * UiScale, endpoint: false) is { } drone) _orders.Select(drone);
                else
                {
                    _orderDrag = _orders.Selected is not null;
                    _orderDragEdited = false;
                    if (_orderDrag) AimOrder(p);
                }
            }
            else if (button == MouseButton.Left && !_camera.Panning && _camera.Contains(x, y) &&
                Flow.State is { Phase: TacticalBattlePhase.Fire } fire)
            {
                ObserveOrders(fire);
                _fireOrders.ChooseAt(View, new(x, y), UiScale);
            }
            return ScreenEvent.None;
        }
        if (_fit.OnMouseDown(x, y, button)) Fit();
        else if (_exitBattle.OnMouseDown(x, y, button)) return ExitAction();
        else if (_enemyForecast.OnMouseDown(x, y, button)) _enemySectors.HasFocus = _allFireSectors.HasFocus = false;
        else if (_enemySectors.OnMouseDown(x, y, button)) _enemyForecast.HasFocus = _allFireSectors.HasFocus = false;
        else if (_allFireSectors.OnMouseDown(x, y, button)) _enemyForecast.HasFocus = _enemySectors.HasFocus = false;
        else if (CanStartNewBattle && _newBattle.OnMouseDown(x, y, button)) return ScreenEvent.NewTacticalBattle;
        else if (_phaseAction.OnMouseDown(x, y, button)) return ActionEvent;
        else if (_tabs.SelectedId == "sides")
        {
            if (_sidesTab.Hit(x, y, button) is { } drone && Flow.State is { } state)
            {
                ObserveOrders(state);
                if (firing) _fireOrders.Select(drone);
                else _orders.Select(drone);
            }
        }
        else if (_tabs.SelectedId == "log") _logTab.OnMouseDown(x, y, button);
        else if (_tabs.SelectedId == "forecast") _forecastTab.OnMouseDown(x, y, button);
        else if (Flow.State?.Phase == TacticalBattlePhase.Move) _movePanel.OnMouseDown(x, y, button);
        else if (firing) _firePanel.OnMouseDown(x, y, button);
        return ScreenEvent.None;
    }

    public override bool OnMouseMove(float x, float y)
    {
        if (ConfirmExit) return base.OnMouseMove(x, y);
        if (TurnBusy) return false;
        if (_camera.PanTo(x, y)) return true;
        bool tabHover = _tabs.Bounds.Contains(x, y) || (_tabs.SelectedId switch
        {
            "sides" => _sidesTab.Hover(x, y),
            "log" => _logTab.OnMouseMove(x, y),
            "forecast" => _forecastTab.OnMouseMove(x, y),
            _ => false
        });
        if (HasSalvoPresentation)
            return tabHover | _fit.OnMouseMove(x, y) | _exitBattle.OnMouseMove(x, y) | _newBattle.OnMouseMove(x, y) |
                _phaseAction.OnMouseMove(x, y) | _replaySalvo.OnMouseMove(x, y);
        if (_orderDrag)
        {
            if (!Flow.Pending && new SKPoint(x, y) != _lastAim) AimOrder(new(x, y));
            return true;
        }
        bool bodyHover = _tabs.SelectedId == "phase" && (Flow.State?.Phase switch
        {
            TacticalBattlePhase.Move => _movePanel.OnMouseMove(x, y),
            TacticalBattlePhase.Fire => _firePanel.OnMouseMove(x, y),
            _ => false
        });
        return tabHover | bodyHover | _fit.OnMouseMove(x, y) | _exitBattle.OnMouseMove(x, y) | _newBattle.OnMouseMove(x, y) | _phaseAction.OnMouseMove(x, y);
    }
    public override void OnMouseUp(float x, float y, MouseButton button)
    {
        if (button == MouseButton.Right) _camera.EndPan();
        _movePanel.OnMouseUp(button);
        _logTab.OnMouseUp(button);
        _forecastTab.OnMouseUp(button);
        if (button != MouseButton.Left || !_orderDrag) return;
        if (!ConfirmExit && !TurnBusy && Flow.State?.Phase == TacticalBattlePhase.Move)
        {
            var p = new SKPoint(x, y);
            if (p != _lastAim) AimOrder(p);
            if (_orderDragEdited) _orders.AdvanceToNearestUntouched();
        }
        _orderDrag = _orderDragEdited = false;
    }
    public override ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        if (ConfirmExit || TurnBusy || _orderDrag) return ScreenEvent.None;
        bool consumed = _tabs.SelectedId switch
        {
            "sides" => _sidesTab.OnMouseWheel(x, y, delta),
            "log" => _logTab.OnMouseWheel(x, y, delta),
            "forecast" => _forecastTab.OnMouseWheel(x, y, delta),
            _ when HasSalvoPresentation => false,
            _ when Flow.State?.Phase == TacticalBattlePhase.Move => _movePanel.OnMouseWheel(x, y, delta),
            _ when Flow.State?.Phase == TacticalBattlePhase.Fire => _firePanel.OnMouseWheel(x, y, delta) || _firePanel.Targets.IsOpen,
            _ => false
        };
        if (!consumed) _camera.Zoom(x, y, delta);
        return ScreenEvent.None;
    }
    public override void OnInputReset()
    {
        base.OnInputReset();
        _camera.EndPan();
        CancelOrderGesture();
        _firePanel.ResetInput();
        _logTab.ResetInput();
        _forecastTab.ResetInput();
        _fit.ResetInput(); _exitBattle.ResetInput(); _newBattle.ResetInput(); _phaseAction.ResetInput(); _replaySalvo.ResetInput();
        _enemyForecast.HasFocus = _enemySectors.HasFocus = _allFireSectors.HasFocus = false;
    }

    private void AdvanceSalvo()
    {
        var current = Volatile.Read(ref _presentation);
        if (current?.Results is not { } salvo) return;
        var next = salvo.InFlight(ClockMilliseconds()) ? salvo with { Skipped = true } : null;
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
        Interlocked.CompareExchange(ref _presentation, current with { Results = salvo with { StartedMs = now, Skipped = false } }, current);
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
    }
}
