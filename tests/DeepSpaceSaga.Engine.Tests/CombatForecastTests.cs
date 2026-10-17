using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using Xunit.Abstractions;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatForecastTests(ITestOutputHelper output)
{
    private static ObjectMotionSnapshot Ship(double turn = 0, double acceleration = 0) => new("target", 0, -300, 1, 90,
        MaxSpeedKmS: 2, HostileCombat: new("owner", "engine", "launcher", 50, true, HostileCombatPhase.Pursuit,
            new(0, 100, 0, 0, -300, 1, 90, acceleration, turn)));

    private static ObjectMotionSnapshot Projectile(ObjectMotionSnapshot target, long now = 0)
    {
        var owner = new ObjectMotionSnapshot("torpedo", 0, 0, 3, 0);
        var route = TorpedoGuidanceMath.Plan(owner, target, 3, 90, now);
        return owner with { Torpedo = new("owner", "launcher", target.ObjectId, now, 3, 90, 0, 0, route, ExpiresAtMotionTimeMs: now + 120_000) };
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(15, 0)]
    [InlineData(-15, 0)]
    [InlineData(15, .2)]
    [InlineData(-15, -.2)]
    public void CombatForecast_continues_current_controls_and_retains_contact_centres(double turn, double acceleration)
    {
        var target = Ship(turn, acceleration);
        var projectile = Projectile(target);
        string before = JsonSerializer.Serialize(new[] { target, projectile });
        var result = CombatForecast.Torpedo(projectile, target, 0);
        Assert.Equal(CombatForecastStatus.Contact, result.Status);
        Assert.True(result.EndMotionTimeMs > 100);
        Assert.Equal(result.Projectile[^1].MotionTimeMs, result.Target[^1].MotionTimeMs);
        double distance = Math.Sqrt(Math.Pow(result.Projectile[^1].X - result.Target[^1].X, 2) + Math.Pow(result.Projectile[^1].Y - result.Target[^1].Y, 2));
        Assert.InRange(distance, 4.99999, 5.00001);
        Assert.Equal(before, JsonSerializer.Serialize(new[] { target, projectile }));
        Assert.Equal(target.X, HostileOrbitMath.Predict(target, 10_000).X - (HostileOrbitMath.Predict(target, 100).X - target.X), 8);
        if (turn != 0) Assert.True(result.Legs.Select(l => l.Route.StartMotionTimeMs).Distinct().Count() > 2);
    }

    [Theory]
    [InlineData(.5, 2)]
    [InlineData(-.5, 0)]
    public void CombatForecast_acceleration_clamps_without_reversing(double acceleration, double expected)
    {
        var sample = CurrentManeuverForecast.Sample(Ship(30, acceleration), 10_000);
        Assert.Equal(expected, sample.SpeedKmS);
        Assert.InRange(sample.Direction, 0, 360);
    }

    [Fact]
    public void CombatForecast_fractional_start_keeps_absolute_grid_and_fractional_contact()
    {
        var target = Ship(20);
        var projectile = Projectile(target);
        var moved = TorpedoGuidanceMath.Predict(projectile, 37);
        var result = CombatForecast.Torpedo(moved, CurrentManeuverForecast.Sample(target, 37), 37);
        Assert.Equal(100, result.Legs[0].To);
        Assert.Equal(CombatForecastStatus.Contact, result.Status);
        Assert.NotEqual(Math.Round(result.EndMotionTimeMs / 100) * 100, result.EndMotionTimeMs);
    }

    [Fact]
    public void CombatForecast_replans_new_linear_course_at_next_absolute_boundary()
    {
        var target = new ObjectMotionSnapshot("target", 0, -200, 1, 90);
        var projectile = new LinearMotionPredictor().Predict(Projectile(target), 37);
        var changed = new LinearMotionPredictor().Predict(target, 37) with { Direction = 180 };
        var result = CombatForecast.Torpedo(projectile, changed, 37);
        Assert.Equal(100, result.Legs[0].To);
        Assert.Equal(100, result.Legs[1].Route.StartMotionTimeMs);
        Assert.NotEqual(projectile.Torpedo!.Route.Segments, result.Legs[1].Route.Segments);
    }

    [Fact]
    public void CombatForecast_has_explicit_work_horizon_and_data_limits()
    {
        var target = Ship();
        var projectile = Projectile(target);
        Assert.Equal(CombatForecastStatus.CalculationLimited, CombatForecast.Torpedo(projectile, target, 0, new(MaxSteps: 1)).Status);
        var shortResult = CombatForecast.Torpedo(projectile, target, 0, new(HorizonMs: 150));
        Assert.Equal(CombatForecastStatus.NoContact, shortResult.Status);
        Assert.Equal(150, shortResult.EndMotionTimeMs);
        Assert.NotEmpty(shortResult.Target);
        Assert.Equal(CombatForecastStatus.InsufficientData,
            CombatForecast.Torpedo(projectile, target with
            {
                MaxSpeedKmS = null,
                HostileCombat = target.HostileCombat! with
                { Segment = target.HostileCombat.Segment with { AccelerationKmS2 = 1 } }
            }, 0).Status);
        Assert.Equal(CombatForecastStatus.TargetLost, CombatForecast.Torpedo(projectile, null, 0).Status);
        Assert.Equal(CombatForecastStatus.TargetLost, CombatForecast.Torpedo(projectile, target with { ObjectId = "wrong-target" }, 0).Status);
        var destroyed = CombatForecast.Torpedo(projectile with { IsDestroyed = true }, target, 0);
        Assert.Equal(CombatForecastStatus.Expired, destroyed.Status);
        Assert.Equal(0, destroyed.EndMotionTimeMs);
    }

    [Fact]
    public void CombatForecast_no_contact_from_spatial_crossing_at_different_times()
    {
        var target = new ObjectMotionSnapshot("target", 100, -100, 10, 90);
        var projectile = Projectile(target);
        var result = CombatForecast.Torpedo(projectile, target, 0, new(HorizonMs: 1000));
        Assert.Equal(CombatForecastStatus.NoContact, result.Status);
        Assert.NotEmpty(result.Target);
    }

    [Theory]
    [InlineData(CountermeasurePhase.Guiding)]
    [InlineData(CountermeasurePhase.MissedCoast)]
    public void CombatForecast_defense_uses_dependent_torpedo_and_captured_lifetime(CountermeasurePhase phase)
    {
        var ship = Ship(10);
        var torpedo = Projectile(ship);
        var targetForecast = CombatForecast.Torpedo(torpedo, ship, 0);
        var owner = ship with { ObjectId = "defense", HostileCombat = null, Direction = 180 };
        var plan = CountermeasureGuidanceMath.PlanManualLaunch(owner, torpedo.Torpedo!.Route, 0, 60_000, 12, 90);
        var flight = new CountermeasureSnapshot("target", "defense-module", "torpedo", phase, plan.Route, 0, 500,
            new(null!, 0), ExpiresAtMotionTimeMs: 60_000, MissExpiresAtMotionTimeMs: 2000, SpeedKmS: 12, TurnRateDegPerSec: 90);
        var result = CombatForecast.Countermeasure(owner with { Countermeasure = flight }, torpedo, targetForecast, 0);
        if (phase == CountermeasurePhase.Guiding)
        {
            Assert.Equal(CombatForecastStatus.Contact, result.Status);
            Assert.True(result.EndMotionTimeMs < targetForecast.EndMotionTimeMs);
            var expected = targetForecast.ProjectilePose(result.EndMotionTimeMs);
            Assert.Equal(expected.X, result.Target[^1].X, 8);
            Assert.Equal(expected.Y, result.Target[^1].Y, 8);
            var wrongVersion = CombatForecast.Countermeasure(owner with { Countermeasure = flight }, torpedo,
                targetForecast with { StartMotionTimeMs = 1 }, 0);
            Assert.Equal(CombatForecastStatus.InsufficientData, wrongVersion.Status);
        }
        else
        {
            Assert.Equal(CombatForecastStatus.MissedCoast, result.Status);
            Assert.True(result.EndMotionTimeMs <= 2000);
            Assert.Empty(result.Target);
        }
    }

    [Fact]
    public void CombatForecast_publishes_launch_lifetime_without_adding_it_to_save_state()
    {
        using var engine = TorpedoImpactTests.Create();
        var snapshot = TorpedoImpactTests.At(engine, 0);
        long lifetime = snapshot.InstalledModules.Single(m => m.ModuleId == TorpedoImpactTests.Launcher).TorpedoMaxFlightTimeMs!.Value;
        engine.ReceiveCommand(TorpedoImpactTests.Fire("lifetime"));
        var flight = Assert.Single(TorpedoImpactTests.At(engine, 0).Objects.Where(o => o.Torpedo is not null)).Torpedo!;
        Assert.Equal(lifetime, flight.ExpiresAtMotionTimeMs - flight.LaunchMotionTimeMs);
        Assert.DoesNotContain("torpedoMaxFlightTimeMs", JsonSerializer.Serialize(engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0)));
    }

    [Fact]
    public void CombatForecast_matches_independent_Engine_flight_and_does_not_change_rng()
    {
        var source = HostileTorpedoFireTests.HostileScenario(300);
        var scenario = source with
        {
            GameState = source.GameState with
            {
                SpaceObjects = source.GameState.SpaceObjects.Select(o => o.ObjectId == TorpedoImpactTests.Player ? o with
                {
                    SpeedMps = 1000,
                    MovementType = "Linear",
                    DirectionDegrees = 90,
                    Modules = o.Modules!.Select(m => m.ModuleTypeId == "module.engine.basic" ? m with { PowerState = "On" } : m).ToArray()
                } : o).ToArray()
            }
        };
        using var enabled = TorpedoImpactTests.Create(scenario);
        using var disabled = TorpedoImpactTests.Create(scenario);
        string launcher = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == TorpedoImpactTests.Target)
            .Modules!.Single(m => m.ModuleTypeId == "module.torpedo.launcher.basic").ModuleId;
        foreach (var engine in new[] { enabled, disabled })
        {
            engine.ReceiveCommand(new("turn", 1, TorpedoImpactTests.Player, "MOD-PLAYER-ENGINE-01", ShipEngineCommandTypes.TurnRightUntilCancel));
            TorpedoImpactTests.At(engine, 0);
            engine.TryStartHostileTorpedoFire(TorpedoImpactTests.Target, launcher, 0);
        }
        var initial = TorpedoImpactTests.At(enabled, 37);
        TorpedoImpactTests.At(disabled, 37);
        var projectile = Assert.Single(initial.Objects.Where(o => o.Torpedo is not null));
        var target = initial.Objects.Single(o => o.ObjectId == TorpedoImpactTests.Player);
        var result = CombatForecast.Torpedo(projectile, target, 37);
        Assert.Equal(CombatForecastStatus.Contact, result.Status);
        for (long time = 100; time < result.EndMotionTimeMs; time += 100)
        {
            var actual = TorpedoImpactTests.At(enabled, time);
            TorpedoImpactTests.At(disabled, time);
            var actualFlight = actual.Objects.Single(o => o.ObjectId == projectile.ObjectId);
            var predicted = result.ProjectilePose(time);
            Assert.Equal(actualFlight.X, predicted.X, 5);
            Assert.Equal(actualFlight.Y, predicted.Y, 5);
        }
        long end = (long)Math.Ceiling(result.EndMotionTimeMs) + 1;
        var final = TorpedoImpactTests.At(enabled, end);
        TorpedoImpactTests.At(disabled, end);
        var impact = Assert.Single(final.CombatImpacts.Where(i => i.TorpedoObjectId == projectile.ObjectId));
        Assert.Equal(result.EndMotionTimeMs, impact.MotionTimeMs, 4);
        Assert.Equal(JsonSerializer.Serialize(enabled.CaptureSaveStateForTests(end, SimulationSpeed.Speed0)),
            JsonSerializer.Serialize(disabled.CaptureSaveStateForTests(end, SimulationSpeed.Speed0)));
    }

    [Fact]
    public void CombatForecast_defense_chain_matches_independent_Engine_contact()
    {
        var source = HostileTorpedoFireTests.HostileScenario(300);
        var scenario = source with
        {
            GameState = source.GameState with
            {
                SpaceObjects = source.GameState.SpaceObjects.Select(o => o.ObjectId == TorpedoImpactTests.Player ? o with
                {
                    SpeedMps = 1000,
                    MovementType = "Linear",
                    DirectionDegrees = 90,
                    Modules = o.Modules!.Select(m => m.ModuleTypeId == "module.engine.basic" ? m with { PowerState = "On" } : m).ToArray()
                } : o).ToArray()
            }
        };
        using var engine = TorpedoImpactTests.Create(scenario);
        engine.ReceiveCommand(new("turn", 1, TorpedoImpactTests.Player, "MOD-PLAYER-ENGINE-01", ShipEngineCommandTypes.TurnRightUntilCancel));
        TorpedoImpactTests.At(engine, 0);
        engine.TryStartHostileTorpedoFire(TorpedoImpactTests.Target, "MOD-PIRATE-TORPEDO-01", 0);
        var before = TorpedoImpactTests.At(engine, 37);
        var torpedo = Assert.Single(before.Objects.Where(o => o.Torpedo is not null));
        engine.ReceiveCommand(new("defense", 2, TorpedoImpactTests.Player, "MOD-PLAYER-COUNTERMEASURE-01", DefenseCommandTypes.Fire, torpedo.ObjectId));
        var initial = TorpedoImpactTests.At(engine, 37);
        var defense = Assert.Single(initial.Objects.Where(o => o.Countermeasure is not null));
        var ship = initial.Objects.Single(o => o.ObjectId == TorpedoImpactTests.Player);
        var targetForecast = CombatForecast.Torpedo(torpedo, ship, 37);
        var result = CombatForecast.Countermeasure(defense, torpedo, targetForecast, 37);
        Assert.Equal(CombatForecastStatus.Contact, result.Status);
        for (long time = 100; time < result.EndMotionTimeMs; time += 100)
        {
            var actual = TorpedoImpactTests.At(engine, time).Objects.Single(o => o.ObjectId == defense.ObjectId);
            var predicted = result.ProjectilePose(time);
            Assert.Equal(actual.X, predicted.X, 5);
            Assert.Equal(actual.Y, predicted.Y, 5);
        }
        var final = TorpedoImpactTests.At(engine, (long)Math.Ceiling(result.EndMotionTimeMs) + 1);
        var contact = Assert.Single(final.CombatJournal.Where(e => e.ProjectileObjectId == defense.ObjectId &&
            e.Type is CombatEventType.Intercept or CombatEventType.Miss));
        Assert.Equal(result.EndMotionTimeMs, contact.MotionTimeMs, 4);
    }

    [Fact]
    public void CombatForecast_headless_preparation_cost()
    {
        var durations = new List<double>();
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int iteration = 0; iteration < 12; iteration++)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 8; i++)
            {
                var target = Ship(i % 2 == 0 ? 10 : -10);
                var result = CombatForecast.Torpedo(Projectile(target), target, 0);
                Assert.True(result.Projectile.Length <= 8001);
                Assert.True(result.Legs.Length <= 2000);
            }
            durations.Add(watch.Elapsed.TotalMilliseconds);
        }
        durations.Sort();
        output.WriteLine($"8 torpedoes, turning 1 km/s targets, 3 km/s 90 deg/s projectiles; 12 batches; p50={durations[6]:F2} p95={durations[11]:F2} p99={durations[11]:F2} ms; bytes={GC.GetAllocatedBytesForCurrentThread() - allocated}; maxSteps=2000 maxPoints=8001");
    }
}
