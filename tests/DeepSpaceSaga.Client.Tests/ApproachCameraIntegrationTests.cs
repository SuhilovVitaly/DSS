using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class ApproachCameraIntegrationTests
{
    [Fact]
    public async Task Approach_can_be_retargeted_after_pan_with_player_offscreen()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "DeepSpaceSaga.sln"))) root = root.Parent;
        using var engine = SimulationEngine.CreateFromSettingsFile(Path.Combine(root!.FullName, "src/DeepSpaceSaga.Client/Settings.json"));
        engine.SetSpeed(SimulationSpeed.Speed0);
        var save = engine.CaptureSaveState();
        var ship = save.GameState.SpaceObjects.Single(o => o.ObjectId == save.GameState.PlayerShipObjectId);
        var asteroid = save.GameState.SpaceObjects.First(o => o.ObjectType == SpaceObjectType.Asteroid);
        engine.LoadScenario(save with { GameState = save.GameState with {
            CurrentSpeed = "Speed0",
            TradingMap = null,
            SpaceObjects = [
                ship with { PositionX = 10000, PositionY = 10000, SpeedMps = 700, DirectionDegrees = 0 },
                asteroid with { ObjectId = "TARGET-A", PositionX = 10000, PositionY = 10100, SpeedMps = 1069, DirectionDegrees = 57 },
                asteroid with { ObjectId = "TARGET-B", PositionX = 11500, PositionY = 10100, SpeedMps = 1911, DirectionDegrees = 110 },
                asteroid with { ObjectId = "TARGET-C", PositionX = 11300, PositionY = 10000, SpeedMps = 1650, DirectionDegrees = 91 }
            ] } });
        var connection = new Connection(engine);
        await using var handle = new GameSessionHandle(connection);
        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        const long simulationTime = 0;
        void Refresh()
        {
            handle.Buffer.Update(engine.CaptureSnapshotForTests(simulationTime, SimulationSpeed.Speed0));
            screen.Render(canvas, bitmap.Width, bitmap.Height);
        }
        void Click(float x, float y)
        {
            screen.OnMouseMove(x, y); screen.OnMouseDown(x, y); screen.OnMouseUp(x, y);
        }
        void Approach(string id, float x, float y)
        {
            Click(x, y);
            Assert.Equal(id, screen.SelectedObjectId);
            screen.Render(canvas, bitmap.Width, bitmap.Height);
            var button = Assert.Single(screen.CommandsPanel.AllCommandButtons, b => b.CommandTypeId == NavigationComputerCommandTypes.Approach);
            Assert.True(button.Enabled);
            Click(button.Rect.MidX, button.Rect.MidY);
            Refresh();
            var player = handle.Buffer.Latest!.Snapshot.Objects.Single(o => o.ObjectId == ship.ObjectId);
            Assert.Equal(id, player.NavigationTargetObjectId);
            Assert.NotNull(player.ApproachRoute);
            Assert.True(screen.GetNavigationTrajectory(ship.ObjectId).Count > 1);
        }
        Refresh();
        Approach("TARGET-A", 640, 460);
        screen.OnMouseDown(900, 530);
        screen.OnMouseMove(-300, 530);
        screen.OnMouseUp(-300, 530);
        Refresh();
        Assert.False(screen.IsFocusAttachedToPlayer);
        Approach("TARGET-B", 940, 460);
        Approach("TARGET-C", 740, 360);
        Assert.False(screen.IsFocusAttachedToPlayer);
    }

    [Fact]
    public void Approach_repeated_in_full_scenario_after_speed100_sized_steps()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "DeepSpaceSaga.sln"))) root = root.Parent;
        using var engine = SimulationEngine.CreateFromScenarioFile(Path.Combine(root!.FullName, "src/DeepSpaceSaga.Client/Settings.json"), Path.Combine(root.FullName, "src/DeepSpaceSaga.Client/Scenarios/Default_500/scenario.json"));
        engine.SetSpeed(SimulationSpeed.Speed0);
        for (int i = 0; i < 30; i++)
        {
            var snapshot = engine.CaptureSnapshotForTests(i * 100_000, SimulationSpeed.Speed4);
            var ship = snapshot.Objects.Single(o => o.ObjectId == snapshot.PlayerShipObjectId);
            var target = snapshot.Objects.Where(o => o.ObjectId.StartsWith("AST-")).OrderBy(o => o.ObjectId).ElementAt(i * 7);
            var module = snapshot.InstalledModules.First(m => m.CommandTypeIds.Contains(NavigationComputerCommandTypes.Approach));
            engine.ReceiveCommand(new("check-" + i, (ulong)i + 1, ship.ObjectId, module.ModuleId, NavigationComputerCommandTypes.Approach, TargetObjectId: target.ObjectId));
            var updated = engine.CaptureSnapshotForTests(i * 100_000, SimulationSpeed.Speed0);
            Assert.NotNull(updated.Objects.Single(o => o.ObjectId == ship.ObjectId).ApproachRoute);
        }
    }

    [Fact]
    public async Task Long_approach_survives_delayed_speed100_snapshot_without_event_overflow()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "DeepSpaceSaga.sln"))) root = root.Parent;
        using var engine = SimulationEngine.CreateFromSettingsFile(Path.Combine(root!.FullName, "src/DeepSpaceSaga.Client/Settings.json"));
        engine.SetSpeed(SimulationSpeed.Speed0);
        var save = engine.CaptureSaveState();
        var ship = save.GameState.SpaceObjects.Single(o => o.ObjectId == save.GameState.PlayerShipObjectId);
        var target = save.GameState.SpaceObjects.First(o => o.ObjectType == SpaceObjectType.Asteroid);
        engine.LoadScenario(save with { GameState = save.GameState with { TradingMap = null, SpaceObjects = [
            ship with { PositionX = 0, PositionY = 0, SpeedMps = 700, DirectionDegrees = 0 },
            target with { PositionX = 200000, PositionY = 0, SpeedMps = 100, DirectionDegrees = 90 },
            target with { ObjectId = "NEXT-TARGET", PositionX = 200000, PositionY = 200000, SpeedMps = 1069, DirectionDegrees = 57 }
        ] } });
        var initial = engine.CaptureSnapshotForTests();
        var module = initial.InstalledModules.First(m => m.CommandTypeIds.Contains(NavigationComputerCommandTypes.Approach));
        engine.ReceiveCommand(new("long-approach", 1, ship.ObjectId, module.ModuleId, NavigationComputerCommandTypes.Approach, TargetObjectId: target.ObjectId));
        var started = engine.CaptureSnapshotForTests();
        var route = started.Objects.Single(o => o.ObjectId == ship.ObjectId).ApproachRoute!;
        var mailbox = new SnapshotMailbox();
        mailbox.Publish(started);
        // Eleven real seconds at x100: 4,400 internal steering checkpoints.
        var delayed = engine.CaptureSnapshotForTests(1_100_000, SimulationSpeed.Speed4);
        Assert.NotNull(delayed.Objects.Single(o => o.ObjectId == ship.ObjectId).ApproachRoute);
        mailbox.Publish(delayed);
        mailbox.Complete();
        await foreach (var received in mailbox.ReadAllAsync(default))
            Assert.Equal(delayed.SnapshotSequence, received.SnapshotSequence);
        Assert.DoesNotContain(delayed.ShipEvents, e => e.EventType == ShipEventTypes.CommandCompleted);
        var completed = engine.CaptureSnapshotForTests((long)Math.Ceiling(route.DurationMs), SimulationSpeed.Speed4);
        Assert.Single(completed.ShipEvents, e => e.EventType == ShipEventTypes.CommandCompleted);
        Assert.Single(completed.CommandResults, r => r.CommandId == "long-approach" && r.Status == CommandResultStatus.Executed);
        engine.ReceiveCommand(new("next-approach", 2, ship.ObjectId, module.ModuleId,
            NavigationComputerCommandTypes.Approach, TargetObjectId: "NEXT-TARGET"));
        var next = engine.CaptureSnapshotForTests((long)Math.Ceiling(route.DurationMs), SimulationSpeed.Speed0);
        var retargeted = next.Objects.Single(o => o.ObjectId == ship.ObjectId);
        Assert.Equal("NEXT-TARGET", retargeted.NavigationTargetObjectId);
        Assert.NotNull(retargeted.ApproachRoute);
    }

    private sealed class Connection(SimulationEngine engine) : IGameSessionConnection
    {
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default)
        { engine.ReceiveCommand(command); return ValueTask.CompletedTask; }
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default)
        { engine.SetObjectInteractionState(activeObjectId, selectedObjectId); return ValueTask.CompletedTask; }
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        { await Task.Delay(Timeout.Infinite, cancellationToken); yield break; }
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
