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
    private static float OverlayBottom(TacticalBattleSnapshot b, TacticalSalvoFrame? salvo, TacticalFirePlan? firePlan) =>
        120 + (salvo is { InFlight: true } ? 262 : salvo is not null ? 246 : b.Phase == TacticalBattlePhase.Over ? 238 : 250 + PhasePanel.TargetListHeight(firePlan));
    private SKRect Raw(SKRect r) => new(r.Left * _viewport.S, r.Top * _viewport.S, r.Right * _viewport.S, r.Bottom * _viewport.S);
    private SKPoint DesignPoint(float x, float y) => new((float)(x / (double)_viewport.S), (float)(y / (double)_viewport.S));
    private IEnumerable<SKRect> OverlayRects()
    {
        yield return _panel;
        if (!_hoverCard.Bounds.IsEmpty) yield return _hoverCard.Bounds;
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
    }
    private ScreenEvent OverlayClick(SKPoint p, MouseButton button)
    {
        if (button == MouseButton.Left && !HasSalvoPresentation && _pilotColumn.Hit(p) is { } id)
        { if (Flow.State?.Phase == TacticalBattlePhase.Move) _orders.Select(id); else _fireOrders.Select(id); }
        if (button != MouseButton.Left) return ScreenEvent.None;
        if (_phasePanel.BeginDrag(p, _orders)) return ScreenEvent.None;
        var action = _phasePanel.HitButton(p);
        const string targetPrefix = "target:";
        if (action?.StartsWith(targetPrefix, StringComparison.Ordinal) == true)
        {
            _fireOrders.Choose(action[targetPrefix.Length..]);
            return ScreenEvent.None;
        }
        switch (action)
        {
            case "straight": _orders.Straight(); break;
            case "ai": _orders.Suggest(); break;
            case "decoy": if (_fireOrders.Selected is { } decoy) _fireOrders.ToggleDecoy(decoy); break;
            case "hold": _fireOrders.Choose(null); break;
            case "evade": if (_fireOrders.Selected is { } evader) _fireOrders.ToggleEvade(evader); break;
            case "primary":
                if (HasSalvoPresentation) { AdvanceSalvo(); return ScreenEvent.None; }
                return Flow.State?.Phase == TacticalBattlePhase.Over ? ScreenEvent.CloseTacticalBattle : ActionEvent;
            case "replay": ReplaySalvo(); break;
            case "new": return CanStartNewBattle ? ScreenEvent.NewTacticalBattle : ScreenEvent.None;
        }
        return ScreenEvent.None;
    }
    private bool OverlayDrag(SKPoint p) => _phasePanel.Drag(p, _orders);
    private void EndOverlayDrag() => _phasePanel.EndDrag();
    private void UpdateHover(SKPoint p)
    {
        if (_hoverFrame is not { } f || OverlayHit(p)) { HoverUnitId = null; return; }
        HoverUnitId = f.Battle.Units.Where(u => u.State != TacticalUnitState.Destroyed)
            .Select(u => (Unit: u, Distance: SKPoint.Distance(p, TacticalBattleMapRenderer.HoverPoint(f, u))))
            .Where(pair => pair.Distance <= (pair.Unit.Kind == TacticalUnitKind.Carrier ? 16 : 12))
            .OrderBy(pair => pair.Distance).FirstOrDefault().Unit?.Id;
    }
}
