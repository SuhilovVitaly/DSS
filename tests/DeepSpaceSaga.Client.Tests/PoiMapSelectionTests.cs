using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PoiMapSelectionTests
{
    private static AuthoritativeSnapshot Snapshot() => new(1, 0, SimulationSpeed.Speed0,
        [new("ship", 0, 0, 0, 0, RenderObjectType: "PlayerShip"), new("station", 200, 0, 0, 0, RenderObjectType: "Station")], "ship",
        InstalledModules: [new("nav", "nav", "Navigation", 0, [NavigationComputerCommandTypes.Dock], "On", "Ready", 100,
            Commands: [new(NavigationComputerCommandTypes.Dock, "Dock", "object")])],
        AiMap: new(1, [], Fields: [new("field", "Debris", .5, "Parent", "station", null, 0, 0, 0, 300, 0, 360, 1)],
            PointsOfInterest: [new("poi", "Ретранслятор", "Неактивный узел связи.", "station", null, 100, 0)]));

    [Fact]
    public void PoiSelectedShowsNameAndDescription()
    {
        var buffer = new SnapshotBuffer(); buffer.Update(Snapshot()); var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080); screen.OnMouseDown(960, 540); screen.OnMouseUp(960, 540);
        screen.OnMouseDown(1260, 540); screen.OnMouseUp(1260, 540); screen.Render(canvas, 1920, 1080);
        Assert.Equal("poi", screen.SelectedPoiId); Assert.Equal("ship", screen.SelectedObjectId); Assert.Null(screen.SelectedFieldId);
        var lines = ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo);
        Assert.Contains(lines, l => l.Value == "Ретранслятор"); Assert.Contains(lines, l => l.Value == "Неактивный узел связи.");
        Assert.Contains(lines, l => l.Value == AiMapPresentation.PoiNotice); Assert.DoesNotContain(lines, l => l.Label == "Speed");
        Assert.False(screen.IsModuleCommandEnabled(NavigationComputerCommandTypes.Dock));
        screen.OnMouseDown(1160, 540); screen.OnMouseUp(1160, 540);
        Assert.Null(screen.SelectedPoiId); Assert.Equal("station", screen.SelectedObjectId);
        Assert.True(screen.IsModuleCommandEnabled(NavigationComputerCommandTypes.Dock));
        var moved = Assert.Single(AiMapPresentation.Points(Snapshot(), [new("station", 400, 50, 0, 0)], 0));
        Assert.Equal(500, moved.X); Assert.Equal(50, moved.Y);
        var orbitSnapshot = Snapshot() with { AiMap = new(1, [], PointsOfInterest: [new("orbit-poi", "O", "D", null, new(100, 100, 30000, 0, 0, 0, "clockwise"), 5, -2)]) };
        var orbit = Assert.Single(AiMapPresentation.Points(orbitSnapshot, [], 25));
        Assert.Equal(105, orbit.X, 6); Assert.Equal(-2, orbit.Y, 6);
    }

    [Fact]
    public void ResourcesAreNotDuplicated()
    {
        using var engine = SimulationEngine.CreateFromSettingsFile(DefaultSystemContentTests.Settings);
        var snapshot = engine.CaptureSnapshot(); string before = JsonSerializer.Serialize(snapshot);
        Assert.NotEmpty(snapshot.AiMap!.PointsOfInterest);
        var buffer = new SnapshotBuffer(); buffer.Update(snapshot); var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        for (int i = 0; i < 3; i++) screen.Render(canvas, 1920, 1080);
        Assert.NotEmpty(snapshot.ClusterMap!.ResourceBindings);
        foreach (var binding in snapshot.ClusterMap.ResourceBindings)
        {
            var resource = Assert.Single(snapshot.Objects, o => o.ObjectId == binding.FieldId);
            Assert.True(resource.Survey!.CompositionKnown);
            Assert.Equal(binding, ClusterMapPresentation.Resource(snapshot, resource.ObjectId));
            Assert.DoesNotContain(snapshot.AiMap.PointsOfInterest, p => p.ObjectId == resource.ObjectId);
        }
        Assert.Equal(before, JsonSerializer.Serialize(snapshot));
    }

    [Fact]
    public async Task PoiSelectionSendsNoExplorationOrTrade()
    {
        var connection = new RecordingConnection(); await using var handle = new GameSessionHandle(connection);
        handle.Buffer.Update(Snapshot()); var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle);
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        for (int i = 0; i < 3; i++) { screen.OnMouseDown(1260, 540); screen.OnMouseUp(1260, 540); screen.Render(canvas, 1920, 1080); }
        Assert.Equal("poi", screen.SelectedPoiId); Assert.False(screen.IsModuleCommandEnabled(NavigationComputerCommandTypes.Dock));
        foreach (var button in screen.CommandsPanel.AllCommandButtons.Where(b => b.CommandTypeId == NavigationComputerCommandTypes.Dock))
        { Assert.False(button.Enabled); screen.OnMouseDown(button.Rect.MidX, button.Rect.MidY); screen.OnMouseUp(button.Rect.MidX, button.Rect.MidY); }
        Assert.Equal(0, connection.Actions); Assert.DoesNotContain(connection.Interactions, p => p.Active == "poi" || p.Selected == "poi");
    }

    private sealed class RecordingConnection : IGameSessionConnection
    {
        public int Actions { get; private set; }
        public List<(string? Active, string? Selected)> Interactions { get; } = [];
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) { Actions++; return ValueTask.CompletedTask; }
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) { Actions++; return ValueTask.CompletedTask; }
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) { Actions++; return ValueTask.CompletedTask; }
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default)
        { Interactions.Add((activeObjectId, selectedObjectId)); return ValueTask.CompletedTask; }
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) { Actions++; return ValueTask.CompletedTask; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        { await Task.CompletedTask; yield break; }
    }
}
