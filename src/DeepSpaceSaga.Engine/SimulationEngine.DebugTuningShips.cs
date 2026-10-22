using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private sealed record DebugShipRetiming(List<SpaceObjectRuntime> Objects,
        Dictionary<string, EvasionState> Evasions, Dictionary<string, HostileCombatSnapshot> Hostiles);

    private bool TryStageDebugShips(GameDataRegistry candidate, long time, out DebugShipRetiming staged)
    {
        staged = new([.. _objects], new(_evasions, StringComparer.Ordinal), new(_hostileCombat, StringComparer.Ordinal));
        var changed = new List<(int Index, ModuleTypeDefinition Type)>();
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
                HostileCombat = null
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
            if (staged.Evasions.TryGetValue(owner.InitialMotion.ObjectId, out var evasion) && evasion.Motion.Steering is { } steering)
            {
                double turnRate = type.AngularInertiaDegPerSec!.Value, acceleration = type.LinearInertiaMps2!.Value / 1000.0;
                var basis = evasion.Motion.BaseMotion with { SpeedKmS = Math.Min(evasion.Motion.BaseMotion.SpeedKmS, maxSpeed), MaxSpeedKmS = maxSpeed };
                EvasionSnapshot schedule;
                if (time <= evasion.Motion.EndsAtMotionTimeMs)
                    schedule = EvasionMotionMath.Plan(motion, basis, time, evasion.Motion.EndsAtMotionTimeMs,
                        evasion.Motion.ThreatProjectileId, evasion.Motion.Side, steering.Direction, maxSpeed, turnRate, acceleration);
                else
                {
                    double angle = Math.Abs((basis.Direction - motion.Direction + 540) % 360 - 180);
                    double end = time + Math.Max(angle / turnRate, Math.Abs(basis.SpeedKmS - motion.SpeedKmS) / acceleration) * 1000;
                    schedule = evasion.Motion with
                    {
                        SampleMotionTimeMs = time,
                        BaseMotion = basis,
                        Steering = steering with { MaxSpeedKmS = maxSpeed, TurnRateDegPerSec = turnRate, AccelerationKmS2 = acceleration, ReturnsAtMotionTimeMs = end }
                    };
                }
                if (!double.IsFinite(EvasionMotionMath.CompletionTime(schedule)) || EvasionMotionMath.CompletionTime(schedule) >= long.MaxValue)
                    return false;
                staged.Evasions[owner.InitialMotion.ObjectId] = evasion with { Motion = schedule };
                motion = motion with { Evasion = schedule };
            }
            staged.Objects[i] = owner with { InitialMotion = motion, StartGameTimeMs = time, Modules = modules };
            changed.Add((i, type));
        }
        foreach (var (index, type) in changed)
        {
            var owner = staged.Objects[index];
            if (owner.InitialMotion.Evasion is not null || owner.IsDocked ||
                _objects[index].InitialMotion.HostileCombat is not { } previous) continue;
            var target = staged.Objects.FirstOrDefault(o => o.InitialMotion.ObjectId == previous.TargetObjectId);
            if (target is null) continue;
            var next = HostileOrbitMath.PlanAttack(owner.InitialMotion, RuntimeMotion.At(target, time), time,
                previous.EngineModuleId, previous.WeaponModuleId, previous.DesiredRadiusKm,
                type.MaxSpeedMps!.Value / 1000.0, type.LinearInertiaMps2!.Value / 1000.0,
                type.AngularInertiaDegPerSec!.Value, owner.AttackAi?.BandKm, owner.AttackAi?.FlankOffsetDeg ?? 0, previous);
            staged.Hostiles[owner.InitialMotion.ObjectId] = next;
            staged.Objects[index] = owner with { InitialMotion = owner.InitialMotion with { HostileCombat = next } };
        }
        return true;
    }
}
