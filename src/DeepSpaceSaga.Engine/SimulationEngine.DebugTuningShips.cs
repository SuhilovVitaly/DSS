using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private bool TryStageDebugShips(GameDataRegistry candidate, long time, out List<SpaceObjectRuntime> staged)
    {
        staged = [.. _objects];
        for (int i = 0; i < _objects.Count; i++)
        {
            var owner = _objects[i];
            if (owner.IsDestroyed) continue;
            var engine = owner.Modules.OrderBy(m => m.ModuleId, StringComparer.Ordinal).FirstOrDefault(m =>
            {
                var old = _registry.ModuleTypes.GetDefinition(m.ModuleTypeIndex);
                var next = candidate.ModuleTypes.GetDefinition(m.ModuleTypeIndex);
                return IsEngineCategory(old) && next.MaxSpeedMps is > 0 && next.LinearInertiaMps2 is > 0 &&
                    next.AngularInertiaDegPerSec is > 0 && (old.MaxSpeedMps != next.MaxSpeedMps ||
                    old.LinearInertiaMps2 != next.LinearInertiaMps2 || old.AngularInertiaDegPerSec != next.AngularInertiaDegPerSec);
            });
            if (engine is null) continue;
            var type = candidate.ModuleTypes.GetDefinition(engine.ModuleTypeIndex);
            double maxSpeed = type.MaxSpeedMps!.Value / 1000.0;
            var pose = RuntimeMotion.At(owner, time);
            var motion = pose with
            {
                SpeedKmS = owner.IsDocked ? pose.SpeedKmS : Math.Min(pose.SpeedKmS, maxSpeed),
                MaxSpeedKmS = maxSpeed,
                TurnHoldRateDegPerSec = pose.TurnHoldRateDegPerSec is { } turn
                    ? Math.CopySign(type.AngularInertiaDegPerSec!.Value, turn) : null,
                SpeedHoldAccelerationKmS2 = pose.SpeedHoldAccelerationKmS2 is { } heldAcceleration
                    ? Math.CopySign(type.LinearInertiaMps2!.Value / 1000.0, heldAcceleration) : null
            };
            var modules = owner.Modules;
            for (int m = 0; m < modules.Length; m++)
            {
                var module = modules[m];
                if (module.ActiveCycle is not { } cycle || !IsEngineCategory(candidate.ModuleTypes.GetDefinition(module.ModuleTypeIndex))) continue;
                long remaining = Math.Max(0, cycle.DurationMs - Math.Max(0, time - cycle.StartedGameTimeMs));
                var next = cycle with { StartedGameTimeMs = time, DurationMs = remaining };
                if (cycle.CommandType is ShipEngineCommandTypes.Orbit or NavigationComputerCommandTypes.Approach)
                {
                    next = next with { DurationMs = MinTurnIntervalMs(type.AngularInertiaDegPerSec!.Value) };
                    if (cycle.CommandType == NavigationComputerCommandTypes.Approach &&
                        _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == cycle.TargetObjectId) is { } target)
                    {
                        var p = RuntimeMotion.At(target, time);
                        var route = ApproachLineCaptureMath.Plan(motion, p.X, p.Y, p.Direction, p.SpeedKmS,
                            cycle.NavigationApproachTrailDistanceWorldUnits ?? 10, type.AngularInertiaDegPerSec!.Value);
                        next = next with { ApproachRoute = route };
                    }
                }
                modules = modules.SetItem(m, module with { ActiveCycle = next });
            }
            staged[i] = owner with { InitialMotion = motion, StartGameTimeMs = time, Modules = modules };
        }
        return true;
    }
}
