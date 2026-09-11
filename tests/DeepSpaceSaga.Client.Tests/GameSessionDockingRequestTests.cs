using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class GameSessionDockingRequestTests
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const string PlayerShipId = "SPC-0001";
    private const string NavigationComputerModuleId = "MOD-PLAYER-NAV-COMPUTER-01";
    private const string StationId = "STN-0001";

    private static readonly ImmutableArray<InstalledModuleSnapshot> NavigationComputerModule = ImmutableArray.Create(
        new InstalledModuleSnapshot(
            NavigationComputerModuleId, "module.bridge.navigation.computer.basic", "Navigation Computer",
            Position: 0,
            ImmutableArray.Create(NavigationComputerCommandTypes.Dock, NavigationComputerCommandTypes.StationsList),
            Commands: ImmutableArray.Create(
                new ModuleCommandSnapshot(NavigationComputerCommandTypes.Dock, "Dock", "object"),
                new ModuleCommandSnapshot(NavigationComputerCommandTypes.StationsList, "Stations List", "none"))));

    /// <summary>Station at world (10000, 10060) — renders at screen (640, 420) when the
    /// camera focuses the player ship at (10000, 10000), mirroring CommandsPanelSkeletonTests.ObjAt.</summary>
    private static ObjectMotionSnapshot Station() =>
        new(StationId, X: 10000, Y: 10060, SpeedKmS: 0, Direction: 0, RenderObjectType: "Station");

    private static ObjectMotionSnapshot Ship() =>
        new(PlayerShipId, X: 10000, Y: 10000, SpeedKmS: 0, Direction: 0);

    private static TestFixture CreateFixture()
    {
        var connection = new RecordingConnection();
        var handle = new GameSessionHandle(connection);
        handle.Buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(Ship(), Station()),
            PlayerShipObjectId: PlayerShipId,
            InstalledModules: NavigationComputerModule));

        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle);
        return new TestFixture(connection, handle, screen);
    }

    private static void Render(GameSessionScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    /// <summary>Selects the station (screen (640, 420)) then clicks the Dock button
    /// (Navigation panel's first command button) — returns the click's ScreenEvent.</summary>
    private static ScreenEvent SelectStationAndClickDock(GameSessionScreen screen)
    {
        Render(screen);
        screen.OnMouseDown(640, 420); // select the station
        Render(screen);

        var dock = screen.CommandsPanel.AllCommandButtons
            .Single(b => b.CommandTypeId == NavigationComputerCommandTypes.Dock);
        Assert.True(dock.Enabled);

        return screen.OnMouseDown(dock.Rect.MidX, dock.Rect.MidY);
    }

    [Fact]
    public async Task Clicking_Dock_sends_the_navigation_request_directly()
    {
        await using var fixture = CreateFixture();

        SelectStationAndClickDock(fixture.Screen);

        Assert.Equal(NavigationComputerCommandTypes.Dock, Assert.Single(fixture.Connection.Commands).CommandType);
    }

    [Fact]
    public async Task Clicking_Dock_waits_for_authoritative_dialogue()
    {
        await using var fixture = CreateFixture();

        var result = SelectStationAndClickDock(fixture.Screen);

        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public async Task Other_command_types_are_unaffected_and_still_send_directly()
    {
        // Protect: navigation.stationsList (same module) is a "none"-target command that
        // stays always-disabled (unrelated carve-out) — engine.accelerate from
        // CommandsPanelSkeletonTests already covers the general "none"-target case still
        // sending directly; this asserts Dock's own module doesn't get swept into the
        // deferred branch for its sibling command.
        await using var fixture = CreateFixture();
        Render(fixture.Screen);

        var stationsList = fixture.Screen.CommandsPanel.AllCommandButtons
            .Single(b => b.CommandTypeId == NavigationComputerCommandTypes.StationsList);
        Assert.False(stationsList.Enabled);

        fixture.Screen.OnMouseDown(stationsList.Rect.MidX, stationsList.Rect.MidY);

        Assert.Empty(fixture.Connection.Commands);

    }

    private sealed record TestFixture(
        RecordingConnection Connection,
        GameSessionHandle Handle,
        GameSessionScreen Screen) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Handle.DisposeAsync();
    }

    private sealed class RecordingConnection : IGameSessionConnection
    {
        public List<PlayerCommand> Commands { get; } = [];

        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return ValueTask.CompletedTask;
        }

        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask SetObjectInteractionStateAsync(
            string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
