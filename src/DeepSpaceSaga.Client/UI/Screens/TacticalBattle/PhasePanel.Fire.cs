using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed partial class PhasePanel
{
    internal static string ThreatText(TacticalBattleSnapshot battle, string droneId, bool evade)
    {
        string text = string.Join(" · ", battle.Incoming.Where(i => i.TargetId == droneId).Select(i =>
        {
            string name = battle.Units.FirstOrDefault(u => u.Id == i.ShooterId) is { } unit ? PilotCardModel.Name(unit) : i.ShooterId;
            return name + ": " + Pct(i.Chance) + (evade ? " → " + Pct(i.ChanceIfEvading ?? i.Chance) : "");
        }));
        return text.Length > 0 ? text : M("NobodyAims");
    }
    partial void DrawFire(SKCanvas c)
    {
        if (_frame.FirePlan?.Selected is not { } row) return;
        var unit = row.Drone;
        var target = _frame.Battle.Units.FirstOrDefault(u => u.Id == row.TargetId);
        var rules = _frame.Battle.Rules.Evasion;
        int mind = unit.Consciousness ?? rules?.MaxConsciousness ?? 100;
        if (row.Evade && rules is not null)
        {
            T(c, M("EvadeTitle", PilotCardModel.Name(unit)), 1050, 84, 15, Text, true);
            var tag = new SKRect(1048, 92, 1266, 110);
            Round(c, tag, PurpleDark, Purple, 1.2f, 0);
            T(c, M("EvadePlan"), 1055, tag.MidY + 11 * .36f, 11, Purple, true);
            T(c, "−" + BattleFormat.Number(rules.HitPenalty), 1050, 148, 28, Purple, true);
            T(c, M("EvadeCaption"), 1051, 163, 10.5f, Muted);
            T(c, M("EvadeMind", mind, mind - rules.Cost), 1264, 136, 12, Text, align: SKTextAlign.Right);
            T(c, M("EvadeNoFire"), 1264, 152, 11, Muted, align: SKTextAlign.Right);
            Line(c, 1050, 1264, 172);
            T(c, M("UnderFire"), 1050, 189, 12, Text);
            BoxText(c, new(1048, 196, 1266, 216), ThreatText(_frame.Battle, unit.Id, true));
            T(c, M("MindRecovery", rules.RecoveryPerTurn), 1050, 234, 11, Muted);
        }
        else if (row.Group.List.IsEmpty)
        {
            T(c, PilotCardModel.Name(unit), 1050, 84, 15, Text, true);
            Tag(c, new(1048, 92, 1266, 110), M("NoTargetsTag"));
            T(c, M("NoTargetsInArc"), 1050, 140, 14, Text, true);
            T(c, M("WeaponReach", unit.Build?.WeaponName, unit.Build?.Range, unit.Build?.ArcDegrees), 1050, 158, 11, Muted);
            Line(c, 1050, 1264, 167);
            T(c, M("UnderFire"), 1050, 185, 12, Text);
            BoxText(c, new(1048, 192, 1266, 212), ThreatText(_frame.Battle, unit.Id, false));
            string hint = row.CanEvade && rules is not null ? M("EvadeHint", rules.HitPenalty, rules.Cost)
                : M("EvadeLowMind", mind, rules?.MinConsciousness ?? 10);
            T(c, hint, 1050, 230, FitSize(hint, 11, 214), row.CanEvade ? Muted : Red);
        }
        else
        {
            Head(c, PilotCardModel.Name(unit) + " → " + (target is null ? "—" : PilotCardModel.Name(target)), "FireTag");
            T(c, row.Target is { } candidate ? Pct(candidate.Chance) : "—", 1050, 148, 28, Yellow, true);
            T(c, U("HitChance"), 1051, 163, 10.5f, Muted);
            T(c, U("Distance", row.Target is null ? "—" : F(row.Target.Distance)), 1264, 136, 12, Text, align: SKTextAlign.Right);
            T(c, U("Angle", row.Target is null ? "—" : F(Math.Abs(row.Target.Angle))), 1264, 154, 12, Text, align: SKTextAlign.Right);
            Line(c, 1050, 1264, 172);
            T(c, U("WeaponDamage", unit.Build?.WeaponName, unit.Build?.DamageMin, unit.Build?.DamageMax), 1050, 189, 12, Text);
            BoxText(c, new(1048, 196, 1266, 216), target is { Armor: > 0 } ? U("ArmorFirst", target.Armor) : U("NoArmor"));
            if (target is not null) T(c, U("TargetHealth", PilotCardModel.Name(target), target.Hull, target.MaxHull, target.Armor, target.MaxArmor), 1050, 234, 11, Text);
        }
        ButtonAt(c, "hold", new(1048, 242, 1154, 264), U("HoldFire"));
        var button = new SKRect(1160, 242, 1266, 264);
        _buttons["evade"] = R(button.Left, button.Top, button.Right, button.Bottom);
        if (Recording is { } layout) layout.Rects["button:evade"] = _buttons["evade"];
        EvadeButton(c, button, M(row.Evade ? "EvadeActive" : row.CanEvade ? "Evade" : "EvadeUnavailable"), row.Evade ? EvadeButtonState.Selected : row.CanEvade ? EvadeButtonState.Available : EvadeButtonState.Unavailable);
        int count = _frame.FirePlan.Rows.Count(r => r.TargetId is not null), evades = _frame.FirePlan.Rows.Count(r => r.Evade);
        string summary = evades == 0 ? U("FireCount", count, _frame.FirePlan.Rows.Length - count)
            : M("FireSummaryEvade", count, M(count == 1 ? "Shoots.One" : "Shoots.Many"), evades,
                M(evades == 1 ? "Evades.One" : "Evades.Many"), _frame.FirePlan.Rows.Length - count - evades);
        StandardAction(c, summary, U("FireStatus"), U("FireAction"));
    }
}
