using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    // Internal engine ownership: NPCs never submit player commands or claim player receipts.
    private readonly Dictionary<string, HostileCombatSnapshot> _hostileCombat = new(StringComparer.Ordinal);
    private long _nextHostileDecisionMs;

    private bool HasHostileMotionCandidates =>
        _objects.Any(o => o.InitialMotion.ObjectId == PlayerShipObjectId && !o.IsDestroyed) &&
        _objects.Any(o => IsHostileActor(o) && !o.IsDocked &&
            o.Modules.Any(m => _launcherCombat.ContainsKey((o.InitialMotion.ObjectId, m.ModuleId))) &&
            o.Modules.Any(m => CanExecuteEngineCommand(m, _registry.ModuleTypes.GetDefinition(m.ModuleTypeIndex),
                Math.Max(_combatProcessedTimeMs, m.DisabledUntilMotionTimeMs ?? _combatProcessedTimeMs))));

    private void RefreshHostileCombat(long time)
    {
        if (time < _nextHostileDecisionMs) return;
        long remainder = time % HostileOrbitMath.DecisionIntervalMs;
        long delta = HostileOrbitMath.DecisionIntervalMs - remainder;
        _nextHostileDecisionMs = time > long.MaxValue - delta ? long.MaxValue : time + delta;
        var player = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId && !o.IsDestroyed);
        var target = player is null ? null : RuntimeMotion.At(player, time);
        foreach (var id in _hostileCombat.Keys.Where(id => !_objects.Any(o => o.InitialMotion.ObjectId == id && IsHostileActor(o))).ToArray())
            _hostileCombat.Remove(id);

        foreach (var owner in _objects.Where(o => IsHostileActor(o) || o.InitialMotion.HostileCombat is not null)
            .OrderBy(o => o.InitialMotion.ObjectId, StringComparer.Ordinal).ToArray())
        {
            var weapon = owner.Modules.OrderBy(m => m.ModuleId, StringComparer.Ordinal)
                .FirstOrDefault(m => _launcherCombat.ContainsKey((owner.InitialMotion.ObjectId, m.ModuleId)));
            var engine = owner.Modules.OrderBy(m => m.ModuleId, StringComparer.Ordinal)
                .FirstOrDefault(m => CanExecuteEngineCommand(m, _registry.ModuleTypes.GetDefinition(m.ModuleTypeIndex), time) && m.ActiveCycle is null);
            var pose = RuntimeMotion.At(owner, time);
            if (target is null || !IsHostileActor(owner) || owner.IsDocked || weapon is null || engine is null)
            {
                if (owner.InitialMotion.HostileCombat is not null)
                    _objects[_objects.IndexOf(owner)] = owner with { InitialMotion = pose with { HostileCombat = null }, StartGameTimeMs = time };
                continue;
            }
            var definition = _registry.ModuleTypes.GetDefinition(engine.ModuleTypeIndex);
            var launcher = _launcherCombat[(owner.InitialMotion.ObjectId, weapon.ModuleId)];
            var state = HostileOrbitMath.Plan(pose, target, time, engine.ModuleId, weapon.ModuleId,
                launcher.RangeKm!.Value / 2, definition.MaxSpeedMps!.Value / 1000.0,
                definition.LinearInertiaMps2!.Value / 1000.0, definition.AngularInertiaDegPerSec!.Value,
                _hostileCombat.GetValueOrDefault(owner.InitialMotion.ObjectId));
            _hostileCombat[owner.InitialMotion.ObjectId] = state;
            _objects[_objects.IndexOf(owner)] = owner with
            {
                InitialMotion = pose with { HostileCombat = state },
                StartGameTimeMs = time
            };
        }
    }
}
