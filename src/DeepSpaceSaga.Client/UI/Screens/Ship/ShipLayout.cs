namespace DeepSpaceSaga.Client.UI.Screens.Ship;

public enum ShipButton
{
    None,
    Close
}

/// <summary>
/// Layout and hit-test geometry for the Ship overlay panel. 1400×800 — the
/// standard panel size for gameplay-mechanic windows (DB/Documentation/02-FirstRelease/Screens/
/// ScreenCatalog.md), same as Finance.
/// </summary>
public sealed class ShipLayout
{
    public const float PanelWidth = 1400f;
    public const float PanelHeight = 800f;

    public const float TitleY = 50f;
    public const float BodyStartY = 100f;
    public const float BodyLineHeight = 28f;

    public const float CloseButtonSize = 28f;
    public const float CloseButtonMargin = 14f;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    private const float SectionGap = 20f;
    private const float SectionTop = 90f;
    private const float SectionBottom = PanelHeight - 20f;
    private const float HalfWidth = PanelWidth / 2f;

    /// <summary>Wing panel rect, local to the panel (add PanelLeft/PanelTop for screen space).</summary>
    public static (float Left, float Top, float Right, float Bottom) WingPanelLocalRect() =>
        (SectionGap, SectionTop, HalfWidth - SectionGap, SectionBottom);

    /// <summary>Crew panel rect, local to the panel (add PanelLeft/PanelTop for screen space).</summary>
    public static (float Left, float Top, float Right, float Bottom) CrewPanelLocalRect() =>
        (HalfWidth + SectionGap, SectionTop, PanelWidth - SectionGap, SectionBottom);

    /// <summary>True when a panel-local rect stays inside the panel bounds (used at 1280×720 and 1920×1080).</summary>
    public static bool FitsInsidePanel((float Left, float Top, float Right, float Bottom) rect) =>
        rect.Left >= 0 && rect.Top >= 0 && rect.Right <= PanelWidth && rect.Bottom <= PanelHeight;

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

    public static ShipButton HitTest(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        var (left, top, right, bottom) = CloseButtonLocalRect();
        if (lx >= left && lx <= right && ly >= top && ly <= bottom)
            return ShipButton.Close;

        return ShipButton.None;
    }
}
