namespace DeepSpaceSaga.Client.UI.Screens.Station;

public enum StationButton
{
    None,
    Close,
    Trade,
    Hire
}

/// <summary>
/// Layout and hit-test geometry for the Station overlay panel. 1400×900 is the
/// standard panel size for gameplay-mechanic windows (Docs/FirstRelease/Screens/
/// ScreenCatalog.md — Station, Trade, Hire, Cargo, Loot, Ship, Character
/// Communication, Dialog, Finance). Structural twin of <see cref="Finance.FinanceLayout"/>.
/// </summary>
public sealed class StationLayout
{
    public const float PanelWidth = 1400f;
    public const float PanelHeight = 900f;

    public const float TitleY = 50f;
    public const float BodyStartY = 100f;
    public const float BodyLineHeight = 28f;

    public const float CloseButtonSize = 28f;
    public const float CloseButtonMargin = 14f;

    public const float TradeButtonWidth = 160f;
    public const float TradeButtonHeight = 32f;

    public const float HireButtonWidth = 160f;
    public const float HireButtonHeight = 32f;

    /// <summary>Body row index of each real button, matching Station.md's "Минимальные
    /// кнопки" order (`Trade`, `Finance`, `Representatives`, `Install Drilling Unit`,
    /// `Hire`, `Undock`) — the remaining placeholder lines keep their row regardless of
    /// which entries have since become real buttons.</summary>
    private const int HireRowIndex = 4;

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

    /// <summary>
    /// TRADE button rect, local to the panel — occupies the first body row (the other
    /// placeholder lines start one row below it). Centered horizontally like the
    /// placeholder text it replaces (Docs/FirstRelease/Screens/Station.md: "Позволяет
    /// открыть экран торговли кнопкой Trade").
    /// </summary>
    public static (float Left, float Top, float Right, float Bottom) TradeButtonLocalRect()
    {
        float left = PanelWidth / 2f - TradeButtonWidth / 2f;
        float top = BodyStartY - 20f;
        return (left, top, left + TradeButtonWidth, top + TradeButtonHeight);
    }

    /// <summary>
    /// HIRE button rect, local to the panel — occupies the row `Hire`'s placeholder text
    /// used to sit in (Docs/FirstRelease/Screens/Station.md: "Позволяет открыть окно
    /// `Hire` для пассажирских контрактов"), same styling/centering as
    /// <see cref="TradeButtonLocalRect"/>.
    /// </summary>
    public static (float Left, float Top, float Right, float Bottom) HireButtonLocalRect()
    {
        float left = PanelWidth / 2f - HireButtonWidth / 2f;
        float top = BodyStartY + HireRowIndex * BodyLineHeight - 20f;
        return (left, top, left + HireButtonWidth, top + HireButtonHeight);
    }

    /// <summary>True when (screenX, screenY) lands inside the panel rect (screen space).</summary>
    public static bool IsInsidePanel(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        return screenX >= panelLeft && screenX <= panelLeft + PanelWidth
            && screenY >= panelTop && screenY <= panelTop + PanelHeight;
    }

    public static StationButton HitTest(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        var (closeLeft, closeTop, closeRight, closeBottom) = CloseButtonLocalRect();
        if (lx >= closeLeft && lx <= closeRight && ly >= closeTop && ly <= closeBottom)
            return StationButton.Close;

        var (tradeLeft, tradeTop, tradeRight, tradeBottom) = TradeButtonLocalRect();
        if (lx >= tradeLeft && lx <= tradeRight && ly >= tradeTop && ly <= tradeBottom)
            return StationButton.Trade;

        var (hireLeft, hireTop, hireRight, hireBottom) = HireButtonLocalRect();
        if (lx >= hireLeft && lx <= hireRight && ly >= hireTop && ly <= hireBottom)
            return StationButton.Hire;

        return StationButton.None;
    }
}
