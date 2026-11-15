using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.Tutorial;

/// <summary>Active signal plus the unit it concerns (target of the {signal} anchor).</summary>
internal readonly record struct TutorialSignalHit(TutorialSignal Signal, string? UnitId);

/// <summary>
/// Battle situations for contextual lessons, computed only from the snapshot the player sees (the frame projection).
/// No RNG, no Flow state and no future FireSteps; collections are scanned in snapshot order.
/// </summary>
internal static class TutorialSignals
{
    private static readonly TutorialSignalHit None = new(TutorialSignal.None, null);

    internal static TutorialSignalHit Find(TacticalBattleSnapshot battle, TutorialSignal signal, string phase)
    {
        bool OwnDrone(TacticalUnitSnapshot u) => u.Side == battle.PlayerSide && u.Kind == TacticalUnitKind.Drone;
        TutorialSignalHit Hit(string? unitId) => new(signal, unitId);
        TutorialSignalHit First(Func<TacticalUnitSnapshot, bool> match) =>
            battle.Units.FirstOrDefault(u => OwnDrone(u) && match(u)) is { } unit ? Hit(unit.Id) : None;

        switch (signal)
        {
            case TutorialSignal.UnderFire:
                if (phase != "Fire") return None;
                foreach (var shot in battle.Incoming)
                    if (battle.Units.Any(u => u.Id == shot.TargetId && OwnDrone(u) && u.State == TacticalUnitState.Air))
                        return Hit(shot.TargetId);
                return None;
            case TutorialSignal.OwnDamaged:
                return First(u => u.State != TacticalUnitState.Destroyed && (u.Hull < u.MaxHull || u.Armor < u.MaxArmor));
            case TutorialSignal.LowConsciousness:
                return battle.Rules.Evasion is { } evasion
                    ? First(u => u.State == TacticalUnitState.Air && u.Consciousness < evasion.MinConsciousness)
                    : None;
            case TutorialSignal.AnomalyPresent:
                if (battle.Anomalies.IsDefaultOrEmpty) return None;
                return Hit(battle.Units.FirstOrDefault(u => OwnDrone(u) && u.InDust)?.Id);
            case TutorialSignal.DroneLost:
                return First(u => u.State is TacticalUnitState.Disabled or TacticalUnitState.Destroyed);
            case TutorialSignal.OutcomeVictory:
            case TutorialSignal.OutcomeDefeat:
            case TutorialSignal.OutcomeOther:
                return OutcomeOf(battle) == signal ? Hit(null) : None;
            default:
                return None;
        }
    }

    private static TutorialSignal OutcomeOf(TacticalBattleSnapshot battle) => battle.Outcome?.Kind switch
    {
        null => TutorialSignal.None,
        TacticalBattleOutcomeCode.Draw or TacticalBattleOutcomeCode.Timeout => TutorialSignal.OutcomeOther,
        TacticalBattleOutcomeCode.A => battle.PlayerSide == "A" ? TutorialSignal.OutcomeVictory : TutorialSignal.OutcomeDefeat,
        TacticalBattleOutcomeCode.B => battle.PlayerSide == "B" ? TutorialSignal.OutcomeVictory : TutorialSignal.OutcomeDefeat,
        _ => TutorialSignal.None,
    };
}
