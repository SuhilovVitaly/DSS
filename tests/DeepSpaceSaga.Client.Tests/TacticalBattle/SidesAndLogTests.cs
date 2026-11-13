using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class SidesAndLogTests
{
    [Fact]
    public async Task JournalGroupsTurnsAndPhasesAndSummarizesOnlyCompleteNoTargetQueues()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var frame = screen.CaptureFrame(1280, 720)!;
        var move = new TacticalBattleLogEvent(1, 1, TacticalBattlePhase.Move, TacticalBattleLogKind.Moved, "pilot");
        var automatic = new TacticalBattleLogEvent(2, 1, TacticalBattlePhase.Move, TacticalBattleLogKind.NoTargets);
        var first = new TacticalBattleLogEvent(3, 1, TacticalBattlePhase.Fire, TacticalBattleLogKind.FireSkipped, "no-target-a")
        { SkipReason = TacticalFireSkipReason.NoTarget, FireStepIndex = 0 };
        var second = first with { Sequence = 4, ActorId = "no-target-b", FireStepIndex = 1 };
        var later = move with { Sequence = 5, Turn = 2 };
        using var locale = Localization.UseLocaleForTests("English");

        var full = frame with { Battle = frame.Battle with { Log = [move, automatic, first, second, later] } };
        var lines = TacticalBattleLogTab.FormatJournal(full).Split('\n');
        Assert.Equal(new[] { "── Turn 1 ──", "─ Movement", "pilot moved.", "─ Firing",
            "No targets. No shots were fired.", "", "── Turn 2 ──", "─ Movement", "pilot moved." }, lines);

        var partial = full with
        {
            Battle = full.Battle with { Turn = 1, Log = [move, first], FireQueue = [new(0, "a", 50), new(1, "b", 40)] },
            Salvo = new(true, false, [], []) { Turn = 1 }
        };
        Assert.DoesNotContain("No targets", TacticalBattleLogTab.FormatJournal(partial));
        var mixed = full with { Battle = full.Battle with { Log = [move, first, second with { SkipReason = TacticalFireSkipReason.InDust }] } };
        var mixedText = TacticalBattleLogTab.FormatJournal(mixed);
        Assert.DoesNotContain("no-target-a", mixedText);
        Assert.DoesNotContain("No targets", mixedText);
        Assert.Contains("no-target-b", mixedText);
    }

    [Fact]
    public async Task JournalShowsOnlyArrivedStepEventsAndTypedSkipReasons()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var results = ResultsPhaseTests.ControlledResults(screen.Flow.State!);
        foreach (long time in new long[] { 1274, 1275, 2250 })
        {
            var frame = screen.CaptureFrame(1280, 720)! with
            { Battle = results.Project(time), Presentation = TacticalBattlePresentation.SalvoFlight, Salvo = results.Capture(time) };
            Assert.Equal(time < 1275 ? 0 : time < 2250 ? 1 : 2, TacticalBattleLogTab.Visible(frame).Count());
            Assert.Equal(frame.Battle.Log.AsEnumerable(), TacticalBattleLogTab.Visible(frame));
        }
        foreach (var reason in Enum.GetValues<TacticalFireSkipReason>().Where(r => r != TacticalFireSkipReason.None))
        {
            var entry = new TacticalBattleLogEvent(1, 1, TacticalBattlePhase.Fire, TacticalBattleLogKind.FireSkipped, "actor")
            { SkipReason = reason, FireStepIndex = 0 };
            Assert.Contains("actor", BattleLogFormatter.Format(entry));
            Assert.Contains(BattleFormat.Localize("Skip." + reason), BattleLogFormatter.Format(entry));
        }
    }

    [Fact]
    public async Task MapCardsAndHeaderShareEveryIntermediateStepState()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var results = ResultsPhaseTests.ControlledResults(screen.Flow.State!);
        foreach (var (time, expectedHull) in new[] { (0L, 100), (1275L, 95), (2250L, 80) })
        {
            var projected = results.Project(time);
            var frame = screen.CaptureFrame(1280, 720)! with
            {
                Battle = projected,
                Presentation = time < results.EndMs ? TacticalBattlePresentation.SalvoFlight : TacticalBattlePresentation.SalvoResults,
                Movement = null,
                FirePlan = null,
                Salvo = results.Capture(time)
            };
            Assert.Equal(expectedHull, Assert.Single(TacticalBattleMapRenderer.VisibleUnits(frame)).Hull);
            Assert.Equal(expectedHull, Assert.Single(TacticalBattleSidesTab.Cards(frame)).Unit.Hull);
            Assert.Equal(projected.Units[0].Armor, Assert.Single(TacticalBattleSidesTab.Cards(frame)).Unit.Armor);
            Assert.Contains("0", TacticalBattlePanelHeader.Capture(frame).Losses);
        }
    }

    [Fact]
    public async Task DisabledAndJammedCardsStopOfferingActionsDuringPlayback()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var frame = screen.CaptureFrame(1280, 720)!;
        var own = frame.Battle.Units.First(u => u.Side == frame.Battle.PlayerSide && u.Kind == TacticalUnitKind.Drone);
        foreach (var state in new[] { TacticalUnitState.Air, TacticalUnitState.Disabled, TacticalUnitState.Destroyed })
        {
            var unit = own with { State = state, Jammed = true };
            var playback = frame with { Battle = frame.Battle with { Units = [unit] }, Presentation = TacticalBattlePresentation.SalvoFlight };
            Assert.False(Assert.Single(TacticalBattleSidesTab.Cards(playback)).Selectable);
        }
        Assert.True(TacticalBattleSidesTab.Describe(own, true).Selectable);
    }

    [Fact]
    public async Task PilotCardsShowOwnAndEnemyInitiativeAndPreserveZero()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var units = screen.Flow.State!.Units;
        foreach (var unit in units.Where(u => u.Kind == TacticalUnitKind.Drone))
        {
            foreach (var state in new[] { TacticalUnitState.Air, TacticalUnitState.Disabled, TacticalUnitState.Destroyed })
            {
                var changed = unit with { Initiative = unit.Side == "A" ? 55 : 0, State = state };
                var card = TacticalBattleSidesTab.Describe(changed, unit.Side == "A");
                string label = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    BattleFormat.Localize("Initiative"), changed.Initiative);
                Assert.Contains(label, card.Details);
            }
            Assert.Contains(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                BattleFormat.Localize("Initiative"), "—"), TacticalBattleSidesTab.Describe(unit with { Initiative = null }, true).Details);
        }
        foreach (var carrier in units.Where(u => u.Kind == TacticalUnitKind.Carrier))
            Assert.DoesNotContain(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                BattleFormat.Localize("Initiative"), "—"), TacticalBattleSidesTab.Describe(carrier, true).Details);
    }

    [Fact]
    public async Task PilotCardInitiativeSurvivesLocaleSkinAndSelectionChanges()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        using var skins = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        string restore = Localization.ReadLanguageSetting();
        try
        {
            foreach (string language in new[] { "Russian", "English" })
            {
                Localization.ApplyLanguage(language);
                foreach (string id in new[] { "classic", "contrast" })
                {
                    var themed = new TacticalBattleScreen(screen.Flow, 1.5f, skins.GetOrClassic(id));
                    themed.PanelTabs.Select("sides", notify: true);
                    using var bitmap = new SKBitmap(1024, 768);
                    using var canvas = new SKCanvas(bitmap);
                    themed.Render(canvas, 1024, 768);
                    foreach (var card in TacticalBattleSidesTab.Cards(themed.CaptureFrame(1024, 768)!))
                    {
                        if (card.Unit.Kind != TacticalUnitKind.Drone) continue;
                        Assert.NotNull(card.Unit.Initiative);
                        Assert.Contains((language == "Russian" ? "иниц " : "init ") + card.Unit.Initiative, card.Details);
                    }
                    themed.OnKeyDown(Key.Tab);
                    Assert.Equal(screen.Flow.State!.Units.Select(u => u.Initiative), themed.CaptureFrame(1024, 768)!.Battle.Units.Select(u => u.Initiative));
                }
            }
        }
        finally { Localization.ApplyLanguage(restore); }
    }







    [Fact]
    public void EveryTypedEventHasLocalizedTextAndKnownDetailsAreRetained()
    {
        var result = new TacticalOutcomeSnapshot(TacticalBattleOutcomeCode.Draw,
            [new(TacticalOutcomeReasonCode.NoDrones, "carrier-a"), new(TacticalOutcomeReasonCode.CarrierDestroyed, "carrier-b")],
            4, [], new(0, 0, 0, 0, 0, 0, 0, 0, 0, null, 0, []), 0);
        TacticalBattleLogEvent Event(TacticalBattleLogKind kind) => new(3, 4, TacticalBattlePhase.Fire, kind,
            "actor", "target", "interceptor", 37, 0, Outcome: result, Shot: new(0.625, true),
            Damage: new(37, 9, 2, 15), Unit: new(TacticalUnitKind.Carrier, 15, 55, "drive-name", 0.5));
        foreach (var kind in Enum.GetValues<TacticalBattleLogKind>())
        {
            string text = BattleLogFormatter.Format(Event(kind));
            Assert.DoesNotContain("TacticalBattle.", text);
            Assert.Contains("4", text);
        }
        string hit = BattleLogFormatter.Format(Event(TacticalBattleLogKind.RearKill));
        Assert.Contains("62%", hit);
        Assert.Contains("37", hit);
        Assert.Contains(BattleFormat.Localize("Log.HitRear"), hit);
        string engine = BattleLogFormatter.Format(Event(TacticalBattleLogKind.EngineDamaged));
        Assert.Contains("drive-name", engine);
        Assert.Contains("50%", engine);
        string outcome = BattleLogFormatter.Format(Event(TacticalBattleLogKind.Outcome));
        Assert.Contains("carrier-a", outcome);
        Assert.Contains("carrier-b", outcome);
    }

    private static void SelectTab(TacticalBattleScreen screen, string id)
    {
        var tabs = screen.PanelTabs;
        int index = Array.FindIndex(tabs.Items.ToArray(), t => t.Id == id);
        float width = tabs.Bounds.Width / tabs.Items.Length;
        screen.OnMouseDown(tabs.Bounds.Left + (index + 0.5f) * width, tabs.Bounds.MidY, MouseButton.Left);
    }

    private static async Task<TacticalBattleScreen> Start(BattleNavigationTests.Fixture fixture)
    {
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        return Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
    }

    private static void Save(SKBitmap bitmap, string name)
    {
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is not { Length: > 0 } root) return;
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var file = File.Create(Path.Combine(root, name));
        png.SaveTo(file);
    }
}
