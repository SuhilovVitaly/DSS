using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Demo;

/// <summary>
/// Text for the authoritative passenger capacity (EP-0034 US-0001). Only formats engine numbers and
/// reason codes; it never computes cabins, rations or availability itself.
/// </summary>
public static class PassengerCapacityPresentation
{
    private const string Prefix = "Demo.Passengers.";

    /// <summary>Title, cabin split and availability line; an explicit empty state when no capacity exists.</summary>
    public static ImmutableArray<string> Lines(PassengerCapacitySnapshot? capacity)
    {
        string title = Localization.Get(Prefix + "Title");
        if (capacity is null)
            return [title, Localization.Get(Prefix + "Empty")];
        string summary = string.Format(Localization.Get(Prefix + "Summary"), capacity.TotalCabins, capacity.CrewCabins,
            capacity.PassengerCabins, capacity.ReservedCabins, capacity.FreeCabins, capacity.PrisonerCabins);
        string? blocker = PassengerCapacityReasons.BoardingBlocker(capacity);
        string status = blocker is null
            ? string.Format(Localization.Get(Prefix + "Ready"), capacity.FreeCabins)
            : Reason(blocker);
        return [title, summary, status];
    }

    /// <summary>Localized explanation of a stable reason code; unknown codes get a generic text, never the raw code.</summary>
    public static string Reason(string? code)
    {
        string key = Prefix + code;
        string localized = code is null ? key : Localization.Get(key);
        return localized == key ? Localization.Get(Prefix + "Blocked") : localized;
    }
}
