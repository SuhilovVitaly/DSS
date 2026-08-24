namespace DeepSpaceSaga.Client.UI.Screens.Trade;

/// <summary>Which zone of the Trade overlay a click landed on.</summary>
public enum TradeButton
{
    None,
    Close,
    Buy,
    Sell,
    Refuel,
    Cancel,
    QuantityMinus,
    QuantityPlus,
    RefuelQuantityMinus,
    RefuelQuantityPlus,
}

/// <summary>
/// Layout and hit-test geometry for the Trade overlay panel (Docs/FirstRelease/Screens/
/// Trade.md), styled after the user's three-column reference screenshot but adapted to the
/// project's real trade model (Energy Cells/Fuel/Ice — see
/// Docs/FirstRelease/Mechanics/StationInventory.md), not the screenshot's goods/prices.
/// 1400×900 is the standard panel size for gameplay-mechanic windows.
///
/// Structure (top to bottom): header (title + station/docked subtitle + close), a stats row
/// (CREDITS/CARGO/FUEL), three side-by-side columns (STATION INVENTORY / TRANSACTION /
/// YOUR CARGO+FUEL — the third column is itself split into a cargo list on top and a fuel
/// sub-panel below, since Fuel is not cargo — Trade.md), and a bottom account summary row
/// with a CANCEL button. Pure geometry — no SKCanvas/SKPaint dependency — following the
/// same pattern as <see cref="ScenarioSelect.ScenarioSelectLayout"/>.
/// </summary>
public static class TradeLayout
{
    public const float PanelWidth = 1400f;
    public const float PanelHeight = 900f;

    // ── Header ──────────────────────────────────────────────────────────────────
    public const float HeaderLeftX = 40f;
    public const float TitleBaselineY = 46f;
    public const float SubtitleBaselineY = 74f;

    // ── Stats row (CREDITS / CARGO / FUEL) ─────────────────────────────────────
    public const float ContentMargin = 40f;
    public const float StatsRowY = 96f;
    public const float StatsRowHeight = 50f;
    public const float StatsBlockGap = 20f;

    public static float StatsBlockWidth => (PanelWidth - 2 * ContentMargin - 2 * StatsBlockGap) / 3f;
    public static float CreditsStatX => ContentMargin;
    public static float CargoStatX => CreditsStatX + StatsBlockWidth + StatsBlockGap;
    public static float FuelStatX => CargoStatX + StatsBlockWidth + StatsBlockGap;

    // ── Three columns ───────────────────────────────────────────────────────────
    public const float ColumnsTopY = 170f;
    public const float ColumnsHeight = 560f;
    public const float ColumnGap = 20f;

    public static float ColumnWidth => (PanelWidth - 2 * ContentMargin - 2 * ColumnGap) / 3f;
    public static float StationColumnX => ContentMargin;
    public static float TransactionColumnX => StationColumnX + ColumnWidth + ColumnGap;
    public static float CargoColumnX => TransactionColumnX + ColumnWidth + ColumnGap;

    public const float ColumnPadding = 16f;
    public const float ColumnTitleBaselineY = ColumnPadding + 16f;

    // ── STATION INVENTORY column: selectable rows ──────────────────────────────
    public const float InventoryRowsTopY = ColumnTitleBaselineY + 24f;
    public const float InventoryRowHeight = 70f;
    public const float InventoryRowSpacing = 8f;

    // ── TRANSACTION column ──────────────────────────────────────────────────────
    public const float TransactionItemNameBaselineY = ColumnTitleBaselineY + 44f;
    public const float TransactionUnitPriceBaselineY = TransactionItemNameBaselineY + 28f;
    public const float TransactionStepperY = TransactionUnitPriceBaselineY + 20f;
    public const float TransactionStepperHeight = 44f;
    public const float TransactionTotalBaselineY = TransactionStepperY + TransactionStepperHeight + 34f;
    public const float TransactionButtonsY = TransactionTotalBaselineY + 26f;
    public const float TransactionButtonHeight = 44f;
    public const float TransactionButtonGap = 20f;

    // ── YOUR CARGO / FUEL column split ─────────────────────────────────────────
    public const float CargoListHeight = 340f;
    public const float CargoFuelGap = 20f;
    public static float FuelPanelY => ColumnsTopY + CargoListHeight + CargoFuelGap;
    public static float FuelPanelHeight => ColumnsHeight - CargoListHeight - CargoFuelGap;

    public const float CargoRowsTopY = ColumnTitleBaselineY + 20f;
    public const float CargoRowHeight = 40f;
    public const float CargoRowSpacing = 6f;

