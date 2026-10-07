using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class ClusterResourceMapTests
{
    [Fact]
    public void KnownResourcesSelectableWithoutScan()
    {
        using var engine = SimulationEngine.CreateFromSettingsFile(DefaultSystemContentTests.Settings);
        var first = engine.CaptureSnapshot();
        var binding = first.ClusterMap!.ResourceBindings[0];
        var snapshot = first with { SelectedObjectId = binding.FieldId };
        var obj = snapshot.Objects.Single(o => o.ObjectId == binding.FieldId);
        Assert.True(obj.Survey!.CompositionKnown); Assert.False(obj.Survey.CanStructuralScan);
        var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        Assert.Equal(binding.FieldId, screen.SelectedObjectId);
        var data = screen.SelectedOrActiveObjectInfo!.Value;
        Assert.NotNull(data.ResourceCluster);
        Assert.Equal(binding.AnchorStationId, data.ResourceOwner);
        Assert.Contains(ObjectInfoPanel.BuildLines(data), l => l.Label == "Composition" && l.Value != "Unknown");
    }

    [Fact]
    public void ResourceIdsStableAcrossLayers()
    {
        using var engine = SimulationEngine.CreateFromSettingsFile(DefaultSystemContentTests.Settings);
        var snapshot = engine.CaptureSnapshot();
        var saved = engine.CaptureSaveState().GameState.StationResourceFields!;
        foreach (var field in saved.Asteroids)
        {
            Assert.Single(snapshot.Objects, o => o.ObjectId == field.ObjectId);
            Assert.Equal(field.ObjectId, ClusterMapPresentation.Resource(snapshot, field.ObjectId)!.FieldId);
            Assert.Equal(field.StationObjectId, ClusterMapPresentation.Resource(snapshot, field.ObjectId)!.AnchorStationId);
        }
        Assert.Equal(saved.Asteroids.Count, snapshot.ClusterMap!.ResourceBindings.Select(b => b.FieldId).Distinct().Count());
    }
}
