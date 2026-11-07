using System.Reflection;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class KeysDuringBattleTests
{
    [Theory]
    [InlineData(Key.F5)]
    [InlineData(Key.F9)]
    [InlineData(Key.F12)]
    [InlineData(Key.F8)]
    [InlineData(Key.I)]
    [InlineData(Key.F, Key.ControlLeft)]
    [InlineData(Key.S, Key.ControlRight)]
    [InlineData(Key.Space)]
    [InlineData(Key.Number1)]
    [InlineData(Key.Number2)]
    [InlineData(Key.Number3)]
    [InlineData(Key.Number4)]
    public async Task WorldKeyShowsNoticeOnBothScreensThroughWindowPolling(Key key, Key modifier = Key.Unknown)
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var keyboard = DispatchProxy.Create<IKeyboard, KeyboardRoutingTests.KeyboardProxy>();
        SetField(fixture.Window, "_keyboard", keyboard);
        var down = ((KeyboardRoutingTests.KeyboardProxy)(object)keyboard).Down;
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        Assert.Equal("", setup.BlockedNotice);
        Press(key);
        Assert.Equal(BattleFormat.Localize("WorldActionBlocked"), setup.BlockedNotice);
        Assert.Same(setup, fixture.Screens.Current);
        Assert.Null(fixture.Engine.GetTacticalBattleState());

        await fixture.Route(ScreenEvent.StartTacticalBattle);
        var battle = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        var before = battle.Flow.State;
        Assert.Equal("", battle.BlockedNotice);
        Press(key);
        Assert.Equal(BattleFormat.Localize("WorldActionBlocked"), battle.BlockedNotice);
        Assert.Same(battle, fixture.Screens.Current);
        Assert.Same(before, battle.Flow.State);
        Assert.Equal(new[] { SimulationSpeed.Speed0 }, fixture.Proxy.Speeds.ToArray());

        void Press(Key value)
        {
            if (modifier != Key.Unknown) down.Add(modifier);
            down.Add(value);
            Poll(fixture.Window);
            down.Clear();
            Poll(fixture.Window);
        }
    }

    [Fact]
    public async Task StaleWorldEventsCannotSwapSessionAndEscapeStillRequiresConfirmation()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        ScreenEvent[] staleEvents =
        [
            ScreenEvent.QuickSave, ScreenEvent.QuickLoad, ScreenEvent.OpenGameMenu, ScreenEvent.OpenSaveWindow,
            ScreenEvent.OpenLoadWindow, ScreenEvent.NewGame, ScreenEvent.MainMenu, ScreenEvent.Resume,
            ScreenEvent.OpenFinance, ScreenEvent.OpenShip, ScreenEvent.OpenTacticalBattle
        ];
        foreach (var evt in staleEvents) await fixture.Route(evt);
        Assert.Same(setup, fixture.Screens.Current);
        Assert.Equal(BattleFormat.Localize("WorldActionBlocked"), setup.BlockedNotice);
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        var battle = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        foreach (var evt in staleEvents) await fixture.Route(evt);
        Assert.Same(battle, fixture.Screens.Current);
        Assert.True(fixture.Engine.IsTacticalBattleActive);
        Assert.Equal(1, fixture.Depth);
        await fixture.PressKey(Key.Escape);
        Assert.True(battle.ConfirmExit);
        await fixture.PressKey(Key.Escape);
        Assert.False(battle.ConfirmExit);
        await fixture.PressKey(Key.Escape);
        await fixture.PressKey(Key.Enter);
        Assert.Same(fixture.Game, fixture.Screens.Current);
        Assert.False(fixture.Engine.IsTacticalBattleActive);
        Assert.Equal(new[] { SimulationSpeed.Speed0, SimulationSpeed.Speed2 }, fixture.Proxy.Speeds.ToArray());
    }

    [Fact]
    public async Task F10CapturesTheTacticalFrameWithoutChangingTheSession()
    {
        // The validation runner supplies an absolute artifact directory on both platforms.
        if (Path.GetDirectoryName(InterfaceLog.FileName) is not { Length: > 0 } root) return;
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        var battle = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        var state = battle.Flow.State;
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        battle.Render(surface.Canvas, 1280, 720);
        surface.Canvas.Flush();
        string directory = Path.Combine(root, "Screenshots");
        string[] existing = Directory.Exists(directory) ? Directory.GetFiles(directory) : [];
        SetField(fixture.Window, "_surface", surface);
        try { await fixture.PressKey(Key.F10); }
        finally { SetField(fixture.Window, "_surface", null); }
        string created = Assert.Single(Directory.GetFiles(directory).Except(existing));
        using var captured = SKBitmap.Decode(created);
        using var expected = surface.Snapshot();
        Assert.Equal((expected.Width, expected.Height), (captured.Width, captured.Height));
        Assert.Equal(battle, fixture.Screens.Current);
        Assert.Same(state, battle.Flow.State);
    }

    private static void SetField(SkiaWindow window, string name, object? value) =>
        typeof(SkiaWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, value);

    private static void Poll(SkiaWindow window) =>
        typeof(SkiaWindow).GetMethod("PollKeyboard", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null);
}
