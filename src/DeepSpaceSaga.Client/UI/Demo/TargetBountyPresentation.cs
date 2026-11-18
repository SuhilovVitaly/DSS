using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Demo;

public sealed record TargetBountyRow(BountyOrder Order, string Headline, string Terms, string Status,
    string PaymentLocation, bool CanToggle, string ToggleLabel);

public static class TargetBountyPresentation
{
    public static ImmutableArray<TargetBountyRow> Build(TargetBountySnapshot? snapshot, string? dockedStation,
        Func<string, string>? stationName = null)
    {
        stationName ??= id => id;
        if (snapshot is null || snapshot.Orders.IsDefault) return [];
        return snapshot.Orders.Where(b => b.IssuerStationId == dockedStation || b.Tracking ||
            b.Eligibility is BountyEligibility.VictoryConfirmed or BountyEligibility.Paid).Select(b => new TargetBountyRow(b,
                string.Format(Localization.Get("bounty.row"), b.TargetGroupId, b.RewardTokens, stationName(b.IssuerStationId)),
                Localization.Get("bounty.no_deadline") + " · " + Localization.Get("bounty.no_deposit"),
                Localization.Get("bounty." + (b.Eligibility == BountyEligibility.Paid ? "already_paid" : b.Eligibility)),
                string.Format(Localization.Get("bounty.claim_at_issuer"), stationName(b.IssuerStationId)),
                b.Tracking || b.Eligibility == BountyEligibility.TargetAvailable,
                Localization.Get(b.Tracking ? "bounty.untrack" : "bounty.track"))).ToImmutableArray();
    }
}
