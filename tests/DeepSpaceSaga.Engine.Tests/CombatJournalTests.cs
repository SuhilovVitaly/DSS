using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
using static DeepSpaceSaga.Engine.Tests.AutomaticDefenseTests;
namespace DeepSpaceSaga.Engine.Tests;

public class CombatJournalTests
{
    [Fact]
    public void Each_transition_logged_once_in_deterministic_order()
    {
        using var engine = Create(DefenseScenario());
        engine.ReceiveCommand(Fire("fire")); At(engine, 0);
        var snapshot = At(engine, 21000);
        Assert.Equal(new[] { CombatEventType.Launch, CombatEventType.Launch, CombatEventType.Miss, CombatEventType.Expired,
            CombatEventType.Hit, CombatEventType.ModuleHit, CombatEventType.ModuleDestroyed }, snapshot.CombatJournal.Select(e => e.Type));
        Assert.Equal(new long[] { 1, 2, 3, 4, 5, 6, 7 }, snapshot.CombatJournal.Select(e => e.EventId));
        var launch = snapshot.CombatJournal[1];
        var miss = snapshot.CombatJournal[2];
        Assert.Null(launch.Roll);
        Assert.Equal(launch.RatingBreakdown, miss.RatingBreakdown);
        Assert.Equal(launch.ChanceTenths, miss.ChanceTenths);
        Assert.Equal(801, miss.Roll);
        Assert.Equal(CountermeasureTerminationKind.MissedCoastExpired, snapshot.CombatJournal[3].TerminationKind);
        Assert.Null(snapshot.CombatJournal[3].Roll);
        Assert.Equal(150, snapshot.CombatJournal[4].Damage);
        Assert.NotNull(snapshot.CombatJournal[5].HitModuleId);
        Assert.Equal(0, snapshot.CombatJournal[5].StructureAfter);
        Assert.Equal(snapshot.CombatJournal.ToArray(), At(engine, 22000).CombatJournal.ToArray());
    }
}
