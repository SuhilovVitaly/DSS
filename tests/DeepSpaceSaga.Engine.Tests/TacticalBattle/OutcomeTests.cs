using DeepSpaceSaga.Engine.TacticalBattle;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class OutcomeTests
{
    [Fact]
    public void CarrierLossPrecedesNoDronesAndCompletedResultIsIdempotent()
    {
        var battle = Battle.Create(AssemblyTests.Catalog(), "initial", 1);
        battle.Carrier("A").Hull = 0;
        foreach (var d in battle.Drones.Where(d => d.Side == "A")) d.Status = TacticalDroneStatus.Disabled;
        var result = battle.CheckVictory()!;
        Assert.Equal(TacticalOutcomeKind.B, result.Kind);
        Assert.Equal("Скиталец", Assert.IsType<TacticalCarrierDestroyed>(Assert.Single(result.Reasons)).CarrierId);
        Assert.Equal(new[] { 4, 0 }, result.Losses);
        Assert.Same(result, battle.CheckVictory());
        Assert.Same(result, battle.Run(StraightOrders));
        Assert.Equal(42, battle.Random.Calls);
        Assert.Throws<InvalidOperationException>(() => battle.Step(StraightOrders));
        Assert.Equal(0, battle.Turn);
    }

    [Fact]
    public void BothSidesLosingProducesOrderedStructuralReasonsAndLossesIncludeDisabled()
    {
        var battle = Battle.Create(AssemblyTests.Catalog(), "initial", 1);
        battle.Carrier("A").Hull = 0;
        foreach (var d in battle.Drones.Where(d => d.Side == "B"))
            d.Status = d.Id == "Таг-1" ? TacticalDroneStatus.Destroyed : TacticalDroneStatus.Disabled;
        var result = battle.CheckVictory()!;
        Assert.Equal(TacticalOutcomeKind.Draw, result.Kind);
        Assert.IsType<TacticalCarrierDestroyed>(result.Reasons[0]);
        Assert.Equal("Логово", Assert.IsType<TacticalNoDrones>(result.Reasons[1]).CarrierId);
        Assert.Equal(new[] { 0, 6 }, result.Losses);
        Assert.Equal(TacticalTurnPhase.Complete, battle.Phase);
    }

    [Theory]
    [InlineData(null, 2)]
    [InlineData(0, 2)]
    [InlineData(3, 3)]
    public void AutomatedRunUsesExplicitCapOrSeriesDefaultAndPreservesExactTurns(int? cap, int expected)
    {
        var battle = DistantBattle();
        var result = battle.Run(StraightOrders, cap);
        Assert.Equal(TacticalOutcomeKind.Timeout, result.Kind);
        Assert.Equal(expected, result.Turns);
        Assert.Equal(expected, Assert.IsType<TacticalTimeout>(Assert.Single(result.Reasons)).Cap);
        Assert.Equal(42, battle.Random.Calls);
        Assert.Equal(0, result.Statistics.Shots);
    }

    [Fact]
    public void ManualCycleHasNoSeriesLimitAndGuardsPhaseOrderBeforeSideEffects()
    {
        var battle = DistantBattle();
        Assert.Throws<InvalidOperationException>(() => battle.Movement());
        Assert.Throws<InvalidOperationException>(() => battle.Firing());
        Assert.Throws<ArgumentNullException>(() => battle.Planning(null!));
        Assert.Equal(0, battle.Turn);
        for (int i = 1; i <= 5; i++)
        {
            battle.Planning(StraightOrders);
            Assert.Equal(i, battle.Turn);
            Assert.Null(battle.LastSalvo);
            Assert.Empty(battle.LastMovement);
            Assert.Equal(TacticalTurnPhase.Movement, battle.Phase);
            Assert.Throws<InvalidOperationException>(() => battle.Planning(StraightOrders));
            battle.Movement();
            Assert.Equal(TacticalTurnPhase.Firing, battle.Phase);
            Assert.Equal(10, battle.LastMovement.Length);
            battle.Firing((_, _) => new(null, false));
            Assert.Equal(TacticalTurnPhase.Planning, battle.Phase);
            Assert.NotNull(battle.LastSalvo);
            Assert.Null(battle.Result);
        }
        Assert.Equal(42, battle.Random.Calls);
        Assert.Throws<ArgumentOutOfRangeException>(() => battle.Run(StraightOrders, -1));
        Assert.Equal(5, battle.Turn);
    }

    [Fact]
    public void MovementVictoryStopsCycleBeforeAnySalvo()
    {
        var battle = DistantBattle();
        battle.Carrier("B").Hull = 0;
        var result = battle.Step(StraightOrders)!;
        Assert.Equal(TacticalOutcomeKind.A, result.Kind);
        Assert.Equal(1, result.Turns);
        Assert.Null(battle.LastSalvo);
        Assert.Equal(0, result.Statistics.Shots);
        Assert.Equal(42, battle.Random.Calls);
    }

    [Fact]
    public void ResultCapturesActualSalvoStatisticsAndRoleChangesImmutably()
    {
        var c = AssemblyTests.Catalog();
        c = c with
        {
            Rules = c.Rules with { Hit = c.Rules.Hit with { MinChance = 100, MaxChance = 100 } },
            Weapons = new(c.Weapons.Entries.Select(e => (e.Id, e.Value with { DamageMin = 20, DamageMax = 20, RearKillChance = 0 })))
        };
        var battle = Battle.Create(c, "initial", 1);
        foreach (var d in battle.Drones) { d.Status = TacticalDroneStatus.Disabled; d.Armor = 0; }
        var a = battle.Drones[0];
        var b = battle.Drones[4];
        a.Status = b.Status = TacticalDroneStatus.Air;
        a.Pose = new(0, 0, 0);
        b.Pose = new(10, 0, 180);
        battle.Carrier("A").Pose = new(-10000, 0, 0);
        battle.Carrier("B").Pose = new(10000, 0, 0);
        TacticalBattleSalvo.Fire(battle);
        battle.RoleChanges = 3; // manually assembled counter state, independent of the later AI policy
        b.Status = TacticalDroneStatus.Disabled;
        var result = battle.CheckVictory()!;
        Assert.Equal(TacticalOutcomeKind.A, result.Kind);
        Assert.Equal(2, result.Statistics.Shots);
        Assert.Equal(2, result.Statistics.Hits);
        Assert.Equal(40, result.Statistics.Hull);
        Assert.Equal(0, result.Statistics.CarrierHull);
        Assert.Equal(0, result.Statistics.RearHits);
        Assert.Equal(0, result.Statistics.RearKills);
        Assert.Equal(0, result.Statistics.Systems);
        Assert.Equal(3, result.Statistics.RoleChanges);
        Assert.Equal(46, result.RandomCalls);
        Assert.Equal(new[] { 3, 6 }, result.Losses);
        Assert.Equal(new[] { a.Weapon.Id, b.Weapon.Id }, result.Statistics.Weapons.Select(w => w.WeaponId));
        Assert.All(result.Statistics.Weapons, w => { Assert.Equal(1, w.Shots); Assert.Equal(1, w.Hits); Assert.Equal(20, w.Hull); });
        battle.Statistics.Shots = 999;
        battle.Statistics.ForWeapon(a.Weapon.Id).Hull = 999;
        battle.RoleChanges = 999;
        battle.Random.Random();
        Assert.Equal(2, result.Statistics.Shots);
        Assert.Equal(20, result.Statistics.Weapons[0].Hull);
        Assert.Equal(3, result.Statistics.RoleChanges);
        Assert.Equal(46, result.RandomCalls);
    }

    private static Battle DistantBattle()
    {
        var c = AssemblyTests.Catalog();
        c = c with { Rules = c.Rules with { SeriesTurnCap = 2 } };
        var battle = Battle.Create(c, "initial", 1);
        foreach (var d in battle.Drones.Where(d => d.Side == "B")) d.Pose = d.Pose with { X = d.Pose.X + 1e9 };
        battle.Carrier("B").Pose = battle.Carrier("B").Pose with { X = 1e9 };
        return battle;
    }
    private static void StraightOrders(Battle battle)
    {
        foreach (var d in battle.Drones.Where(d => d.IsActive)) d.Order = new(d.Speed, 0);
    }
}
