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
        float x = rect.MidX * fixture.Game.UiScale, y = rect.MidY * fixture.Game.UiScale;
        Assert.True(fixture.Game.OnMouseMove(x, y));
        var click = fixture.Game.OnMouseDown(x, y, MouseButton.Left);
        Assert.Equal(ScreenEvent.OpenTacticalBattle, click);
        await fixture.Route(click);
        Assert.True(Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current).IsSetup);
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

    private static void RenderMap(BattleNavigationTests.Fixture fixture)
    {
        using var bitmap = new SKBitmap(1280, 720); using var canvas = new SKCanvas(bitmap);
        fixture.Game.Render(canvas, 1280, 720);
    }
}
