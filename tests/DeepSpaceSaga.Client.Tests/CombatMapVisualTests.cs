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
            HullCombat: new("ship.tetrarch", 1, 450),
            ModuleHealth: [new("engine", "Engine", 0, 0, hp, 450, null, false)]);

    private static ObjectMotionSnapshot Missile(double x, double y) =>
        new("torpedo", x, y, 3, 0, RenderObjectType: SpaceObjectType.Missile,
            OwnProjectile: new(ProjectileKind.Torpedo, "player", "launcher", x, y, x, y, x, y - 600, 0, 10000, 6, "slow", 9, 200, 20));

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void Configured_point_marker_and_five_pixel_wreck_remain_constant_across_zoom(float uiScale)
    {
        SKColor[]? reference = null;
        foreach (double ppu in new[] { 1.0, .1, .001, .00001 })
        {
            var buffer = new SnapshotBuffer(() => 0);
            buffer.Update(new(1, 0, SimulationSpeed.Speed0,
                [Ship("player", 0, 0), Missile(200 / ppu, -120 / ppu),
                    new("wreck", -200 / ppu, -120 / ppu, 0, 0, RenderObjectType: SpaceObjectType.Wreck)], "player"));
            var map = new TacticalMapSettings
            {
                MetersPerPixel = [100 / ppu, 1000 / ppu, 10000 / ppu, 100000 / ppu, 1000000 / ppu],
                LabelDetailPpu = ppu,
                CompactMarkerPpu = ppu,
                ClusterPpu = ppu,
                TrailDetailPpu = ppu
            };
            var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0,
                mapSettings: map, uiScale: uiScale);
            using var bitmap = new SKBitmap(1920, 1080);
            using var canvas = new SKCanvas(bitmap);
            SKColor[]? pixels = null;
            screen.RenderStageCompleted = stage =>
            {
                if (stage == "label_leaders") canvas.Clear(SKColors.Transparent);
                if (stage != "marker_geometry") return;
                pixels = Enumerable.Range(-10, 21).SelectMany(y => Enumerable.Range(-10, 21)
                    .SelectMany(x => new[] { bitmap.GetPixel(1160 + x, 420 + y), bitmap.GetPixel(760 + x, 420 + y) })).ToArray();
                double diameter = Enumerable.Range(-10, 21).Sum(x => bitmap.GetPixel(760 + x, 420).Alpha / (double)screen.CombatSettings.Wreck.Alpha);
                Assert.InRange(diameter, 4.7, 5.1);
                Assert.Equal(screen.CombatSettings.Torpedo, bitmap.GetPixel(1160, 420));
                Assert.Equal(screen.CombatSettings.Wreck.Alpha, bitmap.GetPixel(760, 420).Alpha);
                Assert.Equal(0, bitmap.GetPixel(1165, 420).Alpha); // current point marker has no legacy impact glow
                Assert.Equal(0, bitmap.GetPixel(765, 420).Alpha);
            };
            screen.Render(canvas, 1920, 1080);
            Assert.NotNull(pixels);
            if (reference is null) reference = pixels;
            else Assert.Equal(reference, pixels);
            screen.OnDeactivated();
        }
    }

    [Fact]
    public void Known_ship_labels_remain_visible_without_aggregate_health()
    {
        var palette = CombatVisualSettings.Default with { HullHp = new SKColor(20, 210, 100) };
        var renderer = new ObjectLabelRenderer(palette);
        var known = Enumerable.Range(0, 8).Select(i => Ship(i == 0 ? "player" : "ship" + i, 100 + i * 180, 200)).ToArray();
        var unknown = Ship("hidden", 200, 400) with { RenderObjectType = SpaceObjectType.UnknownSpaceObject };
        var legacy = Ship("legacy", 400, 400) with { ModuleHealth = default, HullCombat = new("ship.tetrarch", 450, 450) };
        var other = Ship("other", 600, 400) with { HullCombat = new("ship.other", 0, 450) };
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
        Assert.DoesNotContain(palette.HullHp, bitmap.Pixels);
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
        var renderer = new ObjectLabelRenderer(CombatVisualSettings.Default with { HullHp = hpColor });
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

    [Fact]
    public void Combat_markers_are_not_duplicated_or_cluster_hidden()
    {
        var buffer = new SnapshotBuffer(() => 0);
        // A neutral target, another known Tetrarch, a projectile and two ordinary
        // contacts share a cluster cell. Only the ordinary contacts may aggregate.
        buffer.Update(new(1, 0, SimulationSpeed.Speed0,
            [Ship("player", 0, 0), Missile(20000000, 20000000), Ship("friendly", 20000001, 20000001),
                new("target", 20000002, 20000002, 0, 0, RenderObjectType: SpaceObjectType.UnknownSpaceObject),
                new("a", 20000003, 20000003, 0, 0), new("b", 20000004, 20000004, 0, 0)], "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        var preset = screen.ScaleButtonRects[4];
        screen.OnMouseDown(preset.MidX, preset.MidY);
        screen.Render(canvas, 1920, 1080);
        Assert.Equal(1, screen.MapClusterCount);
        var field = typeof(GameSessionScreen).GetField("_clusteredObjectIds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        Assert.Equal(new[] { "a", "b", "target" }, ((HashSet<string>)field.GetValue(screen)!).Order(StringComparer.Ordinal));
        Assert.Null(screen.SelectedObjectId);
        screen.OnDeactivated();
    }
}
