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

    private static int IndexOf(LiveTutorial live, string stepId)
    {
        int index = live.Director.Heard.ToList().IndexOf(stepId);
        Assert.True(index >= 0, stepId + " not heard: " + string.Join(", ", live.Director.Heard));
        return index;
    }
}
