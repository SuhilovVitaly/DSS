using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Xunit.Abstractions;

namespace DeepSpaceSaga.Client.Tests;

[Collection("Combat forecast performance")]
public sealed class PointCombatUS0019TK0003Tests(ITestOutputHelper output)
{
    private static ObjectMotionSnapshot Own() => new("own", 0, 0, 6, 90,
        OwnProjectile: new(ProjectileKind.Torpedo, "player", "launcher", 0, 0, 0, 0, 600, 0, 0, 10000, 6, "slow", 9, 200, 20));
    private static ObjectMotionSnapshot Foreign(double x = 400) => new("foreign", x, 0, 6, 270,
        ObservedProjectile: new(ProjectileKind.Torpedo, x, 0, -1, 0, 6, "slow", 9));
    private static AuthoritativeSnapshot Scene(ulong revision = 1, long time = 0, params ObjectMotionSnapshot[] objects) =>
        new(revision, time * 300, SimulationSpeed.Speed1, (objects.Length == 0 ? new[] { Own(), Foreign() } : objects).ToImmutableArray(),
            SimulationTimeMs: time, Encounter: new(CombatEncounterState.Active, 0, null, new(), time / 1000d));

    [Fact]
    public void Forecast_preparation_does_not_copy_the_noncombat_world_every_frame()
    {
        long ticks = 0;
        var buffer = new SnapshotBuffer(() => ticks);
        buffer.Update(Scene(objects: Enumerable.Range(0, 5000)
            .Select(i => new ObjectMotionSnapshot($"asteroid-{i}", i, 0, 0, 0, RenderObjectType: SpaceObjectType.Asteroid)).ToArray()));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => ticks);
        var prepare = typeof(GameSessionScreen).GetMethod("PrepareCombatForecasts", BindingFlags.Instance | BindingFlags.NonPublic)!;
        for (int i = 1; i <= 10; i++) { ticks = Stopwatch.Frequency * i / 1000; prepare.Invoke(screen, null); }
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 11; i <= 20; i++) { ticks = Stopwatch.Frequency * i / 1000; prepare.Invoke(screen, null); }
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        output.WriteLine($"Ten forecast frames with 5000 noncombat objects: {bytes} bytes");
        Assert.True(bytes < 50000, $"Forecast preparation copied the peaceful world: {bytes} bytes");
        screen.OnDeactivated();
    }

    [Fact]
    public void Point_cache_prepares_own_and_foreign_paths_and_invalidates_physical_capture()
    {
        var cache = new CombatForecastCache(); var session = Guid.NewGuid(); var scene = Scene();
        cache.Prepare(session, scene, new(HorizonMs: 20000));
        Assert.Equal(2, cache.Results.Count);
        var own = cache.Results["own"];
        Assert.Equal(10000, own.EndMotionTimeMs); Assert.Equal(600, own.Projectile[^1].X);
        Assert.Empty(own.TargetId); Assert.Empty(own.Target); Assert.Null(own.ContactMotionTimeMs);
        var foreign = cache.Results["foreign"];
        Assert.Equal(CombatForecastStatus.InsufficientData, foreign.Status);
        Assert.Equal(20000, foreign.EndMotionTimeMs); Assert.Equal(-800, foreign.Projectile[^1].X);
        Assert.Empty(foreign.TargetId); Assert.Null(foreign.ContactMotionTimeMs);
        var first = cache.Results;
        cache.Prepare(session, scene, new(HorizonMs: 20000));
        Assert.Same(first, cache.Results);
        var changed = Own() with { OwnProjectile = Own().OwnProjectile! with { CoreFraction = .4, BlastRadiusKm = 2, Damage = 100, Kind = ProjectileKind.Countermissile } };
        cache.Prepare(session, Scene(2, 0, changed, Foreign()), new(HorizonMs: 20000));
        Assert.NotSame(first, cache.Results);
        cache.Prepare(Guid.NewGuid(), Scene(), new(HorizonMs: 20000));
        Assert.NotSame(first, cache.Results);
        cache.Prepare(session, null, CombatForecastOptions.Default);
        Assert.Empty(cache.Results); Assert.Null(cache.Source);
    }

    [Fact]
    public void Client_prediction_uses_admitted_observations_only()
    {
        long ticks = 0; var buffer = new SnapshotBuffer(() => ticks); var cache = new CombatForecastCache();
        var session = Guid.NewGuid(); var bootstrap = Scene();
        buffer.UpdatePresentation(new(session, 1, 0, [], bootstrap, false, 0));
        var future = Scene(2, 1000, Own(), Foreign(9999)) with
        { Encounter = new(CombatEncounterState.Won, 0, 1000, new(), 1) };
        buffer.UpdatePresentation(new(session, 2, 1000, [new(1000, future)], null, false, 0));
        Assert.Equal(CombatEncounterState.Won, buffer.AuthoritativeLatest!.Encounter!.State);
        Assert.Equal(CombatEncounterState.Active, buffer.Presented!.Snapshot.Encounter!.State);
        cache.Prepare(session, buffer.LatestPrediction!.BufferedSnapshot.Snapshot, new(HorizonMs: 2000));
        Assert.Equal(400, cache.Results["foreign"].Projectile[0].X);
        ticks = Stopwatch.Frequency * 5;
        buffer.PreparePresentation();
        Assert.Equal(CombatEncounterState.Won, buffer.Presented!.Snapshot.Encounter!.State);
        cache.Prepare(session, buffer.LatestPrediction!.BufferedSnapshot.Snapshot, new(HorizonMs: 2000));
        Assert.Equal(9999, cache.Results["foreign"].Projectile[0].X);
        var removed = Scene(3, 1000, Foreign(9999)) with { CurrentSpeed = SimulationSpeed.Speed0 };
        var removal = new CombatPresentationUpdate(session, 3, 1000, [new(1000, removed, ["own"])], null, false, 0);
        buffer.UpdatePresentation(removal); buffer.UpdatePresentation(removal);
        ticks += Stopwatch.Frequency;
        buffer.PreparePresentation();
        cache.Prepare(session, buffer.LatestPrediction!.BufferedSnapshot.Snapshot, new(HorizonMs: 2000));
        Assert.DoesNotContain("own", cache.Results.Keys);
        var nextSession = Guid.NewGuid();
        buffer.UpdatePresentation(new(nextSession, 1, 0, [], Scene(1, 0, Own()), false, 0));
        buffer.UpdatePresentation(removal); // a retired session cannot delete the new own flight
        cache.Prepare(nextSession, buffer.LatestPrediction!.BufferedSnapshot.Snapshot, new(HorizonMs: 2000));
        Assert.Single(cache.Results); Assert.Contains("own", cache.Results.Keys);
    }

    [Fact]
    public async Task Actual_point_engine_payload_is_forecast_without_hostile_controls()
    {
        await using var fixture = new PointCombatSessionFixture();
        fixture.Engine.ReceiveCommand(PointCombatSessionFixture.Launch());
        var scene = fixture.Engine.CaptureSnapshot();
        var cache = new CombatForecastCache();
        cache.Prepare(Guid.NewGuid(), scene, new(HorizonMs: 20000));
        Assert.Equal(2, cache.Results.Count);
        Assert.Contains(cache.Results.Values, r => r.EndMotionTimeMs == 3750 && r.Projectile[^1].X == 600);
        Assert.Contains(cache.Results.Values, r => r.Status == CombatForecastStatus.InsufficientData);
    }

    [Theory]
    [InlineData(ProjectileKind.Torpedo)]
    [InlineData(ProjectileKind.Countermissile)]
    public void Point_projector_keeps_own_endpoint_and_never_invents_foreign_fuse(ProjectileKind kind)
    {
        var own = Own() with { OwnProjectile = Own().OwnProjectile! with { Kind = kind } };
        var path = CombatForecast.PointProjectile(own, null, 1500);
        var geometry = CombatTrajectoryProjector.FromPointForecast(path, own, 1500);
        Assert.Equal(new FutureTrajectoryPoint(600, 0), geometry.Detonation);
        Assert.Equal(8.5, geometry.DetonationEtaSeconds); Assert.Equal(9, geometry.BlastRadiusKm);
        Assert.Equal(kind, geometry.Kind); Assert.False(geometry.IsObserved);
        Assert.Equal(90, geometry.Prediction[0].X); Assert.Equal(600, geometry.Prediction[^1].X);
        Assert.Null(geometry.Intercept); Assert.Empty(geometry.Target);
        var observed = Foreign();
        var ray = CombatTrajectoryProjector.FromPointForecast(CombatForecast.PointProjectile(observed, null, 1500), observed, 1500);
        Assert.True(ray.IsObserved); Assert.Null(ray.Detonation); Assert.Null(ray.DetonationEtaSeconds);
        Assert.Null(ray.Intercept); Assert.Null(ray.EtaSeconds); Assert.Empty(ray.Target);
    }

    [Fact]
    public void Cache_reuses_physical_paths_across_samples_and_ignores_unrelated_AI_payload()
    {
        var cache = new CombatForecastCache(); var session = Guid.NewGuid();
        cache.Prepare(session, Scene(), new(HorizonMs: 20000)); var first = cache.Results;
        var own = Own() with { X = 60, OwnProjectile = Own().OwnProjectile! with { X = 60, Reporter = new(BattleReportReporterRole.TorpedoOperator, "new-speaker") } };
        var npc = new ObjectMotionSnapshot("npc", 500, 0, 1, 0, RenderObjectType: SpaceObjectType.NpcShip,
            HostileCombat: new("hidden-target", "private-engine", "private-weapon", 50, true, HostileCombatPhase.Pursuit,
                new(0, 100000, 0, 500, 0, 1, 0, 10, 50)));
        cache.Prepare(session, Scene(2, 1000, own, Foreign(340), npc), new(HorizonMs: 20000));
        Assert.Same(first, cache.Results);
        Assert.Empty(cache.Results["own"].TargetId); Assert.Empty(cache.Results["foreign"].TargetId);
        cache.Prepare(session, Scene(3, 1000) with { Objects = [] }, new(HorizonMs: 20000));
        Assert.Empty(cache.Results);
    }

    [Theory]
    [InlineData("kind")]
    [InlineData("speed")]
    [InlineData("radius")]
    [InlineData("damage")]
    [InlineData("core")]
    [InlineData("deadline")]
    [InlineData("endpoint")]
    public void Every_captured_physical_field_invalidates_the_cache(string field)
    {
        var cache = new CombatForecastCache(); var session = Guid.NewGuid();
        cache.Prepare(session, Scene(), new(HorizonMs: 20000)); var first = cache.Results;
        var captured = Own().OwnProjectile!;
        captured = field switch
        {
            "kind" => captured with { Kind = ProjectileKind.Countermissile },
            "speed" => captured with { SpeedKmS = 10 },
            "radius" => captured with { BlastRadiusKm = 2 },
            "damage" => captured with { Damage = 100 },
            "core" => captured with { CoreFraction = .4 },
            "deadline" => captured with { DetonationMotionTimeMs = 12000 },
            _ => captured with { DetonationX = 700 }
        };
        cache.Prepare(session, Scene(2, 0, Own() with { OwnProjectile = captured }, Foreign()), new(HorizonMs: 20000));
        Assert.NotSame(first, cache.Results);
    }

    [Fact]
    public void Excess_visible_point_flights_are_explicitly_calculation_limited()
    {
        var objects = Enumerable.Range(0, CombatForecastCache.MaxFullFlights + 1)
            .Select(i => Own() with { ObjectId = $"point-{i:D2}" }).ToArray();
        var cache = new CombatForecastCache();
        cache.Prepare(Guid.NewGuid(), Scene(1, 0, objects), new(HorizonMs: 20000));
        Assert.Equal(17, cache.Results.Count);
        Assert.Equal(CombatForecastStatus.CalculationLimited, cache.Results["point-16"].Status);
        Assert.Null(cache.Results["point-16"].ContactMotionTimeMs);
    }

    [Fact]
    [Trait("Category", "ForecastBudget")]
    public void Point_cache_and_geometry_obey_UI_budget()
    {
        var session = Guid.NewGuid(); var cache = new CombatForecastCache(); var timings = new List<double>();
        var geometry = new Dictionary<string, CombatTrajectoryProjector.Geometry>();
        for (int frame = 0; frame < 220; frame++)
        {
            long now = frame * 16;
            var own = Enumerable.Range(0, 8).Select(i => Own() with
            {
                ObjectId = $"own-{i}",
                X = .06 * now,
                Y = i * 20,
                OwnProjectile = Own().OwnProjectile! with { X = .06 * now, Y = i * 20, StartY = i * 20, DetonationY = i * 20 }
            });
            var foreign = Enumerable.Range(0, 4).Select(i => Foreign(400 - .06 * now) with { ObjectId = $"foreign-{i}" });
            var scene = Scene((ulong)frame + 1, now, own.Concat(foreign).ToArray());
            long start = Stopwatch.GetTimestamp();
            cache.Prepare(session, scene, new(HorizonMs: 20000));
            foreach (var projectile in scene.Objects)
            {
                geometry.TryGetValue(projectile.ObjectId, out var previous);
                geometry[projectile.ObjectId] = CombatTrajectoryProjector.FromPointForecast(cache.Results[projectile.ObjectId], projectile, now, previous);
            }
            if (frame >= 20) timings.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
        timings.Sort();
        output.WriteLine($"12 point flights, 200 frames after20 warmup; UI prepare+geometry p99={timings[197]:F4}ms; headless only");
        Assert.Equal(12, cache.Results.Count);
        Assert.True(timings[197] <= 2, $"point UI p99={timings[197]}ms");
    }
}
