using System.Collections.Immutable;
using System.Reflection;
using DeepSpaceSaga.Client.Tests.TacticalBattle;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Load;
using DeepSpaceSaga.Client.UI.Screens.ShipDestroyed;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public sealed class ShipDestroyedScreenTests
{
    [Fact]
    public async Task DestroyedPlayerShipReplacesTheWorldWithTheTerminalScreen()
    {
        await using var f = await BattleNavigationTests.Fixture.Create();
        Assert.False(f.Handle.IsPlayerShipDestroyed);
        PresentDestroyedPlayer(f);
        Assert.True(f.Handle.IsPlayerShipDestroyed);

        Poll(f.Window);
        await (Task)Field<Task>(f.Window, "_shipDestroyedTransitionTask");

        Assert.IsType<ShipDestroyedScreen>(f.Screens.Current);
        Assert.Equal(1, f.Screens.Count);
        Assert.Equal(0, f.Depth);
    }

    [Fact]
    public async Task TerminalScreenBlocksSaveSpeedAndWorldEventsButOffersLoad()
    {
        await using var f = await BattleNavigationTests.Fixture.Create();
        PresentDestroyedPlayer(f);
        await f.Route(ScreenEvent.None);
        int speedsBefore = f.Proxy.Speeds.Count;

        await f.Route(ScreenEvent.QuickSave);
        await f.Route(ScreenEvent.OpenSaveWindow);
        await f.Route(ScreenEvent.OpenGameMenu);
        await f.Route(ScreenEvent.OpenTacticalBattle);
        await f.Handle.SetSpeedAsync(SimulationSpeed.Speed2);
        Assert.IsType<ShipDestroyedScreen>(f.Screens.Current);
        Assert.Equal(SimulationSpeed.Speed0, f.Handle.Buffer.CurrentSpeed);
        Assert.Equal(speedsBefore, f.Proxy.Speeds.Count);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await f.Handle.SaveAsync("slot"));

        await f.Route(ScreenEvent.OpenLoadWindow);
        Assert.IsType<LoadScreen>(f.Screens.Current);
        await f.Route(ScreenEvent.CloseLoadWindow);
        Assert.IsType<ShipDestroyedScreen>(f.Screens.Current);
        Assert.Equal(1, f.Screens.Count);
    }

    [Fact]
    public void ScreenOffersLoadAndMainMenuByKeyboardAndMouse()
    {
        var screen = new ShipDestroyedScreen();
        screen.OnActivated();
        using var surface = SKSurface.Create(new SKImageInfo(1280, 720));
        screen.Render(surface.Canvas, 1280, 720);

        Assert.Equal(ScreenEvent.OpenLoadWindow, screen.OnKeyDown(Key.Enter));
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Tab));
        Assert.Equal(ScreenEvent.MainMenu, screen.OnKeyDown(Key.Space));
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Escape));
        var load = screen.Layout.Load;
        var menu = screen.Layout.MainMenu;
        Assert.Equal(ScreenEvent.OpenLoadWindow, screen.OnMouseDown(load.MidX, load.MidY, MouseButton.Left));
        Assert.Equal(ScreenEvent.MainMenu, screen.OnMouseDown(menu.MidX, menu.MidY, MouseButton.Left));
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(menu.MidX, menu.MidY, MouseButton.Right));
    }

    [Theory]
    [InlineData("Russian.json")]
    [InlineData("English.json")]
    public void BothLocalesNameTheScreenAndItsButtons(string file)
    {
        string text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "Locale", file));
        foreach (string key in new[] { "ShipDestroyed.Title", "ShipDestroyed.Load", "ShipDestroyed.MainMenu" })
            Assert.Contains($"\"{key}\":", text);
    }

    /// <summary>Publishes a newer accepted snapshot in which the player ship is destroyed.</summary>
    private static void PresentDestroyedPlayer(BattleNavigationTests.Fixture f)
    {
        var latest = f.Handle.Buffer.Latest!.Snapshot;
        f.Handle.Buffer.Update(latest with
        {
            SnapshotSequence = latest.SnapshotSequence + 1_000_000,
            Objects = latest.Objects.Select(o => o.ObjectId == latest.PlayerShipObjectId ? o with { IsDestroyed = true } : o).ToImmutableArray()
        });
    }

    private static void Poll(SkiaWindow window) =>
        typeof(SkiaWindow).GetMethod("PollGameSessionAutoTransition", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null);

    private static T Field<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
}
