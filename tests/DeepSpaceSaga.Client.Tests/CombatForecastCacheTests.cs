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
    private static AuthoritativeSnapshot Scene(double turn = 20)
    {
        var target = new ObjectMotionSnapshot("ship", 0, -200, 1, 90, MaxSpeedKmS: 2,
            HostileCombat: new("owner", "engine", "weapon", 50, true, HostileCombatPhase.Pursuit,
                new(0, 100, 0, 0, -200, 1, 90, 0, turn)));
        var origin = new ObjectMotionSnapshot("torpedo", 0, 0, 3, 0);
        var route = TorpedoGuidanceMath.Plan(origin, target, 3, 90);
        var projectile = origin with { Torpedo = new("owner", "launcher", "ship", 0, 3, 90, 0, 0, route, ExpiresAtMotionTimeMs: 120_000) };
        var cmRoute = TorpedoGuidanceMath.Plan(target, origin, 12, 90);
        var defense = target with
        {
            ObjectId = "defense",
            HostileCombat = null,
            Torpedo = null,
            Countermeasure = new("ship", "defense-module", "torpedo", CountermeasurePhase.Guiding, cmRoute, 0, 500,
                new(null!, 0), SpeedKmS: 12, TurnRateDegPerSec: 90, ExpiresAtMotionTimeMs: 60_000)
        };
        return new(1, 0, SimulationSpeed.Speed0, [target, projectile, defense]);
    }

    [Fact]
    public async Task CombatForecast_publishes_across_real_point_engine_updates()
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
            if (cache.PendingWork is { } worker) await worker;
        }
        cache.CancelPending();
        if (cache.PendingWork is { } pending) await pending;
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
                    Assert.Null(cache.PendingWork);
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

    [Theory]
    [InlineData(320, 0, LaunchMode.Auto)]
    [InlineData(1920, 0, LaunchMode.Auto)]
    [InlineData(2560, 0, LaunchMode.Auto)]
    [InlineData(3200, 0, LaunchMode.Auto)]
    [InlineData(2560, 0, LaunchMode.Manual)]
    [InlineData(2560, 2, LaunchMode.Auto)]
    [InlineData(3200, 3, LaunchMode.Manual)]
    [InlineData(32, -5, LaunchMode.Auto)]
    [InlineData(512, 10, LaunchMode.Auto)]
    [InlineData(2048, -2, LaunchMode.Manual)]
    [InlineData(1000, 8, LaunchMode.Auto)]
    public void CombatForecast_defense_replans_match_the_authoritative_planner(int now, int offset, LaunchMode mode)
    {
        var basis = Scene(0);
        var target = basis.Objects[0] with { HostileCombat = null };
        var predictor = new LinearMotionPredictor();
        var observed = predictor.Predict(target, now) with { Direction = target.Direction + now / 16 * 0.01 };
        var origin = basis.Objects[1] with { X = offset * 20 };
        origin = origin with { Torpedo = origin.Torpedo! with { Route = TorpedoGuidanceMath.Plan(origin, target, 3, 90) } };
        var flight = predictor.Predict(origin, now);
        var defenseSource = basis.Objects[2] with
        {
            Countermeasure = basis.Objects[2].Countermeasure! with
            {
                Route = TorpedoGuidanceMath.Plan(target, origin, 12, 90)
            }
        };
        var defense = predictor.Predict(defenseSource, now);
        defense = defense with { Countermeasure = defense.Countermeasure! with { Mode = mode } };
        var forecast = CombatForecast.Torpedo(flight, observed, now);
        var result = CombatForecast.Countermeasure(defense, flight, forecast, now);
        int replans = 0;
        for (int i = 1; i < result.Legs.Length; i++)
        {
            var leg = result.Legs[i];
            var previous = result.Legs[i - 1].Route;
            if (leg.Route.StartMotionTimeMs == previous.StartMotionTimeMs) continue;
            replans++;
            long time = (long)leg.From;
            var pose = TorpedoGuidanceMath.PredictPose(previous, time - previous.StartMotionTimeMs);
            var source = new ObjectMotionSnapshot("defense", pose.X, pose.Y, 12, pose.Direction);
            var targetRoute = forecast.Legs.First(l => l.To > time || l == forecast.Legs[^1]).Route;
            double deadline = mode == LaunchMode.Manual ? forecast.EndMotionTimeMs :
                targetRoute.StartMotionTimeMs + targetRoute.Segments.Sum(segment => segment.DurationMs);
            var expected = CountermeasureGuidanceMath.PlanManual(source, targetRoute, time, 60_000 - time, 12, 90, deadline).Route;
            Assert.Equal(expected.HasIntercept, leg.Route.HasIntercept);
            Assert.Equal(expected.Segments.Length, leg.Route.Segments.Length);
            Assert.InRange(Math.Abs(expected.Segments.Sum(segment => segment.DurationMs) -
                leg.Route.Segments.Sum(segment => segment.DurationMs)), 0, 1e-5);
            foreach (double delta in new[] { 0d, 50, 100, expected.Segments.Sum(segment => segment.DurationMs) })
            {
                var actualPose = TorpedoGuidanceMath.PredictPose(leg.Route, delta);
                var expectedPose = TorpedoGuidanceMath.PredictPose(expected, delta);
                Assert.InRange(Math.Abs(actualPose.X - expectedPose.X), 0, 1e-6);
                Assert.InRange(Math.Abs(actualPose.Y - expectedPose.Y), 0, 1e-6);
                Assert.InRange(Math.Abs((actualPose.Direction - expectedPose.Direction + 540) % 360 - 180), 0, 1e-7);
            }
        }
        Assert.True(replans > 0);
    }

    [Theory]
    [InlineData(23.711)]
    [InlineData(90)]
    public void CombatForecast_tangent_defense_preserves_the_authoritative_route_branch(double angle)
    {
        double radians = angle * Math.PI / 180, radius = 120 / (Math.PI / 2), rendezvous = 100 + angle / 90 * 1000;
        double x = radius * (1 - Math.Cos(radians)) - 0.03 * rendezvous * Math.Sin(radians);
        double y = -12 - radius * Math.Sin(radians) + 0.03 * rendezvous * Math.Cos(radians);
        var destination = new ObjectMotionSnapshot("ship", x + 1000 * Math.Sin(radians), y - 1000 * Math.Cos(radians), 0, 0);
        var origin = new ObjectMotionSnapshot("torpedo", x, y, 3, angle);
        var route = TorpedoGuidanceMath.Plan(origin, destination, 3, 90);
        var torpedo = origin with { Torpedo = new("owner", "launcher", "ship", 0, 3, 90, 0, 0, route, ExpiresAtMotionTimeMs: 200_000) };
        var coast = new TorpedoRoute(0, 1, TorpedoRoutePhase.Straight, false, [new(0, 0, 0, 12, 0, 100)]);
        var defense = new ObjectMotionSnapshot("defense", 0, 0, 12, 0,
            Countermeasure: new("owner", "module", "torpedo", CountermeasurePhase.Guiding, coast, 0, 500,
                new(null!, 0), Mode: LaunchMode.Manual, SpeedKmS: 12, TurnRateDegPerSec: 90, ExpiresAtMotionTimeMs: 2000));
        var targetForecast = CombatForecast.Torpedo(torpedo, destination, 0);
        var forecast = CombatForecast.Countermeasure(defense, torpedo, targetForecast, 0);
        var actual = forecast.Legs.First(l => l.Route.StartMotionTimeMs == 100).Route;
        var targetRoute = targetForecast.Legs.First(l => l.To > 100 || l == targetForecast.Legs[^1]).Route;
        var expected = CountermeasureGuidanceMath.PlanManual(new("defense", 0, -12, 12, 0), targetRoute,
            100, 1900, 12, 90, 2000).Route;
        Assert.Equal(expected.HasIntercept, actual.HasIntercept);
        Assert.InRange(Math.Abs(expected.Segments.Sum(segment => segment.DurationMs) - actual.Segments.Sum(segment => segment.DurationMs)), 0, 1e-5);
        foreach (double time in new[] { 0d, 100, 1000, 1900 })
        {
            var a = TorpedoGuidanceMath.PredictPose(actual, time);
            var e = TorpedoGuidanceMath.PredictPose(expected, time);
            Assert.InRange(Math.Abs(a.X - e.X), 0, 1e-6);
            Assert.InRange(Math.Abs(a.Y - e.Y), 0, 1e-6);
        }
    }

    [Fact]
    public async Task CombatForecast_changing_linear_target_keeps_the_dependent_defense_current()
    {
        var basis = Scene(0);
        var target = basis.Objects[0] with { HostileCombat = null };
        var cache = new CombatForecastCache();
        var session = Guid.NewGuid();
        var predictor = new LinearMotionPredictor();
        for (int i = 0; i < 80; i++)
        {
            long now = i * 16;
            var observed = predictor.Predict(target, now) with { Direction = target.Direction + i * 0.01 };
            var objects = basis.Objects.Skip(1).Select(o => predictor.Predict(o, now)).Prepend(observed).ToImmutableArray();
            cache.Prepare(session, basis with { SimulationTimeMs = now, Objects = objects }, CombatForecastOptions.Default);
            Assert.All(cache.Results.Values, r =>
            {
                Assert.DoesNotContain(r.Status, new[] { CombatForecastStatus.Recalculating, CombatForecastStatus.CalculationLimited });
                Assert.Equal(now, r.StartMotionTimeMs);
            });
            var expectedFlight = CombatForecast.Torpedo(objects[1], observed, now);
            var expectedDefense = CombatForecast.Countermeasure(objects[2], objects[1], expectedFlight, now);
            Assert.Equal(expectedDefense.Status, cache.Results["defense"].Status);
            Assert.Equal(expectedDefense.EndMotionTimeMs, cache.Results["defense"].EndMotionTimeMs, 6);
            Assert.Equal(expectedDefense.Projectile[^1], cache.Results["defense"].Projectile[^1]);
            Assert.Null(cache.PendingWork);
            // Let wall time cross the former 250 ms relabeling threshold.
            await Task.Delay(5);
        }
    }

    [Fact]
    public async Task CombatForecast_unreachable_linear_target_stays_off_the_UI_thread()
    {
        var basis = Scene();
        var target = basis.Objects[0] with { HostileCombat = null, SpeedKmS = 10, Direction = 0 };
        var flight = basis.Objects[1];
        var route = TorpedoGuidanceMath.Plan(flight, target, 3, 90);
        Assert.False(route.HasIntercept);
        flight = flight with { Torpedo = flight.Torpedo! with { Route = route } };
        var cache = new CombatForecastCache();
        cache.Prepare(Guid.NewGuid(), basis with { Objects = [target, flight] }, CombatForecastOptions.Default);
        Assert.NotNull(cache.PendingWork);
        Assert.Null(cache.Results["torpedo"].ContactMotionTimeMs);
        cache.CancelPending();
        await cache.PendingWork!;
    }

    [Theory]
    [InlineData(ShipEngineCommandTypes.Accelerate, 2, 0.5)]
    [InlineData(ShipEngineCommandTypes.Brake, 0, -0.5)]
    public async Task CombatForecast_zero_duration_command_at_speed_limit_does_not_restart_forever(string command, double speed, double acceleration)
    {
        var basis = Scene();
        var target = basis.Objects[0] with
        {
            HostileCombat = null,
            SpeedKmS = speed,
            ActiveEngineCommandType = command,
            EngineManeuver = new(command, 0, 0, true, acceleration, 0, 0, CompletesOnMotionUpdate: true, MotionSampleTimeMs: 0)
        };
        var flight = basis.Objects[1];
        flight = flight with { Torpedo = flight.Torpedo! with { Route = TorpedoGuidanceMath.Plan(flight, target, 3, 90) } };
        basis = basis with { Objects = [target, flight] };
        var cache = new CombatForecastCache();
        var session = Guid.NewGuid();
        cache.Prepare(session, basis, CombatForecastOptions.Default);
        await cache.PendingWork!;
        var currentTarget = CurrentManeuverForecast.Sample(target, 16);
        var current = basis with
        {
            SimulationTimeMs = 16,
            Objects = [currentTarget, TorpedoGuidanceMath.Predict(flight, 16)]
        };
        cache.Prepare(session, current, CombatForecastOptions.Default);
        Assert.Equal(CombatForecastStatus.Contact, cache.Results["torpedo"].Status);
        Assert.Equal(0, cache.Results["torpedo"].StartMotionTimeMs);
        Assert.Null(cache.PendingWork);
        current = current with { Objects = current.Objects.SetItem(0, currentTarget with { SpeedKmS = 1 }) };
        cache.Prepare(session, current, CombatForecastOptions.Default);
        Assert.Null(cache.Results["torpedo"].ContactMotionTimeMs);
        cache.CancelPending();
        if (cache.PendingWork is { } pending) await pending;
    }

    [Theory]
    [InlineData(149999, CombatForecastStatus.Expired)]
    [InlineData(150000, CombatForecastStatus.Contact)]
    [InlineData(150001, CombatForecastStatus.Contact)]
    public void CombatForecast_linear_contact_at_expiry_stays_inclusive(long expiry, CombatForecastStatus status)
    {
        var target = new ObjectMotionSnapshot("target", 0, -1505 - TorpedoCollisionMath.DistanceTolerance, 0, 0);
        var origin = new ObjectMotionSnapshot("torpedo", 0, 0, 1, 0);
        var route = TorpedoGuidanceMath.Plan(origin, target, 1, 90);
        Assert.True(route.HasIntercept);
        var projectile = origin with
        {
            Torpedo = new("owner", "launcher", "target", 0, 1, 90, 0, 0, route,
            ExpiresAtMotionTimeMs: expiry)
        };
        var result = CombatForecast.Torpedo(projectile, target, 0);
        Assert.Equal(status, result.Status);
        Assert.True(result.EndMotionTimeMs <= expiry);
        if (status == CombatForecastStatus.Contact) Assert.Equal(150000, result.EndMotionTimeMs, 5);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CombatForecast_zero_duration_completion_invalidates_the_entire_cached_future(bool worker)
    {
        var basis = Scene();
        var target = basis.Objects[0] with
        {
            HostileCombat = null,
            ActiveEngineCommandType = ShipEngineCommandTypes.TurnRightStep,
            EngineManeuver = new(ShipEngineCommandTypes.TurnRightStep, 0, 0, false, 0, 0, 90,
                CompletesOnMotionUpdate: true, MotionSampleTimeMs: 0)
        };
        basis = basis with { Objects = basis.Objects.SetItem(0, target) };
        var cache = new CombatForecastCache { UseWorker = worker };
        var session = Guid.NewGuid();
        cache.Prepare(session, basis, CombatForecastOptions.Default);
        if (cache.PendingWork is { } pending) { await pending; cache.Prepare(session, basis, CombatForecastOptions.Default); }
        var old = cache.Results["torpedo"];
        var predictor = new LinearMotionPredictor();
        var currentTarget = CurrentManeuverForecast.Sample(target, 37);
        var currentFlight = predictor.Predict(basis.Objects[1], 37);
        var currentDefense = predictor.Predict(basis.Objects[2], 37);
        var current = basis with { SnapshotSequence = 2, SimulationTimeMs = 37, Objects = [currentTarget, currentFlight, currentDefense] };
        cache.Prepare(session, current, CombatForecastOptions.Default);
        Assert.NotSame(old, cache.Results["torpedo"]);
        if (cache.PendingWork is { } next) { await next; cache.Prepare(session, current, CombatForecastOptions.Default); }
        var expected = CombatForecast.Torpedo(currentFlight, currentTarget, 37);
        Assert.Equal(expected.EndMotionTimeMs, cache.Results["torpedo"].EndMotionTimeMs, 6);
        Assert.Equal(expected.Projectile[^1], cache.Results["torpedo"].Projectile[^1]);
        Assert.All(cache.Results.Values, r => Assert.Equal(37, r.StartMotionTimeMs));
    }

    [Fact]
    public async Task CombatForecast_paused_fresh_revision_publishes_bounded_missing_data()
    {
        var scene = Scene();
        scene = scene with
        {
            Objects = scene.Objects.SetItem(1, scene.Objects[1] with
            {
                Torpedo = scene.Objects[1].Torpedo! with { ExpiresAtMotionTimeMs = null }
            })
        };
        var cache = new CombatForecastCache();
        var session = Guid.NewGuid();
        cache.Prepare(session, scene, CombatForecastOptions.Default);
        await cache.PendingWork!;
        var fresh = scene with { SnapshotSequence = 2, Objects = scene.Objects.Select(o => o with { }).ToImmutableArray() };
        cache.Prepare(session, fresh, CombatForecastOptions.Default);
        Assert.Equal(CombatForecastStatus.InsufficientData, cache.Results["torpedo"].Status);
        Assert.Equal(CombatForecastStatus.InsufficientData, cache.Results["defense"].Status);
        Assert.All(cache.Results.Values, r => Assert.Null(r.ContactMotionTimeMs));
        Assert.Null(cache.PendingWork);
    }

    [Fact]
    public async Task CombatForecast_changed_independent_target_cannot_starve_a_torpedo_and_its_defense()
    {
        var scene = Scene();
        var otherTarget = scene.Objects[0] with { ObjectId = "other-ship", X = 300 };
        var otherFlight = scene.Objects[1] with
        {
            ObjectId = "other-torpedo",
            Torpedo = scene.Objects[1].Torpedo! with { TargetObjectId = otherTarget.ObjectId }
        };
        scene = scene with { Objects = scene.Objects.Add(otherTarget).Add(otherFlight) };
        var cache = new CombatForecastCache();
        var session = Guid.NewGuid();
        cache.Prepare(session, scene, CombatForecastOptions.Default);
        await cache.PendingWork!;
        var changed = scene with
        {
            SnapshotSequence = 2,
            Objects = scene.Objects.SetItem(3,
            otherTarget with { Direction = otherTarget.Direction + 30 })
        };
        cache.Prepare(session, changed, CombatForecastOptions.Default);
        Assert.Equal(CombatForecastStatus.Contact, cache.Results["torpedo"].Status);
        Assert.NotEqual(CombatForecastStatus.Recalculating, cache.Results["defense"].Status);
        Assert.Equal(CombatForecastStatus.Recalculating, cache.Results["other-torpedo"].Status);
        Assert.Null(cache.Results["other-torpedo"].ContactMotionTimeMs);
        await cache.PendingWork!;
        cache.Prepare(session, changed, CombatForecastOptions.Default);
        Assert.NotEqual(CombatForecastStatus.Recalculating, cache.Results["other-torpedo"].Status);
    }

    [Fact]
    public async Task CombatForecast_unrelated_control_changes_do_not_cancel_valid_work()
    {
        var basis = Scene();
        var scene = basis with { Objects = basis.Objects.Add(new("unrelated", 1000, 1000, 1, 0)) };
        var cache = new CombatForecastCache();
        var session = Guid.NewGuid();
        cache.Prepare(session, scene, CombatForecastOptions.Default);
        var worker = cache.PendingWork!;
        var changed = scene with
        {
            SnapshotSequence = 2,
            Objects = scene.Objects.SetItem(3,
            scene.Objects[3] with { ActiveEngineCommandType = ShipEngineCommandTypes.Brake, IsDestroyed = true })
        };
        cache.Prepare(session, changed, CombatForecastOptions.Default);
        await worker;
        cache.Prepare(session, changed, CombatForecastOptions.Default);
        Assert.Equal(CombatForecastStatus.Contact, cache.Results["torpedo"].Status);
        Assert.NotEqual(CombatForecastStatus.Recalculating, cache.Results["defense"].Status);
        cache.CancelPending();
        if (cache.PendingWork is { } pending) await pending;
    }

    [Fact]
    public async Task CombatForecast_continuous_changes_bound_recalculating_without_publishing_old_contacts()
    {
        var cache = new CombatForecastCache();
        var session = Guid.NewGuid();
        var scene = Scene();
        cache.Prepare(session, scene, CombatForecastOptions.Default);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        int revision = 0;
        do
        {
            if (cache.PendingWork is { } worker) await worker;
            scene = Scene(++revision % 2 == 0 ? 20 : -20);
            cache.Prepare(session, scene, CombatForecastOptions.Default);
            Assert.All(cache.Results.Values, r => Assert.Null(r.ContactMotionTimeMs));
            await Task.Delay(10);
        } while (watch.ElapsedMilliseconds < 300);
        Assert.All(cache.Results.Values, r => Assert.Equal(CombatForecastStatus.CalculationLimited, r.Status));
        cache.CancelPending();
        if (cache.PendingWork is { } pending) await pending;
    }

    [Fact]
    public void CombatForecast_cancellation_returns_limited_geometry_without_throwing()
    {
        var scene = Scene();
        var target = scene.Objects[0];
        var torpedo = scene.Objects[1];
        var defense = scene.Objects[2];
        var targetForecast = CombatForecast.Torpedo(torpedo, target, 0);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var flight = CombatForecast.Torpedo(torpedo, target, 0, cancellationToken: cancellation.Token);
        var interception = CombatForecast.Countermeasure(defense, torpedo, targetForecast, 0,
            cancellationToken: cancellation.Token);

        foreach (var result in new[] { flight, interception })
        {
            Assert.Equal(CombatForecastStatus.CalculationLimited, result.Status);
            Assert.Null(result.ContactMotionTimeMs);
            Assert.Empty(result.Legs);
            Assert.Equal(0, result.EndMotionTimeMs);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CombatForecast_canceled_worker_completes_without_throwing_and_cannot_publish(bool parallel)
    {
        var cache = new CombatForecastCache();
        var scene = parallel ? Load(20) : Scene();
        var session = Guid.NewGuid();
        cache.Prepare(session, scene, CombatForecastOptions.Default);
        var pending = Assert.IsAssignableFrom<Task>(cache.PendingWork);
        cache.CancelPending();
        await pending;
        Assert.True(pending.IsCompletedSuccessfully);

        cache.Prepare(session, scene, CombatForecastOptions.Default);
        Assert.Equal(0, cache.Hits);
        Assert.All(cache.Results.Values, r => Assert.Contains(r.Status,
            new[] { CombatForecastStatus.Recalculating, CombatForecastStatus.CalculationLimited }));
        cache.CancelPending();
        if (cache.PendingWork is { } next) await next;
    }

    [Fact]
    public async Task CombatForecast_worker_preserves_genuine_calculation_errors()
    {
        var cache = new CombatForecastCache();
        cache.Prepare(Guid.NewGuid(), Scene(), new CombatForecastOptions(MaxSteps: 0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await cache.PendingWork!);
    }

    [Fact]
    public void CombatForecast_cache_invalidates_the_entire_chain_on_paused_control_change()
    {
        var cache = new CombatForecastCache { UseWorker = false };
        var scene = Scene();
        var session = Guid.NewGuid();
        cache.Prepare(session, scene, CombatForecastOptions.Default);
        var first = cache.Results;
        cache.Prepare(session, scene with { CurrentSpeed = SimulationSpeed.Speed4 }, CombatForecastOptions.Default);
        Assert.Same(first, cache.Results);
        Assert.Equal(1, cache.Hits);
        cache.Prepare(session, Scene(-20), CombatForecastOptions.Default);
        Assert.NotSame(first, cache.Results);
        Assert.NotEqual(first["torpedo"].Projectile, cache.Results["torpedo"].Projectile);
        Assert.NotSame(first["defense"], cache.Results["defense"]);
        Assert.Equal(cache.Results["torpedo"].StartMotionTimeMs, cache.Results["defense"].StartMotionTimeMs);
        cache.Prepare(Guid.NewGuid(), scene with { Objects = [] }, CombatForecastOptions.Default);
        Assert.Empty(cache.Results);
    }

    [Fact]
    public async Task CombatForecast_worker_discards_old_revision_and_session()
    {
        var cache = new CombatForecastCache();
        var session = Guid.NewGuid();
        cache.Prepare(session, Scene(), CombatForecastOptions.Default);
        Assert.All(cache.Results.Values, r => Assert.Equal(CombatForecastStatus.Recalculating, r.Status));
        await cache.PendingWork!;
        var changed = Scene(-20);
        cache.Prepare(Guid.NewGuid(), changed, CombatForecastOptions.Default);
        Assert.All(cache.Results.Values, r => Assert.Equal(CombatForecastStatus.Recalculating, r.Status));
        await cache.PendingWork!;
        // A second session change cannot publish even an otherwise matching scene.
        var finalSession = Guid.NewGuid();
        cache.Prepare(finalSession, changed, CombatForecastOptions.Default);
        await cache.PendingWork!;
        cache.Prepare(finalSession, changed, CombatForecastOptions.Default);
        Assert.Equal(CombatForecastStatus.Contact, cache.Results["torpedo"].Status);
        Assert.True(cache.Results["torpedo"].Target[^1].X > 0);
    }

    [Fact]
    public void CombatForecast_cache_reuses_equivalent_fresh_module_health_and_ui_projections()
    {
        var cache = new CombatForecastCache { UseWorker = false };
        var scene = Scene();
        var target = scene.Objects[0] with
        {
            ModuleHealth = [new("engine", "Engine", 10, 10, 10, 10, null, false)],
            Defense = new(false, null, DefenseState.Ready, PendingBurstShots: [])
        };
        scene = scene with { Objects = scene.Objects.SetItem(0, target) };
        var session = Guid.NewGuid();
        cache.Prepare(session, scene, CombatForecastOptions.Default);
        var first = cache.Results;
        var refreshed = target with
        {
            DisplayName = "Updated name",
            ModuleHealth = [new("engine", "Engine", 10, 10, 10, 10, null, false)],
            Defense = target.Defense! with { PendingBurstShots = [] }
        };
        cache.Prepare(session, scene with { SnapshotSequence = 2, Objects = scene.Objects.SetItem(0, refreshed) }, CombatForecastOptions.Default);
        Assert.Same(first, cache.Results);
        refreshed = refreshed with { ModuleHealth = [new("engine", "Engine", 10, 10, 5, 10, null, false)] };
        cache.Prepare(session, scene with { SnapshotSequence = 3, Objects = scene.Objects.SetItem(0, refreshed) }, CombatForecastOptions.Default);
        Assert.NotSame(first, cache.Results);
    }

    [Fact]
    public async Task CombatForecast_maneuver_recalculation_keeps_planned_paths_without_a_stale_contact()
    {
        var cache = new CombatForecastCache();
        var session = Guid.NewGuid();
        var scene = Scene();
        cache.Prepare(session, scene, CombatForecastOptions.Default);
        await cache.PendingWork!;
        cache.Prepare(session, scene, CombatForecastOptions.Default);
        Assert.Equal(CombatForecastStatus.Contact, cache.Results["torpedo"].Status);

        var changed = Scene(-20);
        cache.Prepare(session, changed, CombatForecastOptions.Default);
        foreach (var (id, result) in cache.Results)
        {
            var source = changed.Objects.Single(o => o.ObjectId == id);
            var target = changed.Objects.Single(o => o.ObjectId == result.TargetId);
            var route = source.Torpedo?.Route ?? source.Countermeasure!.Route;
            Assert.Equal(CombatForecastStatus.Recalculating, result.Status);
            Assert.Null(result.ContactMotionTimeMs);
            var geometry = CombatTrajectoryProjector.FromForecast(result, changed.MotionTimeMs);
            Assert.Null(geometry.Intercept);
            Assert.Null(geometry.EtaSeconds);
            Assert.True(result.EndMotionTimeMs > changed.MotionTimeMs + 1000);
            Assert.InRange(result.Projectile.Length, 3, 129);
            Assert.Equal(result.Projectile.Length, result.Target.Length);
            for (int i = 0; i < result.Projectile.Length; i++)
            {
                var point = result.Projectile[i];
                var pose = TorpedoGuidanceMath.PredictPose(route, point.MotionTimeMs - route.StartMotionTimeMs);
                var destination = CurrentManeuverForecast.Sample(target, point.MotionTimeMs - changed.MotionTimeMs);
                Assert.Equal(pose.X, point.X, 6);
                Assert.Equal(pose.Y, point.Y, 6);
                Assert.Equal(point.MotionTimeMs, result.Target[i].MotionTimeMs);
                Assert.Equal(destination.X, result.Target[i].X, 6);
                Assert.Equal(destination.Y, result.Target[i].Y, 6);
            }
        }
        await cache.PendingWork!;
        cache.Prepare(session, changed, CombatForecastOptions.Default);
        Assert.Equal(CombatForecastStatus.Contact, cache.Results["torpedo"].Status);
    }

    [Fact]
    public async Task CombatForecast_provisional_paths_respect_expiry_and_point_budget()
    {
        var cache = new CombatForecastCache();
        var scene = Scene();
        var torpedo = scene.Objects[1];
        scene = scene with
        {
            Objects = scene.Objects.SetItem(1, torpedo with
            {
                Torpedo = torpedo.Torpedo! with { ExpiresAtMotionTimeMs = 500 }
            })
        };
        cache.Prepare(Guid.NewGuid(), scene, new CombatForecastOptions(MaxPoints: 4));
        foreach (var result in cache.Results.Values)
        {
            Assert.InRange(result.EndMotionTimeMs, 0, 500);
            Assert.InRange(result.Projectile.Length, 2, 4);
            Assert.Null(result.ContactMotionTimeMs);
        }
        await cache.PendingWork!;
    }

    [Fact]
    public void CombatForecast_cache_reuses_continuation_without_flicker()
    {
        var cache = new CombatForecastCache { UseWorker = false };
        var scene = Scene(0) with { Objects = Scene(0).Objects.RemoveAt(2) };
        var session = Guid.NewGuid();
        cache.Prepare(session, scene, CombatForecastOptions.Default);
        var first = cache.Results;
        var moved = scene with
        {
            SimulationTimeMs = 100,
            SnapshotSequence = 2,
            Objects = scene.Objects.Select(o => o.Torpedo is null ? CurrentManeuverForecast.Sample(o, 100) :
                new LinearMotionPredictor().Predict(o, 100)).ToImmutableArray()
        };
        cache.Prepare(session, moved, CombatForecastOptions.Default);
        Assert.Same(first, cache.Results);
        var geometry = CombatTrajectoryProjector.FromForecast(first["torpedo"], 100);
        Assert.NotNull(geometry.EtaSeconds);
        Assert.Equal((first["torpedo"].EndMotionTimeMs - 100) / 1000, geometry.EtaSeconds);
    }

    [Fact]
    public void CombatForecast_cache_records_headless_miss_and_hit_cost()
    {
        var cache = new CombatForecastCache { UseWorker = false };
        var times = new List<double>();
        long allocated = 0;
        for (int i = 0; i < 12; i++)
        {
            var source = Scene(i % 2 == 0 ? 20 : -20);
            var objects = ImmutableArray.CreateBuilder<ObjectMotionSnapshot>();
            objects.Add(source.Objects[0]);
            for (int flight = 0; flight < 8; flight++)
                objects.Add(source.Objects[1] with { ObjectId = "torpedo-" + flight });
            for (int flight = 0; flight < 4; flight++)
                objects.Add(source.Objects[2] with
                {
                    ObjectId = "defense-" + flight,
                    Countermeasure = source.Objects[2].Countermeasure! with { TargetTorpedoId = "torpedo-" + flight }
                });
            var scene = source with { Objects = objects.ToImmutable() };
            var session = Guid.NewGuid();
            cache.Prepare(session, scene, CombatForecastOptions.Default);
            times.Add(cache.PreparationMs);
            allocated += cache.AllocatedBytes;
            cache.Prepare(session, scene, CombatForecastOptions.Default);
        }
        times.Sort();
        output.WriteLine($"1 ship + 8 torpedoes + 4 defenses; 12 batches: p50={times[6]:F2} p95={times[11]:F2} p99={times[11]:F2} ms; allocated={allocated}; cache hit={cache.Hits} miss={cache.Misses}; " +
            $"maxStepsUsed={cache.Results.Values.Max(r => r.Legs.Length)} maxPointsUsed={cache.Results.Values.Max(r => r.Projectile.Length)} limited={cache.Results.Values.Count(r => r.Status == CombatForecastStatus.CalculationLimited)}; workers<=4; headless only");
        Assert.Equal(12, cache.Hits);
        Assert.Equal(12, cache.Misses);
    }

    private static AuthoritativeSnapshot Load(double turn, long epoch = 0)
    {
        var source = Scene(turn);
        var objects = ImmutableArray.CreateBuilder<ObjectMotionSnapshot>();
        objects.Add(source.Objects[0]);
        for (int i = 0; i < 8; i++) objects.Add(source.Objects[1] with
        {
            ObjectId = "torpedo-" + i,
            DisplayName = "Torpedo " + i,
            Torpedo = source.Objects[1].Torpedo! with
            {
                LauncherModuleId = "launcher-" + i,
                Route = source.Objects[1].Torpedo!.Route with
                {
                    StartMotionTimeMs = epoch,
                    Segments = source.Objects[1].Torpedo!.Route.Segments.Select(s => s with { }).ToImmutableArray()
                },
                LaunchMotionTimeMs = epoch,
                ExpiresAtMotionTimeMs = epoch + 120_000
            }
        });
        for (int i = 0; i < 4; i++) objects.Add(source.Objects[2] with
        {
            ObjectId = "defense-" + i,
            Countermeasure = source.Objects[2].Countermeasure! with
            {
                TargetTorpedoId = "torpedo-" + i,
                LauncherModuleId = "defense-module-" + i,
                Route = source.Objects[2].Countermeasure!.Route with
                {
                    StartMotionTimeMs = epoch,
                    Segments = source.Objects[2].Countermeasure!.Route.Segments.Select(s => s with { }).ToImmutableArray()
                },
                ExpiresAtMotionTimeMs = epoch + 60_000
            }
        });
        return source with { Objects = objects.ToImmutable(), SimulationTimeMs = epoch };
    }

    [Fact]
    [Trait("Category", "ForecastBudget")]
    public void CombatForecast_headless_Release_budget_200_batches()
    {
        // Two preplanned sources: route construction is outside the measured work.
        var sources = new[] { Load(20), Load(-20) };
        long baseline = GC.GetTotalMemory(true);
        var cache = new CombatForecastCache { UseWorker = false };
        var session = Guid.NewGuid();
        for (int i = 0; i < 20; i++) cache.Prepare(session, sources[i % 2], CombatForecastOptions.Default);
        var times = new List<double>();
        long maximumAllocation = 0;
        for (int i = 0; i < 200; i++)
        {
            cache.Prepare(session, sources[i % 2], CombatForecastOptions.Default);
            times.Add(cache.PreparationMs);
            maximumAllocation = Math.Max(maximumAllocation, cache.AllocatedBytes);
            Assert.All(cache.Results.Values, r => Assert.NotEqual(CombatForecastStatus.CalculationLimited, r.Status));
            Assert.True(cache.Results["torpedo-0"].Legs == cache.Results["torpedo-1"].Legs);
            Assert.True(cache.Results["defense-0"].Legs == cache.Results["defense-1"].Legs);
            Assert.Equal("torpedo-1", cache.Results["torpedo-1"].ProjectileId);
            Assert.Equal("torpedo-1", cache.Results["defense-1"].TargetId);
            cache.Prepare(session, sources[i % 2], CombatForecastOptions.Default);
        }
        long retained = Math.Max(0, GC.GetTotalMemory(true) - baseline);
        times.Sort();
        output.WriteLine($"Release budget: n=200 warmup=20 nearest-rank; p50={times[99]:F3} p95={times[189]:F3} p99={times[197]:F3} ms; maxAllocation={maximumAllocation} retainedDelta={retained}; hit={cache.Hits} miss={cache.Misses}; CPU={Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER")} logical={Environment.ProcessorCount}; OS={Environment.OSVersion}; runtime={Environment.Version}; workers<=4; headless only");
        Assert.True(times[189] <= 100, $"p95={times[189]} ms");
        Assert.True(times[197] <= 200, $"p99={times[197]} ms");
        Assert.InRange(maximumAllocation, 0, 32 * 1024 * 1024);
        Assert.InRange(retained, 0, 64 * 1024 * 1024);
    }

    [Fact]
    [Trait("Category", "ForecastBudget")]
    public async Task CombatForecast_headless_30_seconds_of_presented_updates_do_not_starve_worker()
    {
        var cache = new CombatForecastCache();
        var session = Guid.NewGuid();
        var uiTimes = new List<double>();
        var latencies = new List<double>();
        var timer = System.Diagnostics.Stopwatch.StartNew();
        AuthoritativeSnapshot? basis = null, presented = null;
        IReadOnlyDictionary<string, CombatForecastResult>? oracle = null;
        int previousPeriod = -1;
        long previousTick = -1;
        long changedAt = 0;
        bool waiting = false;
        var geometry = new Dictionary<string, CombatTrajectoryProjector.Geometry>();
        while (timer.Elapsed.TotalSeconds < 30)
        {
            int period = (int)(timer.ElapsedMilliseconds / 2000);
            long tick = timer.ElapsedMilliseconds / 100 * 100;
            if (period != previousPeriod)
            {
                basis = Load(period % 2 == 0 ? 20 : -20, period * 2000);
                var producer = new CombatForecastCache { UseWorker = false };
                producer.Prepare(session, basis, CombatForecastOptions.Default);
                oracle = producer.Results;
                previousPeriod = period;
                waiting = true;
                changedAt = System.Diagnostics.Stopwatch.GetTimestamp();
            }
            if (tick != previousTick)
            {
                double elapsed = tick - basis!.MotionTimeMs;
                presented = basis with
                {
                    SimulationTimeMs = tick,
                    SnapshotSequence = (ulong)tick + 1,
                    Objects = basis.Objects.Select(o =>
                    {
                        if (!oracle!.TryGetValue(o.ObjectId, out var forecast)) return CurrentManeuverForecast.Sample(o, elapsed);
                        var leg = forecast.Legs.First(l => l.To > tick || l == forecast.Legs[^1]);
                        var pose = forecast.ProjectilePose(tick);
                        var route = leg.Route with { ElapsedMs = tick - leg.Route.StartMotionTimeMs };
                        return o with
                        {
                            X = pose.X,
                            Y = pose.Y,
                            Direction = pose.Direction,
                            Torpedo = o.Torpedo is { } torpedo ? torpedo with { Route = route } : null,
                            Countermeasure = o.Countermeasure is { } defense ? defense with { Route = route } : null
                        };
                    }).ToImmutableArray()
                };
                previousTick = tick;
            }
            long start = System.Diagnostics.Stopwatch.GetTimestamp();
            cache.Prepare(session, presented, CombatForecastOptions.Default);
            foreach (var (id, forecast) in cache.Results)
            {
                geometry.TryGetValue(id, out var prior);
                geometry[id] = CombatTrajectoryProjector.FromForecast(forecast, tick, prior);
                Assert.True(geometry[id].Prediction.Count > 0);
            }
            uiTimes.Add(System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            bool complete = cache.Results.Count == 12 && cache.Results.Values.All(r => r.Status is not
                (CombatForecastStatus.Recalculating or CombatForecastStatus.CalculationLimited));
            if (waiting && complete)
            {
                latencies.Add(System.Diagnostics.Stopwatch.GetElapsedTime(changedAt).TotalMilliseconds);
                Assert.Equal(basis!.MotionTimeMs, cache.Source!.MotionTimeMs);
                waiting = false;
            }
            if (waiting) Assert.True(System.Diagnostics.Stopwatch.GetElapsedTime(changedAt).TotalMilliseconds <= 250, "forecast update exceeded 250 ms");
            await Task.Delay(16);
        }
        cache.CancelPending();
        if (cache.PendingWork is { } pending) { try { await pending; } catch (OperationCanceledException) { } }
        uiTimes.Sort();
        double p99 = uiTimes[(int)Math.Ceiling(uiTimes.Count * .99) - 1];
        output.WriteLine($"30 second updates: UI prepare+geometry n={uiTimes.Count} p99={p99:F3} ms; changes={latencies.Count} maximum apply latency={latencies.DefaultIfEmpty(0).Max():F3} ms; hit={cache.Hits} miss={cache.Misses}; headless only");
        Assert.True(p99 <= 2, $"UI p99={p99} ms");
        Assert.True(latencies.Count >= 14);
        Assert.All(latencies, ms => Assert.True(ms <= 250));
    }
}

[CollectionDefinition("Combat forecast performance", DisableParallelization = true)]
public sealed class CombatForecastPerformanceCollection { }
