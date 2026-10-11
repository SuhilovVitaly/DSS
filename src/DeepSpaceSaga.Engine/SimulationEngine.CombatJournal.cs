using DeepSpaceSaga.Contracts;
namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private readonly List<CombatJournalEntry> _combatJournal = new();
    private long _combatJournalSequence;
    private void AppendCombatEvent(CombatJournalEntry entry) =>
        _combatJournal.Add(entry with { EventId = checked(++_combatJournalSequence) });

    private void AppendCountermeasureEvent(SpaceObjectRuntime projectile, double time, CombatEventType type,
        string result, int? roll = null, CountermeasureTerminationKind? termination = null)
    {
        var flight = projectile.InitialMotion.Countermeasure!;
        var point = CombatPath(projectile).Position(time);
        AppendCombatEvent(new(0, time, type, flight.OwnerObjectId, flight.TargetTorpedoId,
            projectile.InitialMotion.ObjectId, point.X, point.Y, flight.FrozenChanceTenths, roll,
            flight.RatingBreakdown, Damage: 0, Result: result, LauncherModuleId: flight.LauncherModuleId,
            LaunchMode: flight.Mode, Accuracy: flight.CapturedAccuracy ?? flight.RatingBreakdown.DefenseOperator.EffectiveRating,
            TargetManeuverability: flight.CapturedTargetManeuverability ?? flight.RatingBreakdown.TorpedoRating,
            TerminationKind: termination));
    }
}
