using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using Xunit.Abstractions;

namespace DeepSpaceSaga.Client.Tests;

[Collection("Combat forecast performance")]
public sealed class CombatForecastCacheTests(ITestOutputHelper output)
{
    [Fact]
    public void CombatForecast_publishes_across_real_point_engine_updates()
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
        using var engine = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(root, "Scenarios", "CombatMukha", "scenario.json"));
        engine.LoadScenario(scenario with { GameState = scenario.GameState with { MasterSeed = 42 } });
        engine.ReceiveCommand(new("forecast-turn", 1, "SPC-0001", "MOD-PLAYER-ENGINE-01", ShipEngineCommandTypes.TurnRightUntilCancel));
        var cache = new CombatForecastCache();
        var session = Guid.NewGuid();
        int ready = 0, flights = 0, playerFlights = 0, defenses = 0;
        // V3 can end the battle before the former19.2s warmup. Launch immediately and retain all subsequent live updates, including defeat.
        for (int i = 0; i < 1800; i++)
        {
            if (i is 0 or 1)
            {
                var pose = engine.CaptureSnapshotForTests(i * 16, SimulationSpeed.Speed1, i * 16).Objects.Single(o => o.ObjectId == "SPC-0001");
                engine.ReceiveCommand(i == 0
                    ? new("forecast-live", 2, "SPC-0001", "MOD-PLAYER-TORPEDO-01", CombatCommandTypes.Launch,
                        TargetWorldX: pose.X + 600, TargetWorldY: pose.Y, ProfileId: "fast")
                    : new("forecast-defense", 3, "SPC-0001", "MOD-PLAYER-COUNTERMEASURE-01", DefenseCommandTypes.Launch,
                        TargetWorldX: pose.X - 200, TargetWorldY: pose.Y, ProfileId: "wide"));
            }
            var scene = engine.CaptureSnapshotForTests(i * 16, SimulationSpeed.Speed1, i * 16);
            scene = scene with
            {
                Objects = scene.Objects.Select(o => o.ObjectType == SpaceObjectType.NpcShip
                ? FutureTrajectoryProjector.ObservedNpcForecastState(o, 0) : o).ToImmutableArray()
            };
            cache.Prepare(session, scene, CombatForecastOptions.Default);
            ready += cache.Results.Values.Count(r => r.Status is not (CombatForecastStatus.Recalculating or CombatForecastStatus.CalculationLimited));
            flights += cache.Results.Count;
            playerFlights += scene.Objects.Count(o => o.OwnProjectile?.Kind == ProjectileKind.Torpedo);
            defenses += scene.Objects.Count(o => o.OwnProjectile?.Kind == ProjectileKind.Countermissile || o.ObservedProjectile?.Kind == ProjectileKind.Countermissile);
        }
        output.WriteLine($"Ready {ready}/{flights}; hits {cache.Hits}; misses {cache.Misses}");
        Assert.True(playerFlights > 0);
        Assert.True(defenses > 0);
        Assert.True(ready >= flights * 0.95, $"Ready {ready}/{flights}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "ForecastBudget")]
    public async Task CombatForecast_changing_observed_linear_targets_obey_UI_budget(bool includeDefenses)
    {
        var target = new ObjectMotionSnapshot("observed-npc", 200, 50, 2, 45, RenderObjectType: SpaceObjectType.NpcShip);
        var flights = Enumerable.Range(0, 8).Select(i => new ObjectMotionSnapshot($"torpedo-{i}", i * 20, i * 5, 6, 90,
            OwnProjectile: new(ProjectileKind.Torpedo, "player", $"launcher-{i}", i * 20, i * 5,
                i * 20, i * 5, i * 20 + 600, i * 5, 0, 10000, 6, "slow", 9, 200, 20))).ToArray();
        var defenses = includeDefenses ? Enumerable.Range(0, 4).Select(i => new ObjectMotionSnapshot($"defense-{i}", 600 + i * 5, 50, 15, 270,
            OwnProjectile: new(ProjectileKind.Countermissile, "player", $"counter-{i}", 600 + i * 5, 50,
                600 + i * 5, 50, 300 + i * 5, 50, 0, 2000, 15, "wide", 1, 0, 0))).ToArray() : [];
        var predictor = new LinearMotionPredictor(); var cache = new CombatForecastCache(); var session = Guid.NewGuid();
        var timings = new List<double>(); var refreshed = new HashSet<string>(StringComparer.Ordinal);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var uiThread = new Thread(() =>
        {
            try
            {
                for (int frame = -20; frame < 200; frame++)
                {
                    int i = frame + 20;
                    long time = i * 4; //16ms real frames at Speed1 advance four physical milliseconds.
                    var observed = predictor.Predict(target, time) with { Direction = target.Direction + i * .01 };
                    var scene = new AuthoritativeSnapshot((ulong)i + 1, time * 300, SimulationSpeed.Speed1,
                        flights.Concat(defenses).Select(o => predictor.Predict(o, time)).Prepend(observed).ToImmutableArray(), SimulationTimeMs: time);
                    long start = System.Diagnostics.Stopwatch.GetTimestamp();
                    cache.Prepare(session, scene, CombatForecastOptions.Default);
                    double ms = System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                    if (frame >= 0) timings.Add(ms);
                    Assert.Equal(8 + defenses.Length, cache.Results.Count);
                    foreach (var source in flights.Concat(defenses))
                    {
                        var result = cache.Results[source.ObjectId]; var captured = source.OwnProjectile!;
                        Assert.InRange(result.StartMotionTimeMs, 0, time); Assert.Equal(captured.DetonationMotionTimeMs, result.EndMotionTimeMs);
                        Assert.Equal(CombatForecastStatus.Expired, result.Status); Assert.Null(result.ContactMotionTimeMs);
                        Assert.Empty(result.TargetId); Assert.Empty(result.Target);
                        Assert.Equal(captured.DetonationX, result.Projectile[^1].X); Assert.Equal(captured.DetonationY, result.Projectile[^1].Y);
                        refreshed.Add(source.ObjectId);
                    }
                    Thread.Sleep(16);
                }
                completion.SetResult();
            }
            catch (Exception error) { completion.SetException(error); }
        })
        { IsBackground = true, Name = "Point forecast UI budget" };
        uiThread.Start(); await completion.Task;
        Assert.Equal(8 + defenses.Length, refreshed.Count); Assert.True(cache.Hits >= 200); timings.Sort();
        output.WriteLine($"POINT V3:8captured torpedoes +{defenses.Length}captured counters; changing observed NPC;16ms real/4ms physical frames; UI p99={timings[197]:F4}ms; headless only. Retired homing measurements are not relabeled as PASS.");
        Assert.True(timings[197] <= 2, $"Point UI p99={timings[197]}ms");
    }

    private static ObjectMotionSnapshot Own(string id = "own", long time = 0, double start = 0) =>
        new(id, start + time * .06, 0, 6, 90, OwnProjectile: new(ProjectileKind.Torpedo, "player", "launcher-" + id,
            start + time * .06, 0, start, 0, start + 600, 0, 0, 10000, 6, "slow", 9, 200, 20));
    private static AuthoritativeSnapshot Snapshot(long time, params ObjectMotionSnapshot[] objects) =>
        new((ulong)time + 1, time * 300, SimulationSpeed.Speed1, objects.ToImmutableArray(), SimulationTimeMs: time);

    [Fact]
    public void Captured_point_path_reuses_across_unrelated_actor_controls_and_rebuilds_for_a_changed_capture()
    {
        var cache = new CombatForecastCache(); var session = Guid.NewGuid(); var options = new CombatForecastOptions(10000);
        cache.Prepare(session, Snapshot(0, Own(), new("npc", 200, 100, 1, 90)), options);
        var first = cache.Results["own"];
        cache.Prepare(session, Snapshot(100, Own(time: 100), new("npc", 900, 200, 100, 270,
            ActiveEngineCommandType: ShipEngineCommandTypes.Accelerate, MaxSpeedKmS: 200,
            EngineManeuver: new(ShipEngineCommandTypes.Accelerate, 0, 0, true, 1, 0, 0, CompletesOnMotionUpdate: true, MotionSampleTimeMs: 100))), options);
        Assert.Same(first, cache.Results["own"]); Assert.Equal(1, cache.Hits);
        var changed = Own(time: 100); changed = changed with
        {
            OwnProjectile = changed.OwnProjectile! with
            { DetonationX = 700, DetonationMotionTimeMs = 700 / .06 }
        };
        cache.Prepare(session, Snapshot(100, changed), options);
        Assert.NotSame(first, cache.Results["own"]); Assert.Equal(2, cache.Misses);
        Assert.Empty(cache.Results["own"].TargetId); Assert.Empty(cache.Results["own"].Target);
    }

    [Fact]
    public void Time_rewind_session_options_and_clear_invalidate_current_results()
    {
        var cache = new CombatForecastCache(); var session = Guid.NewGuid(); var options = new CombatForecastOptions(10000);
        cache.Prepare(session, Snapshot(100, Own(time: 100)), options); var first = cache.Results["own"];
        cache.Prepare(session, Snapshot(50, Own(time: 50)), options); Assert.NotSame(first, cache.Results["own"]);
        var rewound = cache.Results["own"]; session = Guid.NewGuid();
        cache.Prepare(session, Snapshot(50, Own(time: 50)), options); Assert.NotSame(rewound, cache.Results["own"]);
        cache.Prepare(session, Snapshot(50, Own(time: 50)), new(1000)); Assert.Equal(1050, cache.Results["own"].EndMotionTimeMs);
        Assert.Equal(CombatForecastStatus.NoContact, cache.Results["own"].Status);
        cache.Clear(); Assert.Empty(cache.Results); Assert.Null(cache.Source);
        cache.Prepare(session, null, options); Assert.Empty(cache.Results); Assert.Null(cache.Source);
    }

    [Fact]
    public void New_removed_and_corrected_foreign_observations_replace_stale_paths()
    {
        var cache = new CombatForecastCache(); var session = Guid.NewGuid(); var options = new CombatForecastOptions(1000);
        var foreign = new ObjectMotionSnapshot("foreign", 0, 0, 10, 90,
            ObservedProjectile: new(ProjectileKind.Torpedo, 0, 0, 1, 0, 10, "medium", 5));
        cache.Prepare(session, Snapshot(0, Own(), foreign), options); Assert.Equal(2, cache.Results.Count);
        var corrected = foreign with { X = 123, ObservedProjectile = foreign.ObservedProjectile! with { X = 123 } };
        cache.Prepare(session, Snapshot(100, Own(time: 100), corrected), options);
        Assert.Equal(123, cache.Results["foreign"].Projectile[0].X); Assert.Empty(cache.Results["foreign"].TargetId);
        Assert.Equal(CombatForecastStatus.InsufficientData, cache.Results["foreign"].Status);
        cache.Prepare(session, Snapshot(101, Own(time: 101)), options); Assert.Single(cache.Results); Assert.False(cache.Results.ContainsKey("foreign"));
    }

    [Fact]
    public void Work_is_bounded_to_visible_flights_and_full_geometry_slots()
    {
        var cache = new CombatForecastCache(); var flights = Enumerable.Range(0, CombatForecastCache.MaxVisibleFlights + 17)
            .Select(i => Own("shot-" + i, start: i * 10)).ToArray();
        cache.Prepare(Guid.NewGuid(), Snapshot(0, flights), new(10000));
        Assert.Equal(4096, cache.Results.Count);
        Assert.Equal(16, cache.Results.Values.Count(r => r.Status == CombatForecastStatus.Expired));
        Assert.All(cache.Results.Values.Where(r => r.Status == CombatForecastStatus.CalculationLimited), r => Assert.Equal(2, r.Projectile.Length));
        Assert.All(cache.Results.Values, r => { Assert.Null(r.ContactMotionTimeMs); Assert.Empty(r.TargetId); Assert.Empty(r.Target); });
    }

    [Fact]
    public void Genuine_invalid_point_payload_errors_are_not_swallowed()
    {
        var invalid = Own(); invalid = invalid with { OwnProjectile = invalid.OwnProjectile! with { SpeedKmS = double.NaN } };
        Assert.Throws<ArgumentException>(() => new CombatForecastCache().Prepare(Guid.NewGuid(), Snapshot(0, invalid), new(1000)));
    }
}

[CollectionDefinition("Combat forecast performance", DisableParallelization = true)]
public sealed class CombatForecastPerformanceCollection { }
