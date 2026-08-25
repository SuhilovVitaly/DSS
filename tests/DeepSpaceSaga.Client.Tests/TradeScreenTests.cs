using System.Collections.Immutable;
using DeepSpaceSaga.Client;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;
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
    public void Generic_Type_A_assets_are_loaded()
    {
        Assert.True(GenericWindowTypeA.HasAssets);
        Assert.True(GenericButtonTypeA.HasAssets);
    }

    [Fact]
    public async Task Render_draws_the_opaque_Generic_Type_A_shell_and_action_buttons()
    {
        await using var fixture = CreateDockedFixture();
        fixture.Screen.OnActivated();
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);

        fixture.Screen.Render(canvas, ScreenWidth, ScreenHeight);
        canvas.Flush();

        var panel = TradeLayout.PanelRect(ScreenWidth, ScreenHeight);
        var buy = TradeLayout.BuyButtonRect();
        Assert.Equal(255, bitmap.GetPixel((int)panel.MidX, (int)panel.Top + 70).Alpha);
        Assert.True(bitmap.GetPixel(
            (int)(panel.Left + buy.X + buy.W / 2f),
            (int)(panel.Top + buy.Y + buy.H / 2f)).Alpha > 0);
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

        // §59 regression: item.energy-cells is Category=Good (package step 10) — the
        // preselected default quantity must be a whole sell package, not a bare 1 (which the
        // Engine's authoritative Sell package-size check would always reject).
        Assert.Equal("item.energy-cells", fixture.Screen.SelectedItemTypeId);
        Assert.Equal(10, fixture.Screen.Quantity);
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

        // §59 regression: selecting a Good-category row must default to a whole sell package
        // (10), not a bare 1 — see Sell_of_a_good_category_item_is_accepted_by_the_real_engine.
        Assert.Equal("item.energy-cells", fixture.Screen.SelectedItemTypeId);
        Assert.Equal(10, fixture.Screen.Quantity);
    }

    [Fact]
    public async Task Quantity_stepper_plus_and_minus_change_quantity_by_the_selected_items_package_step()
    {
        // item.energy-cells (row 0) is Category=Good — package step 10 (§59). Selection now
        // defaults _quantity to 10 (one package), not 1 — see
        // Clicking_a_station_inventory_row_selects_that_item.
        await using var fixture = CreateDockedFixture();
        RenderScreen(fixture.Screen);
        SelectFirstItem(fixture);
        Assert.Equal(10, fixture.Screen.Quantity);

        var stepper = TradeLayout.QuantityStepperRect();
        float pl = TradeLayout.PanelLeft(ScreenWidth);
        float pt = TradeLayout.PanelTop(ScreenHeight);

        fixture.Screen.OnMouseDown(pl + stepper.X + stepper.W - 5f, pt + stepper.Y + stepper.H / 2f); // plus
        Assert.Equal(20, fixture.Screen.Quantity);

        fixture.Screen.OnMouseDown(pl + stepper.X + 5f, pt + stepper.Y + stepper.H / 2f); // minus
        Assert.Equal(10, fixture.Screen.Quantity);

        // Minus at the floor (one package = 10) never goes below one package — a value below
        // it would never be a valid Sell quantity for a Good-category item (§59).
        fixture.Screen.OnMouseDown(pl + stepper.X + 5f, pt + stepper.Y + stepper.H / 2f);
        Assert.Equal(10, fixture.Screen.Quantity);
        Assert.Equal(0, fixture.Screen.Quantity % 10);
    }

    [Fact]
    public async Task Quantity_stepper_steps_by_100_for_a_resource_category_item()
    {
        // item.ice (row 2 in BaseSnapshot) is Category=Resource — package step 100 (§59).
        // Selection now defaults _quantity to 100 (one package), not 1 (§59 regression: a bare
        // 1 is never a multiple of 100, so it would always be rejected as InvalidPackageQuantity).
        await using var fixture = CreateDockedFixture();
        RenderScreen(fixture.Screen);
        SelectRow(fixture, 2);
        Assert.Equal("item.ice", fixture.Screen.SelectedItemTypeId);
        Assert.Equal(100, fixture.Screen.Quantity);

        var stepper = TradeLayout.QuantityStepperRect();
        float pl = TradeLayout.PanelLeft(ScreenWidth);
        float pt = TradeLayout.PanelTop(ScreenHeight);

        fixture.Screen.OnMouseDown(pl + stepper.X + stepper.W - 5f, pt + stepper.Y + stepper.H / 2f); // plus
        Assert.Equal(200, fixture.Screen.Quantity);
        Assert.Equal(0, fixture.Screen.Quantity % 100);

        fixture.Screen.OnMouseDown(pl + stepper.X + 5f, pt + stepper.Y + stepper.H / 2f); // minus
        fixture.Screen.OnMouseDown(pl + stepper.X + 5f, pt + stepper.Y + stepper.H / 2f); // minus, at floor now
        Assert.Equal(100, fixture.Screen.Quantity);
        Assert.Equal(0, fixture.Screen.Quantity % 100);
    }

    [Fact]
    public async Task Quantity_stepper_steps_by_10_for_a_good_category_item_including_fuel_sold_as_cargo()
    {
        // item.fuel (row 1 in BaseSnapshot) is Category=Good — package step 10 (§59, decision 3:
        // Fuel sells as cargo with the same Good packaging; the Refuel panel's own stepper is unaffected).
        // Selection now defaults _quantity to 10 (one package), not 1.
        await using var fixture = CreateDockedFixture();
        RenderScreen(fixture.Screen);
        SelectRow(fixture, 1);
        Assert.Equal("item.fuel", fixture.Screen.SelectedItemTypeId);
        Assert.Equal(10, fixture.Screen.Quantity);

        var stepper = TradeLayout.QuantityStepperRect();
        float pl = TradeLayout.PanelLeft(ScreenWidth);
        float pt = TradeLayout.PanelTop(ScreenHeight);

        fixture.Screen.OnMouseDown(pl + stepper.X + stepper.W - 5f, pt + stepper.Y + stepper.H / 2f); // plus
        Assert.Equal(20, fixture.Screen.Quantity);
        Assert.Equal(0, fixture.Screen.Quantity % 10);
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
        // Buy shares the same _quantity/stepper as Sell (one Transaction column), so it now
        // also defaults to one sell package (10) instead of a bare 1 — harmless for Buy, which
        // has no package restriction and accepts any quantity including package multiples.
        Assert.Equal(10L, command.Quantity);
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
        // §59 regression: item.energy-cells is Category=Good (package step 10) — the sent
        // quantity after zero stepper clicks must already be a multiple of the package size,
        // or the Engine's authoritative Sell package-size check rejects it
        // (InvalidPackageQuantity) regardless of what the stepper does afterwards. See
        // Sell_of_a_good_category_item_after_zero_stepper_clicks_is_accepted_by_the_real_engine
        // for proof this is actually accepted, not just "a command was sent".
        Assert.Equal(10L, command.Quantity);
        Assert.Equal(0L, command.Quantity % 10);
    }

    [Fact]
    public async Task Sell_click_for_a_resource_category_item_sends_a_quantity_that_is_a_multiple_of_its_package_size()
    {
        // item.ice (row 2 in BaseSnapshot) is Category=Resource — package step 100 (§59).
        // Companion to the Good-category Sell_click test above, closing the same regression
        // class for the other category.
        await using var fixture = CreateDockedFixture();
        RenderScreen(fixture.Screen);
        SelectRow(fixture, 2);
        Assert.Equal("item.ice", fixture.Screen.SelectedItemTypeId);

        ClickButton(fixture.Screen, TradeLayout.SellButtonRect());

        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal(TradeCommandTypes.Sell, command.CommandType);
        Assert.Equal("item.ice", command.ItemTypeId);
        Assert.Equal(100L, command.Quantity);
        Assert.Equal(0L, command.Quantity % 100);
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

    // ── Real-engine regression (§59): the sequence "select a station row → Sell" with zero
    // stepper clicks must actually be ACCEPTED by the authoritative Engine, not merely "some
    // command was sent" — TradeScreenTests above use a RecordingConnection mock that never
    // validates package-size multiples itself, which is exactly why the InvalidPackageQuantity
    // regression this fixes went unnoticed by them. These two tests drive the real
    // SimulationEngine end to end (LocalGameSessionConnection, same wiring
    // LocalSessionIntegrationTests uses), for the two trade categories (Good step 10, Resource
    // step 100), and assert CommandResultStatus.Executed via the real engine's own resulting
    // state change (cargo actually debited) plus TradeScreen's own resolved success message —
    // never CommandReasonCodes.InvalidPackageQuantity. ─────────────────────────────────────

    private const string RealPlayerShipId = "SPC-0001";
    private const string RealStationObjectId = "SPC-0002";
    private const string RealCargoModuleId = "MOD-PLAYER-CARGO-01";
    private const string RealGoodItemTypeId = "item.energy-cells"; // Category=Good, package step 10
    private const string RealResourceItemTypeId = "item.ice"; // Category=Resource, package step 100
    private const long RealInitialCargoQuantity = 1000;

    [Fact]
    public async Task Sell_of_a_good_category_item_after_zero_stepper_clicks_is_accepted_by_the_real_engine()
    {
        await RunRealEngineSellAcceptanceCheckAsync(RealGoodItemTypeId, expectedQuantity: 10);
    }

    [Fact]
    public async Task Sell_of_a_resource_category_item_after_zero_stepper_clicks_is_accepted_by_the_real_engine()
    {
        await RunRealEngineSellAcceptanceCheckAsync(RealResourceItemTypeId, expectedQuantity: 100);
    }

    /// <summary>
    /// Drives TradeScreen against a real <see cref="SimulationEngine"/> (via
    /// <see cref="LocalGameSessionConnection.CreateFromScenarioFile"/>, production
    /// item-types/module-types/commands content — not a custom test registry): select the
    /// given item's station row (zero stepper clicks afterwards), click Sell, then prove the
    /// Engine actually executed it — not Rejected(InvalidPackageQuantity) — via both the
    /// player's cargo stack being debited server-side and TradeScreen's own resolved status
    /// message (Trade.StatusSellSuccess / Trade.StatusSellPartial, never a rejection reason).
    /// </summary>
    private static async Task RunRealEngineSellAcceptanceCheckAsync(string itemTypeId, long expectedQuantity)
    {
        string settingsPath = ResolveRealSettingsPath();
        string scenarioPath = WriteTempRealEngineScenario();
        try
        {
            await using var connection = LocalGameSessionConnection.CreateFromScenarioFile(settingsPath, scenarioPath);
            await using var handle = new GameSessionHandle(connection);

            await WaitUntilAsync(
                () => handle.Buffer.Latest?.Snapshot.DockedStationTrade is not null,
                "docked station trade snapshot");

            var screen = new TradeScreen(handle.Buffer, handle);
            screen.OnActivated();
            RenderScreen(screen); // establishes _screenWidth/_screenHeight for hit-testing

            var trade = handle.Buffer.Latest!.Snapshot.DockedStationTrade!;
            int rowIndex = Array.FindIndex(trade.Items.ToArray(), i => i.ItemTypeId == itemTypeId);
            Assert.True(rowIndex >= 0, $"'{itemTypeId}' not found in the real station's trade snapshot.");
            SelectRow(screen, rowIndex);

            // The fix under test: zero stepper clicks after selection must already be a
            // package-size multiple (10 for Good, 100 for Resource) — never a bare 1.
            Assert.Equal(expectedQuantity, screen.Quantity);
            Assert.Equal(0, screen.Quantity % expectedQuantity);

            long cargoBefore = RealCargoQuantity(handle, itemTypeId);

            ClickButton(screen, TradeLayout.SellButtonRect());

            await WaitUntilAsync(() =>
            {
                RenderScreen(screen); // TradeScreen only resolves CommandResults during Render
                return screen.LastStatusMessage is not null;
            }, "resolved Sell command result");

            // Ground truth: CommandResultStatus.Executed, proven by the Engine's own state
            // mutation (never happens on Rejected) — not just "a message string changed".
            long cargoAfter = RealCargoQuantity(handle, itemTypeId);
            Assert.True(cargoAfter < cargoBefore, $"Expected cargo to be debited by Sell (before={cargoBefore}, after={cargoAfter}).");
            Assert.Equal(0, (cargoBefore - cargoAfter) % expectedQuantity);

            // Station Credits (1,000,000) is far above what this sells, so it must be a full
            // fill, not a partial one — the success message is exact, not just "not a rejection".
            Assert.Equal(Localization.Get("Trade.StatusSellSuccess"), screen.LastStatusMessage);
            Assert.Equal(expectedQuantity, cargoBefore - cargoAfter);
        }
        finally
        {
            string? dir = Path.GetDirectoryName(scenarioPath);
            if (dir is not null && Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    private static long RealCargoQuantity(GameSessionHandle handle, string itemTypeId)
    {
        var modules = handle.Buffer.Latest?.Snapshot.InstalledModules ?? default;
        if (modules.IsDefaultOrEmpty)
            return 0;

        foreach (var module in modules)
        {
            if (module.ModuleId != RealCargoModuleId || module.Cargo.IsDefaultOrEmpty)
                continue;

            foreach (var stack in module.Cargo)
            {
                if (stack.ItemTypeId == itemTypeId)
                    return stack.Quantity;
            }
        }

        return 0;
    }

    private static async Task WaitUntilAsync(Func<bool> condition, string description)
    {
        var deadline = DateTime.UtcNow.AddSeconds(8);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;

            await Task.Delay(25);
        }

        Assert.Fail($"Timed out waiting for {description}.");
    }

    /// <summary>
    /// Minimal docked scenario against the real production content registry (item-types,
    /// module-types, Commands — loaded via <see cref="ResolveRealSettingsPath"/>'s
    /// Settings.json, same as <see cref="LocalSessionIntegrationTests"/>): player ship already
    /// docked, cargo pre-loaded with both a Good-category item (item.energy-cells) and a
    /// Resource-category item (item.ice) so both package sizes (10/100) can be exercised.
    /// Station Credits is set explicitly (well above any quantity this test sells) so the Sell
    /// always fully executes — no dependency on the deterministic-but-opaque masterSeed-derived
    /// random range (10,000..50,000) that would otherwise apply.
    /// </summary>
    private static string WriteTempRealEngineScenario()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"dss-trade-fix-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, "scenario.json");

        File.WriteAllText(path, $$"""
        {
          "scenarioMetadata": { "scenarioId": "trade-fix-regression", "name": "Trade Fix Regression" },
          "gameState": {
            "gameTimeMs": 0,
            "currentSpeed": "Speed0",
            "playerShipObjectId": "{{RealPlayerShipId}}",
            "playerCredits": 100000,
            "spaceObjects": [
              {
                "objectId": "{{RealPlayerShipId}}",
                "objectType": "PlayerShip",
                "persistenceType": "Permanent",
                "positionX": 10000, "positionY": 10000,
                "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary",
                "isDocked": true,
                "dockedStationObjectId": "{{RealStationObjectId}}",
                "hullLayout": { "width": 2, "height": 1, "cells": [ {"x":0,"y":0}, {"x":1,"y":0} ] },
                "modules": [
                  {
                    "moduleId": "{{RealCargoModuleId}}",
                    "moduleTypeId": "module.container.basic",
                    "occupiedCells": [ {"x":0,"y":0} ],
                    "structurePoints": 400,
                    "powerState": "On",
                    "operationalState": "Ready",
                    "activeCycle": null,
                    "cargo": [
                      { "itemTypeId": "{{RealGoodItemTypeId}}", "quantity": {{RealInitialCargoQuantity}} },
                      { "itemTypeId": "{{RealResourceItemTypeId}}", "quantity": {{RealInitialCargoQuantity}} }
                    ]
                  },
                  {
                    "moduleId": "MOD-PLAYER-ENGINE-01",
                    "moduleTypeId": "module.engine.basic",
                    "occupiedCells": [ {"x":1,"y":0} ],
                    "structurePoints": 100,
                    "powerState": "On",
                    "operationalState": "Ready",
                    "activeCycle": null,
                    "cargo": [],
                    "fuelAmountKg": 0
                  }
                ]
              },
              {
                "objectId": "{{RealStationObjectId}}",
                "objectType": "Station",
                "persistenceType": "Permanent",
                "positionX": 10001, "positionY": 10000,
                "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary",
                "credits": 1000000,
                "inventory": [
                  { "itemTypeId": "{{RealGoodItemTypeId}}", "quantity": 1000 },
                  { "itemTypeId": "{{RealResourceItemTypeId}}", "quantity": 1000 }
                ]
              }
            ]
          }
        }
        """);

        return path;
    }

    private static string ResolveRealSettingsPath()
    {
        string settingsPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "DeepSpaceSaga.Client", "Settings.json"));

        if (!File.Exists(settingsPath))
        {
            settingsPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory, "Settings.json"));
        }

        return settingsPath;
    }

    // ── Test helpers ─────────────────────────────────────────────────────────

    private static void SelectFirstItem(TestFixture fixture) => SelectRow(fixture.Screen, 0);

    private static void SelectRow(TestFixture fixture, int rowIndex) => SelectRow(fixture.Screen, rowIndex);

    private static void SelectRow(TradeScreen screen, int rowIndex)
    {
        var row = TradeLayout.InventoryRowRect(rowIndex);
        float cx = TradeLayout.PanelLeft(ScreenWidth) + row.X + row.W / 2f;
        float cy = TradeLayout.PanelTop(ScreenHeight) + row.Y + row.H / 2f;
        screen.OnMouseDown(cx, cy);
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
                new StationInventoryItemSnapshot("item.energy-cells", StockQuantity: 100, UnitPriceCredits: 200, MaxSellableQuantity: 50, Category: TradeItemCategories.Good),
                new StationInventoryItemSnapshot("item.fuel", StockQuantity: 500, UnitPriceCredits: 200, MaxSellableQuantity: 20, Category: TradeItemCategories.Good),
                new StationInventoryItemSnapshot("item.ice", StockQuantity: 300, UnitPriceCredits: 30, MaxSellableQuantity: 300, Category: TradeItemCategories.Resource)));

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
