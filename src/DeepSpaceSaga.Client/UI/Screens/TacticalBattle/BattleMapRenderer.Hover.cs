using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static partial class TacticalBattleMapRenderer
{
    internal static SKPoint HoverPoint(TacticalBattleFrame f, TacticalUnitSnapshot u) => MarkerPoint(f.View, u);
    static partial void DrawHover(SKCanvas c, TacticalBattleFrame f)
    {
        var u = f.Battle.Units.FirstOrDefault(u => u.Id == f.HoverUnitId); if (u is null) return;
        var p = HoverPoint(f, u); var color = u.Side == f.Battle.PlayerSide ? Cyan : Orange;
        foreach (int x in new[] { -1, 1 }) foreach (int y in new[] { -1, 1 })
        { var corner = new SKPoint(p.X + 14 * x, p.Y + 14 * y); Stroke(c, corner, new(corner.X - 5 * x, corner.Y), color, 1.5f); Stroke(c, corner, new(corner.X, corner.Y - 5 * y), color, 1.5f); }
    }
}
