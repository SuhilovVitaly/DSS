using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Demo;

public static class CampaignOutcomePresentation
{
    public static string Guidance(CampaignOutcomeSnapshot? outcome) => outcome is null ? "" : Localization.Get(
        outcome.Status == CampaignOutcomeKind.CarrierDestroyed ? "Demo.Outcome.Terminal"
        : outcome.RecoveryReason == "no_ready_wing" ? "Demo.Outcome.NoWing" : "Demo.Outcome.Active");
}
