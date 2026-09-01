using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Contracts;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// The Contracts overlay screen itself (opened from StationScreen's `CONTRACTS` button —
/// see StationScreenTests's Contracts-button tests). Split out of the original `Hire`
/// screen (passenger contracts vs. crew hiring — see HireScreenTests). Placeholder shell
/// only — passenger contracts aren't in the Engine yet, so there's no real contract data
/// to assert on, just the open/close mechanics. Structural twin of HireScreenTests.
/// </summary>
public class ContractsScreenTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static void RenderScreen(ContractsScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    [Fact]
    public void Escape_returns_CloseContracts()
    {
        var screen = new ContractsScreen();
        var result = screen.OnKeyDown(Key.Escape);
        Assert.Equal(ScreenEvent.CloseContracts, result);
    }

    [Fact]
    public void Exit_button_click_returns_CloseContracts()
    {
        var screen = new ContractsScreen();
        RenderScreen(screen);

        var local = StationToolbar.ExitButtonLocalRect();
        float cx = ContractsLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = ContractsLayout.PanelTop(ScreenHeight) + local.MidY;

        var result = screen.OnMouseDown(cx, cy);
        Assert.Equal(ScreenEvent.CloseContracts, result);
    }

    [Fact]
    public void Hovering_the_exit_button_reports_interactive()
    {
        var screen = new ContractsScreen();
        RenderScreen(screen);

        var local = StationToolbar.ExitButtonLocalRect();
        float cx = ContractsLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = ContractsLayout.PanelTop(ScreenHeight) + local.MidY;

        Assert.True(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Hovering_crew_does_not_report_interactive()
    {
        // The crew readout is not a button — hovering it only shows a tooltip (see
        // StationToolbar), it must not trigger the same cursor swap as the exit button.
        var screen = new ContractsScreen();
        RenderScreen(screen);

        var local = StationToolbar.CrewLocalRect();
        float cx = ContractsLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = ContractsLayout.PanelTop(ScreenHeight) + local.MidY;

        Assert.False(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Click_inside_panel_outside_close_button_returns_None()
    {
        var screen = new ContractsScreen();
        RenderScreen(screen);

        float px = ContractsLayout.PanelLeft(ScreenWidth) + ContractsLayout.PanelWidth / 2f;
        float py = ContractsLayout.PanelTop(ScreenHeight) + ContractsLayout.PanelHeight / 2f;

        var result = screen.OnMouseDown(px, py);
        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void Click_outside_panel_returns_CloseContracts()
    {
        var screen = new ContractsScreen();
        RenderScreen(screen);

        // Top-left corner of the screen — well outside the centered panel.
        var result = screen.OnMouseDown(2f, 2f);
        Assert.Equal(ScreenEvent.CloseContracts, result);
    }

    [Fact]
    public void Right_click_outside_panel_does_not_close()
    {
        var screen = new ContractsScreen();
        RenderScreen(screen);

        var result = screen.OnMouseDown(2f, 2f, MouseButton.Right);
        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void Station_name_click_returns_NavigateToStation()
    {
        var screen = new ContractsScreen(DockedBuffer());
        RenderScreen(screen);

        var (x, y) = StationNameCenter();
        Assert.Equal(ScreenEvent.NavigateToStation, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void Hovering_the_station_name_reports_interactive()
    {
        var screen = new ContractsScreen(DockedBuffer());
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
        float x = ContractsLayout.PanelLeft(ScreenWidth) + local.MidX;
        float y = ContractsLayout.PanelTop(ScreenHeight) + local.MidY;
        return (x, y);
    }
}
