using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Finance;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// The Finance overlay screen itself (opened from GameSessionScreen's Mechanics
/// panel — see MechanicsPanelTests.cs). Placeholder shell only —
/// Money/Trading/StationInventory mechanics aren't in the Engine yet, so there's
/// no real financial data to assert on, just the open/close mechanics.
/// </summary>
public class FinanceScreenTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static void RenderScreen(FinanceScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    [Fact]
    public void Background_image_is_loaded()
    {
        // Regression: the mechanics-window-background-titlebar-1400x900.png asset must
        // resolve at the client's working directory and be registered in the .csproj
        // with CopyToOutputDirectory, or the panel silently falls back to a plain fill.
        Assert.True(FinanceScreen.HasLoadedBackground);
    }

    [Fact]
    public void Escape_returns_CloseFinance()
    {
        var screen = new FinanceScreen();
        var result = screen.OnKeyDown(Key.Escape);
        Assert.Equal(ScreenEvent.CloseFinance, result);
    }

    [Fact]
    public void Exit_button_click_returns_CloseFinance()
    {
        var screen = new FinanceScreen();
        RenderScreen(screen);

        var local = StationToolbar.ExitButtonLocalRect();
        float cx = FinanceLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = FinanceLayout.PanelTop(ScreenHeight) + local.MidY;

        var result = screen.OnMouseDown(cx, cy);
        Assert.Equal(ScreenEvent.CloseFinance, result);
    }

    [Fact]
    public void Hovering_the_exit_button_reports_interactive()
    {
        var screen = new FinanceScreen();
        RenderScreen(screen);

        var local = StationToolbar.ExitButtonLocalRect();
        float cx = FinanceLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = FinanceLayout.PanelTop(ScreenHeight) + local.MidY;

        Assert.True(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Hovering_crew_does_not_report_interactive()
    {
        // The crew readout is not a button — hovering it only shows a tooltip (see
        // StationToolbar), it must not trigger the same cursor swap as the exit button.
        var screen = new FinanceScreen();
        RenderScreen(screen);

        var local = StationToolbar.CrewLocalRect();
        float cx = FinanceLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = FinanceLayout.PanelTop(ScreenHeight) + local.MidY;

        Assert.False(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Click_inside_panel_outside_close_button_returns_None()
    {
        var screen = new FinanceScreen();
        RenderScreen(screen);

        float px = FinanceLayout.PanelLeft(ScreenWidth) + FinanceLayout.PanelWidth / 2f;
        float py = FinanceLayout.PanelTop(ScreenHeight) + FinanceLayout.PanelHeight / 2f;

        var result = screen.OnMouseDown(px, py);
        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void Click_outside_panel_returns_CloseFinance()
    {
        var screen = new FinanceScreen();
        RenderScreen(screen);

        // Top-left corner of the screen — well outside the centered panel.
        var result = screen.OnMouseDown(2f, 2f);
        Assert.Equal(ScreenEvent.CloseFinance, result);
    }

    [Fact]
    public void Right_click_outside_panel_does_not_close()
    {
        var screen = new FinanceScreen();
        RenderScreen(screen);

        var result = screen.OnMouseDown(2f, 2f, MouseButton.Right);
        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void Station_name_click_returns_NavigateToStation()
    {
        var screen = new FinanceScreen(DockedBuffer());
        RenderScreen(screen);

        var (x, y) = StationNameCenter();
        Assert.Equal(ScreenEvent.NavigateToStation, screen.OnMouseDown(x, y));
    }

    [Fact]
    public void Hovering_the_station_name_reports_interactive()
    {
        var screen = new FinanceScreen(DockedBuffer());
        RenderScreen(screen);

        var (x, y) = StationNameCenter();
        Assert.True(screen.OnMouseMove(x, y));
    }

    [Fact]
    public void Station_name_is_absent_and_not_interactive_when_not_docked()
    {
        // Finance is also reachable straight from GameSessionScreen (Ctrl+F) without a
        // docked station — the toolbar shows no name, so nothing there is clickable.
        var screen = new FinanceScreen();
        RenderScreen(screen);

        float px = FinanceLayout.PanelLeft(ScreenWidth) + StationToolbar.NameOffsetX;
        float py = FinanceLayout.PanelTop(ScreenHeight) + StationToolbar.NameOffsetY;
        Assert.False(screen.OnMouseMove(px, py));
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
        float x = FinanceLayout.PanelLeft(ScreenWidth) + local.MidX;
        float y = FinanceLayout.PanelTop(ScreenHeight) + local.MidY;
        return (x, y);
    }
    internal static VoyageFinanceSnapshot Report(string id = "leg", long? net = 123) => new(id, "A", "B", 1, 2,
        VoyageFinanceStates.AwaitingRealization, 1000, 300, false, 40, 200, 50, 150, 0, 0, 0, net,
        [new("item.ice", 7, 999), new("item.water", 2, null)]);
    private static SnapshotBuffer FinanceBuffer(params VoyageFinanceSnapshot[] reports)
    {
        var b = new SnapshotBuffer();
        b.Update(new(1, 0, SimulationSpeed.Speed0, [], VoyageFinances: reports.ToImmutableArray()));
        return b;
    }
    private static string Row(FinanceScreen screen, string key) => screen.VoyageRows.Single(r => r.Label == Localization.Get("Finance." + key)).Value;

    [Theory]
    [InlineData(123L)]
    [InlineData(-123L)]
    [InlineData(0L)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void Finance_shows_authoritative_profit_loss_and_break_even_components(long net)
    {
        var report = Report(net: net);
        var screen = new FinanceScreen(FinanceBuffer(report));
        RenderScreen(screen);
        Assert.Equal(FinanceScreen.MoneyText(net), Row(screen, "NetProfit"));
        Assert.Equal(FinanceScreen.MoneyText(1000), Row(screen, "GrossSales"));
        Assert.Equal(FinanceScreen.MoneyText(300), Row(screen, "CostOfGoodsSold"));
        Assert.Equal(FinanceScreen.MoneyText(40), Row(screen, "RouteFuelCost"));
        // The intentionally inconsistent net proves the Client only displays the supplied value.
        Assert.Equal(report, screen.SelectedReport);
    }

    [Fact]
    public void Finance_shows_unknown_cogs_and_net_as_unavailable_with_gross_sales()
    {
        var report = Report(net: null) with { CostOfGoodsSoldCredits = null, HasUnknownCostOfGoodsSold = true };
        var screen = new FinanceScreen(FinanceBuffer(report));
        Assert.Equal(Localization.Get("Finance.Unavailable"), Row(screen, "CostOfGoodsSold"));
        Assert.Equal(Localization.Get("Finance.Unavailable"), Row(screen, "NetProfit"));
        Assert.Equal(FinanceScreen.MoneyText(1000), Row(screen, "GrossSales"));
        RenderScreen(screen);
    }

    [Fact]
    public void Finance_separates_assessed_paid_and_outstanding_port_fee_and_unsold_assets()
    {
        var screen = new FinanceScreen(FinanceBuffer(Report()));
        Assert.Equal(FinanceScreen.MoneyText(200), Row(screen, "PortFeesAssessed"));
        Assert.Equal(FinanceScreen.MoneyText(50), Row(screen, "PortFeesPaid"));
        Assert.Equal(FinanceScreen.MoneyText(150), Row(screen, "PortFeeDebt"));
        var cargo = screen.VoyageRows.Where(r => r.IsCargo).ToArray();
        Assert.Equal(2, cargo.Length);
        Assert.Contains(FinanceScreen.MoneyText(999), cargo[0].Value);
        Assert.Contains(Localization.Get("Finance.Unavailable"), cargo[1].Value);
        Assert.Equal(FinanceScreen.MoneyText(123), Row(screen, "NetProfit"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Finance_shows_optional_passenger_and_event_rows_only_when_nonzero(bool present)
    {
        var report = Report() with
        {
            EventCostsCredits = present ? 5 : 0,
            PassengerPayoutCredits = present ? 7 : 0,
            PassengerPenaltyCredits = present ? 9 : 0
        };
        var screen = new FinanceScreen(FinanceBuffer(report));
        foreach (string key in new[] { "EventCosts", "PassengerPayout", "PassengerPenalty" })
            Assert.Equal(present, screen.VoyageRows.Any(r => r.Label == Localization.Get("Finance." + key)));
        if (present) Assert.Equal(FinanceScreen.MoneyText(7), Row(screen, "PassengerPayout"));
    }

    [Fact]
    public void Finance_default_snapshot_shows_no_voyages_without_throwing()
    {
        var screen = new FinanceScreen(FinanceBuffer());
        Assert.Empty(screen.VoyageRows);
        Assert.Null(screen.SelectedReport);
        RenderScreen(screen);
        var buffer = new SnapshotBuffer(); buffer.Update(new(1, 0, SimulationSpeed.Speed0, []));
        RenderScreen(new FinanceScreen(buffer));
    }

    [Fact]
    public void Finance_defaults_to_newest_and_supports_selection_and_detail_scroll_within_existing_panel()
    {
        var reports = Enumerable.Range(0, 51).Select(i => Report("leg-" + i, i)).ToArray();
        var buffer = FinanceBuffer(reports);
        var screen = new FinanceScreen(buffer);
        RenderScreen(screen);
        Assert.Equal("leg-50", screen.SelectedReport!.VoyageId);
        float left = FinanceLayout.PanelLeft(ScreenWidth), top = FinanceLayout.PanelTop(ScreenHeight);
        Assert.True(screen.OnMouseMove(left + 45, top + 400));
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(left + 45, top + 400));
        Assert.Equal("leg-41", screen.SelectedReport!.VoyageId);
        screen.OnKeyDown(Key.Up);
        Assert.Equal("leg-40", screen.SelectedReport!.VoyageId);
        var longReport = Report("leg-40") with
        {
            UnsoldCargo = Enumerable.Range(0, 20).Select(i => new VoyageCargoRemainderSnapshot("item-" + i, long.MaxValue, long.MaxValue)).ToImmutableArray(),
            EventCostsCredits = 1,
            PassengerPayoutCredits = 2,
            PassengerPenaltyCredits = 3
        };
        buffer.Update(new(2, 0, SimulationSpeed.Speed0, [], VoyageFinances: [longReport]));
        RenderScreen(screen);
        screen.OnMouseWheel(left + 1000, top + 500, -1);
        Assert.Equal(3, screen.DetailScroll);
        RenderScreen(screen);
        screen.OnMouseWheel(left + 1000, top + 500, 1);
        Assert.Equal(0, screen.DetailScroll);
        RenderScreen(screen);
        if (Environment.GetEnvironmentVariable("DSS_TRADE_RENDER_DIR") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
            using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight); using var canvas = new SKCanvas(bitmap);
            screen.Render(canvas, ScreenWidth, ScreenHeight);
            using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(Path.Combine(directory, "voyage-finance.png"), data.ToArray());
        }
    }
}
