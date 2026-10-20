using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0019TK0005Tests
{
    private static readonly string Source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
    private const string Player = "SPC-0001", EngineModule = "MOD-PLAYER-ENGINE-01";
    private static AuthoritativeSnapshot At(SimulationEngine engine, long time = 0) => engine.CaptureSnapshotForTests(time * 4, simulationTimeMs: time);
    private static ObjectMotionSnapshot Ship(AuthoritativeSnapshot state) => state.Objects.Single(o => o.ObjectId == Player);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Maneuver_command_catalog_resolves_new_contract_ids_and_executes_actual_controls(bool deployed)
    {
        string root = deployed ? AppContext.BaseDirectory : Source;
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
        string[] added = [ShipEngineCommandTypes.TurnHold, ManeuverCommandTypes.Evade, CombatEncounterCommandTypes.AcknowledgeResults];
        string[] original = ["engine.accelerate", "engine.brake", "engine.maintainSpeed", "engine.turnLeftStep", "engine.turnRightStep",
            "engine.turnLeftUntilCancel", "engine.turnRightUntilCancel", "engine.maintainCourse", "engine.speedSynchronization",
            "engine.directionSynchronization", "engine.orbit", "navigation.approach", "trade.refuel"];
        var category = registry.ModuleCategories.GetDefinition(registry.ModuleCategories.GetIndex("module.engine"));
        Assert.Equal(original.Concat(added), category.CommandTypeIds);
        foreach (string identity in added)
        {
            var command = registry.CommandDefinitions.GetDefinition(registry.CommandDefinitions.GetIndex(identity));
            Assert.Equal("module.engine", command.Type); Assert.Equal("none", command.Target);
            Assert.Equal(0, command.ActivationEnergyCellsCost);
            Assert.Equal((CommandDefinition.Neutral, CommandDefinition.Neutral, CommandDefinition.Neutral),
                (command.TimeFactor, command.ComplexityFactor, command.ConsumptionFactor));
        }
        foreach (string identity in new[] { "module.engine.basic", "module.engine.tetrarch", "module.engine.strizh" })
        {
            var module = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex(identity));
            Assert.Equal(category.CommandTypeIds, module.CommandTypeIds);
        }
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(root, "Scenarios/PlayerShipOnly/scenario.json"));
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                CurrentSpeed = "Speed0",
                MasterSeed = 42,
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o with
                {
                    PositionX = o.ObjectId == Player ? 0 : 600,
                    PositionY = 0,
                    DirectionDegrees = 0,
                    SpeedMps = o.ObjectId == Player ? 1000 : 0,
                    MovementType = "Linear",
                    Ai = null,
                    Modules = o.Modules!.Select(m => m with { AutoDefenseEnabled = false, PowerState = "On" }).ToArray()
                }).ToArray()
            }
        };
        using var engine = new SimulationEngine(registry); engine.LoadScenario(scenario);
        var hold = new PlayerCommand("catalog-hold", 1, Player, EngineModule, ShipEngineCommandTypes.TurnHold,
            TurnDirection: TurnDirection.Right, Active: true);
        engine.ReceiveCommand(hold);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(At(engine).CommandResults).Status);
        Assert.Equal(4, Ship(At(engine, 1000)).Direction, 8);
        engine.ReceiveCommand(hold with { CommandId = "catalog-release", Active = false });
        Assert.Null(Ship(At(engine, 1000)).TurnHoldRateDegPerSec);
        engine.ReceiveCommand(new("catalog-step-left", 2, Player, EngineModule, ShipEngineCommandTypes.TurnLeftStep));
        Assert.Equal(ShipEngineCommandTypes.TurnLeftStep, Ship(At(engine, 1000)).ActiveEngineCommandType);
        var leftDone = At(engine, 1250);
        Assert.Equal(3, Ship(leftDone).Direction, 8);
        Assert.Contains(leftDone.CommandResults, r => r.CommandId == "catalog-step-left" && r.Status == CommandResultStatus.Executed);
        engine.ReceiveCommand(new("catalog-step-right-blocked", 3, Player, EngineModule, ShipEngineCommandTypes.TurnRightStep));
        Assert.Equal(CommandReasonCodes.TurnInertiaBlocked, Assert.Single(At(engine, 1250).CommandResults).ReasonCode);
        At(engine, 1500); // Existing angular-inertia cooldown begins at completion of the prior step.
        engine.ReceiveCommand(new("catalog-step-right", 4, Player, EngineModule, ShipEngineCommandTypes.TurnRightStep));
        Assert.Equal(ShipEngineCommandTypes.TurnRightStep, Ship(At(engine, 1500)).ActiveEngineCommandType);
        var rightDone = At(engine, 1750);
        Assert.Equal(4, Ship(rightDone).Direction, 8);
        Assert.Contains(rightDone.CommandResults, r => r.CommandId == "catalog-step-right" && r.Status == CommandResultStatus.Executed);
        Assert.Equal(CommandStartDisposition.Started,
            engine.TryStartHostilePointLaunch("SPC-0002", "MOD-PIRATE-TORPEDO-01", 0, 0, "slow", 1750).Disposition);
        string threat = Assert.Single(At(engine, 1750).Objects, o => o.ObservedProjectile is not null).ObjectId;
        engine.ReceiveCommand(new("catalog-evade", 5, Player, EngineModule, ManeuverCommandTypes.Evade,
            ThreatProjectileId: threat, EvadeSide: EvadeSide.Left));
        var evading = At(engine, 1750);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(evading.CommandResults).Status);
        Assert.NotNull(Ship(evading).Evasion); Assert.Equal(6, Ship(evading).SpeedKmS);
        engine.ReceiveCommand(new("catalog-ack", 6, Player, "", CombatEncounterCommandTypes.AcknowledgeResults));
        var ack = At(engine, 1750);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(ack.CommandResults).Status);
        Assert.Equal(CombatEncounterState.Active, ack.Encounter!.State);
        Assert.Equal(evading.Encounter!.Stats, ack.Encounter.Stats);
    }
}
