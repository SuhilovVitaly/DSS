namespace DeepSpaceSaga.Client.UI.Screens.Contracts;

public enum ContractsButton
{
    None,
    Close
}

/// <summary>
/// Layout and hit-test geometry for the Contracts overlay panel. 1400×900 is the
/// standard panel size for gameplay-mechanic windows (Docs/FirstRelease/Screens/
/// ScreenCatalog.md). Structural twin of <see cref="Hire.HireLayout"/>/
/// <see cref="Trade.TradeLayout"/>/<see cref="Station.StationLayout"/>.
/// </summary>
public sealed class ContractsLayout
{
    public const float PanelWidth = 1400f;
    public const float PanelHeight = 900f;

    public const float TitleY = 50f;
    public const float BodyStartY = 100f;
    public const float BodyLineHeight = 28f;

    public const float CloseButtonSize = 28f;
    public const float CloseButtonMargin = 14f;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    /// <summary>Close button rect, local to the panel (add PanelLeft/PanelTop for screen space).</summary>
    public static (float Left, float Top, float Right, float Bottom) CloseButtonLocalRect()
    {
        float right = PanelWidth - CloseButtonMargin;
        float left = right - CloseButtonSize;
        float top = CloseButtonMargin;
        float bottom = top + CloseButtonSize;
        return (left, top, right, bottom);
    }

    /// <summary>True when (screenX, screenY) lands inside the panel rect (screen space).</summary>
    public static bool IsInsidePanel(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        return screenX >= panelLeft && screenX <= panelLeft + PanelWidth
            && screenY >= panelTop && screenY <= panelTop + PanelHeight;
    }

    public static ContractsButton HitTest(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        var (left, top, right, bottom) = CloseButtonLocalRect();
        if (lx >= left && lx <= right && ly >= top && ly <= bottom)
            return ContractsButton.Close;

        return ContractsButton.None;
    }
}
