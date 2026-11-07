using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.LocalClient;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0009TK0004Tests
{
    private static SimulationEngine Battle(bool engineEnabled = false) => Create(TorpedoImpactTests.Scenario(o => o with
    {
        PositionX = o.ObjectId == Player ? 0 : 600,
        PositionY = 0,
        SpeedMps = 0,
        MovementType = "Stationary",
        Modules = o.Modules!.Select(m => m with
        { PowerState = m.ModuleTypeId == "module.engine.basic" && !engineEnabled ? "Off" : "On" }).ToArray()
    }));
    private static ScenarioFile Save(SimulationEngine engine, long time) => engine.CaptureSaveStateForTests(time * 4, SimulationSpeed.Speed0, time);

    [Fact]
    public void Load_stops_held_turn_and_replayed_receipt_does_not_restart_it()
    {
        using var engine = Battle(true);
        var hold = new PlayerCommand("hold", 1, Player, "MOD-PLAYER-ENGINE-01", ShipEngineCommandTypes.TurnHold,
            TurnDirection: TurnDirection.Right, Active: true);
        engine.ReceiveCommand(hold); Assert.NotNull(At(engine, 0).Objects.Single(o => o.ObjectId == Player).TurnHoldRateDegPerSec);
        var save = Save(engine, 457);
        double direction = save.GameState.SpaceObjects.Single(o => o.ObjectId == Player).DirectionDegrees;
        using var restored = new SimulationEngine(Registry.Value); restored.LoadScenario(save, true);
        restored.ReceiveCommand(hold);
        var ship = At(restored, 1457).Objects.Single(o => o.ObjectId == Player);
        Assert.Null(ship.TurnHoldRateDegPerSec); Assert.Equal(direction, ship.Direction);
    }
}
