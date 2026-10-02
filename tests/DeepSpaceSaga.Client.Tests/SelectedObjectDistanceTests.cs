using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class SelectedObjectDistanceTests
{
    [Theory]
    [InlineData(SpaceObjectType.NpcShip)]
    [InlineData(SpaceObjectType.Station)]
    [InlineData(SpaceObjectType.Asteroid)]
    [InlineData(SpaceObjectType.UnknownSpaceObject)]
    [InlineData(SpaceObjectType.Container)]
    [InlineData(SpaceObjectType.Planet)]
    public void Distance_is_from_player_and_updates_with_target_motion(string type)
    {
        long clock = 0;
        var buffer = new SnapshotBuffer(() => clock);
        var player = new ObjectMotionSnapshot("PLAYER", 10000, 10000, 0, 0, RenderObjectType: SpaceObjectType.PlayerShip);
        var target = new ObjectMotionSnapshot("TARGET", 10030, 10040, .4, 90, RenderObjectType: type);
        buffer.Update(new(1, 0, SimulationSpeed.Speed1, [player, target], PlayerShipObjectId: "PLAYER"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        screen.OnMouseDown(670, 400);
        Assert.Equal("TARGET", screen.SelectedObjectId);
        Assert.Equal(5, screen.SelectedOrActiveObjectInfo!.Value.DistanceKm!.Value, 8);
        Assert.Contains(("Distance", "5 km"), ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo));
        Assert.Null(screen.PlayerShipInfo!.Value.DistanceKm);
        clock = Stopwatch.Frequency;
        screen.Render(canvas, 1280, 720);
        Assert.True(screen.SelectedOrActiveObjectInfo!.Value.DistanceKm > 5);
    }

    [Theory]
    [InlineData(0, "0 m")]
    [InlineData(.5, "500 m")]
    [InlineData(1, "1 km")]
    public void Distance_formats_zero_and_nearby_targets(double km, string expected)
    {
        var data = new ObjectInfoPanelData("TARGET", null, 0, 0, SpaceObjectType.UnknownSpaceObject, DistanceKm: km);
        Assert.Contains(("Distance", expected), ObjectInfoPanel.BuildLines(data));
    }
}
