using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private DefenseLaunchPreview? BuildDefensePreview(SpaceObjectRuntime owner, InstalledModuleRuntime module, long time)
    {
        string ownerId = owner.InitialMotion.ObjectId;
        if (!_defenses.ContainsKey((ownerId, module.ModuleId))) return null;
        var target = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == SelectedObjectId &&
            !o.IsDestroyed && o.InitialMotion.Torpedo is not null);
        decimal? accuracy = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex).CountermeasureAccuracy;
        decimal? maneuverability = null;
        int? chance = null;
        if (target?.InitialMotion.Torpedo is { } flight)
        {
            var shooter = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == flight.OwnerObjectId && !o.IsDestroyed);
            string? relation = shooter is null ? flight.OwnerPlayerRelationAtLaunch : GetRelationToPlayer(shooter);
            if (flight.OwnerObjectId != ownerId && relation == PlayerRelation.Enemy)
            {
                maneuverability = flight.CapturedManeuverability ?? flight.TorpedoRating;
                if (accuracy is { } a && maneuverability is { } m) chance = InterceptionMath.ChanceTenths(a, m);
            }
        }
        string? reason = ValidateManualDefense(ownerId, module.ModuleId, SelectedObjectId, time, out _);
        double? firstContact = null;
        if (reason is null)
        {
            long lifetime = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex).CountermeasureMaxFlightTimeMs!.Value;
            if (time > long.MaxValue - lifetime) reason = "invalid_time";
            else if (!TryGetNextCountermeasureId(out _, out _)) reason = "projectile_id_exhausted";
            else firstContact = FirstPredictedTorpedoContact(target!, time, time + lifetime);
        }
        return new(ownerId, module.ModuleId, target?.InitialMotion.ObjectId, chance, reason is null,
            reason, accuracy, maneuverability, firstContact);
    }
}
