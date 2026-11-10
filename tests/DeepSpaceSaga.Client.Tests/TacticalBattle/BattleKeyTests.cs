using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class BattleKeyTests
{
    [Theory]
    [InlineData("Russian", "тактический бой")]
    [InlineData("English", "tactical battle")]
    public async Task DefaultMapHintAndRealWindowF8RouteOpenPausedPreparation(string language, string title)
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        using (Localization.UseLocaleForTests(language))
        {
            Assert.StartsWith("F8", fixture.Game.TacticalBattleHint);
            Assert.Contains(title, fixture.Game.TacticalBattleHint);
            RenderMap(fixture);
            Assert.False(fixture.Game.TacticalBattleEntryBounds.IsEmpty);
        }
        await fixture.PressKey(Key.F8);
        var setup = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        Assert.True(setup.IsSetup);
        await setup.Flow.OptionsTask!;
        Assert.True(setup.Flow.Options!.Availability.Available);
        Assert.Equal(SimulationSpeed.Speed0, fixture.Engine.CurrentSpeed);
        Assert.Equal(0, fixture.Proxy.Starts);
    }

    [Fact]
    public async Task RemappedKeyOpensOnlyOnConfiguredPhysicalEdge()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create(TacticalBattleInputSettings.Default with { TacticalBattle = Key.F7 });
        Assert.StartsWith("F7", fixture.Game.TacticalBattleHint);
        await fixture.PressKey(Key.F8);
        Assert.Same(fixture.Game, fixture.Screens.Current);
        await fixture.PressKey(Key.F7);
        Assert.True(Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current).IsSetup);
    }

    [Fact]
    public async Task ConflictFallbackStillOffersClickableLaunchInScaledMapWithoutFalseF8Hint()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create(TacticalBattleInputSettings.Default with { ShortcutAvailable = false }, uiScale: 1.2f);
        Assert.DoesNotContain("F8", fixture.Game.TacticalBattleHint);
        await fixture.PressKey(Key.F8);
        Assert.Same(fixture.Game, fixture.Screens.Current);
        RenderMap(fixture);
        var rect = fixture.Game.TacticalBattleEntryBounds;
        Assert.False(rect.IsEmpty);
        var toolbar = fixture.Game.MapToolbarRect;
        Assert.NotEqual(ScreenEvent.OpenTacticalBattle,
            fixture.Game.OnMouseDown(toolbar.MidX * fixture.Game.UiScale, (toolbar.Top - 16) * fixture.Game.UiScale, MouseButton.Left));
        float x = rect.MidX * fixture.Game.UiScale, y = rect.MidY * fixture.Game.UiScale;
        Assert.True(fixture.Game.OnMouseMove(x, y));
        var click = fixture.Game.OnMouseDown(x, y, MouseButton.Left);
        Assert.Equal(ScreenEvent.OpenTacticalBattle, click);
        await fixture.Route(click);
        Assert.True(Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current).IsSetup);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.2f)]
    public async Task BattleEntryStaysInBottomRightAfterResize(float scale)
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create(uiScale: scale);
        foreach (var (width, height) in new[] { (1280, 720), (720, 540) })
        {
            RenderMap(fixture, width, height);
            var game = fixture.Game;
            var entry = game.TacticalBattleEntryBounds;
            Assert.False(entry.IsEmpty);
            Assert.InRange(width / scale - entry.Right, 7.99f, 8.01f);
            Assert.InRange(height / scale - entry.Bottom, 7.99f, 8.01f);
            Assert.False(entry.IntersectsWith(game.MapToolbarRect));
            foreach (var (rect, expected) in new[] { (game.LastFinanceButtonRect, ScreenEvent.OpenFinance), (game.LastShipButtonRect, ScreenEvent.OpenShip) })
            {
                Assert.False(entry.IntersectsWith(rect));
                Assert.Equal(expected, game.OnMouseDown(rect.MidX * scale, rect.MidY * scale, MouseButton.Left));
            }
            Assert.True(game.OnMouseMove(entry.MidX * scale, entry.MidY * scale));
        }
    }

    [Fact]
    public async Task UnavailableDataStillOpensShowsStructuredReasonAndHasNoStartActionOrButton()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        fixture.Proxy.OptionsOverride = new(new(false, new("TacticalBattle/rules.json", "version", "invalid version")), [], [], [], [], []);
        long logLength = File.Exists(InterfaceLog.FileName) ? new FileInfo(InterfaceLog.FileName).Length : 0;
        await fixture.PressKey(Key.F8);
        var setup = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        Assert.False(setup.Flow.Options!.Availability.Available);
        Assert.Contains("TacticalBattle/rules.json", setup.Flow.Error);
        Assert.Contains("invalid version", setup.Flow.Error);
        string log = LogTailReader.ReadTail(logLength);
        Assert.Contains("TACTICAL unavailable:", log);
        Assert.Contains("TacticalBattle/rules.json", log);
        Assert.False(setup.CanStart);
        Assert.Equal(ScreenEvent.None, setup.OnKeyDown(Key.Enter));
        using var bitmap = new SKBitmap(1280, 720); using var canvas = new SKCanvas(bitmap);
        setup.Render(canvas, 1280, 720);
        Assert.Equal(bitmap.GetPixel(0, 0), bitmap.GetPixel(50, 680));
        Assert.Equal(ScreenEvent.None, setup.OnMouseDown(50, 680, MouseButton.Left));
        await fixture.Route(ScreenEvent.StartTacticalBattle); // even a queued event must not submit
        Assert.Equal(0, fixture.Proxy.Starts);
        await fixture.Route(setup.OnKeyDown(Key.Escape));
        Assert.Same(fixture.Game, fixture.Screens.Current);
        Assert.Equal(SimulationSpeed.Speed2, fixture.Engine.CurrentSpeed);
    }

    private static void RenderMap(BattleNavigationTests.Fixture fixture, int width = 1280, int height = 720)
    {
        using var bitmap = new SKBitmap(width, height); using var canvas = new SKCanvas(bitmap);
        fixture.Game.Render(canvas, width, height);
    }
}
