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
            ModuleHealth: [new("engine", "Engine", 0, 0, 225, 450, null, false)]);
        ObjectRenderState[] states = [new(ship, ship, false)];
        var renderer = new ObjectLabelRenderer(CombatVisualSettings.Default with { HullHp = hp });
        var camera = new CameraState(400, 300, 1);
        renderer.ComputeGeometries(states, .02, 800, 600, camera);
        using var bitmap = new SKBitmap(800, 600);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        renderer.DrawPlaques(canvas, states, 0, SimulationSpeed.Speed0, 800, 600, camera);
        var bounds = renderer.Geometries["ship"].PlaqueRect;
        Assert.Equal(16, bounds.Height);
        Assert.DoesNotContain(hp, bitmap.Pixels);
        Assert.DoesNotContain(TacticalMapPalette.HpEmpty, bitmap.Pixels);
        Assert.True(bounds.Width > 0);
        Assert.True(ObjectLabelRenderer.HasSelectableShipLabel(ship));
    }

    [Theory]
    [InlineData(SpaceObjectType.Missile)]
    [InlineData(SpaceObjectType.Countermeasure)]
    public void Real_screen_projectile_label_follows_hover_and_selection_without_hiding_object(string type)
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0,
            [new("player", 0, 0, 0, 0, RenderObjectType: SpaceObjectType.PlayerShip),
             new("projectile", -200, -100, 0, 0, RenderObjectType: type, DisplayName: "Projectile")], "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        void Render() => screen.Render(canvas, 1280, 720);

        Render();
        Assert.DoesNotContain("projectile", screen.MapLabels.Keys);
        screen.OnMouseMove(440, 260);
        Render();
        Assert.Contains("projectile", screen.MapLabels.Keys);
        screen.OnMouseMove(0, 0);
        Render();
        Assert.DoesNotContain("projectile", screen.MapLabels.Keys);
        screen.OnMouseDown(440, 260);
        Render();
        Assert.Contains("projectile", screen.MapLabels.Keys);
        screen.OnMouseMove(0, 0);
        Render();
        Assert.Contains("projectile", screen.MapLabels.Keys);
        screen.OnMouseDown(640, 360);
        Render();
        Assert.DoesNotContain("projectile", screen.MapLabels.Keys);
        Assert.Contains(screen.RenderStates, state => state.Source.ObjectId == "projectile");
        screen.OnDeactivated();
    }

    [Fact]
    public void Selected_and_hovered_projectiles_both_get_labels_at_low_zoom()
    {
        var torpedo = new ObjectMotionSnapshot("torpedo", -200, 0, 0, 0, RenderObjectType: SpaceObjectType.Missile);
        var defense = new ObjectMotionSnapshot("defense", 200, 0, 0, 0, RenderObjectType: SpaceObjectType.Countermeasure);
        ObjectRenderState[] states = [new(torpedo, torpedo, false), new(defense, defense, false)];
        var renderer = new ObjectLabelRenderer();
        renderer.ComputeGeometries(states, .02, 800, 600, new(0, 0, .1),
            mapSettings: new() { MaximumLabels = 0 }, selectedObjectId: "torpedo", hoveredObjectId: "defense");
        Assert.Equal(2, renderer.Geometries.Count);
        renderer.ComputeGeometries(states, .02, 800, 600, new(0, 0, .1));
        Assert.Empty(renderer.Geometries);
    }

    [Fact]
    public void Marker_under_player_plaque_wins_hover_and_click_but_plaque_stays_clickable()
    {
        var player = new ObjectMotionSnapshot("player", 0, 0, 0, 0,
            RenderObjectType: SpaceObjectType.PlayerShip, DisplayName: "Player Ship",
            ModuleHealth: [new("engine", "Engine", 0, 0, 450, 450, null, false)]);
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
