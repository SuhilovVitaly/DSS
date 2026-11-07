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
    public async Task Reference_skin_changes_sides_log_and_forecast_without_changing_visible_facts()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var classic = await Start(fixture);
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var skin = catalog.GetOrClassic("contrast");
        var themed = new TacticalBattleScreen(classic.Flow, classic.UiScale, skin);
        var tabs = new[]
        {
            (Id: "sides", Color: new SKColor(44, 35, 64)),
            (Id: "log", Color: new SKColor(48, 34, 62)),
            (Id: "forecast", Color: new SKColor(46, 41, 73)),
        };
        foreach (var (id, color) in tabs)
        {
            using var baseline = Draw(classic, id);
            using var contrast = Draw(themed, id);
            Assert.Contains(color, contrast.Pixels);
            Assert.DoesNotContain(color, baseline.Pixels);
            Save(contrast, "contrast-" + id + ".png");
            if (id == "sides")
                Assert.Equal(classic.SidesTab.Selectable.ToArray(), themed.SidesTab.Selectable.ToArray());
        }
        Assert.Equal(classic.LogTab.Panel.Text, themed.LogTab.Panel.Text);
        Assert.Equal(classic.ForecastTab.Text, themed.ForecastTab.Text);
        Assert.Same(classic.Flow.State, themed.Flow.State);

        static SKBitmap Draw(TacticalBattleScreen screen, string tab)
        {
            screen.PanelTabs.Select(tab, notify: true);
            var bitmap = new SKBitmap(1280, 720);
            using var canvas = new SKCanvas(bitmap);
            screen.Render(canvas, bitmap.Width, bitmap.Height);
            return bitmap;
        }
    }

    [Fact]
    public async Task OwnSideCardSelectsActualDroneWithoutEditingItsOrder()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        SelectTab(screen, "sides");
        screen.Render(canvas, 1280, 720);
        Assert.Equal("sides", screen.PanelTabs.SelectedId);
        var card = screen.SidesTab.Selectable.First(c => c.Id != screen.Movement.Selected && c.Bounds.MidY < 704);
        screen.OnMouseDown(card.Bounds.MidX, card.Bounds.MidY, MouseButton.Left);
        Assert.Equal(card.Id, screen.Movement.Selected);
        Assert.Empty(screen.BuildMoveRequest().Orders);
        var rows = TacticalBattleSidesTab.Cards(screen.CaptureFrame(1280, 720)!).ToArray();
        Assert.True(rows[0].Own);
        Assert.Equal(TacticalUnitKind.Carrier, rows[0].Unit.Kind);
        Assert.False(rows[0].Selectable);
        Assert.All(rows.Where(r => !r.Own), r => Assert.False(r.Selectable));
        Assert.Contains(rows, r => r.Selectable && r.Unit.Id == card.Id && r.Health.Contains("80/80", StringComparison.Ordinal));
        screen.Render(canvas, 1280, 720);
        Save(bitmap, "battle-sides.png");
    }

    [Fact]
    public async Task LogAndSideCardsDoNotRevealTheAdmittedSalvoUntilArrival()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        for (int i = 0; i < 30 && screen.Flow.State?.Phase == TacticalBattlePhase.Move; i++)
            await fixture.Route(ScreenEvent.MoveTacticalBattle);
        fixture.AdvanceAnimation(TacticalBattleMovementAnimation.FireTransitionDurationMs);
        var before = screen.Flow.State!;
        Assert.Equal(TacticalBattlePhase.Fire, before.Phase);
        fixture.ManualAnimation = true;
        await fixture.Route(ScreenEvent.FireTacticalBattle);
        var accepted = screen.Flow.State!;
        var flight = screen.CaptureFrame(1280, 720)!;
        Assert.Equal(TacticalBattlePresentation.SalvoFlight, flight.Presentation);
        Assert.Equal(before.Log.AsEnumerable(), TacticalBattleLogTab.Visible(flight));
        Assert.Equal(accepted.BeforeSalvoUnits.Select(u => (u.Id, u.Armor, u.Hull, u.State)).OrderBy(u => u.Id),
            TacticalBattleSidesTab.Cards(flight).Select(c => (c.Unit.Id, c.Unit.Armor, c.Unit.Hull, c.Unit.State)).OrderBy(u => u.Id));
        screen.OnKeyDown(Key.Enter);
        var arrived = screen.CaptureFrame(1280, 720)!;
        Assert.Equal(accepted.Log.AsEnumerable(), TacticalBattleLogTab.Visible(arrived));
        Assert.Equal(accepted.Units.Select(u => (u.Id, u.Armor, u.Hull, u.State)).OrderBy(u => u.Id),
            TacticalBattleSidesTab.Cards(arrived).Select(c => (c.Unit.Id, c.Unit.Armor, c.Unit.Hull, c.Unit.State)).OrderBy(u => u.Id));
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        SelectTab(screen, "log");
        screen.Render(canvas, 1280, 720);
        var log = screen.LogTab.Panel;
        Assert.Contains(BattleLogFormatter.Format(accepted.Log.Last()), log.Text);
        Assert.Equal(log.MaximumScroll, log.ScrollOffset);
        Save(bitmap, "battle-journal.png");
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
