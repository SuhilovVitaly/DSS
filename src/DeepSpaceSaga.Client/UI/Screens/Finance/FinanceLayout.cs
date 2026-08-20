namespace DeepSpaceSaga.Client.UI.Screens.Finance;

public enum FinanceButton
{
    None,
    Close
}

/// <summary>
/// Layout and hit-test geometry for the Finance overlay panel. 1700×1200 is the
/// standard panel size for gameplay-mechanic windows (Docs/FirstRelease/Screens/
/// ScreenCatalog.md — Station, Trade, Hire, Cargo, Loot, Ship, Character
/// Communication, Dialog, Finance), distinct from the smaller meta/menu screens
/// (Settings, GameMenu, Save, Load).
/// </summary>
public sealed class FinanceLayout
{
    public const float PanelWidth = 1700f;
    public const float PanelHeight = 1200f;

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

    public static FinanceButton HitTest(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        var (left, top, right, bottom) = CloseButtonLocalRect();
        if (lx >= left && lx <= right && ly >= top && ly <= bottom)
            return FinanceButton.Close;

        return FinanceButton.None;
    }
}
