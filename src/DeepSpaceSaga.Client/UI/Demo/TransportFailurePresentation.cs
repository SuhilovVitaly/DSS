using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Demo;

/// <summary>One active order with its published consequences and the engine's cancellation verdict.</summary>
public sealed record TransportTermsRow(string ContractId, string Headline, string Terms, string? Blocker, bool CanCancel);

/// <summary>Everything the Contracts screen draws for the failures and cancellations section.</summary>
public sealed record TransportFailuresView(
    string Title,
    string? Calendar,
    string? Debt,
    ImmutableArray<TransportTermsRow> Active,
    ImmutableArray<string> Lines,
    string? Empty);

/// <summary>
/// Text for the authoritative transport failures (EP-0034 US-0003): calendar deadline rule, cancellation/expiry
/// terms before the action, outcomes, penalty split, waiting/completed disembarkation, loss and contract debt.
/// Only formats engine values; it never computes a penalty, a deadline or an outcome.
/// </summary>
public static class TransportFailurePresentation
{
    private const string Prefix = "Demo.TransportFailures.";

    public static TransportFailuresView Build(TransportFailuresSnapshot? section, StationJobsSnapshot? jobs,
        Func<string, string>? stationName = null)
    {
        stationName ??= id => id;
        string title = Localization.Get(Prefix + "Title");
        if (section is null)
            return new(title, null, null, [], [], Localization.Get(Prefix + "Unavailable"));
        var obligations = jobs is null || jobs.Obligations.IsDefault ? [] : jobs.Obligations.Where(o => o is not null).ToImmutableArray();

        var active = ImmutableArray.CreateBuilder<TransportTermsRow>();
        foreach (var terms in section.Terms.IsDefault ? [] : section.Terms)
        {
            if (terms is null) continue;
            var obligation = obligations.FirstOrDefault(o => o.ContractId == terms.ContractId);
            active.Add(new TransportTermsRow(terms.ContractId, Describe(obligation, null, stationName),
                string.Format(Localization.Get(Prefix + "Terms"), terms.CancelPenaltyCredits,
                    StationJobsPresentation.Time(terms.DeadlineGameTimeMs), terms.ExpiryPenaltyCredits),
                terms.Blocker is null ? null : Reason(terms.Blocker), terms.Blocker is null));
        }

        var lines = ImmutableArray.CreateBuilder<string>();
        foreach (var failure in section.Failures.IsDefault ? [] : section.Failures)
        {
            if (failure is null || !TransportFailureReasons.IsKnownOutcome(failure.Reason)) continue;
            var obligation = obligations.FirstOrDefault(o => o.ContractId == failure.ContractId);
            lines.Add(string.Format(Localization.Get(Prefix + "Line"), Describe(obligation, failure.Type, stationName),
                Outcome(failure.Reason), failure.PenaltyCredits, failure.PenaltyPaid, failure.RepaidCredits, failure.Debt));
            if (Detail(failure, stationName) is { } detail) lines.Add(detail);
        }

        return new(title, Localization.Get(Prefix + "Calendar"),
            section.ContractDebt > 0 ? string.Format(Localization.Get(Prefix + "Debt"), section.ContractDebt) : null,
            active.ToImmutable(), lines.ToImmutable(), lines.Count == 0 ? Localization.Get(Prefix + "Empty") : null);
    }

    /// <summary>Where the passengers of a failed contract are: waiting aboard, left at a station, or lost.</summary>
    public static string? Detail(TransportFailureSnapshot failure, Func<string, string> stationName)
    {
        int count = failure.PassengerIds.IsDefault ? 0 : failure.PassengerIds.Length;
        if (count == 0) return null;
        if (failure.Reason == TransportFailureReasons.CarrierLost)
            return string.Format(Localization.Get(Prefix + "Lost"), count);
        if (failure.DisembarkPending)
            return string.Format(Localization.Get(Prefix + "DisembarkPending"), count);
        return failure is { DisembarkedAtGameTimeMs: { } at, DisembarkStationId: { } station }
            ? string.Format(Localization.Get(Prefix + "Disembarked"), count, stationName(station), StationJobsPresentation.Time(at))
            : null;
    }

    /// <summary>Confirmed cancellation result, from the published failure (not from the command response).</summary>
    public static string? Cancelled(TransportFailuresSnapshot? section, string? contractId) =>
        TransportFailureReasons.Outcome(section, contractId) is { Reason: TransportFailureReasons.Cancelled } failure
            ? string.Format(Localization.Get(Prefix + "Cancelled"), failure.PenaltyCredits, failure.PenaltyPaid, failure.Debt)
            : null;

    public static string Confirm(long penalty) => string.Format(Localization.Get(Prefix + "CancelConfirm"), penalty);

    public static string Pending => Localization.Get(Prefix + "Pending");
    public static string CancelLabel => Localization.Get(Prefix + "Cancel");

    /// <summary>Localized outcome; an unknown code reads as "not confirmed", never as the raw code.</summary>
    public static string Outcome(string? reason) =>
        TransportFailureReasons.IsKnownOutcome(reason) ? Localization.Get(Prefix + "Outcome." + reason) : Reason(DemoCampaignReasons.Unavailable);

    /// <summary>Localized refusal; unknown codes get the "no confirmed answer" text, never the raw code.</summary>
    public static string Reason(string? code)
    {
        string key = Prefix + "Reason." + code;
        string text = code is null ? key : Localization.Get(key);
        return text == key ? Localization.Get(Prefix + "Reason." + DemoCampaignReasons.Unavailable) : text;
    }

    private static string Describe(JobObligation? obligation, string? type, Func<string, string> stationName)
    {
        string typeKey = "Demo.Jobs.Type." + (obligation?.Type ?? type);
        string typeName = Localization.Get(typeKey);
        if (typeName == typeKey) typeName = Localization.Get(Prefix + "Title");
        return obligation is null ? typeName
            : $"{typeName}: {stationName(obligation.OriginStationId)} → {stationName(obligation.DestinationStationId)}";
    }
}
