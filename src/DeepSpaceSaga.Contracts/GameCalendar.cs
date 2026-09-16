namespace DeepSpaceSaga.Contracts;

/// <summary>Calendar derived exclusively from authoritative elapsed game milliseconds.</summary>
public readonly record struct GameCalendar(long Day, int Hour, int Minute)
{
    public const long HourMs = 3_600_000;
    public const long DayMs = 24 * HourMs;

    public static GameCalendar FromGameTime(long gameTimeMs)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(gameTimeMs);
        long timeOfDay = gameTimeMs % DayMs;
        return new(1 + gameTimeMs / DayMs, (int)(timeOfDay / HourMs),
            (int)(timeOfDay % HourMs / 60_000));
    }
}
