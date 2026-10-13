namespace DeepSpaceSaga.Engine.Combat;

internal readonly record struct ShipModuleLife(string ModuleId, string? CombatRole, bool Destroyed);

internal static class ShipDestructionMath
{
    internal static string? Cause(IReadOnlyList<ShipModuleLife> modules, string lastDestroyedModuleId,
        IReadOnlyCollection<string> criticalRoles, decimal destroyedFraction)
    {
        if (modules.Count == 0 || destroyedFraction is <= 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(modules));
        var last = modules.Single(module => module.ModuleId == lastDestroyedModuleId);
        if (!last.Destroyed) return null;
        if (last.CombatRole is { } role && criticalRoles.Contains(role))
            return "critical:" + lastDestroyedModuleId;
        int dead = modules.Count(module => module.Destroyed);
        int threshold = (int)decimal.Ceiling(modules.Count * destroyedFraction - 0.000000001m);
        return dead >= threshold ? $"threshold:{dead}/{modules.Count}" : null;
    }
}
