using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;

/// <summary>Ordinary structure and condition from the installed-module snapshot.</summary>
internal sealed class ModuleDiagram
{
    internal sealed record Tile(string Id, string Name, int Structure, int StructureMax, bool Destroyed);
    internal IReadOnlyList<Tile> Tiles { get; private set; } = [];
    private ImmutableArray<InstalledModuleSnapshot> _lastModules;

    internal void Update(ImmutableArray<InstalledModuleSnapshot> modules)
    {
        if (modules == _lastModules) return;
        _lastModules = modules;
        Tiles = modules.IsDefaultOrEmpty ? [] : modules
            .Where(m => m.StructureMax > 0 && !m.ModuleTypeId.StartsWith("module.torpedo.", StringComparison.Ordinal) &&
                !m.ModuleTypeId.StartsWith("module.countermeasure.", StringComparison.Ordinal) && m.ModuleTypeId != "module.combat.laser")
            .OrderBy(m => m.Position).ThenBy(m => m.ModuleId, StringComparer.Ordinal)
            .Select(m => new Tile(m.ModuleId, m.DisplayName, m.StructureCurrent,
                m.StructureMax, m.IsDestroyed)).ToArray();
    }
}
