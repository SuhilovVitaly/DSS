using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
using Point = DeepSpaceSaga.Engine.Tests.PointCounterFixture;

namespace DeepSpaceSaga.Engine.Tests;

public class AutomaticDefenseTests
{
    [Theory]
    [InlineData(0d)]
    [InlineData(90d)]
    [InlineData(180d)]
    [InlineData(270d)]
    public void Explicit_point_ray_ignores_carrier_bearing_and_never_replans(double bearing)
    {
        using var engine = Point.Create(configure: o => o.ObjectId == Point.Player ? o with { DirectionDegrees = bearing } : o);
        engine.ReceiveCommand(Point.Shot(x: 150, y: 0)); var state = Point.At(engine); var flight = Point.Flight(state);
        Assert.Equal(bearing, state.Objects.Single(o => o.ObjectId == Point.Player).Direction);
        Assert.Equal(90, state.Objects.Single(o => o.OwnProjectile is not null).Direction);
        var middle = Point.Flight(Point.At(engine, 500)); Assert.Equal(75, middle.X, 8); Assert.Equal(0, middle.Y);
        Assert.Equal(flight.DetonationMotionTimeMs, middle.DetonationMotionTimeMs);
        Assert.Equal((150d, 0d), (middle.DetonationX, middle.DetonationY));
        Assert.DoesNotContain(Point.At(engine, 1001).Objects, o => o.OwnProjectile is not null);
    }

    [Fact]
    public void Active_counter_keeps_captured_profile_when_future_catalog_values_change()
    {
        using var original = Point.Create(); original.ReceiveCommand(Point.Shot(profile: "fast", x: 250, y: 0)); Point.At(original);
        var save = Point.Save(original, 400); var source = Point.Registry();
        static IEnumerable<T> All<T>(TypeRegistry<T> types) where T : ITypeDefinition => Enumerable.Range(0, types.Count).Select(types.GetDefinition);
        var changed = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Select(m => m.TypeId == "module.countermeasure.launcher.basic"
            ? m with { Profiles = [new("wide", 15, 1), new("fast", 7, 2)] } : m), All(source.ItemTypes), All(source.CommandDefinitions),
            All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests), stationMarketProfiles: All(source.StationMarketProfiles),
            shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents), combatRules: source.CombatRules);
        using var loaded = new SimulationEngine(changed); loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), true);
        var flight = Point.Flight(Point.At(loaded, 999)); Assert.Equal(25, flight.SpeedKmS); Assert.Equal(.5, flight.BlastRadiusKm);
        Assert.Equal(1000, flight.DetonationMotionTimeMs); Assert.Equal(249.75, flight.X, 8);
        Assert.DoesNotContain(Point.At(loaded, 1001).Objects, o => o.OwnProjectile is not null);
        Point.At(loaded, 12500); loaded.ReceiveCommand(Point.Shot("next", profile: "fast", x: 250, y: 0));
        var next = Point.Flight(Point.At(loaded, 12500)); Assert.Equal(7, next.SpeedKmS); Assert.Equal(2, next.BlastRadiusKm);
        Assert.Equal(12500 + 250 / 70d * 1000, next.DetonationMotionTimeMs, 8);
    }

    [Fact]
    public void Real_incoming_torpedo_crosses_counter_range_without_automatic_launch_or_attempt()
    {
        using var engine = Point.Create(enemyArmed: true, enemyX: 600);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Point.Enemy, Point.EnemyTorpedo, 0, 0, "slow", 0).Disposition);
        var before = engine.CaptureLaunchScatterState();
        foreach (long time in new long[] { 0, 4900, 5000, 5100, 6000 })
        {
            var state = Point.At(engine, time); Assert.Contains(state.Objects, o => o.ObservedProjectile is not null);
            Assert.DoesNotContain(state.Objects, o => o.OwnProjectile?.Kind == ProjectileKind.Countermissile);
            Assert.DoesNotContain(state.CombatJournal, e => e.Type == CombatEventType.Launch && e.ProjectileKind == ProjectileKind.Countermissile);
            Assert.Equal(before, engine.CaptureLaunchScatterState()); Assert.Equal(0UL, Point.Save(engine, time).GameState.DefenseState!.RngCounter);
        }
    }

    [Fact]
    public void Historical_auto_flags_and_enable_command_cannot_activate_point_defense()
    {
        using var engine = Point.Create(configure: o => o with { Modules = o.Modules!.Select(m => m with { AutoDefenseEnabled = true }).ToArray() });
        engine.ReceiveCommand(new("enable", 1, Point.Player, Point.Counter, "defense.enable")); var state = Point.At(engine);
        Assert.Equal(CommandReasonCodes.UnknownCommandType, Assert.Single(state.CommandResults).ReasonCode);
        Assert.All(Point.Save(engine).GameState.DefenseState!.Launchers, l => Assert.False(l.State.AutoEnabled));
        Assert.DoesNotContain(Point.At(engine, 1000).Objects, o => o.OwnProjectile is not null);
        Assert.Equal(0UL, engine.CaptureLaunchScatterState().Counter);
    }

    [Fact]
    public void Retired_target_fire_cannot_replace_or_mutate_an_existing_area_counter()
    {
        using var engine = Point.Create(); engine.ReceiveCommand(Point.Shot()); var state = Point.At(engine);
        var original = Point.Flight(state); string id = state.Objects.Single(o => o.OwnProjectile is not null).ObjectId;
        var before = engine.CaptureLaunchScatterState();
        engine.ReceiveCommand(new("retired-target", 2, Point.Player, Point.Torpedo, "torpedo.fire", id));
        var rejected = Point.At(engine); Assert.Equal(CommandReasonCodes.UnknownCommandType, Assert.Single(rejected.CommandResults).ReasonCode);
        Assert.Equal(original, Point.Flight(rejected)); Assert.Equal(before, engine.CaptureLaunchScatterState());
    }
}
