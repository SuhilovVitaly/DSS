using System.Collections.Immutable;
using System.Globalization;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Demo;

/// <summary>One offer of the docked station's board as the player sees it.</summary>
public sealed record StationJobOfferRow(string OfferId, string Headline, string Terms, string Payout, string? Blocker, bool CanAccept);

/// <summary>Everything the Contracts screen draws for the board and the journal.</summary>
public sealed record StationJobsView(
    string Title,
    string? Notice,
    ImmutableArray<StationJobOfferRow> Offers,
    string? BoardEmpty,
    string JournalTitle,
    ImmutableArray<string> Journal,
    string? JournalEmpty);

/// <summary>
/// Text for the authoritative station job board and obligations journal (EP-0034 US-0006). Only formats engine
/// numbers, verdicts and reason codes; it never computes payouts, deadlines, capacity or availability itself.
/// </summary>
public static class StationJobsPresentation
{
    private const string Prefix = "Demo.Jobs.";

    /// <summary>Board of the docked station (none in flight) plus the full journal; explicit empty/unavailable states.</summary>
    public static StationJobsView Build(StationJobsSnapshot? board, Func<string, string>? stationName = null)
    {
        stationName ??= id => id;
        string title = Localization.Get(Prefix + "Title");
        string journalTitle = Localization.Get(Prefix + "Journal");
        if (board is null)
            return new(title, Localization.Get(Prefix + "Unavailable"), [], null, journalTitle, [], Localization.Get(Prefix + "JournalEmpty"));

        var offers = board.DockedStationId is not { } docked || board.Offers.IsDefault
            ? ImmutableArray<StationJobOfferRow>.Empty
            : board.Offers.Where(o => o is not null && o.OriginStationId == docked).Select(o =>
            {
                string? blocker = StationJobReasons.AcceptBlocker(board, o.Id);
                return new StationJobOfferRow(o.Id, Headline(o, stationName), Terms(o), Payout(o),
                    blocker is null ? null : Reason(blocker), blocker is null);
            }).ToImmutableArray();
        var journal = board.Obligations.IsDefault
            ? ImmutableArray<string>.Empty
            : board.Obligations.Where(o => o is not null).Select(o => Obligation(o, stationName)).ToImmutableArray();
        return new(title,
            board.DockedStationId is null ? Localization.Get(Prefix + "InFlight") : null,
            offers,
            board.DockedStationId is not null && offers.IsEmpty ? Localization.Get(Prefix + "BoardEmpty") : null,
            journalTitle, journal,
            journal.IsEmpty ? Localization.Get(Prefix + "JournalEmpty") : null);
    }

    public static string Headline(JobOffer offer, Func<string, string> stationName)
    {
        string what = StationJobTypes.CarriesPassengers(offer.Type)
            ? string.Format(Localization.Get(Prefix + "OfferPassengers"), offer.Count, stationName(offer.DestinationStationId))
            : string.Format(Localization.Get(Prefix + "OfferCargo"), Tonnes(offer.MassKg), stationName(offer.DestinationStationId));
        return TypeName(offer.Type) + " · " + what;
    }

    public static string Terms(JobOffer offer) =>
        string.Format(Localization.Get(Prefix + "OfferTerms"), offer.Payout, Duration(offer.DeadlineDurationMs), Time(offer.ExpiresAtGameTimeMs)) +
        " · " + RiskName(offer.RiskBand);

    /// <summary>Explains the engine's payout terms; the total itself is shown in <see cref="Terms"/>.</summary>
    public static string Payout(JobOffer offer) =>
        string.Format(Localization.Get(Prefix + "OfferPayout"), offer.Terms.BaseTokens, offer.Terms.LoadTokens,
            offer.Terms.DistanceTokens, offer.RouteDistanceKm, Factor(offer.Terms.UrgencyPermille), Factor(offer.Terms.RiskPermille));

    public static string Obligation(JobObligation obligation, Func<string, string> stationName) =>
        string.Format(Localization.Get(Prefix + "ObligationLine"), TypeName(obligation.Type), stationName(obligation.OriginStationId),
            stationName(obligation.DestinationStationId), obligation.Payout, Time(obligation.DeadlineGameTimeMs), StatusName(obligation.Status));

    public static string Duration(long ms) =>
        string.Format(Localization.Get(Prefix + "Duration"), Math.Max(0, ms) / GameCalendar.HourMs, Math.Max(0, ms) % GameCalendar.HourMs / 60_000);

    public static string Time(long gameTimeMs)
    {
        var calendar = GameCalendar.FromGameTime(Math.Max(0, gameTimeMs));
        return string.Format(Localization.Get(Prefix + "Time"), calendar.Day,
            calendar.Hour.ToString("00", CultureInfo.InvariantCulture), calendar.Minute.ToString("00", CultureInfo.InvariantCulture));
    }

    /// <summary>Localized explanation of a stable reason code; unknown codes get a generic text, never the raw code.</summary>
    public static string Reason(string? code) => Lookup("Reason." + code, "Rejected");

    public static string Pending => Localization.Get(Prefix + "Pending");
    public static string Accepted => Localization.Get(Prefix + "Accepted");
    public static string AcceptLabel => Localization.Get(Prefix + "Accept");

    private static string TypeName(string type) => Lookup("Type." + type, "Title");
    private static string RiskName(string band) => Lookup("Risk." + band, "Title");
    private static string StatusName(string status) => Lookup("Status." + status, "Journal");

    private static string Lookup(string suffix, string fallback)
    {
        string key = Prefix + suffix;
        string text = Localization.Get(key);
        return text == key ? Localization.Get(Prefix + fallback) : text;
    }

    private static string Factor(int permille) => "×" + (permille / 1000m).ToString("0.###", CultureInfo.InvariantCulture);

    private static string Tonnes(long kg) => (kg / 1000m).ToString("0.#", CultureInfo.InvariantCulture);
}
