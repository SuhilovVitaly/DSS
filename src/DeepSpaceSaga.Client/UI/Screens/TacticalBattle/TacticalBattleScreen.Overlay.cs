using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

public sealed partial class TacticalBattleScreen
{
    private readonly HoverCard _hoverCard = new();
    private TacticalBattleFrame? _hoverFrame;
    private SKPoint? _lastPointer;
    private readonly PhasePanel _phasePanel = new();
    private readonly PilotCardColumn _pilotColumn = new();
    private readonly PilotPortraitCatalog _portraits = PilotPortraitCatalog.Default;
    private BattleViewport _viewport = new(1, 1280);
    internal IReadOnlyList<SKRect> PanelRects => OverlayRects().Select(r => new SKRect(r.Left * _viewport.S, r.Top * _viewport.S, r.Right * _viewport.S, r.Bottom * _viewport.S)).ToArray();
    private SKRect Raw(SKRect r) => new(r.Left * _viewport.S, r.Top * _viewport.S, r.Right * _viewport.S, r.Bottom * _viewport.S);
    private SKPoint DesignPoint(float x, float y) => new((float)(x / (double)_viewport.S), (float)(y / (double)_viewport.S));
    private IEnumerable<SKRect> OverlayRects()
    {
        yield return _panel;
        if (!_hoverCard.Bounds.IsEmpty) yield return _hoverCard.Bounds;
        if (!_pilotColumn.Layout.VisiblePanel.IsEmpty) yield return _pilotColumn.Layout.VisiblePanel;
        for (int i = 0; i < _pilotColumn.Layout.Cards.Count; i++)
        {
            var rect = _pilotColumn.Rect(i);
            if (!rect.IsEmpty) yield return rect;
        }
    }
    private bool OverlayHit(SKPoint p) => OverlayRects().Any(r => r.Contains(p));
    private void DrawOverlay(SKCanvas c, TacticalBattleFrame f)
    {
        _hoverFrame = f;
        _phasePanel.Draw(c, f, Layout);
        _pilotColumn.Draw(c, _portraits);
        _hoverCard.Draw(c, f, _portraits);
        if (!ConfirmExit && !f.Pending && !_orderDrag && !_camera.Panning && _lastPointer is { } p)
        {
            var bounds = InteractiveBounds(p);
            if (!bounds.IsEmpty) BattleDesign.ButtonHover(c, bounds);
        }
    }
    internal SKRect CardCommandBounds(string action) => Raw(_pilotColumn.CommandBounds(action));

    private string? SelectedPlanningDrone => Flow.State?.Phase switch
    {
        TacticalBattlePhase.Move => _orders.Selected,
        TacticalBattlePhase.Fire => _fireOrders.Selected,
        _ => null
    };

    private SKRect InteractiveBounds(SKPoint p)
    {
        if (_pilotColumn.HitCommand(p) is { } command && !HasSalvoPresentation && SelectedPlanningDrone == command.UnitId)
            return command.Visible;
        if (_phasePanel.HitButton(p) is { } action && (action != "new" || CanStartNewBattle))
            return _phasePanel.ButtonBounds(action);
        if (!HasSalvoPresentation && Flow.State is { Phase: TacticalBattlePhase.Move or TacticalBattlePhase.Fire } &&
            !_pilotColumn.Layout.VisiblePanel.Contains(p) && _pilotColumn.HitCard(p) is { } card)
            return card.Bounds;
        return SKRect.Empty;
    }

    private ScreenEvent OverlayClick(SKPoint p, MouseButton button)
    {
        if (button != MouseButton.Left) return ScreenEvent.None;
        if (_pilotColumn.HitCommand(p) is { } command)
        {
            if (!HasSalvoPresentation && _hoverFrame is { Pending: false } && SelectedPlanningDrone == command.UnitId)
            {
                if (!_pilotColumn.BeginSlider(command, p, _orders))
                    ApplyPilotAction(command.Action);
            }
            return ScreenEvent.None;
        }
        if (_pilotColumn.Hit(p) is { } id)
        {
            if (!HasSalvoPresentation && Flow.State is { Phase: TacticalBattlePhase.Move or TacticalBattlePhase.Fire } state)
            {
                CancelOrderGesture();
                if (state.Phase == TacticalBattlePhase.Move) _orders.Select(id);
                else _fireOrders.Select(id);
            }
            return ScreenEvent.None;
        }
        var action = _phasePanel.HitButton(p);
        switch (action)
        {
            case "primary":
                if (HasSalvoPresentation) { AdvanceSalvo(); return ScreenEvent.None; }
                return Flow.State?.Phase == TacticalBattlePhase.Over ? ScreenEvent.CloseTacticalBattle : ActionEvent;
            case "replay": ReplaySalvo(); break;
            case "new": return CanStartNewBattle ? ScreenEvent.NewTacticalBattle : ScreenEvent.None;
        }
        return ScreenEvent.None;
    }
    private bool ApplyPilotAction(string? action)
    {
        const string targetPrefix = "target:";
        if (action?.StartsWith(targetPrefix, StringComparison.Ordinal) == true)
        {
            _fireOrders.Choose(action[targetPrefix.Length..]);
            return true;
        }
        switch (action)
        {
            case "speed-": _orders.Adjust(-2, 0); break;
            case "speed+": _orders.Adjust(2, 0); break;
            case "turn-": _orders.Adjust(0, -5); break;
            case "turn+": _orders.Adjust(0, 5); break;
            case "straight": _orders.Straight(); break;
            case "ai": _orders.Suggest(); break;
            case "decoy": if (_fireOrders.Selected is { } decoy) _fireOrders.ToggleDecoy(decoy); break;
            case "hold": _fireOrders.Choose(null); break;
            case "evade": if (_fireOrders.Selected is { } evader) _fireOrders.ToggleEvade(evader); break;
            default: return false;
        }
        return true;
    }

    private bool OverlayDrag(SKPoint p) => !HasSalvoPresentation && _pilotColumn.DragSlider(p, _orders);

    private void EndOverlayDrag() => _pilotColumn.EndSlider();
    private void UpdateHover(SKPoint p)
    {
        if (_hoverFrame is not { } f || OverlayHit(p)) { HoverUnitId = null; return; }
        bool tabletop = _skin.Metric("field.style", "tabletop", 0) == 1;
        HoverUnitId = f.Battle.Units.Where(u => u.State != TacticalUnitState.Destroyed)
            .Select(u => (Unit: u, Distance: SKPoint.Distance(p, TacticalBattleMapRenderer.HoverPoint(f, u))))
            .Where(pair => pair.Distance <= Math.Max(6, (pair.Unit.Kind == TacticalUnitKind.Carrier
                ? tabletop ? 36 : 16 : tabletop ? 26 : 12) * f.View.UnitScale))
            .OrderBy(pair => pair.Distance).FirstOrDefault().Unit?.Id;
    }
}
