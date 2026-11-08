using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

public sealed partial class TacticalBattleScreen
{
    private readonly PhasePanel _phasePanel = new();
    private readonly PilotCardColumn _pilotColumn = new();
    private readonly PilotPortraitCatalog _portraits = PilotPortraitCatalog.Default;
    private BattleViewport _viewport = new(1, 1280);
    internal IReadOnlyList<SKRect> PanelRects => OverlayRects().Select(r => new SKRect(r.Left * _viewport.S, r.Top * _viewport.S, r.Right * _viewport.S, r.Bottom * _viewport.S)).ToArray();
    private static float OverlayBottom(TacticalBattleSnapshot b, TacticalSalvoFrame? salvo) =>
        120 + (salvo is { InFlight: true } ? 262 : salvo is not null ? 246 : b.Phase == TacticalBattlePhase.Over ? 238 : 250);
    private SKPoint DesignPoint(float x, float y) => new(x / _viewport.S, y / _viewport.S);
    private IEnumerable<SKRect> OverlayRects()
    {
        yield return _panel;
        int count = Flow.State?.Units.Count(u => u.Kind == TacticalUnitKind.Drone && u.Side == Flow.State.PlayerSide) ?? 0;
        for (int i = 0; i < count; i++) yield return new(6, 6 + 87 * i, 196, 88 + 87 * i);
    }
    private bool OverlayHit(SKPoint p) => OverlayRects().Any(r => r.Contains(p));
    private void DrawOverlay(SKCanvas c, TacticalBattleFrame f)
    {
        _phasePanel.Draw(c, f, Layout);
        _pilotColumn.Draw(c, PilotCardModel.Project(f, _portraits), _portraits);
    }
    private ScreenEvent OverlayClick(SKPoint p, MouseButton button)
    {
        if (button == MouseButton.Left && !HasSalvoPresentation && _pilotColumn.Hit(p) is { } id)
        { if (Flow.State?.Phase == TacticalBattlePhase.Move) _orders.Select(id); else _fireOrders.Select(id); }
        return ScreenEvent.None;
    }
    private bool OverlayDrag(SKPoint p) => false;
    private void EndOverlayDrag() { }
    private void UpdateHover(SKPoint p) { }
}
