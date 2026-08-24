using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// The Trade overlay screen (Docs/FirstRelease/Screens/Trade.md) — Buy/Sell/Refuel,
/// docked-station inventory, player Credits/Cargo/Fuel. Follows the same
/// RecordingConnection/handle.Buffer.Update(...) pattern as GameSessionNavigationTests.
/// </summary>
public class TradeScreenTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;
    private const string PlayerShipId = "SPC-0001";
    private const string StationObjectId = "STN-0001";
    private const string ContainerModuleId = "MOD-CONTAINER-01";
    private const string EngineModuleId = "MOD-ENGINE-01";

    private static void RenderScreen(TradeScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    [Fact]
    public void Background_image_is_loaded()
    {
        // Regression: the window-background-1400x900.png asset must resolve at the
        // client's working directory and be registered in the .csproj with
        // CopyToOutputDirectory, or the panel silently falls back to a plain fill.
        Assert.True(TradeScreen.HasLoadedBackground);
    }

    // ── Open/close (structural twin of the old placeholder tests) ─────────────

    [Fact]
    public async Task Escape_returns_CloseTrade()
    {
        await using var fixture = CreateDockedFixture();
        RenderScreen(fixture.Screen);

        var result = fixture.Screen.OnKeyDown(Key.Escape);
        Assert.Equal(ScreenEvent.CloseTrade, result);
    }

    [Fact]
    public async Task Exit_button_click_returns_CloseTrade()
    {
        await using var fixture = CreateDockedFixture();
        RenderScreen(fixture.Screen);

        var result = ClickButtonAndGetEvent(fixture.Screen, TradeLayout.ExitButtonRect());
        Assert.Equal(ScreenEvent.CloseTrade, result);
    }

    [Fact]
    public async Task Click_outside_panel_returns_CloseTrade()
    {
        await using var fixture = CreateDockedFixture();
        RenderScreen(fixture.Screen);

        var result = fixture.Screen.OnMouseDown(0f, 0f);
        Assert.Equal(ScreenEvent.CloseTrade, result);
    }

    // ── No snapshot / not docked — transitional state, no crash ────────────────

    [Fact]
    public async Task Renders_without_exception_before_first_snapshot()
    {
        var connection = new RecordingConnection();
        var handle = new GameSessionHandle(connection);
        var screen = new TradeScreen(handle.Buffer, handle);

        RenderScreen(screen); // must not throw even though Buffer.Latest is null

        await handle.DisposeAsync();
    }

    [Fact]
    public async Task Renders_without_exception_when_not_docked()
    {
        var connection = new RecordingConnection();
        var handle = new GameSessionHandle(connection);
        handle.Buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray<ObjectMotionSnapshot>.Empty,
            PlayerShipObjectId: PlayerShipId,
            PlayerCredits: 500,
            DockedStationTrade: null));

        var screen = new TradeScreen(handle.Buffer, handle);
        RenderScreen(screen); // must not throw on null DockedStationTrade

        await handle.DisposeAsync();
    }

    // ── Docked with inventory — selection, stepper, sends ───────────────────────

    [Fact]
    public async Task OnActivated_preselects_the_first_station_inventory_item()
    {
        await using var fixture = CreateDockedFixture();

        fixture.Screen.OnActivated();

        Assert.Equal("item.energy-cells", fixture.Screen.SelectedItemTypeId);
        Assert.Equal(1, fixture.Screen.Quantity);
    }

    [Fact]
    public async Task OnActivated_does_not_throw_when_not_docked()
    {
        var connection = new RecordingConnection();
        var handle = new GameSessionHandle(connection);
        var screen = new TradeScreen(handle.Buffer, handle);

        screen.OnActivated(); // must not throw even though Buffer.Latest is null

        Assert.Null(screen.SelectedItemTypeId);
        await handle.DisposeAsync();
    }

    [Fact]
    public async Task Clicking_a_station_inventory_row_selects_that_item()
    {
        await using var fixture = CreateDockedFixture();
        RenderScreen(fixture.Screen);

        var row0 = TradeLayout.InventoryRowRect(0);
        float cx = TradeLayout.PanelLeft(ScreenWidth) + row0.X + row0.W / 2f;
        float cy = TradeLayout.PanelTop(ScreenHeight) + row0.Y + row0.H / 2f;

        fixture.Screen.OnMouseDown(cx, cy);

        Assert.Equal("item.energy-cells", fixture.Screen.SelectedItemTypeId);
        Assert.Equal(1, fixture.Screen.Quantity);
    }

    [Fact]
    public async Task Quantity_stepper_plus_and_minus_change_quantity()
    {
        await using var fixture = CreateDockedFixture();
        RenderScreen(fixture.Screen);
        SelectFirstItem(fixture);

        var stepper = TradeLayout.QuantityStepperRect();
        float pl = TradeLayout.PanelLeft(ScreenWidth);
        float pt = TradeLayout.PanelTop(ScreenHeight);

        fixture.Screen.OnMouseDown(pl + stepper.X + stepper.W - 5f, pt + stepper.Y + stepper.H / 2f); // plus
        Assert.Equal(2, fixture.Screen.Quantity);

        fixture.Screen.OnMouseDown(pl + stepper.X + 5f, pt + stepper.Y + stepper.H / 2f); // minus
        Assert.Equal(1, fixture.Screen.Quantity);

        // Minus at floor (1) never goes below 1.
        fixture.Screen.OnMouseDown(pl + stepper.X + 5f, pt + stepper.Y + stepper.H / 2f);
        Assert.Equal(1, fixture.Screen.Quantity);
    }

    [Fact]
    public async Task Buy_click_sends_exactly_one_trade_buy_command()
    {
        await using var fixture = CreateDockedFixture();
        RenderScreen(fixture.Screen);
        SelectFirstItem(fixture);

        ClickButton(fixture.Screen, TradeLayout.BuyButtonRect());

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal(TradeCommandTypes.Buy, command.CommandType);
        Assert.Equal(PlayerShipId, command.ObjectId);
        Assert.Equal(ContainerModuleId, command.ModuleId);
        Assert.Equal("item.energy-cells", command.ItemTypeId);
        Assert.Equal(1L, command.Quantity);
    }

    [Fact]
    public async Task Sell_click_sends_exactly_one_trade_sell_command()
    {
        await using var fixture = CreateDockedFixture();
        RenderScreen(fixture.Screen);
        SelectFirstItem(fixture);

        ClickButton(fixture.Screen, TradeLayout.SellButtonRect());

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal(TradeCommandTypes.Sell, command.CommandType);
        Assert.Equal(PlayerShipId, command.ObjectId);
        Assert.Equal(ContainerModuleId, command.ModuleId);
        Assert.Equal("item.energy-cells", command.ItemTypeId);
        Assert.Equal(1L, command.Quantity);
    }

    [Fact]
    public async Task Refuel_click_sends_exactly_one_trade_refuel_command_addressed_to_engine_module()
    {
        await using var fixture = CreateDockedFixture();
        RenderScreen(fixture.Screen);

        ClickButton(fixture.Screen, TradeLayout.RefuelButtonRect());

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal(TradeCommandTypes.Refuel, command.CommandType);
        Assert.Equal(PlayerShipId, command.ObjectId);
        Assert.Equal(EngineModuleId, command.ModuleId);
        Assert.Equal("item.fuel", command.ItemTypeId);
        Assert.True(command.Quantity is > 0);
    }

    [Fact]
    public async Task Cancel_click_resets_selection_and_quantity()
    {
        await using var fixture = CreateDockedFixture();
        RenderScreen(fixture.Screen);
        SelectFirstItem(fixture);

        ClickButton(fixture.Screen, TradeLayout.CancelButtonRect());

        Assert.Null(fixture.Screen.SelectedItemTypeId);
        Assert.Equal(1, fixture.Screen.Quantity);
    }

    [Fact]
    public async Task Rejected_command_result_in_next_snapshot_does_not_throw_on_render()
    {
        await using var fixture = CreateDockedFixture();
        RenderScreen(fixture.Screen);
        SelectFirstItem(fixture);

        ClickButton(fixture.Screen, TradeLayout.BuyButtonRect());
        var command = Assert.Single(fixture.Connection.Commands);

        var rejected = new CommandResult(
            CommandId: command.CommandId,
            ObjectId: PlayerShipId,
            ModuleId: ContainerModuleId,
            CommandType: TradeCommandTypes.Buy,
            Status: CommandResultStatus.Rejected,
            EffectiveGameTimeMs: 1000,
            ReasonCode: CommandReasonCodes.InsufficientPlayerCredits);

        fixture.Handle.Buffer.Update(BaseSnapshot(2) with
        {
            CommandResults = ImmutableArray.Create(rejected)
        });

        RenderScreen(fixture.Screen); // must not throw

        Assert.Contains("credits", fixture.Screen.LastStatusMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    // ── Test helpers ─────────────────────────────────────────────────────────

    private static void SelectFirstItem(TestFixture fixture)
    {
        var row0 = TradeLayout.InventoryRowRect(0);
        float cx = TradeLayout.PanelLeft(ScreenWidth) + row0.X + row0.W / 2f;
        float cy = TradeLayout.PanelTop(ScreenHeight) + row0.Y + row0.H / 2f;
        fixture.Screen.OnMouseDown(cx, cy);
    }

    private static void ClickButton(TradeScreen screen, (float X, float Y, float W, float H) localRect) =>
        ClickButtonAndGetEvent(screen, localRect);

    private static ScreenEvent ClickButtonAndGetEvent(TradeScreen screen, (float X, float Y, float W, float H) localRect)
    {
        float cx = TradeLayout.PanelLeft(ScreenWidth) + localRect.X + localRect.W / 2f;
        float cy = TradeLayout.PanelTop(ScreenHeight) + localRect.Y + localRect.H / 2f;
        return screen.OnMouseDown(cx, cy);
    }

    private static AuthoritativeSnapshot BaseSnapshot(ulong sequence)
    {
        var containerModule = new InstalledModuleSnapshot(
            ModuleId: ContainerModuleId,
            ModuleTypeId: "module.container.basic",
            DisplayName: "Cargo Container",
            Position: 0,
            CommandTypeIds: ImmutableArray.Create(TradeCommandTypes.Buy, TradeCommandTypes.Sell),
            Cargo: ImmutableArray.Create(new CargoStackSnapshot("item.energy-cells", 5)));

        var engineModule = new InstalledModuleSnapshot(
            ModuleId: EngineModuleId,
            ModuleTypeId: "module.engine.basic",
            DisplayName: "Main Engine",
            Position: 1,
            CommandTypeIds: ImmutableArray.Create(TradeCommandTypes.Refuel),
            FuelAmountKg: 4000);

        var stationObject = new ObjectMotionSnapshot(StationObjectId, 0, 0, SpeedKmS: 0, Direction: 0, DisplayName: "Test Station");

        var trade = new StationTradeSnapshot(
            StationObjectId,
            ImmutableArray.Create(
                new StationInventoryItemSnapshot("item.energy-cells", StockQuantity: 100, UnitPriceCredits: 200, MaxSellableQuantity: 50),
                new StationInventoryItemSnapshot("item.fuel", StockQuantity: 500, UnitPriceCredits: 200, MaxSellableQuantity: 20),
                new StationInventoryItemSnapshot("item.ice", StockQuantity: 300, UnitPriceCredits: 30, MaxSellableQuantity: 300)));

        return new AuthoritativeSnapshot(
            SnapshotSequence: sequence,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(stationObject),
            PlayerShipObjectId: PlayerShipId,
            InstalledModules: ImmutableArray.Create(containerModule, engineModule),
            PlayerCredits: 1000,
            DockedStationTrade: trade);
    }

    private static TestFixture CreateDockedFixture()
    {
        var connection = new RecordingConnection();
        var handle = new GameSessionHandle(connection);
        handle.Buffer.Update(BaseSnapshot(1));

        var screen = new TradeScreen(handle.Buffer, handle);
        return new TestFixture(connection, handle, screen);
    }

    private sealed record TestFixture(
        RecordingConnection Connection,
        GameSessionHandle Handle,
        TradeScreen Screen) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => Handle.DisposeAsync();
    }

    private sealed class RecordingConnection : IGameSessionConnection
    {
        public List<PlayerCommand> Commands { get; } = [];

        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default)
        {
            Commands.Add(command);
            return ValueTask.CompletedTask;
        }

        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask SetObjectInteractionStateAsync(
            string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
