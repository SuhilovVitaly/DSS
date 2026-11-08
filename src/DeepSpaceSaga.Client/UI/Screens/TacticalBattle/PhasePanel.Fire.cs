using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed partial class PhasePanel
{
    partial void DrawFire(SKCanvas c)
    {
        if (_frame.FirePlan?.Selected is not { } row) return;
        var unit = row.Drone; var target = _frame.Battle.Units.FirstOrDefault(u => u.Id == row.TargetId);
        Head(c, PilotCardModel.Name(unit) + " → " + (target is null ? "—" : PilotCardModel.Name(target)), "FireTag");
        T(c, row.Target is { } candidate ? Pct(candidate.Chance) : "—", 1050, 148, 28, Yellow, true);
        T(c, U("HitChance"), 1051, 163, 10.5f, Muted);
        T(c, U("Distance", row.Target is null ? "—" : F(row.Target.Distance)), 1264, 136, 12, Text, align: SKTextAlign.Right);
        T(c, U("Angle", row.Target is null ? "—" : F(Math.Abs(row.Target.Angle))), 1264, 154, 12, Text, align: SKTextAlign.Right);
        Line(c, 1050, 1264, 172);
        T(c, U("WeaponDamage", unit.Build?.WeaponName, unit.Build?.DamageMin, unit.Build?.DamageMax), 1050, 189, 12, Text);
        BoxText(c, new(1048, 196, 1266, 216), target is { Armor: > 0 } ? U("ArmorFirst", target.Armor) : U("NoArmor"));
        if (target is not null) T(c, U("TargetHealth", PilotCardModel.Name(target), target.Hull, target.MaxHull, target.Armor, target.MaxArmor), 1050, 234, 11, Text);
        ButtonAt(c, "hold", new(1048, 242, 1266, 264), U("HoldFire"));
        int count = _frame.FirePlan.Rows.Count(r => r.TargetId is not null);
        StandardAction(c, U("FireCount", count, _frame.FirePlan.Rows.Length - count), U("FireStatus"), U("FireAction"));
    }
}
