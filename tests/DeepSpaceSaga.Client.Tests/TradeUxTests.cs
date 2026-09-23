using System.Collections.Immutable;
using System.Globalization;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class TradeUxTests
{
    private static InstalledModuleSnapshot Hold(string id = "hold-1", long free = 720, long water = 60) =>
        new(id, "container", "Cargo hold", id == "hold-1" ? 0 : 1, [TradeCommandTypes.Buy, TradeCommandTypes.Sell],
            "On", "Ready", 100, Cargo: [new("item.water", water), new("item.steel", 40), new("item.energy-cells", 80), new("item.food-rations", 100), new("item.electronics", 12)],
            AvailableCapacityKg: free, CargoCapacityKg: 1000);
    private static InstalledModuleSnapshot Tank(string id = "tank-1", long amount = 320) =>
        new(id, "engine", "Engine tank", 2, [TradeCommandTypes.Refuel], "On", "Ready", 100, FuelAmountKg: amount, FuelCapacityKg: 500);
    private static StationInventoryItemSnapshot Water(long mass = 1) => new("item.water", 240, 14, 500, TradeItemCategories.Good, mass);
    internal static AuthoritativeSnapshot Snapshot() => new(1, 0, SimulationSpeed.Speed0,
        [new("ship", 0, 0, 0, 0, IsDocked: true, DockedStationObjectId: "station"), new("station", 0, 0, 0, 0, DisplayName: "Orion")], "ship",
        InstalledModules: [Hold(), Hold("hold-2", 20, 3), Tank(), Tank("tank-2", 100)], PlayerCredits: 12480,
        DockedStationTrade: new("station", [Water(), new("item.steel", 120, 40, 500), new("item.energy-cells", 180, 50, 500),
            new("item.food-rations", 320, 20, 500), new("item.electronics", 132, 150, 500), new("item.protein-mass", 90, 110, 500), new("item.fuel", 500, 10, 500),
            new("item.ice", 400, 10, 500, TradeItemCategories.Resource), new("item.iron-ore", 350, 5, 500, TradeItemCategories.Resource),
            new("item.silicon", 150, 40, 500, TradeItemCategories.Resource), new("item.carbon-ore", 90, 30, 500, TradeItemCategories.Resource),
            new("item.uranium-ore", 25, 100, 500, TradeItemCategories.Resource)]));
    private static TradeModel Model()
    {
        var model = new TradeModel(); model.Refresh(Snapshot()); model.Select("item.water"); return model;
    }

    // ── Server quote fixtures (EP-0001-US-0003-TK-0004) ──────────────────────────────────
    // The fake curve deliberately differs from the snapshot list price (water lists at 14):
    // Buy/Refuel = first 10 units at 17, the rest at 19; Sell = first 10 at 11, the rest at 9.
    // A total can therefore never be reproduced as "first price × quantity".
    private const long FixtureRevision = 7;

    private static ImmutableArray<TradePriceStep> Curve(string commandType, long quantity)
    {
        (long first, long rest) = commandType == TradeCommandTypes.Sell ? (11L, 9L) : (17L, 19L);
        var steps = ImmutableArray.CreateBuilder<TradePriceStep>();
        if (quantity > 0) steps.Add(new(Math.Min(10, quantity), first));
        if (quantity > 10) steps.Add(new(quantity - 10, rest));
        return steps.ToImmutable();
    }

    private static long CurveTotal(string commandType, long quantity) =>
        Curve(commandType, quantity).Sum(step => step.Quantity * step.UnitPriceCredits);

    private static TradeQuoteRequest Request(string commandType, long quantity, string moduleId = "hold-1",
        string itemId = "item.water", string requestId = "R-1") =>
        new(requestId, "ship", moduleId, commandType, itemId, quantity);

    private static int s_quoteCounter;

    /// <summary>An executable server quote for <paramref name="request"/>; <paramref name="executable"/> below the request is a partial Sell.</summary>
    private static TradeQuoteSnapshot ServerQuote(TradeQuoteRequest request, long maximum = 200, long? executable = null,
        string station = "station", long revision = FixtureRevision, params string[] limits)
    {
        long filled = executable ?? request.Quantity;
        return new(request.RequestId, $"QTE-{Interlocked.Increment(ref s_quoteCounter)}", revision, station, request.ObjectId,
            request.ModuleId, request.CommandType, request.ItemTypeId, request.Quantity, filled, maximum,
            CurveTotal(request.CommandType, filled), Curve(request.CommandType, filled), null, [.. limits]);
    }

    /// <summary>A disabled server quote: empty QuoteId, nothing executable, the Engine's reason code.</summary>
    private static TradeQuoteSnapshot DisabledServerQuote(TradeQuoteRequest request, string reason, long maximum = 0,
        string station = "station") =>
        new(request.RequestId, "", FixtureRevision, station, request.ObjectId, request.ModuleId, request.CommandType,
            request.ItemTypeId, request.Quantity, 0, maximum, 0, ImmutableArray<TradePriceStep>.Empty, reason, ImmutableArray<string>.Empty);

    /// <summary>Default fake issuer: hold-2 allows 20 units, everything else 200; a request above that is disabled.</summary>
    private static TradeQuoteSnapshot DefaultQuoter(TradeQuoteRequest request)
    {
        long maximum = request.ModuleId == "hold-2" ? 20 : 200;
        return request.Quantity > maximum
            ? DisabledServerQuote(request, CommandReasonCodes.InsufficientPlayerCredits, maximum)
            : ServerQuote(request, maximum);
    }

    [Fact]
    public void Buy_quote_previews_exact_balance_cargo_and_mass()
    {
        var server = ServerQuote(Request(TradeCommandTypes.Buy, 40), maximum: 240);
        var q = TradeQuote.Calculate(Water(), Hold(), TradeMode.Buy, 40, 12480, server);
        long total = 10 * 17 + 30 * 19;
        Assert.NotEqual(40 * Water().UnitPriceCredits, total);
        Assert.Null(q.DisabledReason); Assert.Equal(total, q.Total); Assert.Equal(12480 - total, q.BalanceAfter);
        Assert.Equal(60, q.CargoQuantity); Assert.Equal(720, q.AmountBefore); Assert.Equal(680, q.AmountAfter); Assert.Equal(240, q.Maximum);
        Assert.Equal(40, q.ExecutableQuantity);
    }
    [Theory]
    [InlineData(1, 20)]
    [InlineData(3, 6)]
    [InlineData(21, 0)]
    [InlineData(0, 240)]
    public void Buy_maximum_accounts_for_unit_mass(long mass, long expected)
    {
        // The maximum is the server's; the client only applies the unit mass to the cargo preview.
        var request = Request(TradeCommandTypes.Buy, 1);
        var server = expected == 0 ? DisabledServerQuote(request, CommandReasonCodes.CargoCapacityExceeded) : ServerQuote(request, expected);
        var quote = TradeQuote.Calculate(Water(mass), Hold(free: 20), TradeMode.Buy, 1, 12480, server);
        Assert.Equal(expected, quote.Maximum);
        if (expected == 0) Assert.Equal("CapacityLimit", quote.DisabledReason);
        else Assert.Equal(20 - mass, quote.AmountAfter);
    }
    [Fact]
    public void Buy_is_limited_by_money_and_sell_by_station_budget()
    {
        var money = TradeQuote.Calculate(Water(), Hold(), TradeMode.Buy, 3, 28,
            DisabledServerQuote(Request(TradeCommandTypes.Buy, 3), CommandReasonCodes.InsufficientPlayerCredits, 2));
        Assert.Equal(2, money.Maximum); Assert.Equal("MoneyLimit", money.DisabledReason);
        var quote = TradeQuote.Calculate(Water(), Hold(), TradeMode.Sell, 6, 100,
            DisabledServerQuote(Request(TradeCommandTypes.Sell, 6), CommandReasonCodes.StationBudgetExceeded, 5));
        Assert.Equal(5, quote.Maximum); Assert.Equal("StationBudgetLimit", quote.DisabledReason);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(long.MaxValue)]
    public void Invalid_or_overflowing_quantity_never_gets_a_valid_quote(long quantity)
    {
        // Even an (inconsistent) server quote that claims long.MaxValue credits cannot overflow the preview.
        var request = Request(TradeCommandTypes.Sell, quantity);
        var server = new TradeQuoteSnapshot(request.RequestId, "QTE-X", FixtureRevision, "station", "ship", "hold-1", TradeCommandTypes.Sell,
            "item.water", quantity, Math.Max(0, quantity), long.MaxValue, long.MaxValue, [new(Math.Max(1, quantity), 1)], null, []);
        Assert.NotNull(TradeQuote.Calculate(Water(), Hold(water: long.MaxValue), TradeMode.Sell, quantity, 1000, server).DisabledReason);
        Assert.NotNull(TradeQuote.Calculate(Water(), Hold(), TradeMode.Buy, quantity, 1000, null).DisabledReason);
    }
    [Fact]
    public void Unavailable_container_cannot_trade()
    {
        var server = ServerQuote(Request(TradeCommandTypes.Buy, 1));
        Assert.Equal("ModuleUnavailable", TradeQuote.Calculate(Water(), Hold() with { PowerState = "Off" }, TradeMode.Buy, 1, 1000, server).DisabledReason);
    }
    [Fact]
    public void Selected_container_controls_cargo_and_capacity()
    {
        var model = Model(); model.SelectModule("hold-2");
        model.ApplyQuote(ServerQuote(Request(TradeCommandTypes.Buy, 1, "hold-2"), maximum: 20));
        Assert.Equal(3, model.Cargo("item.water")); Assert.Equal(20, model.Quote.Maximum);
        model.SetMode(TradeMode.Sell);
        Assert.Equal(0, model.Quote.Maximum); // a Buy maximum never leaks into Sell
        model.ApplyQuote(ServerQuote(Request(TradeCommandTypes.Sell, 1, "hold-2"), maximum: 3));
        Assert.Equal(3, model.Quote.Maximum); Assert.Equal("hold-2", model.SelectedModuleId);
    }
    [Fact]
    public void Fuel_has_separate_catalog_and_target_level_presets()
    {
        var model = Model(); Assert.DoesNotContain(model.Rows, r => r.ItemTypeId == TradeModel.FuelId);
        model.SetMode(TradeMode.Refuel); Assert.Equal(TradeModel.FuelId, Assert.Single(model.Rows).ItemTypeId);
        model.ApplyQuote(ServerQuote(Request(TradeCommandTypes.Refuel, 1, "tank-1", TradeModel.FuelId), maximum: 180));
        model.FillTank(75); Assert.Equal(55, model.Quantity);
        model.ApplyQuote(ServerQuote(Request(TradeCommandTypes.Refuel, 55, "tank-1", TradeModel.FuelId), maximum: 180));
        Assert.Equal(375, model.Quote.AmountAfter);
        model.SelectModule("tank-2");
        model.ApplyQuote(ServerQuote(Request(TradeCommandTypes.Refuel, 55, "tank-2", TradeModel.FuelId), maximum: 400));
        model.FillTank(100); Assert.Equal(400, model.Quantity);
        model.ApplyQuote(ServerQuote(Request(TradeCommandTypes.Refuel, 400, "tank-2", TradeModel.FuelId), maximum: 400));
        Assert.Equal(500, model.Quote.AmountAfter);
    }
    [Fact]
    public void Fuel_target_below_current_amount_adds_nothing()
    { var model = Model(); model.SetMode(TradeMode.Refuel); model.FillTank(50); Assert.Equal(0, model.Quantity); Assert.NotNull(model.Quote.DisabledReason); }
    [Fact]
    public void Search_filters_sort_and_new_snapshots_keep_identity()
    {
        var model = Model(); model.SetSort(TradeSort.Price); Assert.Equal("item.water", model.SelectedItemId);
        Assert.Equal(5, model.Rows[0].UnitPriceCredits);
        model.Query = "WATER"; model.Refresh(Snapshot()); Assert.Single(model.Rows); Assert.Equal("item.water", model.SelectedItemId);
        model.Query = ""; model.SetFilter(TradeFilter.Resources); Assert.All(model.Rows, r => Assert.Equal(TradeItemCategories.Resource, r.Category));
        model.SetFilter(TradeFilter.All); model.ToggleCargoOnly(); Assert.All(model.Rows, r => Assert.True(model.Cargo(r.ItemTypeId) > 0));
    }
    [Fact]
    public void Snapshot_refresh_does_not_reset_quantity_or_container()
    {
        var model = Model(); model.SelectModule("hold-2"); model.Quantity = 9;
        model.Refresh(Snapshot() with { SnapshotSequence = 2, PlayerCredits = 12000 });
        Assert.Equal(9, model.Quantity); Assert.Equal("hold-2", model.SelectedModuleId); Assert.Equal("item.water", model.SelectedItemId);
    }
    [Fact]
    public void Default_empty_trade_arrays_are_safe()
    { var model = new TradeModel(); model.Refresh(new(1, 0, SimulationSpeed.Speed0, [], DockedStationTrade: new("station"))); Assert.Empty(model.Rows); Assert.Null(model.Item); }

    [Fact]
    public void Electronics_uses_localized_name_description_and_block_unit()
    {
        Assert.Equal(Localization.Get("Trade.ItemElectronics"), TradeItemPresentation.ItemDisplayName("item.electronics"));
        Assert.Equal(Localization.Get("Trade.DescriptionElectronics"), TradeItemPresentation.ItemDescription("item.electronics"));
        Assert.Equal(Localization.Get("Trade.UnitBlock"), TradeItemPresentation.ItemUnitLabel("item.electronics"));
        Assert.Equal(Localization.Get("Trade.UnitBlockSingle"), TradeItemPresentation.ItemUnitLabel("item.electronics", singular: true));
        Assert.DoesNotContain("item.electronics", TradeItemPresentation.ItemDisplayName("item.electronics"), StringComparison.Ordinal);
        Assert.DoesNotContain("Trade.", TradeItemPresentation.ItemDescription("item.electronics"), StringComparison.Ordinal);
        Assert.Null(TradeItemPresentation.ItemImagePath("item.electronics"));
    }

    [Theory]
    [InlineData("item.water", "Trade.UnitKg", "Trade.UnitKg")]
    [InlineData("item.uranium-ore", "Trade.UnitKg", "Trade.UnitKg")]
    [InlineData("item.food-rations", "Trade.UnitRation", "Trade.UnitRationSingle")]
    [InlineData("item.energy-cells", "Trade.UnitCell", "Trade.UnitCellSingle")]
    [InlineData("item.electronics", "Trade.UnitBlock", "Trade.UnitBlockSingle")]
    [InlineData("item.future", "Trade.UnitGeneric", "Trade.UnitGenericSingle")]
    public void Item_units_distinguish_quantity_from_mass(string itemId, string pluralKey, string singularKey)
    {
        string plural = Localization.Get(pluralKey);
        string singular = Localization.Get(singularKey);
        Assert.Equal(plural, TradeItemPresentation.ItemUnitLabel(itemId));
        Assert.Equal(singular, TradeItemPresentation.ItemUnitLabel(itemId, singular: true));
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, Localization.Get("Trade.AmountWithUnit"),
            7, plural), TradeItemPresentation.FormatQuantity(itemId, 7));
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, Localization.Get("Trade.AmountWithUnit"),
            1, singular), TradeItemPresentation.FormatQuantity(itemId, 1));
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, Localization.Get("Trade.QuantityMass"),
            singular, 3), TradeItemPresentation.FormatUnitMass(itemId, 3));
        Assert.StartsWith(7.ToString(CultureInfo.CurrentCulture), TradeItemPresentation.FormatQuantity(itemId, 7));
    }

    [Fact]
    public void Profile_snapshot_refresh_replaces_market_rows()
    {
        (string StationId, StationInventoryItemSnapshot[] Items)[] profiles =
        [
            ("station-mining", [new("item.ice", 162, 10, 500, TradeItemCategories.Resource)]),
            ("station-industrial", [new("item.electronics", 132, 150, 500)]),
            ("station-hydroponic", [new("item.food-rations", 120, 20, 500)]),
            ("station-transit", [new("item.energy-cells", 144, 50, 500)]),
            ("station-scientific", [new("item.silicon", 48, 40, 500, TradeItemCategories.Resource)])
        ];
        var model = new TradeModel();
        string? previousItem = null;
        foreach (var profile in profiles)
        {
            var snapshot = Snapshot() with { DockedStationTrade = new(profile.StationId, profile.Items.ToImmutableArray()) };
            model.Refresh(snapshot);
            var row = Assert.Single(model.Rows);
            Assert.Equal(profile.Items[0].ItemTypeId, row.ItemTypeId);
            Assert.Equal(profile.Items[0].StockQuantity, row.StockQuantity);
            if (previousItem is not null) Assert.DoesNotContain(model.Rows, item => item.ItemTypeId == previousItem);
            previousItem = row.ItemTypeId;
        }
    }

    [Fact]
    public void Fuel_remains_refuel_only_after_profile_refresh()
    {
        var model = new TradeModel();
        model.Refresh(Snapshot() with { DockedStationTrade = new("station-industrial", [new("item.electronics", 132, 150, 500), new("item.fuel", 200, 10, 500)]) });
        Assert.DoesNotContain(model.Rows, item => item.ItemTypeId == TradeModel.FuelId);
        model.SetMode(TradeMode.Refuel);
        Assert.Equal(TradeModel.FuelId, Assert.Single(model.Rows).ItemTypeId);
        model.ApplyQuote(ServerQuote(Request(TradeCommandTypes.Refuel, 1, "tank-1", TradeModel.FuelId), maximum: 180, station: "station-industrial"));
        model.FillTank(75);
        Assert.Equal(55, model.Quantity);
        model.Refresh(Snapshot() with { SnapshotSequence = 2, DockedStationTrade = new("station-transit", [new("item.water", 144, 14, 500), new("item.fuel", 600, 10, 500)]) });
        Assert.Equal(TradeModel.FuelId, Assert.Single(model.Rows).ItemTypeId);
        Assert.Equal(55, model.Quantity);
        // Another station's quote never previews this one.
        model.ApplyQuote(ServerQuote(Request(TradeCommandTypes.Refuel, 55, "tank-1", TradeModel.FuelId), maximum: 180, station: "station-industrial"));
        Assert.Equal("QuoteLoading", model.Quote.DisabledReason);
        model.ApplyQuote(ServerQuote(Request(TradeCommandTypes.Refuel, 55, "tank-1", TradeModel.FuelId), maximum: 180, station: "station-transit"));
        Assert.Equal(375, model.Quote.AmountAfter);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        internal RecordingConnection Connection { get; } = new();
        internal GameSessionHandle Handle { get; }
        internal SnapshotBuffer Buffer => Handle.Buffer;
        internal TradeScreen Screen { get; }
        internal Fixture(string itemId = "item.water", bool manualQuotes = false)
        {
            Connection.ManualQuotes = manualQuotes;
            Handle = new(Connection); Buffer.Update(Snapshot()); Screen = new(Buffer, Handle); Screen.OnActivated();
            using var bitmap = Render(Screen);
            int row = Array.FindIndex(Screen.Model.Rows, r => r.ItemTypeId == itemId);
            while (row >= Screen.ScrollOffset + TradeLayout.VisibleRows) Screen.OnMouseWheel(300, 500, -1);
            Click(Screen, TradeLayout.Row(row - Screen.ScrollOffset));
        }
        public ValueTask DisposeAsync() => Handle.DisposeAsync();
    }
    private sealed class RecordingConnection : IGameSessionConnection
    {
        internal List<PlayerCommand> Commands { get; } = [];
        internal List<SimulationSpeed> SpeedChanges { get; } = [];
        internal List<TradeQuoteRequest> QuoteRequests { get; } = [];
        internal List<CancellationToken> QuoteTokens { get; } = [];
        /// <summary>Controlled responses in manual mode: one TaskCompletionSource per request, completed by the test.</summary>
        internal List<TaskCompletionSource<TradeQuoteSnapshot>> PendingQuotes { get; } = [];
        internal bool ManualQuotes { get; set; }
        internal Func<TradeQuoteRequest, TradeQuoteSnapshot> Quoter { get; set; } = DefaultQuoter;
        public ValueTask<TradeQuoteSnapshot> GetTradeQuoteAsync(TradeQuoteRequest request, CancellationToken cancellationToken = default)
        {
            QuoteRequests.Add(request); QuoteTokens.Add(cancellationToken);
            if (!ManualQuotes) return ValueTask.FromResult(Quoter(request));
            var pending = new TaskCompletionSource<TradeQuoteSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
            PendingQuotes.Add(pending);
            return new(pending.Task);
        }
        /// <summary>Answer the <paramref name="index"/>-th request with the current quoter.</summary>
        internal TradeQuoteSnapshot Answer(int index)
        {
            var quote = Quoter(QuoteRequests[index]); PendingQuotes[index].SetResult(quote); return quote;
        }
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) { Commands.Add(command); return ValueTask.CompletedTask; }
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) { SpeedChanges.Add(speed); return ValueTask.CompletedTask; }
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        { await Task.Delay(Timeout.Infinite, cancellationToken); yield break; }
    }
    private static SKBitmap Render(TradeScreen screen)
    {
        var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap); canvas.Clear(new SKColor(3, 8, 12));
        screen.Render(canvas, 1920, 1080); return bitmap;
    }
    private static void Click(TradeScreen screen, SKRect local) => screen.OnMouseDown(160 + local.MidX, 140 + local.MidY);
    [Fact]
    public async Task Keyboard_quantity_requires_separate_confirmation_and_pending_blocks_repeat()
    {
        await using var f = new Fixture(); Click(f.Screen, TradeLayout.Quantity);
        foreach (char c in "40") f.Screen.OnTextInput(c);
        Assert.Equal(40, f.Screen.Model.Quantity);
        f.Screen.OnKeyDown(Key.Enter); Assert.Empty(f.Connection.Commands); f.Screen.OnKeyUp(Key.Enter);
        f.Screen.OnKeyDown(Key.Enter); f.Screen.OnKeyDown(Key.Enter); Click(f.Screen, TradeLayout.Confirm);
        var sent = Assert.Single(f.Connection.Commands); Assert.Equal(40, sent.Quantity); Assert.Equal("hold-1", sent.ModuleId);
        Assert.Equal("item.water", f.Screen.Model.SelectedItemId); Assert.True(f.Screen.IsPending);
    }
    [Theory]
    [InlineData(0, "hold-2", TradeCommandTypes.Buy)]
    [InlineData(1, "hold-2", TradeCommandTypes.Sell)]
    [InlineData(2, "tank-2", TradeCommandTypes.Refuel)]
    public async Task Confirm_routes_to_selected_module(int mode, string module, string command)
    {
        await using var f = new Fixture(); f.Screen.Model.SetMode((TradeMode)mode); f.Screen.Model.SelectModule(module);
        Click(f.Screen, TradeLayout.Confirm);
        var sent = Assert.Single(f.Connection.Commands); Assert.Equal(module, sent.ModuleId); Assert.Equal(command, sent.CommandType);
    }

    [Theory]
    [InlineData("item.electronics", 0, TradeCommandTypes.Buy)]
    [InlineData("item.electronics", 1, TradeCommandTypes.Sell)]
    [InlineData("item.food-rations", 0, TradeCommandTypes.Buy)]
    [InlineData("item.food-rations", 1, TradeCommandTypes.Sell)]
    [InlineData("item.energy-cells", 0, TradeCommandTypes.Buy)]
    [InlineData("item.energy-cells", 1, TradeCommandTypes.Sell)]
    public async Task Profile_item_buy_and_sell_send_one_trade_unit(string itemId, int mode, string commandType)
    {
        await using var f = new Fixture(itemId);
        f.Screen.Model.SetMode((TradeMode)mode);
        using var beforeConfirmation = Render(f.Screen);
        Assert.Empty(f.Connection.Commands);
        Click(f.Screen, TradeLayout.Confirm);
        var command = Assert.Single(f.Connection.Commands);
        Assert.Equal(commandType, command.CommandType);
        Assert.Equal(itemId, command.ItemTypeId);
        Assert.Equal(1, command.Quantity);
        Assert.Equal(1, f.Screen.Model.Quantity);
        Assert.Equal(itemId, f.Screen.Model.SelectedItemId);
    }

    [Fact]
    public async Task Profile_item_render_handles_long_names_and_unit_labels()
    {
        await using var f = new Fixture("item.electronics");
        using var market = Render(f.Screen);
        Export(f.Screen, "profile-electronics");
        Assert.Equal(1920, market.Width);
        Assert.Equal(Localization.Get("Trade.UnitBlock"), TradeItemPresentation.ItemUnitLabel("item.electronics"));
        Assert.Contains(Localization.Get("Trade.UnitBlock"), TradeItemPresentation.FormatQuantity("item.electronics", 132), StringComparison.CurrentCulture);
        Assert.Contains(Localization.Get("Trade.UnitBlockSingle"), TradeItemPresentation.FormatUnitMass("item.electronics", 1), StringComparison.CurrentCulture);
        Click(f.Screen, TradeLayout.Confirm);
        Assert.Equal(1, Assert.Single(f.Connection.Commands).Quantity);
        using var pending = Render(f.Screen);
        Export(f.Screen, "profile-electronics-pending");
        Assert.Equal(1080, pending.Height);
    }
    [Fact]
    public async Task Dropdown_selects_another_hold()
    {
        await using var f = new Fixture(); Click(f.Screen, TradeLayout.Module); Click(f.Screen, TradeLayout.ModuleOption(1));
        Assert.Equal("hold-2", f.Screen.Model.SelectedModuleId);
        using var frame = Render(f.Screen); // the server maximum for hold-2 arrives with the next quote
        Assert.Equal(20, f.Screen.Model.Quote.Maximum);
    }
    [Theory]
    [InlineData(CommandResultStatus.Executed)]
    [InlineData(CommandResultStatus.Rejected)]
    public async Task Result_survives_skipped_frame_and_reopening_trade(CommandResultStatus status)
    {
        await using var f = new Fixture(); f.Screen.Model.Quantity = 10; Click(f.Screen, TradeLayout.Confirm);
        var sent = Assert.Single(f.Connection.Commands);
        var result = new CommandResult(sent.CommandId, "ship", sent.ModuleId, sent.CommandType, status, 0,
            status == CommandResultStatus.Rejected ? CommandReasonCodes.InsufficientStationStock : null, ExecutedQuantity: 6);
        f.Buffer.Update(Snapshot() with { SnapshotSequence = 2, CommandResults = [result] });
        f.Buffer.Update(Snapshot() with { SnapshotSequence = 3 });
        using var rendered = Render(f.Screen);
        Assert.False(f.Screen.IsPending); Assert.Equal("item.water", f.Screen.Model.SelectedItemId); Assert.Equal(10, f.Screen.Model.Quantity);
        var reopened = new TradeScreen(f.Buffer, f.Handle); reopened.OnActivated();
        Assert.Equal(result, Assert.Single(reopened.History).Result);
    }
    [Fact]
    public async Task Search_input_and_clear_button_filter_without_sending_commands()
    {
        await using var f = new Fixture(); Click(f.Screen, TradeLayout.Search);
        foreach (char c in "water") f.Screen.OnTextInput(c);
        using var filtered = Render(f.Screen); Assert.Single(f.Screen.Model.Rows);
        Click(f.Screen, TradeLayout.SearchClear); using var cleared = Render(f.Screen);
        Assert.True(f.Screen.Model.Rows.Length > 1); Assert.Empty(f.Connection.Commands);
    }
    [Fact]
    public async Task Wheel_and_drag_slider_clamp_to_valid_ranges()
    {
        await using var f = new Fixture();
        for (int i = 0; i < 40; i++) f.Screen.OnMouseWheel(300, 500, -1);
        Assert.Equal(f.Screen.Model.Rows.Length - TradeLayout.VisibleRows, f.Screen.ScrollOffset);
        Click(f.Screen, TradeLayout.Slider); f.Screen.OnMouseMove(9999, 590); f.Screen.OnMouseUp(9999, 590);
        Assert.Equal(f.Screen.Model.Quote.Maximum, f.Screen.Model.Quantity);
    }
    [Fact]
    public async Task Shared_toolbar_is_pixel_identical_to_unchanged_component()
    {
        await using var f = new Fixture(); using var actual = Render(f.Screen);
        using var expected = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(expected);
        canvas.Clear(new SKColor(3, 8, 12)); MenuStyle.DrawPanel(canvas, new(160, 140, 1760, 940));
        var snapshot = f.Buffer.Latest!.Snapshot;
        StationToolbar.Draw(canvas, 160, 140, "Orion", isStationHub: false, isHovered: false, windowName: "TRADE", isExitButtonHovered: false,
            foodRationsCount: StationToolbar.ResolveFoodRationsCount(snapshot), crewCount: StationToolbar.ResolveCrewCount(snapshot),
            cabinsCount: StationToolbar.ResolveCabinsCount(snapshot), creditsCount: StationToolbar.ResolveCreditsCount(snapshot),
            fuelAmountKg: StationToolbar.ResolveFuelAmountKg(snapshot), fuelCapacityKg: StationToolbar.ResolveFuelCapacityKg(snapshot), gameTimeMs: snapshot.GameTimeMs);
        for (int y = 140; y < 200; y++) for (int x = 160; x < 1760; x++) Assert.Equal(expected.GetPixel(x, y), actual.GetPixel(x, y));
    }
    [Fact]
    public async Task Render_market_fuel_pending_and_history_states()
    {
        await using var f = new Fixture(); f.Screen.Model.Quantity = 40;
        Export(f.Screen, "market");
        Click(f.Screen, TradeLayout.Confirm); Export(f.Screen, "pending");
        var command = Assert.Single(f.Connection.Commands);
        f.Buffer.Update(Snapshot() with { SnapshotSequence = 2, CommandResults = [new(command.CommandId, "ship", command.ModuleId, command.CommandType, CommandResultStatus.Executed, 0)] });
        Click(f.Screen, TradeLayout.History); Export(f.Screen, "history");
        Click(f.Screen, TradeLayout.FuelTab); f.Screen.Model.FillTank(100); Export(f.Screen, "fuel");
    }
    // ── US-0002 TK-0005: authoritative market state in Trade ──────────────────────────────

    /// <summary>A bounded-economy row: every market field is taken verbatim from the fixture, as from the Engine.</summary>
    private static StationInventoryItemSnapshot Bounded(string itemId, long stock, long target, StationMarketStockState state,
        long maxSellable = 500, long? free = null) =>
        new(itemId, stock, 14, maxSellable, TradeItemCategories.Good, 1, target, 2 * target, free ?? 2 * target - stock, state);

    /// <summary>The shared snapshot with the water row replaced by a bounded market row.</summary>
    private static AuthoritativeSnapshot MarketSnapshot(ulong sequence, StationInventoryItemSnapshot water, long gameTimeMs = 0)
    {
        var baseline = Snapshot();
        var items = baseline.DockedStationTrade!.Items.Select(i => i.ItemTypeId == "item.water" ? water : i).ToImmutableArray();
        return baseline with { SnapshotSequence = sequence, GameTimeMs = gameTimeMs, DockedStationTrade = new("station", items) };
    }

    [Fact]
    public async Task Market_badges_display_authoritative_states_instead_of_recalculating_ratio()
    {
        // Each fixture's local ratio points to a different band than the state it carries.
        (long Stock, StationMarketStockState State)[] cases =
        [
            (300, StationMarketStockState.Shortage), // 300/100 would be Surplus
            (5, StationMarketStockState.Normal),     // 5/100 would be Shortage
            (10, StationMarketStockState.Surplus),   // 10/100 would be Shortage
        ];
        foreach (var (stock, state) in cases)
        {
            var row = Bounded("item.water", stock, 100, state);
            string summary = Assert.IsType<string>(TradeScreen.MarketStockSummary(row));
            Assert.StartsWith(TradeScreen.StockStateLabel(state), summary, StringComparison.Ordinal);
            Assert.Equal(TradeScreen.F("MarketStockSummary", TradeScreen.StockStateLabel(state),
                TradeItemPresentation.FormatQuantity("item.water", 100), TradeItemPresentation.FormatQuantity("item.water", 200)), summary);
        }
        Assert.Equal(3, Enum.GetValues<StationMarketStockState>().Select(TradeScreen.StockStateLabel).Distinct().Count());
        Assert.All(Enum.GetValues<StationMarketStockState>(), state => Assert.DoesNotContain("TradeUX.", TradeScreen.StockStateLabel(state)));

        // Null state → no badge at all, never an implied "Normal".
        Assert.Null(TradeScreen.MarketStockSummary(Water()));
        Assert.Null(TradeScreen.MarketStockSummary(Bounded("item.water", 50, 100, StationMarketStockState.Normal) with { StockState = null }));

        await using var f = new Fixture();
        f.Buffer.Update(MarketSnapshot(2, Bounded("item.water", 10, 100, StationMarketStockState.Surplus)));
        using var rendered = Render(f.Screen);
        Assert.Equal(StationMarketStockState.Surplus, f.Screen.Model.Item!.StockState);
        Export(f.Screen, "market-state");
    }

    [Theory]
    [InlineData("item.food-rations", "Trade.UnitRation")]
    [InlineData("item.energy-cells", "Trade.UnitCell")]
    [InlineData("item.electronics", "Trade.UnitBlock")]
    public void Bounded_market_targets_use_item_units_and_legacy_rows_stay_plain(string itemId, string unitKey)
    {
        string summary = TradeScreen.MarketStockSummary(Bounded(itemId, 30, 72, StationMarketStockState.Shortage))!;
        Assert.Contains(TradeItemPresentation.FormatQuantity(itemId, 72), summary, StringComparison.Ordinal);
        Assert.Contains(TradeItemPresentation.FormatQuantity(itemId, 144), summary, StringComparison.Ordinal);
        Assert.Contains(Localization.Get(unitKey), summary, StringComparison.Ordinal);
        Assert.DoesNotContain(" " + Localization.Get("Trade.UnitKg"), summary, StringComparison.Ordinal);

        Assert.Null(TradeScreen.MarketStockSummary(new(itemId, 30, 20, 500)));
        Assert.Null(TradeScreen.MarketStockSummary(new("item.fuel", 500, 10, 500)));
    }

    [Theory]
    [InlineData(CommandReasonCodes.StationCapacityExceeded, 3, "StationCapacityLimit")]
    [InlineData(CommandReasonCodes.StationBudgetExceeded, 2, "StationBudgetLimit")]
    [InlineData(CommandReasonCodes.InsufficientCargoQuantity, 1, "CargoLimit")]
    [InlineData(CommandReasonCodes.StationCapacityExceeded, 0, "StationCapacityLimit")]
    public void Sell_limit_distinguishes_storage_from_budget(string code, long maximum, string reason)
    {
        // The limiter is the server's reason code; the client never derives it from MaxSellableQuantity.
        var item = Bounded("item.water", 300, 160, StationMarketStockState.Normal, 500, 500);
        var quote = TradeQuote.Calculate(item, Hold(water: 10), TradeMode.Sell, 11, 100,
            DisabledServerQuote(Request(TradeCommandTypes.Sell, 11), code, maximum));
        Assert.Equal(maximum, quote.Maximum);
        Assert.Equal(reason, quote.LimitReason);
        Assert.Equal(reason, quote.DisabledReason);
        Assert.DoesNotContain("TradeUX.", TradeScreen.L(reason));

        Assert.Equal("InvalidData", TradeQuote.Calculate(item with { FreeStockCapacity = -1 }, Hold(water: 10), TradeMode.Sell, 1, 100,
            ServerQuote(Request(TradeCommandTypes.Sell, 1))).DisabledReason);
    }

    [Fact]
    public async Task Partial_sell_result_shows_receipt_actual_requested_and_limit()
    {
        await using var f = new Fixture();
        f.Connection.Quoter = request => ServerQuote(request, executable: Math.Min(3, request.Quantity),
            limits: CommandReasonCodes.StationBudgetExceeded);
        f.Screen.Model.SetMode(TradeMode.Sell); f.Screen.Model.Quantity = 5;
        Click(f.Screen, TradeLayout.Confirm);
        var sent = Assert.Single(f.Connection.Commands); Assert.Equal(5, sent.Quantity);
        // The station stock in the result snapshot is deliberately unchanged: remaining must come from the receipt.
        var partial = new CommandResult(sent.CommandId, "ship", sent.ModuleId, sent.CommandType,
            CommandResultStatus.Executed, 0, ExecutedQuantity: 99,
            TradeReceipt: new("station", sent.ItemTypeId, sent.QuoteId, sent.MarketRevision, sent.MarketRevision + 1,
                5, 3, 33, [CommandReasonCodes.StationBudgetExceeded]));
        f.Buffer.Update(Snapshot() with { SnapshotSequence = 2, CommandResults = [partial] });
        using var rendered = Render(f.Screen);

        var entry = Assert.Single(f.Screen.History);
        Assert.Equal(TradeScreen.F("PartialResult", TradeItemPresentation.ItemDisplayName("item.water"),
            TradeItemPresentation.FormatQuantity("item.water", 3), 33L.ToString("N0", CultureInfo.CurrentCulture),
            TradeItemPresentation.FormatQuantity("item.water", 5)) + " · " + TradeScreen.L("StationBudgetLimit"),
            f.Screen.EntryMessage(entry));

        var legacy = entry with { Result = entry.Result! with { TradeReceipt = null } };
        Assert.Equal(TradeScreen.L("ReceiptUnavailable"), f.Screen.EntryMessage(legacy));
    }

    [Fact]
    public async Task Replenished_snapshot_reenables_buy_without_changing_selection()
    {
        await using var f = new Fixture();
        f.Screen.Model.SelectModule("hold-2"); f.Screen.Model.Quantity = 5;
        // The server answers an empty station with the stock limiter; after replenishment it quotes normally.
        f.Connection.Quoter = request => DisabledServerQuote(request, CommandReasonCodes.InsufficientStationStock);
        f.Buffer.Update(MarketSnapshot(2, Bounded("item.water", 0, 160, StationMarketStockState.Shortage)));
        using (Render(f.Screen))
        {
            Assert.False(f.Screen.CanConfirm);
            Assert.Equal("StockLimit", f.Screen.Model.Quote.DisabledReason);
        }

        f.Connection.Quoter = DefaultQuoter;
        f.Buffer.Update(MarketSnapshot(3, Bounded("item.water", 30, 160, StationMarketStockState.Shortage), GameCalendar.HourMs));
        using (Render(f.Screen))
        {
            Assert.Equal("item.water", f.Screen.Model.SelectedItemId);
            Assert.Equal("hold-2", f.Screen.Model.SelectedModuleId);
            Assert.Equal(5, f.Screen.Model.Quantity);
            Assert.Equal(30, f.Screen.Model.Item!.StockQuantity);
            Assert.True(f.Screen.CanConfirm);
        }

        Click(f.Screen, TradeLayout.Confirm);
        var command = Assert.Single(f.Connection.Commands);
        Assert.Equal((TradeCommandTypes.Buy, "item.water", "hold-2", 5L),
            (command.CommandType, command.ItemTypeId, command.ModuleId, command.Quantity!.Value));
    }

    [Fact]
    public async Task Trade_render_does_not_advance_paused_market()
    {
        // UI-side guard only: the Engine's own paused clock is covered by TK-0003. Here nothing but
        // Render/Refresh runs, so any change to the market would have to originate in the client.
        await using var f = new Fixture();
        var paused = MarketSnapshot(2, Bounded("item.water", 36, 72, StationMarketStockState.Shortage));
        Assert.Equal(SimulationSpeed.Speed0, paused.CurrentSpeed);
        f.Buffer.Update(paused);
        for (int frame = 0; frame < 20; frame++)
        {
            if (frame % 5 == 0) { f.Screen.OnDeactivated(); f.Screen.OnActivated(); }
            using var rendered = Render(f.Screen);
            Assert.Same(paused, f.Buffer.Latest!.Snapshot);
            Assert.Equal(36, f.Screen.Model.Item!.StockQuantity);
            Assert.Equal(StationMarketStockState.Shortage, f.Screen.Model.Item.StockState);
            Assert.Equal(0, f.Buffer.Latest.Snapshot.GameTimeMs);
        }
        Assert.Empty(f.Connection.Commands);
        Assert.Empty(f.Connection.SpeedChanges);
    }

    [Fact]
    public async Task Market_reopen_uses_latest_snapshot_after_one_hour()
    {
        await using var f = new Fixture();
        f.Buffer.Update(MarketSnapshot(2, Bounded("item.water", 36, 72, StationMarketStockState.Shortage)));
        using (Render(f.Screen)) Assert.Equal(36, f.Screen.Model.Item!.StockQuantity);
        f.Screen.OnDeactivated();

        // Authoritative state after one explicit game hour; the client replays nothing locally.
        var hourLater = Bounded("item.water", 32, 72, StationMarketStockState.Shortage) with { StockState = StationMarketStockState.Normal };
        f.Buffer.Update(MarketSnapshot(3, hourLater, GameCalendar.HourMs));
        f.Screen.OnActivated();
        using var rendered = Render(f.Screen);

        var item = f.Screen.Model.Item!;
        Assert.Equal(32, item.StockQuantity);
        Assert.Equal(StationMarketStockState.Normal, item.StockState);
        Assert.Equal(TradeScreen.MarketStockSummary(hourLater), TradeScreen.MarketStockSummary(item));
        Assert.Equal("item.water", f.Screen.Model.SelectedItemId);
        Assert.Empty(f.Connection.Commands);
    }

    [Fact]
    public async Task Market_stock_full_rejection_is_localized()
    {
        await using var f = new Fixture();
        f.Screen.Model.SetMode(TradeMode.Sell);
        Click(f.Screen, TradeLayout.Confirm);
        var sent = Assert.Single(f.Connection.Commands);
        var rejected = new CommandResult(sent.CommandId, "ship", sent.ModuleId, sent.CommandType,
            CommandResultStatus.Rejected, 0, "station_stock_full");
        f.Buffer.Update(Snapshot() with { SnapshotSequence = 2, CommandResults = [rejected] });
        using var rendered = Render(f.Screen);

        string message = f.Screen.EntryMessage(Assert.Single(f.Screen.History));
        Assert.Equal(TradeScreen.F("Rejected", TradeItemPresentation.ItemDisplayName("item.water"), TradeScreen.L("StationStorageLimit")), message);
        Assert.DoesNotContain(TradeScreen.L("TradeRejected"), message, StringComparison.Ordinal);
        Assert.DoesNotContain("TradeUX.", message, StringComparison.Ordinal);
    }

    // ── EP-0001-US-0003-TK-0004: authoritative quote lifecycle ───────────────────────────

    /// <summary>Type a quantity into the field and leave it with Enter (the first Enter only applies the value).</summary>
    private static void TypeQuantity(TradeScreen screen, string digits)
    {
        Click(screen, TradeLayout.Quantity);
        foreach (char c in digits) screen.OnTextInput(c);
        screen.OnKeyDown(Key.Enter); screen.OnKeyUp(Key.Enter);
    }

    [Fact]
    public async Task Preview_and_max_use_server_quote_without_local_price_multiplication()
    {
        await using var f = new Fixture();
        TypeQuantity(f.Screen, "25");
        using var rendered = Render(f.Screen);

        var request = f.Connection.QuoteRequests[^1];
        Assert.Equal(("ship", "hold-1", TradeCommandTypes.Buy, "item.water", 25L),
            (request.ObjectId, request.ModuleId, request.CommandType, request.ItemTypeId, request.Quantity));
        var shown = Assert.IsType<TradeQuoteSnapshot>(f.Screen.Model.AuthoritativeQuote);
        Assert.Equal(request.RequestId, shown.RequestId);

        long total = 10 * 17 + 15 * 19;
        var quote = f.Screen.Model.Quote;
        Assert.Equal(total, quote.Total);
        Assert.NotEqual(25 * f.Screen.Model.Item!.UnitPriceCredits, quote.Total);
        Assert.Equal(200, quote.Maximum); // local money/stock/space math would give 240
        Assert.Equal(12480 - total, quote.BalanceAfter);
        Assert.Equal(25, quote.ExecutableQuantity);
        Assert.Equal(TradeScreen.F("Maximum", TradeScreen.N(200), "—"), f.Screen.QuoteMessage);
        Assert.True(f.Screen.CanConfirm);

        Click(f.Screen, TradeLayout.Max);
        Assert.Equal(200, f.Screen.Model.Quantity);
        Assert.Empty(f.Connection.Commands);
    }

    [Fact]
    public async Task Partial_sell_preview_uses_actual_quantity_and_total_but_sends_requested()
    {
        await using var f = new Fixture();
        f.Connection.Quoter = request => request.CommandType == TradeCommandTypes.Sell
            ? ServerQuote(request, maximum: 3, executable: Math.Min(3, request.Quantity), limits: CommandReasonCodes.StationBudgetExceeded)
            : DefaultQuoter(request);
        Click(f.Screen, TradeLayout.Sell);
        TypeQuantity(f.Screen, "5");
        using var rendered = Render(f.Screen);

        var shown = f.Screen.Model.AuthoritativeQuote!;
        var quote = f.Screen.Model.Quote;
        Assert.Null(quote.DisabledReason);
        Assert.Equal(3, quote.ExecutableQuantity);
        Assert.Equal(33, quote.Total);
        Assert.Equal(12480 + 33, quote.BalanceAfter);
        Assert.Equal(60, quote.CargoQuantity);
        Assert.Equal(5, f.Screen.Model.Quantity); // requested input is not silently reduced
        Assert.Equal(TradeScreen.F("PartialPreview", TradeScreen.N(3), TradeScreen.N(5), TradeScreen.N(33)), f.Screen.QuoteMessage);
        Assert.True(f.Screen.CanConfirm);

        Click(f.Screen, TradeLayout.Confirm);
        var sent = Assert.Single(f.Connection.Commands);
        Assert.Equal((TradeCommandTypes.Sell, 5L, shown.QuoteId, FixtureRevision),
            (sent.CommandType, sent.Quantity!.Value, sent.QuoteId, sent.MarketRevision!.Value));
        var entry = Assert.Single(f.Screen.History);
        Assert.Equal((5L, shown.QuoteId, (long?)FixtureRevision, (long?)33), (entry.RequestedQuantity, entry.QuoteId, entry.MarketRevision, entry.QuotedTotalCredits));

        // Buy with a partial fill is never executable.
        var buyRequest = Request(TradeCommandTypes.Buy, 5);
        Assert.NotNull(TradeQuote.Calculate(Water(), Hold(), TradeMode.Buy, 5, 12480, ServerQuote(buyRequest, 5, executable: 3)).DisabledReason);
    }

    [Fact]
    public async Task Changing_quantity_module_mode_or_station_invalidates_quote()
    {
        await using var f = new Fixture(manualQuotes: true);
        Assert.Single(f.Connection.QuoteRequests);
        f.Connection.Answer(0);
        using (Render(f.Screen)) Assert.True(f.Screen.CanConfirm);

        // Identical snapshots and repeated frames never request again.
        f.Buffer.Update(Snapshot() with { SnapshotSequence = 2 });
        for (int frame = 0; frame < 5; frame++) using (Render(f.Screen)) { }
        Assert.Single(f.Connection.QuoteRequests);

        void AssertInvalidatedAndRequested(int count, Func<TradeQuoteRequest, bool> expected)
        {
            using var frame = Render(f.Screen);
            Assert.Null(f.Screen.Model.AuthoritativeQuote);
            Assert.False(f.Screen.CanConfirm);
            Assert.Equal("QuoteLoading", f.Screen.Model.Quote.DisabledReason);
            Assert.Equal(count, f.Connection.QuoteRequests.Count);
            Assert.True(expected(f.Connection.QuoteRequests[^1]));
            f.Connection.Answer(count - 1);
            using var answered = Render(f.Screen);
            Assert.True(f.Screen.CanConfirm);
        }

        Click(f.Screen, TradeLayout.Plus);
        AssertInvalidatedAndRequested(2, r => r.Quantity == 2);
        Click(f.Screen, TradeLayout.Module); Click(f.Screen, TradeLayout.ModuleOption(1));
        AssertInvalidatedAndRequested(3, r => r.ModuleId == "hold-2" && r.Quantity == 2);
        Click(f.Screen, TradeLayout.Sell);
        AssertInvalidatedAndRequested(4, r => r.CommandType == TradeCommandTypes.Sell && r.Quantity == 1);
        f.Buffer.Update(Snapshot() with { SnapshotSequence = 3, DockedStationTrade = Snapshot().DockedStationTrade! with { MarketRevision = 8 } });
        f.Connection.Quoter = request => ServerQuote(request, revision: 8);
        AssertInvalidatedAndRequested(5, r => r.CommandType == TradeCommandTypes.Sell);
        f.Buffer.Update(Snapshot() with { SnapshotSequence = 4, DockedStationTrade = Snapshot().DockedStationTrade! with { StationObjectId = "station-2" } });
        using (Render(f.Screen))
        {
            Assert.Null(f.Screen.Model.AuthoritativeQuote);
            Assert.Equal(6, f.Connection.QuoteRequests.Count);
            Assert.False(f.Screen.CanConfirm);
        }
        Assert.Empty(f.Connection.Commands);
    }

    [Fact]
    public async Task Out_of_order_quote_response_is_ignored()
    {
        await using var f = new Fixture(manualQuotes: true);
        TypeQuantity(f.Screen, "2");
        using (Render(f.Screen)) Assert.Equal(2, f.Connection.QuoteRequests.Count);
        Assert.True(f.Connection.QuoteTokens[0].IsCancellationRequested);
        Assert.False(f.Connection.QuoteTokens[1].IsCancellationRequested);

        var current = f.Connection.Answer(1);
        using (Render(f.Screen)) Assert.Same(current, f.Screen.Model.AuthoritativeQuote);

        // The superseded request completes late: it must not replace the current quote.
        f.Connection.Answer(0);
        using (Render(f.Screen))
        {
            Assert.Same(current, f.Screen.Model.AuthoritativeQuote);
            Assert.Equal(2, f.Screen.Model.Quantity);
            Assert.True(f.Screen.CanConfirm);
        }

        // A response that answers another RequestId for the current key is never shown either.
        Click(f.Screen, TradeLayout.Plus);
        using (Render(f.Screen)) { }
        f.Connection.PendingQuotes[2].SetResult(ServerQuote(f.Connection.QuoteRequests[2] with { RequestId = "foreign" }));
        using (Render(f.Screen))
        {
            Assert.Null(f.Screen.Model.AuthoritativeQuote);
            Assert.False(f.Screen.CanConfirm);
        }
        Click(f.Screen, TradeLayout.Confirm);
        Assert.Empty(f.Connection.Commands);
    }

    [Fact]
    public async Task Max_slider_presets_and_typed_quantity_require_matching_quote_before_submit()
    {
        await using var f = new Fixture(manualQuotes: true);
        f.Connection.Answer(0);
        using (Render(f.Screen)) Assert.Equal(200, f.Screen.Model.Quote.Maximum);

        Click(f.Screen, TradeLayout.Max);
        Assert.Equal(200, f.Screen.Model.Quantity);
        Click(f.Screen, TradeLayout.Confirm);
        Assert.Empty(f.Connection.Commands);

        Click(f.Screen, TradeLayout.Presets[0]);
        Assert.Equal(10, f.Screen.Model.Quantity); // the last valid maximum still bounds presets while loading
        var slider = TradeLayout.Slider;
        f.Screen.OnMouseDown(160 + slider.Right - 1, 140 + slider.MidY); f.Screen.OnMouseUp(0, 0);
        Assert.Equal(200, f.Screen.Model.Quantity);
        TypeQuantity(f.Screen, "33");
        Click(f.Screen, TradeLayout.Confirm);
        f.Screen.OnKeyDown(Key.Enter); f.Screen.OnKeyUp(Key.Enter);
        Assert.Empty(f.Connection.Commands);

        int last = f.Connection.QuoteRequests.Count - 1;
        Assert.Equal(33, f.Connection.QuoteRequests[last].Quantity);
        Assert.All(f.Connection.QuoteRequests.Take(last), request => Assert.NotEqual(33, request.Quantity));
        var shown = f.Connection.Answer(last);
        Click(f.Screen, TradeLayout.Confirm);
        var sent = Assert.Single(f.Connection.Commands);
        Assert.Equal((33L, shown.QuoteId, shown.MarketRevision), (sent.Quantity!.Value, sent.QuoteId, sent.MarketRevision!.Value));
    }

    [Fact]
    public async Task Loading_failed_cancelled_and_stale_quotes_cannot_confirm()
    {
        await using var f = new Fixture(manualQuotes: true);
        using (Render(f.Screen))
        {
            Assert.Equal("QuoteLoading", f.Screen.Model.Quote.DisabledReason);
            Assert.False(f.Screen.CanConfirm);
        }

        f.Connection.PendingQuotes[0].SetException(new IOException("transport down"));
        for (int frame = 0; frame < 5; frame++)
            using (Render(f.Screen))
            {
                Assert.Equal("QuoteUnavailable", f.Screen.Model.Quote.DisabledReason);
                Assert.False(f.Screen.CanConfirm);
            }
        Assert.Single(f.Connection.QuoteRequests); // no per-frame retry
        Assert.Null(f.Handle.Failure);           // a quote failure is not a session failure

        TypeQuantity(f.Screen, "2");
        using (Render(f.Screen)) { }
        f.Connection.PendingQuotes[1].SetCanceled();
        using (Render(f.Screen))
        {
            Assert.Equal("QuoteUnavailable", f.Screen.Model.Quote.DisabledReason);
            Assert.False(f.Screen.CanConfirm);
        }

        TypeQuantity(f.Screen, "3");
        using (Render(f.Screen)) { }
        f.Connection.PendingQuotes[2].SetResult(DisabledServerQuote(f.Connection.QuoteRequests[2], CommandReasonCodes.StaleQuote));
        using (Render(f.Screen))
        {
            Assert.Equal("QuoteStale", f.Screen.Model.Quote.DisabledReason);
            Assert.False(f.Screen.CanConfirm);
        }

        // A shown quote goes stale as soon as the authoritative snapshot moves on.
        TypeQuantity(f.Screen, "4");
        using (Render(f.Screen)) { }
        f.Connection.Answer(3);
        using (Render(f.Screen)) Assert.True(f.Screen.CanConfirm);
        f.Buffer.Update(Snapshot() with { SnapshotSequence = 2, PlayerCredits = 12000 });
        using (Render(f.Screen))
        {
            Assert.Null(f.Screen.Model.AuthoritativeQuote);
            Assert.False(f.Screen.CanConfirm);
        }
        Click(f.Screen, TradeLayout.Confirm);
        Assert.Empty(f.Connection.Commands);
    }

    [Fact]
    public async Task Stale_result_refreshes_once_without_automatic_resubmit()
    {
        await using var f = new Fixture();
        Click(f.Screen, TradeLayout.Confirm);
        var sent = Assert.Single(f.Connection.Commands);
        int requests = f.Connection.QuoteRequests.Count;

        var stale = new CommandResult(sent.CommandId, "ship", sent.ModuleId, sent.CommandType, CommandResultStatus.Rejected, 0,
            CommandReasonCodes.StaleQuote);
        f.Buffer.Update(Snapshot() with { SnapshotSequence = 2, CommandResults = [stale] });
        for (int frame = 0; frame < 5; frame++) using (Render(f.Screen)) { }

        Assert.False(f.Screen.IsPending);
        Assert.Equal(requests + 1, f.Connection.QuoteRequests.Count); // exactly one fresh quote
        Assert.Single(f.Connection.Commands);                          // and no automatic resubmit
        Assert.Equal(TradeScreen.F("Rejected", TradeItemPresentation.ItemDisplayName("item.water"), TradeScreen.L("QuoteStale")),
            f.Screen.EntryMessage(Assert.Single(f.Screen.History)));

        var fresh = f.Screen.Model.AuthoritativeQuote!;
        Assert.NotEqual(sent.QuoteId, fresh.QuoteId);
        Click(f.Screen, TradeLayout.Confirm);
        Assert.Equal(2, f.Connection.Commands.Count);
        Assert.Equal(fresh.QuoteId, f.Connection.Commands[1].QuoteId);
    }

    [Fact]
    public async Task Enter_repeat_and_double_click_send_one_command_with_shown_binding()
    {
        await using var f = new Fixture();
        TypeQuantity(f.Screen, "12");
        using (Render(f.Screen)) { }
        var shown = f.Screen.Model.AuthoritativeQuote!;

        f.Screen.OnKeyDown(Key.Enter); f.Screen.OnKeyDown(Key.Enter); // held key repeats
        f.Screen.OnKeyUp(Key.Enter); f.Screen.OnKeyDown(Key.Enter);    // pending blocks a new press
        Click(f.Screen, TradeLayout.Confirm); Click(f.Screen, TradeLayout.Confirm);

        var sent = Assert.Single(f.Connection.Commands);
        Assert.Equal(("ship", "hold-1", TradeCommandTypes.Buy, "item.water", 12L, shown.QuoteId, shown.MarketRevision),
            (sent.ObjectId, sent.ModuleId, sent.CommandType, sent.ItemTypeId, sent.Quantity!.Value, sent.QuoteId, sent.MarketRevision!.Value));
        var entry = Assert.Single(f.Screen.History);
        Assert.Equal((sent.CommandId, shown.QuoteId, (long?)shown.MarketRevision, (long?)shown.TotalCredits),
            (entry.CommandId, entry.QuoteId, entry.MarketRevision, entry.QuotedTotalCredits));
        Assert.True(f.Screen.IsPending);
    }

    [Fact]
    public async Task Fuel_tab_only_sends_refuel_and_uses_tank_quantity()
    {
        await using var f = new Fixture();
        Assert.DoesNotContain(f.Connection.QuoteRequests, r => r.ItemTypeId == TradeModel.FuelId);
        Click(f.Screen, TradeLayout.FuelTab);
        Assert.Equal(TradeModel.FuelId, Assert.Single(f.Screen.Model.Rows).ItemTypeId);
        Click(f.Screen, TradeLayout.Presets[1]); // 75 % of 500 kg with 320 kg aboard
        Assert.Equal(55, f.Screen.Model.Quantity);
        using (Render(f.Screen)) Assert.Equal(375, f.Screen.Model.Quote.AmountAfter);

        Click(f.Screen, TradeLayout.Confirm);
        var sent = Assert.Single(f.Connection.Commands);
        Assert.Equal((TradeCommandTypes.Refuel, TradeModel.FuelId, "tank-1", 55L),
            (sent.CommandType, sent.ItemTypeId, sent.ModuleId, sent.Quantity!.Value));
        Assert.All(f.Connection.QuoteRequests.Where(r => r.ItemTypeId == TradeModel.FuelId),
            r => Assert.Equal(TradeCommandTypes.Refuel, r.CommandType));
        Assert.All(f.Connection.QuoteRequests.Where(r => r.CommandType == TradeCommandTypes.Refuel),
            r => Assert.Equal(TradeModel.FuelId, r.ItemTypeId));
    }

    [Fact]
    public async Task Closing_screen_cancels_quote_without_erasing_pending_trade()
    {
        await using var f = new Fixture(manualQuotes: true);
        f.Connection.Answer(0);
        Click(f.Screen, TradeLayout.Confirm);
        var sent = Assert.Single(f.Connection.Commands);
        Click(f.Screen, TradeLayout.Plus);
        using (Render(f.Screen)) Assert.Equal(2, f.Connection.QuoteRequests.Count);

        Assert.Equal(ScreenEvent.CloseTrade, f.Screen.OnKeyDown(Key.Escape));
        f.Screen.OnDeactivated();
        Assert.True(f.Connection.QuoteTokens[1].IsCancellationRequested);

        var reopened = new TradeScreen(f.Buffer, f.Handle); reopened.OnActivated();
        Assert.True(reopened.IsPending);
        Assert.Equal(sent.CommandId, Assert.Single(reopened.History).CommandId);

        // The cancelled request finishing late is ignored after the original screen is reactivated.
        f.Connection.Answer(1);
        f.Screen.OnActivated();
        using (Render(f.Screen))
        {
            Assert.Null(f.Screen.Model.AuthoritativeQuote);
            Assert.Equal(sent.CommandId, Assert.Single(f.Screen.History).CommandId);
        }
        Assert.Single(f.Connection.Commands);
    }

    [Theory]
    [InlineData(CommandReasonCodes.StationBudgetExceeded, "StationBudgetLimit")]
    [InlineData(CommandReasonCodes.StationCapacityExceeded, "StationCapacityLimit")]
    [InlineData(CommandReasonCodes.InsufficientPlayerCredits, "MoneyLimit")]
    [InlineData(CommandReasonCodes.InsufficientStationStock, "StockLimit")]
    [InlineData(CommandReasonCodes.CargoCapacityExceeded, "CapacityLimit")]
    [InlineData(CommandReasonCodes.FuelCapacityExceeded, "TankLimit")]
    [InlineData(CommandReasonCodes.InsufficientCargoQuantity, "CargoLimit")]
    [InlineData("value_overflow", "ValueOverflow")]
    [InlineData(CommandReasonCodes.StaleQuote, "QuoteStale")]
    [InlineData(CommandReasonCodes.InvalidQuote, "InvalidQuote")]
    [InlineData(CommandReasonCodes.QuoteRequired, "QuoteRequired")]
    [InlineData(CommandReasonCodes.FuelTradeForbidden, "FuelServiceOnly")]
    [InlineData("request_id_conflict", "QuoteUnavailable")]
    [InlineData("something_new", "QuoteUnavailable")]
    public void Reason_codes_map_to_existing_trade_ux_keys(string code, string key)
    {
        Assert.Equal(key, TradeQuote.QuoteReasonKey(code));
        Assert.NotEqual("TradeUX." + key, TradeScreen.L(key)); // the key exists in the locale
        Assert.Equal(key, TradeQuote.Calculate(Water(), Hold(), TradeMode.Buy, 1, 12480,
            DisabledServerQuote(Request(TradeCommandTypes.Buy, 1), code)).DisabledReason);
    }

    [Theory]
    [InlineData(6, false)]
    [InlineData(7, true)]
    [InlineData(8, true)]
    public async Task Quote_must_not_be_older_than_current_market_snapshot(long revision, bool canConfirm)
    {
        await using var f = new Fixture(manualQuotes: true);
        f.Buffer.Update(Snapshot() with
        {
            SnapshotSequence = 2,
            DockedStationTrade = Snapshot().DockedStationTrade! with { MarketRevision = FixtureRevision }
        });
        using (Render(f.Screen)) { }
        var request = f.Connection.QuoteRequests[^1];
        f.Connection.PendingQuotes[^1].SetResult(ServerQuote(request, revision: revision));
        using var rendered = Render(f.Screen);
        Assert.Equal(canConfirm, f.Screen.CanConfirm);
        Click(f.Screen, TradeLayout.Confirm);
        Assert.Equal(canConfirm ? 1 : 0, f.Connection.Commands.Count);
    }

    [Fact]
    public async Task Failed_quote_retries_on_same_item_selection_without_per_frame_retry()
    {
        await using var f = new Fixture(manualQuotes: true);
        f.Connection.PendingQuotes[0].SetException(new IOException("unavailable"));
        for (int i = 0; i < 3; i++) using (Render(f.Screen)) { }
        Assert.Single(f.Connection.QuoteRequests);
        int row = Array.FindIndex(f.Screen.Model.Rows, item => item.ItemTypeId == "item.water");
        Click(f.Screen, TradeLayout.Row(row - f.Screen.ScrollOffset));
        Assert.Equal(2, f.Connection.QuoteRequests.Count);
        f.Connection.Answer(1);
        using var rendered = Render(f.Screen);
        Assert.True(f.Screen.CanConfirm);
        Assert.Empty(f.Connection.Commands);
    }

    [Theory]
    [InlineData("station")]
    [InlineData("empty-station")]
    [InlineData("ship")]
    [InlineData("module")]
    [InlineData("item")]
    [InlineData("mode")]
    [InlineData("quantity")]
    public async Task Mismatched_quote_binding_cannot_confirm(string fault)
    {
        await using var f = new Fixture(manualQuotes: true);
        var quote = ServerQuote(f.Connection.QuoteRequests[0]);
        quote = fault switch
        {
            "station" => quote with { StationObjectId = "foreign" },
            "empty-station" => quote with { StationObjectId = "" },
            "ship" => quote with { ObjectId = "foreign" },
            "module" => quote with { ModuleId = "foreign" },
            "item" => quote with { ItemTypeId = "item.ice" },
            "mode" => quote with { CommandType = TradeCommandTypes.Sell },
            _ => quote with { RequestedQuantity = 2 }
        };
        f.Connection.PendingQuotes[0].SetResult(quote);
        using var rendered = Render(f.Screen);
        Assert.False(f.Screen.CanConfirm);
        Click(f.Screen, TradeLayout.Confirm);
        Assert.Empty(f.Connection.Commands);
    }

    [Theory]
    [InlineData("money")]
    [InlineData("cargo")]
    [InlineData("tank")]
    [InlineData("overflow")]
    public void Inconsistent_quote_amounts_disable_preview_without_clamping(string fault)
    {
        var mode = fault == "cargo" ? TradeMode.Sell : fault == "tank" ? TradeMode.Refuel : TradeMode.Buy;
        var module = fault == "tank" ? Tank() : Hold();
        long quantity = fault == "cargo" ? 999 : 1;
        var item = Water(fault == "overflow" ? long.MaxValue : 1);
        var quote = ServerQuote(Request(TradeQuote.CommandType(mode), quantity, module.ModuleId), maximum: 1000);
        if (fault == "tank") module = module with { FuelAmountKg = module.FuelCapacityKg };
        if (fault == "overflow") module = module with { Cargo = [new("item.water", long.MaxValue)], AvailableCapacityKg = long.MaxValue };
        var projected = TradeQuote.Calculate(item, module, mode, quantity, fault == "money" ? 0 : 100000, quote);
        Assert.Equal(fault == "overflow" ? "ValueOverflow" : "InvalidData", projected.DisabledReason);
        Assert.Equal(0, projected.ExecutableQuantity);
    }

    private static void Export(TradeScreen screen, string state)
    {
        using var bitmap = Render(screen);
        if (Environment.GetEnvironmentVariable("DSS_TRADE_RENDER_DIR") is not { Length: > 0 } directory) return;
        Directory.CreateDirectory(directory);
        using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(Path.Combine(directory, "trade-implemented-" + state + ".png")); data.SaveTo(stream);
    }
}
