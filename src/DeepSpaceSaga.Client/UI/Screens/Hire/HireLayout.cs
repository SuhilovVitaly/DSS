namespace DeepSpaceSaga.Client.UI.Screens.Hire;

/// <summary>
/// Layout and hit-test geometry for the Hire overlay panel. 1400×800 is the standard
/// panel size for gameplay-mechanic windows (Docs/FirstRelease/Screens/ScreenCatalog.md).
/// Structural twin of <see cref="Trade.TradeLayout"/>/<see cref="Station.StationLayout"/>.
/// The panel has no buttons of its own — closing goes through the shared
/// StationToolbar's exit-button icon (see HireScreen), not a per-panel hit-test enum.
/// </summary>
public sealed class HireLayout
{
    public const float PanelWidth = 1400f;
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
