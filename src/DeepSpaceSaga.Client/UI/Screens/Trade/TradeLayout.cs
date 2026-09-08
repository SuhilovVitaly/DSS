namespace DeepSpaceSaga.Client.UI.Screens.Trade;

/// <summary>
/// Layout and hit-test geometry for the Trade overlay panel. 1600×800 — widened by 200px
/// from the 1400×800 standard for gameplay-mechanic windows (Docs/FirstRelease/Screens/
/// ScreenCatalog.md) to match every other station-hub window (<see cref="Station.StationLayout"/>,
/// <see cref="Hire.HireLayout"/>, <see cref="Contracts.ContractsLayout"/>,
/// <see cref="Finance.FinanceLayout"/>), reserving the extra 200px as a strip at the panel's
/// left edge for the ship-compartment illustration drawn beside the Resources/Goods/Modules
/// grids (see TradeScreen's <c>_compartmentImage</c>/<c>GridPanelOriginX</c>). Structural
/// twin of those layouts otherwise. The panel has no buttons of its own — closing goes
/// through the shared StationToolbar's exit-button icon (see TradeScreen), not a per-panel
/// hit-test enum. Placeholder shell pending redesign (Docs/FirstRelease/Screens/Trade.md).
/// </summary>
public sealed class TradeLayout
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
