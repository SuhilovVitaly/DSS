using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class ClusterInteractionEvidenceTests
{
    private static SimulationEngine Create()
    {
        string settings = DefaultSystemContentTests.Settings, client = Path.GetDirectoryName(settings)!;
        var engine = new SimulationEngine(EngineContentLoader.LoadRegistryFromSettingsFile(settings, out _, out _));
        engine.ConfigureStationResourceFields(JsonSerializer.Deserialize<StationResourceFieldConfig>(File.ReadAllText(Path.Combine(client, "Data/World/station-resource-fields.json")))!);
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(settings)!;
        var source = ScenarioLoader.LoadFromFile(Path.Combine(client, "Scenarios/Default_500/scenario.json"));
        engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = 1, CurrentSpeed = "Speed0" } }, generation: config with
        { MinPlanets = 7, MaxPlanets = 7, MinBelts = 5, MaxBelts = 5, Clusters = config.Clusters! with { MinClusters = 5, MaxClusters = 5, MinStations = 12, MaxStations = 12 } });
        return engine;
    }

    [Theory]
    [InlineData(1280, 720, 1f)]
    [InlineData(1920, 1080, 1.2f)]
    [InlineData(1920, 1080, 1.5f)]
    public void EveryStationSelectableAtMaximumNetwork(int width, int height, float scale)
    {
        using var engine = Create(); var snapshot = engine.CaptureSnapshot();
        Assert.Equal(60, snapshot.ClusterMap!.Stations.Length); Assert.True(snapshot.ClusterMap.ResourceBindings.Length > 500);
        var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale);
        using var bitmap = new SKBitmap(width, height); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, width, height); Assert.True(screen.FitMapView(MapFitMode.System)); screen.Render(canvas, width, height);
        foreach (var cluster in snapshot.ClusterMap.Clusters)
        {
            Assert.True(screen.FitCluster(cluster.Id)); screen.Render(canvas, width, height);
            foreach (string id in cluster.StationIds)
            {
                var pose = screen.RenderStates.Single(s => s.Pose.ObjectId == id).Pose;
                float x = (float)(width / 2d + (pose.X - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit);
                float y = (float)(height / 2d + (pose.Y - screen.CameraFocusY) * screen.CameraPixelsPerWorldUnit);
                screen.OnMouseDown(x, y); screen.OnMouseUp(x, y);
                Assert.True(id == screen.SelectedObjectId, $"expected={id};actual={screen.SelectedObjectId};click={x},{y};ppu={screen.CameraPixelsPerWorldUnit};free={screen.AvailableMapRect()}"); screen.Render(canvas, width, height);
                Assert.Equal(cluster.Name, screen.SelectedOrActiveObjectInfo!.Value.ClusterName);
                Assert.Equal(snapshot.ClusterMap.Stations.Single(s => s.ObjectId == id).MarketProfileId, screen.SelectedOrActiveObjectInfo.Value.ClusterProfile);
            }
        }
    }

    [Fact]
    public void SelectedStationSurvivesLodAndPause()
    {
        using var engine = Create(); var s = engine.CaptureSnapshot(); string id = s.ClusterMap!.Stations[^1].ObjectId;
        s = s with { SelectedObjectId = id };
        var buffer = new SnapshotBuffer(); buffer.Update(s); var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080); screen.FitMapView(MapFitMode.System);
        screen.Render(canvas, 1920, 1080); string? name = screen.SelectedOrActiveObjectInfo!.Value.DisplayName;
        for (int i = 0; i < 10; i++) screen.Render(canvas, 1920, 1080);
        Assert.Equal(id, screen.SelectedObjectId); Assert.Equal(name, screen.SelectedOrActiveObjectInfo!.Value.DisplayName);
        screen.FitCluster(s.ClusterMap.Stations.Single(st => st.ObjectId == id).ClusterId); screen.Render(canvas, 1920, 1080);
        Assert.Equal(id, screen.SelectedObjectId); Assert.NotNull(screen.SelectedOrActiveObjectInfo!.Value.ClusterProfile);
    }
}
