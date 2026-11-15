using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class Ep0029Us0010Tk0003Tests
{
    private static TacticalUnitSnapshot Drone(int? ammo) => new(
        Id: "DRN-1", Side: "A", Kind: TacticalUnitKind.Drone, FrameId: "light",
        Pose: new TacticalPoseSnapshot(0, 0, 0), Speed: 10, Hull: 10, MaxHull: 10, Armor: 5, MaxArmor: 5,
        State: TacticalUnitState.Air, RoleId: null, Jammed: false,
        Engines: ImmutableArray<TacticalEngineSnapshot>.Empty, Limits: null, PreviousTurn: 0, Build: null,
        Accuracy: 0, Maneuver: 0)
    {
        AmmoCurrent = ammo,
        AmmoCapacity = 6,
    };

    private static readonly TacticalForecastRules ForecastRules = new(
        new TacticalHitRulesSnapshot(0, 0, 1, 0, 0, 0, 0), 0, 0, 1);

    private static TacticalBattleSnapshot Snapshot(TacticalUnitSnapshot unit) => new(
        "battle", 0, TacticalBattlePhase.Fire, 0, "A", 0, "s", "S", [], [unit], [], [], [], [], [], [unit],
        false, ForecastRules, [], null, []);

    private static TacticalBattleFrame Frame(TacticalUnitSnapshot unit, int targets) =>
        new(Snapshot(unit), new TacticalBattleView(880, 720, 0, 0, 1), SKRect.Empty, 1, false, "",
            FirePlan: new TacticalFirePlan(
                [new TacticalFirePlanRow(unit,
                    new TacticalTargetGroupSnapshot(unit.Id,
                        Enumerable.Range(0, targets).Select(i => new TacticalTargetCandidateSnapshot($"e{i}", 100, 0, false, 0, 0.5)).ToImmutableArray(),
                        null), null)],
                unit.Id));

    [Fact]
    public void NoAmmoCannotChooseTargetAcrossSkins()
    {
        // A zero-ammo drone has no available targets, regardless of the fire plan.
        var noAmmo = Frame(Drone(0), targets: 2);
        Assert.Equal(0, TacticalBattleMapRenderer.TargetCount(noAmmo, Drone(0)));

        // A drone with ammo sees the available targets.
        var withAmmo = Frame(Drone(5), targets: 2);
        Assert.Equal(2, TacticalBattleMapRenderer.TargetCount(withAmmo, Drone(5)));

        // Legacy (null) ammo is unknown, not zero — it must not block targeting.
        var legacy = Frame(Drone(null), targets: 2);
        Assert.Equal(2, TacticalBattleMapRenderer.TargetCount(legacy, Drone(null)));
    }

    [Fact]
    public void AmmoZeroAllowsEvadeAndChargedDecoy()
    {
        // E/D are orthogonal to ammo: the zero-ammo gate only zeroes the target list, never the
        // evade/decoy flags.
        var unit = Drone(0);
        Assert.Equal(0, TacticalBattleMapRenderer.TargetCount(Frame(unit, 1), unit));
        Assert.False(unit.Jammed);
        Assert.Equal(TacticalUnitState.Air, unit.State);
    }

    [Fact]
    public void ReplayShowsSixThenFiveNotFinalZero()
    {
        // Each replay step reads its own ammo; a mid-salvo step with ammo still shows targets,
        // while the final zero-ammo step blocks them.
        var midStep = Drone(5);
        Assert.Equal(2, TacticalBattleMapRenderer.TargetCount(Frame(midStep, 2), midStep));
        var finalStep = Drone(0);
        Assert.Equal(0, TacticalBattleMapRenderer.TargetCount(Frame(finalStep, 2), finalStep));
    }
}