    /// <summary>FUEL sub-panel internals, local to the fuel panel's own top-left.</summary>
    public const float FuelLabelBaselineY = 30f;
    public const float FuelValueBaselineY = 58f;
    public const float FuelStepperLocalY = 80f;
    public const float FuelButtonLocalY = FuelStepperLocalY + TransactionStepperHeight + 16f;

    // ── Bottom account summary ──────────────────────────────────────────────────
    public const float SummaryGap = 20f;
    public static float SummaryY => ColumnsTopY + ColumnsHeight + SummaryGap;
    public const float SummaryHeight = 110f;
    public const float SummaryPadding = 16f;
    public const float SummaryTitleBaselineY = SummaryPadding + 16f;
    public const float SummaryValuesBaselineY = SummaryPadding + 60f;

    public const float CancelButtonWidth = 140f;
    public const float CancelButtonHeight = 44f;
    public const float CancelButtonMargin = 20f;

    /// <summary>Exit (leave Trade) button — a normal bottom-row button, same size as Cancel,
    /// placed directly to its left in the summary row (see Exit_button_click_returns_CloseTrade).</summary>
    public const float ExitButtonWidth = 140f;
    public const float ExitButtonGap = 20f;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    // ── Column rects, local to the panel ───────────────────────────────────────
    public static (float X, float Y, float W, float H) StationColumnRect() => (StationColumnX, ColumnsTopY, ColumnWidth, ColumnsHeight);
    public static (float X, float Y, float W, float H) TransactionColumnRect() => (TransactionColumnX, ColumnsTopY, ColumnWidth, ColumnsHeight);
    public static (float X, float Y, float W, float H) CargoListRect() => (CargoColumnX, ColumnsTopY, ColumnWidth, CargoListHeight);
    public static (float X, float Y, float W, float H) FuelPanelRect() => (CargoColumnX, FuelPanelY, ColumnWidth, FuelPanelHeight);

    // ── Stats blocks, local to the panel ───────────────────────────────────────
    public static (float X, float Y, float W, float H) CreditsStatRect() => (CreditsStatX, StatsRowY, StatsBlockWidth, StatsRowHeight);
    public static (float X, float Y, float W, float H) CargoStatRect() => (CargoStatX, StatsRowY, StatsBlockWidth, StatsRowHeight);
    public static (float X, float Y, float W, float H) FuelStatRect() => (FuelStatX, StatsRowY, StatsBlockWidth, StatsRowHeight);

    // ── STATION INVENTORY rows, local to the panel ─────────────────────────────
    public static (float X, float Y, float W, float H) InventoryRowRect(int index)
    {
        float y = ColumnsTopY + InventoryRowsTopY + index * (InventoryRowHeight + InventoryRowSpacing);
        return (StationColumnX, y, ColumnWidth, InventoryRowHeight);
    }

