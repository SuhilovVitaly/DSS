using DeepSpaceSaga.Client.UI.Screens.Trade;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Pure geometry/hit-test checks for <see cref="TradeLayout"/> (Docs/FirstRelease/Screens/
/// Trade.md's three-column layout). Structural twin of ScenarioSelectLayoutTests-style
/// geometry suites elsewhere in this project.
/// </summary>
public class TradeLayoutTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    [Fact]
    public void Three_columns_do_not_intersect()
    {
        var station = TradeLayout.StationColumnRect();
        var transaction = TradeLayout.TransactionColumnRect();
        var cargo = TradeLayout.CargoListRect();

        Assert.True(station.X + station.W <= transaction.X + 0.01f);
        Assert.True(transaction.X + transaction.W <= cargo.X + 0.01f);
        Assert.True(cargo.X + cargo.W <= TradeLayout.PanelWidth - TradeLayout.ContentMargin + 0.01f);
    }

    [Fact]
    public void Cargo_list_and_fuel_panel_do_not_intersect()
    {
        var cargoList = TradeLayout.CargoListRect();
        var fuelPanel = TradeLayout.FuelPanelRect();

        Assert.True(cargoList.Y + cargoList.H <= fuelPanel.Y + 0.01f);
        Assert.Equal(cargoList.X, fuelPanel.X, precision: 3);
        Assert.Equal(cargoList.W, fuelPanel.W, precision: 3);
    }

    [Fact]
    public void Stats_row_blocks_do_not_intersect()
    {
        var credits = TradeLayout.CreditsStatRect();
        var cargo = TradeLayout.CargoStatRect();
        var fuel = TradeLayout.FuelStatRect();

        Assert.True(credits.X + credits.W <= cargo.X + 0.01f);
        Assert.True(cargo.X + cargo.W <= fuel.X + 0.01f);
    }

    [Fact]
    public void Buy_and_sell_buttons_are_side_by_side_within_transaction_column()
    {
        var buy = TradeLayout.BuyButtonRect();
        var sell = TradeLayout.SellButtonRect();
        var column = TradeLayout.TransactionColumnRect();

        Assert.True(buy.X + buy.W <= sell.X + 0.01f);
        Assert.True(buy.X >= column.X);
        Assert.True(sell.X + sell.W <= column.X + column.W + 0.01f);
    }

    [Fact]
    public void Close_button_hit_test_returns_Close()
    {
        var (left, top, right, bottom) = TradeLayout.CloseButtonLocalRect();
        float cx = TradeLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = TradeLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        var hit = TradeLayout.HitTest(cx, cy, ScreenWidth, ScreenHeight);
        Assert.Equal(TradeButton.Close, hit);
    }

    [Fact]
    public void Buy_button_hit_test_returns_Buy()
    {
        var rect = TradeLayout.BuyButtonRect();
        float cx = TradeLayout.PanelLeft(ScreenWidth) + rect.X + rect.W / 2f;
        float cy = TradeLayout.PanelTop(ScreenHeight) + rect.Y + rect.H / 2f;

        var hit = TradeLayout.HitTest(cx, cy, ScreenWidth, ScreenHeight);
        Assert.Equal(TradeButton.Buy, hit);
    }

    [Fact]
    public void Sell_button_hit_test_returns_Sell()
    {
        var rect = TradeLayout.SellButtonRect();
        float cx = TradeLayout.PanelLeft(ScreenWidth) + rect.X + rect.W / 2f;
        float cy = TradeLayout.PanelTop(ScreenHeight) + rect.Y + rect.H / 2f;

        var hit = TradeLayout.HitTest(cx, cy, ScreenWidth, ScreenHeight);
        Assert.Equal(TradeButton.Sell, hit);
    }

    [Fact]
    public void Refuel_button_hit_test_returns_Refuel()
    {
        var rect = TradeLayout.RefuelButtonRect();
        float cx = TradeLayout.PanelLeft(ScreenWidth) + rect.X + rect.W / 2f;
        float cy = TradeLayout.PanelTop(ScreenHeight) + rect.Y + rect.H / 2f;

        var hit = TradeLayout.HitTest(cx, cy, ScreenWidth, ScreenHeight);
        Assert.Equal(TradeButton.Refuel, hit);
    }

    [Fact]
    public void Cancel_button_hit_test_returns_Cancel()
    {
        var rect = TradeLayout.CancelButtonRect();
        float cx = TradeLayout.PanelLeft(ScreenWidth) + rect.X + rect.W / 2f;
        float cy = TradeLayout.PanelTop(ScreenHeight) + rect.Y + rect.H / 2f;

        var hit = TradeLayout.HitTest(cx, cy, ScreenWidth, ScreenHeight);
        Assert.Equal(TradeButton.Cancel, hit);
    }

    [Fact]
    public void Quantity_stepper_minus_and_plus_hit_test_distinctly()
    {
        var rect = TradeLayout.QuantityStepperRect();
        float pl = TradeLayout.PanelLeft(ScreenWidth);
        float pt = TradeLayout.PanelTop(ScreenHeight);

        var minusHit = TradeLayout.HitTest(pl + rect.X + 5f, pt + rect.Y + rect.H / 2f, ScreenWidth, ScreenHeight);
        var plusHit = TradeLayout.HitTest(pl + rect.X + rect.W - 5f, pt + rect.Y + rect.H / 2f, ScreenWidth, ScreenHeight);

        Assert.Equal(TradeButton.QuantityMinus, minusHit);
        Assert.Equal(TradeButton.QuantityPlus, plusHit);
    }

    [Fact]
    public void Refuel_quantity_stepper_minus_and_plus_hit_test_distinctly()
    {
        var rect = TradeLayout.RefuelQuantityStepperRect();
        float pl = TradeLayout.PanelLeft(ScreenWidth);
        float pt = TradeLayout.PanelTop(ScreenHeight);

        var minusHit = TradeLayout.HitTest(pl + rect.X + 5f, pt + rect.Y + rect.H / 2f, ScreenWidth, ScreenHeight);
        var plusHit = TradeLayout.HitTest(pl + rect.X + rect.W - 5f, pt + rect.Y + rect.H / 2f, ScreenWidth, ScreenHeight);

        Assert.Equal(TradeButton.RefuelQuantityMinus, minusHit);
        Assert.Equal(TradeButton.RefuelQuantityPlus, plusHit);
    }

    [Fact]
    public void HitTestInventoryRow_returns_correct_row_index()
    {
        var row1 = TradeLayout.InventoryRowRect(1);
        float pl = TradeLayout.PanelLeft(ScreenWidth);
        float pt = TradeLayout.PanelTop(ScreenHeight);
        float cx = pl + row1.X + row1.W / 2f;
        float cy = pt + row1.Y + row1.H / 2f;

        int hitIndex = TradeLayout.HitTestInventoryRow(cx, cy, ScreenWidth, ScreenHeight, itemCount: 3);
        Assert.Equal(1, hitIndex);
    }

    [Fact]
    public void HitTestInventoryRow_returns_minus_one_outside_item_count()
    {
        var row5 = TradeLayout.InventoryRowRect(5);
        float pl = TradeLayout.PanelLeft(ScreenWidth);
        float pt = TradeLayout.PanelTop(ScreenHeight);
        float cx = pl + row5.X + row5.W / 2f;
        float cy = pt + row5.Y + row5.H / 2f;

        // Only 3 items exist — row 5 must not hit even though its geometry exists.
        int hitIndex = TradeLayout.HitTestInventoryRow(cx, cy, ScreenWidth, ScreenHeight, itemCount: 3);
        Assert.Equal(-1, hitIndex);
    }

    [Fact]
    public void HitTestCargoRow_returns_correct_row_index()
    {
        var row0 = TradeLayout.CargoRowRect(0);
        float pl = TradeLayout.PanelLeft(ScreenWidth);
        float pt = TradeLayout.PanelTop(ScreenHeight);
        float cx = pl + row0.X + row0.W / 2f;
        float cy = pt + row0.Y + row0.H / 2f;

        int hitIndex = TradeLayout.HitTestCargoRow(cx, cy, ScreenWidth, ScreenHeight, cargoStackCount: 2);
        Assert.Equal(0, hitIndex);
    }

    [Fact]
    public void IsInsidePanel_true_inside_false_outside()
    {
        float pl = TradeLayout.PanelLeft(ScreenWidth);
        float pt = TradeLayout.PanelTop(ScreenHeight);

        Assert.True(TradeLayout.IsInsidePanel(pl + 10f, pt + 10f, ScreenWidth, ScreenHeight));
        Assert.False(TradeLayout.IsInsidePanel(0f, 0f, ScreenWidth, ScreenHeight));
    }
}
