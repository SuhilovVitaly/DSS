using DeepSpaceSaga.Contracts;
namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private readonly List<CombatJournalEntry> _combatJournal = new();
    private long _combatJournalSequence;
    private void AppendCombatEvent(CombatJournalEntry entry) =>
        _combatJournal.Add(entry with { EventId = checked(++_combatJournalSequence) });
}
