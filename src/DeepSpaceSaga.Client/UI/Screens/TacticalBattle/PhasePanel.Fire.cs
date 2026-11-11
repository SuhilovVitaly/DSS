using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed partial class PhasePanel
{
    private const int VisibleTargetRows = 3;
    private const int TargetRowHeight = 20;
    private TacticalTargetGroupSnapshot? _targetGroup;
    private int _targetFirst;
    private SKRect _targetList;

    internal static int TargetListHeight(TacticalFirePlan? plan) =>
        plan?.Selected is { Group.List.Length: > 1 } row ? Math.Min(VisibleTargetRows, row.Group.List.Length) * TargetRowHeight : 0;

    internal bool ScrollTargets(SKPoint point, float delta)
    {
        if (Phase != "Fire" || _targetList.IsEmpty || !_targetList.Contains(point) || !float.IsFinite(delta)) return false;
        _targetFirst = Math.Clamp(_targetFirst - Math.Sign(delta), 0, Math.Max(0, (_targetGroup?.List.Length ?? 0) - VisibleTargetRows));
        return true;
    }

    private void DrawTargetList(SKCanvas canvas, TacticalFirePlanRow row)
    {
        if (!ReferenceEquals(_targetGroup, row.Group)) { _targetGroup = row.Group; _targetFirst = 0; }
        int rows = Math.Min(VisibleTargetRows, row.Group.List.Length);
        _targetList = R(1048, 271, 1266, 271 + rows * TargetRowHeight);
        for (int index = 0; index < rows; index++)
        {
            var candidate = row.Group.List[_targetFirst + index];
            var target = _frame.Battle.Units.FirstOrDefault(unit => unit.Id == candidate.TargetId);
            string caption = (target is null ? candidate.TargetId : PilotCardModel.Name(target)) + " · " + Pct(candidate.Chance);
            float y = 271 + index * TargetRowHeight;
            var bounds = new SKRect(1048, y, 1266, y + TargetRowHeight - 2);
            string key = "target:" + candidate.TargetId;
            _buttons[key] = R(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
            if (Recording is { } layout) layout.Rects["button:" + key] = _buttons[key];
            bool selected = candidate.TargetId == row.TargetId;
            Round(canvas, bounds, selected ? YellowDark : Box, selected ? Yellow : BoxEdge,
                role: selected ? "ui.box.target.selected" : "ui.box");
            T(canvas, caption, bounds.Left + 6, y + 12.5f, FitSize(caption, 11, bounds.Width - 12),
                selected ? Yellow : Text, selected, role: selected ? "ui.box.target.selected" : "ui.text");
        }
        if (row.Group.List.Length > rows)
        {
            float height = rows * TargetRowHeight - 2;
            float thumb = height * rows / row.Group.List.Length;
            float y = 271 + (height - thumb) * _targetFirst / (row.Group.List.Length - rows);
            Stroke(canvas, new(1264, 271), new(1264, 271 + height), Track, 2);
            Stroke(canvas, new(1264, y), new(1264, y + thumb), Muted, 2);
        }
    }
    internal static string FireCaption(TacticalTargetCandidateSnapshot? candidate) => candidate is { Absorption: > 0 }
        ? M("ChanceWithAbsorption", Pct(candidate.Chance), Math.Round(candidate.Absorption * 100)) : U("HitChance");
    internal static (string Tag, string Message, string Detail) DustText =>
        (M("FireImpossibleTag"), M("DroneInDust"), M("DustForbidsFire"));
    internal static string FireSummary(TacticalFirePlan plan)
    {
        int count = plan.Rows.Count(r => r.TargetId is not null && !r.Evade && !r.Drone.InDust), evades = plan.Rows.Count(r => r.Evade);
        int dust = plan.Rows.Count(r => r.Drone.InDust && !r.Evade), none = plan.Rows.Length - count - evades - dust;
        string shoots = M(count == 1 ? "Shoots.One" : "Shoots.Many"), evading = M(evades == 1 ? "Evades.One" : "Evades.Many");
        string summary = dust > 0 ? M("FireSummaryDust", count, shoots + (evades > 0 ? " · " + evades + " " + evading : ""), dust, none)
            : evades == 0 ? U("FireCount", count, none) : M("FireSummaryEvade", count, shoots, evades, evading, none);
        int decoys = plan.Rows.Count(r => r.Decoy);
        if (decoys > 0)
        {
            string form = decoys % 100 is >= 11 and <= 14 ? "Many" : decoys % 10 == 1 ? "One" : decoys % 10 is >= 2 and <= 4 ? "Few" : "Many";
            summary += " · " + decoys + " " + M("Decoys." + form);
        }
        return summary;
    }
    internal static string ThreatText(TacticalBattleSnapshot battle, string droneId, bool evade, bool decoy = false)
    {
        string text = string.Join(" · ", battle.Incoming.Where(i => i.TargetId == droneId).Select(i =>
        {
            string name = battle.Units.FirstOrDefault(u => u.Id == i.ShooterId) is { } unit ? PilotCardModel.Name(unit) : i.ShooterId;
            return name + ": " + Pct(i.Chance) + (evade ? " → " + Pct(i.ChanceIfEvading ?? i.Chance) : decoy && i.ChanceIfDecoy is { } chance ? " → " + Pct(chance) : "");
        }));
        return text.Length > 0 ? text : M("NobodyAims");
    }
    internal static string DecoyText(TacticalBattleSnapshot battle, TacticalFirePlanRow row)
    {
        if (!row.CanDecoy) return M("DecoyEmpty");
        if (!row.Decoy) return M("Decoy");
        var incoming = battle.Incoming.FirstOrDefault(i => i.TargetId == row.Drone.Id && i.ChanceIfDecoy is not null);
        if (incoming is null) return M("DecoyArmedNoThreat");
        string name = battle.Units.FirstOrDefault(u => u.Id == incoming.ShooterId) is { } unit ? PilotCardModel.Name(unit) : incoming.ShooterId;
        return M("DecoyArmed", name, Pct(incoming.Chance), Pct(incoming.ChanceIfDecoy!.Value));
    }
    private void DrawDecoy(SKCanvas c, TacticalFirePlanRow row, float top)
    {
        var r = new SKRect(1048, top, 1266, 238);
        _buttons["decoy"] = R(r.Left, r.Top, r.Right, r.Bottom);
        if (Recording is { } layout) layout.Rects["button:decoy"] = _buttons["decoy"];
        DecoyRow(c, r, DecoyText(_frame.Battle, row), row.Drone.DecoyCharges ?? 0,
            row.Drone.Build?.DecoyMaxCharges ?? 0, row.DecoyState);
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
        else if (row.Group.List.IsEmpty || unit.InDust)
        {
            T(c, PilotCardModel.Name(unit), 1050, 84, 15, Text, true);
            if (unit.InDust)
            {
                var text = DustText; var tag = new SKRect(1048, 92, 1266, 110);
                Round(c, tag, PurpleDark, Dust, 1.2f, 0); T(c, text.Tag, 1055, tag.MidY + 11 * .36f, 11, Dust, true);
                T(c, text.Message, 1050, 140, 14, Text, true);
                T(c, text.Detail, 1050, 158, FitSize(text.Detail, 11, 214), Dust);
            }
            else
            {
                Tag(c, new(1048, 92, 1266, 110), M("NoTargetsTag"));
                T(c, M("NoTargetsInArc"), 1050, 140, 14, Text, true);
                T(c, M("WeaponReach", unit.Build?.WeaponName, unit.Build?.Range, unit.Build?.ArcDegrees), 1050, 158, 11, Muted);
            }
            Line(c, 1050, 1264, 165);
            T(c, M("UnderFire"), 1050, 180, 12, Text);
            BoxText(c, new(1048, 186, 1266, 204), ThreatText(_frame.Battle, unit.Id, false, row.Decoy));
            string hint = row.CanEvade && rules is not null ? M("EvadeHint", rules.HitPenalty, rules.Cost)
                : M("EvadeLowMind", mind, rules?.MinConsciousness ?? 10);
            T(c, hint, 1050, 217, FitSize(hint, 11, 214), row.CanEvade ? Muted : Red);
            DrawDecoy(c, row, 222);
        }
        else
        {
            Head(c, PilotCardModel.Name(unit) + " → " + (target is null ? "—" : PilotCardModel.Name(target)), "FireTag");
            T(c, row.Target is { } candidate ? Pct(candidate.Chance) : "—", 1050, 148, 28, Yellow, true, role: "ui.fire.chance");
            T(c, FireCaption(row.Target), 1051, 163, 10.5f, row.Target?.Absorption > 0 ? Heat : Muted);
            T(c, U("Distance", row.Target is null ? "—" : F(row.Target.Distance)), 1264, 136, 12, Text, align: SKTextAlign.Right);
            T(c, U("Angle", row.Target is null ? "—" : F(Math.Abs(row.Target.Angle))), 1264, 154, 12, Text, align: SKTextAlign.Right);
            Line(c, 1050, 1264, 172);
            T(c, U("WeaponDamage", unit.Build?.WeaponName, unit.Build?.DamageMin, unit.Build?.DamageMax), 1050, 189, 12, Text);
            BoxText(c, new(1048, 196, 1266, 216), target is null ? U("NoArmor") : target.Armor > 0
                ? M("TargetArmorThenHull", PilotCardModel.Name(target), target.Armor, target.MaxArmor, target.Hull, target.MaxHull)
                : M("TargetHullOnly", PilotCardModel.Name(target), target.Hull, target.MaxHull));
            DrawDecoy(c, row, 221);
        }
        ButtonAt(c, "hold", new(1048, 242, 1154, 264), U("HoldFire"));
        var button = new SKRect(1160, 242, 1266, 264);
        _buttons["evade"] = R(button.Left, button.Top, button.Right, button.Bottom);
        if (Recording is { } layout) layout.Rects["button:evade"] = _buttons["evade"];
        EvadeButton(c, button, M(row.Evade ? "EvadeActive" : row.CanEvade ? "Evade" : "EvadeUnavailable"), row.Evade ? EvadeButtonState.Selected : row.CanEvade ? EvadeButtonState.Available : EvadeButtonState.Unavailable);
        if (row.Group.List.Length > 1) DrawTargetList(c, row);
        StandardAction(c, FireSummary(_frame.FirePlan), U("FireStatus"), U("FireAction"));
    }
}
