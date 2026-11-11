using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.PilotCardColumn;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed class PilotCardFireBlock
{
    private const int VisibleRows = 3;
    private const float RowHeight = 20;
    private const float ListTop = 14;

    private TacticalTargetGroupSnapshot? _group;
    private int _first;
    private SKRect _list;

    public void Observe(TacticalFirePlanRow? selected)
    {
        var group = selected?.Group;
        if (!ReferenceEquals(group, _group))
        {
            _group = group;
            _first = 0;
        }
        _list = SKRect.Empty;
    }

    public bool Scroll(SKPoint point, float delta)
    {
        if (_group is not { List.Length: > VisibleRows } group || _list.IsEmpty ||
            !_list.Contains(point) || !float.IsFinite(delta) || delta == 0)
            return false;
        _first = Math.Clamp(_first - Math.Sign(delta), 0, group.List.Length - VisibleRows);
        return true;
    }

    private static bool HasTargets(TacticalFirePlanRow row) => !row.Evade && !row.Drone.InDust && !row.Group.List.IsEmpty;
    private static float BodyHeight(TacticalFirePlanRow row) =>
        HasTargets(row) ? Math.Min(VisibleRows, row.Group.List.Length) * RowHeight : 36;

    public static float Height(TacticalFirePlanRow row) => ListTop + BodyHeight(row) + 78;

    public void Draw(SKCanvas canvas, PilotCardColumn column, TacticalBattleSnapshot battle,
        TacticalFirePlanRow row, float left, float right, float top)
    {
        string id = row.Drone.Id;
        float width = right - left;
        float listTop = top + ListTop;
        float after = listTop + BodyHeight(row);
        var build = row.Drone.Build;
        var rules = battle.Rules.Evasion;
        int mind = row.Drone.Consciousness ?? rules?.MaxConsciousness ?? 100;
        float weaponWidth = width;
        if (HasTargets(row) && row.Target is { } target)
        {
            string geometry = M("Card.Geometry", F(target.Distance), F(Math.Abs(target.Angle)));
            T(canvas, geometry, right, top + 9, 8.5f, Text, align: SKTextAlign.Right);
            weaponWidth -= Measure(geometry, 8.5f) + 5;
        }
        FittedText(canvas, U("WeaponDamage", build?.WeaponName, build?.DamageMin, build?.DamageMax),
            left, top + 9, 8.5f, weaponWidth, Muted);

        if (HasTargets(row))
        {
            DrawTargets(canvas, column, battle, row, left, right, listTop);
            if (battle.Units.FirstOrDefault(unit => unit.Id == row.TargetId) is { } defense)
            {
                string name = PilotCardModel.Name(defense);
                string condition = defense.Armor > 0
                    ? M("TargetArmorThenHull", name, defense.Armor, defense.MaxArmor, defense.Hull, defense.MaxHull)
                    : M("TargetHullOnly", name, defense.Hull, defense.MaxHull);
                FittedText(canvas, condition, left, after + 9, 8.5f, width, Muted);
            }
        }
        else if (row.Evade && rules is not null)
        {
            State("−" + BattleFormat.Number(rules.HitPenalty) + " " + M("EvadeCaption"),
                M("EvadeMind", mind, mind - rules.Cost) + " · " + M("EvadeNoFire"),
                PurpleDark, Purple, Purple, Text, "ui.box.evade.selected");
            T(canvas, M("MindRecovery", rules.RecoveryPerTurn), left, after + 9, 8.5f, Muted);
        }
        else if (row.Drone.InDust)
        {
            var dust = PhasePanel.DustText;
            State(dust.Message, dust.Detail, PurpleDark, Dust, Dust, Dust, "ui.box.dust");
        }
        else
        {
            State(M("NoTargetsInArc"), M("WeaponReach", build?.WeaponName, build?.Range, build?.ArcDegrees),
                Box, BoxEdge, Text, Muted, "ui.box");
        }

        string label = M("UnderFire") + ": ";
        string threat = PhasePanel.ThreatText(battle, id, row.Evade, row.Decoy);
        float threatSize = 8.5f;
        while (threatSize > 6.5f && Measure(label + threat, threatSize) > width) threatSize -= .25f;
        T(canvas, label, left, after + 21, threatSize, Text);
        float labelWidth = Measure(label, threatSize);
        FittedText(canvas, threat, left + labelWidth, after + 21, threatSize, width - labelWidth, Muted);

        var decoy = new SKRect(left, after + 26, right, after + 44);
        DecoyRow(canvas, decoy, PhasePanel.DecoyText(battle, row), row.Drone.DecoyCharges ?? 0,
            build?.DecoyMaxCharges ?? 0, row.DecoyState, compact: true);
        if (row.DecoyState != DecoyRowState.Unavailable) column.RegisterCommand(id, "decoy", decoy);

        var hold = new SKRect(left, after + 48, left + 87, after + 65);
        CommandButton(canvas, hold, U("HoldFire"));
        column.RegisterCommand(id, "hold", hold);

        var evade = new SKRect(left + 91, after + 48, right, after + 65);
        EvadeButton(canvas, evade, M(row.Evade ? "EvadeActive" : row.CanEvade ? "Evade" : "EvadeUnavailable"),
            row.Evade ? EvadeButtonState.Selected : row.CanEvade ? EvadeButtonState.Available : EvadeButtonState.Unavailable, compact: true);
        if (row.CanEvade) column.RegisterCommand(id, "evade", evade);
        string hint = row.CanEvade && rules is not null
            ? M("EvadeHint", rules.HitPenalty, rules.Cost) : M("EvadeLowMind", mind, rules?.MinConsciousness ?? 10);
        FittedText(canvas, hint, left, after + 75, 7.5f, width, row.CanEvade ? Muted : Red);

        void State(string title, string detail, SKColor fill, SKColor border, SKColor titleTone, SKColor detailTone, string role)
        {
            Round(canvas, SKRect.Create(left, listTop, width, 36), fill, border, 1.2f, role: role);
            FittedText(canvas, title, left + 6, top + 28, 9.5f, width - 12, titleTone, true);
            FittedText(canvas, detail, left + 6, top + 42, 8.5f, width - 12, detailTone);
        }
    }

    private void DrawTargets(SKCanvas canvas, PilotCardColumn column, TacticalBattleSnapshot battle,
        TacticalFirePlanRow row, float left, float right, float top)
    {
        int rows = Math.Min(VisibleRows, row.Group.List.Length);
        bool overflowing = row.Group.List.Length > rows;
        float height = rows * RowHeight - 2;
        _list = SKRect.Intersect(SKRect.Create(left, top, right - left, height), PilotColumnLayout.Viewport);
        for (int i = 0; i < rows; i++)
        {
            var candidate = row.Group.List[_first + i];
            var bounds = SKRect.Create(left, top + i * RowHeight, right - left, RowHeight - 2);
            bool selected = candidate.TargetId == row.TargetId;
            string role = selected ? "ui.box.target.selected" : "ui.box";
            Round(canvas, bounds, selected ? YellowDark : Box, selected ? Yellow : BoxEdge, role: role);
            var target = battle.Units.FirstOrDefault(unit => unit.Id == candidate.TargetId);
            string name = target is null ? candidate.TargetId : PilotCardModel.Name(target);
            string chance = Pct(candidate.Chance);
            var tone = selected ? Yellow : Text;
            float x = bounds.Right - 6;
            float y = bounds.MidY + 3.5f;
            T(canvas, chance, x, y, 9.5f, candidate.Absorption > 0 ? Heat : tone, true, SKTextAlign.Right);
            x -= Measure(chance, 9.5f, true) + 4;
            if (candidate.Absorption > 0)
            {
                string absorption = M("Card.Absorption", Math.Round(candidate.Absorption * 100));
                T(canvas, absorption, x, y, 7.5f, Heat, align: SKTextAlign.Right);
                x -= Measure(absorption, 7.5f) + 4;
            }
            FittedText(canvas, name, bounds.Left + 6, y, 9.5f, x - bounds.Left - 6, tone, selected);
            column.RegisterCommand(row.Drone.Id, "target:" + candidate.TargetId, bounds);
        }
        if (!overflowing) return;
        float xTrack = right - 1;
        float thumb = height * rows / row.Group.List.Length;
        float thumbTop = top + (height - thumb) * _first / (row.Group.List.Length - rows);
        Stroke(canvas, new(xTrack, top), new(xTrack, top + height), Track, 2);
        Stroke(canvas, new(xTrack, thumbTop), new(xTrack, thumbTop + thumb), Muted, 2);
    }
}
