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
            HullCombat: new("ship.tetrarch", hp, 450));

    private static ObjectMotionSnapshot Missile(double x, double y) =>
        new("torpedo", x, y, 3, 0, RenderObjectType: SpaceObjectType.Missile,
            Torpedo: new("player", "launcher", "target", 0, 3, 90, 150, 0,
                new(0, 1, TorpedoRoutePhase.Straight, false, [new(x, y, 0, 3, 0, 1000)])));

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public void Torpedo_and_wreck_remain_five_pixels_across_zoom(float uiScale)
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
                double diameter = Enumerable.Range(-10, 21).Sum(x => bitmap.GetPixel(760 + x, 420).Alpha / 255.0);
                Assert.InRange(diameter, 4.7, 5.1);
                Assert.Equal(SKColors.Yellow, bitmap.GetPixel(1160, 420));
                Assert.Equal(new SKColor(128, 128, 128), bitmap.GetPixel(760, 420));
                Assert.True(bitmap.GetPixel(1165, 420).Alpha > 0); // blur beyond the 5px core
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
    public void All_known_tetrarchs_show_authoritative_hp_below_labels()
    {
        var palette = CombatVisualSettings.Default with { HullHp = new SKColor(20, 210, 100) };
        var renderer = new ObjectLabelRenderer(palette);
        var known = Enumerable.Range(0, 8).Select(i => Ship(i == 0 ? "player" : "ship" + i, 100 + i * 180, 200)).ToArray();
        var unknown = Ship("hidden", 200, 400) with { RenderObjectType = SpaceObjectType.UnknownSpaceObject };
        var legacy = Ship("legacy", 400, 400) with { HullCombat = null };
        var other = Ship("other", 600, 400) with { HullCombat = new("ship.other", 450, 450) };
        var states = known.Concat([unknown, legacy, other]).Select(o => new ObjectRenderState(o, o, o.ObjectId == "player")).ToArray();
        var camera = new CameraState(960, 540, 1);
        renderer.ComputeGeometries(states, .02, 1920, 1080, camera, mapSettings: new() { MaximumLabels = 4 });
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        renderer.DrawPlaques(canvas, states, 0, SimulationSpeed.Speed0, 1920, 1080, camera);
        foreach (var obj in known)
        {
            var geometry = renderer.Geometries[obj.ObjectId];
            Assert.Equal(ObjectLabelLayout.PlaqueHeight + ObjectLabelLayout.HullBarSpace, geometry.PlaqueRect.Height);
            var bar = ObjectLabelLayout.HullBarRect(geometry.PlaqueRect);
            Assert.True(geometry.PlaqueRect.Contains(bar));
            Assert.Equal(palette.HullHp, bitmap.GetPixel((int)bar.MidX, (int)bar.MidY));
        }
        foreach (var obj in new[] { unknown, legacy, other }) Assert.False(ObjectLabelRenderer.HasHullBar(obj));
    }

    [Theory]
    [InlineData(450, 1.0)]
    [InlineData(300, 2.0 / 3)]
    [InlineData(150, 1.0 / 3)]
    [InlineData(900, 1.0)]
    public void Hp_visual_progress_is_full_two_thirds_one_third(int hp, double fraction)
    {
        var obj = Ship("player", 300, 200, hp);
        ObjectRenderState[] states = [new(obj, obj, true)];
        var renderer = new ObjectLabelRenderer();
        var camera = new CameraState(400, 300, 1);
        renderer.ComputeGeometries(states, .02, 800, 600, camera);
        using var bitmap = new SKBitmap(800, 600);
        using var canvas = new SKCanvas(bitmap);
        renderer.DrawPlaques(canvas, states, 0, SimulationSpeed.Speed0, 800, 600, camera);
        var bar = ObjectLabelLayout.HullBarRect(renderer.Geometries["player"].PlaqueRect);
        int green = Enumerable.Range((int)Math.Ceiling(bar.Left), (int)bar.Width)
            .Count(x => bitmap.GetPixel(x, (int)bar.MidY) == SKColors.Lime);
        Assert.InRange(green, bar.Width * fraction - 1, bar.Width * fraction + 1);
        Assert.False(ObjectLabelRenderer.HasHullBar(obj with { HullCombat = obj.HullCombat! with { CurrentHp = 0 } }));
        Assert.False(ObjectLabelRenderer.HasHullBar(obj with { RenderObjectType = SpaceObjectType.Wreck }));
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
        Assert.Equal(new[] { "a", "b" }, ((HashSet<string>)field.GetValue(screen)!).Order(StringComparer.Ordinal));
        Assert.Null(screen.SelectedObjectId);
        screen.OnDeactivated();
    }
}
