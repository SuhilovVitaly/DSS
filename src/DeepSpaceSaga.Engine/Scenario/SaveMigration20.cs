namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Validates the old continuation before projecting it into a world-only Save20.</summary>
internal static class SaveMigration20
{
    internal const int PreviousVersion = 19;

    internal static ScenarioFile Upgrade(ScenarioFile scenario, bool allowNonZeroGameTime = true, HashSet<string>? archivedActors = null)
    {
        ScenarioLoader.RequireSupportedSaveVersion(scenario.SaveFormatVersion);
        if (scenario.GameState is null) throw new ScenarioException("Missing gameState.");
        if (scenario.SaveFormatVersion == SaveFormat.CurrentSaveFormatVersion)
        {
            RequireWorldOnly(scenario.GameState);
            return scenario;
        }

        var validated = ScenarioLoader.ValidateAndNormalize(scenario, allowNonZeroGameTime);
        return validated with
        {
            SaveFormatVersion = SaveFormat.CurrentSaveFormatVersion,
            GameState = ProjectWorld(validated.GameState, removeLegacyHostiles: true, archivedActors)
        };
    }

    internal static GameStateData WorldOnly(GameStateData state) =>
        ProjectWorld(state, removeLegacyHostiles: false, archivedActors: null);

    private static GameStateData ProjectWorld(GameStateData state, bool removeLegacyHostiles, HashSet<string>? archivedActors)
    {
        var removedObjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var removedModules = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var obj in state.SpaceObjects)
        {
            if (CombatObject(obj) || (removeLegacyHostiles && LegacyHostile(obj)) || archivedActors?.Contains(obj.ObjectId) == true)
            {
                removedObjects.Add(obj.ObjectId);
                continue;
            }
            var combatModules = (obj.Modules ?? []).Where(m => CombatModule(m.ModuleTypeId)).Select(m => m.ModuleId);
            removedModules[obj.ObjectId] = new(combatModules, StringComparer.OrdinalIgnoreCase);
        }
        var objects = state.SpaceObjects.Where(o => !removedObjects.Contains(o.ObjectId)).Select(o => o with
        {
            Modules = o.Modules?.Where(m => !CombatModule(m.ModuleTypeId)).Select(m =>
            {
                bool clearCycle = m.ActiveCycle is { } cycle &&
                    (CombatCommand(cycle.CommandType) || Removed(cycle.TargetObjectId) || Removed(cycle.ObjectId));
                return m with
                {
                    ActiveCycle = clearCycle ? null : m.ActiveCycle,
                    Cargo = m.Cargo?.Where(c => !Ammo(c.ItemTypeId)).ToArray()
                };
            }).ToArray()
        }).ToArray();

        return state with
        {
            Focus = state.Focus is { } focus && Removed(focus.ObjectId) ? null : state.Focus,
            SpaceObjects = objects,
            CommandReceipts = state.CommandReceipts?.Where(r => !CombatCommand(r.CommandType) && !Gone(r.ObjectId, r.ModuleId)).ToArray(),
            PendingCommands = state.PendingCommands?.Where(c => !CombatCommand(c.CommandType) && !Gone(c.ObjectId, c.ModuleId) &&
                !Removed(c.TargetObjectId) && !Ammo(c.ItemTypeId)).ToArray(),
            DebugTuning = state.DebugTuning?.Where(v => !CombatModule(v.ModuleTypeId)).ToArray() is { Length: > 0 } tuning ? tuning : null
        };

        bool Removed(string? id) => id is not null && removedObjects.Contains(id);
        bool Gone(string? id, string? module) => Removed(id) ||
            id is not null && module is not null && removedModules.TryGetValue(id, out var modules) && modules.Contains(module);
    }

    internal static void RequireWorldOnly(GameStateData state)
    {
        if (state.SpaceObjects?.Any(o => o is not null && (CombatObject(o) ||
                (o.Modules?.Any(m => m is not null && CombatModule(m.ModuleTypeId)) ?? false))) == true ||
            state.PendingCommands?.Any(c => c is not null && CombatCommand(c.CommandType)) == true ||
            state.CommandReceipts?.Any(r => r is not null && CombatCommand(r.CommandType)) == true)
            throw new ScenarioException("Invalid Save20: obsolete real-time combat data. Save was not modified.");
    }

    private static bool CombatObject(SpaceObjectData obj) =>
        Equal(obj.ObjectType, "Missile") || Equal(obj.ObjectType, "Countermeasure") || Equal(obj.ObjectType, "Wreck");
    private static bool LegacyHostile(SpaceObjectData obj) =>
        Equal(obj.ObjectType, "NpcShip") && Equal(obj.RelationToPlayer, "Enemy");
    private static bool ProjectileModule(string? type) => Prefix(type, "module.torpedo.") || Prefix(type, "module.countermeasure.");
    private static bool CombatModule(string? type) => ProjectileModule(type) || Prefix(type, "module.combat.");
    internal static bool CombatCommand(string? type) => Prefix(type, "torpedo.") || Prefix(type, "defense.") || Prefix(type, "combat.") || Equal(type, "maneuver.evade");
    private static bool Ammo(string? type) => Prefix(type, "item.ammo.torpedo") || Prefix(type, "item.ammo.countermeasure");
    private static bool Prefix(string? value, string prefix) => value?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true;
    private static bool Equal(string? value, string other) => string.Equals(value, other, StringComparison.OrdinalIgnoreCase);
}
