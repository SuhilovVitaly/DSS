using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private BountyPaymentSnapshot BuildBountyPaymentProjection() => new(_demoCampaign.BountyOrders.Select(order =>
    {
        var bounty = ProjectBounty(_demoCampaign, order);
        var paid = _demoCampaign.BountyPayments.SingleOrDefault(p => p.BountyId == order.Id);
        bool eligible = bounty.Eligibility == BountyEligibility.VictoryConfirmed;
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        string? reason = paid is not null ? TargetBountyReasons.AlreadyPaid
            : !eligible ? BountyPaymentReasons.NoVerifiedVictory
            : ship is not { IsDocked: true } || ship.DockedStationObjectId != order.IssuerStationId ? BountyPaymentReasons.WrongStation : null;
        return new BountyPaymentEntry(order.Id, order.IssuerStationId, order.TargetGroupId, bounty.VictoryBattleId,
            paid is not null ? BountyPaymentStates.Paid : eligible ? BountyPaymentStates.Claimable : BountyPaymentStates.Unavailable,
            eligible ? order.RewardTokens : 0, paid?.RewardTokens ?? 0, paid?.PostingId, paid?.PaidAtGameTimeMs, reason);
    }).ToImmutableArray());

    private void PrepareBountyPayment(ClaimBountyCommand command, ref DemoCampaignData candidate,
        ref string? reason, ref Action? commit)
    {
        var order = candidate.BountyOrders.SingleOrDefault(b => b.Id == command.BountyId);
        if (order is null) { reason = TargetBountyReasons.NotFound; return; }
        if (order.PaidPostingId is not null || candidate.BountyPayments.Any(p => p.BountyId == order.Id))
        { reason = TargetBountyReasons.AlreadyPaid; return; }
        var bounty = ProjectBounty(candidate, order);
        if (bounty.Eligibility != BountyEligibility.VictoryConfirmed)
        { reason = BountyPaymentReasons.NoVerifiedVictory; return; }
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is not { IsDocked: true } || ship.DockedStationObjectId != order.IssuerStationId)
        { reason = BountyPaymentReasons.WrongStation; return; }
        if (PlayerCredits > long.MaxValue - order.RewardTokens)
        { reason = BountyPaymentReasons.CreditOverflow; return; }
        long credits = checked(PlayerCredits + order.RewardTokens);
        var payment = new BountyPaymentData(order.Id, order.IssuerStationId, order.TargetGroupId,
            bounty.VictoryBattleId!, order.RewardTokens, "bounty:" + order.Id, _clock.GameTimeMs);
        candidate = candidate with
        {
            BountyOrders = candidate.BountyOrders.Replace(order, order with { PaidPostingId = payment.PostingId, Tracking = false }),
            BountyPayments = candidate.BountyPayments.Add(payment)
        };
        commit = () => PlayerCredits = credits;
    }
}
