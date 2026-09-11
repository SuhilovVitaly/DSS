using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Dialogue;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.Tests;

public class DialogueIntegrationTests
{
    [Fact]
    public async Task Local_connection_publishes_paused_dialogue_then_paid_docking_and_resumes()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DeepSpaceSaga.sln"))) dir = dir.Parent;
        string settings = Path.Combine(dir!.FullName, "src/DeepSpaceSaga.Client/Settings.json");
        var engine = SimulationEngine.CreateFromSettingsFile(settings);
        engine.SetSpeed(SimulationSpeed.Speed0);
        var save = engine.CaptureSaveState();
        var station = save.GameState.SpaceObjects.First(o => o.ObjectType == SpaceObjectType.Station);
        var player = save.GameState.SpaceObjects.Single(o => o.ObjectId == save.GameState.PlayerShipObjectId);
        var nav = player.Modules!.Single(m => m.ModuleTypeId == "module.bridge.navigation.computer.basic");
        engine.LoadScenario(save with { GameState = save.GameState with { CurrentSpeed = "Speed2",
            SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId == player.ObjectId ? o with
            { PositionX = station.PositionX + 10, PositionY = station.PositionY, SpeedMps = station.SpeedMps, DirectionDegrees = station.DirectionDegrees } : o).ToArray() } });
        await using var connection = new LocalGameSessionConnection(engine);
        await using var handle = new GameSessionHandle(connection);
        var initial = await WaitFor(handle, s => true);
        await handle.SendCommandAsync(player.ObjectId, nav.ModuleId, NavigationComputerCommandTypes.Dock, station.ObjectId);
        var request = await WaitFor(handle, s => s.ActiveDialogue?.CurrentNodeId == "request_ship_id");
        Assert.False(request.Objects.Single(o => o.ObjectId == player.ObjectId).IsDocked);
        Assert.Equal(SimulationSpeed.Speed0, request.CurrentSpeed);
        Assert.Equal(2, request.ActiveDialogue!.Choices.Length);
        Assert.NotNull(request.ActiveDialogue.SpeakerPortraitImage);
        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle);
        Assert.Equal(ScreenEvent.OpenDialogue, screen.ConsumePendingAutoTransition());
        var paused = await WaitFor(handle, s => s.SnapshotSequence > request.SnapshotSequence);
        Assert.Equal(request.GameTimeMs, paused.GameTimeMs);
        await Send(handle, paused, "truthful_id", "truthful");
        var fee = await WaitFor(handle, s => s.ActiveDialogue?.CurrentNodeId == "offer_port_fee");
        await Send(handle, fee, "accept_fee", "pay"); await Send(handle, fee, "accept_fee", "pay");
        var paid = await WaitFor(handle, s => s.ActiveDialogue?.CurrentNodeId == "welcome");
        Assert.Equal(initial.PlayerCredits - 100, paid.PlayerCredits);
        Assert.True(paid.Objects.Single(o => o.ObjectId == player.ObjectId).IsDocked);
        await Send(handle, paid, "continue", "finish");
        var completed = await WaitFor(handle, s => s.ActiveDialogue is null && s.SnapshotSequence > paid.SnapshotSequence);
        Assert.Equal(SimulationSpeed.Speed2, completed.CurrentSpeed);
        Assert.Equal(ScreenEvent.OpenStation, screen.ConsumePendingAutoTransition());
        var resumed = await WaitFor(handle, s => s.SnapshotSequence > completed.SnapshotSequence);
        Assert.True(resumed.GameTimeMs > completed.GameTimeMs);
    }
    private static ValueTask Send(GameSessionHandle handle, AuthoritativeSnapshot snapshot, string choice, string id) =>
        handle.SendDialogueCommandAsync(new(id, DialogueAction.Choose, snapshot.ActiveDialogue!.InstanceId, snapshot.ActiveDialogue.Revision, choice));
    private static async Task<AuthoritativeSnapshot> WaitFor(GameSessionHandle handle, Func<AuthoritativeSnapshot, bool> predicate)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(6));
        while (true)
        {
            if (handle.Buffer.Latest?.Snapshot is { } snapshot && predicate(snapshot)) return snapshot;
            await Task.Delay(20, timeout.Token);
        }
    }
}
