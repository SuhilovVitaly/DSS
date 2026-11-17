using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Demo;

public static class StationRepairPresentation
{
    public static string Reason(string? reason)
    {
        string key = "Demo.Repair." + (reason ?? "unavailable");
        string text = Localization.Get(key);
        return text == key ? Localization.Get("Demo.Repair.unavailable") : text;
    }
    public static string Summary(StationRepairSnapshot? state) => state?.Quote is { } quote
        ? string.Format(Localization.Get("Demo.Repair.Quote"), quote.TargetId, quote.Total)
        : state?.LastResult is { } result ? string.Format(Localization.Get("Demo.Repair.Confirmed"), result.Paid)
        : Localization.Get("Demo.Repair.Empty");
}
