using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private CampaignOutcomeSnapshot BuildCampaignOutcome()
    {
        string? shipId = PlayerShipObjectId;
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == shipId);
        bool destroyed = ship?.IsDestroyed == true;
        string? battleId = shipId is not null && _carrierSettlement.TryGetValue(shipId, out var receipt) ? receipt.BattleId : null;
        bool ready = shipId is not null && GetOwnedWing(shipId).Any(d => !d.Destroyed && !d.Disabled && IsHangarReady(d.HangarModuleId));
        return new(destroyed ? CampaignOutcomeKind.CarrierDestroyed : CampaignOutcomeKind.Active, battleId,
            ship is not null && !destroyed, destroyed ? DemoCampaignReasons.CarrierDestroyed : ready ? null : "no_ready_wing");
    }
}
