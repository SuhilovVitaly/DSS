using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    /// <summary>Engine-process debug override, intentionally independent of sessions, saves and user settings.</summary>
    public static DemoDebugSettings ConfigurePirateSquadChance(int? percent = null)
    {
        if (percent is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(percent), "Squad chance must be in 0..100.");
        lock (DemoDebugSettingsGate)
        {
            if (percent is { } value)
                Volatile.Write(ref _processDemoDebugSettings, _processDemoDebugSettings with { PirateSquadChancePercent = value });
            return _processDemoDebugSettings;
        }
    }
}
