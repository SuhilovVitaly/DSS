using DeepSpaceSaga.Contracts;
namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private readonly List<CombatJournalEntry> _combatJournal = new();
    private long _combatJournalSequence;
    private void AppendCombatEvent(CombatJournalEntry entry)
    {
        var committed = ProjectPointCombatFact(entry) with { EventId = checked(++_combatJournalSequence) };
        _combatJournal.Add(committed);
        ObserveEncounterFact(committed);
        ObserveCombatReportFact(committed);
    }

    private CombatJournalEntry ProjectPointCombatFact(CombatJournalEntry entry) => !UsesPointEncounters ? entry : entry with
    {
        ChanceTenths = null,
        Roll = null,
        RatingBreakdown = null,
        TorpedoOperator = null,
        LaunchMode = null,
        Accuracy = null,
        TargetManeuverability = null,
        Attack = null,
        Defense = null,
        TerminationKind = null,
        LauncherModuleId = entry.ActorObjectId == PlayerShipObjectId ? entry.LauncherModuleId : null,
        Damage = entry.TargetObjectId == PlayerShipObjectId ? entry.Damage : null,
        HitModuleId = entry.TargetObjectId == PlayerShipObjectId ? entry.HitModuleId : null,
        ArmorBefore = entry.TargetObjectId == PlayerShipObjectId ? entry.ArmorBefore : null,
        ArmorAfter = entry.TargetObjectId == PlayerShipObjectId ? entry.ArmorAfter : null,
        StructureBefore = entry.TargetObjectId == PlayerShipObjectId ? entry.StructureBefore : null,
        StructureAfter = entry.TargetObjectId == PlayerShipObjectId ? entry.StructureAfter : null,
        DestroyReason = entry.TargetObjectId == PlayerShipObjectId ? entry.DestroyReason : null
    };

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
