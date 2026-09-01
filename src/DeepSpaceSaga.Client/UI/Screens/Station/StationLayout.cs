namespace DeepSpaceSaga.Client.UI.Screens.Station;

public enum StationButton
{
    None,
    Trade,
    Hire,
    Finance,
    Contracts
}

/// <summary>
/// Layout and hit-test geometry for the Station overlay panel. 1400×800 is the
/// standard panel size for gameplay-mechanic windows (Docs/FirstRelease/Screens/
/// ScreenCatalog.md — Station, Trade, Hire, Contracts, Cargo, Loot, Ship, Character
/// Communication, Dialog, Finance). Structural twin of <see cref="Finance.FinanceLayout"/>.
/// </summary>
public sealed class StationLayout
{
    public const float PanelWidth = 1400f;
    public const float PanelHeight = 800f;

    public const float BodyStartY = 100f;
    public const float BodyLineHeight = 28f;

    // 24 px tall, not the 28 px BodyLineHeight — real buttons on adjacent rows (Trade
    // row 0 / Finance row 1) must not overlap under the shared "-20" vertical offset
    // below; a height equal to or greater than BodyLineHeight would collide there
    // (caught by StationScreenTests.Trade_Hire_and_Finance_buttons_do_not_overlap).
    public const float TradeButtonWidth = 160f;
    public const float TradeButtonHeight = 24f;

    public const float HireButtonWidth = 160f;
    public const float HireButtonHeight = 24f;

    public const float FinanceButtonWidth = 160f;
    public const float FinanceButtonHeight = 24f;

    public const float ContractsButtonWidth = 160f;
    public const float ContractsButtonHeight = 24f;

    /// <summary>Body row index of each real button, matching Station.md's "Минимальные
    /// кнопки" order (`Trade`, `Finance`, `Representatives`, `Install Drilling Unit`,
    /// `Hire`, `Undock`) — the remaining placeholder lines keep their row regardless of
    /// which entries have since become real buttons.</summary>
    private const int HireRowIndex = 4;

    private const int FinanceRowIndex = 1;

    /// <summary>
    /// `Contracts` was split out of `Hire` (passenger contracts vs. crew hiring) after
    /// the original six-row layout was already fixed — rather than renumber Undock (row
    /// 5) and every reference to it, Contracts gets its own new row appended at the end.
    /// </summary>
    private const int ContractsRowIndex = 6;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

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

    /// <summary>
    /// FINANCE button rect, local to the panel — occupies the row `Finance`'s placeholder
    /// text used to sit in (Docs/FirstRelease/Screens/Station.md: "Позволяет открыть
    /// финансовую сводку/операции кнопкой `Finance`"), same styling/centering as
    /// <see cref="TradeButtonLocalRect"/>/<see cref="HireButtonLocalRect"/>. Unlike Trade
    /// and Hire, this opens the pre-existing <see cref="Finance.FinanceScreen"/> (already
    /// reachable from GameSessionScreen's Mechanics panel / Ctrl+F) rather than a new
    /// screen class.
    /// </summary>
    public static (float Left, float Top, float Right, float Bottom) FinanceButtonLocalRect()
    {
        float left = PanelWidth / 2f - FinanceButtonWidth / 2f;
        float top = BodyStartY + FinanceRowIndex * BodyLineHeight - 20f;
        return (left, top, left + FinanceButtonWidth, top + FinanceButtonHeight);
    }

    /// <summary>
    /// CONTRACTS button rect, local to the panel — a new row appended after `Undock`
    /// (row 6), not a converted placeholder line: `Contracts` was split out of `Hire`
    /// (Docs/FirstRelease/Screens/Contracts.md — passenger contracts, separate from
    /// `Hire`'s now crew-hiring-specific scope) after the original six-row layout was
    /// already fixed. Same styling/centering as the other row buttons.
    /// </summary>
    public static (float Left, float Top, float Right, float Bottom) ContractsButtonLocalRect()
    {
        float left = PanelWidth / 2f - ContractsButtonWidth / 2f;
        float top = BodyStartY + ContractsRowIndex * BodyLineHeight - 20f;
        return (left, top, left + ContractsButtonWidth, top + ContractsButtonHeight);
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

        var (tradeLeft, tradeTop, tradeRight, tradeBottom) = TradeButtonLocalRect();
        if (lx >= tradeLeft && lx <= tradeRight && ly >= tradeTop && ly <= tradeBottom)
            return StationButton.Trade;

        var (hireLeft, hireTop, hireRight, hireBottom) = HireButtonLocalRect();
        if (lx >= hireLeft && lx <= hireRight && ly >= hireTop && ly <= hireBottom)
            return StationButton.Hire;

        var (financeLeft, financeTop, financeRight, financeBottom) = FinanceButtonLocalRect();
        if (lx >= financeLeft && lx <= financeRight && ly >= financeTop && ly <= financeBottom)
            return StationButton.Finance;

        var (contractsLeft, contractsTop, contractsRight, contractsBottom) = ContractsButtonLocalRect();
        if (lx >= contractsLeft && lx <= contractsRight && ly >= contractsTop && ly <= contractsBottom)
            return StationButton.Contracts;

        return StationButton.None;
    }
}
