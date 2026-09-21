using System.Collections.Immutable;
using System.Globalization;
using DeepSpaceSaga.Client.UI.Controls;
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
    [Fact]
    public void Buy_quote_previews_exact_balance_cargo_and_mass()
    {
        var q = TradeQuote.Calculate(Water(), Hold(), TradeMode.Buy, 40, 12480);
        Assert.Null(q.DisabledReason); Assert.Equal(560, q.Total); Assert.Equal(11920, q.BalanceAfter);
        Assert.Equal(60, q.CargoQuantity); Assert.Equal(720, q.AmountBefore); Assert.Equal(680, q.AmountAfter); Assert.Equal(240, q.Maximum);
    }
    [Theory]
    [InlineData(1, 20)] [InlineData(3, 6)] [InlineData(21, 0)] [InlineData(0, 240)]
    public void Buy_maximum_accounts_for_unit_mass(long mass, long expected)
    { Assert.Equal(expected, TradeQuote.Calculate(Water(mass), Hold(free: 20), TradeMode.Buy, 1, 12480).Maximum); }
    [Fact]
    public void Buy_is_limited_by_money_and_sell_by_station_budget()
    {
        Assert.Equal(2, TradeQuote.Calculate(Water(), Hold(), TradeMode.Buy, 1, 28).Maximum);
        var quote = TradeQuote.Calculate(Water() with { MaxSellableQuantity = 5 }, Hold(), TradeMode.Sell, 6, 100);
        Assert.Equal(5, quote.Maximum); Assert.Equal("StationBudgetLimit", quote.DisabledReason);
    }
    [Theory]
    [InlineData(0)] [InlineData(-1)] [InlineData(long.MaxValue)]
    public void Invalid_or_overflowing_quantity_never_gets_a_valid_quote(long quantity)
    { Assert.NotNull(TradeQuote.Calculate(Water(), Hold(), TradeMode.Buy, quantity, 1000).DisabledReason); }
    [Fact]
    public void Unavailable_container_cannot_trade()
    { Assert.Equal("ModuleUnavailable", TradeQuote.Calculate(Water(), Hold() with { PowerState = "Off" }, TradeMode.Buy, 1, 1000).DisabledReason); }
    [Fact]
    public void Selected_container_controls_cargo_and_capacity()
    {
        var model = Model(); model.SelectModule("hold-2");
        Assert.Equal(3, model.Cargo("item.water")); Assert.Equal(20, model.Quote.Maximum);
        model.SetMode(TradeMode.Sell); Assert.Equal(3, model.Quote.Maximum); Assert.Equal("hold-2", model.SelectedModuleId);
    }
    [Fact]
    public void Fuel_has_separate_catalog_and_target_level_presets()
    {
        var model = Model(); Assert.DoesNotContain(model.Rows, r => r.ItemTypeId == TradeModel.FuelId);
        model.SetMode(TradeMode.Refuel); Assert.Equal(TradeModel.FuelId, Assert.Single(model.Rows).ItemTypeId);
        model.FillTank(75); Assert.Equal(55, model.Quantity); Assert.Equal(375, model.Quote.AmountAfter);
        model.SelectModule("tank-2"); model.FillTank(100); Assert.Equal(400, model.Quantity); Assert.Equal(500, model.Quote.AmountAfter);
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
        model.FillTank(75);
        Assert.Equal(55, model.Quantity);
        model.Refresh(Snapshot() with { SnapshotSequence = 2, DockedStationTrade = new("station-transit", [new("item.water", 144, 14, 500), new("item.fuel", 600, 10, 500)]) });
        Assert.Equal(TradeModel.FuelId, Assert.Single(model.Rows).ItemTypeId);
        Assert.Equal(55, model.Quantity);
        Assert.Equal(375, model.Quote.AmountAfter);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        internal RecordingConnection Connection { get; } = new();
        internal GameSessionHandle Handle { get; }
        internal SnapshotBuffer Buffer => Handle.Buffer;
        internal TradeScreen Screen { get; }
        internal Fixture(string itemId = "item.water")
        {
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
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) { Commands.Add(command); return ValueTask.CompletedTask; }
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
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
        Assert.Equal("hold-2", f.Screen.Model.SelectedModuleId); Assert.Equal(20, f.Screen.Model.Quote.Maximum);
    }
    [Theory]
    [InlineData(CommandResultStatus.Executed)] [InlineData(CommandResultStatus.Rejected)]
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
    private static void Export(TradeScreen screen, string state)
    {
        using var bitmap = Render(screen);
        if (Environment.GetEnvironmentVariable("DSS_TRADE_RENDER_DIR") is not { Length: > 0 } directory) return;
        Directory.CreateDirectory(directory);
        using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(Path.Combine(directory, "trade-implemented-" + state + ".png")); data.SaveTo(stream);
    }
}
