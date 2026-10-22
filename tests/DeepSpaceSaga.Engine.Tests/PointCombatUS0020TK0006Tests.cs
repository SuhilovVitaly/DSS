using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0008TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public class PointCombatUS0020TK0006Tests
{
    [Theory]
    [InlineData(SimulationSpeed.Speed1, 1000)]
    [InlineData(SimulationSpeed.SpeedDouble, 2000)]
    [InlineData(SimulationSpeed.Speed2, 5000)]
    [InlineData(SimulationSpeed.SpeedTen, 10000)]
    public void Combat_speed_exact_advancement(SimulationSpeed speed, long motion)
    {
        long real = 0; var clock = new SimulationClock(SimulationSpeed.Speed0, () => real);
        using var engine = Battle(clock, SimulationSpeed.Speed0, remainder: 0);
        Assert.Equal(CombatEncounterState.Active, engine.CaptureSnapshot().Encounter!.State);
        Assert.Null(engine.TrySetSpeed(speed));
        real = 4000; var scene = engine.CaptureSnapshot(advanceClock: true);
        Assert.Equal(motion, scene.MotionTimeMs); Assert.Equal(motion * 300, scene.GameTimeMs);
        Assert.Equal(speed, scene.CurrentSpeed);
    }

    [Fact]
    public void Combat_speed_switch_and_pause()
    {
        long real = 0; var clock = new SimulationClock(SimulationSpeed.Speed0, () => real);
        using var engine = Battle(clock, SimulationSpeed.Speed0, remainder: 0);
        engine.TrySetSpeed(SimulationSpeed.SpeedDouble); real = 2000;
        engine.TrySetSpeed(SimulationSpeed.SpeedTen); real = 4000;
        engine.TrySetSpeed(SimulationSpeed.Speed0); real = 8000;
        Assert.Equal(6000, engine.CaptureSnapshot(advanceClock: true).MotionTimeMs);
    }

    [Theory]
    [InlineData(SimulationSpeed.Speed1)]
    [InlineData(SimulationSpeed.SpeedDouble)]
    [InlineData(SimulationSpeed.Speed2)]
    [InlineData(SimulationSpeed.SpeedTen)]
    public void Combat_speed_validation_and_save_load(SimulationSpeed speed)
    {
        long real = 0; var clock = new SimulationClock(SimulationSpeed.Speed0, () => real);
        using var engine = Battle(clock, SimulationSpeed.Speed0, remainder: 0);
        Assert.Null(engine.TrySetSpeed(speed));
        Assert.Equal(CommandReasonCodes.CombatSpeedLocked, engine.TrySetSpeed(SimulationSpeed.Speed3));
        Assert.Equal(CommandReasonCodes.CombatSpeedLocked, engine.TrySetSpeed(SimulationSpeed.Speed4));
        real = 400; engine.CaptureSnapshot(advanceClock: true);
        var save = engine.CaptureSaveState();
        using var restored = new SimulationEngine(PointCombatUS0003TK0002Tests.Registry(scatter: 0));
        restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true), true);
        Assert.Equal(speed, restored.CurrentSpeed);
        Assert.Equal(save.GameState.MotionTimeMs, restored.CaptureSnapshot().MotionTimeMs);
    }

    [Fact]
    public void Combat_speed_scheduler_parity()
    {
        long realA = 0, realB = 0;
        using var a = Battle(new(SimulationSpeed.Speed0, () => realA), SimulationSpeed.Speed0, remainder: 0);
        using var b = Battle(new(SimulationSpeed.Speed0, () => realB), SimulationSpeed.Speed0, remainder: 0);
        foreach (var engine in new[] { a, b })
        {
            engine.ReceiveCommand(PointCombatUS0003TK0002Tests.Launch(x: -300, profile: "fast"));
            engine.CaptureSnapshot();
        }
        a.TrySetSpeed(SimulationSpeed.Speed2); b.TrySetSpeed(SimulationSpeed.SpeedTen);
        realA = 4000; realB = 2000;
        var x = a.CaptureSnapshot(advanceClock: true); var y = b.CaptureSnapshot(advanceClock: true);
        Assert.Equal(x.MotionTimeMs, y.MotionTimeMs);
        Assert.Equal(JsonSerializer.Serialize(x.Objects), JsonSerializer.Serialize(y.Objects));
        Assert.Equal(JsonSerializer.Serialize(x.CombatJournal), JsonSerializer.Serialize(y.CombatJournal));
        Assert.Equal(a.CaptureLaunchScatterState(), b.CaptureLaunchScatterState());
        Assert.Equal(a.CaptureNpcDodgeRngState(), b.CaptureNpcDodgeRngState());
    }
}
