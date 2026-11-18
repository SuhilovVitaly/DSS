using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Demo;

public sealed record BountyPaymentView(string Amount, bool CanClaim, string? Reason);

public static class BountyPaymentPresentation
{
    public static BountyPaymentView? Build(BountyPaymentSnapshot? snapshot, string bountyId)
    {
        if (snapshot is null || snapshot.Entries.IsDefault) return null;
        var entry = snapshot.Entries.FirstOrDefault(p => p.BountyId == bountyId);
        if (entry is null) return null;
        bool paid = entry.State == BountyPaymentStates.Paid;
        return new(string.Format(Localization.Get(paid ? "bounty.paid" : "bounty.due"), paid ? entry.PaidTokens : entry.DueTokens),
            entry.State == BountyPaymentStates.Claimable && entry.Reason is null,
            entry.Reason is null ? null : Localization.Get("bounty." + entry.Reason));
    }
}
