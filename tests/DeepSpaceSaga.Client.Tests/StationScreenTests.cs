using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Station;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// The Station overlay screen itself (opened from GameSessionScreen by left-clicking
/// the station the player ship is docked to — see
/// GameSessionObjectInteractionTests's docked-station-click tests). Placeholder shell:
/// Representatives/Install Drilling Unit aren't in the Engine yet, so there's no
/// real station data to assert on for those; `Undock`, `Trade`, `Hire`, `Finance` and `Contracts`
/// are real buttons — `Trade`/`Hire`/`Contracts` open their own stub screens
/// (TradeScreenTests/HireScreenTests/ContractsScreenTests), `Finance` opens the
/// pre-existing FinanceScreen (FinanceScreenTests). Structural twin of FinanceScreenTests.
/// </summary>
public class StationScreenTests
{
    [Fact]
    public void District_buttons_emit_travel_only_for_a_different_district()
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(new(1, 203_400_000, SimulationSpeed.Speed0, [], CurrentStationDistrict: StationDistrict.Dock));
        var screen = new StationScreen(buffer);
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(600, 475));
        Assert.Equal(ScreenEvent.TravelMarket, screen.OnMouseDown(800, 475));
        Assert.Equal(203_400_000, buffer.Latest!.Snapshot.GameTimeMs);
        Assert.Equal(ScreenEvent.CloseStation, screen.OnKeyDown(Key.Escape));
        if (Environment.GetEnvironmentVariable("DSS_TACTICAL_RENDER_DIR") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(Path.Combine(directory, "station-time.png"));
            data.SaveTo(file);
        }
    }

    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static void RenderScreen(StationScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    [Fact]
    public void Escape_returns_CloseStation()
    {
        var screen = new StationScreen();
        var result = screen.OnKeyDown(Key.Escape);
        Assert.Equal(ScreenEvent.CloseStation, result);
    }

    [Fact]
    public void Exit_button_click_returns_CloseStation()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var local = StationToolbar.ExitButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = StationLayout.PanelTop(ScreenHeight) + local.MidY;

        var result = screen.OnMouseDown(cx, cy);
        Assert.Equal(ScreenEvent.CloseStation, result);
    }

    [Fact]
    public void Hovering_the_exit_button_reports_interactive()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var local = StationToolbar.ExitButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = StationLayout.PanelTop(ScreenHeight) + local.MidY;

        Assert.True(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Hovering_crew_does_not_report_interactive()
    {
        // The crew readout is not a button — hovering it only shows a tooltip (see
        // StationToolbar), it must not trigger the same cursor swap as the exit button.
        var screen = new StationScreen();
        RenderScreen(screen);

        var local = StationToolbar.CrewLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + local.MidX;
        float cy = StationLayout.PanelTop(ScreenHeight) + local.MidY;

        Assert.False(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public async Task Trade_button_click_returns_OpenTrade()
    {
        await using var fixture = new VoyageUiFixture();
        var screen = fixture.Station();
        RenderScreen(screen);

        var hit = StationLayout.HitTest(
            StationLayout.PanelLeft(ScreenWidth) + StationLayout.TradeButtonLocalRect().Left + 1f,
            StationLayout.PanelTop(ScreenHeight) + StationLayout.TradeButtonLocalRect().Top + 1f,
            ScreenWidth, ScreenHeight);
        Assert.Equal(StationButton.Trade, hit);

        var (left, top, right, bottom) = StationLayout.TradeButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        var result = screen.OnMouseDown(cx, cy);
        Assert.Equal(ScreenEvent.OpenTrade, result);
    }

    [Fact]
    public async Task Trade_button_hover_is_reported_interactive()
    {
        await using var fixture = new VoyageUiFixture();
        var screen = fixture.Station();
        RenderScreen(screen);

        var (left, top, right, bottom) = StationLayout.TradeButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        Assert.True(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public async Task Trade_cannot_open_from_stale_station_click_in_flight()
    {
        await using var fixture = new VoyageUiFixture();
        var station = fixture.Station();
        RenderScreen(station);
        var click = VoyageUiFixture.TradeClick();
        fixture.At(null, 0, 2);
        Assert.Equal(ScreenEvent.None, station.OnMouseDown(click.X, click.Y));
        Assert.False(station.OnMouseMove(click.X, click.Y));
        fixture.At("A", 200, 3);
        Assert.False(station.HasValidVisit);
        Assert.Equal(ScreenEvent.None, station.OnMouseDown(click.X, click.Y));
        var fresh = fixture.Station();
        RenderScreen(fresh);
        Assert.Equal(ScreenEvent.OpenTrade, fresh.OnMouseDown(click.X, click.Y));
    }

    [Fact]
    public async Task Departure_removes_trade_and_station_with_one_final_resume()
    {
        await using var fixture = new VoyageUiFixture();
        var stack = NewStack(fixture);
        stack.Push(fixture.Trade());
        fixture.At(null, 0, 2);
        int pops = 0, resumes = 0;
        var navigation = new StationTradeNavigation(() => fixture.Handle);
        await navigation.RemoveInvalidOverlaysAsync(stack, () => fixture.Buffer.Latest?.Snapshot,
            () => { stack.Pop(); pops++; if (stack.Count == 1) resumes++; return Task.CompletedTask; });
        Assert.Equal(2, pops);
        Assert.Equal(1, resumes);
        Assert.Equal(1, stack.Count);
    }

    [Fact]
    public async Task Rejected_departure_preserves_station_and_pause()
    {
        await using var fixture = new VoyageUiFixture();
        var stack = NewStack(fixture);
        var navigation = new StationTradeNavigation(() => fixture.Handle);
        int pops = 0;
        await navigation.RemoveInvalidOverlaysAsync(stack, () => fixture.Buffer.Latest?.Snapshot,
            () => { pops++; stack.Pop(); return Task.CompletedTask; });
        Assert.IsType<StationScreen>(stack.Current);
        Assert.Equal(0, pops);
    }

    [Fact]
    public async Task Arrival_and_return_open_only_current_station_market()
    {
        await using var fixture = new VoyageUiFixture();
        Assert.True(StationTradeNavigation.CanOpen(fixture.Buffer.Latest?.Snapshot));
        var old = fixture.Station();
        fixture.At(null, 0, 2);
        Assert.False(StationTradeNavigation.CanOpen(fixture.Buffer.Latest?.Snapshot));
        fixture.At("B", 300, 3);
        Assert.False(old.HasValidVisit);
        Assert.Equal("B", fixture.Station().OpenedForStationObjectId);
        fixture.At("A", 400, 4);
        Assert.False(old.HasValidVisit);
        Assert.Equal(400, fixture.Station().OpenedAtPortFeeGameTimeMs);
    }

    [Fact]
    public async Task Duplicate_cleanup_while_resume_pending_does_not_pop_twice()
    {
        await using var fixture = new VoyageUiFixture();
        var stack = NewStack(fixture);
        stack.Push(fixture.Trade());
        fixture.At(null, 0, 2);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int pops = 0;
        Task Pop()
        {
            stack.Pop(); pops++;
            return stack.Count == 1 ? gate.Task : Task.CompletedTask;
        }
        var navigation = new StationTradeNavigation(() => fixture.Handle);
        var first = navigation.RemoveInvalidOverlaysAsync(stack, () => fixture.Buffer.Latest?.Snapshot, Pop);
        var duplicate = navigation.RemoveInvalidOverlaysAsync(stack, () => fixture.Buffer.Latest?.Snapshot, Pop);
        Assert.Same(first, duplicate);
        Assert.Equal(2, pops);
        gate.SetResult();
        await Task.WhenAll(first, duplicate);
        Assert.Equal(2, pops);
    }

    [Fact]
    public async Task Context_or_session_change_during_pause_does_not_push_stale_window()
    {
        await using var fixture = new VoyageUiFixture();
        var station = fixture.Station();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool pushed = false;
        GameSessionHandle? currentHandle = fixture.Handle;
        async Task AttemptOpen()
        {
            await gate.Task;
            if (ReferenceEquals(currentHandle, station.OpenedForHandle) && station.HasValidVisit &&
                StationTradeNavigation.CanOpen(fixture.Buffer.Latest?.Snapshot))
                pushed = true;
        }
        var opening = AttemptOpen();
        fixture.At("B", 200, 2);
        gate.SetResult();
        await opening;
        Assert.False(pushed);
        var currentStation = fixture.Station();
        var secondGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task AttemptAfterSessionChange()
        {
            await secondGate.Task;
            if (ReferenceEquals(currentHandle, currentStation.OpenedForHandle) && currentStation.HasValidVisit)
                pushed = true;
        }
        var secondOpening = AttemptAfterSessionChange();
        currentHandle = null;
        secondGate.SetResult();
        await secondOpening;
        Assert.False(pushed);
    }

    [Fact]
    public async Task Unrelated_modal_is_preserved_until_invalid_station_is_exposed()
    {
        await using var fixture = new VoyageUiFixture();
        var stack = NewStack(fixture);
        stack.Push(new DeepSpaceSaga.Client.UI.Screens.GameMenu.GameMenuScreen());
        fixture.At(null, 0, 2);
        int pops = 0;
        var navigation = new StationTradeNavigation(() => fixture.Handle);
        Task Pop() { stack.Pop(); pops++; return Task.CompletedTask; }
        await navigation.RemoveInvalidOverlaysAsync(stack, () => fixture.Buffer.Latest?.Snapshot, Pop);
        Assert.Equal(0, pops);
        stack.Pop();
        await navigation.RemoveInvalidOverlaysAsync(stack, () => fixture.Buffer.Latest?.Snapshot, Pop);
        Assert.Equal(1, pops);
    }

    [Fact]
    public async Task Initially_paused_session_remains_paused_after_cleanup()
    {
        await using var fixture = new VoyageUiFixture();
        await fixture.Handle.SetSpeedAsync(SimulationSpeed.Speed0);
        var savedSpeed = fixture.Buffer.CurrentSpeed;
        var stack = NewStack(fixture);
        fixture.At(null, 0, 2);
        var navigation = new StationTradeNavigation(() => fixture.Handle);
        await navigation.RemoveInvalidOverlaysAsync(stack, () => fixture.Buffer.Latest?.Snapshot,
            async () => { stack.Pop(); await fixture.Handle.SetSpeedAsync(savedSpeed); });
        Assert.Equal(SimulationSpeed.Speed0, fixture.Buffer.CurrentSpeed);
        Assert.Equal([SimulationSpeed.Speed0, SimulationSpeed.Speed0], fixture.Wire.Speeds);
    }

    private static ScreenStack NewStack(VoyageUiFixture fixture)
    {
        var stack = new ScreenStack();
        stack.SetRoot(new StationScreen());
        stack.Push(fixture.Station());
        return stack;
    }

    [Fact]
    public void Hire_button_click_returns_OpenHire()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var hit = StationLayout.HitTest(
            StationLayout.PanelLeft(ScreenWidth) + StationLayout.HireButtonLocalRect().Left + 1f,
            StationLayout.PanelTop(ScreenHeight) + StationLayout.HireButtonLocalRect().Top + 1f,
            ScreenWidth, ScreenHeight);
        Assert.Equal(StationButton.Hire, hit);

        var (left, top, right, bottom) = StationLayout.HireButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        var result = screen.OnMouseDown(cx, cy);
        Assert.Equal(ScreenEvent.OpenHire, result);
    }

    [Fact]
    public void Hire_button_hover_is_reported_interactive()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var (left, top, right, bottom) = StationLayout.HireButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        Assert.True(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Finance_button_click_returns_OpenFinance()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var hit = StationLayout.HitTest(
            StationLayout.PanelLeft(ScreenWidth) + StationLayout.FinanceButtonLocalRect().Left + 1f,
            StationLayout.PanelTop(ScreenHeight) + StationLayout.FinanceButtonLocalRect().Top + 1f,
            ScreenWidth, ScreenHeight);
        Assert.Equal(StationButton.Finance, hit);

        var (left, top, right, bottom) = StationLayout.FinanceButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        var result = screen.OnMouseDown(cx, cy);
        Assert.Equal(ScreenEvent.OpenFinance, result);
    }

    [Fact]
    public void Finance_button_hover_is_reported_interactive()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var (left, top, right, bottom) = StationLayout.FinanceButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        Assert.True(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Contracts_button_click_returns_OpenContracts()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var hit = StationLayout.HitTest(
            StationLayout.PanelLeft(ScreenWidth) + StationLayout.ContractsButtonLocalRect().Left + 1f,
            StationLayout.PanelTop(ScreenHeight) + StationLayout.ContractsButtonLocalRect().Top + 1f,
            ScreenWidth, ScreenHeight);
        Assert.Equal(StationButton.Contracts, hit);

        var (left, top, right, bottom) = StationLayout.ContractsButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        var result = screen.OnMouseDown(cx, cy);
        Assert.Equal(ScreenEvent.OpenContracts, result);
    }

    [Fact]
    public void Contracts_button_hover_is_reported_interactive()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var (left, top, right, bottom) = StationLayout.ContractsButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        Assert.True(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Undock_button_click_returns_Undock()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var hit = StationLayout.HitTest(
            StationLayout.PanelLeft(ScreenWidth) + StationLayout.UndockButtonLocalRect().Left + 1f,
            StationLayout.PanelTop(ScreenHeight) + StationLayout.UndockButtonLocalRect().Top + 1f,
            ScreenWidth, ScreenHeight);
        Assert.Equal(StationButton.Undock, hit);

        var (left, top, right, bottom) = StationLayout.UndockButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        Assert.Equal(ScreenEvent.Undock, screen.OnMouseDown(cx, cy));
    }

    [Fact]
    public void Map_departure_requires_a_selected_current_station_route()
    {
        var buffer = new SnapshotBuffer();
        AuthoritativeSnapshot AtStation(string stationId, string destination) =>
            new(1, 0, SimulationSpeed.Speed0,
                [new ObjectMotionSnapshot("ship", 0, 0, 0, 0)
                    { IsDocked = true, DockedStationObjectId = stationId }],
                PlayerShipObjectId: "ship",
                Voyage: new VoyageSnapshot(VoyagePhases.Docked,
                    RouteOptions: [new VoyageRouteOptionSnapshot(destination, destination, 1000, "Short")]));
        buffer.Update(AtStation("A", "B"));
        var screen = new StationScreen(buffer);
        RenderScreen(screen);
        screen.OnActivated();
        var (left, top, right, bottom) = StationLayout.UndockButtonLocalRect();
        float undockX = StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float undockY = StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;
        Assert.Equal(ScreenEvent.Undock, screen.OnMouseDown(undockX, undockY));

        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(
            StationLayout.PanelLeft(ScreenWidth) + 410,
            StationLayout.PanelTop(ScreenHeight) + 450));
        Assert.Equal("B", screen.SelectedDestinationId);
        Assert.Equal(ScreenEvent.Undock, screen.OnMouseDown(undockX, undockY));

        buffer.Update(AtStation("C", "D"));
        Assert.Equal("D", screen.SelectedDestinationId);
        Assert.Equal(ScreenEvent.Undock, screen.OnMouseDown(undockX, undockY));
    }

    [Fact]
    public void Undock_button_hover_is_reported_interactive()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var (left, top, right, bottom) = StationLayout.UndockButtonLocalRect();
        float cx = StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2f;
        float cy = StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2f;

        Assert.True(screen.OnMouseMove(cx, cy));
    }

    [Fact]
    public void Trade_Hire_Finance_and_Contracts_buttons_do_not_overlap()
    {
        var buttons = new[]
        {
            StationLayout.TradeButtonLocalRect(),
            StationLayout.HireButtonLocalRect(),
            StationLayout.FinanceButtonLocalRect(),
            StationLayout.ContractsButtonLocalRect(),
            StationLayout.UndockButtonLocalRect(),
        };

        for (int i = 0; i < buttons.Length; i++)
        {
            for (int j = i + 1; j < buttons.Length; j++)
            {
                var a = buttons[i];
                var b = buttons[j];
                Assert.True(a.Bottom <= b.Top || b.Bottom <= a.Top);
            }
        }
    }

    [Fact]
    public void Click_inside_panel_outside_close_button_returns_None()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        float px = StationLayout.PanelLeft(ScreenWidth) + StationLayout.PanelWidth / 2f;
        float py = StationLayout.PanelTop(ScreenHeight) + StationLayout.PanelHeight / 2f;

        var result = screen.OnMouseDown(px, py);
        Assert.Equal(ScreenEvent.None, result);
    }

    [Fact]
    public void Click_outside_panel_returns_CloseStation()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        // Top-left corner of the screen — well outside the centered panel.
        var result = screen.OnMouseDown(2f, 2f);
        Assert.Equal(ScreenEvent.CloseStation, result);
    }

    [Fact]
    public void Right_click_outside_panel_does_not_close()
    {
        var screen = new StationScreen();
        RenderScreen(screen);

        var result = screen.OnMouseDown(2f, 2f, MouseButton.Right);
        Assert.Equal(ScreenEvent.None, result);
    }
    [Theory]
    [InlineData(CommandReasonCodes.VoyageOutstandingDebt)]
    [InlineData(CommandReasonCodes.VoyageInsufficientFuel)]
    [InlineData(CommandReasonCodes.VoyageDestinationUnavailable)]
    public void Blocked_route_can_be_selected_for_reason_without_departure(string reason)
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(new(1, 0, SimulationSpeed.Speed0,
            [new ObjectMotionSnapshot("ship", 0, 0, 0, 0) { IsDocked = true, DockedStationObjectId = "A" }],
            PlayerShipObjectId: "ship", Voyage: new(VoyagePhases.Docked, RouteOptions:
            [new("blocked", "Blocked", 3600000, "Short", false, reason),
             new("open", "Open", 7200000, "Medium")])));
        var screen = new StationScreen(buffer);
        screen.OnActivated();
        RenderScreen(screen);
        Assert.Equal("open", screen.SelectedDestinationId);
        screen.OnMouseDown(StationLayout.PanelLeft(ScreenWidth) + 410, StationLayout.PanelTop(ScreenHeight) + 450);
        Assert.Equal("blocked", screen.SelectedVoyageDestinationObjectId);
        Assert.Null(screen.SelectedDestinationId);
        var (left, top, right, bottom) = StationLayout.UndockButtonLocalRect();
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(StationLayout.PanelLeft(ScreenWidth) + (left + right) / 2,
            StationLayout.PanelTop(ScreenHeight) + (top + bottom) / 2));
        Assert.DoesNotContain(reason, StationScreen.DepartureReasonText(reason));
        RenderScreen(screen);
        Assert.Equal("blocked", screen.SelectedVoyageDestinationObjectId);
    }

    [Fact]
    public void Selection_is_preserved_then_falls_back_when_option_disappears()
    {
        var buffer = new SnapshotBuffer();
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0,
            [new ObjectMotionSnapshot("ship", 0, 0, 0, 0) { IsDocked = true, DockedStationObjectId = "A" }],
            PlayerShipObjectId: "ship", Voyage: new(VoyagePhases.Docked,
                RouteOptions: [new("B", "Beta", 3600000, "Short"), new("C", "Gamma", 7200000, "Medium")]));
        buffer.Update(snapshot);
        var screen = new StationScreen(buffer);
        RenderScreen(screen);
        screen.OnMouseDown(StationLayout.PanelLeft(ScreenWidth) + 410, StationLayout.PanelTop(ScreenHeight) + 490);
        Assert.Equal("C", screen.SelectedDestinationId);
        buffer.Update(snapshot with { SnapshotSequence = 2 });
        Assert.Equal("C", screen.SelectedDestinationId);
        buffer.Update(snapshot with
        {
            SnapshotSequence = 3,
            Voyage = snapshot.Voyage! with
            { RouteOptions = [new("B", "Beta", 3600000, "Short")] }
        });
        Assert.Equal("B", screen.SelectedDestinationId);
        Assert.Equal("Beta  Short  ETA 1:00:00", StationScreen.RouteOptionText(snapshot.Voyage!.RouteOptions[0]));
    }
}
