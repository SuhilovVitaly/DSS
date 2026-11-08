using System.Globalization;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

public static class BattleLogFormatter
{
    public static string Format(TacticalBattleLogEvent entry)
    {
        string actor = entry.ActorId ?? "?", target = entry.TargetId ?? "?";
        string chance = entry.Shot is { } shot ? BattleFormat.Probability(shot.Chance) : BattleFormat.Text("—");
        string body = entry.Kind switch
        {
            TacticalBattleLogKind.Started => L("Log.Started"),
            TacticalBattleLogKind.RoleChanged when entry.NoAttackers => F("Log.NoAttackers", actor),
            TacticalBattleLogKind.RoleChanged => F("Log.Role", actor, entry.RoleName ?? entry.RoleId ?? ""),
            TacticalBattleLogKind.FighterBreak => F("Log.FighterBreak", actor),
            TacticalBattleLogKind.DecoyLaunched => F("Log.DecoyLaunched", actor),
            TacticalBattleLogKind.NoTargets => L("Log.NoTargets"),
            TacticalBattleLogKind.FireSkipped => F("Log.FireSkipped", actor, SkipText(entry.SkipReason)),
            TacticalBattleLogKind.ShotMissed => F("Log.ShotMiss", actor, target, chance),
            TacticalBattleLogKind.ShotHit or TacticalBattleLogKind.RearKill => F("Log.ShotHit", actor, target,
                L(entry.Shot?.Rear == true || entry.Kind == TacticalBattleLogKind.RearKill ? "Log.HitRear" : "Log.HitHull"),
                BattleFormat.Number(entry.Amount ?? 0),
                entry.Kind == TacticalBattleLogKind.RearKill ? L("Log.KillSuffix") : "", chance),
            TacticalBattleLogKind.DamageApplied when entry.Damage is { } damage =>
                F("Log.Damage", target, BattleFormat.Signed(-damage.Hull),
                    damage.Armor > 0 ? F("Log.ArmorSuffix", BattleFormat.Signed(-damage.Armor)) : "",
                    BattleFormat.Number(damage.Hits), BattleFormat.Number(damage.HullRemaining)),
            TacticalBattleLogKind.Disabled when entry.Unit is { } unit =>
                F("Log.Disabled", target, BattleFormat.Number(unit.Hull), BattleFormat.Number(unit.MaxHull)),
            TacticalBattleLogKind.Disabled => F("Log.DisabledLegacy", target),
            TacticalBattleLogKind.Destroyed => F(entry.Unit?.Kind == TacticalUnitKind.Carrier ? "Log.CarrierDestroyed" : "Log.Destroyed", target),
            TacticalBattleLogKind.WeaponJammed => F("Log.Jammed", target),
            TacticalBattleLogKind.EngineDamaged => F("Log.EngineDamaged", target,
                entry.Unit?.EngineName ?? BattleFormat.Number((entry.EngineIndex ?? 0) + 1),
                entry.Unit?.EngineFactor is { } factor ? BattleFormat.Probability(factor) : BattleFormat.Text("—")),
            TacticalBattleLogKind.Outcome when entry.Outcome is { } outcome => F("Log.Outcome", string.Join("; ",
                outcome.Reasons.Select(r => F("OutcomeReason." + r.Kind,
                    r.Cap is { } cap ? BattleFormat.Number(cap) : r.CarrierId ?? "?")))),
            TacticalBattleLogKind.Exited => L("Log.Exited"),
            _ => L("Log.Unknown")
        };
        string phase = L(entry.Phase == TacticalBattlePhase.Fire ? "Log.FirePhase" : "Phase." + entry.Phase);
        return F("Log.Line", BattleFormat.Number(entry.Turn), phase, body);
    }

    private static string SkipText(TacticalFireSkipReason? reason) => reason switch
    {
        TacticalFireSkipReason.Evading => L("Skip.Evading"),
        TacticalFireSkipReason.NoTarget => L("Skip.NoTarget"),
        TacticalFireSkipReason.Withheld => L("Skip.Withheld"),
        TacticalFireSkipReason.ShooterDestroyed => L("Skip.ShooterDestroyed"),
        TacticalFireSkipReason.ShooterDisabled => L("Skip.ShooterDisabled"),
        TacticalFireSkipReason.WeaponJammed => L("Skip.WeaponJammed"),
        TacticalFireSkipReason.TargetUnavailable => L("Skip.TargetUnavailable"),
        TacticalFireSkipReason.BattleEnded => L("Skip.BattleEnded"),
        _ => L("Log.Unknown")
    };

    private static string L(string key) => BattleFormat.Localize(key);
    private static string F(string key, params object[] args) => BattleFormat.Text(string.Format(CultureInfo.InvariantCulture, L(key), args));
}
