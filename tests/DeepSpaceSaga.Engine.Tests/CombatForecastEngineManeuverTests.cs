using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatForecastEngineManeuverTests
{
    [Theory]
    [InlineData(ShipEngineCommandTypes.Accelerate)]
    [InlineData(ShipEngineCommandTypes.Brake)]
    [InlineData(ShipEngineCommandTypes.TurnLeftStep)]
    [InlineData(ShipEngineCommandTypes.TurnRightStep)]
    [InlineData(ShipEngineCommandTypes.SpeedSynchronization)]
    [InlineData(ShipEngineCommandTypes.DirectionSynchronization)]
    public void Zero_duration_updates_are_authoritative_and_forecasts_do_not_guess_their_completion_grid(string command)
    {
        // Both an early endpoint and a direct advance across guidance boundaries
        // must use Engine's actual next-update semantics, not a captured 100 ms TTL.
        foreach (bool earlyUpdate in new[] { true, false })
        {
            using var engine = TorpedoImpactTests.Create(TorpedoImpactTests.Scenario(o => o with
            {
                SpeedMps = o.ObjectId == TorpedoImpactTests.Player ? 1000 : 1500,
                DirectionDegrees = o.ObjectId == TorpedoImpactTests.Player ? 0 : 90,
                MovementType = "Linear",
                Modules = o.Modules?.Select(m => m.ModuleTypeId == "module.engine.basic" ? m with { PowerState = "On" } : m).ToArray()
            }));
            engine.ReceiveCommand(new("zero-phase", 1, TorpedoImpactTests.Player, "MOD-PLAYER-ENGINE-01", command,
                command is ShipEngineCommandTypes.SpeedSynchronization or ShipEngineCommandTypes.DirectionSynchronization ? TorpedoImpactTests.Target : null));
            engine.ReceiveCommand(TorpedoImpactTests.LaunchAt("phase-flight", 700, 0, "slow"));
            var source = TorpedoImpactTests.At(engine, 37).Objects.Single(o => o.ObjectId == TorpedoImpactTests.Player);
            Assert.True(source.EngineManeuver!.CompletesOnMotionUpdate);
            Assert.Equal(0, source.EngineManeuver.CycleIntervalMs);
            Assert.Equal(0, source.EngineManeuver.CycleRemainingMs);
            Assert.Equal(source, CurrentManeuverForecast.Sample(source, 0));
            Assert.False(CurrentManeuverForecast.HasRequiredData(source));
            var conservative = CurrentManeuverForecast.Sample(source, 1000);
            Assert.Equal(source.Direction, conservative.Direction); Assert.Equal(source.SpeedKmS, conservative.SpeedKmS);
            Assert.Equal(source.ActiveEngineCommandType, conservative.ActiveEngineCommandType);
            Assert.True(conservative.EngineManeuver!.CompletesOnMotionUpdate);
            long previous = 37;
            foreach (long time in earlyUpdate ? new long[] { 50, 99, 100, 137, 200, 2000 } : new long[] { 137, 2000 })
            {
                var actual = TorpedoImpactTests.At(engine, time);
                var current = actual.Objects.Single(o => o.ObjectId == source.ObjectId);
                if (previous == 37)
                    Assert.Contains(actual.ShipEvents, e => e.ObjectId == source.ObjectId && e.ModuleId == "MOD-PLAYER-ENGINE-01" &&
                        e.EventType == ShipEventTypes.CommandCompleted && e.GameTimeMs == (earlyUpdate ? 50 : 100));
                if (source.EngineManeuver is null)
                {
                    var predicted = CurrentManeuverForecast.Sample(source, time - previous);
                    Assert.Equal(current.X, predicted.X, 6); Assert.Equal(current.Y, predicted.Y, 6);
                    Assert.Equal(current.SpeedKmS, predicted.SpeedKmS, 8); Assert.Equal(current.Direction, predicted.Direction, 8);
                }
                Assert.InRange(current.SpeedKmS, 0, current.MaxSpeedKmS!.Value);
                Assert.True(double.IsFinite(current.X) && double.IsFinite(current.Y));
                source = current; previous = time;
            }
        }
    }

    [Theory]
    [InlineData(ShipEngineCommandTypes.Accelerate)]
    [InlineData(ShipEngineCommandTypes.Brake)]
    [InlineData(ShipEngineCommandTypes.TurnLeftStep)]
    [InlineData(ShipEngineCommandTypes.TurnRightStep)]
    [InlineData(ShipEngineCommandTypes.SpeedSynchronization)]
    [InlineData(ShipEngineCommandTypes.DirectionSynchronization)]
    public void Known_nonzero_player_maneuver_matches_Engine_beyond_command_end(string command)
    {
        var scenario = TorpedoImpactTests.Scenario(o => o with
        {
            SpeedMps = o.ObjectId == TorpedoImpactTests.Player ? 1000 : 1500,
            DirectionDegrees = o.ObjectId == TorpedoImpactTests.Player ? 0 : 90,
            MovementType = "Linear",
            Modules = o.Modules?.Select(m => m.ModuleTypeId == "module.engine.basic" ? m with { PowerState = "On" } : m).ToArray()
        });
        var sourceRegistry = TorpedoImpactTests.Registry.Value;
        static IEnumerable<T> All<T>(TypeRegistry<T> r) where T : ITypeDefinition => Enumerable.Range(0, r.Count).Select(r.GetDefinition);
        var registry = GameDataRegistry.Create(All(sourceRegistry.ModuleCategories), All(sourceRegistry.ModuleTypes), All(sourceRegistry.ItemTypes),
            All(sourceRegistry.CommandDefinitions).Select(c => c.TypeId == command ? c with { TimeFactor = 1000 } : c),
            All(sourceRegistry.FactoryTypes), All(sourceRegistry.Recipes), All(sourceRegistry.Dialogues), All(sourceRegistry.Quests),
            stationMarketProfiles: All(sourceRegistry.StationMarketProfiles), shipClasses: All(sourceRegistry.ShipClasses),
            stationMarketEvents: All(sourceRegistry.StationMarketEvents), combatRules: sourceRegistry.CombatRules);
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o.ObjectId != TorpedoImpactTests.Target ? o : o with
                { Modules = o.Modules!.Select(m => m.ModuleTypeId == "module.engine.basic" ? m with { PowerState = "Off" } : m).ToArray() }).ToArray()
            }
        };
        using var engine = TorpedoImpactTests.Create(scenario, registry);
        engine.ReceiveCommand(new("maneuver", 1, TorpedoImpactTests.Player, "MOD-PLAYER-ENGINE-01", command,
            command is ShipEngineCommandTypes.SpeedSynchronization or ShipEngineCommandTypes.DirectionSynchronization ? TorpedoImpactTests.Target : null));
        engine.ReceiveCommand(TorpedoImpactTests.LaunchAt("forecast-flight", 700, 0, "slow"));
        var initial = TorpedoImpactTests.At(engine, 37).Objects.Single(o => o.ObjectId == TorpedoImpactTests.Player);
        Assert.NotNull(initial.EngineManeuver);
        Assert.True(CurrentManeuverForecast.HasRequiredData(initial), JsonSerializer.Serialize(initial));
        foreach (long time in new long[] { 100, 200, 1000, 2000, 3000 })
        {
            var expected = TorpedoImpactTests.At(engine, time).Objects.Single(o => o.ObjectId == initial.ObjectId);
            var actual = CurrentManeuverForecast.Sample(initial, time - 37);
            Assert.Equal(expected.X, actual.X, 6);
            Assert.Equal(expected.Y, actual.Y, 6);
            Assert.Equal(expected.SpeedKmS, actual.SpeedKmS, 8);
            Assert.Equal(expected.Direction, actual.Direction, 8);
        }
        string save = JsonSerializer.Serialize(engine.CaptureSaveStateForTests(3000, SimulationSpeed.Speed0));
        Assert.DoesNotContain("engineManeuver", save);
    }

    [Fact]
    public void CombatForecast_phase_can_be_sliced_and_finite_turn_does_not_repeat()
    {
        var state = new ObjectMotionSnapshot("ship", 0, 0, 1, 0, ShipEngineCommandTypes.TurnRightStep,
            MaxSpeedKmS: 2, EngineManeuver: new(ShipEngineCommandTypes.TurnRightStep, 63, 1000, false, 0, 0, 30));
        var partial = CurrentManeuverForecast.Sample(state, 37.5);
        var sliced = CurrentManeuverForecast.Sample(partial, 3000);
        var direct = CurrentManeuverForecast.Sample(state, 3037.5);
        Assert.Equal(direct.X, sliced.X, 8);
        Assert.Equal(direct.Y, sliced.Y, 8);
        Assert.Equal(30, sliced.Direction);
        Assert.Null(sliced.EngineManeuver);
        Assert.Null(sliced.ActiveEngineCommandType);
    }

    [Theory]
    [InlineData(ShipEngineCommandTypes.Accelerate)]
    [InlineData(ShipEngineCommandTypes.Brake)]
    [InlineData(ShipEngineCommandTypes.TurnRightStep)]
    [InlineData(ShipEngineCommandTypes.SpeedSynchronization)]
    [InlineData(ShipEngineCommandTypes.DirectionSynchronization)]
    public void CombatForecast_nonzero_command_phase_matches_Engine_at_arbitrary_times(string command)
    {
        var source = TorpedoImpactTests.Registry.Value;
        static IEnumerable<T> All<T>(TypeRegistry<T> r) where T : ITypeDefinition => Enumerable.Range(0, r.Count).Select(r.GetDefinition);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes), All(source.ItemTypes),
            All(source.CommandDefinitions).Select(c => c.TypeId == command ? c with { TimeFactor = 1000 } : c),
            All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses),
            stationMarketEvents: All(source.StationMarketEvents), combatRules: source.CombatRules, fireModes: source.FireModes);
        using var engine = TorpedoImpactTests.Create(TorpedoImpactTests.Scenario(o => o with
        { SpeedMps = 1000, MovementType = "Linear" }), registry);
        engine.ReceiveCommand(new("phase", 1, TorpedoImpactTests.Player, "MOD-PLAYER-ENGINE-01", command,
            command is ShipEngineCommandTypes.SpeedSynchronization or ShipEngineCommandTypes.DirectionSynchronization ? TorpedoImpactTests.Target : null));
        TorpedoImpactTests.At(engine, 0);
        var initial = TorpedoImpactTests.At(engine, 137).Objects.Single(o => o.ObjectId == TorpedoImpactTests.Player);
        Assert.True(initial.EngineManeuver!.CycleRemainingMs > 0);
        foreach (long time in new long[] { 150, 999, 1000, 1037, 2000, 20_000, 200_000 })
        {
            var expected = TorpedoImpactTests.At(engine, time).Objects.Single(o => o.ObjectId == initial.ObjectId);
            var actual = CurrentManeuverForecast.Sample(initial, time - 137);
            Assert.Equal(expected.X, actual.X, 6);
            Assert.Equal(expected.Y, actual.Y, 6);
            Assert.Equal(expected.SpeedKmS, actual.SpeedKmS, 8);
            Assert.Equal(expected.Direction, actual.Direction, 8);
        }
    }
}
