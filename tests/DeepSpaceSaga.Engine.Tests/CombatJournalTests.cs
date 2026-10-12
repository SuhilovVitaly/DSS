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
        Assert.Equal(2, snapshot.CombatJournal.Count(e => e.Type == CombatEventType.Launch && e.Result == "countermeasure"));
        Assert.Equal(Enumerable.Range(1, snapshot.CombatJournal.Length).Select(i => (long)i),
            snapshot.CombatJournal.Select(e => e.EventId));
        var launch = snapshot.CombatJournal[1];
        var miss = snapshot.CombatJournal[2];
        Assert.Null(launch.Roll);
        Assert.Equal(launch.RatingBreakdown, miss.RatingBreakdown);
        Assert.Equal(launch.ChanceTenths, miss.ChanceTenths);
        Assert.Equal(801, miss.Roll);
        Assert.Equal(CountermeasureTerminationKind.MissedCoastExpired, snapshot.CombatJournal[3].TerminationKind);
        Assert.Null(snapshot.CombatJournal[3].Roll);
        Assert.Contains(snapshot.CombatJournal, e => e.Type == CombatEventType.TorpedoHitRoll && e.Result == "hit");
        Assert.Contains(snapshot.CombatJournal, e => e.Type == CombatEventType.Hit && e.Damage == 150);
        Assert.Contains(snapshot.CombatJournal, e => e.Type == CombatEventType.ModuleHit && e.HitModuleId is not null);
        Assert.Equal(snapshot.CombatJournal.ToArray(), At(engine, 22000).CombatJournal.ToArray());
    }
}
