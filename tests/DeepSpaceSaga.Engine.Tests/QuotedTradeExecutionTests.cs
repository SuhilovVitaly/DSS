using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// EP-0001-US-0003-TK-0002 — atomic execution of an engine-issued trade quote. Every quote here comes
/// from <c>SimulationEngine.GetTradeQuote</c> (never a hand-made total), and the world is the shipped
/// catalog with one bounded market profile spliced in, so prices come from the real formula.
/// </summary>
public class QuotedTradeExecutionTests
{
    internal const string ShipId = "SPC-0001";
    internal const string StationId = "SPC-0002";
    internal const string OtherStationId = "SPC-0999";
    internal const string CargoModuleId = "MOD-PLAYER-CARGO-01";
    internal const string SecondCargoModuleId = "MOD-PLAYER-CARGO-02";
    internal const string EngineModuleId = "MOD-PLAYER-ENGINE-01";
    internal const string BridgeModuleId = "MOD-PLAYER-BRIDGE-01";

    internal const string Ice = "item.ice";
    internal const string Water = "item.water";
    internal const string Steel = "item.steel";
    internal const string Fuel = "item.fuel";
    internal const string EnergyCells = "item.energy-cells";

    // ---------------------------------------------------------------------------------------
    // Bounded-market fixture: a copy of EconomyTimeContinuityTests' private BoundedProfile /
    // MarketRegistry / MarketTemplate (those helpers are private to that class). Dialogues are
    // spliced in as well so the ship can undock and dock at a second station within one session.
    // ---------------------------------------------------------------------------------------

    internal const string MarketProfileId = "market.quoted";
    internal const long IceTarget = 108;
    internal const long WaterTarget = 72;
    internal const long SteelTarget = 72;
    internal const long MarketInitialCredits = 9600;

    internal static readonly GameDataRegistry Registry = MarketRegistry(BoundedProfile());

    internal static ImmutableDictionary<StationSize, int> MarketSizeFactors =>
        new Dictionary<StationSize, int>
        {
            [StationSize.Outpost] = 500,
            [StationSize.Medium] = 1000,
            [StationSize.Large] = 1500,
            [StationSize.Huge] = 2000,
        }.ToImmutableDictionary();

    internal static StationMarketProfileDefinition BoundedProfile() =>
        new(
            TypeId: MarketProfileId,
            DisplayName: MarketProfileId,
            SupplyItemTypeIds: [Ice],
            DemandItemTypeIds: [Water, Steel],
            InitialInventory: [new(Ice, IceTarget), new(Water, WaterTarget), new(Steel, SteelTarget)],
            InitialCredits: MarketInitialCredits,
            RefuelStockKg: 200,
            SizeFactors: MarketSizeFactors,
            Economy: new StationMarketEconomyDefinition(
                ProductionSource: StationMarketProductionSource.Profile,
                HourlyInputs: [new(Water, 4)],
                HourlyOutputs: [new(Ice, 18)],
                HourlyConsumption: [new(Steel, 2)],
                StockTargets: [new(Ice, IceTarget), new(Water, WaterTarget), new(Steel, SteelTarget)],
                ShortageThresholdPermille: 500,
                SurplusThresholdPermille: 1500,
                BudgetRegenerationDivisorPerDay: 24));

