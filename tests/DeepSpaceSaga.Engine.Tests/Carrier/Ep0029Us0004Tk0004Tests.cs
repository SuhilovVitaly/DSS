using System.Collections.Immutable;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.Tests.TacticalBattle;
using DeepSpaceSaga.Motion.TacticalBattle;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class Ep0029Us0004Tk0004Tests
{
    [Fact]
    public void HitMissAbsorptionEachConsumeOne()
    {
        // Hit.
        var (hit, a, b) = SalvoTests.Arena(chance: 100);
        int hitBefore = a.AmmoCurrent;
        TacticalBattleSalvo.Fire(hit, (d, _) => new(d == a ? b.Id : null, false));
        Assert.Equal(hitBefore - 1, a.AmmoCurrent);

        // Miss.
        var (miss, a2, b2) = SalvoTests.Arena(chance: 0);
        int missBefore = a2.AmmoCurrent;
        TacticalBattleSalvo.Fire(miss, (d, _) => new(d == a2 ? b2.Id : null, false));
        Assert.Equal(missBefore - 1, a2.AmmoCurrent);

        // Absorption — the decrement happens before the absorption/hit RNG, so an absorbed shot still spends one.
        var (absorb, a3, b3) = AbsorbArena();
        int absorbBefore = a3.AmmoCurrent;
        var shots = TacticalBattleSalvo.Fire(absorb, (d, _) => new(d == a3 ? b3.Id : null, false)).Shots;
        Assert.Single(shots);
        Assert.True(shots[0].Absorbed);
        Assert.Equal(absorbBefore - 1, a3.AmmoCurrent);
    }

    [Fact]
    public void KilledBeforeInitiativeConsumesZero()
    {
        var (battle, a, b) = SalvoTests.Arena(chance: 100);
        a.Status = TacticalDroneStatus.Destroyed; // killed before its fire slot
        int before = a.AmmoCurrent;
        TacticalBattleSalvo.Fire(battle, (d, _) => new(d == a ? b.Id : null, false));
        Assert.Equal(before, a.AmmoCurrent);
    }

    [Fact]
    public void DeadTargetAndJamConsumeZero()
    {
        // Jammed shooter skips (no reassignment), consumes nothing.
        var (jam, a, _) = SalvoTests.Arena(chance: 100);
        a.Jammed = true;
        int jamBefore = a.AmmoCurrent;
        TacticalBattleSalvo.Fire(jam);
        Assert.Equal(jamBefore, a.AmmoCurrent);

        // A destroyed target is no candidate → the shooter is withheld and consumes nothing.
        var (dead, a2, b2) = SalvoTests.Arena(chance: 100);
        b2.Status = TacticalDroneStatus.Destroyed;
        int deadBefore = a2.AmmoCurrent;
        TacticalBattleSalvo.Fire(dead);
        Assert.Equal(deadBefore, a2.AmmoCurrent);
    }

    [Fact]
    public void InvalidMixedPacketIsAtomic()
    {
        var (battle, a, b) = SalvoTests.Arena(chance: 100);
        int before = a.AmmoCurrent;
        // First plan valid, second invalid — the whole packet is rejected before any draw or spend.
        Assert.Throws<ArgumentException>(() => TacticalBattleSalvo.Fire(battle,
            (d, _) => new(d == a ? b.Id : "missing", false)));
        Assert.Equal(before, a.AmmoCurrent);
    }

    private static (Battle Battle, TacticalDrone A, TacticalDrone B) AbsorbArena()
    {
        var (source, a, b) = SalvoTests.Arena(chance: 100);
        var battle = new Battle(source.Catalog, source.ScenarioId, source.Seed,
            source.Drones, source.Carriers, source.Tactics, source.Random)
        {
            Anomalies =
            [
                new TacticalAnomaly("zone", TacticalAnomalyKind.Heat,
                    [new TacticalClump(5, 0, 10000, 100, 100, 0)]),
            ],
        };
        battle.InitializeFireOrder();
        return (battle, a, b);
    }
}
