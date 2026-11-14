using System.Collections.Immutable;
using System.Reflection;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens.Tutorial;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.Tutorial;

public sealed class TutorialDirectorTests
{
    private static TutorialStep Step(string id, string phase, TutorialCompletion complete, int priority = 50,
        int? turn = null, int? minTurn = null, string? after = null, TutorialSignal signal = TutorialSignal.None) =>
        new(id, "Tutorial.Line." + id, new TutorialCondition(phase, turn, minTurn, after, signal), complete, null, priority);

    private static TutorialDirector Director(params TutorialStep[] steps) =>
        new(new TutorialScript(1, null, new TutorialLimits(), [.. steps]));

    private static readonly TacticalBattleSnapshot MoveBattle = TutorialTestBattle.Battle(TacticalBattlePhase.Move, 1);
    private static readonly TacticalBattleSnapshot FireBattle = TutorialTestBattle.Battle(TacticalBattlePhase.Fire, 1);

    private static TutorialObservation Move(string? selected = null, TacticalMovementFrame? movement = null, int turn = 1,
        long now = 0, bool busy = false) =>
        new("Move", turn, turn == 1 ? MoveBattle : MoveBattle with { Turn = turn }, selected, movement, null, now, Busy: busy);

    private static TutorialObservation Fire(TacticalFirePlan? plan = null, string? selected = null, long now = 0) =>
        new("Fire", 1, FireBattle, selected ?? plan?.SelectedDroneId, null, plan, now);

    [Fact]
    public void ShowsHighestPriorityEligibleStepAndCompletesWhenPlayerSelectsPilot()
    {
        var director = Director(
            Step("later", "Move", TutorialCompletion.Acknowledged, priority: 10),
            Step("fire", "Fire", TutorialCompletion.Acknowledged, priority: 100),
            Step("select", "Move", TutorialCompletion.PilotSelected, priority: 90, turn: 1),
            Step("select-twin", "Move", TutorialCompletion.PilotSelected, priority: 90, turn: 1));

        director.Observe(Move(now: 100));
        Assert.Equal("select", director.Current?.Step.Id);
        Assert.Equal(100, director.Current!.ShownAtMs);
        Assert.Equal(TutorialStepState.Shown, director.StateOf("select"));
        Assert.Equal(TutorialStepState.Waiting, director.StateOf("later"));

        director.Observe(Move(selected: "vera", now: 200));

        Assert.Equal(TutorialStepState.Completed, director.StateOf("select"));
        Assert.Equal(TutorialStepState.Completed, director.StateOf("select-twin")); // already done before it was shown
        Assert.Equal("later", director.Current?.Step.Id);
        Assert.Equal(["select", "later"], director.Heard);
        Assert.Equal(
        [
            new TutorialEvent("select", TutorialStepState.Shown, "Move", 1, 100),
            new TutorialEvent("select", TutorialStepState.Completed, "Move", 1, 200),
            new TutorialEvent("select-twin", TutorialStepState.Completed, "Move", 1, 200),
            new TutorialEvent("later", TutorialStepState.Shown, "Move", 1, 200),
        ], director.DrainEvents());
        Assert.Empty(director.DrainEvents());
    }

    [Fact]
    public void ShowsOnlyWhenConditionHolds()
    {
        var director = Director(
            Step("t2", "Move", TutorialCompletion.Acknowledged, turn: 2),
            Step("min3", "Move", TutorialCompletion.Acknowledged, minTurn: 3),
            Step("after", "Move", TutorialCompletion.Acknowledged, after: "t2"),
            Step("signal", "Move", TutorialCompletion.Acknowledged, priority: 100, signal: TutorialSignal.UnderFire));

        director.Observe(Move(turn: 1));
        Assert.Null(director.Current);

        director.Observe(Move(turn: 2));
        Assert.Equal("t2", director.Current?.Step.Id);
        director.Acknowledge();
        Assert.Null(director.Current);
        director.Observe(Move(turn: 2));
        Assert.Equal("after", director.Current?.Step.Id);
        director.Acknowledge();

        director.Observe(Move(turn: 3));
        Assert.Equal("min3", director.Current?.Step.Id);
        director.Acknowledge();
        director.Observe(Move(turn: 4));
        Assert.Null(director.Current);
        Assert.Equal(TutorialStepState.Waiting, director.StateOf("signal"));
    }

