using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class CompactObjectLabelTests
{
    [Theory]
    [InlineData(0, 18)]
    [InlineData(5, 23)]
    [InlineData(80, 98)]
    public void Width_tracks_text_without_a_fixed_minimum(float textWidth, float expectedWidth)
    {
        var geometry = ObjectLabelLayout.Create(new(400, 300), 90, textWidth, new(800, 600), 5);
        Assert.Equal(expectedWidth, geometry.PlaqueRect.Width, 3);
        Assert.Equal(16, geometry.PlaqueRect.Height);
        Assert.Equal(4, geometry.PlaqueRect.Right - geometry.TextOrigin.X - textWidth, 3);
    }

    [Fact]
    public void Ship_keeps_compact_label_without_an_aggregate_hp_bar()
    {
        var hp = new SKColor(60, 180, 120);
        var ship = new ObjectMotionSnapshot("ship", 300, 200, 0, 0,
            RenderObjectType: SpaceObjectType.NpcShip, DisplayName: "Ship",
            ModuleHealth: [new("engine", "Engine", 225, 450, false)]);
        ObjectRenderState[] states = [new(ship, ship, false)];
        var renderer = new ObjectLabelRenderer();
        var camera = new CameraState(400, 300, 1);
        renderer.ComputeGeometries(states, .02, 800, 600, camera);
        using var bitmap = new SKBitmap(800, 600);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        renderer.DrawPlaques(canvas, states, 0, SimulationSpeed.Speed0, 800, 600, camera);
        var bounds = renderer.Geometries["ship"].PlaqueRect;
        Assert.Equal(16, bounds.Height);
        Assert.DoesNotContain(hp, bitmap.Pixels);
        Assert.DoesNotContain(new SKColor(35, 44, 52), bitmap.Pixels);
        Assert.True(bounds.Width > 0);
        Assert.True(ObjectLabelRenderer.HasSelectableShipLabel(ship));
    }

    [Fact]
    public void Marker_under_player_plaque_wins_hover_and_click_but_plaque_stays_clickable()
    {
        var player = new ObjectMotionSnapshot("player", 0, 0, 0, 0,
            RenderObjectType: SpaceObjectType.PlayerShip, DisplayName: "Player Ship",
            ModuleHealth: [new("engine", "Engine", 450, 450, false)]);
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [player], "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        var plaque = screen.MapLabels["player"].PlaqueRect;
        float x = plaque.MidX, y = plaque.MidY;
        var target = new ObjectMotionSnapshot("target",
            screen.CameraFocusX + (x - 640) / screen.CameraPixelsPerWorldUnit,
            screen.CameraFocusY + (y - 360) / screen.CameraPixelsPerWorldUnit,
            0, 0, RenderObjectType: SpaceObjectType.NpcShip, DisplayName: "Target");
        buffer.Update(new(2, 0, SimulationSpeed.Speed0, [player, target], "player"));
        screen.Render(canvas, 1280, 720);
        Assert.True(screen.MapLabels["player"].PlaqueRect.Contains(x, y));
        screen.OnMouseMove(x, y);
        Assert.Equal("target", screen.SelectedOrActiveObjectInfo?.ObjectId);
        screen.OnMouseDown(x, y);
        Assert.Equal("target", screen.SelectedObjectId);

        buffer.Update(new(3, 0, SimulationSpeed.Speed0, [player], "player"));
        screen.OnMouseMove(0, 0);
        screen.Render(canvas, 1280, 720);
        screen.OnMouseDown(x, y);
        Assert.Equal("player", screen.SelectedObjectId);
        screen.OnDeactivated();
    }
}
