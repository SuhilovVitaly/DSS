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
        fixture.AdvanceAnimation(1000);
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
