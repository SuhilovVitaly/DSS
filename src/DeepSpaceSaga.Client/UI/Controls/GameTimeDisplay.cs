using System.Globalization;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Controls;

public static class GameTimeDisplay
{
    public static string Minutes(long? gameTimeMs)
    {
        if (gameTimeMs is null) return "День — · —:—";
        var time = GameCalendar.FromGameTime(gameTimeMs.Value);
        return string.Create(CultureInfo.InvariantCulture, $"День {time.Day} · {time.Hour:00}:{time.Minute:00}");
    }

    public static string Hours(long? gameTimeMs)
    {
        if (gameTimeMs is null) return "День — · — ч";
        var time = GameCalendar.FromGameTime(gameTimeMs.Value);
        return string.Create(CultureInfo.InvariantCulture, $"День {time.Day} · {time.Hour:00} ч");
    }

    public static string Status(SimulationSpeed speed) => speed == SimulationSpeed.Speed0
        ? "Пауза" : string.Create(CultureInfo.InvariantCulture, $"×{(int)speed}");
}
