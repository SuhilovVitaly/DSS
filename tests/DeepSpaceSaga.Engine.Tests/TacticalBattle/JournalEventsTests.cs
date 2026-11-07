using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.TacticalBattle;
using Availability = DeepSpaceSaga.Engine.TacticalBattle.Content.TacticalBattleAvailability;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class JournalEventsTests
{
    [Fact]
    public void RetainedJournalCarriesActualShotsDamageAndSystemChangesInResolutionOrder()
    {
        using var engine = Create();
        var frame = engine.StartTacticalBattle(new("start-journal", 1)).State!;
        var remembered = new List<TacticalBattleLogEvent>();
        int shotsSeen = 0, damageSeen = 0, unitChangesSeen = 0;
        for (int n = 0; n < 100 && frame.Phase != TacticalBattlePhase.Over; n++)
        {
            var before = frame;
            var response = frame.Phase == TacticalBattlePhase.Move
                ? engine.MoveTacticalBattle(new("m-" + n, frame.BattleId, frame.Revision))
                : engine.FireTacticalBattle(new("f-" + n, frame.BattleId, frame.Revision,
                    frame.Targets.Select(g => new TacticalFireTarget(g.DroneId, g.DefaultTargetId)).ToImmutableArray()));
            Assert.True(response.Accepted);
            frame = response.State!;
            Assert.Equal(before.Log.AsEnumerable(), frame.Log.Take(before.Log.Length));
            Assert.Equal(response.NewEvents.AsEnumerable(), frame.Log.Skip(before.Log.Length));
            if (frame.Shots.IsEmpty) continue;

            var fired = response.NewEvents.Where(e => e.Shot is not null).ToArray();
            Assert.Equal(frame.Shots.Select(s => (s.ShooterId, s.TargetId, s.Chance, s.Rear, s.Damage)),
                fired.Select(e => (e.ActorId!, e.TargetId!, e.Shot!.Chance, e.Shot.Rear, e.Amount!.Value)));
            Assert.All(fired, e => Assert.Equal(TacticalBattlePhase.Fire, e.Phase));
            shotsSeen += fired.Length;
            var damages = response.NewEvents.Where(e => e.Kind == TacticalBattleLogKind.DamageApplied).ToArray();
            Assert.Equal(frame.Shots.Where(s => s.Hit).Select(s => s.TargetId).Distinct(), damages.Select(e => e.TargetId!));
            foreach (var row in damages)
            {
                var original = frame.BeforeSalvoUnits.Single(u => u.Id == row.TargetId);
                var now = frame.Units.Single(u => u.Id == row.TargetId);
                Assert.Equal(new TacticalLogDamage(original.Hull - now.Hull, original.Armor - now.Armor,
                    frame.Shots.Count(s => s.TargetId == row.TargetId && s.Hit), now.Hull), row.Damage);
                Assert.True(row.Sequence > fired[^1].Sequence);
            }
            damageSeen += damages.Length;
            foreach (var evt in response.NewEvents.Where(e => e.Unit is not null))
            {
                var unit = frame.Units.Single(u => u.Id == evt.TargetId);
                Assert.Equal(unit.Kind, evt.Unit!.Kind);
                Assert.Equal(unit.MaxHull, evt.Unit.MaxHull);
                Assert.Equal(unit.Hull, evt.Unit.Hull);
                if (evt.EngineIndex is { } index)
                {
                    Assert.Equal(unit.Engines[index].Name, evt.Unit.EngineName);
                    Assert.Equal(unit.Engines[index].Factor, evt.Unit.EngineFactor);
                }
                Assert.True(evt.Sequence > damages[^1].Sequence);
                unitChangesSeen++;
            }
            if (remembered.Count == 0) remembered.AddRange(fired);
        }
        Assert.Equal(TacticalBattlePhase.Over, frame.Phase);
        Assert.True(shotsSeen > 0 && damageSeen > 0 && unitChangesSeen > 0);
        Assert.All(remembered, e => Assert.Contains(e, frame.Log));
        var outcome = Assert.Single(frame.Log.Where(e => e.Kind == TacticalBattleLogKind.Outcome));
        Assert.Equal(TacticalBattlePhase.Fire, outcome.Phase);
        Assert.Equal(frame.Outcome!.Kind, outcome.Outcome!.Kind);
        Assert.Equal(frame.Outcome.Reasons.AsEnumerable(), outcome.Outcome.Reasons);
    }

    [Fact]
    public void OutcomeJournalKeepsMovePhaseWhenVictoryIsCheckedAfterMovement()
    {
        using var engine = Create();
        var frame = engine.StartTacticalBattle(new("start-terminal-move", 1)).State!;
        foreach (var drone in engine.TacticalBattleSessionForTests!.Battle.Drones.Where(d => d.Side == "B"))
            drone.Status = TacticalDroneStatus.Disabled;
        var response = engine.MoveTacticalBattle(new("finish-move", frame.BattleId, frame.Revision));
        Assert.Equal(TacticalBattlePhase.Over, response.State!.Phase);
        var outcome = Assert.Single(response.NewEvents.Where(e => e.Kind == TacticalBattleLogKind.Outcome));
        Assert.Equal(TacticalBattlePhase.Move, outcome.Phase);
        Assert.Equal(TacticalBattleOutcomeCode.A, outcome.Outcome!.Kind);
    }

    private static SimulationEngine Create() =>
        new(GameDataRegistry.Create([], [], [], [], tacticalBattle: Availability.Available(AssemblyTests.Catalog())),
            clock: new SimulationClock(SimulationSpeed.Speed0));
}
