using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

/// <summary>One modal lifetime; all combat facts come from the session connection.</summary>
public sealed class TacticalBattleFlow(GameSessionHandle session)
{
    private TacticalBattleOptions? _options;
    private TacticalBattleSnapshot? _state;
    private string _error = "";
    private int _pending;
    private CancellationTokenSource? _optionsCancellation;
    public GameSessionHandle Session { get; } = session;
    public TacticalBattleOptions? Options => Volatile.Read(ref _options);
    public TacticalBattleSnapshot? State => Volatile.Read(ref _state);
    public string Error => Volatile.Read(ref _error);
    public bool Pending => Volatile.Read(ref _pending) != 0;
    public Task? OptionsTask { get; private set; }
    public StartTacticalBattleRequest? LastStart { get; private set; }
    public void BeginLoadOptions() => OptionsTask ??= LoadOptionsAsync();
    private async Task LoadOptionsAsync()
    {
        using var cancellation = new CancellationTokenSource();
        Volatile.Write(ref _optionsCancellation, cancellation);
        try
        {
            var options = await Session.GetTacticalBattleOptionsAsync(cancellation.Token);
            if (!cancellation.IsCancellationRequested)
            {
                Volatile.Write(ref _options, options);
                if (!options.Availability.Available)
                    Volatile.Write(ref _error, BattleFormat.Localize("Unavailable") + " " + Issue(options.Availability.Issue));
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (Exception error) { Volatile.Write(ref _error, BattleFormat.Localize("Unavailable") + " " + error.Message); }
        finally { Volatile.Write(ref _optionsCancellation, null); }
    }
    public void AbortOptionsLoad()
    {
        try { Volatile.Read(ref _optionsCancellation)?.Cancel(); }
        catch (ObjectDisposedException) { } // query completion raced this UI deactivation
    }
    private static string Issue(TacticalContentIssue? issue) => issue is null ? "" :
        BattleFormat.Text(string.Join(" · ", new[] { issue.File, issue.Field, issue.Message }.Where(s => s.Length > 0)));
    public async Task<TacticalBattleResponse?> StartAsync(StartTacticalBattleRequest request)
    {
        var response = await Submit(() => Session.StartTacticalBattleAsync(request), starting: true);
        if (response?.Accepted == true) LastStart = request;
        return response;
    }
    public Task<TacticalBattleResponse?> MoveAsync(MoveTacticalBattleRequest request) => Submit(() => Session.MoveTacticalBattleAsync(request));
    public Task<TacticalBattleResponse?> FireAsync(FireTacticalBattleRequest request) => Submit(() => Session.FireTacticalBattleAsync(request));
    /// <summary>Exits the battle the engine reports, which can differ from ours after a lost response; true once none is active.</summary>
    public async Task<bool> ExitAsync()
    {
        for (int attempt = 0; attempt < 2; attempt++)
        {
            var state = State ?? await Session.GetTacticalBattleStateAsync();
            if (state is null) return Forget();
            var response = await Submit(() => Session.ExitTacticalBattleAsync(new(NewIdentity("exit"), state.BattleId)), exiting: true);
            if (response is null) return false;
            if (response.Accepted) return true;
            if (response.Code == TacticalBattleRejectionCode.NoBattle) return Forget();
            if (response.Code == TacticalBattleRejectionCode.WrongBattle && response.State is not null) continue; // Submit adopted the live battle
            return await Session.GetTacticalBattleStateAsync() is null && Forget();
        }
        return false;
    }
    private bool Forget()
    {
        Volatile.Write(ref _state, null);
        return true;
    }
    private static bool IsLiveBattleRefusal(TacticalBattleResponse response) => !response.Accepted &&
        response.Code is TacticalBattleRejectionCode.AlreadyActive or TacticalBattleRejectionCode.WrongBattle;
    private async Task<TacticalBattleResponse?> Submit(Func<ValueTask<TacticalBattleResponse>> send, bool starting = false, bool exiting = false)
    {
        if (Interlocked.CompareExchange(ref _pending, 1, 0) != 0) return null;
        try
        {
            var response = await send();
            var previous = State;
            if (response.Accepted && exiting) Volatile.Write(ref _state, null);
            else if (response.State is { } state && (starting && response.Accepted || IsLiveBattleRefusal(response) || previous is not null && state.BattleId == previous.BattleId && state.Revision >= previous.Revision))
                Volatile.Write(ref _state, state);
            Volatile.Write(ref _error, response.Accepted ? "" : BattleFormat.Text(GameSessionHandle.TacticalBattleRefusalText(response.Code) + " " + Issue(response.ContentError)));
            return response;
        }
        finally { Volatile.Write(ref _pending, 0); }
    }
    public static string NewIdentity(string operation) => "TB-" + operation + "-" + Guid.NewGuid().ToString("N");
}

/// <summary>
/// Concrete connection-backed modal base. Rich setup/map views extend its presentation in dependent tickets.
/// The compact default view remains functional: it submits actual AI orders/default targets and shows real units.
/// </summary>
public class TacticalBattleModalScreen : IScreen
{
    private readonly ShortcutButton _action = new("");
    private readonly ShortcutButton _close = new("");
    private readonly ShortcutButton _new = new("");
    private readonly ShortcutButton _confirm = new("");
    private readonly ShortcutButton _cancel = new("");
    private readonly ScrollTextPanel _error = new() { AutoScroll = false };
    private ScreenEvent _queued;
    public TacticalBattleFlow Flow { get; }
    public bool IsSetup { get; }
    public float UiScale { get; }
    public bool ConfirmExit { get; protected set; }
    internal Key LaunchKey { get; init; } = Key.F8;
    internal Key WorldDebugKey { get; init; } = Key.F12;
    private long _blockedNoticeUntil;
    internal string BlockedNotice => Environment.TickCount64 < _blockedNoticeUntil ? L("WorldActionBlocked") : "";

    internal void NotifyWorldActionBlocked() => _blockedNoticeUntil = Environment.TickCount64 + 4000;

    protected bool BlockWorldKey(Key key, KeyModifiers modifiers, bool editingSeed = false)
    {
        bool speedKey = key is Key.Number1 or Key.Number2 or Key.Number3 or Key.Number4;
        if (speedKey && editingSeed && (modifiers & (KeyModifiers.Control | KeyModifiers.Alt)) == 0) return false;
        if (!speedKey && key != LaunchKey && key != WorldDebugKey && key is not (Key.F5 or Key.F9 or Key.F12 or Key.F8 or Key.I or Key.Space)
            && !(key is Key.F or Key.S && modifiers.HasFlag(KeyModifiers.Control))) return false;
        NotifyWorldActionBlocked();
        return true;
    }
    public bool IsOpaque => true;
    public bool WantsArrowRepeat => true;
    public bool UsesBrowserWheelDelta => true;
    public TacticalBattleModalScreen(TacticalBattleFlow flow, bool isSetup, float uiScale = 1)
    {
        Flow = flow; IsSetup = isSetup;
        UiScale = float.IsFinite(uiScale) && uiScale > 0 ? uiScale : 1;
        _action.Clicked += () => _queued = ActionEvent;
        _close.Clicked += () => { if (IsSetup) _queued = ScreenEvent.CloseTacticalBattle; else ConfirmExit = true; };
        _new.Clicked += () => _queued = ScreenEvent.NewTacticalBattle;
        _confirm.Clicked += () => _queued = ScreenEvent.CloseTacticalBattle;
        _cancel.Clicked += () => ConfirmExit = false;
    }
    internal virtual bool CanStart => IsSetup && !Flow.Pending && Flow.Options is { Availability.Available: true, Scenarios.IsEmpty: false };
    protected ScreenEvent ActionEvent => Flow.Pending ? ScreenEvent.None : IsSetup
        ? CanStart ? ScreenEvent.StartTacticalBattle : ScreenEvent.None
        : Flow.State?.Phase switch { TacticalBattlePhase.Move => ScreenEvent.MoveTacticalBattle, TacticalBattlePhase.Fire => ScreenEvent.FireTacticalBattle, _ => ScreenEvent.None };
    public virtual StartTacticalBattleRequest BuildStartRequest() => Flow.LastStart is { } previous
        ? previous with { CommandId = TacticalBattleFlow.NewIdentity("start") } : new(TacticalBattleFlow.NewIdentity("start"), 1);
    public virtual MoveTacticalBattleRequest BuildMoveRequest()
    {
        var state = Flow.State;
        return new(TacticalBattleFlow.NewIdentity("move"), state?.BattleId ?? "", state?.Revision ?? 0);
    }
    public virtual FireTacticalBattleRequest BuildFireRequest()
    {
        var state = Flow.State;
        return new(TacticalBattleFlow.NewIdentity("fire"), state?.BattleId ?? "", state?.Revision ?? 0,
            state is not null ? state.Targets.Select(t => new TacticalFireTarget(t.DroneId, t.DefaultTargetId)).ToImmutableArray() : []);
    }
    public virtual void OnBattleUpdated(TacticalBattleSnapshot? before, TacticalBattleResponse response) { }
    protected static string L(string key) => BattleFormat.Localize(key);
    public virtual void Render(SKCanvas canvas, int width, int height)
    {
        canvas.Clear(new SKColor(2, 12, 22));
        float s = UiScale;
        ControlVisuals.Label(canvas, L(IsSetup ? "SetupTitle" : "BattleTitle"), new(24 * s, 12 * s, width - 24 * s, 52 * s), true, s);
        if (IsSetup)
        {
            var options = Flow.Options;
            string text = options is null ? L("Loading") : !options.Availability.Available ? L("Unavailable") :
                string.Join("\n\n", options.Scenarios.Select(scenario => scenario.Name + "\n" + scenario.Description));
            _error.Layout(new(24 * s, 72 * s, width - 24 * s, height - 120 * s), s);
            _error.SetText(Flow.Error.Length > 0 ? Flow.Error : text);
            _error.Draw(canvas);
        }
        else if (Flow.State is { } state)
        {
            string header = state.ScenarioName + " · " + L("Turn") + " " + BattleFormat.Number(state.Turn) + " · " + L("Phase." + state.Phase);
            ControlVisuals.Label(canvas, header, new(24 * s, 60 * s, width - 24 * s, 90 * s), true, s);
            float y = 105 * s;
            foreach (var unit in state.Units)
            {
                string row = unit.Id + " · " + L("Hull") + " " + BattleFormat.Number(unit.Hull) + "/" + BattleFormat.Number(unit.MaxHull)
                    + " · " + L("Armor") + " " + BattleFormat.Number(unit.Armor) + " · " + L("Speed") + " " + BattleFormat.Number(unit.Speed, 1)
                    + " · " + L("UnitState." + unit.State);
                ControlVisuals.Label(canvas, row, new(24 * s, y, width - 24 * s, y + 24 * s), true, s);
                y += 26 * s;
            }
            if (Flow.Error.Length > 0) ControlVisuals.Label(canvas, Flow.Error, new(24 * s, height - 115 * s, width - 24 * s, height - 80 * s), true, s);
        }
        _action.Label = L(IsSetup ? "Start" : Flow.State?.Phase == TacticalBattlePhase.Fire ? "Fire" : "Move");
        _action.KeyHint = "Enter"; _action.Enabled = ActionEvent != ScreenEvent.None;
        bool showAction = IsSetup ? Flow.Options is { Availability.Available: true, Scenarios.IsEmpty: false }
            : Flow.State?.Phase is TacticalBattlePhase.Move or TacticalBattlePhase.Fire;
        _action.Bounds = showAction ? SKRect.Create(24 * s, height - 64 * s, 230 * s, 40 * s) : SKRect.Empty;
        _close.Label = L("ToMap"); _close.KeyHint = "Esc"; _close.Enabled = !Flow.Pending;
        _close.Bounds = SKRect.Create(width - 254 * s, height - 64 * s, 230 * s, 40 * s);
        _new.Label = L("NewBattle"); _new.Enabled = !Flow.Pending && Flow.State?.Phase == TacticalBattlePhase.Over;
        _new.Bounds = !IsSetup && Flow.State?.Phase == TacticalBattlePhase.Over ? SKRect.Create(270 * s, height - 64 * s, 230 * s, 40 * s) : SKRect.Empty;
        if (showAction) _action.Draw(canvas, s);
        _close.Draw(canvas, s);
        if (!IsSetup && Flow.State?.Phase == TacticalBattlePhase.Over) _new.Draw(canvas, s);
        if (Flow.Pending) ControlVisuals.Label(canvas, L("Waiting"), new(24 * s, height - 100 * s, width - 24 * s, height - 70 * s), true, s);
        if (ConfirmExit) DrawExitConfirmation(canvas, width, height);
        if (BlockedNotice is { Length: > 0 } notice)
            ControlVisuals.Label(canvas, notice, new(24 * s, height - 96 * s, width - 24 * s, height - 65 * s), true, s);
    }
    protected void DrawExitConfirmation(SKCanvas canvas, int width, int height)
    {
        float s = UiScale;
        using var dim = new SKPaint { Color = new SKColor(0, 0, 0, 180) }; canvas.DrawRect(0, 0, width, height, dim);
        var rect = SKRect.Create((float)(width / 2.0) - 260 * s, (float)(height / 2.0) - 90 * s, 520 * s, 180 * s);
        GenericWindowTypeA.DrawOpaque(canvas, rect);
        ControlVisuals.Label(canvas, L("ConfirmExit"), new(rect.Left + 24 * s, rect.Top + 24 * s, rect.Right - 24 * s, rect.Top + 72 * s), true, s);
        _confirm.Label = L("ExitBattle"); _confirm.Enabled = !Flow.Pending;
        _cancel.Label = L("Cancel");
        _confirm.Bounds = SKRect.Create(rect.Left + 24 * s, rect.Bottom - 68 * s, 220 * s, 40 * s);
        _cancel.Bounds = SKRect.Create(rect.Right - 244 * s, rect.Bottom - 68 * s, 220 * s, 40 * s);
        _confirm.Draw(canvas, s); _cancel.Draw(canvas, s);
    }
    public virtual ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (Flow.Pending) return ScreenEvent.None;
        _queued = ScreenEvent.None;
        if (ConfirmExit) { _confirm.OnMouseDown(x, y, button); _cancel.OnMouseDown(x, y, button); return _queued; }
        _action.OnMouseDown(x, y, button); _close.OnMouseDown(x, y, button);
        if (!IsSetup && Flow.State?.Phase == TacticalBattlePhase.Over) _new.OnMouseDown(x, y, button);
        if (IsSetup) _error.OnMouseDown(x, y, button);
        return _queued;
    }
    public virtual bool OnMouseMove(float x, float y) => ConfirmExit
        ? _confirm.OnMouseMove(x, y) | _cancel.OnMouseMove(x, y)
        : _action.OnMouseMove(x, y) | _close.OnMouseMove(x, y) | _new.OnMouseMove(x, y) | (IsSetup && _error.OnMouseMove(x, y));
    public virtual void OnMouseUp(float x, float y, MouseButton button) { if (IsSetup) _error.OnMouseUp(button); }
    public virtual ScreenEvent OnMouseWheel(float x, float y, float delta) { if (IsSetup) _error.OnMouseWheel(x, y, delta); return ScreenEvent.None; }
    public virtual ScreenEvent OnKeyDown(Key key) => OnKeyDown(key, KeyModifiers.None, false);
    public virtual ScreenEvent OnKeyDown(Key key, KeyModifiers modifiers, bool repeat)
    {
        if (BlockWorldKey(key, modifiers)) return ScreenEvent.None;
        if (Flow.Pending) return ScreenEvent.None;
        if (key == Key.Escape)
        {
            if (ConfirmExit) ConfirmExit = false;
            else if (IsSetup) return ScreenEvent.CloseTacticalBattle;
            else ConfirmExit = true;
        }
        if (key is Key.Enter or Key.KeypadEnter && !repeat) return ConfirmExit ? ScreenEvent.CloseTacticalBattle : ActionEvent;
        return ScreenEvent.None;
    }
    public virtual void OnInputReset()
    {
        _action.ResetInput(); _close.ResetInput(); _new.ResetInput(); _confirm.ResetInput(); _cancel.ResetInput(); _error.ResetInput();
    }
    public virtual void OnActivated() { }
    public virtual void OnDeactivated() { OnInputReset(); Flow.AbortOptionsLoad(); }
    public virtual void OnTextInput(char c) { }
}
