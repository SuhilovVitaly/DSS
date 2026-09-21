using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;

namespace DeepSpaceSaga.Engine.Tests;

public class SimulationClockTests
{
    [Theory]
    [InlineData(SimulationSpeed.Speed0, 0)]
    [InlineData(SimulationSpeed.Speed1, 300_000)]
    [InlineData(SimulationSpeed.Speed2, 1_500_000)]
    [InlineData(SimulationSpeed.Speed3, 6_000_000)]
    [InlineData(SimulationSpeed.Speed4, 30_000_000)]
    public void One_real_second_uses_the_configured_base_pace(SimulationSpeed speed, long expected)
    {
        long realMs = 0;
        var clock = new SimulationClock(speed, () => realMs);
        realMs = 1000;
        Assert.Equal(new SimulationClockState(expected, speed, 1000 * (int)speed), clock.UpdateAndCapture());
    }

    [Fact]
    public void Capture_returns_both_timestamps_without_consuming_elapsed_time()
    {
        long realMs = 10;
        var clock = new SimulationClock(SimulationSpeed.Speed2, () => realMs);
        Assert.Equal(new SimulationClockState(0, SimulationSpeed.Speed2, 0), clock.Capture());
        realMs = 30;
        Assert.Equal(new SimulationClockState(0, SimulationSpeed.Speed2, 0), clock.Capture());
        Assert.Equal(new SimulationClockState(30_000, SimulationSpeed.Speed2, 100), clock.UpdateAndCapture());
        Assert.Equal(clock.Capture(), clock.UpdateAndCapture());
    }

    [Fact]
    public void Multiple_updates_accumulate_both_domains_exactly()
    {
        long realMs = 0;
        var clock = new SimulationClock(SimulationSpeed.Speed1, () => realMs);
        realMs = 17;
        clock.Update();
        Assert.Equal(new SimulationClockState(5100, SimulationSpeed.Speed1, 17), clock.Capture());
        realMs = 43;
        Assert.Equal(new SimulationClockState(12_900, SimulationSpeed.Speed1, 43), clock.UpdateAndCapture());
    }

    [Fact]
    public void SetSpeed_accounts_for_partial_interval_at_old_speed_before_switching()
    {
        long realMs = 0;
        var clock = new SimulationClock(SimulationSpeed.Speed1, () => realMs);
        realMs = 20;
        clock.UpdateAndCapture();
        realMs = 37;
        clock.SetSpeed(SimulationSpeed.Speed2);
        Assert.Equal(new SimulationClockState(11_100, SimulationSpeed.Speed2, 37), clock.Capture());
        realMs = 60;
        Assert.Equal(new SimulationClockState(45_600, SimulationSpeed.Speed2, 152), clock.UpdateAndCapture());
    }

    [Fact]
    public void Speed0_freezes_both_domains_and_resume_excludes_paused_interval()
    {
        long realMs = 0;
        var clock = new SimulationClock(SimulationSpeed.Speed1, () => realMs);
        realMs = 100;
        clock.SetSpeed(SimulationSpeed.Speed0);
        var paused = new SimulationClockState(30_000, SimulationSpeed.Speed0, 100);
        Assert.Equal(paused, clock.Capture());
        realMs = 1100;
        clock.Update();
        Assert.Equal(paused, clock.UpdateAndCapture());
        realMs = 2100;
        clock.SetSpeed(SimulationSpeed.Speed2);
        realMs = 2120;
        Assert.Equal(new SimulationClockState(60_000, SimulationSpeed.Speed2, 200), clock.UpdateAndCapture());
    }

    [Theory]
    [InlineData(null, 900_000)]
    [InlineData(1234L, 1234)]
    public void Reset_preserves_explicit_or_legacy_motion_baseline_and_discards_backlog(long? motion, long expected)
    {
        long realMs = 0;
        var clock = new SimulationClock(SimulationSpeed.Speed4, () => realMs);
        realMs = 100;
        clock.Update();
        realMs = 1000;
        clock.Reset(900_000, SimulationSpeed.Speed1, motion);
        Assert.Equal(new SimulationClockState(900_000, SimulationSpeed.Speed1, expected), clock.Capture());
        realMs = 1007;
        Assert.Equal(new SimulationClockState(902_100, SimulationSpeed.Speed1, expected + 7), clock.UpdateAndCapture());
    }

    [Fact]
    public void ResetRealBaseline_discards_backlog_without_changing_either_domain()
    {
        long realMs = 0;
        var clock = new SimulationClock(SimulationSpeed.Speed1, () => realMs);
        realMs = 10;
        var before = clock.UpdateAndCapture();
        realMs = 1000;
        clock.ResetRealBaseline();
        Assert.Equal(before, clock.Capture());
        realMs = 1005;
        Assert.Equal(new SimulationClockState(4500, SimulationSpeed.Speed1, 15), clock.UpdateAndCapture());
    }

    [Fact]
    public void Legacy_clock_state_uses_calendar_time_when_motion_is_absent()
    {
        Assert.Equal(1234, new SimulationClockState(1234, SimulationSpeed.Speed0).MotionTimeMs);
        Assert.Equal(17, new SimulationClockState(1234, SimulationSpeed.Speed0, 17).MotionTimeMs);
    }
}
