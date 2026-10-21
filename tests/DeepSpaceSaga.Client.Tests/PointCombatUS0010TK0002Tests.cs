using System.Collections.Immutable;
using System.Diagnostics;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0010TK0002Tests
{
    private static ObjectMotionSnapshot Own() => new("own", 0, -120, 6, 90, RenderObjectType: SpaceObjectType.Missile,
        OwnProjectile: new(ProjectileKind.Torpedo, "player", "launcher", 0, -120, 0, -120, 300, -120, 0, 5000, 6, "slow", 9, 200, 20));
    private static ObjectMotionSnapshot Foreign() => new("foreign", -200, -120, 6, 270, RenderObjectType: SpaceObjectType.Missile,
        ObservedProjectile: new(ProjectileKind.Torpedo, -200, -120, -1, 0, 6, "slow", 9));
    private static AuthoritativeSnapshot Scene(params ObjectMotionSnapshot[] flights) => new(1, 0, SimulationSpeed.Speed0,
        new[] { new ObjectMotionSnapshot("player", 0, 0, 0, 0, RenderObjectType: SpaceObjectType.PlayerShip) }.Concat(flights).ToImmutableArray(), "player", CombatJournal: []);
    private static TacticalMapSettings Map(double ppu) => new()
    {
        MetersPerPixel = [100 / ppu, 1000 / ppu, 10000 / ppu, 100000 / ppu, 1000000 / ppu],
        LabelDetailPpu = ppu,
        CompactMarkerPpu = ppu,
        ClusterPpu = ppu,
        TrailDetailPpu = ppu
    };

    [Theory]
    [InlineData(1280, 720, 1.5f)]
    [InlineData(1920, 1080, 1f)]
    public void Own_countdown_and_foreign_profile_labels_remain_inside_physical_viewport(int width, int height, float uiScale)
    {
        double edgeX = width / 2d - 2, edgeY = height / 2d - 2;
        var own = Own() with
        {
            OwnProjectile = Own().OwnProjectile! with
            {
                DetonationX = edgeX,
                DetonationY = 0,
                DetonationMotionTimeMs = double.Hypot(edgeX, 120) / 60 * 1000
            }
        };
        var foreign = Foreign() with
        {
            X = edgeX,
            Y = edgeY,
            ObservedProjectile = Foreign().ObservedProjectile! with { X = edgeX, Y = edgeY }
        };
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(Scene(own, foreign));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0,
            mapSettings: Map(1), uiScale: uiScale);
        using var bitmap = new SKBitmap(width, height); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, width, height);
        foreach (string id in new[] { "own" })
        {
            var visual = screen.PointProjectileVisuals[id];
            Assert.False(visual.LabelBounds.IsEmpty);
            Assert.True(new SKRect(0, 0, width, height).Contains(visual.LabelBounds), $"{id}: {visual.LabelBounds}");
        }
        Assert.NotNull(screen.PointProjectileVisuals["own"].RemainingRealSeconds);
        Assert.Null(screen.PointProjectileVisuals["foreign"].RemainingRealSeconds);
        screen.OnDeactivated();
    }

    [Fact]
    public void Projectile_visuals_use_own_or_observed_schema()
    {
        long now = 0;
        var buffer = new SnapshotBuffer(() => now);
        var counter = Own() with
        {
            ObjectId = "counter",
            Y = 120,
            RenderObjectType = SpaceObjectType.Countermeasure,
            OwnProjectile = Own().OwnProjectile! with { Kind = ProjectileKind.Countermissile, Y = 120, StartY = 120, DetonationY = 120, BlastRadiusKm = 1, ProfileId = "wide" }
        };
        buffer.Update(Scene(Own(), Foreign(), counter));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => now, mapSettings: Map(1));
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        Assert.Equal(3, screen.PointProjectileVisuals.Count);
        var own = screen.PointProjectileVisuals["own"];
        Assert.Equal(90, own.PhysicalRadiusPx); Assert.Equal(27, own.CoreRadiusPx);
        Assert.Equal(20, own.RemainingRealSeconds); Assert.NotNull(own.DetonationCenter);
        var foreign = screen.PointProjectileVisuals["foreign"];
        Assert.Null(foreign.PhysicalRadiusPx); Assert.Null(foreign.DetonationCenter); Assert.Null(foreign.RemainingRealSeconds);
        Assert.Empty(foreign.Label); Assert.True(foreign.LabelBounds.IsEmpty);
        Assert.Equal(10, screen.PointProjectileVisuals["counter"].PhysicalRadiusPx);
        Assert.Equal(0, screen.PointProjectileVisuals["counter"].CoreRadiusPx);
        Assert.All(screen.PointProjectileVisuals.Values, v => Assert.DoesNotContain("%", v.Label));
        now = Stopwatch.Frequency * 10; screen.Render(canvas, 1920, 1080);
        Assert.Equal(20, screen.PointProjectileVisuals["own"].RemainingRealSeconds); // pause freezes physical countdown
        screen.OnDeactivated();
    }

    [Theory]
    [InlineData(1.0, 1f)]
    [InlineData(.1, 1.5f)]
    [InlineData(.001, 1f)]
    public void Blast_geometry_scales_correctly_and_marks_minimum_size(double ppu, float uiScale)
    {
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(Scene(Own()));
        var styles = CombatVisualSettings.Default with
        { PointCombat = PointCombatVisualSettings.Default with { BlastStroke = new(255, 0, 255, 255) } };
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0,
            mapSettings: Map(ppu), uiScale: uiScale, combatSettings: styles);
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        bool inspected = false;
        screen.RenderStageCompleted = stage =>
        {
            if (stage == "trails") canvas.Clear(SKColors.Transparent);
            if (stage != "combat_trajectories") return;
            inspected = true;
            var visual = screen.PointProjectileVisuals["own"];
            Assert.Equal(90 * ppu, visual.PhysicalRadiusPx!.Value, 10);
            Assert.Equal(27 * ppu, visual.CoreRadiusPx!.Value, 10);
            Assert.Equal(90 * ppu < 6, visual.HasMinimumMarker);
            var center = visual.DetonationCenter!.Value;
            if (ppu >= .1)
            {
                var magenta = Enumerable.Range(0, 110).Where(dx =>
                {
                    var pixel = bitmap.GetPixel((int)Math.Round(center.X) + dx, (int)Math.Round(center.Y));
                    return pixel.Alpha > 30 && pixel.Red > 240 && pixel.Blue > 240 && pixel.Green < 10;
                }).ToArray();
                Assert.NotEmpty(magenta);
                Assert.InRange(Math.Abs(magenta.Max() - 90 * ppu), 0, 1);
            }
            else Assert.True(bitmap.GetPixel((int)Math.Round(center.X) + 3, (int)Math.Round(center.Y)).Alpha > 0);
        };
        screen.Render(canvas, 1920, 1080); Assert.True(inspected); screen.OnDeactivated();
    }

    [Fact]
    public void Point_effects_use_real_durations_deduplicate_and_do_not_replay_load_baseline()
    {
        long now = 0;
        var store = new CombatEffectStore(() => now) { StartOnPresentation = true };
        var blast = new CombatJournalEntry(1, 0, CombatEventType.TorpedoDetonated, "enemy", "", "flight", 300, 0,
            ProjectileKind: ProjectileKind.Torpedo, BlastRadiusKm: 9);
        var shot = blast with { EventId = 2, Type = CombatEventType.ShotDown };
        store.ReceiveJournal([blast, shot], 0); Assert.Equal(2, store.PointEffects.Count);
        now = Stopwatch.Frequency * 300 / 1000;
        store.ReceiveJournal([blast, shot], 0); Assert.Equal(2, store.PointEffects.Count);
        Assert.Equal(.5, store.PointProgress(blast, 0, now), 5);
        now = Stopwatch.Frequency * 400 / 1000;
        store.ReceiveJournal([blast, shot], 0); Assert.Equal(1, Assert.Single(store.PointEffects).Entry.EventId);
        now = Stopwatch.Frequency;
        store.ReceiveJournal([blast, shot], 0); Assert.Empty(store.PointEffects);
        store.ResetJournal([blast, shot]); store.ReceiveJournal([blast, shot], 0); Assert.Empty(store.PointEffects);
    }

    [Fact]
    public void Transport_watchdog_requires_real_time_instead_of_accelerated_scene_time()
    {
        long ticks = 0;
        var actual = new SnapshotBuffer(() => ticks);
        var mismatched = new SnapshotBuffer(() => ticks * 20);
        var state = Scene() with { CurrentSpeed = SimulationSpeed.Speed1 };
        var packet = new CombatPresentationUpdate(Guid.NewGuid(), 1, 0, [], state, false, 0);
        actual.UpdatePresentation(packet); mismatched.UpdatePresentation(packet);
        ticks = Stopwatch.Frequency * 60 / 1000; // still before the next100ms transport publication
        Assert.False(actual.PresentationPauseRequested);
        Assert.True(mismatched.PresentationPauseRequested);
        ticks = Stopwatch.Frequency * 1001 / 1000;
        Assert.True(actual.PresentationPauseRequested); // the real safety threshold is retained
    }

    [Theory]
    [InlineData(CombatEventType.TorpedoDetonated)]
    [InlineData(CombatEventType.CountermissileDetonated)]
    [InlineData(CombatEventType.ManuallyDetonated)]
    [InlineData(CombatEventType.ShotDown)]
    public void Actual_screen_draws_point_facts_and_expires_them_in_real_time(CombatEventType type)
    {
        long now = 0; var buffer = new SnapshotBuffer(() => now); var baseline = Scene(); buffer.Update(baseline);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => now, mapSettings: Map(1));
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        var entry = new CombatJournalEntry(1, 0, type, "enemy", "", "projectile", 300, 0,
            ProjectileKind: type == CombatEventType.CountermissileDetonated ? ProjectileKind.Countermissile : ProjectileKind.Torpedo,
            BlastRadiusKm: type == CombatEventType.CountermissileDetonated ? 1 : 9);
        buffer.Update(baseline with { SnapshotSequence = 2, CombatJournal = [entry] });
        int alpha = 0;
        screen.RenderStageCompleted = stage =>
        {
            if (stage == "label_plaques") canvas.Clear(SKColors.Transparent);
            if (stage == "combat_effects")
                alpha = Enumerable.Range(1160, 201).SelectMany(x => Enumerable.Range(440, 201).Select(y => (int)bitmap.GetPixel(x, y).Alpha)).Max();
        };
        screen.Render(canvas, 1920, 1080); Assert.True(alpha > 0); Assert.Single(screen.CombatEffects.PointEffects);
        now = Stopwatch.Frequency;
        screen.Render(canvas, 1920, 1080); Assert.Equal(0, alpha); Assert.Empty(screen.CombatEffects.PointEffects);
        screen.OnDeactivated();
    }

    [Fact]
    public async Task Actual_engine_point_payload_reaches_the_screen()
    {
        await using var fixture = new PointCombatSessionFixture();
        fixture.Engine.ReceiveCommand(PointCombatSessionFixture.Launch() with { TargetWorldX = 300 });
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(fixture.Engine.CaptureSnapshot());
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0, mapSettings: Map(1));
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        Assert.Equal(2, screen.PointProjectileVisuals.Count);
        Assert.Single(screen.PointProjectileVisuals.Values, v => v.DetonationCenter is not null);
        Assert.Single(screen.PointProjectileVisuals.Values, v => v.DetonationCenter is null);
        if (Environment.GetEnvironmentVariable("DSS_POINT_RENDER_ARTIFACT_DIRECTORY") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
            using var output = File.Create(Path.Combine(directory, "point-projectiles-headless.png"));
            using var image = SKImage.FromBitmap(bitmap); using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
            encoded.SaveTo(output);
        }
        var own = buffer.Latest!.Snapshot.Objects.Single(o => o.OwnProjectile is not null);
        fixture.Engine.ReceiveCommand(new("render-detonate", 2, PointCombatSessionFixture.Player, PointCombatSessionFixture.Torpedo,
            CombatCommandTypes.Detonate, own.ObjectId));
        buffer.Update(fixture.Engine.CaptureSnapshot());
        screen.Render(canvas, 1920, 1080);
        Assert.DoesNotContain(own.ObjectId, screen.PointProjectileVisuals.Keys);
        Assert.Equal(CombatEventType.ManuallyDetonated, Assert.Single(screen.CombatEffects.PointEffects).Entry.Type);
        screen.OnDeactivated();
    }
}
