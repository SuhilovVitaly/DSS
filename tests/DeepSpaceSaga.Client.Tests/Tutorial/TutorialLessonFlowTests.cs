using System.Collections.Immutable;
using DeepSpaceSaga.Client.Tests.TacticalBattle;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens.Tutorial;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.Tutorial;

/// <summary>
/// EP-0028-US-0003: the shipped lessons in a live Tabletop tutorial battle — the same start request as the New Game tutorial
/// (default scenario «initial», seed 1, default loadout, <see cref="TacticalBattleFlow.IsTutorial"/>) with the shipped script.
/// Shared by TK-0001…TK-0005.
/// </summary>
[Collection("InterfaceLog")]
public sealed class TutorialLessonFlowTests
{
    /// <summary>A started tutorial battle on screen, with the shipped tutorial session attached.</summary>
    internal sealed class LiveTutorial : IAsyncDisposable
    {
        private readonly TacticalBattleSkinCatalog _skins;

        private LiveTutorial(BattleNavigationTests.Fixture fixture, TacticalBattleSkinCatalog skins, TacticalBattleScreen screen)
        {
            Fixture = fixture;
            _skins = skins;
            Screen = screen;
        }

        internal BattleNavigationTests.Fixture Fixture { get; }
        internal TacticalBattleScreen Screen { get; }
        internal TutorialSession Session => Screen.Tutorial!;
        internal TutorialDirector Director => Session.Director;
        internal string Phase => Session.LastObservation?.Phase ?? "";

        internal static async Task<LiveTutorial> Start(TutorialScript? script = null)
        {
            var fixture = await BattleNavigationTests.Fixture.Create(initialSpeed: SimulationSpeed.Speed0);
            var skins = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
            try
            {
                // Same request as SkiaWindow.StartTutorialAsync: seed 1, default scenario and loadout.
                var flow = new TacticalBattleFlow(fixture.Handle) { IsTutorial = true };
                var started = await flow.StartAsync(new StartTacticalBattleRequest(TacticalBattleFlow.NewIdentity("start"), 1));
                Assert.True(started?.Accepted, "tutorial battle start rejected: " + started?.Code);
                Assert.Equal("initial", flow.State!.ScenarioId);
                var skin = skins.GetOrClassic("tabletop");
                var screen = new TacticalBattleScreen(flow, 1, skin)
                { Tutorial = new TutorialSession(script ?? TutorialContentTests.Shipped()) };
                Assert.Equal("tabletop", screen.SkinId);
                fixture.Screens.Push(screen);
                await fixture.Route(ScreenEvent.None); // attaches the fixture clock
                return new LiveTutorial(fixture, skins, screen);
            }
            catch
            {
                skins.Dispose();
                await fixture.DisposeAsync();
                throw;
            }
        }

        internal void Render()
        {
            using var bitmap = new SKBitmap(1280, 720);
            using var canvas = new SKCanvas(bitmap);
            Screen.Render(canvas, 1280, 720);
        }

        /// <summary>Lets the quiet rules' minimum gap pass and renders a frame (the next speech moment).</summary>
        internal void Pause(long ms = 0)
        {
            Fixture.AdvanceAnimation(ms > 0 ? ms : Session.Script.Limits.MinGapMs);
            Render();
        }

        internal string? HighlightKey()
        {
            if (Director.Current is not { } line || Session.LastObservation is not { } observation) return null;
            return TutorialHighlight.ResolveKey(line.Step.Anchor, Screen.Layout, observation, line.SignalUnitId);
        }

        internal IEnumerable<string> Texts => Screen.Layout.Texts.Select(t => t.Text);

        internal int Turn => Session.LastObservation?.Turn ?? 0;

        /// <summary>Presses a key on the battle screen, routes the resulting event and renders the next frame.</summary>
        internal async Task Press(Key key)
        {
            var evt = Screen.OnKeyDown(key);
            if (evt != ScreenEvent.None) await Fixture.Route(evt);
            Render();
        }

        /// <summary>
        /// One pure-Enter step with a rendered frame right after it (so the officer observes every presentation phase), then
        /// lets the movement animation finish and renders again.
        /// </summary>
        internal async Task Enter()
        {
            await Press(Key.Enter);
            Fixture.AdvanceAnimation(TacticalBattleMovementAnimation.FireTransitionDurationMs);
            Render();
        }

