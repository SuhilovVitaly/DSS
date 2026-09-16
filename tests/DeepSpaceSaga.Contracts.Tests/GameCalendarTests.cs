using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class GameCalendarTests
{
    [Theory]
    [InlineData(0, 1, 0, 0)]
    [InlineData(86_399_999, 1, 23, 59)]
    [InlineData(86_400_000, 2, 0, 0)]
    [InlineData(203_459_999, 3, 8, 30)]
    public void Calendar_uses_elapsed_days_and_truncates_time(long ms, long day, int hour, int minute)
        => Assert.Equal(new GameCalendar(day, hour, minute), GameCalendar.FromGameTime(ms));
}
