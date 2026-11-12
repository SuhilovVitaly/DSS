using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.PilotCardColumn;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static class PilotCardSalvoBlock
{
    public static float Height(TacticalSalvoFrame salvo) => salvo.InFlight ? 16 : 30;

    public static void Draw(SKCanvas canvas, TacticalBattleFrame frame, string drone, float left, float right, float top)
    {
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
}