        /// <summary>Pure Enter until the given phase (and turn, if any) is on screen.</summary>
        internal async Task EnterUntil(string phase, int? turn = null, int limit = 100)
        {
            Render();
            for (int i = 0; i < limit && !(Phase == phase && (turn is null || Turn == turn)); i++) await Enter();
            Assert.Equal(phase, Phase);
            if (turn is not null) Assert.Equal(turn, Turn);
        }

        public async ValueTask DisposeAsync()
        {
            _skins.Dispose();
            await Fixture.DisposeAsync();
        }
    }

    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public async Task IntroGreetsOnFirstMoveAndClosesWhenPilotSelected(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        await using var live = await LiveTutorial.Start();
        var officer = live.Session.Script.Officer!;

        live.Render();
        Assert.Equal("Move", live.Phase);
        Assert.Null(live.Screen.Movement.Selected); // Tabletop Move starts without a selected pilot
        Assert.Equal("intro", live.Director.Current?.Step.Id);
        Assert.Equal(["intro"], live.Director.Heard);
        Assert.Contains(live.Texts, t => t.StartsWith(ControlText.SafeSymbols(Localization.Get(officer.NameKey)), StringComparison.Ordinal));
        Assert.DoesNotContain(live.Texts, t => t.StartsWith("Tutorial.", StringComparison.Ordinal));
        Assert.StartsWith("pilot:", live.HighlightKey());
        Assert.True(live.Screen.Layout.Rects.ContainsKey("tutorial:highlight"));
        Assert.NotNull(live.Session.Portrait);

        Assert.Equal(ScreenEvent.None, live.Screen.OnKeyDown(Key.Tab));
        live.Render();
        Assert.Equal(TutorialStepState.Completed, live.Director.StateOf("intro"));
    }

    [Fact]
    public async Task FirstTwoTurnsTeachMovementByPlayerActions()
    {
        await using var live = await LiveTutorial.Start();
        live.Render();
        Assert.Equal("intro", live.Director.Current?.Step.Id);

        // T1: select a pilot → steer → submit.
        await live.Press(Key.Tab);
        Assert.Equal(TutorialStepState.Completed, live.Director.StateOf("intro"));
        Assert.Null(live.Director.Current); // the minimum gap after the player's action
        live.Pause();
        Assert.Equal("move.steer", live.Director.Current?.Step.Id);
        Assert.Equal("endpoint:" + live.Screen.Movement.Selected, live.HighlightKey());

        await live.Press(Key.Right);
        Assert.Equal(TutorialStepState.Completed, live.Director.StateOf("move.steer"));
        live.Pause();
        Assert.Equal("move.submit", live.Director.Current?.Step.Id);
        Assert.Equal("button:primary", live.HighlightKey());

        await live.Enter(); // submit; the movement animation plays, then the next phase is on screen
        Assert.NotEqual(("Move", 1), (live.Phase, live.Turn));
        Assert.Equal(TutorialStepState.Completed, live.Director.StateOf("move.submit"));

        // T2: Tab cycles pilots → the reachable zone.
        await live.EnterUntil("Move", 2);
        live.Pause();
        Assert.Equal("move.cycle", live.Director.Current?.Step.Id);
        Assert.StartsWith("pilot:", live.HighlightKey());
        await live.Press(Key.Tab);
        Assert.Equal(TutorialStepState.Shown, live.Director.StateOf("move.cycle")); // the first pick is not a cycle yet
        await live.Press(Key.Tab);
        Assert.Equal(TutorialStepState.Completed, live.Director.StateOf("move.cycle"));
        live.Pause();
        Assert.Equal("move.reachable", live.Director.Current?.Step.Id);
        Assert.Equal("reachable", live.HighlightKey());
        await live.Press(Key.Up);
        Assert.Equal(TutorialStepState.Completed, live.Director.StateOf("move.reachable"));

        int intro = IndexOf(live, "intro"), steer = IndexOf(live, "move.steer"), submit = IndexOf(live, "move.submit");
        int cycle = IndexOf(live, "move.cycle"), reachable = IndexOf(live, "move.reachable");
        Assert.True(intro < steer && steer < submit && submit < cycle && cycle < reachable, string.Join(", ", live.Director.Heard));
    }

