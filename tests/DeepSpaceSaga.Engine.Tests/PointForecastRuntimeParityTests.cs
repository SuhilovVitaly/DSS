using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0004TK0001Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointForecastRuntimeParityTests
{
    private static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition => Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);
    private static SimulationEngine Battle(double targetX, double core, bool station = false)
    {
        var source = Registry(scatter: 0);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Select(m => m with { StructurePointsMax = 1000 }),
            All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents),
            combatRules: source.CombatRules! with { Blast = new(core) });
        var scenario = TorpedoImpactTests.Scenario(o => o with
        {
            Ai = null,
            IsKnown = true,
            PositionX = o.ObjectId == Player ? 0 : targetX,
            PositionY = 0,
            Modules = o.Modules!.Select(m => m with { StructurePoints = 1000, ArmorClass = "none", Armor = 0 }).ToArray()
        });
        if (station) scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Append(new SpaceObjectData("station", SpaceObjectType.Station, "Permanent", "Station",
                600, 0, 0, 0, "Stationary", null, null, null, IsKnown: true)).ToArray()
            }
        };
        var engine = new SimulationEngine(registry); engine.LoadScenario(scenario); return engine;
    }

    [Theory]
    [InlineData(.3, 689.99, false)]
    [InlineData(.3, 689.8, true)]
    [InlineData(.8, 689.9, true)]
    [InlineData(.3, 689.9, false)]
    public void Rounded_blast_forecast_matches_actual_runtime_and_captured_core(double core, double targetX, bool hits)
    {
        using var engine = Battle(targetX, core);
        engine.ReceiveCommand(Launch(x: 600, y: 0, profile: "slow"));
        var start = At(engine); var shot = Flight(start);
        Assert.Equal(core, shot.OwnProjectile!.CoreFraction);
        var random = engine.CaptureLaunchScatterState();
        var forecast = CombatForecast.PointProjectile(shot, start.Objects.Single(o => o.ObjectId == Target), 0);
        Assert.Equal(hits ? CombatForecastStatus.Contact : CombatForecastStatus.Expired, forecast.Status);
        Assert.Equal(random, engine.CaptureLaunchScatterState());
        var actual = At(engine, 10000);
        Assert.Equal(hits, actual.CombatJournal.Any(e => e.Type == CombatEventType.Hit && e.TargetObjectId == Target));
        int loss = 1000 * engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == Target).Modules.Length -
            engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == Target).Modules.Sum(m => m.StructurePoints);
        Assert.Equal(hits ? 1 : 0, loss);
    }

    [Fact]
    public void Shared_line_samples_match_engine_collision_path_and_continued_snapshots()
    {
        using var engine = Battle(900, .3);
        engine.ReceiveCommand(Launch(x: 600, y: 0, profile: "slow"));
        var first = Flight(At(engine));
        var forecast = CombatForecast.PointProjectile(first, null, 0);
        var path = SimulationEngine.CombatPath(engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == first.ObjectId));
        var expected = path.Position(3750.25); var predicted = forecast.ProjectilePose(3750.25);
        Assert.Equal(225.015, expected.X, 9);
        Assert.Equal(expected.X, predicted.X, 9); Assert.Equal(expected.Y, predicted.Y, 9);
        var current = Flight(At(engine, 3750));
        var continued = CombatForecast.PointProjectile(current, null, 3750);
        Assert.Equal(10000, continued.EndMotionTimeMs);
        Assert.Equal(600, continued.Projectile[^1].X);
        Assert.Equal(225, continued.Projectile[0].X, 9);
    }

    [Fact]
    public void Counter_ship_forecast_is_not_damage_and_runtime_keeps_modules_intact()
    {
        using var engine = CreateCounter(o => o with { IsKnown = true, PositionX = o.ObjectId == Player ? 0 : 150, PositionY = 0 }, scatter: 0);
        engine.ReceiveCommand(CounterLaunch(x: 150)); var start = At(engine);
        var shot = start.Objects.Single(o => o.OwnProjectile?.Kind == ProjectileKind.Countermissile);
        var result = CombatForecast.PointProjectile(shot, start.Objects.Single(o => o.ObjectId == Target), 0);
        Assert.Equal(CombatForecastStatus.Expired, result.Status);
        var health = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == Target).Modules.Select(m => m.StructurePoints).ToArray();
        var actual = At(engine, 1000);
        Assert.DoesNotContain(actual.CombatJournal, e => e.Type is CombatEventType.Hit or CombatEventType.ModuleHit);
        Assert.Equal(health, engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == Target).Modules.Select(m => m.StructurePoints));
    }

    [Fact]
    public void Torpedo_does_not_forecast_or_destroy_a_nearby_projectile()
    {
        using var engine = CreateCounter(scatter: 0, torpedoScatter: 0);
        engine.ReceiveCommand(Launch(x: 160, y: 0, profile: "fast"));
        engine.ReceiveCommand(CounterLaunch(x: 300)); var start = At(engine);
        var torpedo = start.Objects.Single(o => o.OwnProjectile?.Kind == ProjectileKind.Torpedo);
        var counter = start.Objects.Single(o => o.OwnProjectile?.Kind == ProjectileKind.Countermissile);
        Assert.Equal(CombatForecastStatus.Expired, CombatForecast.PointProjectile(torpedo, counter, 0).Status);
        Assert.Contains(At(engine, 1000).Objects, o => o.ObjectId == counter.ObjectId);
    }

    [Fact]
    public void Same_time_counter_forecast_uses_runtime_ordinal_priority()
    {
        using var engine = CreateCounter(scatter: 0);
        engine.ReceiveCommand(CounterLaunch("first", x: 150));
        engine.ReceiveCommand(CounterLaunch("second", x: 150, module: Defense2)); var start = At(engine);
        var shots = start.Objects.Where(o => o.OwnProjectile?.Kind == ProjectileKind.Countermissile).OrderBy(o => o.ObjectId, StringComparer.Ordinal).ToArray();
        Assert.Equal(CombatForecastStatus.Contact, CombatForecast.PointProjectile(shots[0], shots[1], 0).Status);
        Assert.Equal(CombatForecastStatus.TargetTerminated, CombatForecast.PointProjectile(shots[1], shots[0], 0).Status);
        var actual = At(engine, 1000);
        Assert.Equal(shots[0].ObjectId, Assert.Single(actual.CombatJournal, e => e.Type == CombatEventType.CountermissileDetonated).ProjectileObjectId);
        Assert.Equal(shots[1].ObjectId, Assert.Single(actual.CombatJournal, e => e.Type == CombatEventType.ShotDown).ProjectileObjectId);
    }

    [Fact]
    public void Torpedo_station_forecast_matches_runtime_no_damage()
    {
        using var engine = Battle(900, .3, station: true);
        engine.ReceiveCommand(Launch(x: 600, y: 0, profile: "slow")); var start = At(engine);
        Assert.Equal(CombatForecastStatus.Expired, CombatForecast.PointProjectile(Flight(start), start.Objects.Single(o => o.ObjectId == "station"), 0).Status);
        var actual = At(engine, 10000);
        Assert.DoesNotContain(actual.CombatJournal, e => e.Type == CombatEventType.Hit);
        Assert.Contains(actual.Objects, o => o.ObjectId == "station" && !o.IsDestroyed);
    }
}
