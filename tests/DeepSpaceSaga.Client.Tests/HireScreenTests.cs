using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Hire;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// The Hire overlay screen itself (opened from StationScreen's `HIRE` button — see
/// StationScreenTests's Hire-button tests). Placeholder shell only — passenger
/// contracts aren't in the Engine yet, so there's no real contract data to assert on,
/// just the open/close mechanics. Structural twin of TradeScreenTests/StationScreenTests.
/// </summary>
public class HireScreenTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static void RenderScreen(HireScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    [Fact]
    public void Escape_returns_CloseHire()
    {
        var screen = new HireScreen();
        var result = screen.OnKeyDown(Key.Escape);
        Assert.Equal(ScreenEvent.CloseHire, result);
    }

    [Fact]
    public void Exit_button_click_returns_CloseHire()
    {
        var screen = new HireScreen();
        RenderScreen(screen);

        var local = StationToolbar.ExitButtonLocalRect();
        float cx = HireLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = HireLayout.PanelTop(ScreenHeight) + local.MidY;

        var result = screen.OnMouseDown(cx, cy);
        Assert.Equal(ScreenEvent.CloseHire, result);
    }

    [Fact]
    public void Hovering_the_exit_button_reports_interactive()
    {
        var screen = new HireScreen();
        RenderScreen(screen);

        var local = StationToolbar.ExitButtonLocalRect();
        float cx = HireLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = HireLayout.PanelTop(ScreenHeight) + local.MidY;

        Assert.True(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Hovering_food_rations_does_not_report_interactive()
    {
        // The readout is not a button — hovering it only shows a tooltip, it must not
        // trigger the same cursor swap as the name link / exit button.
        var screen = new HireScreen();
        RenderScreen(screen);

        var local = StationToolbar.FoodRationsLocalRect();
        float cx = HireLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = HireLayout.PanelTop(ScreenHeight) + local.MidY;

        Assert.False(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Food_rations_tooltip_only_appears_after_the_configured_hover_delay()
    {
        var screen = new HireScreen();
        RenderScreen(screen);

        var local = StationToolbar.FoodRationsLocalRect();
        float cx = HireLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = HireLayout.PanelTop(ScreenHeight) + local.MidY;

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
        var screen = new HireScreen();
        RenderScreen(screen);

        var local = StationToolbar.CrewLocalRect();
        float cx = HireLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = HireLayout.PanelTop(ScreenHeight) + local.MidY;

        Assert.False(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Crew_tooltip_only_appears_after_the_configured_hover_delay()
    {
        var screen = new HireScreen();
        RenderScreen(screen);

        var local = StationToolbar.CrewLocalRect();
        float cx = HireLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = HireLayout.PanelTop(ScreenHeight) + local.MidY;

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
        var screen = new HireScreen();
        RenderScreen(screen);

        var local = StationToolbar.TokensLocalRect();
        float cx = HireLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = HireLayout.PanelTop(ScreenHeight) + local.MidY;

        Assert.False(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Tokens_tooltip_only_appears_after_the_configured_hover_delay()
    {
        var screen = new HireScreen();
        RenderScreen(screen);

        var local = StationToolbar.TokensLocalRect();
        float cx = HireLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = HireLayout.PanelTop(ScreenHeight) + local.MidY;

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
        var screen = new HireScreen();
        RenderScreen(screen);

        var local = StationToolbar.FuelLocalRect();
        float cx = HireLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = HireLayout.PanelTop(ScreenHeight) + local.MidY;

        Assert.False(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Fuel_tooltip_only_appears_after_the_configured_hover_delay()
    {
        var screen = new HireScreen();
        RenderScreen(screen);

        var local = StationToolbar.FuelLocalRect();
        float cx = HireLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = HireLayout.PanelTop(ScreenHeight) + local.MidY;

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
        var screen = new HireScreen();
        RenderScreen(screen);

        float px = HireLayout.PanelLeft(ScreenWidth) + HireLayout.PanelWidth / 2f;
        float py = HireLayout.PanelTop(ScreenHeight) + HireLayout.PanelHeight / 2f;

        var result = screen.OnMouseDown(px, py);
        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void Click_outside_panel_returns_CloseHire()
    {
        var screen = new HireScreen();
        RenderScreen(screen);

        // Top-left corner of the screen — well outside the centered panel.
        var result = screen.OnMouseDown(2f, 2f);
        Assert.Equal(ScreenEvent.CloseHire, result);
    }

    [Fact]
    public void Right_click_outside_panel_does_not_close()
    {
        var screen = new HireScreen();
        RenderScreen(screen);

        var result = screen.OnMouseDown(2f, 2f, MouseButton.Right);
        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void Station_name_click_returns_NavigateToStation()
    {
        var screen = new HireScreen(DockedBuffer());
        RenderScreen(screen);

        var (x, y) = StationNameCenter();
        Assert.Equal(ScreenEvent.NavigateToStation, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void Hovering_the_station_name_reports_interactive()
    {
        var screen = new HireScreen(DockedBuffer());
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

    /// <summary>Screen-space center of the toolbar's "Test Station" name label (see DockedBuffer).</summary>
    private static (float X, float Y) StationNameCenter()
    {
        var local = StationToolbar.NameLocalRect("Test Station");
        float x = HireLayout.PanelLeft(ScreenWidth) + local.MidX;
        float y = HireLayout.PanelTop(ScreenHeight) + local.MidY;
        return (x, y);
    }
}