    public static TheoryData<string> Predicates() =>
    [
        nameof(TutorialCompletion.PilotSelected), nameof(TutorialCompletion.PathEdited), nameof(TutorialCompletion.PhaseLeft),
        nameof(TutorialCompletion.TargetChosen), nameof(TutorialCompletion.PilotCycled), nameof(TutorialCompletion.EvadeOrDecoy),
    ];

    [Theory]
    [MemberData(nameof(Predicates))]
    public void EachCompletionPredicateClosesTheShownStep(string predicate)
    {
        var completion = Enum.Parse<TutorialCompletion>(predicate);
        bool fire = completion is TutorialCompletion.TargetChosen or TutorialCompletion.EvadeOrDecoy;
        var director = Director(Step("step", fire ? "Fire" : "Move", completion));
        var (before, after) = completion switch
        {
            TutorialCompletion.PilotSelected => (Move(), Move(selected: "vera")),
            TutorialCompletion.PathEdited => (Move(movement: TutorialTestBattle.Movement(MoveBattle, null)),
                Move(selected: "vera", movement: TutorialTestBattle.Movement(MoveBattle, "vera", "vera"))),
            TutorialCompletion.PhaseLeft => (Move(selected: "vera"), Fire(TutorialTestBattle.Plan(FireBattle))),
            TutorialCompletion.TargetChosen => (Fire(TutorialTestBattle.Plan(FireBattle)),
                Fire(TutorialTestBattle.Plan(FireBattle, retarget: ("vera", null)))),
            TutorialCompletion.PilotCycled => (Move(selected: "vera"), Move(selected: "yun")),
            TutorialCompletion.EvadeOrDecoy => (Fire(TutorialTestBattle.Plan(FireBattle)),
                Fire(TutorialTestBattle.Plan(FireBattle, decoy: "yun"))),
            _ => throw new ArgumentOutOfRangeException(nameof(completion)),
        };

        director.Observe(before with { NowMs = 10 });
        Assert.Equal(TutorialStepState.Shown, director.StateOf("step"));
        director.Observe(before with { NowMs = 20 });
        Assert.Equal(TutorialStepState.Shown, director.StateOf("step"));

        director.Observe(after with { NowMs = 30 });

        Assert.Equal(TutorialStepState.Completed, director.StateOf("step"));
        Assert.Null(director.Current);
        Assert.Equal(TutorialStepState.Completed, director.DrainEvents()[^1].State);
    }

    [Fact]
    public void EvadeAlsoCompletesEvadeOrDecoy()
    {
        var director = Director(Step("step", "Fire", TutorialCompletion.EvadeOrDecoy));
        director.Observe(Fire(TutorialTestBattle.Plan(FireBattle)));
        director.Observe(Fire(TutorialTestBattle.Plan(FireBattle, evade: "vera")));
        Assert.Equal(TutorialStepState.Completed, director.StateOf("step"));
    }

    [Fact]
    public void PilotCycledNeedsAChangeBetweenTwoPilotsWithinThePhase()
    {
        var director = Director(Step("cycle", "Move", TutorialCompletion.PilotCycled));
        director.Observe(Move());
        director.Observe(Move(selected: "vera")); // first selection is not a cycle
        Assert.Equal(TutorialStepState.Shown, director.StateOf("cycle"));
        director.Observe(Move(selected: "vera"));
        Assert.Equal(TutorialStepState.Shown, director.StateOf("cycle"));
        director.Observe(Move(selected: "yun"));
        Assert.Equal(TutorialStepState.Completed, director.StateOf("cycle"));
    }

    [Fact]
    public void AcknowledgeCompletesAcknowledgedStepAndDismissesOthers()
    {
        var director = Director(
            Step("intro", "Move", TutorialCompletion.Acknowledged, priority: 100),
            Step("select", "Move", TutorialCompletion.PilotSelected));
        director.Observe(Move(now: 5));
        director.Acknowledge();
        Assert.Equal(TutorialStepState.Completed, director.StateOf("intro"));
        Assert.Null(director.Current);
        director.Observe(Move(now: 6));
        Assert.Equal("select", director.Current?.Step.Id);
        director.Acknowledge();
        Assert.Equal(TutorialStepState.Dismissed, director.StateOf("select"));
        director.Acknowledge(); // nothing shown: no-op
        director.Observe(Move(selected: "vera", now: 7));
        Assert.Equal(TutorialStepState.Dismissed, director.StateOf("select"));
        Assert.Null(director.Current);
        Assert.Equal(
            [TutorialStepState.Shown, TutorialStepState.Completed, TutorialStepState.Shown, TutorialStepState.Dismissed],
            director.DrainEvents().Select(e => e.State));
    }

