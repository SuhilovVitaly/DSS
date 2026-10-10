using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class MapLayerControlsTests
{
    private static AuthoritativeSnapshot Overlap() => new(1, 0, SimulationSpeed.Speed0,
        [new("ship", 0, 0, 0, 0, RenderObjectType: "PlayerShip"), new("base", 100, 0, 0, 0, RenderObjectType: "Station")], "ship",
        InstalledModules: [new("nav", "nav", "Navigation", 0, [NavigationComputerCommandTypes.Dock], "On", "Ready", 100,
            Commands: [new(NavigationComputerCommandTypes.Dock, "Dock", "object")])],
        SolarSystemMap: new(1, 1, 1000, [], [], []),
        AiMap: new(1, [new("base", "Orbital", "Ai", null, new(100, 100, 30000, 0, 90, 0, "clockwise"), 0, 0)],
            [new("territory", "base", 20, 40)], [new("field", "Debris", .75, "Parent", "base", null, 0, 0, 0, 300, 0, 360, 1)],
            [new("poi", "Relay", "Inactive relay", "base", null, 0, 0)]));

    private static void Click(GameSessionScreen screen, SKRect button, float scale = 1)
    { screen.OnMouseDown(button.MidX * scale, button.MidY * scale); screen.OnMouseUp(button.MidX * scale, button.MidY * scale); }

    [Fact]
    public async Task ToggleAllLayersPreservesWorldAndQuotes()
    {
        var connection = new QuietConnection(); await using var handle = new GameSessionHandle(connection);
        handle.Buffer.Update(Overlap()); var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle);
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080); screen.OnMouseDown(1060, 540); screen.OnMouseUp(1060, 540);
        string before = JsonSerializer.Serialize(handle.Buffer.Latest!.Snapshot);
        foreach (int index in new[] { 5, 8, 9, 10 }) Click(screen, screen.MapViewButtonRects[index]);
        screen.Render(canvas, 1920, 1080); Assert.Equal((MapLayerFlags)0, screen.MapLayers); Assert.Equal("base", screen.SelectedObjectId);
        screen.OnMouseDown(1060, 540); screen.OnMouseUp(1060, 540);
        Assert.Null(screen.SelectedFieldId); Assert.Null(screen.SelectedPoiId); Assert.Equal("base", screen.SelectedObjectId);
        foreach (int index in new[] { 5, 8, 9, 10 }) Click(screen, screen.MapViewButtonRects[index]);
        Assert.Equal(MapLayerFlags.All, screen.MapLayers); Assert.Equal(before, JsonSerializer.Serialize(handle.Buffer.Latest!.Snapshot));
        Assert.Equal(0, connection.Actions);
    }

    [Fact]
    public void OverlapSelectionAndControlsAtAllScales()
    {
        foreach (int width in new[] { 1280, 1920 })
            foreach (float scale in new[] { 1f, 1.2f, 1.5f })
            {
                int height = width * 9 / 16;
                var buffer = new SnapshotBuffer(); buffer.Update(Overlap()); var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale);
                using var bitmap = new SKBitmap(width, height); using var canvas = new SKCanvas(bitmap);
                screen.Render(canvas, width, height);
                var free = screen.AvailableMapRect(); float pickX = free.MidX, pickY = free.MidY;
                var snapshot = Overlap();
                buffer = new SnapshotBuffer(); buffer.Update(snapshot with { Objects = snapshot.Objects.SetItem(1, snapshot.Objects[1] with { X = pickX - width / 2, Y = pickY - height / 2 }) });
                screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale);
                screen.Render(canvas, width, height);
                void Pick() { screen.OnMouseDown(pickX, pickY); screen.OnMouseUp(pickX, pickY); screen.Render(canvas, width, height); }
                screen.OnMouseMove(pickX, pickY);
                Pick(); Assert.True(screen.SelectedObjectId == "base", $"width={width}, scale={scale}, selected={screen.SelectedObjectId}, free={screen.AvailableMapRect()}");
                Pick(); Assert.True(screen.SelectedPoiId == "poi", $"width={width}, scale={scale}, selected={screen.SelectedObjectId}, field={screen.SelectedFieldId}, point={pickX},{pickY}, free={screen.AvailableMapRect()}, panel={screen.ObjectInfoPanel.BodyRect}"); Assert.Equal("poi", screen.SelectedOrActiveObjectInfo!.Value.ObjectId);
                Pick(); Assert.Equal("field", screen.SelectedFieldId); Assert.Null(screen.SelectedPoiId);
                Pick(); Assert.Null(screen.SelectedFieldId); Assert.Equal("base", screen.SelectedObjectId);
                screen.OnMouseMove(pickX + 10, pickY); Pick(); Assert.True(screen.SelectedObjectId == "base", $"width={width}, scale={scale}, selected={screen.SelectedObjectId}, free={screen.AvailableMapRect()}"); Assert.Null(screen.SelectedPoiId);
                foreach (int i in new[] { 5, 8, 9, 10 })
                {
                    var rect = screen.MapViewButtonRects[i]; Assert.True(rect.Left >= 0 && rect.Right * scale <= width && rect.Top >= 0 && rect.Bottom * scale <= height);
                    var previous = screen.MapLayers; Click(screen, rect, scale); Assert.NotEqual(previous, screen.MapLayers);
                }
                Assert.True(screen.FitMapView(MapFitMode.System)); screen.Render(canvas, width, height);
            }
        foreach (bool maximum in new[] { false, true })
        {
            var config = EngineContentLoader.LoadSolarSystemGenerationConfig(DefaultSystemContentTests.Settings)!;
            config = config with
            {
                MinPlanets = maximum ? config.MaxPlanets : config.MinPlanets,
                MaxPlanets = maximum ? config.MaxPlanets : config.MinPlanets,
                MinBelts = maximum ? config.MaxBelts : config.MinBelts,
                MaxBelts = maximum ? config.MaxBelts : config.MinBelts,
                Ai = config.Ai! with { MinBases = maximum ? config.Ai.MaxBases : config.Ai.MinBases, MaxBases = maximum ? config.Ai.MaxBases : config.Ai.MinBases }
            };
            using var engine = SimulationEngine.CreateFromSettingsFile(DefaultSystemContentTests.Settings);
            var source = ScenarioLoader.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Scenarios", "Default_500", "scenario.json"));
            engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = 42, CurrentSpeed = "Speed0" } }, generation: config);
            foreach (int width in new[] { 1280, 1920 })
                foreach (float scale in new[] { 1f, 1.2f, 1.5f })
                {
                    int height = width * 9 / 16;
                    var buffer = new SnapshotBuffer(); buffer.Update(engine.CaptureSnapshot()); var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale);
                    using var bitmap = new SKBitmap(width, height); using var canvas = new SKCanvas(bitmap); screen.Render(canvas, width, height);
                    Assert.True(screen.FitMapView(MapFitMode.System)); screen.Render(canvas, width, height);
                    foreach (int i in new[] { 5, 8, 9, 10 }) { var previous = screen.MapLayers; Click(screen, screen.MapViewButtonRects[i], scale); Assert.NotEqual(previous, screen.MapLayers); }
                    Assert.False(screen.MapToolbarRect.IntersectsWith(screen.CommandsPanel.BodyRect));
                    Assert.False(screen.MapToolbarRect.IntersectsWith(screen.ObjectInfoPanel.BodyRect));
                }
        }
    }

    [Fact]
    public void BaseAndFieldInfoStayReadable()
    {
        var buffer = new SnapshotBuffer(); buffer.Update(Overlap()); var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap); screen.Render(canvas, 1920, 1080);
        screen.OnMouseDown(1060, 540); screen.OnMouseUp(1060, 540); screen.Render(canvas, 1920, 1080);
        var lines = ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo);
        Assert.Contains(lines, l => l.Value == "Ai"); Assert.Contains(lines, l => l.Label == "Defence radius" && l.Value == "20 km");
        Assert.Contains(lines, l => l.Value == AiMapPresentation.TerritoryNotice);
        screen.OnMouseDown(1120, 580); screen.OnMouseUp(1120, 580); screen.Render(canvas, 1920, 1080);
        lines = ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo);
        Assert.Contains(lines, l => l.Value == "Debris"); Assert.Contains(lines, l => l.Label == "Intensity" && l.Value == "0.75");
        Assert.Contains(lines, l => l.Value == EnvironmentFieldRenderer.Notice);
    }

    private sealed class QuietConnection : IGameSessionConnection
    {
        public int Actions { get; private set; }
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) { Actions++; return ValueTask.CompletedTask; }
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) { Actions++; return ValueTask.CompletedTask; }
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) { Actions++; return ValueTask.CompletedTask; }
        public ValueTask<TradeQuoteSnapshot> GetTradeQuoteAsync(TradeQuoteRequest request, CancellationToken cancellationToken = default) { Actions++; throw new InvalidOperationException("Unexpected quote request"); }
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) { Actions++; return ValueTask.CompletedTask; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        { await Task.CompletedTask; yield break; }
    }
}