    [Fact]
    public async Task PureEnterNeverWaitsForTheOfficer()
    {
        await using var live = await LiveTutorial.Start();
        var stuck = new List<string>();
        live.Render();
        for (int i = 0; i < 100 && live.Screen.Flow.State!.Phase != TacticalBattlePhase.Over; i++)
        {
            string phase = live.Phase;
            int turn = live.Turn;
            await live.Enter();
            // Enter always moves the battle on: a shown line never holds the phase, and it never outlives its phase.
            if (live.Director.Current is { } line && (line.Step.When.Phase != live.Phase))
                stuck.Add($"{line.Step.Id} shown in {live.Phase}");
            if (phase == live.Phase && turn == live.Turn && live.Phase != "Outcome")
                stuck.Add($"Enter did not leave {phase} T{turn}");
        }
        await live.EnterUntil("Outcome");
        Assert.Equal(TacticalBattlePhase.Over, live.Screen.Flow.State!.Phase);
        Assert.Empty(stuck);
        Assert.Contains("intro", live.Director.Heard); // spoken, then left behind by Enter
        Assert.Equal(TutorialStepState.Expired, live.Director.StateOf("intro"));
    }

    [Fact]
    public async Task FirstFireAndSalvoAreTaughtThenGetOutOfTheWay()
    {
        await using var live = await LiveTutorial.Start();
        await live.EnterUntil("Fire");
        live.Pause();
        Assert.Equal("fire.target", live.Director.Current?.Step.Id);
        Assert.StartsWith("candidate:", live.HighlightKey());

        // Pick a pilot and give it another target: another enemy on the field, otherwise «Hold fire» on its card.
        await live.Press(Key.Tab);
        var plan = live.Session.LastObservation!.FirePlan!;
        var row = plan.Rows.First(r => r.Drone.Id == plan.SelectedDroneId);
        var other = live.Screen.Layout.Rects.FirstOrDefault(r => r.Key.StartsWith("candidate:", StringComparison.Ordinal) &&
            r.Key != "candidate:" + row.TargetId && row.Group.List.Any(c => "candidate:" + c.TargetId == r.Key));
        var click = other.Key is not null ? other.Value : live.Screen.Layout.Rects["card-command:hold"];
        live.Screen.OnMouseDown(click.MidX, click.MidY, MouseButton.Left);
        live.Screen.OnMouseUp(click.MidX, click.MidY, MouseButton.Left);
        live.Render();
        Assert.Equal(TutorialStepState.Completed, live.Director.StateOf("fire.target"));

        live.Pause();
        Assert.Equal("fire.submit", live.Director.Current?.Step.Id);
        Assert.Equal("button:primary", live.HighlightKey());

        await live.Press(Key.Enter); // open fire: the salvo is in flight on the next frame
        Assert.Equal("Salvo", live.Phase);
        Assert.Equal(TutorialStepState.Completed, live.Director.StateOf("fire.submit"));
        live.Pause();
        Assert.Equal("Salvo", live.Phase);
        Assert.Equal("salvo.watch", live.Director.Current?.Step.Id);
        Assert.Equal("button:primary", live.HighlightKey());

        await live.Press(Key.Enter); // skip the flight
        Assert.Equal("Results", live.Phase);
        Assert.Equal(TutorialStepState.Completed, live.Director.StateOf("salvo.watch"));
        live.Pause();
        Assert.Equal("results.read", live.Director.Current?.Step.Id);
        Assert.Equal("button:replay", live.HighlightKey());

        await live.Press(Key.Enter); // on to the next turn
        Assert.Equal("Move", live.Phase);
        Assert.Equal(TutorialStepState.Completed, live.Director.StateOf("results.read"));
        Assert.Null(live.Director.Current); // nothing from the salvo lingers into the next Move
    }

    /// <summary>A patient player: lets the officer speak (minimum gap) before every Enter, records every shown line with its phase.</summary>
    private static async Task<List<(string Step, string Phase, int Turn)>> PlayPatiently(LiveTutorial live, int limit = 120)
    {
        var shown = new List<(string, string, int)>();
        void Record()
        {
            if (live.Director.Current is { } line) shown.Add((line.Step.Id, live.Phase, live.Turn));
        }
        live.Render();
        for (int i = 0; i < limit && live.Phase != "Outcome"; i++)
        {
            live.Pause();
            Record();
            live.Pause();
            Record();
            await live.Press(Key.Enter);
            Record();
            live.Fixture.AdvanceAnimation(TacticalBattleMovementAnimation.FireTransitionDurationMs);
            live.Render();
            Record();
        }
        live.Pause();
        Record();
        return shown;
    }

