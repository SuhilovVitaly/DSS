using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private PointCombatStateData? CapturePointCombatState(long time)
    {
        if (!UsesPointEncounters && !_launcherCombat.Values.Any(l => l.Profiles is { IsDefaultOrEmpty: false }) &&
            !_defenses.Values.Any(d => d.Profiles is { IsDefaultOrEmpty: false })) return null;
        return new(1,
            _objects.Where(o => o.InitialMotion.OwnProjectile is not null).OrderBy(o => o.InitialMotion.ObjectId, StringComparer.Ordinal)
                .Select(o => new PointProjectileSaveData(o.InitialMotion.ObjectId, RuntimeMotion.At(o, time).OwnProjectile!,
                    _debugProjectileTypes.GetValueOrDefault(o.InitialMotion.ObjectId),
                    _debugOriginalFlights.GetValueOrDefault(o.InitialMotion.ObjectId))).ToArray(),
            _evasions.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new EvasionSaveData(p.Key,
                p.Value.EngineModuleId, p.Value.StartedAt, p.Value.Motion,
                p.Value.OrbitCycle, p.Value.LastTurnTime,
                _objects.Single(o => o.InitialMotion.ObjectId == p.Key).InitialMotion with { Evasion = null })).ToArray(),
            new(LaunchScatterRng.AlgorithmVersion, _launchScatterRng.State, _launchScatterRng.Counter),
            new(1, _npcDodgeRng.State, _npcDodgeRng.Counter),
            _npcDodgeThreats.Values.OrderBy(t => t.OwnerId, StringComparer.Ordinal).ThenBy(t => t.ProjectileId, StringComparer.Ordinal)
                .Select(t => new NpcDodgeSaveData(t.OwnerId, t.EngineModuleId, t.ProjectileId, t.DetectedAt,
                    t.DueAt, t.Evaluated, t.RollSucceeded, t.Evaded)).ToArray(),
            new(_encounterState, _encounterStartedAt, _encounterEndedAt, _encounterQuietSince,
                _encounterEnemyIds.Order(StringComparer.Ordinal).ToArray(), _encounterProjectileIds.Order(StringComparer.Ordinal).ToArray(),
                _encounterStats, _encounterResumeSpeed), _playerEvasionEpisodes);
    }

    // Candidate objects are local to LoadScenario. All checks precede publication under the world lock.
    private void StagePointCombatRestore(GameStateData state, bool loadingSave, List<SpaceObjectRuntime> objects,
        Dictionary<(string ObjectId, string ModuleId), LauncherCombatSnapshot> launchers,
        Dictionary<(string ObjectId, string ModuleId), DefenseSnapshot> defenses)
    {
        var saved = state.PointCombatState;
        if (saved is null)
        {
            if (loadingSave && (UsesPointEncounters || launchers.Values.Any(l => l.Profiles is { IsDefaultOrEmpty: false }) ||
                defenses.Values.Any(d => d.Profiles is { IsDefaultOrEmpty: false })))
                throw new ScenarioException("Invalid point combat save: missing continuation.");
            return;
        }
        if (saved.Encounter.State == CombatEncounterState.Lost ||
            saved.Encounter.State == CombatEncounterState.Active && !Enum.Parse<SimulationSpeed>(state.CurrentSpeed).IsAllowedInCombat())
            throw new ScenarioException("Invalid point combat save: defeated state or active speed.");
        if (!UsesPointEncounters && !objects.SelectMany(o => o.Modules).Any(m => _registry.ModuleTypes.GetDefinition(m.ModuleTypeIndex).IsPointLauncher))
            throw new ScenarioException("Invalid point combat save: no point combat content.");
        foreach (var d in saved.DodgeThreats)
        {
            var owner = objects.FirstOrDefault(o => o.InitialMotion.ObjectId == d.OwnerId);
            if (owner is null) continue;
            var module = owner.Modules.FirstOrDefault(m => m.ModuleId == d.EngineModuleId);
            if (module is null || !IsEngineCategory(_registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex)))
                throw new ScenarioException("Invalid point combat save: NPC dodge module is not an engine.");
        }
        foreach (var row in saved.Projectiles)
        {
            int index = objects.FindIndex(o => o.InitialMotion.ObjectId == row.ObjectId);
            var f = row.Flight;
            if (row.LauncherModuleTypeId is { } typeId)
            {
                if (!_registry.ModuleTypes.Contains(typeId))
                    throw new ScenarioException("Invalid point combat save: unknown launch module type.");
                var type = _registry.ModuleTypes.GetDefinition(_registry.ModuleTypes.GetIndex(typeId));
                if (!type.IsPointLauncher || type.CategoryTypeId != (f.Kind == ProjectileKind.Torpedo
                    ? "module.torpedo.launcher" : "module.countermeasure.launcher"))
                    throw new ScenarioException("Invalid point combat save: invalid launch module type.");
                if (state.DebugTuning?.Any(v => v.ModuleTypeId == typeId && v.ProfileId == f.ProfileId &&
                    (v.Field == "speedKmS" && v.Value != f.SpeedKmS || v.Field == "blastRadiusKm" && v.Value != f.BlastRadiusKm)) == true)
                    throw new ScenarioException("Invalid point combat save: captured profile does not match effective tuning.");
            }
            if (index < 0 || objects[index].ObjectType != (f.Kind == ProjectileKind.Torpedo ? SpaceObjectType.Missile : SpaceObjectType.Countermeasure))
                throw new ScenarioException("Invalid point combat save: missing projectile object or kind mismatch.");
            var key = (f.OwnerObjectId, f.LauncherModuleId);
            var owner = objects.FirstOrDefault(o => o.InitialMotion.ObjectId == f.OwnerObjectId);
            var module = owner?.Modules.FirstOrDefault(m => m.ModuleId == f.LauncherModuleId);
            if (module is not null && row.LauncherModuleTypeId is { } capturedType &&
                _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex).TypeId != capturedType)
                throw new ScenarioException("Invalid point combat save: launch module type identity mismatch.");
            if (owner is not null && (f.Kind == ProjectileKind.Torpedo
                ? !launchers.TryGetValue(key, out var launcher) || !launcher.ActiveTorpedoIds.Contains(row.ObjectId)
                : !defenses.TryGetValue(key, out var defense) || defense.ActiveProjectileIds?.Contains(row.ObjectId) != true))
                throw new ScenarioException("Invalid point combat save: flight ownership mismatch.");
            var obj = objects[index];
            // Restore the original integration anchor, not a new incremental segment at save time.
            // Repeated interpolation from the saved pose changes floating-point low bits and can alter edge hits.
            objects[index] = obj with
            {
                StartGameTimeMs = f.LaunchMotionTimeMs,
                InitialMotion = obj.InitialMotion with
                {
                    X = f.StartX,
                    Y = f.StartY,
                    OwnProjectile = f with { X = f.StartX, Y = f.StartY }
                }
            };
        }
        foreach (var e in saved.Evasions)
        {
            int index = objects.FindIndex(o => o.InitialMotion.ObjectId == e.ObjectId && !o.IsDestroyed);
            var owner = index < 0 ? null : objects[index];
            var engine = owner?.Modules.FirstOrDefault(m => m.ModuleId == e.EngineModuleId);
            if (engine is null || e.IntegrationAnchor is null || !IsEngineCategory(_registry.ModuleTypes.GetDefinition(engine.ModuleTypeIndex)))
                throw new ScenarioException("Invalid point combat save: missing evasion engine.");
            objects[index] = owner! with
            {
                InitialMotion = e.IntegrationAnchor with { Evasion = e.Motion },
                StartGameTimeMs = (long)e.Motion.SampleMotionTimeMs
            };
        }
    }

    // Assignment only, after schema/world/content preflight succeeds.
    private void RestorePointCombatState(PointCombatStateData? saved)
    {
        if (saved is null) return;
        foreach (var p in saved.Projectiles)
        {
            if (p.OriginalFlight is { } original) _debugOriginalFlights[p.ObjectId] = original;
            if (p.LauncherModuleTypeId is { } type) _debugProjectileTypes[p.ObjectId] = type;
            else
            {
                var module = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == p.Flight.OwnerObjectId)?.Modules
                    .FirstOrDefault(m => m.ModuleId == p.Flight.LauncherModuleId);
                if (module is not null) _debugProjectileTypes[p.ObjectId] = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex).TypeId;
            }
        }
        _launchScatterRng = new(saved.LaunchScatter.State, saved.LaunchScatter.Counter);
        _npcDodgeRng = new(saved.NpcDodge.State, saved.NpcDodge.Counter);
        _playerEvasionEpisodes = saved.PlayerEvasionEpisodes;
        foreach (var e in saved.Evasions)
            _evasions.Add(e.ObjectId, new(e.EngineModuleId, e.StartedAt, e.Motion, e.OrbitCycle, e.LastTurnTime));
        foreach (var d in saved.DodgeThreats)
            _npcDodgeThreats.Add((d.OwnerId, d.ProjectileId), new(d.OwnerId, d.EngineModuleId, d.ProjectileId,
                d.DetectedAt, d.DueAt, d.Evaluated, d.RollSucceeded, d.Evaded));
        var encounter = saved.Encounter;
        _encounterState = encounter.State; _encounterStartedAt = encounter.StartedAt;
        _encounterEndedAt = encounter.EndedAt; _encounterQuietSince = encounter.QuietSince;
        _encounterStats = encounter.Stats; _encounterResumeSpeed = encounter.ResumeSpeed;
        _encounterEnemyIds.UnionWith(encounter.EnemyIds); _encounterProjectileIds.UnionWith(encounter.ProjectileIds);
    }
}
