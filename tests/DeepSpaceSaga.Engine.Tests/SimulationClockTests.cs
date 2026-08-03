using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;

namespace DeepSpaceSaga.Engine.Tests;

public class SimulationClockTests
{
    [Fact]
    public void GameTime_starts_at_zero()
    {
        var clock = new SimulationClock(SimulationSpeed.Speed1);
        Assert.Equal(0, clock.GameTimeMs);
    }

    [Fact]
    public void Initial_speed_is_set()
    {
        var clock = new SimulationClock(SimulationSpeed.Speed2);
        Assert.Equal(SimulationSpeed.Speed2, clock.Speed);
    }

    [Fact]
    public void Update_advances_game_time_at_speed1()
    {
        var clock = new SimulationClock(SimulationSpeed.Speed1);

        // Wait a small amount of real time
        Thread.Sleep(50);
        clock.Update();

        Assert.True(clock.GameTimeMs > 0, "GameTime should advance at Speed1");
    }

    [Fact]
    public void Update_at_speed0_does_not_advance_time()
    {
        var clock = new SimulationClock(SimulationSpeed.Speed0);

        Thread.Sleep(50);
        clock.Update();

        Assert.Equal(0, clock.GameTimeMs);
    }

    [Fact]
    public void Update_at_speed2_advances_faster_than_speed1()
    {
        var clock1 = new SimulationClock(SimulationSpeed.Speed1);
        var clock2 = new SimulationClock(SimulationSpeed.Speed2);

        Thread.Sleep(100);
        clock1.Update();
        clock2.Update();

        // Speed2 (5x) should accumulate roughly 5x more time than Speed1
        Assert.True(clock2.GameTimeMs > clock1.GameTimeMs * 3,
            $"Speed2={clock2.GameTimeMs} should be > 3× Speed1={clock1.GameTimeMs}");
    }

    [Fact]
    public void SetSpeed_resets_real_baseline()
    {
        var clock = new SimulationClock(SimulationSpeed.Speed1);

        // Let some real time pass at Speed1
        Thread.Sleep(50);
        clock.Update();
        long timeBeforePause = clock.GameTimeMs;
        Assert.True(timeBeforePause > 0);

        // Pause
        clock.SetSpeed(SimulationSpeed.Speed0);

        // Wait real time while paused
        Thread.Sleep(50);
        clock.Update();
        Assert.Equal(timeBeforePause, clock.GameTimeMs); // No advance

        // Resume
        clock.SetSpeed(SimulationSpeed.Speed1);
        Thread.Sleep(50);
        clock.Update();
        Assert.True(clock.GameTimeMs > timeBeforePause, "Should advance after resume");
    }

    [Fact]
    public void Multiple_updates_accumulate_correctly()
    {
        var clock = new SimulationClock(SimulationSpeed.Speed1);

        clock.Update();
        long t1 = clock.GameTimeMs;

        Thread.Sleep(50);
        clock.Update();
        long t2 = clock.GameTimeMs;

        Assert.True(t2 > t1, "GameTime should increase with each Update");
    }

    [Fact]
    public void Speed_change_preserves_accumulated_time()
    {
        var clock = new SimulationClock(SimulationSpeed.Speed1);
        Thread.Sleep(20);
        clock.Update();
        long accumulated = clock.GameTimeMs;
        Assert.True(accumulated > 0);

        // Change speed — accumulated time should be preserved
        clock.SetSpeed(SimulationSpeed.Speed2);
        Assert.Equal(accumulated, clock.GameTimeMs);
        Assert.Equal(SimulationSpeed.Speed2, clock.Speed);
    }
}
