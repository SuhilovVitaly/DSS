using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.Tutorial;
using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Client.Tests.Tutorial.TutorialTestBattle;

namespace DeepSpaceSaga.Client.Tests.Tutorial;

public sealed class TutorialSignalsTests
{
    private static TacticalIncomingShotSnapshot Shot(string shooter, string target) => new(shooter, target, 0.5, 100, 0, false, 0);

    private static TacticalOutcomeSnapshot Outcome(TacticalBattleOutcomeCode kind) =>
        new(kind, [], 5, [], new(0, 0, 0, 0, 0, 0, 0, 0, 0, null, 0, []), 0);

    private static ImmutableArray<TacticalUnitSnapshot> With(params TacticalUnitSnapshot[] replaced) =>
        [.. DefaultUnits.Select(u => replaced.FirstOrDefault(r => r.Id == u.Id) ?? u)];

    /// <summary>Positive case, expected unit, negative case (same phase) for every signal.</summary>
    private static (TacticalBattleSnapshot Positive, string Phase, string? UnitId, TacticalBattleSnapshot Negative) Case(TutorialSignal signal)
    {
        var fire = Battle(TacticalBattlePhase.Fire);
        var move = Battle(TacticalBattlePhase.Move);
        var over = Battle(TacticalBattlePhase.Over);
        return signal switch
        {
            TutorialSignal.UnderFire => (fire with { Incoming = [Shot("tag-1", "Скиталец"), Shot("tag-1", "yun"), Shot("tag-2", "vera")] }, "Fire", "yun",
                fire with { Incoming = [Shot("tag-1", "Скиталец"), Shot("vera", "tag-2")] }),
            TutorialSignal.OwnDamaged => (move with { Units = With(Unit("yun", armor: 49), Unit("tag-1", "B", hull: 1)) }, "Move", "yun",
                move with { Units = With(Unit("vera", state: TacticalUnitState.Destroyed, hull: 0), Unit("tag-1", "B", hull: 1)) }),
            TutorialSignal.LowConsciousness => (fire with { Units = With(Unit("yun", consciousness: 9)) }, "Fire", "yun",
                fire with { Units = With(Unit("yun", consciousness: 10), Unit("vera", state: TacticalUnitState.Disabled, consciousness: 1),
                    Unit("tag-1", "B", consciousness: 0)) }),
            TutorialSignal.AnomalyPresent => (move with { Anomalies = [new("dust-1", TacticalAnomalyKindCode.Dust, [])], Units = With(Unit("yun", inDust: true)) },
                "Move", "yun", move with { Units = With(Unit("yun", inDust: true)) }),
            TutorialSignal.DroneLost => (fire with { Units = With(Unit("yun", state: TacticalUnitState.Disabled), Unit("vera", state: TacticalUnitState.Destroyed)) },
                "Fire", "vera", fire with { Units = With(Unit("tag-1", "B", state: TacticalUnitState.Destroyed)) }),
            TutorialSignal.OutcomeVictory => (over with { Outcome = Outcome(TacticalBattleOutcomeCode.A) }, "Outcome", null,
                over with { Outcome = Outcome(TacticalBattleOutcomeCode.B) }),
            TutorialSignal.OutcomeDefeat => (over with { Outcome = Outcome(TacticalBattleOutcomeCode.B) }, "Outcome", null,
                over with { Outcome = Outcome(TacticalBattleOutcomeCode.A) }),
            TutorialSignal.OutcomeOther => (over with { Outcome = Outcome(TacticalBattleOutcomeCode.Timeout) }, "Outcome", null,
                over with { Outcome = Outcome(TacticalBattleOutcomeCode.A) }),
            _ => throw new ArgumentOutOfRangeException(nameof(signal)),
        };
    }

    public static TheoryData<string> Signals() => [.. Enum.GetNames<TutorialSignal>().Skip(1)];

    [Theory]
    [MemberData(nameof(Signals))]
    public void FindsEachSignalFromTheVisibleSnapshot(string name)
    {
        var signal = Enum.Parse<TutorialSignal>(name);
        var (positive, phase, unitId, negative) = Case(signal);

        Assert.Equal(new TutorialSignalHit(signal, unitId), TutorialSignals.Find(positive, signal, phase));
        Assert.Equal(TutorialSignal.None, TutorialSignals.Find(negative, signal, phase).Signal);
        Assert.Null(TutorialSignals.Find(negative, signal, phase).UnitId);
    }

