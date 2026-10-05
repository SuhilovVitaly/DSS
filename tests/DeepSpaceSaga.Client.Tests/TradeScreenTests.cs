using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Assets;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>Shared station toolbar, tooltips and navigation remain unchanged by the trading redesign.</summary>
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

    private static TradeJournal.Entry ReceiptEntry(long requested = 3, long actual = 3, long total = 431,
        string? limit = null)
    {
        var receipt = new TradeExecutionReceipt("old-station", "item.energy-cells", "quote", 7, 8,
            requested, actual, total, limit is null ? [] : [limit]);
        var result = new CommandResult("command", "ship", "hold", TradeCommandTypes.Sell,
            CommandResultStatus.Executed, 0, ExecutedQuantity: 99, TradeReceipt: receipt);
        return new("command", "item.energy-cells", "hold", TradeMode.Sell, requested, 50, result, "Hold 01",
            "quote", 7, total, actual);
    }

    private static string ReceiptText(TradeJournal.Entry entry)
    {
        var receipt = entry.ConfirmedReceipt!;
        string item = TradeItemPresentation.ItemDisplayName(entry.ItemId);
        string actual = TradeItemPresentation.FormatQuantity(entry.ItemId, receipt.ExecutedQuantity);
        string total = receipt.TotalCredits.ToString("N0", System.Globalization.CultureInfo.CurrentCulture);
        return receipt.ExecutedQuantity == receipt.RequestedQuantity
            ? TradeScreen.F("SuccessResult", item, actual, total)
            : TradeScreen.F("PartialResult", item, actual, total,
                TradeItemPresentation.FormatQuantity(entry.ItemId, receipt.RequestedQuantity!.Value)) + " · " +
                string.Join(" · ", receipt.LimitReasons.Select(reason => TradeScreen.L(TradeQuote.QuoteReasonKey(reason))));
    }

    [Fact]
    public void History_uses_receipt_total_when_curve_differs_from_unit_price_times_quantity()
    {
        var entry = ReceiptEntry();
        var screen = new TradeScreen();
        Assert.NotEqual(entry.UnitPrice * entry.RequestedQuantity, entry.Result!.TradeReceipt!.TotalCredits);
        Assert.Equal(ReceiptText(entry), screen.EntryMessage(entry));
        Assert.Equal(screen.EntryMessage(entry), screen.EntryMessage(entry with { UnitPrice = 9999 }));
    }

    [Theory]
    [InlineData(CommandReasonCodes.StationBudgetExceeded)]
    [InlineData(CommandReasonCodes.StationCapacityExceeded)]
    public void Partial_receipt_shows_actual_requested_total_and_capacity_or_budget_reason(string reason)
    {
        var entry = ReceiptEntry(requested: 10, actual: 3, limit: reason);
        Assert.Equal(ReceiptText(entry), new TradeScreen().EntryMessage(entry));
    }

    [Fact]
    public void Duplicate_receipt_and_track_do_not_duplicate_history()
    {
        var entry = ReceiptEntry();
        var pending = entry with { Result = null };
        var journal = new TradeJournal();
        var buffer = new SnapshotBuffer();
        journal.Track(pending);
        journal.Track(pending);
        Assert.Single(journal.Entries);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [], CommandResults: [entry.Result!]));
        journal.Refresh(buffer);
        journal.Track(pending);
        buffer.Update(new(2, 0, SimulationSpeed.Speed0, [], CommandResults: [entry.Result!]));
        journal.Refresh(buffer);
        Assert.Equal(entry.Result, Assert.Single(journal.Entries).Result);
        Assert.False(journal.IsPending);
        for (int i = 0; i < 51; i++) journal.Track(entry with { CommandId = $"other-{i}" });
        Assert.Equal(50, journal.Entries.Count);
        Assert.Equal("other-1", journal.Entries[0].CommandId);
        Assert.Equal("other-50", journal.Latest!.CommandId);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("command")]
    [InlineData("module")]
    [InlineData("type")]
    [InlineData("item")]
    [InlineData("quote")]
    [InlineData("revision")]
    [InlineData("requested")]
    [InlineData("station")]
    [InlineData("result-revision")]
    [InlineData("zero")]
    [InlineData("excess")]
    [InlineData("negative-total")]
    [InlineData("changed-total")]
    [InlineData("changed-executable")]
    [InlineData("partial-buy")]
    [InlineData("partial-without-reason")]
    public void Missing_or_mismatched_receipt_never_invents_confirmed_total(string fault)
    {
        var entry = ReceiptEntry();
        var result = entry.Result!;
        var receipt = result.TradeReceipt!;
        entry = fault switch
        {
            "missing" => entry with { Result = result with { TradeReceipt = null } },
            "command" => entry with { Result = result with { CommandId = "foreign" } },
            "module" => entry with { Result = result with { ModuleId = "foreign" } },
            "type" => entry with { Result = result with { CommandType = TradeCommandTypes.Buy } },
            "item" => entry with { Result = result with { TradeReceipt = receipt with { ItemTypeId = "item.water" } } },
            "quote" => entry with { Result = result with { TradeReceipt = receipt with { QuoteId = "foreign" } } },
            "revision" => entry with { Result = result with { TradeReceipt = receipt with { QuotedMarketRevision = 6 } } },
            "requested" => entry with { Result = result with { TradeReceipt = receipt with { RequestedQuantity = 4 } } },
            "station" => entry with { Result = result with { TradeReceipt = receipt with { StationObjectId = "" } } },
            "result-revision" => entry with { Result = result with { TradeReceipt = receipt with { ResultMarketRevision = 7 } } },
            "zero" => entry with { Result = result with { TradeReceipt = receipt with { ExecutedQuantity = 0 } } },
            "excess" => entry with { Result = result with { TradeReceipt = receipt with { ExecutedQuantity = 4 } } },
            "negative-total" => entry with { Result = result with { TradeReceipt = receipt with { TotalCredits = -1 } } },
            "changed-total" => entry with { Result = result with { TradeReceipt = receipt with { TotalCredits = 432 } } },
            "changed-executable" => entry with
            {
                Result = result with
                {
                    TradeReceipt = receipt with
                    {
                        ExecutedQuantity = 2,
                        LimitReasons = [CommandReasonCodes.StationBudgetExceeded]
                    }
                }
            },
            "partial-buy" => ReceiptEntry(10, 3, limit: CommandReasonCodes.StationBudgetExceeded) with
            {
                Mode = TradeMode.Buy,
                Result = result with
                {
                    CommandType = TradeCommandTypes.Buy,
                    TradeReceipt = receipt with { RequestedQuantity = 10, LimitReasons = [CommandReasonCodes.StationBudgetExceeded] }
                }
            },
            _ => ReceiptEntry(10, 3)
        };
        Assert.Equal(TradeScreen.L("ReceiptUnavailable"), new TradeScreen().EntryMessage(entry));
        // Refresh must not silently accept a foreign result as a financial success either.
        var journal = new TradeJournal();
        journal.Track(entry with { Result = null });
        var buffer = new SnapshotBuffer();
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [], CommandResults: [entry.Result!]));
        journal.Refresh(buffer);
        Assert.Null(Assert.Single(journal.Entries).ConfirmedReceipt);
    }

    [Fact]
    public void Legacy_rejection_keeps_its_reason_without_a_receipt()
    {
        var entry = ReceiptEntry();
        entry = entry with
        {
            Result = entry.Result! with
            {
                Status = CommandResultStatus.Rejected,
                ReasonCode = CommandReasonCodes.StaleQuote,
                TradeReceipt = null
            }
        };
        Assert.Equal(TradeScreen.F("Rejected", TradeItemPresentation.ItemDisplayName(entry.ItemId), TradeScreen.L("QuoteStale")),
            new TradeScreen().EntryMessage(entry));
    }

    [Fact]
    public async Task Closing_and_reopening_trade_preserves_pending_then_completed_entry()
    {
        await using var f = new RealTradeFixture();
        var quote = f.Prepare(TradeMode.Buy, 3);
        f.Submit();
        Assert.True(f.Screen.IsPending);
        f.Screen.OnDeactivated();
        f.Reopen();
        Assert.True(f.Screen.IsPending);
        f.Screen.OnDeactivated();
        f.Publish();
        f.Reopen();
        var entry = Assert.Single(f.Screen.History);
        Assert.False(f.Screen.IsPending);
        Assert.Equal(quote.TotalCredits, entry.ConfirmedReceipt!.TotalCredits);
        string message = f.Screen.EntryMessage(entry);
        // A later station/context must not invalidate or re-price a historical receipt.
        var snapshot = f.Handle.Buffer.Latest!.Snapshot;
        f.Handle.Buffer.Update(snapshot with { DockedStationTrade = null });
        f.Reopen();
        Assert.Equal(message, f.Screen.EntryMessage(Assert.Single(f.Screen.History)));
        Assert.Single(f.Connection.Commands);
    }

    [Fact]
    public async Task Existing_exit_quantity_controls_and_modal_snapshot_pause_are_preserved()
    {
        await using var f = new RealTradeFixture();
        f.Prepare(TradeMode.Buy, 1);
        ClickTrade(f.Screen, TradeLayout.Plus);
        Assert.Equal(2, f.Screen.Model.Quantity);
        ClickTrade(f.Screen, TradeLayout.Minus);
        Assert.Equal(1, f.Screen.Model.Quantity);
        for (int i = 0; i < 3; i++) RenderScreen(f.Screen);
        Assert.Equal(ScreenEvent.CloseTrade, f.Screen.OnKeyDown(Key.Escape));
        f.Publish();
        Assert.Equal(SimulationSpeed.Speed0, f.Handle.Buffer.CurrentSpeed);
        Assert.Equal(0, f.Handle.Buffer.Latest!.Snapshot.GameTimeMs);
        Assert.Equal(0, f.Handle.Buffer.Latest.Snapshot.MotionTimeMs);
        Assert.Empty(f.Connection.Commands);
        Assert.Empty(f.Connection.SpeedChanges);
    }

    [Fact]
    public async Task Real_quote_execution_receipt_and_history_agree_for_buy_partial_sell_and_refuel()
    {
        await using var f = new RealTradeFixture();
        long initial = f.Balance;
        var buy = f.Execute(TradeMode.Buy, 3);
        Assert.Equal(initial - buy.TotalCredits, f.Balance);
        var fullSell = f.Prepare(TradeMode.Sell, 3);
        Assert.Null(fullSell.DisabledReason);
        long prefix = fullSell.Curve[0].UnitPriceCredits;
        var save = f.Engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        f.Engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId == "SPC-0002"
                    ? o with { MarketBudgetCredits = prefix, Credits = Math.Max(prefix, o.Credits ?? 0) } : o).ToArray()
            }
        });
        f.Publish();
        f.Reopen();
        var partial = f.Execute(TradeMode.Sell, 3);
        Assert.Equal(1, partial.ExecutedQuantity);
        Assert.Equal(3, partial.RequestedQuantity);
        Assert.Equal(prefix, partial.TotalCredits);
        Assert.Contains(CommandReasonCodes.StationBudgetExceeded, partial.LimitReasons);
        Assert.Equal(initial - buy.TotalCredits + prefix, f.Balance);
        long beforeFuel = f.Balance;
        var refuel = f.Execute(TradeMode.Refuel, 3);
        Assert.Equal(beforeFuel - refuel.TotalCredits, f.Balance);
        Assert.Equal(3, refuel.ExecutedQuantity);
        Assert.Equal(3, f.Screen.History.Count);
        Assert.NotEqual(buy.TotalCredits, partial.TotalCredits);
        Assert.All(f.Screen.History, entry => Assert.Equal(ReceiptText(entry), f.Screen.EntryMessage(entry)));
        Assert.Equal(0, f.Handle.Buffer.Latest!.Snapshot.GameTimeMs);
        Assert.Equal(0, f.Handle.Buffer.Latest.Snapshot.MotionTimeMs);
    }

    [Fact]
    public async Task Immediate_roundtrip_and_replayed_command_do_not_increase_balance()
    {
        await using var f = new RealTradeFixture();
        long initial = f.Balance;
        var buy = f.Execute(TradeMode.Buy, 3);
        var sell = f.Execute(TradeMode.Sell, 3);
        Assert.Equal(3, sell.ExecutedQuantity);
        Assert.True(sell.TotalCredits < buy.TotalCredits);
        Assert.Equal(initial - buy.TotalCredits + sell.TotalCredits, f.Balance);
        long after = f.Balance;
        string[] messages = f.Screen.History.Select(f.Screen.EntryMessage).ToArray();
        foreach (var command in f.Connection.Commands.ToArray())
        {
            await f.Connection.SendCommandAsync(command);
            f.Publish();
            RenderScreen(f.Screen);
        }
        f.Reopen();
        Assert.Equal(after, f.Balance);
        Assert.Equal(2, f.Screen.History.Count);
        Assert.Equal(messages, f.Screen.History.Select(f.Screen.EntryMessage));
    }

    private static void ClickTrade(TradeScreen screen, SKRect rectangle) => screen.OnMouseDown(
        TradeLayout.PanelLeft(ScreenWidth) + rectangle.MidX, TradeLayout.PanelTop(ScreenHeight) + rectangle.MidY);

    private sealed class RealTradeFixture : IAsyncDisposable
    {
        internal SimulationEngine Engine { get; }
        internal EngineTradeConnection Connection { get; }
        internal GameSessionHandle Handle { get; }
        internal TradeScreen Screen { get; private set; } = null!;
        private ulong _sequence;
        internal long Balance => Handle.Buffer.Latest!.Snapshot.PlayerCredits;

        internal RealTradeFixture()
        {
            string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
            string client = Path.Combine(root, "src", "DeepSpaceSaga.Client");
            var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(client, "Settings.json"), out _, out _);
            var source = ScenarioLoader.LoadFromFile(Path.Combine(client, "Scenarios", "Default", "scenario.json"));
            var station = source.GameState.SpaceObjects.Single(o => o.ObjectId == "SPC-0002") with
            {
                MarketProfileId = "market.industrial",
                MarketProfileFingerprint = null,
                Inventory = null,
                Credits = null,
                StationSize = "Medium",
                ProducingModules = [],
                Events = []
            };
            var ship = source.GameState.SpaceObjects.Single(o => o.ObjectId == source.GameState.PlayerShipObjectId) with
            {
                IsDocked = true,
                DockedStationObjectId = station.ObjectId,
                PositionX = station.PositionX + 1,
                PositionY = station.PositionY + 1,
                SpeedMps = 0,
                MovementType = "Stationary"
            };
            var scenario = new ScenarioFile(new("quoted-trade-test", "Quoted trade test"),
                new(0, "Speed0", ship.ObjectId, null, [ship, station], MasterSeed: 42, PlayerTokens: 1000000));
            Engine = new(registry, [], new SimulationClock(SimulationSpeed.Speed0, () => 0));
            Engine.LoadScenario(scenario);
            Connection = new(Engine);
            Handle = new(Connection);
            Publish();
            Reopen();
        }

        internal void Publish() => Handle.Buffer.Update(Engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0)
            with
        { SnapshotSequence = ++_sequence });

        internal void Reopen()
        {
            Screen?.OnDeactivated();
            Screen = new(Handle.Buffer, Handle);
            Screen.OnActivated();
            RenderScreen(Screen);
        }

        internal TradeQuoteSnapshot Prepare(TradeMode mode, long quantity)
        {
            Screen.Model.SetMode(mode);
            string command = TradeQuote.CommandType(mode);
            var module = Handle.Buffer.Latest!.Snapshot.InstalledModules.First(m => m.CommandTypeIds.Contains(command));
            Screen.Model.SelectModule(module.ModuleId);
            Screen.Model.Select(mode == TradeMode.Refuel ? "item.fuel" : "item.energy-cells");
            Screen.Model.Quantity = quantity;
            RenderScreen(Screen);
            Assert.True(Screen.CanConfirm, Screen.QuoteMessage);
            return Screen.Model.AuthoritativeQuote!;
        }

        internal void Submit()
        {
            Screen.OnKeyDown(Key.Enter);
            Screen.OnKeyUp(Key.Enter);
            Assert.Null(Handle.Failure);
        }

        internal TradeExecutionReceipt Execute(TradeMode mode, long quantity)
        {
            var quote = Prepare(mode, quantity);
            var before = Screen.Model.Module!;
            long cargoBefore = Screen.Model.Cargo(quote.ItemTypeId);
            int count = Connection.Commands.Count;
            Submit();
            Assert.Equal(count + 1, Connection.Commands.Count);
            var command = Connection.Commands[^1];
            Assert.Equal((quote.QuoteId, quote.MarketRevision, quantity),
                (command.QuoteId, command.MarketRevision!.Value, command.Quantity!.Value));
            Publish();
            RenderScreen(Screen);
            var entry = Screen.History[^1];
            Assert.Equal(CommandResultStatus.Executed, entry.Result!.Status);
            var receipt = Assert.IsType<TradeExecutionReceipt>(entry.ConfirmedReceipt);
            Assert.Equal((quote.ExecutableQuantity, quote.TotalCredits), (receipt.ExecutedQuantity, receipt.TotalCredits));
            Assert.Equal(ReceiptText(entry), Screen.EntryMessage(entry));
            var after = Handle.Buffer.Latest!.Snapshot.InstalledModules.Single(m => m.ModuleId == quote.ModuleId);
            if (mode == TradeMode.Refuel)
                Assert.Equal(before.FuelAmountKg + receipt.ExecutedQuantity, after.FuelAmountKg);
            else
                Assert.Equal(cargoBefore + (mode == TradeMode.Sell ? -receipt.ExecutedQuantity : receipt.ExecutedQuantity),
                    after.Cargo.FirstOrDefault(c => c.ItemTypeId == quote.ItemTypeId)?.Quantity ?? 0);
            return receipt;
        }

        public async ValueTask DisposeAsync()
        {
            Screen.OnDeactivated();
            await Handle.DisposeAsync();
            Engine.Dispose();
        }
    }

    private sealed class EngineTradeConnection(SimulationEngine engine) : IGameSessionConnection
    {
        internal List<PlayerCommand> Commands { get; } = [];
        internal List<SimulationSpeed> SpeedChanges { get; } = [];
        public ValueTask<TradeQuoteSnapshot> GetTradeQuoteAsync(TradeQuoteRequest request, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(engine.GetTradeQuote(request));
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            engine.ReceiveCommand(command);
            return ValueTask.CompletedTask;
        }
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default)
        { SpeedChanges.Add(speed); return ValueTask.CompletedTask; }
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            yield break;
        }
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

    private static (float X, float Y) StationNameCenter()
    {
        var local = StationToolbar.NameLocalRect("Test Station");
        float x = TradeLayout.PanelLeft(ScreenWidth) + local.MidX;
        float y = TradeLayout.PanelTop(ScreenHeight) + local.MidY;
        return (x, y);
    }
    private static StationMarketEventSnapshot EventSnapshot(string id = "A", long start = 0, long remaining = 3600001) =>
        new(id, "event.reactor-accident", "TradeUX.Event.ReactorAccident.Name", "TradeUX.Event.ReactorAccident.Description",
            "TradeUX.Event.ReactorAccident.Effect", start, start + remaining, remaining);

    private static AuthoritativeSnapshot SnapshotWithEvents(params StationMarketEventSnapshot[] events)
    {
        var snapshot = TradeUxTests.Snapshot();
        return snapshot with { DockedStationTrade = snapshot.DockedStationTrade! with { ActiveEvents = events.ToImmutableArray() } };
    }

    private sealed class EventUiConnection : IGameSessionConnection
    {
        internal int Commands;
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) { Commands++; return ValueTask.CompletedTask; }
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        { await Task.Delay(Timeout.Infinite, cancellationToken); yield break; }
    }

    private static SKBitmap EventBitmap(TradeScreen screen)
    {
        var bitmap = new SKBitmap(1600, 800);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1600, 800);
        return bitmap;
    }

    [Fact]
    public void Event_model_orders_filters_caps_and_clears_authoritative_events_without_price_mutation()
    {
        var model = new TradeModel();
        var snapshot = SnapshotWithEvents(EventSnapshot("B", 2), EventSnapshot("A", 2), EventSnapshot("C", 3), EventSnapshot("Expired", 0, 0));
        model.Refresh(snapshot);
        Assert.Equal(new[] { "A", "B" }, model.ActiveEvents.Select(e => e.EventId));
        Assert.Equal(snapshot.DockedStationTrade!.Items.Single(i => i.ItemTypeId == "item.water").UnitPriceCredits,
            model.Rows.Single(i => i.ItemTypeId == "item.water").UnitPriceCredits);
        model.Refresh(snapshot with { SnapshotSequence = 2, DockedStationTrade = snapshot.DockedStationTrade with { ActiveEvents = default } });
        Assert.Empty(model.ActiveEvents);
        model.Refresh(snapshot);
        model.Refresh(snapshot with { SnapshotSequence = 3, DockedStationTrade = null });
        Assert.Empty(model.ActiveEvents);
    }

    [Fact]
    public void Event_text_handles_full_suffix_unknown_empty_and_legacy_keys()
    {
        string translated = Localization.Get("TradeUX.Event.ReactorAccident.Name");
        Assert.Equal(translated, TradeModel.EventText("TradeUX.Event.ReactorAccident.Name", "Legacy", "ID"));
        Assert.Equal(translated, TradeModel.EventText("Event.ReactorAccident.Name", "Legacy", "ID"));
        Assert.Equal("Legacy", TradeModel.EventText("Unknown.Name", "Legacy", "ID"));
        Assert.Equal("ID", TradeModel.EventText("", null, "ID"));
        Assert.Equal("ID", TradeModel.EventText("Unknown.Name", " ", "ID"));
        Assert.Equal("Authored explanation", TradeModel.EventText("", "Authored explanation", "ID"));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3600000, 1)]
    [InlineData(3600001, 2)]
    [InlineData(7200000, 2)]
    [InlineData(long.MaxValue - 1, 2562047788016)]
    public void Event_duration_uses_authoritative_ceil_hours_without_TimeSpan_overflow(long remaining, long hours)
    {
        var evt = EventSnapshot(remaining: remaining);
        Assert.Equal(string.Format(System.Globalization.CultureInfo.CurrentCulture, Localization.Get("TradeUX.EventRemainingHours"), hours), TradeModel.EventRemaining(evt));
        Assert.Equal(Localization.Get("TradeUX.EventPermanent"), TradeModel.EventRemaining(evt with { RemainingGameTimeMs = long.MaxValue }));
    }

    [Fact]
    public async Task Event_badge_click_preserves_search_focus_selection_mode_and_history()
    {
        var connection = new EventUiConnection();
        await using var handle = new GameSessionHandle(connection);
        handle.Buffer.Update(SnapshotWithEvents(EventSnapshot()));
        var screen = new TradeScreen(handle.Buffer, handle);
        screen.OnActivated();
        using var first = EventBitmap(screen);
        screen.Model.Select("item.water");
        screen.OnMouseDown(TradeLayout.Search.Left + 5, TradeLayout.Search.MidY);
        foreach (char c in "item.") screen.OnTextInput(c);
        string selected = screen.Model.SelectedItemId!;
        Assert.True(screen.OnMouseMove(TradeLayout.EventBadge.MidX, TradeLayout.EventBadge.MidY));
        Assert.True(screen.IsEventTooltipVisible);
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(TradeLayout.EventBadge.MidX, TradeLayout.EventBadge.MidY));
        screen.OnTextInput('w');
        Assert.Equal("item.w", screen.Model.Query);
        Assert.Equal(selected, screen.Model.SelectedItemId);
        Assert.Equal(TradeMode.Buy, screen.Model.Mode);
        Assert.Empty(screen.History);
        Assert.Equal(0, connection.Commands);
        screen.OnMouseMove(0, 0);
        Assert.False(screen.IsEventTooltipVisible);
        screen.OnMouseMove(TradeLayout.EventBadge.MidX, TradeLayout.EventBadge.MidY);
        screen.OnDeactivated();
        Assert.False(screen.IsEventTooltipVisible);
        screen.OnActivated();
        Assert.False(screen.IsEventTooltipVisible);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Event_tooltip_pixels_stay_within_bounds_and_paused_renders_are_identical(int count)
    {
        await using var handle = new GameSessionHandle(new EventUiConnection());
        handle.Buffer.Update(SnapshotWithEvents(Enumerable.Range(0, count).Select(i => EventSnapshot(i.ToString(), i)).ToArray()));
        var screen = new TradeScreen(handle.Buffer, handle);
        screen.OnActivated();
        using var plain = EventBitmap(screen);
        Assert.True(screen.OnMouseMove(TradeLayout.EventBadge.MidX, TradeLayout.EventBadge.MidY));
        using var hover = EventBitmap(screen);
        using var repeated = EventBitmap(screen);
        Assert.Equal(hover.Pixels, repeated.Pixels);
        int changed = 0;
        var before = plain.Pixels;
        var after = hover.Pixels;
        for (int i = 0; i < before.Length; i++)
        {
            if (before[i] == after[i]) continue;
            changed++;
            Assert.True(TradeLayout.EventTooltip.Contains(i % plain.Width + .5f, i / plain.Width + .5f), $"pixel {i % plain.Width},{i / plain.Width}");
        }
        Assert.True(changed > 1000);
        if (count == 2)
        {
            using var image = SKImage.FromBitmap(hover);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(Path.Combine(AppContext.BaseDirectory, "market-events-tooltip.png"), data.ToArray());
        }
    }

    [Fact]
    public async Task Event_badge_remains_in_fuel_and_history_and_disappears_when_station_context_ends()
    {
        await using var handle = new GameSessionHandle(new EventUiConnection());
        var snapshot = SnapshotWithEvents(EventSnapshot());
        handle.Buffer.Update(snapshot);
        var screen = new TradeScreen(handle.Buffer, handle);
        screen.OnActivated();
        using var market = EventBitmap(screen);
        screen.OnMouseDown(TradeLayout.FuelTab.MidX, TradeLayout.FuelTab.MidY);
        Assert.Equal(TradeMode.Refuel, screen.Model.Mode);
        screen.OnMouseDown(TradeLayout.History.MidX, TradeLayout.History.MidY);
        Assert.True(screen.OnMouseMove(TradeLayout.EventBadge.MidX, TradeLayout.EventBadge.MidY));
        Assert.True(screen.IsEventTooltipVisible);
        using var history = EventBitmap(screen);
        Assert.Single(screen.Model.ActiveEvents);
        handle.Buffer.Update(snapshot with { SnapshotSequence = 2, DockedStationTrade = null });
        Assert.False(screen.OnMouseMove(TradeLayout.EventBadge.MidX, TradeLayout.EventBadge.MidY));
        Assert.Empty(screen.Model.ActiveEvents);
        Assert.False(screen.IsEventTooltipVisible);
    }

    [Fact]
    public async Task No_event_preserves_control_geometry_and_badge_is_not_interactive()
    {
        Assert.Equal(new SKRect(20, 80, 185, 126), TradeLayout.MarketTab);
        Assert.Equal(new SKRect(197, 80, 380, 126), TradeLayout.FuelTab);
        Assert.Equal(new SKRect(1040, 190, 1300, 228), TradeLayout.Buy);
        Assert.Equal(new SKRect(1300, 190, 1560, 228), TradeLayout.Sell);
        await using var handle = new GameSessionHandle(new EventUiConnection());
        handle.Buffer.Update(SnapshotWithEvents());
        var screen = new TradeScreen(handle.Buffer, handle);
        screen.OnActivated();
        using var image = EventBitmap(screen);
        Assert.False(screen.OnMouseMove(TradeLayout.EventBadge.MidX, TradeLayout.EventBadge.MidY));
        Assert.False(screen.IsEventTooltipVisible);
        Assert.True(screen.OnMouseMove(TradeLayout.MarketTab.MidX, TradeLayout.MarketTab.MidY));
        screen.OnMouseDown(TradeLayout.FuelTab.MidX, TradeLayout.FuelTab.MidY);
        Assert.Equal(TradeMode.Refuel, screen.Model.Mode);
    }
}
