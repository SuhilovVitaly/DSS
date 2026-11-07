using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class SystemMapNavigationTests
{
    private static AuthoritativeSnapshot NavigationWorld()
    {
        var source = LocalClusterMapTests.Snapshot(12);
        var target = source.Objects.Single(o => o.ObjectId == "s2");
        return source with
        {
            SelectedObjectId = "s1",
            Objects = source.Objects.SetItem(0, source.Objects[0] with
            { X = 0, NavigationTargetObjectId = target.ObjectId, NavigationTargetX = target.X, NavigationTargetY = target.Y, MaxSpeedKmS = 4 }),
            SolarSystemMap = new(2, 42, 1000000, [new("belt", 650000, 750000, 42)], [], []),
            ClusterMap = source.ClusterMap! with { RulesVersion = 2, Geography = new(4, 1, 1, 3) }
        };
    }

    [Fact]
    public async Task SystemClusterStationPlayerNavigationPreservesTarget()
    {
        var connection = new PoiMapSelectionTests.RecordingConnection();
        await using var handle = new GameSessionHandle(connection);
        var snapshot = NavigationWorld(); handle.Buffer.Update(snapshot);
        string before = JsonSerializer.Serialize(snapshot);
        var screen = new GameSessionScreen(handle.Buffer, new LinearMotionPredictor(), handle);
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        void Frame() => screen.Render(canvas, 1920, 1080);
        void Button(int index) { var b = screen.MapViewButtonRects[index]; screen.OnMouseDown(b.MidX, b.MidY); screen.OnMouseUp(b.MidX, b.MidY); Frame(); }
        Frame(); Button(3);
        Assert.Equal(MapDetailLevel.System, screen.MapDetail);
        var label = Assert.Single(screen.MapSummaryLabels);
        screen.OnMouseDown(label.Left + 3, label.Top + 3); screen.OnMouseUp(label.Left + 3, label.Top + 3); Frame();
        Assert.True(screen.CameraPixelsPerWorldUnit > .001);
        Assert.Equal("s1", screen.SelectedObjectId);
        Assert.False(screen.MapViewButtonRects[11].IsEmpty);
        Button(11);
        Assert.Equal(MapDetailLevel.Local, screen.MapDetail);
        Assert.Equal(screen.LocalMaximumPpu, screen.CameraPixelsPerWorldUnit);
        Assert.False(screen.IsFocusAttachedToPlayer);
        var station = screen.RenderStates.Single(s => s.Pose.ObjectId == "s1").Pose;
        var free = screen.AvailableMapRect();
        Assert.InRange(960 + (station.X - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit, free.Left, free.Right);
        Button(0);
        Assert.True(screen.IsFocusAttachedToPlayer); Assert.Equal(0, screen.CameraFocusX);
        Assert.Equal("s2", screen.RenderStates.Single(s => s.IsPlayerShip).Pose.NavigationTargetObjectId);
        Assert.Equal(0, connection.Actions);
        Assert.Equal(before, JsonSerializer.Serialize(snapshot));
    }

    [Fact]
    public void ClusterFitUsesPresentedMembersAndWholeSystemBounds()
    {
        var snapshot = NavigationWorld();
        var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        // A transport update before the next render must not change the camera's current frame context.
        buffer.Update(snapshot with
        {
            SnapshotSequence = 2,
            SolarSystemMap = snapshot.SolarSystemMap! with { SystemRadius = 1e12 },
            ClusterMap = snapshot.ClusterMap! with { Clusters = [] }
        });
        Assert.True(screen.FitCluster("c"));
        var free = screen.AvailableMapRect();
        foreach (var station in screen.RenderStates.Where(s => s.Pose.RenderObjectType == SpaceObjectType.Station))
        {
            double x = 960 + (station.Pose.X - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit;
            Assert.InRange(x, free.Left, free.Right);
        }
        Assert.True(screen.FitMapView(MapFitMode.System));
        foreach (double sign in new[] { -1d, 1d })
        {
            double x = 960 + (sign * snapshot.SolarSystemMap!.SystemRadius - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit;
            double y = 540 + (sign * snapshot.SolarSystemMap.SystemRadius - screen.CameraFocusY) * screen.CameraPixelsPerWorldUnit;
            Assert.InRange(x, free.Left - .01, free.Right + .01); Assert.InRange(y, free.Top - .01, free.Bottom + .01);
        }
        Assert.False(screen.FocusStation("unknown"));
    }

    [Fact]
    public void OrbitToggleKeepsWorldAndSelection()
    {
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0,
            [new("ship", 700000, 0, 0, 0, RenderObjectType: "PlayerShip"),
             new("station", 700150, 0, 0, 0, RenderObjectType: "Station")], "ship",
            SolarSystemMap: new(1, 1, 1100000, [new("belt", 650000, 750000, 1)], [], []));
        var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        screen.OnMouseDown(1110, 540); screen.OnMouseUp(1110, 540);
        Assert.Equal("station", screen.SelectedObjectId);
        string before = JsonSerializer.Serialize(buffer.Latest!.Snapshot);
        var button = screen.MapViewButtonRects[5];
        screen.OnMouseDown(button.MidX, button.MidY);
        Assert.False(screen.ShowOrbits);
        screen.Render(canvas, 1920, 1080);
        Assert.Equal("station", screen.SelectedObjectId);
        Assert.Equal(before, JsonSerializer.Serialize(buffer.Latest!.Snapshot));
        screen.OnMouseDown(button.MidX, button.MidY);
        Assert.True(screen.ShowOrbits);
        button = screen.MapViewButtonRects[6];
        screen.OnMouseDown(button.MidX, button.MidY);
        Assert.False(screen.IsFocusAttachedToPlayer);
        Assert.False(screen.FitBelt("missing"));
        Assert.Equal("station", screen.SelectedObjectId);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.2f)]
    [InlineData(1.5f)]
    public void SelectedLabelWinsAtEveryScale(float scale)
    {
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0,
            [new("ship", 700000, 0, 0, 0, RenderObjectType: "PlayerShip"),
             new("station", 700150, 0, 0, 0, RenderObjectType: "Station"),
             new("asteroid", 700151, 0, 0, 0, RenderObjectType: "Asteroid")], "ship",
            SolarSystemMap: new(1, 1, 1100000, [new("belt", 650000, 750000, 1)], [], []));
        var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale);
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        screen.OnMouseDown(1110, 540); screen.OnMouseUp(1110, 540);
        Assert.Equal("station", screen.SelectedObjectId);
        Assert.True(screen.FitMapView(MapFitMode.System));
        screen.Render(canvas, 1920, 1080);
        Assert.Contains("station", screen.MapLabels.Keys);
        Assert.Contains("ship", screen.MapLabels.Keys);
        var button = screen.MapViewButtonRects[5];
        screen.OnMouseDown(button.MidX * scale, button.MidY * scale);
        Assert.False(screen.ShowOrbits);
        Assert.Equal("station", screen.SelectedObjectId);
    }

    [Fact]
    public void MapClustersDoNotPaintOverSelectedPlaque()
    {
        using var engine = SimulationEngine.CreateFromSettingsFile(DefaultSystemContentTests.Settings);
        var source = ScenarioLoader.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Scenarios", "Default_500", "scenario.json"));
        var config = DeepSpaceSaga.Engine.Content.EngineContentLoader.LoadSolarSystemGenerationConfig(DefaultSystemContentTests.Settings)! with
        { MinPlanets = 7, MaxPlanets = 7, MinBelts = 5, MaxBelts = 5, StartMinDays = 75, StartMaxDays = 75 };
        engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = 1, CurrentSpeed = "Speed0" } }, generation: config);
        var buffer = new SnapshotBuffer();
        buffer.Update(engine.CaptureSnapshot() with { SelectedObjectId = "SPC-0002" });
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        screen.FitMapView(MapFitMode.System);
        SKColor[]? plaque = null;
        screen.RenderStageCompleted = stage =>
        {
            if (stage is not ("label_plaques" or "markers_and_labels")) return;
            var rect = screen.MapLabels["SPC-0002"].PlaqueRect;
            var pixels = new List<SKColor>();
            for (int y = Math.Max(0, (int)rect.Top); y < Math.Min(bitmap.Height, (int)rect.Bottom); y++)
                for (int x = Math.Max(0, (int)rect.Left); x < Math.Min(bitmap.Width, (int)rect.Right); x++)
                    pixels.Add(bitmap.GetPixel(x, y));
            if (stage == "label_plaques") plaque = pixels.ToArray();
            else Assert.Equal(plaque!, pixels.ToArray());
        };
        screen.Render(canvas, 1280, 720);
        Assert.NotNull(plaque);
        Assert.True(screen.MapClusterCount > 0);
    }
    [Fact]
    public void FitAndResizePreserveWorldDistances()
    {
        var solarScenarios = DeepSpaceSaga.Engine.Content.EngineContentLoader
            .LoadSolarSystemGenerationConfig(DefaultSystemContentTests.Settings)!.EnabledScenarios;
        foreach (var scenario in ScenarioRepository.ListScenarios(Path.Combine(AppContext.BaseDirectory, "Scenarios"))
            .Where(s => solarScenarios.Contains(Path.GetFileName(Path.GetDirectoryName(s.ScenarioPath)), StringComparer.Ordinal)))
        {
            using var engine = SimulationEngine.CreateFromScenarioFile(DefaultSystemContentTests.Settings, scenario.ScenarioPath);
            engine.SetSpeed(SimulationSpeed.Speed0);
            var snapshot = engine.CaptureSnapshot();
            var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
            string before = JsonSerializer.Serialize(snapshot);
            var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
            foreach (var (width, height) in new[] { (1280, 720), (1920, 1080) })
                foreach (float scale in new[] { 1f, 1.2f, 1.5f })
                {
                    screen.SetUiScale(scale);
                    using var bitmap = new SKBitmap(width, height);
                    using var canvas = new SKCanvas(bitmap);
                    screen.Render(canvas, width, height);
                    Assert.True(screen.FitMapView(MapFitMode.System));
                    CheckBounds(snapshot.SolarSystemMap!.SystemRadius);
                    foreach (var belt in snapshot.SolarSystemMap.Belts)
                    {
                        Assert.True(screen.FitBelt(belt.Id));
                        CheckBounds(belt.OuterRadius);
                    }
                    Assert.Equal(before, JsonSerializer.Serialize(buffer.Latest!.Snapshot));
                    void CheckBounds(double radius)
                    {
                        var free = screen.AvailableMapRect();
                        foreach (double sign in new[] { -1d, 1d })
                        {
                            double x = width / 2d + (sign * radius - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit;
                            double y = height / 2d + (sign * radius - screen.CameraFocusY) * screen.CameraPixelsPerWorldUnit;
                            Assert.InRange(x, free.Left - .01, free.Right + .01);
                            Assert.InRange(y, free.Top - .01, free.Bottom + .01);
                        }
                    }
                }
        }
    }
}
