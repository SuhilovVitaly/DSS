using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private static BattleReportReporterSnapshot CaptureWeaponReportIdentity(WeaponOperatorSnapshot? captured,
        BattleReportReporterRole role) => new(role, captured?.CrewId, captured?.DisplayName);

    private BattleReportReporterSnapshot CaptureReportIdentity(SpaceObjectRuntime owner, string? moduleId,
        BattleReportReporterRole role)
    {
        if (role == BattleReportReporterRole.Captain)
            return new(role, DisplayName: owner.CaptainDisplayName, PortraitImage: owner.CaptainPortraitImage);
        var module = owner.Modules.FirstOrDefault(m => m.ModuleId == moduleId);
        var crew = owner.Crew.IsDefaultOrEmpty ? null : owner.Crew.FirstOrDefault(c => c.Id == module?.OperatorCrewId);
        return new(role, crew?.Id, crew?.DisplayName);
    }

    private (string? Id, string? Name) CaptureKnownParticipant(string? objectId)
    {
        var participant = _objects.Find(o => o.InitialMotion.ObjectId == objectId);
        return participant is not null && (participant.IsKnown || objectId == PlayerShipObjectId)
            ? (objectId, participant.Name) : (null, null);
    }

    private InstalledModuleRuntime? CombatReportScanner(SpaceObjectRuntime player, double motionTimeMs) =>
        player.Modules.Where(m => IsResourceScanner(m) && CanExecuteModuleCommand(m, motionTimeMs))
            .OrderBy(m => m.ModuleId, StringComparer.Ordinal).FirstOrDefault();
}