    internal static GameDataRegistry RealRegistry()
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
        return EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
    }

    internal static GameDataRegistry MarketRegistry(StationMarketProfileDefinition profile)
    {
        var source = RealRegistry();
        return GameDataRegistry.Create(
            Enumerable.Range(0, source.ModuleCategories.Count).Select(source.ModuleCategories.GetDefinition),
            Enumerable.Range(0, source.ModuleTypes.Count).Select(source.ModuleTypes.GetDefinition),
            Enumerable.Range(0, source.ItemTypes.Count).Select(source.ItemTypes.GetDefinition),
            Enumerable.Range(0, source.CommandDefinitions.Count).Select(source.CommandDefinitions.GetDefinition),
            Enumerable.Range(0, source.FactoryTypes.Count).Select(source.FactoryTypes.GetDefinition),
            Enumerable.Range(0, source.Recipes.Count).Select(source.Recipes.GetDefinition),
            dialogues: Enumerable.Range(0, source.Dialogues.Count).Select(source.Dialogues.GetDefinition),
            legacyCatalogFingerprint: source.LegacyCatalogFingerprint,
            stationMarketProfiles: Enumerable.Range(0, source.StationMarketProfiles.Count)
                .Select(source.StationMarketProfiles.GetDefinition).Append(profile));
    }

    internal static ScenarioFile MarketTemplate(IReadOnlyList<StationInventoryItemData>? stock = null)
    {
        using var template = RationScheduleTests.CreateEngine(0, passengers: 0, rations: 200);
        var save = template.CaptureSaveState();
        var gs = save.GameState;
        return save with
        {
            SaveFormatVersion = 0,
            GameState = gs with
            {
                TradingMap = null,
                SpaceObjects = gs.SpaceObjects
                    .Where(o => o.ObjectId == ShipId || o.ObjectId == StationId)
                    .Select(o => o.ObjectId != StationId ? o : o with
                    {
                        MarketProfileId = MarketProfileId,
                        MarketProfileFingerprint = null,
                        StationSize = nameof(StationSize.Medium),
                        Credits = null,
                        MarketBudgetCredits = null,
                        MarketRevision = null,
                        Inventory = stock,
                        ProducingModules = null,
                        Events = null,
                        PortFeeCreditsPerDay = null,
                    }).ToArray(),
            },
        };
    }

    internal static SimulationEngine CreateMarketEngine(
        IReadOnlyList<StationInventoryItemData>? stock = null,
        Func<ScenarioFile, ScenarioFile>? adjust = null)
    {
        var save = MarketTemplate(stock);
        if (adjust is not null) save = adjust(save);
        var engine = new SimulationEngine(Registry, [], new SimulationClock(SimulationSpeed.Speed0, () => 0));
        engine.LoadScenario(save);
        return engine;
    }

    internal static IReadOnlyList<StationInventoryItemData> IceStock(long ice) =>
        [new(Ice, ice), new(Water, WaterTarget), new(Steel, SteelTarget)];

    internal static ScenarioFile WithPlayerCredits(ScenarioFile save, long credits) =>
        save with { GameState = save.GameState with { PlayerTokens = credits } };

    internal static ScenarioFile WithStationCredits(ScenarioFile save, long credits) => save with
    {
        GameState = save.GameState with
        {
            SpaceObjects = save.GameState.SpaceObjects
                .Select(o => o.ObjectId != StationId ? o : o with { Credits = credits }).ToArray(),
        },
    };

    internal static ScenarioFile WithShipModules(ScenarioFile save, Func<ShipModuleData, ShipModuleData> update) => save with
    {
        GameState = save.GameState with
        {
            SpaceObjects = save.GameState.SpaceObjects
                .Select(o => o.ObjectId != ShipId ? o : o with { Modules = o.Modules!.Select(update).ToArray() }).ToArray(),
        },
    };

    internal static ScenarioFile WithCargo(ScenarioFile save, string itemTypeId, long quantity) =>
        WithShipModules(save, m => m.ModuleId != CargoModuleId ? m : m with
        {
            Cargo = (m.Cargo ?? []).Any(c => c.ItemTypeId == itemTypeId)
                ? m.Cargo!.Select(c => c.ItemTypeId == itemTypeId ? c with { Quantity = quantity } : c).ToArray()
                : (m.Cargo ?? []).Append(new CargoStackData(itemTypeId, quantity)).ToArray(),
        });

    internal static ScenarioFile WithFuel(ScenarioFile save, long fuelKg) =>
        WithShipModules(save, m => m.ModuleId != EngineModuleId ? m : m with { FuelAmountKg = fuelKg });

    /// <summary>A second container module on a free hull cell, so a quote can be replayed against another module.</summary>
    internal static ScenarioFile WithSecondContainer(ScenarioFile save) => save with
    {
        GameState = save.GameState with
        {
            SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId != ShipId ? o : o with
            {
                Modules = o.Modules!.Append(o.Modules!.Single(m => m.ModuleId == CargoModuleId) with
                {
                    ModuleId = SecondCargoModuleId,
                    OccupiedCells = [new HullCellCoordinate(3, 1)],
                    Cargo = [],
                }).ToArray(),
            }).ToArray(),
        },
    };

    /// <summary>A plain (profile-less) station at the market station's position, reachable by undock + dock.</summary>
    internal static ScenarioFile WithOtherStation(ScenarioFile save) => save with
    {
        GameState = save.GameState with
        {
            SpaceObjects = save.GameState.SpaceObjects.Append(
                save.GameState.SpaceObjects.Single(o => o.ObjectId == StationId) with
                {
                    ObjectId = OtherStationId,
                    Name = "Other Station",
                    MarketProfileId = null,
                    MarketProfileFingerprint = null,
                    MarketBudgetCredits = null,
                    MarketRevision = null,
                    Credits = 100_000,
                    Inventory = [new(Ice, 500)],
                    PortFeeCreditsPerDay = 10,
                }).ToArray(),
        },
    };

    // --- Queries ------------------------------------------------------------------------

    internal static SpaceObjectRuntime Station(SimulationEngine engine, string stationId = StationId) =>
        engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == stationId);

    internal static long Stock(SimulationEngine engine, string itemTypeId, string stationId = StationId)
    {
        int index = Registry.ItemTypes.GetIndex(itemTypeId);
        return Station(engine, stationId).Inventory.FirstOrDefault(i => i.ItemTypeIndex == index)?.StockQuantity ?? 0;
    }

    internal static InstalledModuleRuntime Module(SimulationEngine engine, string moduleId) =>
        engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == ShipId).Modules.Single(m => m.ModuleId == moduleId);

    internal static long CargoQuantity(SimulationEngine engine, string itemTypeId, string moduleId = CargoModuleId)
    {
        int index = Registry.ItemTypes.GetIndex(itemTypeId);
        return Module(engine, moduleId).Cargo.FirstOrDefault(c => c.ItemTypeIndex == index)?.Quantity ?? 0;
    }

    internal static long Revision(SimulationEngine engine, string stationId = StationId) =>
        Station(engine, stationId).MarketRevision;

    /// <summary>
    /// Save projection of money, stock, cargo, tank and budget, plus every runtime market revision (a
    /// profile-less station's revision is never saved).
    /// The command journal is excluded: a rejection legitimately adds its own receipt there.
    /// </summary>
    internal static string WorldProjection(SimulationEngine engine)
    {
        var save = engine.CaptureSaveState();
        string world = ScenarioLoader.Serialize(save with { GameState = save.GameState with { CommandReceipts = null } });
        return world + "|revisions=" + string.Join(",", engine.RuntimeObjects.Select(o => o.MarketRevision));
    }

    internal static TradeQuoteSnapshot Quote(
        SimulationEngine engine, string commandType, string itemTypeId, long quantity, string? moduleId = null) =>
        engine.GetTradeQuote(new TradeQuoteRequest(
            "req-" + Guid.NewGuid().ToString("N"), ShipId,
            moduleId ?? (commandType == TradeCommandTypes.Refuel ? EngineModuleId : CargoModuleId),
            commandType, itemTypeId, quantity));

    internal static PlayerCommand Bind(string commandId, TradeQuoteSnapshot quote) =>
        new(commandId, 1, quote.ObjectId, quote.ModuleId, quote.CommandType,
            ItemTypeId: quote.ItemTypeId, Quantity: quote.RequestedQuantity,
            QuoteId: quote.QuoteId, MarketRevision: quote.MarketRevision);

    internal static CommandResult Apply(SimulationEngine engine, PlayerCommand command)
    {
        engine.ReceiveCommand(command);
        return Assert.Single(engine.CaptureSnapshot().CommandResults);
    }

    internal static CommandResult ApplyAt(SimulationEngine engine, PlayerCommand command, long gameTimeMs)
    {
        engine.ReceiveCommand(command);
        return Assert.Single(engine.CaptureSnapshotForTests(gameTimeMs).CommandResults);
    }

    /// <summary>Price of the first unit of a quote curve (EP-0001-US-0015-TK-0004: a curve may have several steps).</summary>
    internal static long UnitPrice(TradeQuoteSnapshot quote) => quote.Curve[0].UnitPriceCredits;

    /// <summary>Checked total of the first <paramref name="units"/> units of a quote curve.</summary>
    internal static long PrefixTotal(TradeQuoteSnapshot quote, long units)
    {
        long total = 0;
        foreach (var step in quote.Curve)
        {
            long take = Math.Min(step.Quantity, units);
            total = checked(total + checked(take * step.UnitPriceCredits));
            units -= take;
            if (units == 0) break;
        }

        Assert.Equal(0, units);
        return total;
    }

    /// <summary>Static list price of one item in the docked station's snapshot row (the legacy charge).</summary>
    internal static long ListPrice(SimulationEngine engine, string itemTypeId) =>
        engine.CaptureSnapshot().DockedStationTrade!.Items.Single(i => i.ItemTypeId == itemTypeId).UnitPriceCredits;

    internal static void AssertEnabled(TradeQuoteSnapshot quote, long executable)
    {
        Assert.Null(quote.DisabledReason);
        Assert.StartsWith("QTE-", quote.QuoteId);
        Assert.Equal(executable, quote.ExecutableQuantity);
        Assert.Equal(executable, quote.Curve.Sum(s => s.Quantity));
        Assert.Equal(quote.Curve.Sum(s => s.Quantity * s.UnitPriceCredits), quote.TotalCredits);
    }

    internal static void AssertDisabled(TradeQuoteSnapshot quote, string reason)
    {
        Assert.Equal(reason, quote.DisabledReason);
        Assert.Equal("", quote.QuoteId);
        Assert.Equal(0, quote.ExecutableQuantity);
        Assert.Equal(0, quote.TotalCredits);
        Assert.Empty(quote.Curve);
    }

    /// <summary>Zero-effect rejection receipt: raw request fields echoed, known station and unchanged revision.</summary>
    internal static void AssertRejected(
        CommandResult result, string reason, PlayerCommand command, string? stationId, long? currentRevision)
    {
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(reason, result.ReasonCode);
        Assert.Null(result.ExecutedQuantity);
        var receipt = Assert.IsType<TradeExecutionReceipt>(result.TradeReceipt);
        Assert.Equal(stationId, receipt.StationObjectId);
        Assert.Equal(command.ItemTypeId, receipt.ItemTypeId);
        Assert.Equal(command.QuoteId, receipt.QuoteId);
        Assert.Equal(command.MarketRevision, receipt.QuotedMarketRevision);
        Assert.Equal(currentRevision, receipt.ResultMarketRevision);
        Assert.Equal(command.Quantity, receipt.RequestedQuantity);
        Assert.Equal(0, receipt.ExecutedQuantity);
        Assert.Equal(0, receipt.TotalCredits);
        Assert.Empty(receipt.LimitReasons);
    }

    /// <summary>Executed receipt that matches the quote it consumed exactly.</summary>
    internal static TradeExecutionReceipt AssertExecuted(CommandResult result, TradeQuoteSnapshot quote)
    {
        Assert.Equal(CommandResultStatus.Executed, result.Status);
        Assert.Null(result.ReasonCode);
        var receipt = Assert.IsType<TradeExecutionReceipt>(result.TradeReceipt);
        Assert.Equal(quote.StationObjectId, receipt.StationObjectId);
        Assert.Equal(quote.ItemTypeId, receipt.ItemTypeId);
        Assert.Equal(quote.QuoteId, receipt.QuoteId);
        Assert.Equal(quote.MarketRevision, receipt.QuotedMarketRevision);
        Assert.Equal(quote.MarketRevision + 1, receipt.ResultMarketRevision);
        Assert.Equal(quote.RequestedQuantity, receipt.RequestedQuantity);
        Assert.Equal(quote.ExecutableQuantity, receipt.ExecutedQuantity);
        Assert.Equal(quote.TotalCredits, receipt.TotalCredits);
        Assert.Equal(quote.LimitReasons.ToArray(), receipt.LimitReasons.ToArray());
        Assert.Equal(
            quote.ExecutableQuantity < quote.RequestedQuantity ? quote.ExecutableQuantity : null,
            result.ExecutedQuantity);
        return receipt;
    }

    /// <summary>Field-wise equality: ImmutableArray does not survive a JSON round trip by reference.</summary>
    internal static void AssertSameResult(CommandResult expected, CommandResult actual)
    {
        Assert.Equal(expected with { TradeReceipt = null }, actual with { TradeReceipt = null });
        var e = Assert.IsType<TradeExecutionReceipt>(expected.TradeReceipt);
        var a = Assert.IsType<TradeExecutionReceipt>(actual.TradeReceipt);
        Assert.Equal(e with { LimitReasons = default }, a with { LimitReasons = default });
        Assert.True(e.LimitReasons.SequenceEqual(a.LimitReasons));
    }

    internal static void Reload(SimulationEngine engine)
    {
        var save = engine.CaptureSaveState();
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true));
    }

    // --- AC-01 / AC-03 ------------------------------------------------------------------

    [Theory]
    [InlineData(TradeCommandTypes.Buy, Ice, 5L, 2000L, 750L, 1000L, null)]
    [InlineData(TradeCommandTypes.Buy, Ice, 100L, 500L, 750L, 1000L, CommandReasonCodes.InsufficientPlayerCredits)]
    [InlineData(TradeCommandTypes.Buy, Ice, 109L, 2000L, 750L, 1000L, CommandReasonCodes.InsufficientStationStock)]
    [InlineData(TradeCommandTypes.Buy, Ice, 10L, 2000L, 750L, 99_795L, CommandReasonCodes.CargoCapacityExceeded)]
    [InlineData(TradeCommandTypes.Refuel, Fuel, 100L, 2000L, 750L, 1000L, null)]
    [InlineData(TradeCommandTypes.Refuel, Fuel, 50L, 100L, 750L, 1000L, CommandReasonCodes.InsufficientPlayerCredits)]
    [InlineData(TradeCommandTypes.Refuel, Fuel, 201L, 5000L, 750L, 1000L, CommandReasonCodes.InsufficientStationStock)]
    [InlineData(TradeCommandTypes.Refuel, Fuel, 60L, 2000L, 950L, 1000L, CommandReasonCodes.FuelCapacityExceeded)]
    public void Buy_and_refuel_commit_whole_quote_or_leave_all_state_unchanged(
        string commandType, string itemTypeId, long quantity, long playerCredits, long fuelKg, long energyCells,
        string? expectedReason)
    {
        using var engine = CreateMarketEngine(adjust: save =>
            WithCargo(WithFuel(WithPlayerCredits(save, playerCredits), fuelKg), EnergyCells, energyCells));
        bool refuel = commandType == TradeCommandTypes.Refuel;
        long playerBefore = engine.PlayerCredits;
        long stationCreditsBefore = Station(engine).Credits;
        long budgetBefore = Station(engine).MarketBudgetCredits!.Value;
        long stockBefore = Stock(engine, itemTypeId);
        long cargoBefore = CargoQuantity(engine, itemTypeId);
        long tankBefore = Module(engine, EngineModuleId).FuelAmountKg;
        long revisionBefore = Revision(engine);
        Assert.Equal(1, revisionBefore);

        var quote = Quote(engine, commandType, itemTypeId, quantity);
        Assert.Equal(StationId, quote.StationObjectId);
        Assert.Equal(revisionBefore, quote.MarketRevision);
        Assert.Equal(quantity, quote.RequestedQuantity);

        if (expectedReason is not null)
        {
            // A refused quote is never issued, and whatever the client sends for it changes nothing.
            AssertDisabled(quote, expectedReason);
            Assert.True(quote.MaximumQuantity < quantity);
            string before = WorldProjection(engine);
            var command = Bind("cmd-refused", quote);
            var refused = Apply(engine, command);
            AssertRejected(refused, CommandReasonCodes.InvalidQuote, command, StationId, revisionBefore);
            Assert.Equal(before, WorldProjection(engine));
            return;
        }

        AssertEnabled(quote, quantity);
        Assert.True(quote.MaximumQuantity >= quantity);
        Assert.Empty(quote.LimitReasons);
        long total = quote.TotalCredits;
        // The total is the sequential curve, not quantity × first price (EP-0001-US-0015-TK-0004).
        Assert.Equal(PrefixTotal(quote, quantity), total);
        if (refuel)
            Assert.Equal(quantity * UnitPrice(quote), total);

        var result = Apply(engine, Bind("cmd-ok", quote));
        var receipt = AssertExecuted(result, quote);
        Assert.Null(result.ExecutedQuantity);
        Assert.Equal(quantity, receipt.ExecutedQuantity);
        Assert.Equal(revisionBefore + 1, receipt.ResultMarketRevision);
        Assert.Equal(revisionBefore + 1, Revision(engine));

        Assert.Equal(playerBefore - total, engine.PlayerCredits);
        Assert.Equal(stationCreditsBefore + total, Station(engine).Credits);
        Assert.Equal(Math.Min(2 * MarketInitialCredits, budgetBefore + total), Station(engine).MarketBudgetCredits);
        Assert.Equal(stockBefore - quantity, Stock(engine, itemTypeId));
        Assert.Equal(refuel ? cargoBefore : cargoBefore + quantity, CargoQuantity(engine, itemTypeId));
        Assert.Equal(refuel ? tankBefore + quantity : tankBefore, Module(engine, EngineModuleId).FuelAmountKg);
    }

    // --- AC-02 --------------------------------------------------------------------------

    [Theory]
    [InlineData("none")]
    [InlineData("budget")]
    [InlineData("capacity")]
    [InlineData("both")]
    [InlineData("budget-zero")]
    [InlineData("capacity-zero")]
    public void Sell_fills_largest_budget_and_capacity_prefix_and_receipts_actual_total(string limit)
    {
        long iceStock = limit switch
        {
            "capacity" or "both" => 2 * IceTarget - 4,
            "capacity-zero" => 2 * IceTarget,
            _ => IceTarget,
        };

        // Budgets are sums of the sequential sell curve at the same starting stock (EP-0001-US-0015-TK-0004).
        long probeQuantity = limit switch
        {
            "capacity" or "both" => 4,
            "capacity-zero" => 0,
            _ => 10,
        };
        TradeQuoteSnapshot? probeQuote = null;
        if (probeQuantity > 0)
        {
            using var probe = CreateMarketEngine(IceStock(iceStock), save => WithCargo(save, Ice, 50));
            probeQuote = Quote(probe, TradeCommandTypes.Sell, Ice, probeQuantity);
            AssertEnabled(probeQuote, probeQuantity);
            Assert.True(UnitPrice(probeQuote) > 1);
        }

        long? stationCredits = limit switch
        {
            "budget" => PrefixTotal(probeQuote!, 5) + 3,
            "both" => PrefixTotal(probeQuote!, 4) + 1,
            "budget-zero" => UnitPrice(probeQuote!) - 1,
            _ => null,
        };
        using var engine = CreateMarketEngine(IceStock(iceStock), save =>
        {
            save = WithCargo(save, Ice, 50);
            return stationCredits is { } credits ? WithStationCredits(save, credits) : save;
        });

        const long requested = 10;
        long playerBefore = engine.PlayerCredits;
        long stationCreditsBefore = Station(engine).Credits;
        long budgetBefore = Station(engine).MarketBudgetCredits!.Value;
        long revisionBefore = Revision(engine);
        var quote = Quote(engine, TradeCommandTypes.Sell, Ice, requested);

        if (limit is "budget-zero" or "capacity-zero")
        {
            AssertDisabled(quote, limit == "budget-zero"
                ? CommandReasonCodes.StationBudgetExceeded
                : CommandReasonCodes.StationCapacityExceeded);
            string before = WorldProjection(engine);
            var command = Bind("sell-zero", quote);
            AssertRejected(Apply(engine, command), CommandReasonCodes.InvalidQuote, command, StationId, revisionBefore);
            Assert.Equal(before, WorldProjection(engine));
            return;
        }

        (long expected, string[] reasons) = limit switch
        {
            "budget" => (5L, new[] { CommandReasonCodes.StationBudgetExceeded }),
            "capacity" => (4L, new[] { CommandReasonCodes.StationCapacityExceeded }),
            "both" => (4L, new[] { CommandReasonCodes.StationBudgetExceeded, CommandReasonCodes.StationCapacityExceeded }),
            _ => (requested, Array.Empty<string>()),
        };
        AssertEnabled(quote, expected);
        Assert.Equal(reasons, quote.LimitReasons.ToArray());
        Assert.Equal(PrefixTotal(probeQuote!, expected), quote.TotalCredits);

        var result = Apply(engine, Bind("sell", quote));
        var receipt = AssertExecuted(result, quote);
        Assert.Equal(expected, receipt.ExecutedQuantity);
        Assert.Equal(expected < requested ? expected : null, result.ExecutedQuantity);
        Assert.Equal(reasons, receipt.LimitReasons.ToArray());

        long total = quote.TotalCredits;
        Assert.Equal(total, receipt.TotalCredits);
        Assert.Equal(playerBefore + total, engine.PlayerCredits);
        Assert.Equal(stationCreditsBefore - total, Station(engine).Credits);
        Assert.Equal(budgetBefore - total, Station(engine).MarketBudgetCredits);
        Assert.Equal(iceStock + expected, Stock(engine, Ice));
        Assert.Equal(50 - expected, CargoQuantity(engine, Ice));
        Assert.Equal(revisionBefore + 1, Revision(engine));
    }

    [Fact]
    public void Zero_fill_missing_cargo_and_arithmetic_overflow_are_zero_effect()
    {
        // No cargo to sell: the engine refuses to quote, and a made-up token is simply unknown.
        using (var engine = CreateMarketEngine())
        {
            AssertDisabled(Quote(engine, TradeCommandTypes.Sell, Ice, 5), CommandReasonCodes.InsufficientCargoQuantity);
            string before = WorldProjection(engine);
            var forged = new PlayerCommand("sell-forged", 1, ShipId, CargoModuleId, TradeCommandTypes.Sell,
                ItemTypeId: Ice, Quantity: 5, QuoteId: "QTE-forged-1", MarketRevision: Revision(engine));
            AssertRejected(Apply(engine, forged), CommandReasonCodes.StaleQuote, forged, StationId, 1);
            Assert.Equal(before, WorldProjection(engine));
        }

        // Sell proceeds that would overflow the player's balance: since EP-0001-US-0015-TK-0004 the issuer
        // caps Sell by the player's remaining headroom and refuses the whole quote with value_overflow, so
        // nothing executable is ever issued; whatever the client sends for it is a zero-effect rejection.
        using (var engine = CreateMarketEngine(adjust: save => WithCargo(WithPlayerCredits(save, long.MaxValue - 5), Ice, 10)))
        {
            var quote = Quote(engine, TradeCommandTypes.Sell, Ice, 10);
            AssertDisabled(quote, "value_overflow");
            string before = WorldProjection(engine);
            var command = Bind("sell-overflow", quote);
            AssertRejected(Apply(engine, command), CommandReasonCodes.InvalidQuote, command, StationId, 1);
            Assert.Equal(before, WorldProjection(engine));

            // Nothing was consumed: the same binding fails the same way again.
            var retry = Bind("sell-overflow-retry", quote);
            AssertRejected(Apply(engine, retry), CommandReasonCodes.InvalidQuote, retry, StationId, 1);
            Assert.Equal(before, WorldProjection(engine));
        }

        // Buy income that would overflow the station's hidden Credits is refused at quote issuance too.
        using (var engine = CreateMarketEngine(adjust: save => WithStationCredits(save, long.MaxValue - 5)))
        {
            var quote = Quote(engine, TradeCommandTypes.Buy, Ice, 5);
            AssertDisabled(quote, "value_overflow");
            string before = WorldProjection(engine);
            var command = Bind("buy-overflow", quote);
            AssertRejected(Apply(engine, command), CommandReasonCodes.InvalidQuote, command, StationId, 1);
            Assert.Equal(before, WorldProjection(engine));
        }
    }

    [Fact]
    public void Fuel_cannot_be_bought_sold_or_refuelled_as_another_item()
    {
        using (var engine = CreateMarketEngine())
        {
            AssertDisabled(Quote(engine, TradeCommandTypes.Buy, Fuel, 10), CommandReasonCodes.FuelTradeForbidden);
            AssertDisabled(Quote(engine, TradeCommandTypes.Sell, Fuel, 10), CommandReasonCodes.FuelTradeForbidden);
            AssertDisabled(Quote(engine, TradeCommandTypes.Refuel, Ice, 10), CommandReasonCodes.FuelTradeForbidden);

            // The storage guard runs before any quote lookup on the quoted path.
            string before = WorldProjection(engine);
            var quoted = new[]
            {
                new PlayerCommand("q-buy-fuel", 1, ShipId, CargoModuleId, TradeCommandTypes.Buy,
                    ItemTypeId: Fuel, Quantity: 10, QuoteId: "QTE-any-1", MarketRevision: 1),
                new PlayerCommand("q-sell-fuel", 2, ShipId, CargoModuleId, TradeCommandTypes.Sell,
                    ItemTypeId: Fuel, Quantity: 10, QuoteId: "QTE-any-2", MarketRevision: 1),
                new PlayerCommand("q-refuel-ice", 3, ShipId, EngineModuleId, TradeCommandTypes.Refuel,
                    ItemTypeId: Ice, Quantity: 10, QuoteId: "QTE-any-3", MarketRevision: 1),
            };
            foreach (var command in quoted)
                AssertRejected(Apply(engine, command), CommandReasonCodes.FuelTradeForbidden, command, StationId, 1);
            Assert.Equal(before, WorldProjection(engine));
        }

        // Legacy (unquoted) path at a profile-less station: same guard, no receipt.
        using (var legacy = TradeCommandTests.CreateEngine(shipCargo: [(Ice, 10)]))
        {
            var before = legacy.CaptureSaveState();
            var commands = new[]
            {
                new PlayerCommand("l-buy-fuel", 1, "SPC-0001", "MOD-CARGO-01", TradeCommandTypes.Buy, ItemTypeId: Fuel, Quantity: 10),
                new PlayerCommand("l-sell-fuel", 2, "SPC-0001", "MOD-CARGO-01", TradeCommandTypes.Sell, ItemTypeId: Fuel, Quantity: 10),
                new PlayerCommand("l-refuel-ice", 3, "SPC-0001", "MOD-ENG-01", TradeCommandTypes.Refuel, ItemTypeId: Ice, Quantity: 10),
            };
            foreach (var command in commands)
            {
                var result = Apply(legacy, command);
                Assert.Equal(CommandResultStatus.Rejected, result.Status);
                Assert.Equal(CommandReasonCodes.FuelTradeForbidden, result.ReasonCode);
                Assert.Null(result.TradeReceipt);
            }

            var after = legacy.CaptureSaveState();
            Assert.Equal(before.GameState.PlayerTokens, after.GameState.PlayerTokens);
            Assert.Equal(
                ScenarioLoader.Serialize(before with { GameState = before.GameState with { CommandReceipts = null } }),
                ScenarioLoader.Serialize(after with { GameState = after.GameState with { CommandReceipts = null } }));
        }
    }

    // --- AC-04 --------------------------------------------------------------------------

    [Theory]
    [InlineData("module", CommandReasonCodes.InvalidQuote)]
    [InlineData("item", CommandReasonCodes.InvalidQuote)]
    [InlineData("quantity", CommandReasonCodes.InvalidQuote)]
    [InlineData("type", CommandReasonCodes.InvalidQuote)]
    [InlineData("revision", CommandReasonCodes.InvalidQuote)]
    [InlineData("station", CommandReasonCodes.InvalidQuote)]
    [InlineData("missing-revision", CommandReasonCodes.InvalidQuote)]
    [InlineData("missing-id", CommandReasonCodes.InvalidQuote)]
    [InlineData("empty-id", CommandReasonCodes.InvalidQuote)]
    [InlineData("zero-revision", CommandReasonCodes.InvalidQuote)]
    [InlineData("negative-revision", CommandReasonCodes.InvalidQuote)]
    [InlineData("hour", CommandReasonCodes.StaleQuote)]
    [InlineData("player-credits", CommandReasonCodes.StaleQuote)]
    [InlineData("stock", CommandReasonCodes.StaleQuote)]
    [InlineData("unknown-id", CommandReasonCodes.StaleQuote)]
    public void Stale_and_cross_station_module_item_quantity_quotes_are_zero_effect(string variant, string expectedReason)
    {
        const long portFeeStart = GameCalendar.DayMs + 29 * 60_000;
        using var engine = CreateMarketEngine(adjust: save =>
        {
            save = WithCargo(WithOtherStation(WithSecondContainer(save)), Ice, 20);
            if (variant != "player-credits") return save;
            // A port fee falls due two minutes later, well away from any hour boundary.
            return save with
            {
                GameState = save.GameState with
                {
                    GameTimeMs = portFeeStart,
                    SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId switch
                    {
                        ShipId => o with
                        {
                            FirstPortFeeGameTimeMs = 30 * 60_000,
                            NextPortFeeDueGameTimeMs = GameCalendar.DayMs + 30 * 60_000,
                        },
                        StationId => o with { PortFeeCreditsPerDay = 100 },
                        _ => o,
                    }).ToArray(),
                },
            };
        });
        long now = variant == "player-credits" ? portFeeStart : 0;
        engine.CaptureSnapshotForTests(now);

        var quote = Quote(engine, TradeCommandTypes.Buy, Ice, 5);
        AssertEnabled(quote, 5);
        var command = Bind("cmd-" + variant, quote);
        string? expectedStation = StationId;

        switch (variant)
        {
            case "module":
                command = command with { ModuleId = SecondCargoModuleId };
                break;
            case "item":
                command = command with { ItemTypeId = Water };
                break;
            case "quantity":
                command = command with { Quantity = 4 };
                break;
            case "type":
                command = command with { CommandType = TradeCommandTypes.Sell };
                break;
            case "revision":
                command = command with { MarketRevision = quote.MarketRevision + 1 };
                break;
            case "missing-revision":
                command = command with { MarketRevision = null };
                break;
            case "missing-id":
                command = command with { QuoteId = null };
                break;
            case "empty-id":
                command = command with { QuoteId = "" };
                break;
            case "zero-revision":
                command = command with { MarketRevision = 0 };
                break;
            case "negative-revision":
                command = command with { MarketRevision = -1 };
                break;
            case "unknown-id":
                command = command with { QuoteId = "QTE-0000000000000000-999" };
                break;
            case "station":
                {
                    // Undock and dock at another station inside the same session: the quote is bound to the first.
                    Assert.Equal(CommandResultStatus.Executed, Apply(engine,
                        new PlayerCommand("undock", 1, ShipId, BridgeModuleId, NavigationComputerCommandTypes.Undock)).Status);
                    Assert.Equal(CommandResultStatus.Executed, Apply(engine,
                        new PlayerCommand("dock", 2, ShipId, BridgeModuleId, NavigationComputerCommandTypes.Dock,
                            TargetObjectId: OtherStationId)).Status);
                    DialogueTests.PayAndFinish(engine);
                    Assert.Equal(OtherStationId, engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == ShipId).DockedStationObjectId);
                    expectedStation = OtherStationId;
                    break;
                }
            case "hour":
                {
                    long stockBefore = Stock(engine, Ice);
                    Assert.True(engine.TravelStation(new("travel", StationDistrict.Market)).Accepted);
                    Assert.NotEqual(stockBefore, Stock(engine, Ice));
                    // The hourly market pass commits exactly one revision (EP-0001-US-0015-TK-0003).
                    Assert.Equal(quote.MarketRevision + 1, Revision(engine));
                    break;
                }
            case "player-credits":
                {
                    long creditsBefore = engine.PlayerCredits;
                    long stockBefore = Stock(engine, Ice);
                    now += 2 * 60_000;
                    engine.CaptureSnapshotForTests(now);
                    Assert.Equal(creditsBefore - 100, engine.PlayerCredits);
                    Assert.Equal(stockBefore, Stock(engine, Ice));
                    Assert.Equal(quote.MarketRevision, Revision(engine));
                    break;
                }
            case "stock":
                {
                    // An unquoted trade of the same item by another command moves stock (and the revision).
                    var other = Apply(engine, new PlayerCommand("legacy-buy", 9, ShipId, CargoModuleId, TradeCommandTypes.Buy,
                        ItemTypeId: Ice, Quantity: 1));
                    Assert.Equal(CommandResultStatus.Executed, other.Status);
                    break;
                }
        }

        string before = WorldProjection(engine);
        long? revisionNow = Revision(engine, expectedStation);
        var rejected = variant == "player-credits" ? ApplyAt(engine, command, now) : Apply(engine, command);
        AssertRejected(rejected, expectedReason, command, expectedStation, revisionNow);
        Assert.Equal(before, WorldProjection(engine));

        // A fresh quote for the current market still executes after the refusal.
        var fresh = Quote(engine, TradeCommandTypes.Buy, Ice, 5);
        AssertEnabled(fresh, 5);
        Assert.Equal(expectedStation, fresh.StationObjectId);
        var freshCommand = Bind("fresh-" + variant, fresh);
        AssertExecuted(variant == "player-credits" ? ApplyAt(engine, freshCommand, now) : Apply(engine, freshCommand), fresh);
    }

    [Fact]
    public void Two_commands_from_one_revision_commit_once()
    {
        using var engine = CreateMarketEngine();
        var first = Quote(engine, TradeCommandTypes.Buy, Ice, 5);
        var second = Quote(engine, TradeCommandTypes.Buy, Ice, 5);
        AssertEnabled(first, 5);
        AssertEnabled(second, 5);
        Assert.Equal(first.MarketRevision, second.MarketRevision);
        Assert.NotEqual(first.QuoteId, second.QuoteId);
        long playerBefore = engine.PlayerCredits;
        long stockBefore = Stock(engine, Ice);

        // All three arrive in the same tick: one quote replayed under a new CommandId, one sibling quote.
        var commands = new[] { Bind("a", first), Bind("b", first), Bind("c", second) };
        foreach (var command in commands) engine.ReceiveCommand(command);
        var results = engine.CaptureSnapshot().CommandResults;

        Assert.Equal(3, results.Length);
        AssertExecuted(results[0], first);
        AssertRejected(results[1], CommandReasonCodes.StaleQuote, commands[1], StationId, first.MarketRevision + 1);
        AssertRejected(results[2], CommandReasonCodes.StaleQuote, commands[2], StationId, first.MarketRevision + 1);
        Assert.Equal(playerBefore - first.TotalCredits, engine.PlayerCredits);
        Assert.Equal(stockBefore - 5, Stock(engine, Ice));
        Assert.Equal(first.MarketRevision + 1, Revision(engine));
    }

    [Fact]
    public void Duplicate_command_returns_identical_receipt_before_and_after_reload()
    {
        using var engine = CreateMarketEngine();
        var quote = Quote(engine, TradeCommandTypes.Buy, Ice, 5);
        var command = Bind("dup", quote);
        var original = Apply(engine, command);
        AssertExecuted(original, quote);
        string executed = WorldProjection(engine);

        // Retry of the same CommandId before a reload: the journal answers, the world stays put.
        AssertSameResult(original, Apply(engine, command));
        Assert.Equal(executed, WorldProjection(engine));

        Reload(engine);
        Assert.Equal(original.TradeReceipt!.ResultMarketRevision, Revision(engine));
        string reloaded = WorldProjection(engine);
        AssertSameResult(original, Apply(engine, command));
        Assert.Equal(reloaded, WorldProjection(engine));
    }

    [Fact]
    public void Evicted_receipt_cannot_reexecute_consumed_quote()
    {
        using var engine = CreateMarketEngine();
        var quote = Quote(engine, TradeCommandTypes.Buy, Ice, 5);
        var command = Bind("evicted", quote);
        AssertExecuted(Apply(engine, command), quote);

        // Push the original receipt out of the bounded journal with other terminal results.
        for (int i = 0; i <= SimulationEngine.CommandReceiptLimit; i++)
            engine.ReceiveCommand(new PlayerCommand($"filler-{i}", (ulong)i, "missing-object", CargoModuleId, "engine.none"));
        var fillers = engine.CaptureSnapshot().CommandResults;
        Assert.Equal(SimulationEngine.CommandReceiptLimit + 1, fillers.Length);
        Assert.All(fillers, r => Assert.Equal(CommandReasonCodes.UnknownObject, r.ReasonCode));

        // The CommandId is forgotten, so the retry is treated as new — and the consumed quote is stale.
        string before = WorldProjection(engine);
        long revision = Revision(engine);
        AssertRejected(Apply(engine, command), CommandReasonCodes.StaleQuote, command, StationId, revision);
        Assert.Equal(before, WorldProjection(engine));
    }

    [Fact]
    public void Partial_receipt_survives_save_and_malformed_receipt_is_rejected()
    {
        // The budget covers exactly the first three units of the sequential sell curve.
        long budget;
        using (var probe = CreateMarketEngine(adjust: save => WithCargo(save, Ice, 50)))
            budget = PrefixTotal(Quote(probe, TradeCommandTypes.Sell, Ice, 10), 3);
        using var engine = CreateMarketEngine(adjust: save => WithStationCredits(WithCargo(save, Ice, 50), budget));
        var quote = Quote(engine, TradeCommandTypes.Sell, Ice, 10);
        AssertEnabled(quote, 3);
        var command = Bind("partial", quote);
        var original = Apply(engine, command);
        AssertExecuted(original, quote);
        Assert.Equal(3, original.ExecutedQuantity);

        var save = engine.CaptureSaveState();
        var saved = Assert.Single(save.GameState.CommandReceipts!, r => r.CommandId == "partial");
        Assert.Equal(new[] { CommandReasonCodes.StationBudgetExceeded }, saved.TradeReceipt!.LimitReasons.ToArray());

        // A fresh engine restores the receipt; the profile market's revision comes from the save
        // (EP-0001-US-0015-TK-0003), a profile-less one's from the journal.
        Assert.Equal(original.TradeReceipt!.ResultMarketRevision,
            save.GameState.SpaceObjects.Single(o => o.ObjectId == StationId).MarketRevision);
        using var restored = new SimulationEngine(Registry, [], new SimulationClock(SimulationSpeed.Speed0, () => 0));
        restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true));
        Assert.Equal(original.TradeReceipt!.ResultMarketRevision, Revision(restored));
        string world = WorldProjection(restored);
        AssertSameResult(original, Apply(restored, command));
        Assert.Equal(world, WorldProjection(restored));

        // Legacy results without a receipt stay loadable: the saved profile revision alone is kept, and a
        // save predating marketRevision (and without receipts) starts the profile market at 1.
        var legacyOnly = save with
        {
            GameState = save.GameState with
            {
                CommandReceipts = save.GameState.CommandReceipts!.Select(r => r with { TradeReceipt = null }).ToArray(),
            },
        };
        using (var legacyEngine = new SimulationEngine(Registry, [], new SimulationClock(SimulationSpeed.Speed0, () => 0)))
        {
            legacyEngine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(legacyOnly), true));
            Assert.Equal(original.TradeReceipt!.ResultMarketRevision, Revision(legacyEngine));

            var withoutRevision = legacyOnly with
            {
                GameState = legacyOnly.GameState with
                {
                    SpaceObjects = legacyOnly.GameState.SpaceObjects
                        .Select(o => o with { MarketRevision = null }).ToArray(),
                },
            };
            legacyEngine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(withoutRevision), true));
            Assert.Equal(1, Revision(legacyEngine));
        }

        var executed = saved.TradeReceipt!;
        var rejectedResult = saved with
        {
            CommandId = "rejected",
            Status = CommandResultStatus.Rejected,
            ReasonCode = CommandReasonCodes.StaleQuote,
            ExecutedQuantity = null,
            TradeReceipt = executed with
            {
                ResultMarketRevision = 1,
                ExecutedQuantity = 0,
                TotalCredits = 0,
                LimitReasons = [],
                RequestedQuantity = -7,
                QuotedMarketRevision = -3,
                QuoteId = null,
                ItemTypeId = null,
            },
        };
        // A rejection may echo a nonsensical raw request; that alone does not invalidate a save.
        using (var rawEngine = new SimulationEngine(Registry, [], new SimulationClock(SimulationSpeed.Speed0, () => 0)))
        {
            var withRejection = save with
            {
                GameState = save.GameState with { CommandReceipts = save.GameState.CommandReceipts!.Append(rejectedResult).ToArray() },
            };
            rawEngine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(withRejection), true));
        }

        var malformed = new (string Name, CommandResult Result)[]
        {
            ("next revision", saved with { TradeReceipt = executed with { ResultMarketRevision = executed.QuotedMarketRevision } }),
            ("quoted revision", saved with { TradeReceipt = executed with { QuotedMarketRevision = 0, ResultMarketRevision = 1 } }),
            ("executed above requested", saved with { TradeReceipt = executed with { ExecutedQuantity = 11 } }),
            ("zero executed", saved with { TradeReceipt = executed with { ExecutedQuantity = 0 } }),
            ("requested", saved with { TradeReceipt = executed with { RequestedQuantity = 0 } }),
            ("negative total", saved with { TradeReceipt = executed with { TotalCredits = -1 } }),
            ("empty limit reason", saved with { TradeReceipt = executed with { LimitReasons = [""] } }),
            ("missing station", saved with { TradeReceipt = executed with { StationObjectId = null } }),
            ("missing item", saved with { TradeReceipt = executed with { ItemTypeId = " " } }),
            ("missing quote", saved with { TradeReceipt = executed with { QuoteId = null } }),
            ("partial buy", saved with { CommandType = TradeCommandTypes.Buy }),
            ("non-trade command", saved with { CommandType = "engine.turn-left" }),
            ("executed with reason", saved with { ReasonCode = CommandReasonCodes.StaleQuote }),
            ("rejected with fill", rejectedResult with { TradeReceipt = rejectedResult.TradeReceipt! with { ExecutedQuantity = 1 } }),
            ("rejected with total", rejectedResult with { TradeReceipt = rejectedResult.TradeReceipt! with { TotalCredits = 5 } }),
            ("rejected without revision", rejectedResult with { TradeReceipt = rejectedResult.TradeReceipt! with { ResultMarketRevision = null } }),
            ("rejected revision without station", rejectedResult with
            {
                TradeReceipt = rejectedResult.TradeReceipt! with { StationObjectId = null, ResultMarketRevision = 1 },
            }),
        };
        foreach (var (name, bad) in malformed)
        {
            var corrupted = save with
            {
                GameState = save.GameState with
                {
                    CommandReceipts = save.GameState.CommandReceipts!
                        .Select(r => r.CommandId == "partial" ? bad : r).ToArray(),
                },
            };
            long creditsBefore = restored.PlayerCredits;
            string worldBefore = WorldProjection(restored);
            var error = Assert.Throws<ScenarioException>(() => restored.LoadScenario(corrupted, isSave: true));
            Assert.True(error.Message == "Invalid saved trade receipt.", name);
            Assert.Equal(creditsBefore, restored.PlayerCredits);
            Assert.Equal(worldBefore, WorldProjection(restored));
        }
    }

    [Fact]
    public void Rejection_at_maximum_revision_keeps_save_loadable()
    {
        using var source = CreateMarketEngine(adjust: save => WithCargo(save, Ice, 50));
        var first = Quote(source, TradeCommandTypes.Sell, Ice, 1);
        AssertExecuted(Apply(source, Bind("first", first)), first);

        // A legitimate journal whose last commit reached the maximum revision.
        var save = source.CaptureSaveState();
        var atMaximum = save with
        {
            GameState = save.GameState with
            {
                CommandReceipts = save.GameState.CommandReceipts!.Select(r => r.CommandId != "first" ? r : r with
                {
                    TradeReceipt = r.TradeReceipt! with
                    {
                        QuotedMarketRevision = long.MaxValue - 1,
                        ResultMarketRevision = long.MaxValue,
                    },
                }).ToArray(),
            },
        };
        using var engine = new SimulationEngine(Registry, [], new SimulationClock(SimulationSpeed.Speed0, () => 0));
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(atMaximum), true));
        Assert.Equal(long.MaxValue, Revision(engine));

        // The next commit cannot advance the revision: zero effect, receipt keeps the current revision.
        var quote = Quote(engine, TradeCommandTypes.Sell, Ice, 1);
        AssertEnabled(quote, 1);
        string before = WorldProjection(engine);
        var command = Bind("overflow", quote);
        AssertRejected(Apply(engine, command), "value_overflow", command, StationId, long.MaxValue);
        Assert.Equal(before, WorldProjection(engine));

        // That rejection receipt is valid saved state.
        Reload(engine);
        Assert.Equal(long.MaxValue, Revision(engine));
        Assert.Equal(before, WorldProjection(engine));
    }

    // --- AC-07 --------------------------------------------------------------------------

    [Theory]
    [InlineData(40L, 1L)]
    [InlineData(40L, 40L)]
    [InlineData(IceTarget, 1L)]
    [InlineData(IceTarget, 100L)]
    [InlineData(200L, 1L)]
    [InlineData(200L, 100L)]
    public void Quote_prefix_equals_receipt_and_immediate_buy_sell_never_increases_balance(long stock, long quantity)
    {
        // Frozen time, no events, the same station and item: shortage, normal and surplus bands.
        using var engine = CreateMarketEngine(IceStock(stock));
        long balanceBefore = engine.PlayerCredits;

        var buy = Quote(engine, TradeCommandTypes.Buy, Ice, quantity);
        AssertEnabled(buy, quantity);
        var bought = AssertExecuted(Apply(engine, Bind("buy", buy)), buy);
        Assert.Equal(buy.Curve.Sum(s => s.Quantity * s.UnitPriceCredits), bought.TotalCredits);

        var sell = Quote(engine, TradeCommandTypes.Sell, Ice, quantity);
        AssertEnabled(sell, quantity);
        var sold = AssertExecuted(Apply(engine, Bind("sell", sell)), sell);
        Assert.Equal(sell.Curve.Sum(s => s.Quantity * s.UnitPriceCredits), sold.TotalCredits);

        Assert.True(sold.TotalCredits <= bought.TotalCredits);
        Assert.True(engine.PlayerCredits <= balanceBefore);
        Assert.Equal(stock, Stock(engine, Ice));
        Assert.Equal(3, Revision(engine));
    }

    // --- Legacy coexistence and quote identity ------------------------------------------

    [Fact]
    public void Unquoted_profile_trade_keeps_legacy_path_until_quote_ui()
    {
        using var engine = CreateMarketEngine();
        // The unquoted legacy path charges the static list price of the snapshot row, not the quote curve
        // (story CP-0 (c), R4): the two may differ.
        long price = ListPrice(engine, Ice);
        var pending = Quote(engine, TradeCommandTypes.Buy, Ice, 5);
        long playerBefore = engine.PlayerCredits;

        var result = Apply(engine, new PlayerCommand("unquoted", 1, ShipId, CargoModuleId, TradeCommandTypes.Buy,
            ItemTypeId: Ice, Quantity: 5));
        Assert.Equal(CommandResultStatus.Executed, result.Status);
        Assert.Null(result.ReasonCode);
        Assert.Null(result.ExecutedQuantity);
        Assert.Null(result.TradeReceipt);
        Assert.Equal(playerBefore - 5 * price, engine.PlayerCredits);

        // The legacy commit still advances the market revision, so quotes issued before it are stale.
        Assert.Equal(2, Revision(engine));
        var stale = Bind("after-legacy", pending);
        AssertRejected(Apply(engine, stale), CommandReasonCodes.StaleQuote, stale, StationId, 2);

        // Legacy Sell and Refuel advance it exactly once each as well.
        var sell = Apply(engine, new PlayerCommand("unquoted-sell", 2, ShipId, CargoModuleId, TradeCommandTypes.Sell,
            ItemTypeId: Ice, Quantity: 5));
        Assert.Equal(CommandResultStatus.Executed, sell.Status);
        Assert.Null(sell.TradeReceipt);
        Assert.Equal(3, Revision(engine));

        var refuel = Apply(engine, new PlayerCommand("unquoted-refuel", 3, ShipId, EngineModuleId, TradeCommandTypes.Refuel,
            ItemTypeId: Fuel, Quantity: 10));
        Assert.Equal(CommandResultStatus.Executed, refuel.Status);
        Assert.Null(refuel.TradeReceipt);
        Assert.Equal(4, Revision(engine));
    }

    [Fact]
    public void Quoted_command_rejected_before_trade_dispatch_still_gets_zero_effect_receipt()
    {
        // player_destroyed: the ship is still docked, so the receipt names the station and its revision.
        using (var engine = CreateMarketEngine(adjust: save => save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects
                    .Select(o => o.ObjectId == ShipId ? o with { IsDestroyed = true } : o).ToArray(),
            },
        }))
        {
            string before = WorldProjection(engine);
            var command = new PlayerCommand("destroyed", 1, ShipId, CargoModuleId, TradeCommandTypes.Buy,
                ItemTypeId: Ice, Quantity: 5, QuoteId: "QTE-any-1", MarketRevision: 1);
            AssertRejected(Apply(engine, command), "player_destroyed", command, StationId, 1);
            Assert.Equal(before, WorldProjection(engine));
        }

        // dialogue_active: undocked and negotiating with another station, so no station is known.
        using (var engine = CreateMarketEngine(adjust: WithOtherStation))
        {
            Assert.Equal(CommandResultStatus.Executed, Apply(engine,
                new PlayerCommand("undock", 1, ShipId, BridgeModuleId, NavigationComputerCommandTypes.Undock)).Status);
            Assert.Equal(CommandResultStatus.Executed, Apply(engine,
                new PlayerCommand("dock", 2, ShipId, BridgeModuleId, NavigationComputerCommandTypes.Dock,
                    TargetObjectId: OtherStationId)).Status);
            string before = WorldProjection(engine);
            var command = new PlayerCommand("in-dialogue", 3, ShipId, CargoModuleId, TradeCommandTypes.Sell,
                ItemTypeId: Ice, Quantity: 5, QuoteId: "QTE-any-2", MarketRevision: 1);
            AssertRejected(Apply(engine, command), "dialogue_active", command, null, null);
            Assert.Equal(before, WorldProjection(engine));

            // An unquoted command keeps the legacy rejection shape: no receipt.
            var unquoted = Apply(engine, command with { CommandId = "in-dialogue-legacy", QuoteId = null, MarketRevision = null });
            Assert.Equal("dialogue_active", unquoted.ReasonCode);
            Assert.Null(unquoted.TradeReceipt);
        }
    }

    [Fact]
    public void Quote_ids_are_unique_and_preload_quotes_are_stale_after_reload()
    {
        using var engine = CreateMarketEngine();
        var quotes = Enumerable.Range(0, 50).Select(_ => Quote(engine, TradeCommandTypes.Buy, Ice, 1)).ToArray();
        Assert.All(quotes, q => Assert.StartsWith("QTE-", q.QuoteId));
        Assert.Equal(quotes.Length, quotes.Select(q => q.QuoteId).Distinct(StringComparer.Ordinal).Count());

        // A disabled quote is not issued at all.
        AssertDisabled(Quote(engine, TradeCommandTypes.Buy, "item.does-not-exist", 1), CommandReasonCodes.UnknownItemType);
        AssertDisabled(Quote(engine, TradeCommandTypes.Buy, Ice, 0), CommandReasonCodes.InvalidQuantity);
        Assert.Throws<ArgumentNullException>(() => engine.GetTradeQuote(null!));

        Reload(engine);
        string before = WorldProjection(engine);
        var stale = Bind("preload", quotes[0]);
        AssertRejected(Apply(engine, stale), CommandReasonCodes.StaleQuote, stale, StationId, 1);
        Assert.Equal(before, WorldProjection(engine));

        var fresh = Quote(engine, TradeCommandTypes.Buy, Ice, 1);
        Assert.DoesNotContain(fresh.QuoteId, quotes.Select(q => q.QuoteId));
        AssertExecuted(Apply(engine, Bind("postload", fresh)), fresh);
    }
}
