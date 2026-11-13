using DeepSpaceSaga.Contracts;
using SkiaSharp;
using Skin = DeepSpaceSaga.Client.UI.Screens.TacticalBattle.TacticalBattleSkinRenderScope;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.PilotCardColumn;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed partial class PilotCardFireBlock
{
    internal static float FireHeight(TacticalBattleSnapshot battle, TacticalFirePlanRow row, TacticalSalvoForecast? forecast) =>
        new PilotCardFireBlock().DrawSlim(null, null, battle, row, forecast, 6, 144, 5);

    // Measurement and drawing share every vertical increment and wrapped text run.
    private float DrawSlim(SKCanvas? c, PilotCardColumn? column, TacticalBattleSnapshot battle,
        TacticalFirePlanRow row, TacticalSalvoForecast? forecast, float left, float right, float top)
    {
        var ink = SKColor.Parse("#E4ECF4"); var sub = SKColor.Parse("#9AAABB");
        var damage = SKColor.Parse("#FFA060"); var threat = SKColor.Parse("#FF8A6A");
        float width = right - left, y = top;
        var build = row.Drone.Build;
        string weapon = U("WeaponDamage", build?.WeaponName, build?.DamageMin, build?.DamageMax);
        string name = build?.WeaponName ?? "";
        string amount = $"{build?.DamageMin}–{build?.DamageMax}";
        float nameWidth = Measure(name, 6.6f, true);
        string middle = weapon.StartsWith(name, StringComparison.Ordinal) && weapon.EndsWith(amount, StringComparison.Ordinal)
            ? weapon[name.Length..^amount.Length] : " ";
        TextAt(name, left, y + 8, 6.6f, ink, true);
        TextAt(middle, left + nameWidth, y + 8, 6.6f, sub);
        TextAt(amount, left + nameWidth + Measure(middle, 6.6f), y + 8, 6.6f, damage, true);
        if (row.Target is { } target)
            TextAt(M("Card.Geometry", F(target.Distance), F(Math.Abs(target.Angle))), left, y + 15, 6, sub);
        y += 19;

        if (HasTargets(row))
        {
            int rows = Math.Min(VisibleRows, row.Group.List.Length);
            if (c is not null && column is not null)
                _list = SKRect.Intersect(SKRect.Create(left, y, width, rows * RowHeight - 3), column.Layout.VisiblePanel);
            for (int i = 0; i < rows; i++)
            {
                var candidate = row.Group.List[_first + i];
                bool selected = candidate.TargetId == row.TargetId;
                var bounds = SKRect.Create(left, y, width, 17);
                BoxAt(bounds, selected ? "ui.box.target.selected" : "ui.button");
                var unit = battle.Units.FirstOrDefault(u => u.Id == candidate.TargetId);
                if (c is not null && unit is not null)
                    TacticalShipSprites.Draw(c, unit, new(left + 17, y + 8.5f), false, 180,
                        factor: Math.Max(1, TacticalShipSprites.Factor(TacticalShipSprites.CanvasScale(c)) / 2), shadow: false);
                string chance = Pct(candidate.Chance);
                string targetName = unit is null ? candidate.TargetId : PilotCardModel.Name(unit);
                float nameSpace = width - 39 - Measure(chance, 9.4f, true);
                TextAt(BuildCardModel.Ellipsize(targetName, nameSpace, 7.2f, true), left + 34, y + 11.4f,
                    7.2f, selected ? SKColor.Parse("#2A1A08") : ink, true);
                TextAt(chance, right - 5, y + 12, 9.4f, selected ? SKColor.Parse("#5A2210") : ink, true, align: SKTextAlign.Right);
                column?.RegisterCommand(row.Drone.Id, "target:" + candidate.TargetId, bounds);
                y += 20;
            }
            if (c is not null && row.Group.List.Length > rows)
            {
                float h = rows * RowHeight - 3, thumb = h * rows / row.Group.List.Length, start = y - rows * RowHeight;
                Stroke(c, new(right - 1, start), new(right - 1, start + h), Track, 1);
                float thumbTop = start + (h - thumb) * _first / (row.Group.List.Length - rows);
                Stroke(c, new(right - 1, thumbTop), new(right - 1, thumbTop + thumb), sub, 1);
            }
            if (battle.Units.FirstOrDefault(u => u.Id == row.TargetId) is { } defense)
            {
                string condition = defense.Armor > 0
                    ? M("TargetArmorThenHull", PilotCardModel.Name(defense), defense.Armor, defense.MaxArmor, defense.Hull, defense.MaxHull)
                    : M("TargetHullOnly", PilotCardModel.Name(defense), defense.Hull, defense.MaxHull);
                Wrapped(condition, 6.2f, 8, ink);
            }
            if (row.Target?.Factors is { } factors)
                Wrapped(BattleFormat.HitFactors(factors), 6.2f, 8, sub);
            if (forecast is not null)
            {
                Wrapped(M("Card.SalvoForecast", forecast.Shots, BattleFormat.Probability(forecast.DisableChance, 1)), 7, 9, ink);
                Wrapped(M("Card.SalvoDamage", forecast.MinDamage, forecast.MaxDamage), 6.6f, 8, damage);
                Wrapped(M("Card.SalvoAssumption"), 6, 8, sub);
            }
            else if (row.TargetId is null)
                Wrapped(M("Card.FireHeld"), 6.6f, 9, sub);
        }
        else if (row.Evade || row.Drone.InDust)
        {
            string role = row.Evade ? "ui.box.evade.selected" : "ui.box.dust";
            BoxAt(SKRect.Create(left, y, width, 17), role);
            TextAt(row.Evade ? M("EvadeActive") : PhasePanel.DustText.Message, left + 6, y + 11, 7,
                Skin.Color(role, "text", sub), true);
            y += 20;
        }
        else
        {
            if (c is not null)
            {
                using var dash = SKPathEffect.CreateDash([2, 1.5f], 0);
                using var p = new SKPaint { Color = sub, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = .7f, PathEffect = dash };
                c.DrawRect(SKRect.Create(left, y, width, 13), p);
            }
            TextAt(M(row.Drone.Jammed ? "Card.FireJammed" : "Card.NoShot"), (left + right) / 2, y + 9,
                7, sub, italic: true, align: SKTextAlign.Center);
            y += 16;
        }

        if (!row.Evade && !HasTargets(row) && TacticalFireBlocker.Nearest(battle, row.Drone) is { } blocker)
        {
            Wrapped(M("Card.NearestTarget", PilotCardModel.Name(blocker.Target)), 7, 9, ink);
            if (blocker.RangeGap > 0)
                Wrapped(M("Card.RangeGap", BattleFormat.Number(Math.Ceiling(blocker.RangeGap * 10) / 10, 1)), 6.6f, 9, sub);
            if (blocker.AngleGap > 0)
                Wrapped(M("Card.AngleGap", BattleFormat.Degrees(Math.Ceiling(blocker.AngleGap * 10) / 10, 1)), 6.6f, 9, sub);
            if (row.Drone.InDust)
                Wrapped(M("Card.ClearDust"), 6.6f, 9, sub);
        }

        var incoming = battle.Incoming.Where(i => i.TargetId == row.Drone.Id).ToArray();
        foreach (var shot in incoming)
        {
            string shooter = battle.Units.FirstOrDefault(u => u.Id == shot.ShooterId) is { } unit ? PilotCardModel.Name(unit) : shot.ShooterId;
            string chance = Pct(row.Evade ? shot.ChanceIfEvading ?? shot.Chance : row.Decoy ? shot.ChanceIfDecoy ?? shot.Chance : shot.Chance);
            string prefix = M("Panel.UnderFireShort", "", "").TrimEnd() + " ";
            TextAt(prefix, left, y + 6, 6.2f, sub);
            TextAt(BuildCardModel.Ellipsize(shooter + " " + chance, width - Measure(prefix, 6.2f), 6.2f, true),
                left + Measure(prefix, 6.2f), y + 6, 6.2f, threat, true);
            y += 9;
        }
        if (incoming.Length == 0) y += 2;
        if (c is not null) Stroke(c, new(left, y), new(right, y), SKColor.Parse("#5A4E3E"), .6f);
        y += 4;
        string decoyRole = row.DecoyState == DecoyRowState.Unavailable ? "ui.box.decoy.unavailable" : row.Decoy ? "ui.box.decoy.selected" : "ui.box.decoy";
        var decoy = SKRect.Create(left, y, width, 14); BoxAt(decoy, decoyRole);
        var decoyColor = Skin.Color(decoyRole, "text", SKColor.Parse("#B8E8A0"));
        TextAt(M("Decoy"), left + 6, y + 9.6f, 7, decoyColor, true);
        if (c is not null)
            for (int i = 0; i < Math.Min(8, row.Drone.DecoyCharges ?? 0); i++)
            {
                float x = right - 10 - i * 6, cy = y + 7;
                Polygon(c, [new(x, cy - 2.4f), new(x + 2.4f, cy), new(x, cy + 2.4f), new(x - 2.4f, cy)], decoyColor);
            }
        if (row.DecoyState != DecoyRowState.Unavailable) column?.RegisterCommand(row.Drone.Id, "decoy", decoy);
        y += 17;
        string evadeRole = row.Evade ? "ui.box.evade.selected" : row.CanEvade ? "ui.box.evade" : "ui.box.evade.unavailable";
        var evade = SKRect.Create(left, y, 67, 14); BoxAt(evade, evadeRole);
        TextAt(M("Panel.EvadeShort"), evade.MidX, y + 9.5f, 6.6f, Skin.Color(evadeRole, "text", ink), true, align: SKTextAlign.Center);
        if (row.CanEvade) column?.RegisterCommand(row.Drone.Id, "evade", evade);
        var hold = SKRect.Create(left + 69, y, 67, 14);
        string holdRole = HasTargets(row) ? "ui.button" : "ui.button.pending"; BoxAt(hold, holdRole);
        TextAt(U("HoldFire"), hold.MidX, y + 9.5f, 6, Skin.Color(holdRole, "text", ink), true, align: SKTextAlign.Center);
        column?.RegisterCommand(row.Drone.Id, "hold", hold);
        y += 17;
        var rules = battle.Rules.Evasion;
        int mind = row.Drone.Consciousness ?? rules?.MaxConsciousness ?? 100;
        string hint = row.CanEvade && rules is not null ? M("EvadeHint", rules.HitPenalty, rules.Cost)
            : M("EvadeLowMind", mind, rules?.MinConsciousness ?? 10);
        Wrapped(hint, 5.6f, 7, sub, true);
        return Math.Max(106, y - top + 8);

        void BoxAt(SKRect r, string role) { if (c is not null) Round(c, r, Box, BoxEdge, role: role); }
        void TextAt(string text, float x, float baseline, float size, SKColor color, bool bold = false, bool italic = false, SKTextAlign align = SKTextAlign.Left)
        { if (c is not null) SmallText(c, text, x, baseline, size, color, bold, italic, align); }
        void Wrapped(string text, float size, float step, SKColor color, bool italic = false)
        {
            foreach (string line in WrapSlim(text, width, size, italic))
            {
                TextAt(line, left, y + (italic ? 4 : 6), size, color, italic: italic);
                y += step;
            }
        }
    }

    internal static IReadOnlyList<string> WrapSlim(string text, float width, float size, bool italic = false)
    {
        using var paint = BattleTypography.TextPaint(size, false);
        if (italic) paint.TextSkewX = -.18f;
        List<string> lines = []; string line = "";
        foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = line.Length == 0 ? word : line + " " + word;
            if (paint.MeasureText(candidate) <= width) { line = candidate; continue; }
            if (line.Length > 0) { lines.Add(line); line = ""; }
            foreach (char ch in word)
            {
                if (line.Length > 0 && paint.MeasureText(line + ch) > width) { lines.Add(line); line = ""; }
                line += ch;
            }
        }
        if (line.Length > 0) lines.Add(line.TrimEnd());
        return lines;
    }
}