    [Fact]
    public void StepAlreadyDoneBeforeShowIsCompletedSilently()
    {
        var director = Director(
            Step("intro", "Move", TutorialCompletion.Acknowledged, priority: 100),
            Step("select", "Move", TutorialCompletion.PilotSelected, after: "intro"),
            Step("edit", "Move", TutorialCompletion.PathEdited, after: "select"));
        director.Observe(Move());
        Assert.Equal("intro", director.Current?.Step.Id);

        director.Observe(Move(selected: "vera", movement: TutorialTestBattle.Movement(MoveBattle, "vera")));
        director.Acknowledge();
        director.Observe(Move(selected: "vera", movement: TutorialTestBattle.Movement(MoveBattle, "vera", "vera")));

        Assert.Equal(TutorialStepState.Completed, director.StateOf("select"));
        Assert.Equal(TutorialStepState.Completed, director.StateOf("edit"));
        Assert.Null(director.Current);
        Assert.Equal(["intro"], director.Heard);
        Assert.DoesNotContain(director.DrainEvents(), e => e.StepId != "intro" && e.State == TutorialStepState.Shown);
    }

    [Fact]
    public void UnfinishedStepExpiresWhenPhaseEnds()
    {
        var director = Director(
            Step("select", "Move", TutorialCompletion.PilotSelected),
            Step("leave", "Fire", TutorialCompletion.PhaseLeft));
        director.Observe(Move(now: 1));
        Assert.Equal("select", director.Current?.Step.Id);

        director.Observe(Fire(TutorialTestBattle.Plan(FireBattle), now: 2));
        Assert.Equal(TutorialStepState.Expired, director.StateOf("select"));
        Assert.Equal("leave", director.Current?.Step.Id);

        director.Observe(Move(turn: 2, now: 3));
        Assert.Equal(TutorialStepState.Completed, director.StateOf("leave")); // leaving the phase is its completion
        Assert.Null(director.Current);
        director.Observe(Move(turn: 2, selected: "vera", now: 4));
        Assert.Equal(TutorialStepState.Expired, director.StateOf("select"));
        Assert.Equal(["select", "leave"], director.Heard);
        Assert.Contains(new TutorialEvent("select", TutorialStepState.Expired, "Fire", 1, 2), director.DrainEvents());
    }

    [Fact]
    public void ShownStepExpiresWhenTurnChangesInTheSamePhaseName()
    {
        var director = Director(Step("edit", "Move", TutorialCompletion.PathEdited));
        director.Observe(Move(turn: 1));
        director.Observe(Move(turn: 2));
        Assert.Equal(TutorialStepState.Expired, director.StateOf("edit"));
        Assert.Null(director.Current);
    }

    [Fact]
    public void DisabledDirectorShowsNothingButTracksCompletion()
    {
        var director = Director(
            Step("intro", "Move", TutorialCompletion.Acknowledged, priority: 100),
            Step("select", "Move", TutorialCompletion.PilotSelected));
        director.Observe(Move(now: 1));
        Assert.Equal("intro", director.Current?.Step.Id);

        director.Enabled = false;
        Assert.Null(director.Current);
        Assert.Equal(TutorialStepState.Dismissed, director.StateOf("intro"));

        director.Observe(Move(now: 2));
        Assert.Null(director.Current);
        director.Observe(Move(selected: "vera", now: 3));
        Assert.Null(director.Current);
        Assert.Equal(TutorialStepState.Completed, director.StateOf("select"));

        director.Enabled = true;
        director.Observe(Move(selected: "vera", now: 4));
        Assert.Null(director.Current);
        Assert.Equal(["intro"], director.Heard);
    }

    [Fact]
    public void BusyObservationDefersNewLines()
    {
        var director = Director(
            Step("intro", "Move", TutorialCompletion.Acknowledged, priority: 100),
            Step("select", "Move", TutorialCompletion.PilotSelected, priority: 90),
            Step("edit", "Move", TutorialCompletion.PathEdited, priority: 80));
        director.Observe(Move(busy: true));
        Assert.Null(director.Current);
        Assert.Equal(TutorialStepState.Waiting, director.StateOf("intro"));

        director.Observe(Move(selected: "vera", busy: true));
        Assert.Null(director.Current);
        Assert.Equal(TutorialStepState.Completed, director.StateOf("select"));

        director.Observe(Move(selected: "vera"));
        Assert.Equal("intro", director.Current?.Step.Id);
        director.Observe(Move(selected: "vera", movement: TutorialTestBattle.Movement(MoveBattle, "vera", "vera"), busy: true));
        Assert.Equal("intro", director.Current?.Step.Id); // a shown line stays while busy
        Assert.Equal(TutorialStepState.Completed, director.StateOf("edit"));
    }

