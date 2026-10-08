using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class FullMapRenderEvidenceTests
{
    private static AuthoritativeSnapshot Maximum()
    {
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(DefaultSystemContentTests.Settings)!;
        config = config with
        {
            MinPlanets = 7,
            MaxPlanets = 7,
            MinBelts = 5,
            MaxBelts = 5,
            Clusters = config.Clusters! with { MinClusters = 5, MaxClusters = 5, MinStations = 12, MaxStations = 12 },
            Ai = config.Ai! with { MinBases = 4, MaxBases = 4 }
        };
        using var engine = SimulationEngine.CreateFromSettingsFile(DefaultSystemContentTests.Settings);
        var source = ScenarioLoader.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Scenarios/Default_500/scenario.json"));
        engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = 1, CurrentSpeed = "Speed0" } }, generation: config);
        return engine.CaptureSnapshot();
    }

    [Fact]
    public async Task RenderNeverCallsSession()
    {
        var connection = new ThrowingConnection(); await using var handle = new GameSessionHandle(connection);
        var snapshot = Maximum(); handle.Buffer.Update(snapshot);
        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle);
        using var bitmap = new SKBitmap(1280, 720); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720); screen.FitMapView(MapFitMode.System);
        for (int i = 0; i < 100; i++) screen.Render(canvas, 1280, 720);
        Assert.Equal(0, connection.Calls); Assert.Null(handle.Failure);
        var changed = snapshot with { SolarSystemMap = snapshot.SolarSystemMap! with { Belts = snapshot.SolarSystemMap!.Belts.Select(b => b with { DecorationSamples = 1 }).ToImmutableArray() } };
        handle.Buffer.Update(changed with { SnapshotSequence = snapshot.SnapshotSequence + 1 });
        screen.Render(canvas, 1280, 720);
        Assert.Equal(snapshot.Objects.Length, handle.Buffer.Latest!.Snapshot.Objects.Length); Assert.Equal(0, connection.Calls);
    }

    [Fact]
    public void FullMapInteractionMatrix()
    {
        var snapshot = Maximum();
        foreach (int width in new[] { 1280, 1920 }) foreach (float scale in new[] { 1f, 1.2f, 1.5f })
        {
            int height = width * 9 / 16; var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
            var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale);
            using var bitmap = new SKBitmap(width, height); using var canvas = new SKCanvas(bitmap);
            void Render() => screen.Render(canvas, width, height);
            Render(); Assert.True(screen.FitMapView(MapFitMode.System)); Render();
            Assert.True(screen.FitCluster(snapshot.ClusterMap!.StartClusterId)); Render();
            Assert.True(screen.FitMapView(MapFitMode.System)); Render();
            void Pick(double x, double y, Func<bool> selected)
            {
                float sx = (float)(width / 2d + (x - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit);
                float sy = (float)(height / 2d + (y - screen.CameraFocusY) * screen.CameraPixelsPerWorldUnit);
                for (int i = 0; i < 64 && !selected(); i++) { screen.OnMouseDown(sx, sy); screen.OnMouseUp(sx, sy); Render(); }
                Assert.True(selected(), $"width={width};scale={scale};point={sx},{sy};selected={screen.SelectedObjectId};field={screen.SelectedFieldId};poi={screen.SelectedPoiId}");
            }
            var ai = screen.RenderStates.First(s => s.Pose.ObjectId == snapshot.AiMap!.Bases[0].ObjectId).Pose;
            Pick(ai.X, ai.Y, () => screen.SelectedObjectId == ai.ObjectId);
            Assert.False(screen.MapToolbarRect.IntersectsWith(screen.ObjectInfoPanel.BodyRect), $"Toolbar/info overlap: {width}, UI{scale}");
            var body = screen.ObjectInfoPanel.RowBodyRects[1]; double beforeZoom = screen.CameraPixelsPerWorldUnit;
            screen.OnMouseWheel(body.MidX * scale, body.MidY * scale, -1);
            if (width / scale < 1100) Assert.True(screen.ObjectInfoPanel.ScrollOffset(1) > 0);
            Assert.Equal(beforeZoom, screen.CameraPixelsPerWorldUnit);
            screen.OnMouseWheel(body.MidX * scale, body.MidY * scale, 1);
            var fields = EnvironmentFieldRenderer.Resolve(snapshot, screen.RenderStates.Select(s => s.Predicted), snapshot.MotionTimeMs);
            var field = fields.First(f => f.Data.Kind == "Radiation");
            Pick(field.X + field.Data.OuterRadius * .5, field.Y, () => screen.SelectedFieldId == field.Data.Id);
            var poi = AiMapPresentation.Points(snapshot, screen.RenderStates.Select(s => s.Predicted), snapshot.MotionTimeMs)[0];
            Pick(poi.X, poi.Y, () => screen.SelectedPoiId == poi.Data.ObjectId);
            foreach (int i in new[] { 5, 8, 9, 10 }) { var r = screen.MapViewButtonRects[i]; screen.OnMouseDown(r.MidX * scale, r.MidY * scale); screen.OnMouseUp(r.MidX * scale, r.MidY * scale); }
            Assert.Equal((MapLayerFlags)0, screen.MapLayers); Assert.Null(screen.SelectedPoiId); Assert.Null(screen.SelectedFieldId);
            screen.OnMouseMove(1190, 650); Render();
            var close = screen.LastCloseRect; screen.OnMouseDown(close.MidX * scale, close.MidY * scale); screen.OnMouseUp(close.MidX * scale, close.MidY * scale);
            Assert.False(screen.IsPanelVisible);
            screen.SetUiScale(scale == 1 ? 1.5f : 1); screen.Render(canvas, width - 100, height - 50);
            Assert.Equal(snapshot.Objects.Length, buffer.Latest!.Snapshot.Objects.Length);
        }
    }

    [Fact]
    public void FrameReportIdentifiesActualMap()
    {
        var snapshot = Maximum(); string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var context = new MapFrameContext(1280, 720, true, 100, "test GPU", "test GL", 1.5f, "Speed0", 1, 1,
                7, 5, snapshot.Objects.Length, .001, snapshot.AiMap!.Bases[0].ObjectId, MapFrameCounts.From(snapshot), "All", 300000, 1000, "field", "poi");
            var evidence = new MapFrameEvidence(path); long ticks = Stopwatch.Frequency;
            for (int i = 0; i < 720; i++) { ticks += Stopwatch.Frequency / 100; evidence.Record(ticks, 2, context); }
            using var doc = JsonDocument.Parse(File.ReadAllText(path)); var c = doc.RootElement.GetProperty("context");
            Assert.Equal("All", c.GetProperty("Layers").GetString()); Assert.Equal(1000, c.GetProperty("MotionEpochMs").GetInt64());
            Assert.Equal(300000, c.GetProperty("CalendarEpochMs").GetInt64());
            var counts = c.GetProperty("MapCounts");
            Assert.Equal(snapshot.Objects.Length, counts.GetProperty("Entities").GetInt32()); Assert.Equal(60, counts.GetProperty("HumanStations").GetInt32());
            Assert.Equal(4, counts.GetProperty("AiBases").GetInt32()); Assert.Equal(5, counts.GetProperty("Fields").GetInt32()); Assert.Equal(2, counts.GetProperty("PointsOfInterest").GetInt32());
            Assert.Contains("not measured", doc.RootElement.GetProperty("timingDefinition").GetString());
        }
        finally { File.Delete(path); }
    }

    private sealed class ThrowingConnection : IGameSessionConnection
    {
        public int Calls;
        private Exception Unexpected() { Interlocked.Increment(ref Calls); return new InvalidOperationException("Render called session"); }
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) => throw Unexpected();
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => throw Unexpected();
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => throw Unexpected();
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => throw Unexpected();
        public ValueTask<TradeQuoteSnapshot> GetTradeQuoteAsync(TradeQuoteRequest request, CancellationToken cancellationToken = default) => throw Unexpected();
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => throw Unexpected();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default) { await Task.Delay(Timeout.Infinite, cancellationToken); yield break; }
    }
}
