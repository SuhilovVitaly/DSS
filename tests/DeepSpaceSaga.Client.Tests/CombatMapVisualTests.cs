using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatMapVisualTests
{
    private static ObjectMotionSnapshot Ship(string id, double x, double y, int hp = 450) =>
        new(id, x, y, 0, 0, RenderObjectType: id == "player" ? SpaceObjectType.PlayerShip : SpaceObjectType.NpcShip,
            ModuleHealth: [new("engine", "Engine", 0, 0, hp, 450, null, false)]);

    [Fact]
    public void Known_ship_labels_remain_visible_without_aggregate_health()
    {
        var renderer = new ObjectLabelRenderer();
        var known = Enumerable.Range(0, 8).Select(i => Ship(i == 0 ? "player" : "ship" + i, 100 + i * 180, 200)).ToArray();
        var unknown = Ship("hidden", 200, 400) with { RenderObjectType = SpaceObjectType.UnknownSpaceObject };
        var legacy = Ship("legacy", 400, 400) with { ModuleHealth = default };
        var other = Ship("other", 600, 400);
        var states = known.Concat([unknown, legacy, other]).Select(o => new ObjectRenderState(o, o, o.ObjectId == "player")).ToArray();
        var camera = new CameraState(960, 540, 1);
        renderer.ComputeGeometries(states, .02, 1920, 1080, camera, mapSettings: new() { MaximumLabels = 4 });
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        renderer.DrawPlaques(canvas, states, 0, SimulationSpeed.Speed0, 1920, 1080, camera);
        foreach (var obj in known)
        {
            var geometry = renderer.Geometries[obj.ObjectId];
            Assert.Equal(ObjectLabelLayout.PlaqueHeight, geometry.PlaqueRect.Height);
            Assert.True(ObjectLabelRenderer.HasSelectableShipLabel(obj));
        }
        Assert.False(ObjectLabelRenderer.HasSelectableShipLabel(unknown));
        Assert.True(ObjectLabelRenderer.HasSelectableShipLabel(legacy));
        Assert.True(ObjectLabelRenderer.HasSelectableShipLabel(other));
        Assert.DoesNotContain(new SKColor(20, 210, 100), bitmap.Pixels);
    }

    [Theory]
    [InlineData(450)]
    [InlineData(300)]
    [InlineData(150)]
    [InlineData(900)]
    public void Health_changes_do_not_create_a_hull_bar(int hp)
    {
        var obj = Ship("player", 300, 200, hp);
        ObjectRenderState[] states = [new(obj, obj, true)];
        var hpColor = new SKColor(123, 45, 67);
        var renderer = new ObjectLabelRenderer();
        var camera = new CameraState(400, 300, 1);
        renderer.ComputeGeometries(states, .02, 800, 600, camera);
        using var bitmap = new SKBitmap(800, 600);
        using var canvas = new SKCanvas(bitmap);
        renderer.DrawPlaques(canvas, states, 0, SimulationSpeed.Speed0, 800, 600, camera);
        Assert.Equal(ObjectLabelLayout.PlaqueHeight, renderer.Geometries["player"].PlaqueRect.Height);
        Assert.DoesNotContain(hpColor, bitmap.Pixels);
        Assert.False(ObjectLabelRenderer.HasSelectableShipLabel(obj with { RenderObjectType = SpaceObjectType.Wreck }));
    }

    [Fact]
    public void Per_module_health_does_not_change_label_selection_policy()
    {
        var ship = Ship("player", 0, 0) with
        {
            ModuleHealth = [new("a", "A", 10, 20, 20, 40, null, false), new("b", "B", 0, 20, 40, 80, null, false)]
        };
        Assert.True(ObjectLabelRenderer.HasSelectableShipLabel(ship));
        Assert.True(ObjectLabelRenderer.HasSelectableShipLabel(ship with { ModuleHealth = [] }));
        Assert.False(ObjectLabelRenderer.HasSelectableShipLabel(ship with { IsDestroyed = true }));
    }
}
