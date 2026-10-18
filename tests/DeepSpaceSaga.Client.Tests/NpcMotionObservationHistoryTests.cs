namespace DeepSpaceSaga.Client.Tests;

public sealed class NpcMotionObservationHistoryTests
{
    private readonly NpcMotionObservationHistory _history = new();
    private void Observe(double time, double heading, Guid? session = null, double x = 0) =>
        _history.ObserveScene(session, time, [new("npc", time, x, 0, 1, heading)]);

    [Theory]
    [InlineData(10, 20, 30, 10)]
    [InlineData(30, 20, 10, -10)]
    public void TwoEqualTurnsConfirmRate(double a, double b, double c, double rate)
    {
        Observe(0, a); Observe(1000, b); Observe(2000, c);
        Assert.Equal(rate, _history.TurnRateDegPerSec("npc"), 9);
    }

    [Fact]
    public void TwoStraightIntervalsStayStraight()
    {
        Observe(0, 20); Observe(1000, 20); Observe(2000, 20);
        Assert.Equal(0, _history.TurnRateDegPerSec("npc"));
    }

    [Fact]
    public void UnconfirmedOrChangedTrendFallsBackImmediately()
    {
        Observe(0, 0); Observe(1000, 10);
        Assert.Equal(0, _history.TurnRateDegPerSec("npc"));
        Observe(2000, 20);
        Assert.Equal(10, _history.TurnRateDegPerSec("npc"));
        Observe(3000, 35);
        Assert.Equal(0, _history.TurnRateDegPerSec("npc"));
    }

    [Theory]
    [InlineData(359, 0, 1, 1)]
    [InlineData(1, 0, 359, -1)]
    public void WrapAroundKeepsTurnSign(double a, double b, double c, double expected)
    {
        Observe(0, a); Observe(1000, b); Observe(2000, c);
        Assert.Equal(expected, _history.TurnRateDegPerSec("npc"), 9);
    }

    [Theory]
    [InlineData(10, 20.001, 10.0005)]
    [InlineData(10, 20.0011, 0)]
    [InlineData(0.0005, 0.001, 0)]
    [InlineData(180, 0, 0)]
    public void ToleranceAndHalfTurnBoundaries(double b, double c, double expected)
    {
        Observe(0, 0); Observe(1000, b); Observe(2000, c);
        Assert.Equal(expected, _history.TurnRateDegPerSec("npc"), 8);
    }

    [Fact]
    public void OnlyPhysicalSecondsCount()
    {
        for (int i = 0; i < 20; i++) Observe(i * 100, i);
        Assert.Equal(0, _history.TurnRateDegPerSec("npc"));
        Observe(2000, 20);
        Assert.Equal(10, _history.TurnRateDegPerSec("npc"), 9);
    }

    [Fact]
    public void ConfirmedInterpolationNeverExtrapolates()
    {
        Observe(100, 1); Observe(700, 7); Observe(1400, 14); Observe(2000, 20);
        Assert.Equal(0, _history.TurnRateDegPerSec("npc"));
        Observe(2300, 23);
        Assert.Equal(10, _history.TurnRateDegPerSec("npc"), 9);
    }

    [Fact]
    public void DuplicatePauseGapAndRollbackResetSafely()
    {
        Observe(0, 0); Observe(1000, 10); Observe(2000, 20);
        Observe(2000, 20);
        Assert.Equal(10, _history.TurnRateDegPerSec("npc"));
        Observe(2000, 20, x: 1);
        Assert.Equal(0, _history.TurnRateDegPerSec("npc"));
        Observe(3000, 30); Observe(4000, 40);
        Assert.Equal(10, _history.TurnRateDegPerSec("npc"));
        Observe(5000.01, 50);
        Assert.Equal(0, _history.TurnRateDegPerSec("npc"));
        Observe(100, 1);
        Assert.Equal(1, _history.SampleCount);
    }

    [Fact]
    public void SessionAndObjectRemovalClearHistory()
    {
        Observe(0, 0); Observe(1000, 10); Observe(2000, 20);
        Observe(3000, 30, Guid.NewGuid());
        Assert.Equal(0, _history.TurnRateDegPerSec("npc"));
        _history.ObserveScene(null, 3000, []);
        Assert.Equal(0, _history.ObjectCount);
        Observe(4000, 40);
        Assert.Equal(1, _history.SampleCount);
        _history.Clear();
        Assert.Equal(0, _history.SampleCount);
    }

    [Fact]
    public void HistoryStorageIsBounded()
    {
        for (int i = 0; i < 10000; i++) Observe(i * 1000, i % 360);
        Assert.Equal(1, _history.ObjectCount);
        Assert.Equal(3, _history.SampleCount);
        _history.ObserveScene(null, 10000000, [new("other", 10000000, 0, 0, 1, 0)]);
        Assert.Equal(1, _history.ObjectCount);
        Assert.Equal(1, _history.SampleCount);
    }
}
