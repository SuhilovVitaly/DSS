using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
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
