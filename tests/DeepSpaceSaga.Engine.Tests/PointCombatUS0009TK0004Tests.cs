using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Content;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0004TK0001Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0009TK0004Tests
{
    private static IEnumerable<T> All<T>(TypeRegistry<T> source) where T : ITypeDefinition => Enumerable.Range(0, source.Count).Select(source.GetDefinition);
    private static SimulationEngine Battle(bool engineEnabled = false) => CreateCounter(o => o with
    {
        PositionX = o.ObjectId == Player ? 0 : 600,
        PositionY = 0,
        SpeedMps = 0,
        MovementType = "Stationary",
        Modules = o.Modules!.Select(m => m with
        { PowerState = m.ModuleTypeId == "module.engine.basic" && !engineEnabled ? "Off" : "On" }).ToArray()
    });
    private static ScenarioFile Save(SimulationEngine engine, long time) => engine.CaptureSaveStateForTests(time * 4, SimulationSpeed.Speed0, time);

    [Fact]
    public async Task Defeated_point_world_cannot_overwrite_an_existing_slot()
    {
        var engine = PointCombatUS0008TK0001Tests.Battle(ai: false, enemyLauncherOn: true);
        Assert.Equal(CommandStartDisposition.Started, engine.TryStartHostilePointLaunch(Target, "MOD-PIRATE-TORPEDO-01", 0, 0, "fast", 0).Disposition);
        Assert.Equal(SessionOutcomeState.Defeated, At(engine, 2500).EffectiveOutcome.State);
        string directory = Directory.CreateTempSubdirectory("dss-point-defeat-").FullName;
        string path = Path.Combine(directory, "slot.json"); byte[] original = [1, 2, 3, 4]; File.WriteAllBytes(path, original);
        try
        {
            await using (var connection = new LocalGameSessionConnection(engine, directory))
            {
                var error = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.SaveAsync("slot").AsTask());
                Assert.Equal(CommandReasonCodes.SessionDefeated, error.Message);
                Assert.Equal(original, File.ReadAllBytes(path)); Assert.Single(Directory.GetFiles(directory));
            }
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void Load_stops_held_turn_and_replayed_receipt_does_not_restart_it()
    {
        using var engine = Battle(true);
        var hold = new PlayerCommand("hold", 1, Player, "MOD-PLAYER-ENGINE-01", ShipEngineCommandTypes.TurnHold,
            TurnDirection: TurnDirection.Right, Active: true);
        engine.ReceiveCommand(hold); Assert.NotNull(At(engine).Objects.Single(o => o.ObjectId == Player).TurnHoldRateDegPerSec);
        var save = Save(engine, 457);
        double direction = save.GameState.SpaceObjects.Single(o => o.ObjectId == Player).DirectionDegrees;
        using var restored = new SimulationEngine(CounterRegistry()); restored.LoadScenario(save, true);
        restored.ReceiveCommand(hold);
        var ship = At(restored, 1457).Objects.Single(o => o.ObjectId == Player);
        Assert.Null(ship.TurnHoldRateDegPerSec); Assert.Equal(direction, ship.Direction);
    }
}
