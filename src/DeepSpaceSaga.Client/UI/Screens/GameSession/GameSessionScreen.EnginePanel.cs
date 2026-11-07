using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private InstalledModuleSnapshot? PanelEngine => _buffer.Presented?.Snapshot is { InstalledModules.IsDefaultOrEmpty: false } snapshot
        ? snapshot.InstalledModules.FirstOrDefault(m => m.EngineMotion is not null) : null;

    private IReadOnlyList<string> GetEnginePanelDetails(string panel)
    {
        if (panel != "Engine" || PanelEngine is not { EngineMotion: { } limits } module) return [];
        double speed = FindPlayerShip(_renderStates)?.Pose.SpeedKmS ?? 0;
        int filled = limits.MaxSpeedKmS > 0 ? (int)Math.Round(Math.Clamp(speed / limits.MaxSpeedKmS, 0, 1) * 10) : 0;
        string bar = Localization.Get("World.Panel.BarLeft") + string.Concat(Enumerable.Repeat(Localization.Get("World.Panel.BarFull"), filled)) +
            string.Concat(Enumerable.Repeat(Localization.Get("World.Panel.BarEmpty"), 10 - filled)) + Localization.Get("World.Panel.BarRight");
        double? accelerationTime = limits.AccelerationKmS2 > 0 ? Math.Max(0, limits.MaxSpeedKmS - speed) / limits.AccelerationKmS2 : null;
        double? turnTime = limits.TurnRateDegPerSec > 0 ? 90 / limits.TurnRateDegPerSec : null;
        List<string> lines =
        [
            $"{Localization.Get("World.Panel.Speed")} {speed:0.0} / {limits.MaxSpeedKmS:0.0} {Localization.Get("World.Panel.KilometersPerSecond")} {bar}",
            $"{Localization.Get("World.Panel.Acceleration")} {limits.AccelerationKmS2:0.##} {Localization.Get("World.Panel.AccelerationUnit")} · {Localization.Get("World.Panel.TurnRate")} {limits.TurnRateDegPerSec:0.##} {Localization.Get("World.Panel.TurnRateUnit")}",
            $"{Localization.Get("World.Panel.AccelerationTime")}: {EngineDuration(accelerationTime)} · {Localization.Get("World.Panel.TurnTime")}: {EngineDuration(turnTime)}"
        ];
        if (module.IsDestroyed) lines.Add(Localization.Get("World.Panel.Destroyed"));
        else if (module.PowerState != "On" || module.OperationalState != "Ready") lines.Add(Localization.Get("World.Panel.Unavailable"));
        return lines;
    }

    private string EngineDuration(double? physicalSeconds) => physicalSeconds is { } seconds && double.IsFinite(seconds)
        ? PanelTime.Format(PanelTime.RealSeconds(seconds, PanelDisplaySpeed)) + " " + Localization.Get("World.Panel.Seconds")
        : Localization.Get("World.Panel.UnknownValue");

    private string? GetEnginePanelTimeTooltip(string panel)
    {
        if (panel != "Engine" || PanelEngine is not { EngineMotion: { } limits }) return null;
        double speed = FindPlayerShip(_renderStates)?.Pose.SpeedKmS ?? 0;
        List<string> lines = [];
        if (limits.AccelerationKmS2 > 0)
            lines.Add(Localization.Get("World.Panel.AccelerationTime") + ": " + PanelTime.Tooltip(Math.Max(0, limits.MaxSpeedKmS - speed) / limits.AccelerationKmS2, PanelDisplaySpeed));
        if (limits.TurnRateDegPerSec > 0)
            lines.Add(Localization.Get("World.Panel.TurnTime") + ": " + PanelTime.Tooltip(90 / limits.TurnRateDegPerSec, PanelDisplaySpeed));
        return string.Join('\n', lines);
    }
}
