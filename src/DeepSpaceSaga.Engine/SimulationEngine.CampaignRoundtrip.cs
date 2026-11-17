using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private static SortieSettlementSnapshot ProjectSettlement(string shipId, SortieSettlementData receipt) => new(
        receipt.BattleId, shipId, receipt.CarrierRevision, receipt.ShipDestroyed,
        receipt.LostDroneIds?.ToImmutableArray() ?? [], receipt.ConsumedTorpedoes,
        Enum.Parse<SortieExitKind>(receipt.ExitKind));

    private DemoCompanySnapshot BuildDemoCompanyProjection()
    {
        var company = _demoCampaign.Company ?? new();
        return company with
        {
            Supply = BuildSupplyProjection(),
            Replacement = BuildReplacementProjection(_demoCampaign),
            PilotLoss = BuildPilotLossProjection(),
            Outcome = BuildCampaignOutcome(),
            Roundtrip = PlayerShipObjectId is { } ship && _carrierSettlement.TryGetValue(ship, out var receipt)
                ? new(ProjectSettlement(ship, receipt)) : null
        };
    }
}
