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
}
