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

}