    [Fact]
    public void NoSignalOnCleanSnapshot()
    {
        foreach (var phase in new[] { "Move", "Fire", "Salvo", "Results", "Outcome" })
        foreach (var signal in Enum.GetValues<TutorialSignal>())
        {
            var battle = Battle(phase == "Outcome" ? TacticalBattlePhase.Over : phase == "Move" ? TacticalBattlePhase.Move : TacticalBattlePhase.Fire);
            Assert.Equal(default(TutorialSignalHit) with { Signal = TutorialSignal.None }, TutorialSignals.Find(battle, signal, phase));
        }
    }

    [Fact]
    public void UnderFireOnlyInFirePhase()
    {
        var battle = Battle(TacticalBattlePhase.Fire) with { Incoming = [Shot("tag-1", "vera")] };
        Assert.Equal(TutorialSignal.UnderFire, TutorialSignals.Find(battle, TutorialSignal.UnderFire, "Fire").Signal);
        Assert.Equal(TutorialSignal.None, TutorialSignals.Find(battle, TutorialSignal.UnderFire, "Salvo").Signal);
        var gone = battle with { Units = With(Unit("vera", state: TacticalUnitState.Disabled)) };
        Assert.Equal(TutorialSignal.None, TutorialSignals.Find(gone, TutorialSignal.UnderFire, "Fire").Signal);
    }

    [Fact]
    public void LowConsciousnessNeedsEvasionRules()
    {
        var battle = Battle(TacticalBattlePhase.Fire) with { Units = With(Unit("yun", consciousness: 1)) };
        Assert.Equal(TutorialSignal.LowConsciousness, TutorialSignals.Find(battle, TutorialSignal.LowConsciousness, "Fire").Signal);
        var noRules = battle with { Rules = battle.Rules with { Evasion = null } };
        Assert.Equal(TutorialSignal.None, TutorialSignals.Find(noRules, TutorialSignal.LowConsciousness, "Fire").Signal);
    }

    [Fact]
    public void OutcomeFollowsThePlayerSide()
    {
        var battle = Battle(TacticalBattlePhase.Over, playerSide: "B") with { Outcome = Outcome(TacticalBattleOutcomeCode.B) };
        Assert.Equal(TutorialSignal.OutcomeVictory, TutorialSignals.Find(battle, TutorialSignal.OutcomeVictory, "Outcome").Signal);
        Assert.Equal(TutorialSignal.None, TutorialSignals.Find(battle, TutorialSignal.OutcomeDefeat, "Outcome").Signal);
        var draw = battle with { Outcome = Outcome(TacticalBattleOutcomeCode.Draw) };
        Assert.Equal(TutorialSignal.OutcomeOther, TutorialSignals.Find(draw, TutorialSignal.OutcomeOther, "Outcome").Signal);
    }

    [Fact]
    public void DroneLostFollowsTheProjectionNotTheFinalState()
    {
        // The accepted state already carries the whole resolved salvo; the salvo projection shows units as of the current step.
        var resolved = With(Unit("vera", state: TacticalUnitState.Destroyed, hull: 0));
        var step = new TacticalFireStepSnapshot(0, "tag-1", "vera", TacticalFireSkipReason.None, null, resolved, [], null);
        var projectionBeforeImpact = Battle(TacticalBattlePhase.Fire) with { FireSteps = [step], BeforeSalvoUnits = resolved };

        Assert.Equal(TutorialSignal.None, TutorialSignals.Find(projectionBeforeImpact, TutorialSignal.DroneLost, "Salvo").Signal);
        Assert.Equal(TutorialSignal.None, TutorialSignals.Find(projectionBeforeImpact, TutorialSignal.OwnDamaged, "Salvo").Signal);

        var projectionAfterImpact = projectionBeforeImpact with { Units = resolved };
        Assert.Equal(new TutorialSignalHit(TutorialSignal.DroneLost, "vera"),
            TutorialSignals.Find(projectionAfterImpact, TutorialSignal.DroneLost, "Salvo"));
    }
}
