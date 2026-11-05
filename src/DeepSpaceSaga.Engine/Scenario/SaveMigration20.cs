namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Validates the old continuation before projecting it into a world-only Save20.</summary>
internal static class SaveMigration20
{
    internal const int PreviousVersion = 19;

    internal static ScenarioFile Upgrade(ScenarioFile scenario, bool allowNonZeroGameTime = true)
    {
        ScenarioLoader.RequireSupportedSaveVersion(scenario.SaveFormatVersion);
        if (scenario.GameState is null) throw new ScenarioException("Missing gameState.");
        if (scenario.SaveFormatVersion == SaveFormat.CurrentSaveFormatVersion)
        {
            RequireWorldOnly(scenario.GameState);
            return scenario;
        }
        if (scenario.SaveFormatVersion == 18 && (scenario.GameState.DebugTuning is not null ||
            scenario.GameState.PointCombatState?.Projectiles?.Any(p => p?.LauncherModuleTypeId is not null || p?.OriginalFlight is not null) == true))
            throw new ScenarioException("Invalid Save18: debug tuning/provenance requires Save19. Save was not modified.");

        // Use the old schema validation rules on the whole payload; stripping data is not a repair.
        var validated = ScenarioLoader.ValidateAndNormalize(scenario with { SaveFormatVersion = PreviousVersion }, allowNonZeroGameTime);
        if (validated.GameState.CombatReactionDelays is { Count: > 0 })
            throw new ScenarioException("Retired randomized combat reaction continuation is unsupported.");
        if (validated.GameState.PointCombatState is null && validated.GameState.SpaceObjects.Any(o =>
            o.Modules?.Any(m => ProjectileModule(m.ModuleTypeId)) == true))
            throw new ScenarioException("Invalid point combat save: missing continuation.");
        return validated with { SaveFormatVersion = SaveFormat.CurrentSaveFormatVersion, GameState = WorldOnly(validated.GameState) };
    }

    internal static GameStateData WorldOnly(GameStateData state)
    {
        var combatActors = new HashSet<string>(state.HostileCombatState?.Actors.Select(a => a.ObjectId) ?? [], StringComparer.OrdinalIgnoreCase);
        var removedObjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var removedModules = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var obj in state.SpaceObjects)
        {
            if (CombatObject(obj) || combatActors.Contains(obj.ObjectId))
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
                    ArmorClass = "none",
                    Armor = null,
                    AutoDefenseEnabled = false,
                    DisabledUntilMotionTimeMs = null,
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
            CombatState = null,
            DefenseState = null,
            HostileCombatState = null,
            CombatReportsState = null,
            CombatReactionDelays = null,
            PointCombatState = null,
            DebugTuning = state.DebugTuning?.Where(v => !CombatModule(v.ModuleTypeId)).ToArray() is { Length: > 0 } tuning ? tuning : null
        };

        bool Removed(string? id) => id is not null && removedObjects.Contains(id);
        bool Gone(string? id, string? module) => Removed(id) ||
            id is not null && module is not null && removedModules.TryGetValue(id, out var modules) && modules.Contains(module);
    }

    internal static void RequireWorldOnly(GameStateData state)
    {
        if (state.CombatState is not null || state.DefenseState is not null || state.HostileCombatState is not null ||
            state.CombatReportsState is not null || state.CombatReactionDelays is not null || state.PointCombatState is not null ||
            state.SpaceObjects?.Any(o => o is not null && (CombatObject(o) ||
                (o.Modules?.Any(m => m is not null && CombatModule(m.ModuleTypeId)) ?? false))) == true ||
            state.PendingCommands?.Any(c => c is not null && CombatCommand(c.CommandType)) == true ||
            state.CommandReceipts?.Any(r => r is not null && CombatCommand(r.CommandType)) == true)
            throw new ScenarioException("Invalid Save20: obsolete real-time combat data. Save was not modified.");
    }

    private static bool CombatObject(SpaceObjectData obj) =>
        Equal(obj.ObjectType, "Missile") || Equal(obj.ObjectType, "Countermeasure") || Equal(obj.ObjectType, "Wreck") ||
        Equal(obj.ObjectType, "NpcShip") && (obj.Ai is not null || Equal(obj.RelationToPlayer, "Enemy"));
    private static bool ProjectileModule(string? type) => Prefix(type, "module.torpedo.") || Prefix(type, "module.countermeasure.");
    private static bool CombatModule(string? type) => ProjectileModule(type) || Equal(type, "module.combat.laser");
    private static bool CombatCommand(string? type) => Prefix(type, "torpedo.") || Prefix(type, "defense.") || Equal(type, "maneuver.evade");
    private static bool Ammo(string? type) => Prefix(type, "item.ammo.torpedo") || Prefix(type, "item.ammo.countermeasure");
    private static bool Prefix(string? value, string prefix) => value?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true;
    private static bool Equal(string? value, string other) => string.Equals(value, other, StringComparison.OrdinalIgnoreCase);
}
