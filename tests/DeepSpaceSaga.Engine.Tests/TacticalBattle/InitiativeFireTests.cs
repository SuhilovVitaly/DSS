using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class InitiativeFireTests
{
    [Fact]
    public void HighInitiativeKillCancelsReplyWithoutDraws()
    {
        var (battle, a, b) = SalvoTests.Arena(damage: 40, hull: 40);
        long calls = battle.Random.Calls;
        var salvo = TacticalBattleSalvo.Fire(battle);
        Assert.Equal(a.Id, Assert.Single(salvo.Shots).Shooter.Id);
        Assert.Equal(2, battle.Random.Calls - calls);
        Assert.Equal(40, a.Hull);
        Assert.Equal(0, b.Hull);
        Assert.Equal("B", battle.Statistics.FirstLoss);
        Assert.Equal(1, battle.Statistics.Shots);
    }

    [Fact]
    public void DisableAndWeaponJamBeforeTurnCancelReply()
    {
        foreach (bool disable in new[] { false, true })
        {
            var (battle, a, b) = SalvoTests.Arena(damage: disable ? 80 : 40, systems: "weaponJammed");
            long calls = battle.Random.Calls;
            var salvo = TacticalBattleSalvo.Fire(battle);
            Assert.Equal(a.Id, Assert.Single(salvo.Shots).Shooter.Id);
            Assert.Equal(disable ? TacticalDroneStatus.Disabled : TacticalDroneStatus.Air, b.Status);
            Assert.Equal(!disable, b.Jammed);
            Assert.Equal(disable ? 2 : 3, battle.Random.Calls - calls);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LostFrozenTargetIsSkippedForPlayerAndAiWithoutRetarget(bool explicitOrders)
    {
        var (battle, a, b) = SalvoTests.Arena(damage: 40, hull: 40);
        var second = SalvoTests.ActivateSecondA(battle);
        var other = battle.Drones[5];
        other.Status = TacticalDroneStatus.Air;
        other.Pose = b.Pose with { Y = 10 }; // both A shooters initially prefer b
        other.Jammed = true;
        var salvo = TacticalBattleSalvo.Fire(battle, explicitOrders ? (d, _) => new(d.Side == "A" ? b.Id : null, false) : null);
        Assert.Equal(a.Id, Assert.Single(salvo.Shots).Shooter.Id);
        Assert.Equal(40, other.Hull);
        Assert.Equal(1, battle.Statistics.Shots);
        Assert.DoesNotContain(salvo.Shots, s => s.Shooter == second);
        Assert.Equal(TacticalFireSkipReason.TargetUnavailable, salvo.Steps.Single(s => s.ShooterId == second.Id).SkipReason);
        Assert.Equal(TacticalFireSkipReason.ShooterDestroyed, salvo.Steps.Single(s => s.ShooterId == b.Id).SkipReason);
    }

    [Fact]
    public void TwoHitsUseRemainingArmorAndCreditActualHull()
    {
        var (battle, _, b) = SalvoTests.Arena(damage: 15, hull: 100);
        SalvoTests.ActivateSecondA(battle);
        b.Armor = 10;
        var salvo = TacticalBattleSalvo.Fire(battle, (d, _) => new(d.Side == "A" ? b.Id : null, false));
        Assert.Equal(2, salvo.Shots.Length);
        Assert.Equal(80, b.Hull);
        Assert.Equal(0, b.Armor);
        Assert.Equal(20, battle.Statistics.Hull);
        Assert.Equal(20, Assert.Single(battle.Statistics.Weapons).Hull);
        Assert.Equal(new[] { 5, 15 }, salvo.Shots.Select(s => s.SoloHullLoss));
        Assert.Equal((0, 95), Health(salvo.Steps[0]));
        Assert.Equal((0, 80), Health(salvo.Steps[1]));
        (int, int) Health(TacticalSalvoStep step)
        {
            var target = step.UnitsAfter.Single(u => u.Id == b.Id);
            return (target.Armor, target.Hull);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void VictoryAtCarrierOrLastDroneStopsQueueAndPreservesComplete(bool carrier)
    {
        var (battle, a, b) = SalvoTests.Arena(damage: 40, hull: 40);
        if (carrier) { battle.Carrier("B").Pose = b.Pose; battle.Carrier("B").Hull = 40; }
        string target = carrier ? battle.Carrier("B").Id : b.Id;
        battle.Planning(_ => { });
        foreach (var d in battle.Drones) d.Order = new(0, 0);
        // Enter the production Firing phase, then set controlled non-rear geometry.
        battle.Movement();
        a.Pose = new(0, 0, 0); b.Pose = new(10, 0, 180);
        if (carrier) battle.Carrier("B").Pose = b.Pose;
        battle.Firing((d, _) => new(d == a ? target : a.Id, false));
        Assert.Equal(TacticalTurnPhase.Complete, battle.Phase);
        Assert.Equal(TacticalOutcomeKind.A, battle.Result!.Kind);
        Assert.Equal(1, battle.Result.Statistics.Shots);
        Assert.Single(battle.LastSalvo!.Shots);
        Assert.Equal(TacticalFireSkipReason.BattleEnded, battle.LastSalvo.Steps[^1].SkipReason);
        Assert.Equal(40, a.Hull);
    }

    [Fact]
    public void MissWithheldNoTargetsAndInvalidSelectionHaveExactDrawOwnership()
    {
        var (battle, a, b) = SalvoTests.Arena(chance: 0);
        long calls = battle.Random.Calls;
        uint initiativeState = battle.InitiativeRandom.State;
        a.Dry = 3;
        Assert.Throws<ArgumentException>(() => TacticalBattleSalvo.Fire(battle, (_, _) => new("invalid", false)));
        Assert.Equal(calls, battle.Random.Calls);
        Assert.Equal(3, a.Dry);
        Assert.Equal(0, battle.Statistics.Shots);
        var held = TacticalBattleSalvo.Fire(battle, (_, _) => new(null, false));
        Assert.All(held.Steps, step => Assert.Equal(TacticalFireSkipReason.Withheld, step.SkipReason));
        Assert.Equal(calls, battle.Random.Calls);
        var miss = TacticalBattleSalvo.Fire(battle);
        Assert.Equal(2, miss.Shots.Length);
        Assert.All(miss.Shots, shot => Assert.False(shot.Hit));
        Assert.Equal(calls + 2, battle.Random.Calls);
        b.Pose = b.Pose with { X = 1e9 };
        var empty = TacticalBattleSalvo.Fire(battle);
        Assert.Empty(empty.Shots);
        Assert.All(empty.Steps, step => Assert.Equal(TacticalFireSkipReason.NoTarget, step.SkipReason));
        Assert.Equal(calls + 2, battle.Random.Calls);
        Assert.Equal(initiativeState, battle.InitiativeRandom.State);
    }
}
