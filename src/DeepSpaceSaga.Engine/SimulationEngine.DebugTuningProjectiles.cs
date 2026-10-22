using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    // Original immutable launch facts remain separate from the retimed integration anchor.
    private readonly Dictionary<string, OwnProjectileSnapshot> _debugOriginalFlights = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _debugProjectileTypes = new(StringComparer.Ordinal);

    private bool TryStageDebugProjectiles(GameDataRegistry candidate, long time, List<SpaceObjectRuntime> objects,
        out Dictionary<string, OwnProjectileSnapshot> originals, out Dictionary<string, string> launcherTypes)
    {
        originals = new(_debugOriginalFlights, StringComparer.Ordinal);
        launcherTypes = new(_debugProjectileTypes, StringComparer.Ordinal);
        for (int i = 0; i < objects.Count; i++)
        {
            var projectile = objects[i];
            if (projectile.InitialMotion.OwnProjectile is not { } flight) continue;
            string id = projectile.InitialMotion.ObjectId;
            if (!launcherTypes.TryGetValue(id, out var typeId))
            {
                var owner = objects.FirstOrDefault(o => o.InitialMotion.ObjectId == flight.OwnerObjectId);
                var module = owner?.Modules.FirstOrDefault(m => m.ModuleId == flight.LauncherModuleId);
                if (module is null) continue;
                typeId = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex).TypeId;
                launcherTypes[id] = typeId;
            }
            if (!candidate.ModuleTypes.Contains(typeId)) continue;
            var type = candidate.ModuleTypes.GetDefinition(candidate.ModuleTypes.GetIndex(typeId));
            if (type.Profiles.IsDefault) continue;
            var profile = type.Profiles.FirstOrDefault(p => p.ProfileId == flight.ProfileId);
            if (profile is null || profile.SpeedKmS == flight.SpeedKmS && profile.BlastRadiusKm == flight.BlastRadiusKm) continue;
            originals.TryAdd(id, flight);
            var pose = RuntimeMotion.At(projectile, time);
            var replacement = flight with { X = pose.X, Y = pose.Y, BlastRadiusKm = profile.BlastRadiusKm };
            if (profile.SpeedKmS != flight.SpeedKmS)
            {
                double distance = double.Hypot(flight.DetonationX - pose.X, flight.DetonationY - pose.Y) / WorldUnitsPerKm;
                double deadline = BlastMath.DetonationTimeMs(time, distance, profile.SpeedKmS);
                if (!double.IsFinite(deadline) || deadline >= long.MaxValue) return false;
                replacement = replacement with
                {
                    StartX = pose.X,
                    StartY = pose.Y,
                    LaunchMotionTimeMs = time,
                    DetonationMotionTimeMs = deadline,
                    SpeedKmS = profile.SpeedKmS
                };
            }
            // Radius-only changes keep the original integration anchor to avoid numerical drift.
            var anchor = profile.SpeedKmS == flight.SpeedKmS ? projectile.InitialMotion : pose;
            objects[i] = projectile with
            {
                StartGameTimeMs = replacement.LaunchMotionTimeMs,
                InitialMotion = anchor with
                {
                    X = replacement.StartX,
                    Y = replacement.StartY,
                    SpeedKmS = replacement.SpeedKmS,
                    OwnProjectile = replacement with { X = replacement.StartX, Y = replacement.StartY }
                }
            };
        }
        return true;
    }
}
