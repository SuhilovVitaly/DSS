using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Demo;

public static class CampaignRoundtripPresentation
{
    public static string Summary(CampaignRoundtripSnapshot? state) => state is null
        ? Localization.Get("Demo.Roundtrip.Empty")
        : string.Format(Localization.Get("Demo.Roundtrip.Summary"), state.LostDroneIds.Length, state.ConsumedTorpedoes);

    public static string Guidance(CampaignRoundtripSnapshot? state) => state is null ? ""
        : Localization.Get(state.CarrierDestroyed ? "Demo.Roundtrip.Terminal" : "Demo.Roundtrip.Continue");

    public static string RearmReason(string? reason)
    {
        string key = "Demo.Rearm." + reason;
        string localized = Localization.Get(key);
        return localized == key ? Localization.Get("Demo.Rearm.Rejected") : localized;
    }
}
