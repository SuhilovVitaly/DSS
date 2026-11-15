using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.Tests.TacticalBattle;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class Ep0029Us0004Tk0005Tests
{
    [Fact]
    public void BothEmptyEndInStalemate()
    {
        var (battle, a, b) = SalvoTests.Arena(chance: 100);
        a.AmmoCurrent = 0;
        b.AmmoCurrent = 0;
        long calls = battle.Random.Calls;

        TacticalBattleSalvo.Fire(battle);
        battle.CheckVictory();

        Assert.NotNull(battle.Result);
        Assert.Equal(TacticalOutcomeKind.Draw, battle.Result!.Kind);
        Assert.Contains(battle.Result.Reasons, r => r is TacticalNoCombatCapability);
        Assert.Equal(calls, battle.Random.Calls); // stalemate consumes no additional RNG
    }

    [Fact]
    public void OneEmptyStillTakesDamage()
    {
        var (battle, a, b) = SalvoTests.Arena(chance: 100);
        a.AmmoCurrent = 0; // A empty, B still armed
        int hullBefore = a.Hull;

        TacticalBattleSalvo.Fire(battle, (d, _) => new(d == b ? a.Id : null, false));
        battle.CheckVictory();

        Assert.Null(battle.Result); // one empty side does not stop the other from firing
        Assert.True(a.Hull < hullBefore);
    }

    [Fact]
    public void DustOrDistanceIsNotExhaustion()
    {
        var (battle, a, b) = SalvoTests.Arena(chance: 100);
        a.AmmoCurrent = 1;
        b.AmmoCurrent = 1;
        b.Pose = new(100000, 0, 0); // distance, not exhaustion

        TacticalBattleSalvo.Fire(battle);
        battle.CheckVictory();

        Assert.Null(battle.Result); // distance never reads as NoCombatCapability
    }

    [Fact]
    public void InteractiveTurnCapEnds()
    {
        var baseCatalog = AssemblyTests.Catalog();
        var catalog = baseCatalog with { Rules = baseCatalog.Rules with { SeriesTurnCap = 3 } };
        var session = TacticalBattleSession.TryCreate(catalog, new StartTacticalBattleRequest("cmd", 1, "initial"), out _)!;

        // No side can reach a shot, so the battle survives to the series turn cap instead of a normal victory.
        foreach (var drone in session.Battle.Drones)
            drone.Pose = new(drone.Pose.X + (drone.Side == "A" ? -100000 : 100000), drone.Pose.Y, drone.Pose.Course);

        for (int i = 0; i < 20 && session.Phase != TacticalBattlePhase.Over; i++)
        {
            if (session.Phase == TacticalBattlePhase.Move)
                session.Move(new MoveTacticalBattleRequest($"m{i}", session.Id, session.Revision, []));
            else if (session.Phase == TacticalBattlePhase.Fire)
                session.Fire(new FireTacticalBattleRequest($"f{i}", session.Id, session.Revision, []));
        }

        Assert.Equal(TacticalBattlePhase.Over, session.Phase);
        var outcome = TacticalBattleSnapshotBuilder.Outcome(session.Battle.Result!)!;
        Assert.Equal(TacticalOutcomeReasonCode.Timeout, Assert.Single(outcome.Reasons).Kind);
    }
}
