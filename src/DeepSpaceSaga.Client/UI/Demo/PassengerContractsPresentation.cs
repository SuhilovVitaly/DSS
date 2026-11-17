using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Demo;

/// <summary>Everything the Contracts screen draws for the passenger contracts section.</summary>
public sealed record PassengerContractsView(string Title, string? Summary, ImmutableArray<string> Lines, string? Empty);

/// <summary>
/// Text for the authoritative passenger contracts (EP-0034 US-0002): people aboard, route, payout, deadline, delivery
/// state and the confirmed delivery result. Only formats engine values; it never decides delivery, payout or lateness.
/// </summary>
public static class PassengerContractsPresentation
{
    private const string Prefix = "Demo.PassengerContracts.";

    public static PassengerContractsView Build(PassengerContractsSnapshot? section, Func<string, string>? stationName = null)
    {
        stationName ??= id => id;
        string title = Localization.Get(Prefix + "Title");
        if (section is null)
            return new(title, null, [], Localization.Get(Prefix + "Unavailable"));
        var contracts = section.Contracts.IsDefault ? [] : section.Contracts.Where(c => c is not null).ToImmutableArray();
        if (contracts.IsEmpty)
            return new(title, null, [], Localization.Get(Prefix + "Empty"));

        var lines = ImmutableArray.CreateBuilder<string>();
        foreach (var contract in contracts)
        {
            lines.Add(Line(contract, stationName));
            if (Delivered(contract) is { } delivered) lines.Add(delivered);
        }
        return new(title, string.Format(Localization.Get(Prefix + "Aboard"), Math.Max(0, section.PassengersAboard)), lines.ToImmutable(), null);
    }

    public static string Line(PassengerContract contract, Func<string, string> stationName) =>
        string.Format(Localization.Get(Prefix + "Line"), contract.Count, stationName(contract.OriginStationId),
            stationName(contract.DestinationStationId), contract.Payout, StationJobsPresentation.Time(contract.DeadlineGameTimeMs),
            State(contract.DeliveryState));

    /// <summary>Confirmed delivery result; null until the engine reports the delivery receipt.</summary>
    public static string? Delivered(PassengerContract contract) =>
        contract.DeliveryState == PassengerDeliveryStates.Delivered && contract.PaidCredits is { } paid &&
        contract.DeliveredAtGameTimeMs is { } at
            ? string.Format(Localization.Get(Prefix + "Delivered"), StationJobsPresentation.Time(at), paid)
            : null;

    /// <summary>
    /// Confirmed boarding result of an accepted offer, from the snapshot (not from the command response); null while
    /// the engine has not published the contract yet or when the offer carried no passengers.
    /// </summary>
    public static string? Boarded(PassengerContractsSnapshot? section, string? offerId, Func<string, string>? stationName = null)
    {
        stationName ??= id => id;
        if (section is null || offerId is null || section.Contracts.IsDefault) return null;
        var contract = section.Contracts.FirstOrDefault(c => c is not null && c.OfferId == offerId);
        return contract is null ? null
            : string.Format(Localization.Get(Prefix + "Boarded"), contract.Count, stationName(contract.DestinationStationId),
                StationJobsPresentation.Time(contract.DeadlineGameTimeMs));
    }

    /// <summary>Localized delivery state; unknown or missing states read as "not confirmed", never as the raw code.</summary>
    public static string State(string? state)
    {
        string key = Prefix + "State." + (PassengerDeliveryStates.IsKnown(state) ? state : DemoCampaignReasons.Unavailable);
        return Localization.Get(key);
    }
}
