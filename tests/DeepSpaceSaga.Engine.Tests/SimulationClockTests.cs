using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;

namespace DeepSpaceSaga.Engine.Tests;

public class SimulationClockTests
{
    [Theory]
    [InlineData(SimulationSpeed.Speed0, 0)]
    [InlineData(SimulationSpeed.Speed1, 75_000)]
    [InlineData(SimulationSpeed.Speed2, 375_000)]
    [InlineData(SimulationSpeed.Speed3, 1_500_000)]
    [InlineData(SimulationSpeed.Speed4, 7_500_000)]
    public void One_real_second_uses_the_configured_base_pace(SimulationSpeed speed, long expected)
    {
        long realMs = 0;
        var clock = new SimulationClock(speed, () => realMs);
        realMs = 1000;
        Assert.Equal(new SimulationClockState(expected, speed, 250 * (int)speed), clock.UpdateAndCapture());
    }

    [Fact]
    public void Capture_returns_both_timestamps_without_consuming_elapsed_time()
    {
        long realMs = 10;
        var clock = new SimulationClock(SimulationSpeed.Speed2, () => realMs);
        Assert.Equal(new SimulationClockState(0, SimulationSpeed.Speed2, 0), clock.Capture());
        realMs = 30;
        Assert.Equal(new SimulationClockState(0, SimulationSpeed.Speed2, 0), clock.Capture());
        Assert.Equal(new SimulationClockState(7500, SimulationSpeed.Speed2, 25), clock.UpdateAndCapture());
        Assert.Equal(clock.Capture(), clock.UpdateAndCapture());
    }

    [Fact]
    public void Multiple_updates_accumulate_both_domains_exactly()
    {
        long realMs = 0;
        var clock = new SimulationClock(SimulationSpeed.Speed1, () => realMs);
        realMs = 17;
        clock.Update();
        Assert.Equal(new SimulationClockState(1275, SimulationSpeed.Speed1, 4, 1), clock.Capture());
        realMs = 43;
        Assert.Equal(new SimulationClockState(3225, SimulationSpeed.Speed1, 10, 3), clock.UpdateAndCapture());
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
        Assert.Equal(new SimulationClockState(2775, SimulationSpeed.Speed2, 9, 1), clock.Capture());
        realMs = 60;
        Assert.Equal(new SimulationClockState(11400, SimulationSpeed.Speed2, 38), clock.UpdateAndCapture());
    }

    [Fact]
    public void Speed0_freezes_both_domains_and_resume_excludes_paused_interval()
    {
        long realMs = 0;
        var clock = new SimulationClock(SimulationSpeed.Speed1, () => realMs);
        realMs = 100;
        clock.SetSpeed(SimulationSpeed.Speed0);
        var paused = new SimulationClockState(7500, SimulationSpeed.Speed0, 25);
        Assert.Equal(paused, clock.Capture());
        realMs = 1100;
        clock.Update();
        Assert.Equal(paused, clock.UpdateAndCapture());
        realMs = 2100;
        clock.SetSpeed(SimulationSpeed.Speed2);
        realMs = 2120;
        Assert.Equal(new SimulationClockState(15000, SimulationSpeed.Speed2, 50), clock.UpdateAndCapture());
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
        Assert.Equal(new SimulationClockState(900525, SimulationSpeed.Speed1, expected + 1, 3), clock.UpdateAndCapture());
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
        Assert.Equal(new SimulationClockState(1125, SimulationSpeed.Speed1, 3, 3), clock.UpdateAndCapture());
    }

    [Fact]
    public void Millisecond_updates_preserve_submillisecond_motion_across_speed_changes_and_pause()
    {
        long now = 0;
        var clock = new SimulationClock(SimulationSpeed.Speed1, () => now);
        now = 1;
        clock.SetSpeed(SimulationSpeed.Speed2); // 1 quarter of a motion millisecond.
        now = 2;
        clock.SetSpeed(SimulationSpeed.Speed0); // 6 quarters accumulated.
        Assert.Equal(new SimulationClockState(450, SimulationSpeed.Speed0, 1, 2), clock.Capture());
        now = 1000;
        clock.SetSpeed(SimulationSpeed.Speed1);
        now = 1002;
        Assert.Equal(new SimulationClockState(600, SimulationSpeed.Speed1, 2), clock.UpdateAndCapture());
    }

    [Fact]
    public void Base_pace_is_partition_invariant_at_single_real_millisecond_updates()
    {
        long now = 0;
        var split = new SimulationClock(SimulationSpeed.Speed1, () => now);
        var whole = new SimulationClock(SimulationSpeed.Speed1, () => now);
        for (now = 1; now <= 1000; now++) split.Update();
        now = 1000;
        Assert.Equal(whole.UpdateAndCapture(), split.Capture());
        Assert.Equal(250, split.SimulationTimeMs);
    }

    [Fact]
    public void Legacy_clock_state_uses_calendar_time_when_motion_is_absent()
    {
        Assert.Equal(1234, new SimulationClockState(1234, SimulationSpeed.Speed0).MotionTimeMs);
        Assert.Equal(17, new SimulationClockState(1234, SimulationSpeed.Speed0, 17).MotionTimeMs);
    }
}
