using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed partial class PhasePanel
{
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
}
