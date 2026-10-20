using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0004TK0001Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0018TK0003Tests
{
    private const string NpcLauncher = "MOD-PIRATE-TORPEDO-01";

    [Theory]
    [InlineData("slow", 10000)]
    [InlineData("medium", 6000)]
    [InlineData("fast", 3750)]
    public void Own_forecast_uses_captured_deadline_and_module_has_no_fixed_legacy_lifetime(string profile, long deadline)
    {
        using var engine = Create(o => o with { IsKnown = true, PositionX = o.ObjectId == Player ? 0 : 600, PositionY = 0 }, scatter: 0);
        engine.ReceiveCommand(Launch(x: 600, y: 0, profile: profile));
        var start = At(engine); var shot = Flight(start);
        Assert.Null(start.InstalledModules.Single(m => m.ModuleId == Launcher).TorpedoMaxFlightTimeMs);
        Assert.Null(shot.Torpedo);
        var rng = engine.CaptureLaunchScatterState();
        var forecast = CombatForecast.PointProjectile(shot, start.Objects.Single(o => o.ObjectId == Target), 0);
        Assert.Equal(CombatForecastStatus.Contact, forecast.Status);
        Assert.Equal(deadline, forecast.ContactMotionTimeMs);
        Assert.Empty(forecast.Legs);
        var intermediate = Flight(At(engine, 500));
        var expected = forecast.ProjectilePose(500);
        Assert.Equal(expected.X, intermediate.X, 9); Assert.Equal(expected.Y, intermediate.Y, 9);
        Assert.Equal(rng, engine.CaptureLaunchScatterState());
        var actual = At(engine, deadline);
        Assert.Equal(deadline, Assert.Single(actual.CombatJournal, e => e.Type == CombatEventType.Hit && e.TargetObjectId == Target).MotionTimeMs);
    }

    [Fact]
    public void Foreign_forecast_is_identical_for_different_hidden_detonation_points()
    {
        static SimulationEngine World(double endpoint)
        {
            var engine = CreateCounter(o => o with
            {
                IsKnown = true,
                PositionX = o.ObjectId == Player ? 0 : 100,
                PositionY = 0,
                Modules = o.Modules!.Select(m => m.ModuleId == NpcLauncher ? m with { PowerState = "On" } : m).ToArray()
            }, scatter: 0, torpedoScatter: 0);
            Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, NpcLauncher, endpoint, 0, "slow", 0).Disposition);
            return engine;
        }
        using var first = World(500); using var second = World(700);
        CombatForecastResult Inspect(SimulationEngine engine)
        {
            var start = At(engine, 500);
            var shot = Assert.Single(start.Objects, o => o.ObservedProjectile is not null);
            Assert.Null(shot.OwnProjectile); Assert.Null(shot.Torpedo);
            engine.SetObjectInteractionState(shot.ObjectId, shot.ObjectId);
            var selected = At(engine, 500);
            Assert.All(selected.InstalledModules.Where(m => m.Defense is not null), m => Assert.Null(m.DefensePreview));
            var rng = engine.CaptureLaunchScatterState();
            var forecast = CombatForecast.PointProjectile(shot, start.Objects.Single(o => o.ObjectId == Player), 500, new(HorizonMs: 3000));
            Assert.Equal(CombatForecastStatus.InsufficientData, forecast.Status);
            Assert.Null(forecast.ContactMotionTimeMs);
            Assert.Empty(forecast.TargetId); Assert.Empty(forecast.Target);
            Assert.Empty(forecast.Legs);
            Assert.Equal(3500, forecast.EndMotionTimeMs);
            Assert.Equal(310, forecast.Projectile[^1].X, 9);
            Assert.Equal(rng, engine.CaptureLaunchScatterState());
            return forecast;
        }
        Assert.Equal(JsonSerializer.Serialize(Inspect(first)), JsonSerializer.Serialize(Inspect(second)));
    }
}
