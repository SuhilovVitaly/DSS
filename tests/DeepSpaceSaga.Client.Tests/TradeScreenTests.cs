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
    public void ResourcePrices_and_ResourceCounts_match_the_station_snapshot_in_ResourceNames_order()
    {
        var screen = new TradeScreen(DockedBufferWithSixResources());

        // Row order: Carbon Ore, Ice, Iron Ore, Magnesium Ore, Silicon, Uranium Ore.
        Assert.Equal(new[] { "30", "10", "5", "30", "40", "30" }, screen.ResourcePrices);
        Assert.Equal(new[] { "90", "320", "410", "120", "70", "20" }, screen.ResourceCounts);
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

    /// <summary>Screen-space center of the toolbar's "Test Station" name label (see DockedBuffer).</summary>
    private static (float X, float Y) StationNameCenter()
    {
        var local = StationToolbar.NameLocalRect("Test Station");
        float x = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float y = TradeLayout.PanelTop(ScreenHeight) + local.MidY;
        return (x, y);
    }
}
