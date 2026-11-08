using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

public sealed partial class TacticalBattleScreen
{
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
        BattleDesign.PanelBox(c, f.Panel);
        Layout.Rects["phase"] = f.Panel;
        int i = 0;
        foreach (var u in f.Battle.Units.Where(u => u.Kind == TacticalUnitKind.Drone && u.Side == f.Battle.PlayerSide))
        { var r = new SKRect(6, 6 + 87 * i, 196, 88 + 87 * i); BattleDesign.PanelBox(c, r, u.Id == f.SelectedDroneId, 5); Layout.Rects["pilot:" + u.Id] = r; i++; }
    }
    private ScreenEvent OverlayClick(SKPoint p, MouseButton button) => ScreenEvent.None;
    private bool OverlayDrag(SKPoint p) => false;
    private void EndOverlayDrag() { }
    private void UpdateHover(SKPoint p) { }
}
