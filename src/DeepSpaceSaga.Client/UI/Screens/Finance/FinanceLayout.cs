namespace DeepSpaceSaga.Client.UI.Screens.Finance;

/// <summary>
/// Layout and hit-test geometry for the Finance overlay panel. 1600×800 — widened by
/// 200px from the 1400×800 standard for gameplay-mechanic windows (Docs/FirstRelease/
/// Screens/ScreenCatalog.md — Station, Trade, Hire, Cargo, Loot, Ship, Character
/// Communication, Dialog, Finance) to match every other station-hub window
/// (<see cref="Station.StationLayout"/>, <see cref="Trade.TradeLayout"/>,
/// <see cref="Hire.HireLayout"/>, <see cref="Contracts.ContractsLayout"/>), reserving
/// room for a left-side illustration consistent with <see cref="Trade.TradeScreen"/>'s —
/// distinct from the smaller meta/menu screens (Settings, GameMenu, Save, Load). The
/// panel has no buttons of its own — closing goes through the shared StationToolbar's
/// exit-button icon (see FinanceScreen), not a per-panel hit-test enum.
/// </summary>
public sealed class FinanceLayout
{
    public const float PanelWidth = 1600f;
    public const float PanelHeight = 800f;

    public const float BodyStartY = 100f;
    public const float BodyLineHeight = 28f;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    /// <summary>True when (screenX, screenY) lands inside the panel rect (screen space).</summary>
    public static bool IsInsidePanel(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        return screenX >= panelLeft && screenX <= panelLeft + PanelWidth
            && screenY >= panelTop && screenY <= panelTop + PanelHeight;
    }
}
