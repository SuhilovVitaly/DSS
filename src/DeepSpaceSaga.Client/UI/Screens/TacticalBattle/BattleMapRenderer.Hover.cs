using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static partial class TacticalBattleMapRenderer
{
    internal static SKPoint HoverPoint(TacticalBattleFrame f, TacticalUnitSnapshot u) => MarkerPoint(f.View, u) +
        (TacticalBattleSkinRenderScope.Tabletop ? TabletopDodgeOffset(f, u) : SKPoint.Empty);
    static partial void DrawHover(SKCanvas c, TacticalBattleFrame f)
    {
        var u = f.Battle.Units.FirstOrDefault(u => u.Id == f.HoverUnitId && u.State != TacticalUnitState.Destroyed);
        var kind = AnomalyPresentation.HoverKind(f);
        if (u is null && kind is null) return;
        var p = u is not null ? HoverPoint(f, u) : f.HoverPoint!.Value;
        var color = u is not null ? u.Side == f.Battle.PlayerSide ? Cyan : Orange : kind == TacticalAnomalyKindCode.Heat ? Heat : Dust;
        foreach (int x in new[] { -1, 1 }) foreach (int y in new[] { -1, 1 })
        { var corner = new SKPoint(p.X + 14 * x, p.Y + 14 * y); Stroke(c, corner, new(corner.X - 5 * x, corner.Y), color, 1.5f); Stroke(c, corner, new(corner.X, corner.Y - 5 * y), color, 1.5f); }
    }
}
