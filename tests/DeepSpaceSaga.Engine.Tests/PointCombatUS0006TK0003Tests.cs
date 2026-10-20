using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using DeepSpaceSaga.Engine.Content;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0006TK0003Tests
{
    private const string Engine = "MOD-PLAYER-ENGINE-01";
    private static AuthoritativeSnapshot At(SimulationEngine engine, long time = 0) => engine.CaptureSnapshotForTests(time * 4, simulationTimeMs: time);
    private static ObjectMotionSnapshot Ship(AuthoritativeSnapshot snapshot) => snapshot.Objects.Single(o => o.ObjectId == Player);
    private static SimulationEngine Battle() => Create(o => o.ObjectId == Player ? o with
    { SpeedMps = 2000, DirectionDegrees = 0, MovementType = "Linear" } : o);
    private static PlayerCommand Hold(string id = "hold", TurnDirection direction = TurnDirection.Right, bool? active = true) =>
        new(id, 1, Player, Engine, ShipEngineCommandTypes.TurnHold, TurnDirection: direction, Active: active);

    [Theory]
    [InlineData(TurnDirection.Right, 4)]
    [InlineData(TurnDirection.Left, -4)]
    public void Turn_hold_integrates_until_release(TurnDirection direction, double rate)
    {
        using var engine = Battle();
        engine.ReceiveCommand(Hold(direction: direction));
        var start = Ship(At(engine));
        Assert.Equal(rate, start.TurnHoldRateDegPerSec);
        var expected = new LinearMotionPredictor().Predict(start, 1500);
        var actual = Ship(At(engine, 1500));
        Assert.Equal((rate * 1.5 + 360) % 360, actual.Direction, 10);
        Assert.Equal(expected.X, actual.X, 10);
        Assert.Equal(expected.Y, actual.Y, 10);
        engine.ReceiveCommand(Hold("release", direction, false));
        var released = Ship(At(engine, 1500));
        Assert.Null(released.TurnHoldRateDegPerSec);
        var coast = new LinearMotionPredictor().Predict(released, 1000);
        actual = Ship(At(engine, 2500));
        Assert.Equal(released.Direction, actual.Direction, 10);
        Assert.Equal(coast.X, actual.X, 10);
        Assert.Equal(coast.Y, actual.Y, 10);
    }

    [Fact]
    public void Wrong_key_release_and_replay_do_not_cancel_or_restart_the_turn()
    {
        using var engine = Battle();
        engine.ReceiveCommand(Hold()); At(engine);
        At(engine, 250);
        engine.ReceiveCommand(Hold());
        engine.ReceiveCommand(Hold("left-release", TurnDirection.Left, false));
        Assert.Equal(4, Ship(At(engine, 500)).TurnHoldRateDegPerSec);
        Assert.Equal(2, Ship(At(engine, 500)).Direction, 10);
        engine.ReceiveCommand(Hold("left", TurnDirection.Left)); At(engine, 500);
        Assert.Equal(0, Ship(At(engine, 1000)).Direction, 10);
    }

    [Fact]
    public void Turn_hold_stops_on_pause_and_session_reset()
    {
        using var engine = Battle();
        engine.ReceiveCommand(Hold()); At(engine); At(engine, 500);
        engine.SetSpeed(SimulationSpeed.Speed0);
        Assert.Null(Ship(At(engine, 500)).TurnHoldRateDegPerSec);
        engine.SetSpeed(SimulationSpeed.Speed1);
        Assert.Equal(2, Ship(At(engine, 1000)).Direction, 10);
        engine.ReceiveCommand(Hold("again")); At(engine, 1000);
        engine.LoadScenario(TorpedoImpactTests.Scenario(o => o));
        Assert.Null(Ship(At(engine)).TurnHoldRateDegPerSec);
    }

    [Fact]
    public void Invalid_payload_or_engine_override_preserves_the_held_control()
    {
        using var engine = Battle();
        engine.ReceiveCommand(Hold()); At(engine);
        var expected = JsonSerializer.Serialize(Ship(At(engine, 500)));
        foreach (var command in new[] { Hold("bad", (TurnDirection)999), Hold("null", active: null),
            new PlayerCommand("bad-orbit", 1, Player, Engine, ShipEngineCommandTypes.Orbit) })
        {
            engine.ReceiveCommand(command);
            var result = At(engine, 500);
            Assert.Equal(CommandResultStatus.Rejected, Assert.Single(result.CommandResults).Status);
            Assert.Equal(expected, JsonSerializer.Serialize(Ship(result)));
        }
        engine.ReceiveCommand(new("cancel", 1, Player, Engine, ShipEngineCommandTypes.MaintainCourse));
        Assert.Null(Ship(At(engine, 500)).TurnHoldRateDegPerSec);
        Assert.Equal(2, Ship(At(engine, 1000)).Direction, 10);
    }

    [Fact]
    public void Outside_combat_existing_step_still_turns_once()
    {
        using var engine = Battle();
        engine.ReceiveCommand(new("step", 1, Player, Engine, ShipEngineCommandTypes.TurnRightStep));
        At(engine);
        var actual = Ship(At(engine, 2000));
        Assert.Equal(1, actual.Direction, 10);
        Assert.Null(actual.TurnHoldRateDegPerSec);
    }

    [Fact]
    public void Turn_hold_stops_on_engine_damage_and_keeps_inertial_motion()
    {
        var source = Registry(scatter: 0);
        static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition => Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Select(m => m with
        {
            MassKg = m.CategoryTypeId == "module.engine" ? 1_000_000 : 1,
            TorpedoDamage = m.TorpedoDamage is null ? null : 1,
            Profiles = m.CategoryTypeId == "module.torpedo.launcher" ? [new("slow", 6, 99)] : m.Profiles
        }), All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes),
            All(source.Dialogues), All(source.Quests), stationMarketProfiles: All(source.StationMarketProfiles),
            shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents), combatRules: source.CombatRules);
        var scenario = TorpedoImpactTests.Scenario(o => o.ObjectId == Player ? o with
        { Ai = null, SpeedMps = 2000, DirectionDegrees = 0, MovementType = "Linear" } : o with
        {
            Ai = null,
            PositionX = 100,
            PositionY = 0,
            Modules = o.Modules!.Select(m => m.ModuleId == "MOD-PIRATE-TORPEDO-01" ? m with { PowerState = "On" } : m).ToArray()
        });
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(scenario);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, "MOD-PIRATE-TORPEDO-01", 0, 0, "slow", 0).Disposition);
        string threat = Assert.Single(At(engine).Objects, o => o.ObservedProjectile is not null).ObjectId;
        engine.ReceiveCommand(Hold()); At(engine);
        var snapshot = At(engine, 2500);
        var hit = Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.ModuleHit && e.TargetObjectId == Player);
        Assert.Equal(Engine, hit.HitModuleId);
        var ship = Ship(snapshot);
        Assert.Null(ship.TurnHoldRateDegPerSec);
        Assert.Equal(4 * hit.MotionTimeMs / 1000, ship.Direction, 7);
        Assert.Equal(2, ship.SpeedKmS);
        Assert.Equal(ship.Direction, Ship(At(engine, 3000)).Direction, 8);
        Assert.True(snapshot.InstalledModules.Single(m => m.ModuleId == Engine).DisabledUntilMotionTimeMs > 2500);
    }
}
