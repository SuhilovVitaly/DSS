using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Combat;

internal static class FireModeMath
{
    private static readonly FireModeDefinition Standard = new("standard", "Standard", 0, 0, 0, 1,
        ["module.torpedo.launcher"]);

    internal static FireModeDefinition Resolve(GameDataRegistry registry, string modeId)
    {
        if (registry.FireModes.IsDefaultOrEmpty && modeId == "standard") return Standard;
        return registry.FireModes.FirstOrDefault(mode => mode.ModeId == modeId &&
            mode.AppliesTo.Contains("module.torpedo.launcher")) ??
            throw new InvalidOperationException($"Unsupported torpedo fire mode '{modeId}'.");
    }
}
