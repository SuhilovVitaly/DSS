using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class MapDetailLabelTests
{
    private static AuthoritativeSnapshot World(string name = "Docked")
    {
        using var engine = SimulationEngine.CreateFromSettingsFile(DefaultSystemContentTests.Settings);
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "Scenarios", name, "scenario.json"));
        engine.LoadScenario(scenario with { GameState = scenario.GameState with { MasterSeed = 42 } },
            generation: EngineContentLoader.LoadSolarSystemGenerationConfig(DefaultSystemContentTests.Settings));
        return engine.CaptureSnapshot();
    }

    [Fact]
    public void SelectedStationRemainsVisibleAndSelectableAtEveryLevel()
    {
        var source = World();
        string selected = source.Objects.Single(o => o.ObjectId == source.PlayerShipObjectId).DockedStationObjectId!;
        var snapshot = source with { SelectedObjectId = selected };
        var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), mapSettings: new() { MaximumLabels = 4 });
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        foreach (var (days, level) in new[] { (.1, MapDetailLevel.Local), (.5, MapDetailLevel.Region), (10d, MapDetailLevel.Cluster), (100d, MapDetailLevel.System) })
        {
            double target = 1080 / (days * snapshot.ClusterMap!.Geography!.ReferenceSpeedKmS * 86400 / 300 * 10);
            var free = screen.AvailableMapRect();
            screen.OnMouseWheel(free.MidX, free.MidY, (float)(Math.Log(target / screen.CameraPixelsPerWorldUnit) / Math.Log(1.25)));
            screen.Render(canvas, 1920, 1080);
            Assert.Equal(level, screen.MapDetail);
            Assert.True(screen.MapLabels.TryGetValue(selected, out var label));
            screen.OnMouseMove(label.PlaqueRect.MidX, label.PlaqueRect.MidY);
            Assert.Equal(selected, screen.ActiveObjectId);
            screen.OnMouseDown(label.PlaqueRect.MidX, label.PlaqueRect.MidY, MouseButton.Right);
            Assert.Null(screen.SelectedObjectId);
            screen.OnMouseDown(label.PlaqueRect.MidX, label.PlaqueRect.MidY, MouseButton.Left);
            Assert.Equal(selected, screen.SelectedObjectId);
        }
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void DenseLabelsStayInsideViewport(float scale)
    {
        var source = World();
        var player = source.Objects.Single(o => o.ObjectId == source.PlayerShipObjectId);
        string selected = player.DockedStationObjectId!;
        var dense = Enumerable.Range(0, 100).Select(i => new ObjectMotionSnapshot("dense" + i,
            player.X + i % 10 * 8, player.Y + i / 10 * 8, 0, 0, RenderObjectType: SpaceObjectType.Station,
            DisplayName: new string('W', 500)));
        var buffer = new SnapshotBuffer(); buffer.Update(source with { SelectedObjectId = selected, Objects = source.Objects.AddRange(dense) });
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), uiScale: scale, mapSettings: new() { MaximumLabels = 4 });
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        Assert.Contains(selected, screen.MapLabels.Keys);
        Assert.Contains(player.ObjectId, screen.MapLabels.Keys);
        Assert.InRange(screen.MapLabels.Count, 2, 4);
        var labels = screen.MapLabels.Values.Select(g => g.PlaqueRect).ToArray();
        foreach (var rect in labels)
        {
            Assert.InRange(rect.Left, 0, 1920); Assert.InRange(rect.Right, 0, 1920);
            Assert.InRange(rect.Top, 0, 1080); Assert.InRange(rect.Bottom, 0, 1080);
        }
        for (int i = 0; i < labels.Length; i++)
            for (int j = i + 1; j < labels.Length; j++) Assert.False(labels[i].IntersectsWith(labels[j]));
    }

    [Fact]
    public void SummariesDoNotRevealUnknownResourceComposition()
    {
        var source = World("Default");
        var resource = source.Objects.First(o => o.Survey is not null);
        var snapshot = source with { SelectedObjectId = resource.ObjectId };
        string before = JsonSerializer.Serialize(snapshot);
        var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        var overview = screen.ScaleButtonRects[4]; screen.OnMouseDown(overview.MidX, overview.MidY);
        screen.Render(canvas, 1920, 1080);
        Assert.Equal(MapDetailLevel.System, screen.MapDetail);
        Assert.NotEmpty(screen.MapSummaryLabels);
        using var locale = Localization.UseLocaleForTests("English");
        Assert.Contains(ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo!.Value), l => l.Label == "Composition" && l.Value == "Unknown");
        Assert.All(snapshot.Objects.Where(o => o.Survey is not null), o =>
        { Assert.False(o.Survey!.CompositionKnown); Assert.Empty(o.Survey.Resources); });
        Assert.Equal(before, JsonSerializer.Serialize(snapshot));
    }
}
