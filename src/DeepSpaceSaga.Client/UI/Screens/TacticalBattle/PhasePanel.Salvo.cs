using DeepSpaceSaga.Motion.TacticalBattle;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed partial class PhasePanel
{
    internal static double QueueChance(TacticalBattleFrame frame, TacticalShotSnapshot shot) =>
        shot.Decoyed && frame.Battle.Units.First(u => u.Id == shot.TargetId).Side != frame.Battle.PlayerSide
            ? shot.ChanceWithoutDecoy ?? shot.Chance : shot.Chance;
    internal static (string Title, string Detail)? AbsorbedText(TacticalShotSnapshot? shot) => shot?.Absorbed == true
        ? (M("Absorbed"), M("AbsorbedBy", Math.Round(shot.Absorption * 100))) : null;
    private int _queueFirst, _queueCount;
    private const int QueueVisibleRows = 10;
    private (string BattleId, int Turn, string? Shooter)? _queueFocus;
    internal SKRect QueueBounds { get; private set; }
    internal bool ScrollQueue(SKPoint point, float delta)
    {
        if (_frame is null || Phase != "Salvo" || !QueueBounds.Contains(point) || !float.IsFinite(delta)) return false;
        if (delta != 0) _queueFirst = (int)PyMath.Clamp(_queueFirst - Math.Sign(delta) * 3, 0, PyMath.Max(0, _queueCount - QueueVisibleRows));
        return true;
    }
    partial void DrawSalvo(SKCanvas c)
    {
        var salvo = _frame.Salvo!;
        var current = salvo.InFlight ? salvo.Flights.FirstOrDefault(f => f.Flying) ?? salvo.Flights.FirstOrDefault(f => !f.Arrived) ?? salvo.Flights.LastOrDefault() : null;
        var shot = current?.Shot ?? PilotCardModel.ResultShot(_frame);
        string Name(string id) => _frame.Battle.Units.FirstOrDefault(u => u.Id == id) is { } u ? PilotCardModel.Name(u) : id;
        Head(c, shot is null ? "—" : Name(shot.ShooterId) + " → " + Name(shot.TargetId), salvo.InFlight ? "FlightTag" : "ResultTag");
        if (salvo.InFlight)
        {
            _queueCount = _frame.Battle.FireQueue.Length;
            var focus = (_frame.Battle.BattleId, _frame.Battle.Turn, current?.Shot.ShooterId);
            if (_queueFocus != focus)
            {
                int activeIndex = Array.FindIndex(_frame.Battle.FireQueue.ToArray(), q => q.ShooterId == current?.Shot.ShooterId);
                if (activeIndex >= 0) _queueFirst = (int)PyMath.Clamp(_queueFirst, PyMath.Max(0, activeIndex - QueueVisibleRows + 1), activeIndex);
                _queueFocus = focus;
            }
            _queueFirst = (int)PyMath.Clamp(_queueFirst, 0, PyMath.Max(0, _queueCount - QueueVisibleRows));
            QueueBounds = R(1045, 133, 1273, 281);
            if (Recording is { } layout) layout.Rects["queue"] = QueueBounds;
            T(c, U("FireQueue"), 1050, 128, 10.5f, Muted);
            c.Save(); c.ClipRect(new(1045, 133, 1273, 281));
            try
            {
                for (int i = _queueFirst; i < PyMath.Min(_queueCount, _queueFirst + QueueVisibleRows); i++)
                {
                    var q = _frame.Battle.FireQueue[i]; float y = 145 + 13.6f * (i - _queueFirst);
                    var flight = salvo.Flights.FirstOrDefault(f => f.Shot.ShooterId == q.ShooterId);
                    bool active = current?.Shot.ShooterId == q.ShooterId;
                    bool enemy = _frame.Battle.Units.First(u => u.Id == q.ShooterId).Side != _frame.Battle.PlayerSide;
                    var color = active ? Yellow : flight is null ? Muted.WithAlpha(150) : enemy ? Orange : Text;
                    string role = active ? "ui.box.salvo.active" : "ui.text";
                    if (active) Round(c, new(1046, y - 11, 1268, y + 4), YellowDark, Yellow, role: role);
                    string line = flight is null ? U("NoTargetRow", Name(q.ShooterId)) : Name(q.ShooterId) + " → " + Name(flight.Shot.TargetId);
                    T(c, (i + 1) + ".", 1050, y, 11, color, active, role: role);
                    T(c, line, 1070, y, 11, color, active, role: role);
                    if (flight is not null)
                    {
                        bool ownTarget = _frame.Battle.Units.First(u => u.Id == flight.Shot.TargetId).Side == _frame.Battle.PlayerSide;
                        bool decoyed = flight.Shot.Decoyed && ownTarget;
                        double chance = QueueChance(_frame, flight.Shot);
                        T(c, Pct(chance), 1264, y, 11, decoyed ? Green : color, active || decoyed, SKTextAlign.Right);
                    }
                }
            }
            finally { c.Restore(); }
            if (_queueCount > QueueVisibleRows)
            {
                float thumb = (float)(146d * QueueVisibleRows / (double)_queueCount);
                float top = 134 + (float)((146 - thumb) * _queueFirst / (double)(_queueCount - QueueVisibleRows));
                Stroke(c, new(1271, 134), new(1271, 280), Track, 2);
                Stroke(c, new(1271, top), new(1271, top + thumb), Muted, 2);
            }
            StandardAction(c, U("FlightTitle"), U("FlightStatus", (current?.Index ?? 0) + 1, salvo.Flights.Length), U("FlightAction"));
        }
        else
        {
            var absorbed = AbsorbedText(shot);
            string result = absorbed?.Title ?? (shot is null ? M("EvadeNoFire") : U(shot.Hit ? "Hit" : "Miss"));
            string resultRole = shot is { Absorbed: false } ? shot.Hit ? "ui.salvo.result.hit" : "ui.salvo.result.miss" : "ui.text";
            T(c, result, 1050, 140, FitSize(result, 22, 214, true), absorbed is not null ? Heat : shot is null ? Purple : Yellow,
                true, role: resultRole);
            if (shot is { Decoyed: true, Absorbed: false })
            {
                T(c, M("DecoyOfTarget"), 1264, 128, 10.5f, Green, align: SKTextAlign.Right);
                T(c, Pct(shot.ChanceWithoutDecoy ?? shot.Chance) + " → " + Pct(shot.Chance),
                    1264, 144, 12, Green, true, SKTextAlign.Right);
            }
            string detail = absorbed?.Detail ?? U("DamageDealt", shot?.Hit == true ? PilotCardModel.Dealt(_frame.Battle, shot.ShooterId, shot.Turn) : 0);
            T(c, detail, 1050, 158, FitSize(detail, 12, 214), Text); Line(c, 1050, 1264, 167);
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
