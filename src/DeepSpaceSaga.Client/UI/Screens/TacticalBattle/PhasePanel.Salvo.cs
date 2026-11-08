using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed partial class PhasePanel
{
    partial void DrawSalvo(SKCanvas c)
    {
        var salvo = _frame.Salvo!;
        var current = salvo.InFlight ? salvo.Flights.FirstOrDefault(f => f.Flying) ?? salvo.Flights.FirstOrDefault(f => !f.Arrived) ?? salvo.Flights.LastOrDefault() : null;
        var shot = current?.Shot ?? PilotCardModel.ResultShot(_frame);
        string Name(string id) => _frame.Battle.Units.FirstOrDefault(u => u.Id == id) is { } u ? PilotCardModel.Name(u) : id;
        Head(c, shot is null ? "—" : Name(shot.ShooterId) + " → " + Name(shot.TargetId), salvo.InFlight ? "FlightTag" : "ResultTag");
        if (salvo.InFlight)
        {
            T(c, U("FireQueue"), 1050, 128, 10.5f, Muted);
            for (int i = 0; i < _frame.Battle.FireQueue.Length; i++)
            {
                var q = _frame.Battle.FireQueue[i]; float y = 145 + 13.6f * i;
                var flight = salvo.Flights.FirstOrDefault(f => f.Shot.ShooterId == q.ShooterId);
                bool active = current?.Shot.ShooterId == q.ShooterId;
                bool enemy = _frame.Battle.Units.First(u => u.Id == q.ShooterId).Side != _frame.Battle.PlayerSide;
                var color = active ? Yellow : flight is null ? Muted.WithAlpha(150) : enemy ? Orange : Text;
                if (active) Round(c, new(1046, y - 11, 1268, y + 4), YellowDark, Yellow);
                string line = flight is null ? U("NoTargetRow", Name(q.ShooterId)) : Name(q.ShooterId) + " → " + Name(flight.Shot.TargetId);
                T(c, (i + 1) + ".", 1050, y, 11, color, active); T(c, line, 1070, y, 11, color, active);
                if (flight is not null) T(c, Pct(flight.Shot.Chance), 1264, y, 11, color, active, SKTextAlign.Right);
            }
            StandardAction(c, U("FlightTitle"), U("FlightStatus", (current?.Index ?? 0) + 1, salvo.Flights.Length), U("FlightAction"));
        }
        else
        {
            T(c, U(shot?.Hit == true ? "Hit" : "Miss"), 1050, 140, 22, Yellow, true);
            T(c, U("DamageDealt", shot?.Hit == true ? PilotCardModel.Dealt(_frame.Battle, shot.ShooterId, shot.Turn) : 0), 1050, 158, 12, Text); Line(c, 1050, 1264, 167);
            var unit = _frame.Battle.Units.FirstOrDefault(u => u.Id == shot?.ShooterId);
            var before = _frame.Battle.BeforeSalvoUnits.FirstOrDefault(u => u.Id == unit?.Id);
            if (unit is not null && before is not null)
            {
                T(c, U("DamageTitle", Genitive(Name(unit.Id))), 1050, 185, 12, Text, true);
                T(c, U("ArmorChange", before.Armor, unit.Armor), 1050, 202, 12, Text);
                double armor = unit.MaxArmor > 0 ? unit.Armor / (double)unit.MaxArmor : 0;
                Bar(c, 1050, 209, 170, 7, armor, Purple); T(c, Pct(armor), 1264, 216, 11, Text, align: SKTextAlign.Right);
                T(c, before.Hull == unit.Hull ? U("HullNoDamage", unit.Hull, unit.MaxHull) : U("HullChange", before.Hull, unit.Hull), 1050, 234, 12, Text);
                BoxText(c, new(1048, 242, 1266, 262), PilotCardModel.State(unit), true);
            }
            float y = _db + 28;
            T(c, U("SalvoComplete"), 1050, y + 20, 14, Text, true);
            string Count(int n, string key) => Plural(n, U(key + "One"), U(key + "Few"), U(key + "Many"));
            T(c, Count(salvo.Flights.Length, "Shot") + " · " + Count(salvo.Hits, "Hit"), 1050, y + 41, 11.5f, Text);
            ButtonAt(c, "replay", new(1048, y + 56, 1112, _db + 112), U("Replay"));
            ButtonAt(c, "primary", new(1118, y + 56, 1266, _db + 112), U(_frame.Battle.Phase == global::DeepSpaceSaga.Contracts.TacticalBattlePhase.Over ? "ToOutcome" : "ToMove"), true);
        }
    }
}
