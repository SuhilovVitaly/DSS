using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests;

public class GameTimeDisplayTests
{
    [Theory]
    [InlineData(0, "День 1 · 00:00")]
    [InlineData(86_399_999, "День 1 · 23:59")]
    [InlineData(86_400_000, "День 2 · 00:00")]
    [InlineData(203_459_999, "День 3 · 08:30")]
    public void Map_time_truncates_minutes(long time, string expected)
        => Assert.Equal(expected, GameTimeDisplay.Minutes(time));

    [Theory]
    [InlineData(3_599_999, "День 1 · 00 ч")]
    [InlineData(86_399_999, "День 1 · 23 ч")]
    [InlineData(86_400_000, "День 2 · 00 ч")]
    public void Station_time_truncates_hours(long time, string expected)
        => Assert.Equal(expected, GameTimeDisplay.Hours(time));

    [Fact]
    public void Missing_snapshot_and_paused_status_are_explicit()
    {
        Assert.Equal("День — · —:—", GameTimeDisplay.Minutes(null));
        Assert.Equal("Пауза", GameTimeDisplay.Status(SimulationSpeed.Speed0));
        Assert.Equal("×100", GameTimeDisplay.Status(SimulationSpeed.Speed4));
    }
}
