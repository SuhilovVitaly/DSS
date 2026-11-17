using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    internal static void SetLootDebug(SetLootDebugRequest request)
    {
        lock (DemoDebugSettingsGate)
            Volatile.Write(ref _processDemoDebugSettings, _processDemoDebugSettings with
            { EngineerBonusPp = request.EngineerBonusPp, DestructionRolls = request.DestructionRolls, LootAttempts = request.Attempts });
    }
    private static bool LootDestroyed(int roll) => DemoDebugSettings.DestructionRolls switch
    { "keep" => false, "destroy" => true, _ => roll < 5000 };
    private static bool LootAttemptSucceeded(int roll, int chance) => DemoDebugSettings.LootAttempts switch
    { "success" => true, "fail" => false, _ => roll < chance };
    public static DemoDebugSettings ConfigureFogOfWar(bool? enabled = null)
    {
        lock (DemoDebugSettingsGate)
        {
            if (enabled is { } value)
                Volatile.Write(ref _processDemoDebugSettings, _processDemoDebugSettings with { FogOfWar = value });
            return _processDemoDebugSettings;
        }
    }

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
