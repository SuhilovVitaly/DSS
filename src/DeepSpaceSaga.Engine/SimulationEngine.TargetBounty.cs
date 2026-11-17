using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private DemoCampaignData PrepareBountiesForLoad(DemoCampaignData campaign)
    {
        if (campaign.CatalogFingerprint is null)
        {
            if (!campaign.BountyOrders.IsEmpty || campaign.BountyCatalogFingerprint is not null)
                throw new ScenarioException("Bounties require a demo campaign.");
            return campaign;
        }
        var definitions = _demoDefinitions.Jobs?.Bounties ?? [];
        string? fingerprint = _demoDefinitions.BountyFingerprint;
        if (campaign.BountyCatalogFingerprint is null)
            return campaign with
            {
                BountyCatalogFingerprint = fingerprint,
                BountyOrders = definitions.Select(b => new BountyOrderData(b.Id, b.IssuerStationId,
                    b.TargetGroupId, b.RewardTokens, false)).ToImmutableArray()
            };
        if (campaign.BountyCatalogFingerprint != fingerprint || campaign.BountyOrders.Length != definitions.Length ||
            campaign.BountyOrders.Any(b => !definitions.Any(d => d.Id == b.Id && d.IssuerStationId == b.IssuerStationId &&
                d.TargetGroupId == b.TargetGroupId && d.RewardTokens == b.RewardTokens)))
            throw new ScenarioException("incompatible_bounty_catalog: saved campaign was not modified.");
        return campaign;
    }

    private DemoRewardsSnapshot? BuildDemoRewardsProjection()
    {
        if (_demoCampaign.BountyOrders.IsEmpty) return _demoCampaign.Rewards;
        return (_demoCampaign.Rewards ?? new()) with { Bounties = BuildTargetBountyProjection() };
    }

    private TargetBountySnapshot BuildTargetBountyProjection() => new(_demoCampaign.BountyOrders
        .Select(b => ProjectBounty(_demoCampaign, b)).ToImmutableArray());

    private static BountyOrder ProjectBounty(DemoCampaignData campaign, BountyOrderData order)
    {
        var victory = campaign.EncounterResults.SingleOrDefault(r => r.PlayerWon &&
            r.Binding.GroupId == order.TargetGroupId && r.Binding.BattleId == "battle:" + order.TargetGroupId);
        bool available = campaign.PirateWorld?.Groups.Any(g => g.GroupId == order.TargetGroupId && !g.Retired) == true;
        string eligibility = order.PaidPostingId is not null ? BountyEligibility.Paid
            : victory is not null ? BountyEligibility.VictoryConfirmed
            : available ? BountyEligibility.TargetAvailable : BountyEligibility.Unavailable;
        return new(order.Id, order.IssuerStationId, order.TargetGroupId, order.RewardTokens,
            order.Tracking, eligibility, victory?.Binding.BattleId,
            Reason: eligibility == BountyEligibility.Unavailable ? TargetBountyReasons.Unavailable : null);
    }

    private static void PrepareTargetBounty(DemoCampaignCommand command, ref DemoCampaignData candidate, ref string? reason)
    {
        string id = command is AcceptBountyCommand accept ? accept.BountyId : ((CancelBountyCommand)command).BountyId;
        int index = -1;
        for (int i = 0; i < candidate.BountyOrders.Length; i++)
            if (candidate.BountyOrders[i].Id == id) { index = i; break; }
        if (index < 0) { reason = TargetBountyReasons.NotFound; return; }
        var order = candidate.BountyOrders[index];
        var projection = ProjectBounty(candidate, order);
        bool tracking = command is AcceptBountyCommand;
        if (tracking && projection.Eligibility != BountyEligibility.TargetAvailable)
        {
            reason = projection.Eligibility == BountyEligibility.Paid ? TargetBountyReasons.AlreadyPaid : TargetBountyReasons.Unavailable;
            return;
        }
        candidate = candidate with { BountyOrders = candidate.BountyOrders.SetItem(index, order with { Tracking = tracking }) };
    }
}
