using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.PilotCardColumn;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static class PilotCardSalvoBlock
{
    public static float Height(TacticalSalvoFrame salvo) => salvo.InFlight ? 16 : 30;
    internal static float SalvoHeight(bool inFlight) => inFlight ? 51 : 71;

    public static void Draw(SKCanvas canvas, TacticalBattleFrame frame, string drone, float left, float right, float top)
    {
        if (TacticalBattleSkinRenderScope.SlimPilots)
        {
            DrawSlim(canvas, frame, drone, left, right, top - 5);
            return;
        }
        var salvo = frame.Salvo!;
        var shot = salvo.InFlight
            ? salvo.Flights.FirstOrDefault(flight => flight.Shot.ShooterId == drone)?.Shot
            : PilotCardModel.ResultShot(frame);
        if (shot is null)
        {
            T(canvas, U("NoTarget"), left, top + 10, 9.5f, Muted);
            return;
        }

        var target = frame.Battle.Units.FirstOrDefault(unit => unit.Id == shot.TargetId);
        string name = target is null ? shot.TargetId : PilotCardModel.Name(target);
        string aim = "→ " + name + " · " + Pct(shot.Chance);
        var (caption, tone) = salvo.InFlight ? (U("Firing"), Yellow) :
            shot.Absorbed ? (M("AbsorbedChip"), Heat) :
            shot.TargetEvading ? (M("EvadeChipResult"), Purple) :
            shot.Hit ? (U("HitChip"), Yellow) : (U("MissChip"), Muted);
        FittedText(canvas, aim, left, top + 10, 9.5f, Math.Min(120, right - left - Measure(caption, 9.5f, true) - 5), Text, true);
        T(canvas, caption, right, top + 10, 9.5f, tone, true, SKTextAlign.Right);
        if (salvo.InFlight) return;

        int dealt = shot.Hit ? PilotCardModel.Dealt(frame.Battle, drone, shot.Turn) : 0;
        T(canvas, M("Card.Dealt", dealt), left, top + 24, 8.5f, shot.Hit ? Orange : Muted);
        if (target is not null)
        {
            string defense = M("Card.TargetAfter", name, target.Armor, target.MaxArmor, target.Hull, target.MaxHull);
            FittedText(canvas, defense, right, top + 24, 8.5f, 110, Muted, align: SKTextAlign.Right);
        }
    }

    private static void DrawSlim(SKCanvas c, TacticalBattleFrame frame, string drone, float left, float right, float top)
    {
        var salvo = frame.Salvo!;
        var flight = salvo.Flights.FirstOrDefault(f => f.Shot.ShooterId == drone);
        var shot = salvo.InFlight ? flight?.Shot : PilotCardModel.ResultShot(frame);
        var ink = SKColor.Parse("#E4ECF4"); var sub = SKColor.Parse("#9AAABB"); var value = SKColor.Parse("#FFE27A");
        if (shot is null) { TextAt(U("NoTarget"), 12, 7, sub); return; }
        var target = frame.Battle.Units.FirstOrDefault(u => u.Id == shot.TargetId);
        string name = target is null ? shot.TargetId : PilotCardModel.Name(target);
        string chance = Pct(shot.Chance);
        if (salvo.InFlight)
        {
            TextAt(M("Panel.FlightTitle"), 16, 11, value, true);
            string number = $"{Array.FindIndex(frame.Battle.FireQueue.ToArray(), q => q.ShooterId == drone) + 1}/{frame.Battle.FireQueue.Length}";
            string prefix = M("Panel.FlightQueue", "");
            SmallText(c, number, right, top + 15, 6.8f, ink, true, align: SKTextAlign.Right);
            SmallText(c, prefix, right - Measure(number, 6.8f, true), top + 15, 6.8f, sub, align: SKTextAlign.Right);
            TextAt(M("Panel.FlightTarget", name, chance), 27, 7, ink);
            var track = SKRect.Create(left, top + 31, right - left, 4);
            using var paint = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#2B241A") };
            c.DrawRect(track, paint);
            float end = left + (right - left) * (float)(flight?.Progress ?? 0);
            paint.Color = SKColor.Parse("#C79A3C"); c.DrawRect(new(left, track.Top, end, track.Bottom), paint);
            Polygon(c, [new(end, track.Bottom + 2), new(end - 2.4f, track.Top - 2), new(end + 2.4f, track.Top - 2)], value);
            if (Recording is { } layout) layout.Rects["pilot-flight-progress"] = new(left, track.Top, end, track.Bottom);
            TextAt(M("Panel.FlightHint"), 45, 5.6f, sub, italic: true);
            return;
        }

        int dealt = shot.Hit ? PilotCardModel.Dealt(frame.Battle, drone, shot.Turn) : 0;
        TextAt(M("Panel.ResultLine", name, chance, dealt), 12, 7, ink);
        string caption = shot.Absorbed ? M("AbsorbedChip").ToUpperInvariant() : shot.TargetEvading ? M("EvadeChipResult").ToUpperInvariant() : U(shot.Hit ? "Hit" : "Miss");
        var tone = shot.Absorbed || shot.TargetEvading ? sub : shot.Hit ? SKColor.Parse("#FF9A6A") : SKColor.Parse("#C8D4EC");
        TextAt(caption, 31, 17, tone, true);
        Stroke(c, new(left, top + 37), new(right, top + 37), SKColor.Parse("#5A4E3E"), .6f);
        if (target is not null)
            TextAt(M("Card.TargetAfter", name, target.Armor, target.MaxArmor, target.Hull, target.MaxHull), 47, 6.4f, ink);
        if (shot.Decoyed)
            TextAt(M("DecoyOfTarget") + ": " + Pct(shot.ChanceWithoutDecoy ?? shot.Chance) + " → " + chance, 56, 6.4f, SKColor.Parse("#A8E08A"), true);
        var unit = frame.Battle.Units.FirstOrDefault(u => u.Id == drone);
        var before = frame.Battle.BeforeSalvoUnits.FirstOrDefault(u => u.Id == drone);
        if (unit is not null && before is not null)
            TextAt(M("Panel.SelfState", PilotCardModel.Name(unit), before.Armor, unit.Armor, unit.Hull, unit.MaxHull), 65, 6.4f, sub);

        void TextAt(string text, float y, float size, SKColor color, bool bold = false, bool italic = false) =>
            SmallText(c, BuildCardModel.Ellipsize(text, right - left, size, bold), left, top + y, size, color, bold, italic);
    }
}
