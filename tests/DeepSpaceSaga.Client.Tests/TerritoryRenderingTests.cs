using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class TerritoryRenderingTests
{
    [Fact]
    public void BothRadiiMatchWorldAtEveryZoom()
    {
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [],
            AiMap: new(1, [], [new("area", "base", 2, 10)]));
        var geometry = Assert.Single(AiMapPresentation.Territories(snapshot, [new("base", 7, 13, 0, 0)]));
        Assert.Equal(20, geometry.DefenceRadiusWorld); Assert.Equal(100, geometry.PatrolRadiusWorld);
        Assert.Equal(7, geometry.X); Assert.Equal(13, geometry.Y);
        foreach (double ppu in new[] { 0.1, 0.5, 1.0 })
        {
            var camera = new CameraState(7, 13, ppu);
            using var bitmap = new SKBitmap(400, 400); using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Transparent);
            AiMapPresentation.DrawTerritories(canvas, snapshot, [new("base", 7, 13, 0, 0)], camera, 400, 400, null);
            int edge = 200 + (int)(100 * ppu);
            Assert.True(bitmap.GetPixel(edge, 200).Alpha > 0);
            Assert.Equal(0, bitmap.GetPixel(edge + 3, 200).Alpha);
        }
    }

    [Fact]
    public void OverlapPreservesSelectionAndInformationalText()
    {
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0,
            [new("ship", 0, 0, 0, 0, RenderObjectType: "PlayerShip"),
             new("base1", 150, 0, 0, 0, RenderObjectType: "Station"), new("base2", 210, 0, 0, 0, RenderObjectType: "Station")], "ship",
            AiMap: new(1, [new("base1", "Orbital", "Ai", null, null, 0, 0), new("base2", "Orbital", "Ai", null, null, 0, 0)],
                [new("t1", "base1", 2, 10), new("t2", "base2", 2, 10)]));
        var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        var stages = new List<string>(); screen.RenderStageCompleted = stages.Add;
        screen.Render(canvas, 1920, 1080);
        foreach (var (id, x) in new[] { ("base1", 1110), ("base2", 1170) })
        {
            screen.OnMouseDown(x, 540); screen.OnMouseUp(x, 540); screen.Render(canvas, 1920, 1080);
            Assert.Equal(id, screen.SelectedObjectId);
            var lines = ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo);
            Assert.Contains(lines, l => l.Value == AiMapPresentation.TerritoryNotice);
            Assert.Contains(lines, l => l.Label == "Patrol radius" && l.Value == "10 km");
        }
        Assert.True(stages.IndexOf("territories") < stages.IndexOf("marker_geometry"));
    }
}
