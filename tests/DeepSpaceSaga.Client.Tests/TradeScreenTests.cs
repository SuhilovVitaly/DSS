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

        // Left/right edges mirror the grids' 15px margins from the Trade panel's own edges.
        Assert.Equal(977f, upper.Left);
        Assert.Equal(977f, lower.Left);
        Assert.Equal(1385f, upper.Right);
        Assert.Equal(1385f, lower.Right);
    }

    /// <summary>The upper right-hand panel spans the same vertical range as the Resources and Goods grids combined (top of Resources to bottom of Goods).</summary>
    [Fact]
    public void Upper_right_panel_height_matches_the_resources_and_goods_grids_combined()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        var (upper, _) = screen.RightPanels;

        // Height still matches the Resources+Goods grids combined (443), but the panel is
        // vertically centered on the Resources/Goods white outline frames — (90 + 539) / 2 =
        // 314.5 — not on the grids' own (differently-offset) top/bottom.
        Assert.Equal(443f, upper.Height);
        Assert.Equal(93f, upper.Top);
        Assert.Equal(536f, upper.Bottom);
    }

    /// <summary>The lower right-hand panel matches the Modules grid's height and is centered on the Modules white outline frame.</summary>
    [Fact]
    public void Lower_right_panel_height_and_position_match_the_modules_grid()
    {
        var screen = new TradeScreen();
        RenderScreen(screen);

        var (_, lower) = screen.RightPanels;

        // Centered on the Modules white outline frame (588 + 788) / 2 = 688, not on the
        // Modules grid's own top/bottom.
        Assert.Equal(194f, lower.Height);
        Assert.Equal(591f, lower.Top);
        Assert.Equal(785f, lower.Bottom);
    }
}
