using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

/// <summary>All information used to draw a battle frame is captured before painting it.</summary>
public sealed record TacticalBattleFrame(TacticalBattleSnapshot Battle, TacticalBattleView View, SKRect Panel,
    float UiScale, bool Pending, string Status, TacticalBattlePresentation Presentation = TacticalBattlePresentation.Current,
    bool EnemyForecast = true, bool EnemySectors = false, string? SelectedDroneId = null);

public sealed partial class TacticalBattleScreen : TacticalBattleModalScreen
{
    private readonly TacticalBattleCamera _camera = new();
    private readonly ShortcutButton _fit = new("", "0");
    private readonly ShortcutButton _exitBattle = new("", "Esc");
    private readonly ShortcutButton _phaseAction = new("", "Enter");
    private readonly ShortcutButton _newBattle = new("");
    private readonly Toggle _enemyForecast = new("", true);
    private readonly Toggle _enemySectors = new("");
    private SKRect _panel;
    internal SKRect NewBattleBounds => _newBattle.Bounds;
    internal (SKRect Forecast, SKRect Sectors) OverlayControls => (_enemyForecast.Bounds, _enemySectors.Bounds);
    internal TacticalBattleView View => _camera.View;

    public TacticalBattleScreen(TacticalBattleFlow flow, float uiScale) : base(flow, isSetup: false, uiScale) { }

    private TacticalBattleFrame Capture(TacticalBattleSnapshot state, int width, int height)
    {
        int panelWidth = (int)PyMath.Min(400 * UiScale, width * 0.60);
        int mapWidth = (int)PyMath.Max(1, width - panelWidth);
        _panel = new(mapWidth, 0, width, height);
        _camera.Update(state, mapWidth, height);
        return new(state, _camera.View, _panel, UiScale, Flow.Pending,
            BlockedNotice is { Length: > 0 } notice ? notice : Flow.Error,
            EnemyForecast: _enemyForecast.Value, EnemySectors: _enemySectors.Value);
    }

    public override void Render(SKCanvas canvas, int width, int height)
    {
        if (Flow.State is not { } state || width < 2 || height < 1)
        {
            base.Render(canvas, width, height);
            return;
        }
        var frame = Capture(state, width, height);
        canvas.Clear(new SKColor(2, 12, 22));
        canvas.Save();
        canvas.ClipRect(SKRect.Create(frame.View.PixelWidth, frame.View.PixelHeight));
        TacticalBattleMapRenderer.Draw(canvas, frame);
        canvas.Restore();

        canvas.Save();
        canvas.ClipRect(frame.Panel);
        DrawPanel(canvas, frame);
        canvas.Restore();
        if (ConfirmExit) DrawExitConfirmation(canvas, width, height);
        else _fit.DrawTooltip(canvas, SKRect.Create(width, height), frame.UiScale);
    }

    private void DrawPanel(SKCanvas canvas, TacticalBattleFrame frame)
    {
        var panel = frame.Panel;
        float s = frame.UiScale;
        using var background = new SKPaint { Color = new SKColor(10, 24, 36) };
        canvas.DrawRect(panel, background);
        background.Color = new SKColor(30, 71, 92);
        canvas.DrawRect(panel.Left, panel.Top, s, panel.Height, background);
        var content = new SKRect(panel.Left + 12 * s, panel.Top, panel.Right - 12 * s, panel.Bottom);
        var battle = frame.Battle;
        var header = TacticalBattlePanelHeader.Capture(frame);
        float y = header.Draw(canvas, content, s);

        float col = (float)PyMath.Max(0, (content.Width - 8 * s) * 0.5);
        _fit.Bounds = SKRect.Create(content.Left, y, col, 34 * s);
        _exitBattle.Bounds = SKRect.Create(content.Right - col, y, col, 34 * s);
        _enemyForecast.Bounds = SKRect.Create(content.Left, y + 43 * s, content.Width, 22 * s);
        _enemySectors.Bounds = SKRect.Create(content.Left, y + 69 * s, content.Width, 22 * s);
        _phaseAction.Bounds = SKRect.Create(content.Left, y + 100 * s, content.Width, 36 * s);
        _newBattle.Bounds = header.ShowNew ? _phaseAction.Bounds : SKRect.Empty;
        _fit.Label = L("FitField");
        _fit.Tooltip = L("FitFieldHelp");
        _enemyForecast.Label = L("EnemyForecast");
        _enemySectors.Label = L("EnemySectors");
        _enemyForecast.Draw(canvas, s);
        _enemySectors.Draw(canvas, s);
        _exitBattle.Label = L("ToMap"); _exitBattle.Enabled = !frame.Pending;
        _phaseAction.Label = L(battle.Phase == TacticalBattlePhase.Fire ? "Fire" : "Move");
        _phaseAction.Enabled = !frame.Pending && battle.Phase != TacticalBattlePhase.Over;
        _newBattle.Label = L("NewBattle"); _newBattle.Enabled = !frame.Pending;
        _fit.Draw(canvas, s); _exitBattle.Draw(canvas, s);
        if (header.ShowNew) _newBattle.Draw(canvas, s);
        else _phaseAction.Draw(canvas, s);
    }

    private void Fit()
    {
        if (Flow.State is { } state) _camera.Fit(state);
    }

    public override ScreenEvent OnKeyDown(Key key, KeyModifiers modifiers, bool repeat)
    {
        if (!ConfirmExit && !repeat && modifiers == KeyModifiers.None && key is Key.Number0 or Key.Keypad0)
        {
            Fit();
            return ScreenEvent.None;
        }
        return base.OnKeyDown(key, modifiers, repeat);
    }

    public override ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (ConfirmExit) return base.OnMouseDown(x, y, button);
        if (Flow.Pending) return ScreenEvent.None;
        if (button == MouseButton.Right) { _camera.BeginPan(x, y); return ScreenEvent.None; }
        if (!_panel.Contains(x, y)) return ScreenEvent.None;
        if (_fit.OnMouseDown(x, y, button)) Fit();
        else if (_exitBattle.OnMouseDown(x, y, button)) ConfirmExit = true;
        else if (_enemyForecast.OnMouseDown(x, y, button)) _enemySectors.HasFocus = false;
        else if (_enemySectors.OnMouseDown(x, y, button)) _enemyForecast.HasFocus = false;
        else if (_newBattle.OnMouseDown(x, y, button)) return ScreenEvent.NewTacticalBattle;
        else if (_phaseAction.OnMouseDown(x, y, button)) return ActionEvent;
        return ScreenEvent.None;
    }

    public override bool OnMouseMove(float x, float y)
    {
        if (ConfirmExit) return base.OnMouseMove(x, y);
        if (_camera.PanTo(x, y)) return true;
        return _fit.OnMouseMove(x, y) | _exitBattle.OnMouseMove(x, y) | _newBattle.OnMouseMove(x, y) | _phaseAction.OnMouseMove(x, y);
    }
    public override void OnMouseUp(float x, float y, MouseButton button)
    {
        if (button == MouseButton.Right) _camera.EndPan();
    }
    public override ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        if (!ConfirmExit && !Flow.Pending) _camera.Zoom(x, y, delta);
        return ScreenEvent.None;
    }
    public override void OnInputReset()
    {
        base.OnInputReset();
        _camera.EndPan();
        _fit.ResetInput(); _exitBattle.ResetInput(); _newBattle.ResetInput(); _phaseAction.ResetInput();
        _enemyForecast.HasFocus = _enemySectors.HasFocus = false;
    }
}