    [Fact]
    public async Task ContextLessonsAppearOnceWhenTheirSituationArises()
    {
        // Live: the seed-1 tutorial battle played patiently with Enter only.
        await using (var live = await LiveTutorial.Start())
        {
            var shown = await PlayPatiently(live);
            Assert.Equal("Outcome", live.Phase);
            foreach (var step in live.Session.Script.Steps.Where(s => s.Id.StartsWith("ctx.", StringComparison.Ordinal)))
            {
                Assert.True(live.Director.Heard.Count(id => id == step.Id) <= 1, step.Id + " heard twice");
                Assert.All(shown.Where(s => s.Step == step.Id), s => Assert.Equal(step.When.Phase, s.Phase));
            }
        }

        // Synthetic: every signal, observed through a session with the shipped contextual steps (snapshots as in TutorialSignalsTests).
        var shipped = TutorialContentTests.Shipped();
        var context = shipped with { Steps = [.. shipped.Steps.Where(s => s.Id.StartsWith("ctx.", StringComparison.Ordinal))] };
        Assert.Equal(5, context.Steps.Length);
        var units = TutorialTestBattle.DefaultUnits;
        ImmutableArray<TacticalUnitSnapshot> With(params TacticalUnitSnapshot[] replaced) =>
            [.. units.Select(u => replaced.FirstOrDefault(r => r.Id == u.Id) ?? u)];
        var fire = TutorialTestBattle.Battle(TacticalBattlePhase.Fire, 2);
        var move = TutorialTestBattle.Battle(TacticalBattlePhase.Move, 3);
        (string Step, TutorialObservation Observation, string Unit)[] cases =
        [
            ("ctx.under-fire", new("Fire", 2, fire with { Incoming = [new("tag-1", "yun", 0.5, 100, 0, false, 0)] }, null, null, null, 0), "yun"),
            ("ctx.damage", new("Results", 1, fire with { Turn = 1, Units = With(TutorialTestBattle.Unit("yun", armor: 49)) }, null, null, null, 0), "yun"),
            ("ctx.low-mind", new("Fire", 2, fire with { Units = With(TutorialTestBattle.Unit("yun", consciousness: 9)) }, null, null, null, 0), "yun"),
            ("ctx.anomaly", new("Move", 3, move with { Anomalies = [new("dust-1", TacticalAnomalyKindCode.Dust, [])],
                Units = With(TutorialTestBattle.Unit("yun", inDust: true)) }, null, null, null, 0), "yun"),
            ("ctx.drone-lost", new("Results", 1, fire with { Turn = 1, Units = With(TutorialTestBattle.Unit("vera", state: TacticalUnitState.Destroyed)) },
                null, null, null, 0), "vera"),
        ];
        foreach (var (stepId, observation, unit) in cases)
        {
            var session = new TutorialSession(context);
            string otherPhase = observation.Phase == "Move" ? "Fire" : "Move";
            session.Observe(observation with { Phase = otherPhase, NowMs = 0 }); // the situation, but not its phase: silence
            Assert.Null(session.Director.Current);
            session.Observe(observation with { NowMs = 10_000 });
            Assert.Equal(stepId, session.Director.Current?.Step.Id);
            Assert.Equal(unit, session.Director.Current!.SignalUnitId);
            session.Observe(observation with { Phase = otherPhase, Turn = observation.Turn + 1, NowMs = 20_000 }); // phase left
            Assert.Null(session.Director.Current);
            session.Observe(observation with { Turn = observation.Turn + 2, NowMs = 40_000 }); // the same situation again: no repeat
            Assert.NotEqual(stepId, session.Director.Current?.Step.Id);
            Assert.Equal(1, session.Director.Heard.Count(id => id == stepId));
            session.Release();
        }
    }

    private static int IndexOf(LiveTutorial live, string stepId)
    {
        int index = live.Director.Heard.ToList().IndexOf(stepId);
        Assert.True(index >= 0, stepId + " not heard: " + string.Join(", ", live.Director.Heard));
        return index;
    }
}
