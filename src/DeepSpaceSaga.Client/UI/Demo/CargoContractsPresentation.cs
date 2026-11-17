using System.Collections.Immutable;
using System.Globalization;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Demo;

/// <summary>Everything the Contracts screen draws for the cargo contracts section.</summary>
public sealed record CargoContractsView(string Title, string? Hold, string? NotForSale, ImmutableArray<string> Lines, string? Empty);

/// <summary>
/// Text for the authoritative cargo contracts (EP-0034 US-0004): the hold split (sealed contract cargo, the player's
/// own goods, space left for trade), each contract with item, route, payout, deadline and hold state, and the confirmed
/// sealing, delivery and return results. Only formats engine values; it never decides delivery, payout or reservation.
/// </summary>
public static class CargoContractsPresentation
{
    private const string Prefix = "Demo.CargoContracts.";

    public static CargoContractsView Build(CargoContractsSnapshot? section, Func<string, string>? stationName = null)
    {
        stationName ??= id => id;
        string title = Localization.Get(Prefix + "Title");
        if (section is null)
            return new(title, null, null, [], Localization.Get(Prefix + "Unavailable"));
        var contracts = section.Contracts.IsDefault ? [] : section.Contracts.Where(c => c is not null).ToImmutableArray();
        string? notForSale = section.ReservedMassKg > 0 ? Localization.Get(Prefix + "NotForSale") : null;
        if (contracts.IsEmpty)
            return new(title, Hold(section), notForSale, [], Localization.Get(Prefix + "Empty"));

        var lines = ImmutableArray.CreateBuilder<string>();
        foreach (var contract in contracts)
        {
            lines.Add(Line(contract, stationName));
            if (Result(contract, stationName) is { } result) lines.Add(result);
        }
        return new(title, Hold(section), notForSale, lines.ToImmutable(), null);
    }

    /// <summary>Hold split published by the engine; null without a section (never invented numbers).</summary>
    public static string? Hold(CargoContractsSnapshot? section) => section is null ? null
        : string.Format(Localization.Get(Prefix + "Hold"), Tonnes(section.ReservedMassKg), Tonnes(section.PlayerCargoMassKg),
            Tonnes(section.FreeHoldKg));

    public static string Line(CargoContract contract, Func<string, string> stationName) =>
        string.Format(Localization.Get(Prefix + "Line"), Tonnes(contract.MassKg), ItemName(contract.ItemTypeId),
            stationName(contract.OriginStationId), stationName(contract.DestinationStationId), contract.Payout,
            StationJobsPresentation.Time(contract.DeadlineGameTimeMs), State(contract.HoldState));

    /// <summary>Confirmed delivery or return result; null until the engine reports the receipt.</summary>
    public static string? Result(CargoContract contract, Func<string, string> stationName)
    {
        if (contract.HoldState == CargoHoldStates.Delivered && contract is { PaidCredits: { } paid, DeliveredAtGameTimeMs: { } at })
            return string.Format(Localization.Get(Prefix + "Delivered"), StationJobsPresentation.Time(at), paid);
        if (contract.HoldState == CargoHoldStates.Returned && contract is { ReturnedAtGameTimeMs: { } back, ReturnStationId: { } station })
            return string.Format(Localization.Get(Prefix + "Returned"), stationName(station), StationJobsPresentation.Time(back));
        return null;
    }

    /// <summary>
    /// Confirmed sealing of an accepted offer, from the snapshot (not from the command response); null while the
    /// engine has not published the contract yet or when the offer carried no cargo.
    /// </summary>
    public static string? Sealed(CargoContractsSnapshot? section, string? offerId, Func<string, string>? stationName = null)
    {
        stationName ??= id => id;
        if (section is null || offerId is null || section.Contracts.IsDefault) return null;
        var contract = section.Contracts.FirstOrDefault(c => c is not null && c.OfferId == offerId);
        return contract is null ? null
            : string.Format(Localization.Get(Prefix + "Sealed"), Tonnes(contract.MassKg), ItemName(contract.ItemTypeId),
                stationName(contract.DestinationStationId), StationJobsPresentation.Time(contract.DeadlineGameTimeMs));
    }

    /// <summary>Localized hold state; unknown or missing states read as "not confirmed", never as the raw code.</summary>
    public static string State(string? state) =>
        Localization.Get(Prefix + "State." + (CargoHoldStates.IsKnown(state) ? state : DemoCampaignReasons.Unavailable));

    /// <summary>Trade name of the sealed item; an unknown id reads as "Cargo", never as the persistent id.</summary>
    public static string ItemName(string? itemTypeId)
    {
        string name = string.IsNullOrWhiteSpace(itemTypeId) ? "" : TradeItemPresentation.ItemDisplayName(itemTypeId);
        return name.Length == 0 || name == itemTypeId ? Localization.Get("Demo.Jobs.Type." + StationJobTypes.Cargo) : name;
    }

    private static string Tonnes(long kg) => (Math.Max(0, kg) / 1000m).ToString("0.#", CultureInfo.InvariantCulture);
}