    [Fact]
    public void ObservationFromFrameCopiesFrameFacts()
    {
        var movement = TutorialTestBattle.Movement(MoveBattle, "vera", "vera");
        var frame = new TacticalBattleFrame(MoveBattle with { Turn = 3 }, new TacticalBattleView(1280, 720, 0, 0, 1), SKRect.Empty, 1,
            false, "", SelectedDroneId: "vera", Movement: movement);

        var observation = TutorialObservation.From(frame, "Move", 1234, interacting: true, busy: true);

        Assert.Equal(new TutorialObservation("Move", 3, frame.Battle, "vera", movement, null, 1234, true, true), observation);
    }

    [Fact]
    public void DirectorExposesNoInputBlockingDecision()
    {
        var methods = typeof(TutorialDirector).GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public |
            BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Where(m => !m.IsSpecialName && !m.IsPrivate);
        Assert.DoesNotContain(methods, m => m.ReturnType == typeof(bool));
    }
}

/// <summary>Synthetic battle snapshots for tutorial tests (no Engine involved).</summary>
internal static class TutorialTestBattle
{
    internal static readonly TacticalForecastRules Rules = new(new(50, 5, 95, 15, 12, 10, 30), 0.5, 45, 4)
    { Evasion = new(10, 10, 10, 5, 100) };

    internal static TacticalUnitSnapshot Unit(string id, string side = "A", TacticalUnitKind kind = TacticalUnitKind.Drone,
        TacticalUnitState state = TacticalUnitState.Air, int hull = 100, int maxHull = 100, int armor = 50, int maxArmor = 50,
        int? consciousness = 100, bool inDust = false) =>
        new(id, side, kind, "frame", new(0, 0, 0), 50, hull, maxHull, armor, maxArmor, state, null, false, [], null, 0, null, 50, 50)
        { Consciousness = consciousness, InDust = inDust };

    internal static ImmutableArray<TacticalUnitSnapshot> DefaultUnits { get; } =
        [Unit("Скиталец", kind: TacticalUnitKind.Carrier, state: TacticalUnitState.Alive), Unit("vera"), Unit("yun"),
         Unit("Логово", "B", TacticalUnitKind.Carrier, TacticalUnitState.Alive), Unit("tag-1", "B"), Unit("tag-2", "B")];

    internal static TacticalBattleSnapshot Battle(TacticalBattlePhase phase = TacticalBattlePhase.Move, int turn = 1,
        ImmutableArray<TacticalUnitSnapshot>? units = null, string playerSide = "A") =>
        new("battle", 1, phase, turn, playerSide, 1, "initial", "Initial", [], units ?? DefaultUnits, [], [], [], [], [], [], false,
            Rules, [], null, []);

    internal static TacticalMovementFrame Movement(TacticalBattleSnapshot battle, string? selected, params string[] touched) =>
        new([.. battle.Units.Where(u => u.Side == battle.PlayerSide && u.Kind == TacticalUnitKind.Drone).Select(u =>
            new TacticalOrderPreview(u, new(u.Id, 50, touched.Contains(u.Id) ? 5 : 0), new(u.Id, 50, 0), touched.Contains(u.Id),
                10, 100, 30, 30, []))], selected, []);

    internal static TacticalFirePlan Plan(TacticalBattleSnapshot battle, (string Drone, string? Target)? retarget = null,
        string? evade = null, string? decoy = null)
    {
        var rows = battle.Units.Where(u => u.Side == battle.PlayerSide && u.Kind == TacticalUnitKind.Drone).Select(u =>
        {
            var group = new TacticalTargetGroupSnapshot(u.Id, [new("tag-1", 100, 0, false, 0, 0.5), new("tag-2", 120, 0, false, 0, 0.4)], "tag-1");
            string? target = retarget is { } r && r.Drone == u.Id ? r.Target : group.DefaultTargetId;
            return new TacticalFirePlanRow(u, group, target) { Evade = u.Id == evade, Decoy = u.Id == decoy };
        });
        return new([.. rows], "vera");
    }
}
