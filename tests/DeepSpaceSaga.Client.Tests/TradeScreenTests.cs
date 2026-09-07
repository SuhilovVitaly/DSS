using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// The Trade overlay screen itself (opened from StationScreen's `TRADE` button — see
/// StationScreenTests's Trade-button tests). Placeholder shell only — the previous Buy/
/// Sell/Refuel MVP was stripped down ahead of a full redesign, so there's no trade data
/// to assert on, just the open/close mechanics. Structural twin of HireScreenTests/
/// ContractsScreenTests/StationScreenTests.
/// </summary>
public class TradeScreenTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static void RenderScreen(TradeScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    [Fact]
    public void Escape_returns_CloseTrade()
    {
        var screen = new TradeScreen();
        var result = screen.OnKeyDown(Key.Escape);
        Assert.Equal(ScreenEvent.CloseTrade, result);
    }

    [Fact]
    public void Exit_button_click_returns_CloseTrade()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        var local = StationToolbar.ExitButtonLocalRect();
        float cx = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = TradeLayout.PanelTop(ScreenHeight) + local.MidY;

        var result = screen.OnMouseDown(cx, cy);
        Assert.Equal(ScreenEvent.CloseTrade, result);
    }

    [Fact]
    public void Hovering_the_exit_button_reports_interactive()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        var local = StationToolbar.ExitButtonLocalRect();
        float cx = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = TradeLayout.PanelTop(ScreenHeight) + local.MidY;

        Assert.True(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Hovering_food_rations_does_not_report_interactive()
    {
        // The readout is not a button — hovering it only shows a tooltip, it must not
        // trigger the same cursor swap as the name link / exit button.
        var screen = new TradeScreen();
        RenderScreen(screen);

        var local = StationToolbar.FoodRationsLocalRect();
        float cx = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = TradeLayout.PanelTop(ScreenHeight) + local.MidY;

        Assert.False(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Food_rations_tooltip_only_appears_after_the_configured_hover_delay()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        var local = StationToolbar.FoodRationsLocalRect();
        float cx = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = TradeLayout.PanelTop(ScreenHeight) + local.MidY;

        screen.OnMouseMove(cx, cy);
        Assert.False(screen.IsFoodRationsTooltipVisible);

        Thread.Sleep((int)(MenuStyle.TooltipHoverDelaySeconds * 1000) + 150);

        // No further OnMouseMove call — the delay must be re-checked purely from elapsed
        // real time (Render re-evaluates it every frame even while the pointer sits still).
        Assert.True(screen.IsFoodRationsTooltipVisible);
    }

    [Fact]
    public void Hovering_crew_does_not_report_interactive()
    {
        // Same "plain readout" rule as food rations — hovering it only shows a tooltip, it
        // must not trigger the same cursor swap as the name link / exit button.
        var screen = new TradeScreen();
        RenderScreen(screen);

        var local = StationToolbar.CrewLocalRect();
        float cx = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = TradeLayout.PanelTop(ScreenHeight) + local.MidY;

        Assert.False(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Crew_tooltip_only_appears_after_the_configured_hover_delay()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        var local = StationToolbar.CrewLocalRect();
        float cx = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = TradeLayout.PanelTop(ScreenHeight) + local.MidY;

        screen.OnMouseMove(cx, cy);
        Assert.False(screen.IsCrewTooltipVisible);

        Thread.Sleep((int)(MenuStyle.TooltipHoverDelaySeconds * 1000) + 150);

        // No further OnMouseMove call — the delay must be re-checked purely from elapsed
        // real time (Render re-evaluates it every frame even while the pointer sits still).
        Assert.True(screen.IsCrewTooltipVisible);
    }

    [Fact]
    public void Hovering_tokens_does_not_report_interactive()
    {
        // Same "plain readout" rule as food rations and crew — hovering it only shows a
        // tooltip, it must not trigger the same cursor swap as the name link / exit button.
        var screen = new TradeScreen();
        RenderScreen(screen);

        var local = StationToolbar.TokensLocalRect();
        float cx = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = TradeLayout.PanelTop(ScreenHeight) + local.MidY;

        Assert.False(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Tokens_tooltip_only_appears_after_the_configured_hover_delay()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        var local = StationToolbar.TokensLocalRect();
        float cx = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = TradeLayout.PanelTop(ScreenHeight) + local.MidY;

        screen.OnMouseMove(cx, cy);
        Assert.False(screen.IsTokensTooltipVisible);

        Thread.Sleep((int)(MenuStyle.TooltipHoverDelaySeconds * 1000) + 150);

        // No further OnMouseMove call — the delay must be re-checked purely from elapsed
        // real time (Render re-evaluates it every frame even while the pointer sits still).
        Assert.True(screen.IsTokensTooltipVisible);
    }

    [Fact]
    public void Hovering_fuel_does_not_report_interactive()
    {
        // Same "plain readout" rule as the other readouts — hovering it only shows a
        // tooltip, it must not trigger the same cursor swap as the name link / exit button.
        var screen = new TradeScreen();
        RenderScreen(screen);

        var local = StationToolbar.FuelLocalRect();
        float cx = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = TradeLayout.PanelTop(ScreenHeight) + local.MidY;

        Assert.False(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Fuel_tooltip_only_appears_after_the_configured_hover_delay()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        var local = StationToolbar.FuelLocalRect();
        float cx = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = TradeLayout.PanelTop(ScreenHeight) + local.MidY;

        screen.OnMouseMove(cx, cy);
        Assert.False(screen.IsFuelTooltipVisible);

        Thread.Sleep((int)(MenuStyle.TooltipHoverDelaySeconds * 1000) + 150);

        // No further OnMouseMove call — the delay must be re-checked purely from elapsed
        // real time (Render re-evaluates it every frame even while the pointer sits still).
        Assert.True(screen.IsFuelTooltipVisible);
    }

    [Fact]
    public void Click_inside_panel_outside_close_button_returns_None()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        float px = TradeLayout.PanelLeft(ScreenWidth) + TradeLayout.PanelWidth / 2f;
        float py = TradeLayout.PanelTop(ScreenHeight) + TradeLayout.PanelHeight / 2f;

        var result = screen.OnMouseDown(px, py);
        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void Click_outside_panel_returns_CloseTrade()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        // Top-left corner of the screen — well outside the centered panel.
        var result = screen.OnMouseDown(2f, 2f);
        Assert.Equal(ScreenEvent.CloseTrade, result);
    }

    [Fact]
    public void Right_click_outside_panel_does_not_close()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        var result = screen.OnMouseDown(2f, 2f, MouseButton.Right);
        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void Station_name_click_returns_NavigateToStation()
    {
        var screen = new TradeScreen(DockedBuffer());
        RenderScreen(screen);

        var (x, y) = StationNameCenter();
        Assert.Equal(ScreenEvent.NavigateToStation, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void Hovering_the_station_name_reports_interactive()
    {
        var screen = new TradeScreen(DockedBuffer());
        RenderScreen(screen);

        var (x, y) = StationNameCenter();
        Assert.True(screen.OnMouseMove(x, y));
    }

    private static SnapshotBuffer DockedBuffer()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1, GameTimeMs: 0, CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(
                new ObjectMotionSnapshot("SHIP-01", 0, 0, 0, 0, IsDocked: true, DockedStationObjectId: "STN-01"),
                new ObjectMotionSnapshot("STN-01", 0, 0, 0, 0, DisplayName: "Test Station")),
            PlayerShipObjectId: "SHIP-01"));
        return buffer;
    }

    /// <summary>Six resource items on the docked station (item catalog as of this story) — one more than GridPanel.MaxVisibleRows, exercising the scrollbar's active/scrolling path below.</summary>
    private static SnapshotBuffer DockedBufferWithSixResources()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1, GameTimeMs: 0, CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(
                new ObjectMotionSnapshot("SHIP-01", 0, 0, 0, 0, IsDocked: true, DockedStationObjectId: "STN-01"),
                new ObjectMotionSnapshot("STN-01", 0, 0, 0, 0, DisplayName: "Test Station")),
            PlayerShipObjectId: "SHIP-01",
            DockedStationTrade: new StationTradeSnapshot("STN-01", ImmutableArray.Create(
                new StationInventoryItemSnapshot("item.uranium-ore", 20, 30, 20, TradeItemCategories.Resource),
                new StationInventoryItemSnapshot("item.silicon", 70, 40, 70, TradeItemCategories.Resource),
                new StationInventoryItemSnapshot("item.fuel", 700, 5, 700, TradeItemCategories.Good),
                new StationInventoryItemSnapshot("item.ice", 320, 10, 320, TradeItemCategories.Resource),
                new StationInventoryItemSnapshot("item.iron-ore", 410, 5, 410, TradeItemCategories.Resource),
                new StationInventoryItemSnapshot("item.carbon-ore", 90, 30, 90, TradeItemCategories.Resource),
                new StationInventoryItemSnapshot("item.magnesium-ore", 120, 30, 120, TradeItemCategories.Resource)))));
        return buffer;
    }

    /// <summary>Six good items on the docked station — one more than GridPanel.MaxVisibleRows, exercising the Goods grid's scrollbar's active/scrolling path below (mirrors DockedBufferWithSixResources).</summary>
    private static SnapshotBuffer DockedBufferWithSixGoods()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1, GameTimeMs: 0, CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(
                new ObjectMotionSnapshot("SHIP-01", 0, 0, 0, 0, IsDocked: true, DockedStationObjectId: "STN-01"),
                new ObjectMotionSnapshot("STN-01", 0, 0, 0, 0, DisplayName: "Test Station")),
            PlayerShipObjectId: "SHIP-01",
            DockedStationTrade: new StationTradeSnapshot("STN-01", ImmutableArray.Create(
                new StationInventoryItemSnapshot("item.fuel", 700, 5, 700, TradeItemCategories.Good),
                new StationInventoryItemSnapshot("item.steel", 40, 15, 40, TradeItemCategories.Good),
                new StationInventoryItemSnapshot("item.ice", 320, 10, 320, TradeItemCategories.Resource),
                new StationInventoryItemSnapshot("item.water", 14, 20, 14, TradeItemCategories.Good),
                new StationInventoryItemSnapshot("item.energy-cells", 50, 25, 50, TradeItemCategories.Good),
                new StationInventoryItemSnapshot("item.protein-mass", 110, 30, 110, TradeItemCategories.Good),
                new StationInventoryItemSnapshot("item.food-rations", 90, 8, 90, TradeItemCategories.Good)))));
        return buffer;
    }

    /// <summary>Resource names come from the docked station's real trade snapshot, filtered to TradeItemCategories.Resource and alphabetically sorted — Good items (Fuel here) are excluded.</summary>
    [Fact]
    public void ResourceNames_are_resource_category_items_sorted_alphabetically()
    {
        var screen = new TradeScreen(DockedBufferWithSixResources());

        Assert.Equal(
            new[] { "Carbon Ore", "Ice", "Iron Ore", "Magnesium Ore", "Silicon", "Uranium Ore" },
            screen.ResourceNames);
    }

    /// <summary>Selling price/count columns read UnitPriceCredits/StockQuantity off the same items, in the same name-sorted row order as ResourceNames — never re-sorted independently.</summary>
    [Fact]
    public void ResourceSellingPrices_and_ResourceSellingCounts_match_the_station_snapshot_in_ResourceNames_order()
    {
        var screen = new TradeScreen(DockedBufferWithSixResources());

        // Row order: Carbon Ore, Ice, Iron Ore, Magnesium Ore, Silicon, Uranium Ore.
        Assert.Equal(new[] { "30", "10", "5", "30", "40", "30" }, screen.ResourceSellingPrices);
        Assert.Equal(new[] { "90", "320", "410", "120", "70", "20" }, screen.ResourceSellingCounts);
    }

    /// <summary>
    /// Buying price reuses the station's single UnitPriceCredits (same MVP price both
    /// directions — no separate buy/sell price field exists), but Buying count comes from
    /// the player's own ship cargo, not the station: present items report their cargo
    /// quantity, everything else (Iron Ore/Magnesium Ore/Silicon/Uranium Ore here) is 0.
    /// </summary>
    [Fact]
    public void ResourceBuyingPrices_match_selling_prices_and_ResourceBuyingCounts_come_from_ship_cargo()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1, GameTimeMs: 0, CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(
                new ObjectMotionSnapshot("SHIP-01", 0, 0, 0, 0, IsDocked: true, DockedStationObjectId: "STN-01"),
                new ObjectMotionSnapshot("STN-01", 0, 0, 0, 0, DisplayName: "Test Station")),
            PlayerShipObjectId: "SHIP-01",
            DockedStationTrade: new StationTradeSnapshot("STN-01", ImmutableArray.Create(
                new StationInventoryItemSnapshot("item.silicon", 70, 40, 70, TradeItemCategories.Resource),
                new StationInventoryItemSnapshot("item.ice", 320, 10, 320, TradeItemCategories.Resource),
                new StationInventoryItemSnapshot("item.iron-ore", 410, 5, 410, TradeItemCategories.Resource))),
            InstalledModules: ImmutableArray.Create(
                new InstalledModuleSnapshot(
                    ModuleId: "MOD-CONTAINER", ModuleTypeId: "module.container", DisplayName: "Container",
                    Position: 0, CommandTypeIds: ImmutableArray.Create(TradeCommandTypes.Buy, TradeCommandTypes.Sell),
                    Cargo: ImmutableArray.Create(
                        new CargoStackSnapshot("item.ice", 50),
                        new CargoStackSnapshot("item.silicon", 5))))));

        var screen = new TradeScreen(buffer);

        // Row order: Ice, Iron Ore, Silicon.
        Assert.Equal(screen.ResourceSellingPrices, screen.ResourceBuyingPrices);
        Assert.Equal(new[] { "50", "0", "5" }, screen.ResourceBuyingCounts);
    }

    /// <summary>Clicking a column title sorts by it — starting ascending, numerically (not the lexicographic order the raw strings would give: "120" &lt; "20" as text, but 20 &lt; 120 as a count).</summary>
    [Fact]
    public void Clicking_a_column_title_sorts_by_it_ascending()
    {
        var screen = new TradeScreen(DockedBufferWithSixResources());
        RenderScreen(screen);
        Assert.Equal(GridSortColumn.Name, screen.SortColumn);
        Assert.False(screen.SortDescending);

        var (x, y) = TrailingColumnHeaderCenter(columnIndex: 1); // Selling count
        screen.OnMouseDown(x, y);

        Assert.Equal(GridSortColumn.SellingCount, screen.SortColumn);
        Assert.False(screen.SortDescending);
        Assert.Equal(
            new[] { "Uranium Ore", "Silicon", "Carbon Ore", "Magnesium Ore", "Ice", "Iron Ore" },
            screen.ResourceNames);
        Assert.Equal(new[] { "20", "70", "90", "120", "320", "410" }, screen.ResourceSellingCounts);
    }

    /// <summary>A second click on the same title flips direction instead of doing nothing.</summary>
    [Fact]
    public void Clicking_the_same_column_title_again_flips_the_sort_direction()
    {
        var screen = new TradeScreen(DockedBufferWithSixResources());
        RenderScreen(screen);

        var (x, y) = TrailingColumnHeaderCenter(columnIndex: 1); // Selling count
        screen.OnMouseDown(x, y);
        screen.OnMouseDown(x, y);

        Assert.Equal(GridSortColumn.SellingCount, screen.SortColumn);
        Assert.True(screen.SortDescending);
        Assert.Equal(new[] { "410", "320", "120", "90", "70", "20" }, screen.ResourceSellingCounts);
    }

    /// <summary>Clicking a different title switches column and resets to ascending, rather than carrying over the previous direction.</summary>
    [Fact]
    public void Clicking_a_different_column_title_switches_column_and_resets_to_ascending()
    {
        var screen = new TradeScreen(DockedBufferWithSixResources());
        RenderScreen(screen);

        var (countX, countY) = TrailingColumnHeaderCenter(columnIndex: 1); // Selling count
        screen.OnMouseDown(countX, countY);
        screen.OnMouseDown(countX, countY); // now descending

        var titleRect = GridPanel.TitleLocalRect(15f, 76f, "Resources");
        float titleX = TradeLayout.PanelLeft(ScreenWidth) + titleRect.MidX;
        float titleY = TradeLayout.PanelTop(ScreenHeight) + titleRect.MidY;
        screen.OnMouseDown(titleX, titleY);

        Assert.Equal(GridSortColumn.Name, screen.SortColumn);
        Assert.False(screen.SortDescending);
    }

    [Fact]
    public void Hovering_a_column_title_reports_interactive()
    {
        var screen = new TradeScreen(DockedBufferWithSixResources());
        RenderScreen(screen);

        var (x, y) = TrailingColumnHeaderCenter(columnIndex: 0); // Selling price
        Assert.True(screen.OnMouseMove(x, y));
    }

    /// <summary>Re-sorting invalidates index-based selection — the row under the old index is very likely a different item now.</summary>
    [Fact]
    public void Row_selection_survives_a_resort_and_follows_the_selected_item_to_its_new_position()
    {
        var screen = new TradeScreen(DockedBufferWithSixResources());
        RenderScreen(screen);

        // Default sort (Name ascending): Carbon Ore, Ice, Iron Ore, Magnesium Ore, Silicon,
        // Uranium Ore — row slot 1 is "Ice".
        var (rowX, rowY) = ResourceRowCenter(rowSlot: 1);
        screen.OnMouseDown(rowX, rowY);
        Assert.Equal(1, screen.SelectedResourceIndex);
        Assert.Equal("Ice", screen.ResourceNames[screen.SelectedResourceIndex!.Value]);

        // Sort by Selling count ascending: Uranium Ore(20), Silicon(70), Carbon Ore(90),
        // Magnesium Ore(120), Ice(320), Iron Ore(410) — "Ice" is now at index 4, not 1, and
        // must still be the one highlighted (not cleared, not stuck at the old index).
        var (titleX, titleY) = TrailingColumnHeaderCenter(columnIndex: 1);
        screen.OnMouseDown(titleX, titleY);

        Assert.Equal(4, screen.SelectedResourceIndex);
        Assert.Equal("Ice", screen.ResourceNames[screen.SelectedResourceIndex!.Value]);
    }

    /// <summary>Screen-space center of trailing column header <paramref name="columnIndex"/> (0=Selling price, 1=Selling count, 2=Buying price, 3=Buying count).</summary>
    private static (float X, float Y) TrailingColumnHeaderCenter(int columnIndex)
    {
        var local = GridPanel.TrailingColumnHeaderLocalRect(15f, 76f, columnIndex);
        float x = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float y = TradeLayout.PanelTop(ScreenHeight) + local.MidY;
        return (x, y);
    }

    /// <summary>
    /// Regression for the reported bug: 6 resource items in a 5-row grid must let the
    /// down arrow actually scroll (previously the scrollbar was active and the thumb
    /// moved, but the drawn window never changed — nothing consumed the offset).
    /// </summary>
    [Fact]
    public void Scrollbar_down_arrow_advances_the_scroll_offset_when_rows_exceed_the_visible_window()
    {
        var screen = new TradeScreen(DockedBufferWithSixResources());
        RenderScreen(screen);
        Assert.Equal(6, screen.ResourceNames.Length);
        Assert.Equal(0, screen.ScrollOffset);

        var local = GridPanel.ScrollDownArrowLocalRect(15f, 76f, screen.ResourceNames.Length);
        float x = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float y = TradeLayout.PanelTop(ScreenHeight) + local.MidY;

        screen.OnMouseDown(x, y);
        Assert.Equal(1, screen.ScrollOffset);

        // Clamped at GridPanel.MaxScrollOffset(6) = 1 — a further click must not overshoot.
        screen.OnMouseDown(x, y);
        Assert.Equal(1, screen.ScrollOffset);
    }

    [Fact]
    public void Mouse_wheel_scrolls_the_resources_grid_and_clamps_at_both_bounds()
    {
        var screen = new TradeScreen(DockedBufferWithSixResources());
        RenderScreen(screen);
        Assert.Equal(0, screen.ScrollOffset);

        screen.OnMouseWheel(0f, 0f, -1f);
        Assert.Equal(1, screen.ScrollOffset);

        // Clamped at GridPanel.MaxScrollOffset(6) = 1 — a further tick the same direction must not overshoot.
        screen.OnMouseWheel(0f, 0f, -1f);
        Assert.Equal(1, screen.ScrollOffset);

        screen.OnMouseWheel(0f, 0f, 1f);
        Assert.Equal(0, screen.ScrollOffset);

        // Clamped at 0 — a further tick the same direction must not go negative.
        screen.OnMouseWheel(0f, 0f, 1f);
        Assert.Equal(0, screen.ScrollOffset);
    }

    [Fact]
    public void Dragging_the_scrollbar_thumb_moves_the_scroll_offset()
    {
        var screen = new TradeScreen(DockedBufferWithSixResources());
        RenderScreen(screen);

        var thumbLocal = GridPanel.ScrollThumbLocalRect(15f, 76f, screen.ResourceNames.Length, screen.ScrollOffset);
        float pl = TradeLayout.PanelLeft(ScreenWidth);
        float pt = TradeLayout.PanelTop(ScreenHeight);
        float grabX = pl + thumbLocal.MidX;
        float grabY = pt + thumbLocal.MidY;

        // Grab the thumb, then drag it well past the track's bottom — must clamp at
        // GridPanel.MaxScrollOffset(6) = 1, not overshoot or throw.
        screen.OnMouseDown(grabX, grabY);
        screen.OnMouseMove(grabX, pt + 1000f);
        Assert.Equal(1, screen.ScrollOffset);

        // Drag back up past the track's top — must clamp at 0.
        screen.OnMouseMove(grabX, pt - 1000f);
        Assert.Equal(0, screen.ScrollOffset);

        // Releasing ends the drag — further movement must not change the offset.
        screen.OnMouseUp(grabX, pt + 1000f);
        screen.OnMouseMove(grabX, pt + 1000f);
        Assert.Equal(0, screen.ScrollOffset);
    }

    [Fact]
    public void Clicking_a_resource_row_selects_it()
    {
        var screen = new TradeScreen(DockedBufferWithSixResources());
        RenderScreen(screen);
        Assert.Null(screen.SelectedResourceIndex);

        var (x, y) = ResourceRowCenter(rowSlot: 2);
        screen.OnMouseDown(x, y);

        Assert.Equal(2, screen.SelectedResourceIndex);
    }

    /// <summary>Clicking a different row moves the selection rather than toggling/adding to it.</summary>
    [Fact]
    public void Clicking_a_different_resource_row_changes_the_selection()
    {
        var screen = new TradeScreen(DockedBufferWithSixResources());
        RenderScreen(screen);

        var (firstX, firstY) = ResourceRowCenter(rowSlot: 0);
        screen.OnMouseDown(firstX, firstY);
        Assert.Equal(0, screen.SelectedResourceIndex);

        var (secondX, secondY) = ResourceRowCenter(rowSlot: 3);
        screen.OnMouseDown(secondX, secondY);
        Assert.Equal(3, screen.SelectedResourceIndex);
    }

    [Fact]
    public void Clicking_outside_the_grid_rows_leaves_the_selection_unchanged()
    {
        var screen = new TradeScreen(DockedBufferWithSixResources());
        RenderScreen(screen);

        var (x, y) = ResourceRowCenter(rowSlot: 1);
        screen.OnMouseDown(x, y);
        Assert.Equal(1, screen.SelectedResourceIndex);

        // Inside the panel, well below the grid rows and scrollbar — must not clear or move the selection.
        float px = TradeLayout.PanelLeft(ScreenWidth) + TradeLayout.PanelWidth / 2f;
        float py = TradeLayout.PanelTop(ScreenHeight) + TradeLayout.PanelHeight - 10f;
        screen.OnMouseDown(px, py);

        Assert.Equal(1, screen.SelectedResourceIndex);
    }

    /// <summary>Screen-space center of the resources grid's visible row slot (0 = topmost drawn row) — see GridPanel's origin (15, 76).</summary>
    private static (float X, float Y) ResourceRowCenter(int rowSlot)
    {
        var local = GridPanel.RowLocalRect(15f, 76f, rowSlot);
        float x = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float y = TradeLayout.PanelTop(ScreenHeight) + local.MidY;
        return (x, y);
    }

    /// <summary>Screen-space center of the toolbar's "Test Station" name label (see DockedBuffer).</summary>
    private static (float X, float Y) StationNameCenter()
    {
        var local = StationToolbar.NameLocalRect("Test Station");
        float x = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float y = TradeLayout.PanelTop(ScreenHeight) + local.MidY;
        return (x, y);
    }

    // ── Goods grid — same size/columns/scroll/sort as the Resources grid above, at its own
    // origin (15, 325) directly below it. Mirrors the Resources grid test coverage.

    /// <summary>Good names come from the docked station's real trade snapshot, filtered to TradeItemCategories.Good and alphabetically sorted — the Resource item (Ice here) is excluded.</summary>
    [Fact]
    public void GoodNames_are_good_category_items_sorted_alphabetically()
    {
        var screen = new TradeScreen(DockedBufferWithSixGoods());

        Assert.Equal(
            new[] { "Energy Cells", "Food Rations", "Fuel", "Protein Mass", "Steel", "Water" },
            screen.GoodNames);
    }

    /// <summary>Selling price/count columns read UnitPriceCredits/StockQuantity off the same items, in the same name-sorted row order as GoodNames.</summary>
    [Fact]
    public void GoodSellingPrices_and_GoodSellingCounts_match_the_station_snapshot_in_GoodNames_order()
    {
        var screen = new TradeScreen(DockedBufferWithSixGoods());

        // Row order: Energy Cells, Food Rations, Fuel, Protein Mass, Steel, Water.
        Assert.Equal(new[] { "25", "8", "5", "30", "15", "20" }, screen.GoodSellingPrices);
        Assert.Equal(new[] { "50", "90", "700", "110", "40", "14" }, screen.GoodSellingCounts);
    }

    /// <summary>Buying price reuses the station's UnitPriceCredits, and Buying count comes from the player's own ship cargo — same MVP rule as the Resources grid.</summary>
    [Fact]
    public void GoodBuyingPrices_match_selling_prices_and_GoodBuyingCounts_come_from_ship_cargo()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1, GameTimeMs: 0, CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(
                new ObjectMotionSnapshot("SHIP-01", 0, 0, 0, 0, IsDocked: true, DockedStationObjectId: "STN-01"),
                new ObjectMotionSnapshot("STN-01", 0, 0, 0, 0, DisplayName: "Test Station")),
            PlayerShipObjectId: "SHIP-01",
            DockedStationTrade: new StationTradeSnapshot("STN-01", ImmutableArray.Create(
                new StationInventoryItemSnapshot("item.fuel", 700, 5, 700, TradeItemCategories.Good),
                new StationInventoryItemSnapshot("item.water", 14, 20, 14, TradeItemCategories.Good),
                new StationInventoryItemSnapshot("item.steel", 40, 15, 40, TradeItemCategories.Good))),
            InstalledModules: ImmutableArray.Create(
                new InstalledModuleSnapshot(
                    ModuleId: "MOD-CONTAINER", ModuleTypeId: "module.container", DisplayName: "Container",
                    Position: 0, CommandTypeIds: ImmutableArray.Create(TradeCommandTypes.Buy, TradeCommandTypes.Sell),
                    Cargo: ImmutableArray.Create(
                        new CargoStackSnapshot("item.fuel", 200),
                        new CargoStackSnapshot("item.water", 3))))));

        var screen = new TradeScreen(buffer);

        // Row order: Fuel, Steel, Water.
        Assert.Equal(screen.GoodSellingPrices, screen.GoodBuyingPrices);
        Assert.Equal(new[] { "200", "0", "3" }, screen.GoodBuyingCounts);
    }

    [Fact]
    public void Clicking_a_good_column_title_sorts_by_it_ascending()
    {
        var screen = new TradeScreen(DockedBufferWithSixGoods());
        RenderScreen(screen);
        Assert.Equal(GridSortColumn.Name, screen.SortColumnGoods);
        Assert.False(screen.SortDescendingGoods);

        var (x, y) = GoodTrailingColumnHeaderCenter(columnIndex: 1); // Selling count
        screen.OnMouseDown(x, y);

        Assert.Equal(GridSortColumn.SellingCount, screen.SortColumnGoods);
        Assert.False(screen.SortDescendingGoods);
        Assert.Equal(
            new[] { "Water", "Steel", "Energy Cells", "Food Rations", "Protein Mass", "Fuel" },
            screen.GoodNames);
        Assert.Equal(new[] { "14", "40", "50", "90", "110", "700" }, screen.GoodSellingCounts);
    }

    [Fact]
    public void Clicking_the_same_good_column_title_again_flips_the_sort_direction()
    {
        var screen = new TradeScreen(DockedBufferWithSixGoods());
        RenderScreen(screen);

        var (x, y) = GoodTrailingColumnHeaderCenter(columnIndex: 1); // Selling count
        screen.OnMouseDown(x, y);
        screen.OnMouseDown(x, y);

        Assert.Equal(GridSortColumn.SellingCount, screen.SortColumnGoods);
        Assert.True(screen.SortDescendingGoods);
        Assert.Equal(new[] { "700", "110", "90", "50", "40", "14" }, screen.GoodSellingCounts);
    }

    /// <summary>Sorting the Goods grid must not disturb the independent Resources grid's own sort state.</summary>
    [Fact]
    public void Sorting_the_goods_grid_does_not_affect_the_resources_grid_sort_state()
    {
        var screen = new TradeScreen(DockedBufferWithSixGoods());
        RenderScreen(screen);

        var (x, y) = GoodTrailingColumnHeaderCenter(columnIndex: 1); // Selling count
        screen.OnMouseDown(x, y);

        Assert.Equal(GridSortColumn.SellingCount, screen.SortColumnGoods);
        Assert.Equal(GridSortColumn.Name, screen.SortColumn);
        Assert.False(screen.SortDescending);
    }

    /// <summary>Re-sorting invalidates index-based selection — the row under the old index is very likely a different item now.</summary>
    [Fact]
    public void Good_row_selection_survives_a_resort_and_follows_the_selected_item_to_its_new_position()
    {
        var screen = new TradeScreen(DockedBufferWithSixGoods());
        RenderScreen(screen);

        // Default sort (Name ascending): Energy Cells, Food Rations, Fuel, Protein Mass,
        // Steel, Water — row slot 1 is "Food Rations".
        var (rowX, rowY) = GoodRowCenter(rowSlot: 1);
        screen.OnMouseDown(rowX, rowY);
        Assert.Equal(1, screen.SelectedGoodIndex);
        Assert.Equal("Food Rations", screen.GoodNames[screen.SelectedGoodIndex!.Value]);

        // Sort by Selling count ascending: Water(14), Steel(40), Energy Cells(50),
        // Food Rations(90), Protein Mass(110), Fuel(700) — "Food Rations" is now at index 3.
        var (titleX, titleY) = GoodTrailingColumnHeaderCenter(columnIndex: 1);
        screen.OnMouseDown(titleX, titleY);

        Assert.Equal(3, screen.SelectedGoodIndex);
        Assert.Equal("Food Rations", screen.GoodNames[screen.SelectedGoodIndex!.Value]);
    }

    /// <summary>Regression coverage mirroring the Resources grid's scrollbar fix, for the Goods grid.</summary>
    [Fact]
    public void Goods_scrollbar_down_arrow_advances_the_scroll_offset_when_rows_exceed_the_visible_window()
    {
        var screen = new TradeScreen(DockedBufferWithSixGoods());
        RenderScreen(screen);
        Assert.Equal(6, screen.GoodNames.Length);
        Assert.Equal(0, screen.ScrollOffsetGoods);

        var local = GridPanel.ScrollDownArrowLocalRect(15f, 325f, screen.GoodNames.Length);
        float x = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float y = TradeLayout.PanelTop(ScreenHeight) + local.MidY;

        screen.OnMouseDown(x, y);
        Assert.Equal(1, screen.ScrollOffsetGoods);

        // Clamped at GridPanel.MaxScrollOffset(6) = 1 — a further click must not overshoot.
        screen.OnMouseDown(x, y);
        Assert.Equal(1, screen.ScrollOffsetGoods);
    }

    /// <summary>The mouse wheel scrolls whichever grid the pointer is over — hovering the Goods grid's rows must scroll it, not the Resources grid.</summary>
    [Fact]
    public void Mouse_wheel_over_the_goods_grid_scrolls_it_and_clamps_at_both_bounds()
    {
        var screen = new TradeScreen(DockedBufferWithSixGoods());
        RenderScreen(screen);
        Assert.Equal(0, screen.ScrollOffsetGoods);

        var (x, y) = GoodRowCenter(rowSlot: 0);

        screen.OnMouseWheel(x, y, -1f);
        Assert.Equal(1, screen.ScrollOffsetGoods);
        Assert.Equal(0, screen.ScrollOffset); // Resources grid untouched.

        // Clamped at GridPanel.MaxScrollOffset(6) = 1 — a further tick the same direction must not overshoot.
        screen.OnMouseWheel(x, y, -1f);
        Assert.Equal(1, screen.ScrollOffsetGoods);

        screen.OnMouseWheel(x, y, 1f);
        Assert.Equal(0, screen.ScrollOffsetGoods);
    }

    [Fact]
    public void Dragging_the_goods_scrollbar_thumb_moves_the_scroll_offset()
    {
        var screen = new TradeScreen(DockedBufferWithSixGoods());
        RenderScreen(screen);

        var thumbLocal = GridPanel.ScrollThumbLocalRect(15f, 325f, screen.GoodNames.Length, screen.ScrollOffsetGoods);
        float pl = TradeLayout.PanelLeft(ScreenWidth);
        float pt = TradeLayout.PanelTop(ScreenHeight);
        float grabX = pl + thumbLocal.MidX;
        float grabY = pt + thumbLocal.MidY;

        // Grab the thumb, then drag it well past the track's bottom — must clamp at
        // GridPanel.MaxScrollOffset(6) = 1, not overshoot or throw.
        screen.OnMouseDown(grabX, grabY);
        screen.OnMouseMove(grabX, pt + 1000f);
        Assert.Equal(1, screen.ScrollOffsetGoods);

        // Drag back up past the track's top — must clamp at 0.
        screen.OnMouseMove(grabX, pt - 1000f);
        Assert.Equal(0, screen.ScrollOffsetGoods);

        // Releasing ends the drag — further movement must not change the offset.
        screen.OnMouseUp(grabX, pt + 1000f);
        screen.OnMouseMove(grabX, pt + 1000f);
        Assert.Equal(0, screen.ScrollOffsetGoods);
    }

    [Fact]
    public void Clicking_a_good_row_selects_it()
    {
        var screen = new TradeScreen(DockedBufferWithSixGoods());
        RenderScreen(screen);
        Assert.Null(screen.SelectedGoodIndex);

        var (x, y) = GoodRowCenter(rowSlot: 2);
        screen.OnMouseDown(x, y);

        Assert.Equal(2, screen.SelectedGoodIndex);
    }

    /// <summary>The three grids share a single selection — picking a row in one grid clears whatever was selected in the other two.</summary>
    [Fact]
    public void Selecting_a_good_row_clears_the_resources_grid_selection_and_vice_versa()
    {
        var screen = new TradeScreen(DockedBufferWithSixGoods());
        RenderScreen(screen);

        var (resourceX, resourceY) = ResourceRowCenter(rowSlot: 0);
        screen.OnMouseDown(resourceX, resourceY);
        Assert.Equal(0, screen.SelectedResourceIndex);

        var (goodX, goodY) = GoodRowCenter(rowSlot: 2);
        screen.OnMouseDown(goodX, goodY);

        Assert.Equal(2, screen.SelectedGoodIndex);
        Assert.Null(screen.SelectedResourceIndex);

        // DockedBufferWithSixGoods has a single Resource item (Ice), so row slot 0 is the
        // only clickable Resources row — re-selecting it must clear the Goods selection.
        screen.OnMouseDown(resourceX, resourceY);

        Assert.Equal(0, screen.SelectedResourceIndex);
        Assert.Null(screen.SelectedGoodIndex);
    }

    /// <summary>Screen-space center of the goods grid's visible row slot (0 = topmost drawn row) — see GridPanel's origin (15, 325), directly below the Resources grid's (15, 76).</summary>
    private static (float X, float Y) GoodRowCenter(int rowSlot)
    {
        var local = GridPanel.RowLocalRect(15f, 325f, rowSlot);
        float x = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float y = TradeLayout.PanelTop(ScreenHeight) + local.MidY;
        return (x, y);
    }

    /// <summary>Screen-space center of the goods grid's trailing column header <paramref name="columnIndex"/> (0=Selling price, 1=Selling count, 2=Buying price, 3=Buying count).</summary>
    private static (float X, float Y) GoodTrailingColumnHeaderCenter(int columnIndex)
    {
        var local = GridPanel.TrailingColumnHeaderLocalRect(15f, 325f, columnIndex);
        float x = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float y = TradeLayout.PanelTop(ScreenHeight) + local.MidY;
        return (x, y);
    }

    // ── Modules grid — same size/columns/scroll/sort scaffolding as the other two grids,
    // at origin (15, 574) directly below the Goods grid, but a placeholder with no rows
    // until a real station-module-for-sale data model exists (see ResolveModuleRows).

    [Fact]
    public void Modules_grid_starts_as_an_empty_placeholder_with_no_selection_or_scroll()
    {
        var screen = new TradeScreen(DockedBufferWithSixGoods());
        RenderScreen(screen);

        Assert.Empty(screen.ModuleNames);
        Assert.Equal(0, screen.ScrollOffsetModules);
        Assert.Null(screen.SelectedModuleIndex);
        Assert.Equal(GridSortColumn.Name, screen.SortColumnModules);
        Assert.False(screen.SortDescendingModules);
    }

    /// <summary>Clicking where a module row would be must not throw, select anything, or disturb the other grids' selections — the grid has no rows to hit yet.</summary>
    [Fact]
    public void Clicking_inside_the_modules_grid_area_does_not_select_a_row()
    {
        var screen = new TradeScreen(DockedBufferWithSixGoods());
        RenderScreen(screen);

        var (resourceX, resourceY) = ResourceRowCenter(rowSlot: 0);
        screen.OnMouseDown(resourceX, resourceY);
        Assert.Equal(0, screen.SelectedResourceIndex);

        var (moduleX, moduleY) = ModuleRowCenter(rowSlot: 0);
        screen.OnMouseDown(moduleX, moduleY);

        Assert.Null(screen.SelectedModuleIndex);
        Assert.Equal(0, screen.SelectedResourceIndex);
    }

    /// <summary>The column titles are still clickable/sortable ahead of real data — toggling them must not throw or affect the other grids' sort state.</summary>
    [Fact]
    public void Clicking_a_modules_column_title_toggles_its_sort_state_without_affecting_the_other_grids()
    {
        var screen = new TradeScreen(DockedBufferWithSixGoods());
        RenderScreen(screen);

        var (x, y) = ModuleTrailingColumnHeaderCenter(columnIndex: 1); // Selling count
        screen.OnMouseDown(x, y);

        Assert.Equal(GridSortColumn.SellingCount, screen.SortColumnModules);
        Assert.False(screen.SortDescendingModules);
        Assert.Equal(GridSortColumn.Name, screen.SortColumn);
        Assert.Equal(GridSortColumn.Name, screen.SortColumnGoods);
    }

    /// <summary>The mouse wheel over the Modules grid's rows must not throw and must not scroll the other two grids.</summary>
    [Fact]
    public void Mouse_wheel_over_the_modules_grid_area_does_not_scroll_the_other_grids()
    {
        var screen = new TradeScreen(DockedBufferWithSixGoods());
        RenderScreen(screen);

        var (x, y) = ModuleRowCenter(rowSlot: 0);
        screen.OnMouseWheel(x, y, -1f);

        Assert.Equal(0, screen.ScrollOffsetModules);
        Assert.Equal(0, screen.ScrollOffset);
        Assert.Equal(0, screen.ScrollOffsetGoods);
    }

    /// <summary>Screen-space center of the modules grid's visible row slot (0 = topmost drawn row) — see GridPanel's origin (15, 574), directly below the Goods grid's (15, 325).</summary>
    private static (float X, float Y) ModuleRowCenter(int rowSlot)
    {
        var local = GridPanel.RowLocalRect(15f, 574f, rowSlot);
        float x = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float y = TradeLayout.PanelTop(ScreenHeight) + local.MidY;
        return (x, y);
    }

    /// <summary>Screen-space center of the modules grid's trailing column header <paramref name="columnIndex"/> (0=Selling price, 1=Selling count, 2=Buying price, 3=Buying count).</summary>
    private static (float X, float Y) ModuleTrailingColumnHeaderCenter(int columnIndex)
    {
        var local = GridPanel.TrailingColumnHeaderLocalRect(15f, 574f, columnIndex);
        float x = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float y = TradeLayout.PanelTop(ScreenHeight) + local.MidY;
        return (x, y);
    }

    // ── Right-hand info panels — no grid, just outlines to the right of the three grids.
    // Left edge sits as far from the grids' right edge (962) as the grids sit from the
    // Trade panel's own left edge (15) — i.e. 977. Right edge mirrors that same 15px gap
    // from the Trade panel's own right edge (1400): 1385.

    [Fact]
    public void Right_panels_render_without_a_grid_and_do_not_crash()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        var (upper, lower) = screen.RightPanels;

        // Right edges mirror the grids' 15px margin from the Trade panel's own right edge.
        Assert.Equal(1385f, upper.Right);
        Assert.Equal(1385f, lower.Right);

        // Both panels' left edge is the grids' 15px margin from the Trade panel's own left
        // edge, plus a shared 25px extra nudge right (RightPanelExtraLeftInset), minus a 5px
        // extra width (RightPanelExtraWidth) — net left edge 977+25-5=997.
        Assert.Equal(997f, upper.Left);
        Assert.Equal(997f, lower.Left);
    }

    /// <summary>The upper right-hand panel is 1/3 of the combined column height (Resources white frame's top down to Modules white frame's bottom), the lower is 2/3 — a product decision, no longer tied to the left grids' own Resources+Goods-vs-Modules stacking.</summary>
    [Fact]
    public void Upper_right_panel_outline_is_one_third_of_the_column_height()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        var (upper, _) = screen.RightPanels;

        Assert.Equal(90f, upper.Top); // Resources white frame's own top
        Assert.Equal(313f, upper.Bottom); // 90 + round((788-90-30)/3) = 90 + 223
        Assert.Equal(223f, upper.Height);
        Assert.Equal(388f, upper.Width); // 408 (977..1385) minus 25 (extra left inset) plus 5 (extra width)
    }

    /// <summary>The lower right-hand panel is 2/3 of the combined column height — see <see cref="Upper_right_panel_outline_is_one_third_of_the_column_height"/>.</summary>
    [Fact]
    public void Lower_right_panel_outline_is_two_thirds_of_the_column_height()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        var (_, lower) = screen.RightPanels;

        Assert.Equal(343f, lower.Top); // upper.Bottom (313) + the 30px gap between the panels
        Assert.Equal(788f, lower.Bottom); // Modules white frame's own bottom — unchanged column footprint
        Assert.Equal(445f, lower.Height);
        Assert.Equal(388f, lower.Width); // 408 (977..1385) minus 25 (extra left inset) plus 5 (extra width)
    }

    /// <summary>Both right-hand panels get a gray rounded-corner titlebar at their top, same style/height as GridPanel's own header bar.</summary>
    [Fact]
    public void Right_panels_have_a_titlebar_matching_gridpanels_header_height()
    {
        var screen = new TradeScreen();
        RenderScreen(screen); // must not throw while drawing the titlebars

        var (upper, lower) = screen.RightPanels;
        var (upperTitleBar, lowerTitleBar) = screen.RightPanelTitleBars;

        Assert.Equal(GridPanel.HeaderHeight, upperTitleBar.Height);
        Assert.Equal(GridPanel.HeaderHeight, lowerTitleBar.Height);

        // Sanity: both panels are tall enough to actually contain a 30px titlebar.
        Assert.True(upper.Height >= GridPanel.HeaderHeight);
        Assert.True(lower.Height >= GridPanel.HeaderHeight);
    }

    /// <summary>
    /// The titlebar's left edge is inset from its own panel's white outline by the same
    /// margin the Resources grid's header keeps from its white outline (header left 15 vs.
    /// outline left 10 = 5px) — and the right edge keeps that same 5px margin too (these
    /// panels have no scrollbar to justify the grid header's wider, asymmetric right gap).
    /// </summary>
    [Fact]
    public void Right_panel_titlebars_keep_equal_left_and_right_margins_from_the_white_outline()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        var (upper, lower) = screen.RightPanels;
        var (upperTitleBar, lowerTitleBar) = screen.RightPanelTitleBars;

        Assert.Equal(5f, upperTitleBar.Left - upper.Left);
        Assert.Equal(5f, lowerTitleBar.Left - lower.Left);
        Assert.Equal(5f, upper.Right - upperTitleBar.Right);
        Assert.Equal(5f, lower.Right - lowerTitleBar.Right);
    }

    /// <summary>
    /// The titlebar must sit as far above its own panel's white outline top edge as the
    /// Resources grid's own header bar sits above that same white outline on the left
    /// (origin Y 76 vs. white-frame top 90 — a 14px offset), not flush with it. Both right
    /// panels use that same 14px overlap above their own (now independently sized) top.
    /// </summary>
    [Fact]
    public void Right_panel_titlebars_are_offset_above_the_white_outline_the_same_way_as_the_grid_headers()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        var (upper, lower) = screen.RightPanels;
        var (upperTitleBar, lowerTitleBar) = screen.RightPanelTitleBars;

        Assert.Equal(76f, upperTitleBar.Top); // upper.Top (90) - 14, matches the Resources grid's own header top
        Assert.Equal(329f, lowerTitleBar.Top); // lower.Top (343) - 14
        Assert.Equal(14f, upper.Top - upperTitleBar.Top);
        Assert.Equal(14f, lower.Top - lowerTitleBar.Top);
    }

    // ── Trade action panel (lower right-hand frame) — Docs/FirstRelease/Screens/Trade.md
    // "UI-решение: панель действия". Buy/Sell/Refuel for whichever item is selected across
    // the three grids above. RecordingConnection/GameSessionHandle wiring mirrors
    // CommandsPanelSkeletonTests's pattern for asserting sent PlayerCommands.

    private const string ShipId = "SHIP-01";
    private const string StationId = "STN-01";
    private const string ContainerModuleId = "MOD-CONTAINER";
    private const string EngineModuleId = "MOD-ENGINE";

    /// <summary>
    /// Docked snapshot with a container module (Buy/Sell) and an engine module (Refuel)
    /// installed, and the given station inventory — the shared fixture shape for the trade
    /// action panel tests below.
    /// </summary>
    private static AuthoritativeSnapshot BuildTradeSnapshot(
        ImmutableArray<StationInventoryItemSnapshot> items,
        long playerCredits = 10_000,
        long containerAvailableCapacityKg = 5_000,
        ImmutableArray<CargoStackSnapshot> containerCargo = default,
        long fuelAmountKg = 100,
        long fuelCapacityKg = 500)
    {
        return new AuthoritativeSnapshot(
            SnapshotSequence: 1, GameTimeMs: 0, CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(
                new ObjectMotionSnapshot(ShipId, 0, 0, 0, 0, IsDocked: true, DockedStationObjectId: StationId),
                new ObjectMotionSnapshot(StationId, 0, 0, 0, 0, DisplayName: "Test Station")),
            PlayerShipObjectId: ShipId,
            PlayerCredits: playerCredits,
            DockedStationTrade: new StationTradeSnapshot(StationId, items),
            InstalledModules: ImmutableArray.Create(
                new InstalledModuleSnapshot(
                    ModuleId: ContainerModuleId, ModuleTypeId: "module.container", DisplayName: "Container",
                    Position: 0, CommandTypeIds: ImmutableArray.Create(TradeCommandTypes.Buy, TradeCommandTypes.Sell),
                    Cargo: containerCargo, AvailableCapacityKg: containerAvailableCapacityKg),
                new InstalledModuleSnapshot(
                    ModuleId: EngineModuleId, ModuleTypeId: "module.engine.basic", DisplayName: "Engine",
                    Position: 1, CommandTypeIds: ImmutableArray.Create(TradeCommandTypes.Refuel),
                    FuelAmountKg: fuelAmountKg, FuelCapacityKg: fuelCapacityKg)));
    }

    private sealed record TradeFixture(RecordingConnection Connection, GameSessionHandle Handle, TradeScreen Screen) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Handle.DisposeAsync();
    }

    private static TradeFixture CreateTradeFixture(AuthoritativeSnapshot snapshot)
    {
        var connection = new RecordingConnection();
        var handle = new GameSessionHandle(connection);
        handle.Buffer.Update(snapshot);
        var screen = new TradeScreen(handle.Buffer, handle);
        return new TradeFixture(connection, handle, screen);
    }

    private static (float X, float Y) ScreenCenter(SKRect local) =>
        (TradeLayout.PanelLeft(ScreenWidth) + local.MidX, TradeLayout.PanelTop(ScreenHeight) + local.MidY);

    [Fact]
    public void Selecting_a_resource_row_populates_the_trade_action_panel()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(BuildTradeSnapshot(ImmutableArray.Create(
            new StationInventoryItemSnapshot("item.silicon", 1000, 40, 1000, TradeItemCategories.Resource))));
        var screen = new TradeScreen(buffer);
        RenderScreen(screen);
        Assert.Null(screen.SelectedTradeItemTypeId);

        var (x, y) = ResourceRowCenter(rowSlot: 0);
        screen.OnMouseDown(x, y);
        RenderScreen(screen);

        Assert.Equal("item.silicon", screen.SelectedTradeItemTypeId);
        Assert.True(screen.IsTradeBuyMode);
        Assert.Equal(1, screen.TradeQuantity); // Trading is fully per-unit.
    }

    [Fact]
    public void Buy_sell_toggle_switches_mode_and_resets_quantity_to_one()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(BuildTradeSnapshot(
            ImmutableArray.Create(new StationInventoryItemSnapshot("item.silicon", 1000, 40, 1000, TradeItemCategories.Resource)),
            containerCargo: ImmutableArray.Create(new CargoStackSnapshot("item.silicon", 500))));
        var screen = new TradeScreen(buffer);
        RenderScreen(screen);

        var (rowX, rowY) = ResourceRowCenter(rowSlot: 0);
        screen.OnMouseDown(rowX, rowY);
        RenderScreen(screen);
        Assert.True(screen.IsTradeBuyMode);

        var (sellX, sellY) = ScreenCenter(screen.TradeModeToggleRects.Sell);
        screen.OnMouseDown(sellX, sellY);

        Assert.False(screen.IsTradeBuyMode);
        Assert.Equal(1, screen.TradeQuantity);

        var (buyX, buyY) = ScreenCenter(screen.TradeModeToggleRects.Buy);
        screen.OnMouseDown(buyX, buyY);

        Assert.True(screen.IsTradeBuyMode);
    }

    /// <summary>Fuel has no Sell counterpart (Docs/FirstRelease/Screens/Trade.md's routing rule) — the toggle stays locked on Buy for it.</summary>
    [Fact]
    public void Selecting_fuel_locks_the_toggle_on_buy_mode()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(BuildTradeSnapshot(ImmutableArray.Create(
            new StationInventoryItemSnapshot("item.fuel", 1000, 5, 1000, TradeItemCategories.Good))));
        var screen = new TradeScreen(buffer);
        RenderScreen(screen);

        var (goodX, goodY) = GoodRowCenter(rowSlot: 0);
        screen.OnMouseDown(goodX, goodY);
        RenderScreen(screen);
        Assert.Equal("item.fuel", screen.SelectedTradeItemTypeId);
        Assert.True(screen.IsTradeBuyMode);

        var (sellX, sellY) = ScreenCenter(screen.TradeModeToggleRects.Sell);
        screen.OnMouseDown(sellX, sellY);

        // Clicking Sell for Fuel must be a no-op — the toggle stays on Buy.
        Assert.True(screen.IsTradeBuyMode);
    }

    [Fact]
    public void Plus_and_minus_move_the_quantity_by_one_and_clamp_at_both_bounds()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(BuildTradeSnapshot(
            ImmutableArray.Create(new StationInventoryItemSnapshot("item.silicon", 1000, 40, 1000, TradeItemCategories.Resource)),
            playerCredits: 120)); // Max = min(120/40, 1000) = 3.
        var screen = new TradeScreen(buffer);
        RenderScreen(screen);

        var (rowX, rowY) = ResourceRowCenter(rowSlot: 0);
        screen.OnMouseDown(rowX, rowY);
        RenderScreen(screen);
        Assert.Equal(1, screen.TradeQuantity);
        Assert.Equal(3, screen.TradeMaxQuantity);

        var (plusX, plusY) = ScreenCenter(screen.TradeStepperRects.Plus);
        screen.OnMouseDown(plusX, plusY);
        Assert.Equal(2, screen.TradeQuantity);

        screen.OnMouseDown(plusX, plusY);
        Assert.Equal(3, screen.TradeQuantity);

        screen.OnMouseDown(plusX, plusY); // 4 would exceed Max(3) — clamp.
        Assert.Equal(3, screen.TradeQuantity);

        var (minusX, minusY) = ScreenCenter(screen.TradeStepperRects.Minus);
        screen.OnMouseDown(minusX, minusY);
        Assert.Equal(2, screen.TradeQuantity);

        screen.OnMouseDown(minusX, minusY);
        screen.OnMouseDown(minusX, minusY); // would go to 0 — clamp at 1 (the minimum quantity).
        Assert.Equal(1, screen.TradeQuantity);
    }

    [Fact]
    public void Max_sets_the_buy_quantity_to_the_affordable_station_stock_bound()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(BuildTradeSnapshot(
            ImmutableArray.Create(new StationInventoryItemSnapshot("item.silicon", 1000, 40, 1000, TradeItemCategories.Resource)),
            playerCredits: 10_000)); // floor(10000/40)=250, stock=1000 -> Max=250.
        var screen = new TradeScreen(buffer);
        RenderScreen(screen);

        var (rowX, rowY) = ResourceRowCenter(rowSlot: 0);
        screen.OnMouseDown(rowX, rowY);
        RenderScreen(screen);

        var (maxX, maxY) = ScreenCenter(screen.TradeStepperRects.Max);
        screen.OnMouseDown(maxX, maxY);

        Assert.Equal(250, screen.TradeQuantity);
    }

    /// <summary>Max for Buying Fuel is additionally bounded by the remaining tank capacity, and routes through the engine module's Refuel numbers, not the container's.</summary>
    [Fact]
    public void Max_for_fuel_is_also_bounded_by_remaining_tank_capacity()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(BuildTradeSnapshot(
            ImmutableArray.Create(new StationInventoryItemSnapshot("item.fuel", 1000, 5, 1000, TradeItemCategories.Good)),
            playerCredits: 100_000, fuelAmountKg: 470, fuelCapacityKg: 500)); // remaining capacity = 30.
        var screen = new TradeScreen(buffer);
        RenderScreen(screen);

        var (goodX, goodY) = GoodRowCenter(rowSlot: 0);
        screen.OnMouseDown(goodX, goodY);
        RenderScreen(screen);

        var (maxX, maxY) = ScreenCenter(screen.TradeStepperRects.Max);
        screen.OnMouseDown(maxX, maxY);

        Assert.Equal(30, screen.TradeQuantity);
    }

    /// <summary>Sell's Max is bounded by cargo-on-hand/MaxSellableQuantity, with no package rounding (selling is fully per-unit).</summary>
    [Fact]
    public void Max_for_sell_equals_cargo_on_hand_without_package_rounding()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(BuildTradeSnapshot(
            ImmutableArray.Create(new StationInventoryItemSnapshot("item.steel", 1000, 15, 1000, TradeItemCategories.Good)),
            containerCargo: ImmutableArray.Create(new CargoStackSnapshot("item.steel", 137))));
        var screen = new TradeScreen(buffer);
        RenderScreen(screen);

        var (goodX, goodY) = GoodRowCenter(rowSlot: 0);
        screen.OnMouseDown(goodX, goodY);
        RenderScreen(screen);

        var (sellX, sellY) = ScreenCenter(screen.TradeModeToggleRects.Sell);
        screen.OnMouseDown(sellX, sellY);

        var (maxX, maxY) = ScreenCenter(screen.TradeStepperRects.Max);
        screen.OnMouseDown(maxX, maxY);

        Assert.Equal(137, screen.TradeQuantity);
    }

    /// <summary>Max is a no-op (button does nothing) when the container has no cargo space left at all for a non-Fuel Buy.</summary>
    [Fact]
    public void Max_is_a_no_op_when_the_container_has_no_cargo_space_for_a_non_fuel_buy()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(BuildTradeSnapshot(
            ImmutableArray.Create(new StationInventoryItemSnapshot("item.silicon", 1000, 40, 1000, TradeItemCategories.Resource)),
            containerAvailableCapacityKg: 0));
        var screen = new TradeScreen(buffer);
        RenderScreen(screen);

        var (rowX, rowY) = ResourceRowCenter(rowSlot: 0);
        screen.OnMouseDown(rowX, rowY);
        RenderScreen(screen);
        Assert.Equal(1, screen.TradeQuantity);

        var (maxX, maxY) = ScreenCenter(screen.TradeStepperRects.Max);
        screen.OnMouseDown(maxX, maxY);

        Assert.Equal(1, screen.TradeQuantity); // unchanged
        Assert.Equal(0, screen.TradeMaxQuantity);
    }

    /// <summary>Clicking anywhere on the slider's track jumps the quantity directly to the position clicked, linearly interpolated between [1, Max] (Docs/FirstRelease/Screens/Trade.md, "UI-решение: панель действия", step 5).</summary>
    [Fact]
    public void Clicking_the_slider_track_jumps_the_quantity_to_the_clicked_position()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(BuildTradeSnapshot(
            ImmutableArray.Create(new StationInventoryItemSnapshot("item.silicon", 1000, 40, 1000, TradeItemCategories.Resource)),
            playerCredits: 10_000)); // Max = min(10000/40, 1000) = 250.
        var screen = new TradeScreen(buffer);
        RenderScreen(screen);

        var (rowX, rowY) = ResourceRowCenter(rowSlot: 0);
        screen.OnMouseDown(rowX, rowY);
        RenderScreen(screen);
        Assert.Equal(1, screen.TradeQuantity);

        var sliderRect = screen.TradeSliderRect;
        float pl = TradeLayout.PanelLeft(ScreenWidth);
        float pt = TradeLayout.PanelTop(ScreenHeight);
        float midY = pt + sliderRect.MidY;

        // Click at the very left edge of the track — jumps to the minimum quantity (1).
        screen.OnMouseDown(pl + sliderRect.Left, midY);
        Assert.Equal(1, screen.TradeQuantity);

        // Click at the very right edge of the track — jumps to Max (250).
        screen.OnMouseDown(pl + sliderRect.Right, midY);
        Assert.Equal(250, screen.TradeQuantity);

        // Click at the exact midpoint — 1 + round(0.5 * (250 - 1)) = 1 + 125 = 126.
        var (midX, _) = ScreenCenter(sliderRect);
        screen.OnMouseDown(midX, midY);
        Assert.Equal(126, screen.TradeQuantity);
    }

    /// <summary>After the initial click, dragging (mouse held down) continues to update the quantity as the pointer moves — same drag-state shape as the grid scrollbar-thumb drag (Dragging_the_scrollbar_thumb_moves_the_scroll_offset).</summary>
    [Fact]
    public void Dragging_the_slider_continues_to_update_the_quantity_after_the_initial_click()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(BuildTradeSnapshot(
            ImmutableArray.Create(new StationInventoryItemSnapshot("item.silicon", 1000, 40, 1000, TradeItemCategories.Resource)),
            playerCredits: 10_000)); // Max = min(10000/40, 1000) = 250.
        var screen = new TradeScreen(buffer);
        RenderScreen(screen);

        var (rowX, rowY) = ResourceRowCenter(rowSlot: 0);
        screen.OnMouseDown(rowX, rowY);
        RenderScreen(screen);

        var sliderRect = screen.TradeSliderRect;
        float pl = TradeLayout.PanelLeft(ScreenWidth);
        float pt = TradeLayout.PanelTop(ScreenHeight);
        float leftX = pl + sliderRect.Left;
        float rightX = pl + sliderRect.Right;
        float midY = pt + sliderRect.MidY;

        screen.OnMouseDown(leftX, midY);
        Assert.Equal(1, screen.TradeQuantity);

        // Drag continues to update the quantity — must clamp at Max(250) when dragged well
        // past the track's right edge, not overshoot or throw.
        screen.OnMouseMove(rightX + 1000f, midY);
        Assert.Equal(250, screen.TradeQuantity);

        // Drag back past the track's left edge — must clamp at 1, not go to 0 or negative.
        screen.OnMouseMove(leftX - 1000f, midY);
        Assert.Equal(1, screen.TradeQuantity);

        // Releasing ends the drag — further movement must not change the quantity.
        screen.OnMouseUp(rightX, midY);
        screen.OnMouseMove(rightX, midY);
        Assert.Equal(1, screen.TradeQuantity);
    }

    [Fact]
    public async Task Confirm_sends_a_buy_command_to_the_container_module_for_a_non_fuel_item()
    {
        await using var fixture = CreateTradeFixture(BuildTradeSnapshot(
            ImmutableArray.Create(new StationInventoryItemSnapshot("item.silicon", 1000, 40, 1000, TradeItemCategories.Resource))));
        RenderScreen(fixture.Screen);

        var (rowX, rowY) = ResourceRowCenter(rowSlot: 0);
        fixture.Screen.OnMouseDown(rowX, rowY);
        RenderScreen(fixture.Screen);
        Assert.True(fixture.Screen.CanConfirmTrade);

        var (confirmX, confirmY) = ScreenCenter(fixture.Screen.TradeConfirmButtonRect);
        fixture.Screen.OnMouseDown(confirmX, confirmY);

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal(TradeCommandTypes.Buy, command.CommandType);
        Assert.Equal(ShipId, command.ObjectId);
        Assert.Equal(ContainerModuleId, command.ModuleId);
        Assert.Equal("item.silicon", command.ItemTypeId);
        Assert.Equal(1, command.Quantity);
    }

    [Fact]
    public async Task Confirm_sends_a_sell_command_to_the_container_module()
    {
        await using var fixture = CreateTradeFixture(BuildTradeSnapshot(
            ImmutableArray.Create(new StationInventoryItemSnapshot("item.silicon", 1000, 40, 1000, TradeItemCategories.Resource)),
            containerCargo: ImmutableArray.Create(new CargoStackSnapshot("item.silicon", 500))));
        RenderScreen(fixture.Screen);

        var (rowX, rowY) = ResourceRowCenter(rowSlot: 0);
        fixture.Screen.OnMouseDown(rowX, rowY);
        RenderScreen(fixture.Screen);

        var (sellX, sellY) = ScreenCenter(fixture.Screen.TradeModeToggleRects.Sell);
        fixture.Screen.OnMouseDown(sellX, sellY);
        RenderScreen(fixture.Screen);
        Assert.True(fixture.Screen.CanConfirmTrade);

        var (confirmX, confirmY) = ScreenCenter(fixture.Screen.TradeConfirmButtonRect);
        fixture.Screen.OnMouseDown(confirmX, confirmY);

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal(TradeCommandTypes.Sell, command.CommandType);
        Assert.Equal(ContainerModuleId, command.ModuleId);
        Assert.Equal("item.silicon", command.ItemTypeId);
        Assert.Equal(1, command.Quantity);
    }

    [Fact]
    public async Task Confirm_sends_a_refuel_command_to_the_engine_module_for_fuel()
    {
        await using var fixture = CreateTradeFixture(BuildTradeSnapshot(
            ImmutableArray.Create(new StationInventoryItemSnapshot("item.fuel", 1000, 5, 1000, TradeItemCategories.Good))));
        RenderScreen(fixture.Screen);

        var (goodX, goodY) = GoodRowCenter(rowSlot: 0);
        fixture.Screen.OnMouseDown(goodX, goodY);
        RenderScreen(fixture.Screen);
        Assert.True(fixture.Screen.CanConfirmTrade);

        var (confirmX, confirmY) = ScreenCenter(fixture.Screen.TradeConfirmButtonRect);
        fixture.Screen.OnMouseDown(confirmX, confirmY);

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal(TradeCommandTypes.Refuel, command.CommandType);
        Assert.Equal(EngineModuleId, command.ModuleId);
        Assert.Equal("item.fuel", command.ItemTypeId);
        Assert.Equal(1, command.Quantity); // Trading is fully per-unit.
    }

    [Fact]
    public async Task Confirm_is_disabled_and_sends_nothing_when_the_player_cannot_afford_it()
    {
        await using var fixture = CreateTradeFixture(BuildTradeSnapshot(
            ImmutableArray.Create(new StationInventoryItemSnapshot("item.silicon", 1000, 1_000_000, 1000, TradeItemCategories.Resource)),
            playerCredits: 100));
        RenderScreen(fixture.Screen);

        var (rowX, rowY) = ResourceRowCenter(rowSlot: 0);
        fixture.Screen.OnMouseDown(rowX, rowY);
        RenderScreen(fixture.Screen);

        Assert.False(fixture.Screen.CanConfirmTrade);
        Assert.Equal("Trade.ReasonInsufficientPlayerCredits", fixture.Screen.TradeDisabledReasonKey);

        var (confirmX, confirmY) = ScreenCenter(fixture.Screen.TradeConfirmButtonRect);
        fixture.Screen.OnMouseDown(confirmX, confirmY);

        Assert.Empty(fixture.Connection.Commands);
    }

    [Fact]
    public async Task Confirm_is_disabled_and_sends_nothing_when_the_container_has_no_cargo_space()
    {
        await using var fixture = CreateTradeFixture(BuildTradeSnapshot(
            ImmutableArray.Create(new StationInventoryItemSnapshot("item.silicon", 1000, 40, 1000, TradeItemCategories.Resource)),
            containerAvailableCapacityKg: 0));
        RenderScreen(fixture.Screen);

        var (rowX, rowY) = ResourceRowCenter(rowSlot: 0);
        fixture.Screen.OnMouseDown(rowX, rowY);
        RenderScreen(fixture.Screen);

        Assert.False(fixture.Screen.CanConfirmTrade);
        Assert.Equal("Trade.NoCargoSpace", fixture.Screen.TradeDisabledReasonKey);

        var (confirmX, confirmY) = ScreenCenter(fixture.Screen.TradeConfirmButtonRect);
        fixture.Screen.OnMouseDown(confirmX, confirmY);

        Assert.Empty(fixture.Connection.Commands);
    }

    [Fact]
    public async Task Confirm_is_disabled_and_sends_nothing_when_selling_an_item_not_in_cargo()
    {
        await using var fixture = CreateTradeFixture(BuildTradeSnapshot(
            ImmutableArray.Create(new StationInventoryItemSnapshot("item.silicon", 1000, 40, 1000, TradeItemCategories.Resource))));
        RenderScreen(fixture.Screen);

        var (rowX, rowY) = ResourceRowCenter(rowSlot: 0);
        fixture.Screen.OnMouseDown(rowX, rowY);
        RenderScreen(fixture.Screen);

        var (sellX, sellY) = ScreenCenter(fixture.Screen.TradeModeToggleRects.Sell);
        fixture.Screen.OnMouseDown(sellX, sellY);
        RenderScreen(fixture.Screen);

        Assert.False(fixture.Screen.CanConfirmTrade);
        Assert.Equal("Trade.NoneInCargo", fixture.Screen.TradeDisabledReasonKey);

        var (confirmX, confirmY) = ScreenCenter(fixture.Screen.TradeConfirmButtonRect);
        fixture.Screen.OnMouseDown(confirmX, confirmY);

        Assert.Empty(fixture.Connection.Commands);
    }

    /// <summary>A trade command rejection is correlated back via CommandResults (GameSessionHandle.SendTradeCommand's doc comment) and surfaced as the disabled-reason text.</summary>
    [Fact]
    public async Task A_rejected_trade_command_surfaces_its_reason_once_observed_in_a_later_snapshot()
    {
        var snapshot = BuildTradeSnapshot(
            ImmutableArray.Create(new StationInventoryItemSnapshot("item.silicon", 1000, 40, 1000, TradeItemCategories.Resource)));
        await using var fixture = CreateTradeFixture(snapshot);
        RenderScreen(fixture.Screen);

        var (rowX, rowY) = ResourceRowCenter(rowSlot: 0);
        fixture.Screen.OnMouseDown(rowX, rowY);
        RenderScreen(fixture.Screen);

        var (confirmX, confirmY) = ScreenCenter(fixture.Screen.TradeConfirmButtonRect);
        fixture.Screen.OnMouseDown(confirmX, confirmY);
        var sentCommand = Assert.Single(fixture.Connection.Commands);

        // Simulate the engine's next snapshot rejecting the command.
        fixture.Handle.Buffer.Update(snapshot with
        {
            SnapshotSequence = 2,
            CommandResults = ImmutableArray.Create(new CommandResult(
                sentCommand.CommandId, ShipId, ContainerModuleId, TradeCommandTypes.Buy,
                CommandResultStatus.Rejected, EffectiveGameTimeMs: 100,
                ReasonCode: CommandReasonCodes.InsufficientStationStock))
        });

        RenderScreen(fixture.Screen);

        Assert.Equal("Trade.ReasonInsufficientStationStock", fixture.Screen.TradeDisabledReasonKey);
    }

    /// <summary>Records every PlayerCommand sent through it — mirrors CommandsPanelSkeletonTests's RecordingConnection.</summary>
    private sealed class RecordingConnection : IGameSessionConnection
    {
        public List<PlayerCommand> Commands { get; } = [];

        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return ValueTask.CompletedTask;
        }

        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public ValueTask SetObjectInteractionStateAsync(
            string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