    /// <summary>Returns the row index (0-based) hit by a click, or -1 if none.</summary>
    public static int HitTestInventoryRow(float screenX, float screenY, int screenWidth, int screenHeight, int itemCount)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);
        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        for (int i = 0; i < itemCount; i++)
        {
            if (IsInRect(lx, ly, InventoryRowRect(i)))
                return i;
        }

        return -1;
    }

    // ── YOUR CARGO rows, local to the panel ────────────────────────────────────
    public static (float X, float Y, float W, float H) CargoRowRect(int index)
    {
        float y = ColumnsTopY + CargoRowsTopY + index * (CargoRowHeight + CargoRowSpacing);
        return (CargoColumnX, y, ColumnWidth, CargoRowHeight);
    }

    /// <summary>Returns the cargo row index (0-based) hit by a click, or -1 if none.</summary>
    public static int HitTestCargoRow(float screenX, float screenY, int screenWidth, int screenHeight, int cargoStackCount)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);
        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        for (int i = 0; i < cargoStackCount; i++)
        {
            if (IsInRect(lx, ly, CargoRowRect(i)))
                return i;
        }

        return -1;
    }

    // ── TRANSACTION column controls, local to the panel ────────────────────────
    public static (float X, float Y, float W, float H) QuantityStepperRect() =>
        (TransactionColumnX + ColumnPadding, ColumnsTopY + TransactionStepperY, ColumnWidth - 2 * ColumnPadding, TransactionStepperHeight);

    public static (float X, float Y, float W, float H) BuySellButtonsRect() =>
        (TransactionColumnX + ColumnPadding, ColumnsTopY + TransactionButtonsY, ColumnWidth - 2 * ColumnPadding, TransactionButtonHeight);

    public static (float X, float Y, float W, float H) BuyButtonRect()
    {
        var row = BuySellButtonsRect();
        float w = (row.W - TransactionButtonGap) / 2f;
        return (row.X, row.Y, w, row.H);
    }

    public static (float X, float Y, float W, float H) SellButtonRect()
    {
        var row = BuySellButtonsRect();
        float w = (row.W - TransactionButtonGap) / 2f;
        return (row.X + w + TransactionButtonGap, row.Y, w, row.H);
    }

    // ── FUEL sub-panel controls, local to the panel ─────────────────────────────
    public static (float X, float Y, float W, float H) RefuelQuantityStepperRect() =>
        (CargoColumnX + ColumnPadding, FuelPanelY + FuelStepperLocalY, ColumnWidth - 2 * ColumnPadding, TransactionStepperHeight);

    public static (float X, float Y, float W, float H) RefuelButtonRect() =>
        (CargoColumnX + ColumnPadding, FuelPanelY + FuelButtonLocalY, ColumnWidth - 2 * ColumnPadding, TransactionButtonHeight);

    // ── Bottom account summary, local to the panel ──────────────────────────────
    public static (float X, float Y, float W, float H) SummaryRect() => (ContentMargin, SummaryY, PanelWidth - 2 * ContentMargin, SummaryHeight);

    public static (float X, float Y, float W, float H) CancelButtonRect()
    {
        var summary = SummaryRect();
        float y = summary.Y + (summary.H - CancelButtonHeight) / 2f;
        float x = summary.X + summary.W - CancelButtonMargin - CancelButtonWidth;
        return (x, y, CancelButtonWidth, CancelButtonHeight);
    }

    /// <summary>Exit button rect — directly left of Cancel, same row/height.</summary>
    public static (float X, float Y, float W, float H) ExitButtonRect()
    {
        var cancel = CancelButtonRect();
        float x = cancel.X - ExitButtonGap - ExitButtonWidth;
        return (x, cancel.Y, ExitButtonWidth, cancel.H);
    }

    /// <summary>True when (screenX, screenY) lands inside the panel rect (screen space).</summary>
    public static bool IsInsidePanel(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        return screenX >= panelLeft && screenX <= panelLeft + PanelWidth
            && screenY >= panelTop && screenY <= panelTop + PanelHeight;
    }

    /// <summary>
    /// Hit-tests everything except inventory/cargo rows — those have variable count and
    /// are resolved separately via <see cref="HitTestInventoryRow"/>/<see cref="HitTestCargoRow"/>
    /// (mirrors <see cref="ScenarioSelect.ScenarioSelectLayout.HitTest"/>'s row/non-row split).
    /// </summary>
    public static TradeButton HitTest(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        if (IsInRect(lx, ly, ExitButtonRect()))
            return TradeButton.Close;

        var stepper = QuantityStepperRect();
        if (IsInRect(lx, ly, (stepper.X, stepper.Y, Controls.QuantityStepper.ButtonWidth, stepper.H)))
            return TradeButton.QuantityMinus;
        if (IsInRect(lx, ly, (stepper.X + stepper.W - Controls.QuantityStepper.ButtonWidth, stepper.Y, Controls.QuantityStepper.ButtonWidth, stepper.H)))
            return TradeButton.QuantityPlus;

        if (IsInRect(lx, ly, BuyButtonRect()))
            return TradeButton.Buy;
        if (IsInRect(lx, ly, SellButtonRect()))
            return TradeButton.Sell;

        var refuelStepper = RefuelQuantityStepperRect();
        if (IsInRect(lx, ly, (refuelStepper.X, refuelStepper.Y, Controls.QuantityStepper.ButtonWidth, refuelStepper.H)))
            return TradeButton.RefuelQuantityMinus;
        if (IsInRect(lx, ly, (refuelStepper.X + refuelStepper.W - Controls.QuantityStepper.ButtonWidth, refuelStepper.Y, Controls.QuantityStepper.ButtonWidth, refuelStepper.H)))
            return TradeButton.RefuelQuantityPlus;

        if (IsInRect(lx, ly, RefuelButtonRect()))
            return TradeButton.Refuel;

        if (IsInRect(lx, ly, CancelButtonRect()))
            return TradeButton.Cancel;

        return TradeButton.None;
    }

    private static bool IsInRect(float localX, float localY, (float X, float Y, float W, float H) rect) =>
        localX >= rect.X && localX <= rect.X + rect.W
        && localY >= rect.Y && localY <= rect.Y + rect.H;
}
