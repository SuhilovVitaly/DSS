using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.QuotedTradeExecutionTests;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// EP-0001-US-0015-TK-0004 — the engine quote issuer: sequential curve through TradeQuoteCalculator, exact
/// maximum over every authoritative limit, RequestId idempotency, bounded session cache and a validator that
/// never mutates. Real engine, real catalog and the bounded-market fixture of
/// <see cref="QuotedTradeExecutionTests"/>; no hand-made totals.
/// </summary>
public class TradeQuoteIssuanceTests
{
    private static TradeQuoteSnapshot Request(
        SimulationEngine engine, string requestId, string commandType, string itemTypeId, long quantity, string? moduleId = null) =>
        engine.GetTradeQuote(new TradeQuoteRequest(
            requestId, ShipId,
            moduleId ?? (commandType == TradeCommandTypes.Refuel ? EngineModuleId : CargoModuleId),
            commandType, itemTypeId, quantity));

    /// <summary>The docked station without a market profile: constant curve, no hourly market.</summary>
    private static ScenarioFile AsPlainStation(ScenarioFile save) => save with
    {
        GameState = save.GameState with
        {
            SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId != StationId ? o : o with
            {
                MarketProfileId = null,
                Credits = 100_000,
                Inventory = [new(Ice, 500)],
            }).ToArray(),
        },
    };

    private static ScenarioFile WithStationEvents(ScenarioFile save, params StationEventData[] events) => save with
    {
        GameState = save.GameState with
        {
            SpaceObjects = save.GameState.SpaceObjects
                .Select(o => o.ObjectId != StationId ? o : o with { Events = events }).ToArray(),
        },
    };

    // --- AC-03 / AC-07 ------------------------------------------------------------------

    [Fact]
    public void Quote_binds_station_ship_module_direction_item_quantity_and_revision()
    {
        using var engine = CreateMarketEngine(IceStock(40));
        long published = engine.CaptureSnapshot().DockedStationTrade!.MarketRevision!.Value;

        var quote = Request(engine, "req-bind", TradeCommandTypes.Buy, Ice, 12);
        AssertEnabled(quote, 12);
        Assert.Equal("req-bind", quote.RequestId);
        Assert.Equal(StationId, quote.StationObjectId);
        Assert.Equal(ShipId, quote.ObjectId);
        Assert.Equal(CargoModuleId, quote.ModuleId);
        Assert.Equal(TradeCommandTypes.Buy, quote.CommandType);
        Assert.Equal(Ice, quote.ItemTypeId);
        Assert.Equal(12, quote.RequestedQuantity);
        Assert.Equal(published, quote.MarketRevision);
        Assert.True(quote.MaximumQuantity >= 12);
        Assert.Empty(quote.LimitReasons);
        Assert.False(quote.PriceReasons.IsDefaultOrEmpty);

        // The curve is sequential: at a shortage each bought unit costs at least as much as the previous one.
        Assert.True(quote.Curve.Length > 1);
        for (int i = 1; i < quote.Curve.Length; i++)
            Assert.True(quote.Curve[i].UnitPriceCredits > quote.Curve[i - 1].UnitPriceCredits);

        // The binding executes exactly as quoted and nothing else.
        var receipt = AssertExecuted(Apply(engine, Bind("bound", quote)), quote);
        Assert.Equal(quote.TotalCredits, receipt.TotalCredits);
        Assert.Equal(published + 1, engine.CaptureSnapshot().DockedStationTrade!.MarketRevision);
    }

    [Fact]
    public void Buy_refuel_and_sell_maximum_use_full_sequential_prefix_and_all_authoritative_limits()
    {
        // Player money: the largest sequential prefix, never budget / first unit price.
        using (var engine = CreateMarketEngine(adjust: save => WithPlayerCredits(save, 500)))
        {
            long max = Request(engine, "m-1", TradeCommandTypes.Buy, Ice, 1).MaximumQuantity;
            var atMax = Request(engine, "m-max", TradeCommandTypes.Buy, Ice, max);
            AssertEnabled(atMax, max);
            Assert.True(atMax.TotalCredits <= 500);
            var above = Request(engine, "m-above", TradeCommandTypes.Buy, Ice, max + 1);
            AssertDisabled(above, CommandReasonCodes.InsufficientPlayerCredits);
            Assert.Equal(max, above.MaximumQuantity);
            Assert.Equal(max, Request(engine, "m-many", TradeCommandTypes.Buy, Ice, 10_000).MaximumQuantity);
            long firstUnit = Request(engine, "m-first", TradeCommandTypes.Buy, Ice, 1).TotalCredits;
            Assert.True(max < 500 / firstUnit);
        }

        // Station stock caps Buy when money and cargo are ample.
        using (var engine = CreateMarketEngine(adjust: save => WithPlayerCredits(save, 1_000_000)))
        {
            Assert.Equal(IceTarget, Request(engine, "s-1", TradeCommandTypes.Buy, Ice, 1).MaximumQuantity);
            AssertEnabled(Request(engine, "s-all", TradeCommandTypes.Buy, Ice, IceTarget), IceTarget);
            AssertDisabled(Request(engine, "s-above", TradeCommandTypes.Buy, Ice, IceTarget + 1),
                CommandReasonCodes.InsufficientStationStock);
        }

        // Free cargo mass caps Buy (Ice is 1 kg per unit).
        using (var engine = CreateMarketEngine(adjust: save => WithCargo(WithPlayerCredits(save, 1_000_000), EnergyCells, 1)))
        {
            long freeKg = Module(engine, CargoModuleId).AvailableCapacityKg!.Value;
            using var filled = CreateMarketEngine(adjust: save =>
                WithCargo(WithPlayerCredits(save, 1_000_000), EnergyCells, 1 + freeKg - 7));
            Assert.Equal(7, Request(filled, "c-1", TradeCommandTypes.Buy, Ice, 1).MaximumQuantity);
            AssertDisabled(Request(filled, "c-8", TradeCommandTypes.Buy, Ice, 8), CommandReasonCodes.CargoCapacityExceeded);
        }

        // Refuel: constant buy-spread price, capped by money, tank room and fuel stock.
        using (var engine = CreateMarketEngine(adjust: save => WithFuel(WithPlayerCredits(save, 1000), 750)))
        {
            var one = Request(engine, "r-1", TradeCommandTypes.Refuel, Fuel, 1);
            long price = one.TotalCredits;
            Assert.Equal(Math.Min(1000 / price, 200), one.MaximumQuantity);
        }
        using (var engine = CreateMarketEngine(adjust: save => WithFuel(WithPlayerCredits(save, 1_000_000), 900)))
        {
            long room = 1000 - 900;
            Assert.Equal(room, Request(engine, "r-room", TradeCommandTypes.Refuel, Fuel, 1).MaximumQuantity);
            AssertDisabled(Request(engine, "r-over", TradeCommandTypes.Refuel, Fuel, room + 1),
                CommandReasonCodes.FuelCapacityExceeded);
        }

        // Sell: cargo, free storage and the station's budget along the falling sell curve.
        long budget;
        using (var probe = CreateMarketEngine(adjust: save => WithCargo(save, Ice, 50)))
        {
            var curve = Request(probe, "p", TradeCommandTypes.Sell, Ice, 10);
            AssertEnabled(curve, 10);
            budget = PrefixTotal(curve, 6) + 2;
        }
        using (var engine = CreateMarketEngine(adjust: save => WithStationCredits(WithCargo(save, Ice, 50), budget)))
        {
            Assert.Equal(6, Request(engine, "b-1", TradeCommandTypes.Sell, Ice, 1).MaximumQuantity);
            Assert.Equal(6, Request(engine, "b-10", TradeCommandTypes.Sell, Ice, 10).MaximumQuantity);
        }
        using (var engine = CreateMarketEngine(IceStock(2 * IceTarget - 3), save => WithCargo(save, Ice, 50)))
            Assert.Equal(3, Request(engine, "room", TradeCommandTypes.Sell, Ice, 1).MaximumQuantity);
        using (var engine = CreateMarketEngine(adjust: save => WithCargo(save, Ice, 4)))
            Assert.Equal(4, Request(engine, "cargo", TradeCommandTypes.Sell, Ice, 1).MaximumQuantity);
    }

    [Theory]
    [InlineData(TradeCommandTypes.Buy, Ice)]
    [InlineData(TradeCommandTypes.Refuel, Fuel)]
    public void Profile_station_credit_headroom_limits_quotes_and_exact_limit_executes(string commandType, string itemTypeId)
    {
        long headroom;
        using (var probe = CreateMarketEngine(adjust: save => WithFuel(save, 750)))
        {
            var quote = Request(probe, "probe", commandType, itemTypeId, 2);
            AssertEnabled(quote, 2);
            headroom = quote.TotalCredits;
        }

        using var engine = CreateMarketEngine(adjust: save =>
            WithStationCredits(WithFuel(save, 750), long.MaxValue - headroom));
        string before = WorldProjection(engine);
        var atLimit = Request(engine, "at-limit", commandType, itemTypeId, 2);
        AssertEnabled(atLimit, 2);
        Assert.Equal(2, atLimit.MaximumQuantity);
        var aboveLimit = Request(engine, "above-limit", commandType, itemTypeId, 3);
        AssertDisabled(aboveLimit, "value_overflow");
        Assert.Equal(2, aboveLimit.MaximumQuantity);
        Assert.Equal(before, WorldProjection(engine));

        AssertExecuted(Apply(engine, Bind("execute-at-limit", atLimit)), atLimit);
        Assert.Equal(long.MaxValue, Station(engine).Credits);
        string full = WorldProjection(engine);
        var atMaximum = Request(engine, "full", commandType, itemTypeId, 1);
        AssertDisabled(atMaximum, "value_overflow");
        Assert.Equal(0, atMaximum.MaximumQuantity);
        Assert.Equal(full, WorldProjection(engine));
    }

    [Fact]
    public void Sell_partial_is_only_budget_or_station_capacity_and_never_leaks_amounts()
    {
        long budget;
        using (var probe = CreateMarketEngine(adjust: save => WithCargo(save, Ice, 50)))
            budget = PrefixTotal(Request(probe, "p", TradeCommandTypes.Sell, Ice, 10), 3) + 1;

        using (var engine = CreateMarketEngine(adjust: save => WithStationCredits(WithCargo(save, Ice, 50), budget)))
        {
            var partial = Request(engine, "partial-budget", TradeCommandTypes.Sell, Ice, 10);
            AssertEnabled(partial, 3);
            Assert.Null(partial.DisabledReason);
            Assert.Equal(10, partial.RequestedQuantity);
            Assert.Equal(new[] { CommandReasonCodes.StationBudgetExceeded }, partial.LimitReasons.ToArray());
            Assert.True(partial.TotalCredits <= budget);

            string json = JsonSerializer.Serialize(partial);
            // Only the limit code names the budget; no field carries its amount or the station's Credits.
            Assert.DoesNotContain("BudgetCredits", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"Credits\"", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(":" + budget + ",", json);
            Assert.DoesNotContain(":" + Station(engine).Credits + ",", json);

            // Missing cargo is never partial: the whole request is refused.
            AssertDisabled(Request(engine, "cargo-short", TradeCommandTypes.Sell, Ice, 51), CommandReasonCodes.InsufficientCargoQuantity);
            // Buy above its maximum is refused as a whole, never partial.
            var buy = Request(engine, "buy-over", TradeCommandTypes.Buy, Ice, IceTarget + 1);
            AssertDisabled(buy, CommandReasonCodes.InsufficientStationStock);
            Assert.Empty(buy.LimitReasons);
        }

        using (var engine = CreateMarketEngine(IceStock(2 * IceTarget - 2), save => WithCargo(save, Ice, 50)))
        {
            var partial = Request(engine, "partial-room", TradeCommandTypes.Sell, Ice, 10);
            AssertEnabled(partial, 2);
            Assert.Equal(new[] { CommandReasonCodes.StationCapacityExceeded }, partial.LimitReasons.ToArray());
        }

        using (var engine = CreateMarketEngine(IceStock(2 * IceTarget), save => WithCargo(save, Ice, 50)))
            AssertDisabled(Request(engine, "room-zero", TradeCommandTypes.Sell, Ice, 10), CommandReasonCodes.StationCapacityExceeded);

        // A payout the player's balance cannot hold is not a partial fill: the quote is disabled whole.
        using (var engine = CreateMarketEngine(adjust: save => WithCargo(WithPlayerCredits(save, long.MaxValue - 5), Ice, 10)))
            AssertDisabled(Request(engine, "player-overflow", TradeCommandTypes.Sell, Ice, 10), "value_overflow");
    }

    // --- AC-06 ----------------------------------------------------------------------------

    [Fact]
    public void Same_request_is_idempotent_but_request_id_collision_is_disabled()
    {
        using var engine = CreateMarketEngine(adjust: save => WithCargo(save, Ice, 20));
        var first = Request(engine, "req-1", TradeCommandTypes.Buy, Ice, 5);
        AssertEnabled(first, 5);
        Assert.Same(first, Request(engine, "req-1", TradeCommandTypes.Buy, Ice, 5));

        // Same RequestId, different binding: refused, not cached, and the original quote survives.
        foreach (var conflict in new[]
        {
            Request(engine, "req-1", TradeCommandTypes.Buy, Ice, 6),
            Request(engine, "req-1", TradeCommandTypes.Sell, Ice, 5),
            Request(engine, "req-1", TradeCommandTypes.Buy, Water, 5),
        })
        {
            AssertDisabled(conflict, "request_id_conflict");
            Assert.Equal("req-1", conflict.RequestId);
        }
        Assert.True(engine.IsQuoteIssuedForTests(first.QuoteId));
        Assert.Same(first, Request(engine, "req-1", TradeCommandTypes.Buy, Ice, 5));

        // Disabled quotes are never cached: the same RequestId may then be used for a valid request.
        AssertDisabled(Request(engine, "req-2", TradeCommandTypes.Buy, Ice, 0), CommandReasonCodes.InvalidQuantity);
        AssertEnabled(Request(engine, "req-2", TradeCommandTypes.Buy, Ice, 2), 2);

        // Once the market moves, the same request is answered with a new quote for the new revision.
        engine.CommitEventMarketChangeForTests(StationId);
        var renewed = Request(engine, "req-1", TradeCommandTypes.Buy, Ice, 5);
        AssertEnabled(renewed, 5);
        Assert.NotEqual(first.QuoteId, renewed.QuoteId);
        Assert.Equal(first.MarketRevision + 1, renewed.MarketRevision);
        Assert.Same(renewed, Request(engine, "req-1", TradeCommandTypes.Buy, Ice, 5));
        AssertExecuted(Apply(engine, Bind("renewed", renewed)), renewed);
    }

    // --- AC-04 ----------------------------------------------------------------------------

    [Fact]
    public void Market_revision_change_evicts_quote_and_validator_reports_stale_without_mutation()
    {
        using var engine = CreateMarketEngine(adjust: save => WithCargo(save, Ice, 20));
        var buy = Request(engine, "buy", TradeCommandTypes.Buy, Ice, 5);
        var sell = Request(engine, "sell", TradeCommandTypes.Sell, Ice, 5);
        AssertEnabled(buy, 5);
        AssertEnabled(sell, 5);

        engine.CommitEventMarketChangeForTests(StationId);
        Assert.False(engine.IsQuoteIssuedForTests(buy.QuoteId));
        Assert.False(engine.IsQuoteIssuedForTests(sell.QuoteId));

        string before = WorldProjection(engine);
        foreach (var (id, quote) in new[] { ("stale-buy", buy), ("stale-sell", sell) })
        {
            var command = Bind(id, quote);
            AssertRejected(Apply(engine, command), CommandReasonCodes.StaleQuote, command, StationId, 2);
            Assert.Equal(before, WorldProjection(engine));
        }

        // An hourly market pass invalidates the same way.
        var hourly = Request(engine, "hourly", TradeCommandTypes.Buy, Ice, 5);
        engine.CaptureSnapshotForTests(GameCalendar.HourMs);
        Assert.False(engine.IsQuoteIssuedForTests(hourly.QuoteId));
        Assert.Equal(3, Revision(engine));
    }

    [Theory]
    [InlineData("player")]
    [InlineData("module")]
    [InlineData("docking")]
    public void Player_module_or_docking_context_change_makes_quote_stale(string variant)
    {
        const long portFeeStart = GameCalendar.DayMs + 29 * 60_000;
        using var engine = CreateMarketEngine(adjust: save =>
        {
            save = AsPlainStation(save);
            if (variant != "player") return save;
            // A port fee falls due two minutes later, well away from any hour or meal boundary.
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
        long now = variant == "player" ? portFeeStart : 12 * GameCalendar.HourMs - 1;
        engine.CaptureSnapshotForTests(now);
        var quote = Request(engine, "ctx", TradeCommandTypes.Buy, Ice, 5);
        AssertEnabled(quote, 5);
        long revision = Revision(engine);

        switch (variant)
        {
            case "player":
                now += 2 * 60_000;
                long credits = engine.PlayerCredits;
                engine.CaptureSnapshotForTests(now);
                Assert.Equal(credits - 100, engine.PlayerCredits);
                break;
            case "module":
                // The noon meal takes rations out of the addressed cargo module: its free mass changes.
                long freeBefore = Module(engine, CargoModuleId).AvailableCapacityKg!.Value;
                now += 1;
                engine.CaptureSnapshotForTests(now);
                Assert.NotEqual(freeBefore, Module(engine, CargoModuleId).AvailableCapacityKg);
                break;
            case "docking":
                Assert.Equal(CommandResultStatus.Executed, ApplyAt(engine,
                    new PlayerCommand("undock", 1, ShipId, BridgeModuleId, NavigationComputerCommandTypes.Undock), now).Status);
                break;
        }

        // None of these is a market change, so the revision stays; the quote is stale all the same.
        Assert.Equal(revision, Revision(engine));
        string before = WorldProjection(engine);
        var command = Bind("ctx-" + variant, quote);
        var result = ApplyAt(engine, command, now);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(variant == "docking" ? CommandReasonCodes.NotDocked : CommandReasonCodes.StaleQuote, result.ReasonCode);
        Assert.Equal(before, WorldProjection(engine));
    }

    [Theory]
    [InlineData("module", CommandReasonCodes.InvalidQuote)]
    [InlineData("item", CommandReasonCodes.InvalidQuote)]
    [InlineData("direction", CommandReasonCodes.InvalidQuote)]
    [InlineData("quantity", CommandReasonCodes.InvalidQuote)]
    [InlineData("revision", CommandReasonCodes.InvalidQuote)]
    [InlineData("station", CommandReasonCodes.InvalidQuote)]
    public void Cross_station_module_item_direction_quantity_or_revision_binding_is_invalid(string variant, string expected)
    {
        using var engine = CreateMarketEngine(adjust: save => WithCargo(WithOtherStation(WithSecondContainer(save)), Ice, 20));
        var quote = Request(engine, "cross", TradeCommandTypes.Sell, Ice, 5);
        AssertEnabled(quote, 5);
        var command = Bind("cross-" + variant, quote);
        string? station = StationId;
        switch (variant)
        {
            case "module": command = command with { ModuleId = SecondCargoModuleId }; break;
            case "item": command = command with { ItemTypeId = Water }; break;
            case "direction": command = command with { CommandType = TradeCommandTypes.Buy }; break;
            case "quantity": command = command with { Quantity = 6 }; break;
            case "revision": command = command with { MarketRevision = quote.MarketRevision + 1 }; break;
            case "station":
                Assert.Equal(CommandResultStatus.Executed, Apply(engine,
                    new PlayerCommand("undock", 1, ShipId, BridgeModuleId, NavigationComputerCommandTypes.Undock)).Status);
                Assert.Equal(CommandResultStatus.Executed, Apply(engine,
                    new PlayerCommand("dock", 2, ShipId, BridgeModuleId, NavigationComputerCommandTypes.Dock,
                        TargetObjectId: OtherStationId)).Status);
                DialogueTests.PayAndFinish(engine);
                station = OtherStationId;
                break;
        }

        string before = WorldProjection(engine);
        var result = Apply(engine, command);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
        Assert.Equal(expected, result.ReasonCode);
        Assert.Equal(station, result.TradeReceipt!.StationObjectId);
        Assert.Equal(before, WorldProjection(engine));
        if (variant != "station")
            Assert.True(engine.IsQuoteIssuedForTests(quote.QuoteId));
    }

    // --- AC-05 ----------------------------------------------------------------------------

    [Fact]
    public void Evicted_1025th_quote_and_preload_quote_are_stale_and_ids_are_not_reused()
    {
        using var engine = CreateMarketEngine();
        var quotes = Enumerable.Range(0, 1025)
            .Select(i => Request(engine, $"fill-{i}", TradeCommandTypes.Buy, Ice, 1)).ToArray();
        Assert.Equal(quotes.Length, quotes.Select(q => q.QuoteId).Distinct(StringComparer.Ordinal).Count());
        Assert.False(engine.IsQuoteIssuedForTests(quotes[0].QuoteId));
        Assert.True(engine.IsQuoteIssuedForTests(quotes[1].QuoteId));

        string before = WorldProjection(engine);
        var evicted = Bind("evicted", quotes[0]);
        AssertRejected(Apply(engine, evicted), CommandReasonCodes.StaleQuote, evicted, StationId, 1);
        Assert.Equal(before, WorldProjection(engine));
        // The evicted RequestId binding is forgotten too: the same request now yields a new quote.
        var reissued = Request(engine, "fill-0", TradeCommandTypes.Buy, Ice, 1);
        AssertEnabled(reissued, 1);
        Assert.NotEqual(quotes[0].QuoteId, reissued.QuoteId);

        var live = quotes[^1];
        Reload(engine);
        var preload = Bind("preload", live);
        string reloaded = WorldProjection(engine);
        AssertRejected(Apply(engine, preload), CommandReasonCodes.StaleQuote, preload, StationId, 1);
        Assert.Equal(reloaded, WorldProjection(engine));

        var fresh = Request(engine, $"fill-{quotes.Length - 1}", TradeCommandTypes.Buy, Ice, 1);
        AssertEnabled(fresh, 1);
        Assert.DoesNotContain(fresh.QuoteId, quotes.Select(q => q.QuoteId).Append(reissued.QuoteId));
        AssertExecuted(Apply(engine, Bind("postload", fresh)), fresh);
    }

    // --- AC-02 / AC-03: fuel and safety -----------------------------------------------------

    [Fact]
    public void Fuel_refuel_uses_buy_curve_and_forbidden_fuel_routes_are_disabled()
    {
        using var engine = CreateMarketEngine(adjust: save => WithFuel(save, 0));
        var refuel = Request(engine, "refuel", TradeCommandTypes.Refuel, Fuel, 100);
        AssertEnabled(refuel, 100);
        // Fuel has no stock target: the stock factor stays neutral and the curve is one constant run.
        var step = Assert.Single(refuel.Curve);
        var fuel = Registry.ItemTypes.GetDefinition(Registry.ItemTypes.GetIndex(Fuel));
        int size = StationSizeFactors.Resolve(StationSize.Medium, fuel.Category);
        long expected = (long)Math.Round(fuel.BasePriceCredits!.Value * (size / 1000m) * 1.15m, MidpointRounding.AwayFromZero);
        Assert.Equal(expected, step.UnitPriceCredits);
        Assert.Contains(new TradePriceReason("buy_spread", 1150), refuel.PriceReasons);
        Assert.Contains(new TradePriceReason("stock_normal", 1000), refuel.PriceReasons);
        AssertExecuted(Apply(engine, Bind("refuel", refuel)), refuel);

        AssertDisabled(Request(engine, "buy-fuel", TradeCommandTypes.Buy, Fuel, 10), CommandReasonCodes.FuelTradeForbidden);
        AssertDisabled(Request(engine, "sell-fuel", TradeCommandTypes.Sell, Fuel, 10), CommandReasonCodes.FuelTradeForbidden);
        AssertDisabled(Request(engine, "refuel-ice", TradeCommandTypes.Refuel, Ice, 10), CommandReasonCodes.FuelTradeForbidden);
    }

    [Fact]
    public void Invalid_and_overflow_requests_return_zero_effect_disabled_quote()
    {
        using var engine = CreateMarketEngine(adjust: save => WithCargo(WithPlayerCredits(save, long.MaxValue - 5), Ice, 10));
        string before = WorldProjection(engine);
        var cases = new (TradeQuoteRequest Request, string Reason)[]
        {
            (new("i-1", "SPC-0404", CargoModuleId, TradeCommandTypes.Buy, Ice, 1), CommandReasonCodes.UnknownObject),
            (new("i-2", ShipId, "MOD-NONE", TradeCommandTypes.Buy, Ice, 1), CommandReasonCodes.UnknownModule),
            (new("i-3", ShipId, CargoModuleId, "trade.steal", Ice, 1), CommandReasonCodes.UnknownCommandType),
            (new("i-4", ShipId, CargoModuleId, TradeCommandTypes.Buy, Ice, 0), CommandReasonCodes.InvalidQuantity),
            (new("i-5", ShipId, CargoModuleId, TradeCommandTypes.Buy, Ice, -3), CommandReasonCodes.InvalidQuantity),
            (new("i-6", ShipId, CargoModuleId, TradeCommandTypes.Buy, "item.none", 1), CommandReasonCodes.UnknownItemType),
            (new("i-7", ShipId, CargoModuleId, TradeCommandTypes.Buy, Ice, long.MaxValue), CommandReasonCodes.InsufficientStationStock),
            (new("i-8", ShipId, CargoModuleId, TradeCommandTypes.Sell, Ice, 10), "value_overflow"),
            (new("i-9", ShipId, CargoModuleId, TradeCommandTypes.Sell, Ice, long.MaxValue), CommandReasonCodes.InsufficientCargoQuantity),
            (new("i-10", null!, null!, null!, null!, 1), CommandReasonCodes.UnknownObject),
        };
        foreach (var (request, reason) in cases)
        {
            var quote = engine.GetTradeQuote(request);
            Assert.True(reason == quote.DisabledReason, request.RequestId + ": " + quote.DisabledReason);
            AssertDisabled(quote, reason);
            Assert.Equal(request.RequestId, quote.RequestId);
            Assert.True(quote.PriceReasons.IsDefaultOrEmpty);
        }

        Assert.Equal(before, WorldProjection(engine));
        Assert.Throws<ArgumentNullException>(() => engine.GetTradeQuote(null!));
    }

    [Fact]
    public void Price_reasons_explain_profile_stock_event_spread_and_clamp_in_deterministic_order()
    {
        using var engine = CreateMarketEngine(IceStock(20), save => WithCargo(WithStationEvents(save,
            new StationEventData("event.b", "B", null, 0, null, [new StationEventPriceFactorData(null, Ice, 1200)]),
            new StationEventData("event.a", "A", null, 0, null, [new StationEventPriceFactorData("Resource", null, 2000)]),
            new StationEventData("event.c", "C", null, 0, null, [new StationEventPriceFactorData(null, Water, 5000)])),
            Ice, 5));
        var ice = Registry.ItemTypes.GetDefinition(Registry.ItemTypes.GetIndex(Ice));
        int size = StationSizeFactors.Resolve(StationSize.Medium, ice.Category);

        var buy = Request(engine, "reasons-buy", TradeCommandTypes.Buy, Ice, 3);
        AssertEnabled(buy, 3);
        TradePriceReason[] expectedBuy =
        [
            new("base_price", 1000, Ice),
            new("station_profile", size, MarketProfileId),
            new("event", 2000, "event.a"),
            new("event", 1200, "event.b"),
            // 1 + 0.70 × (1 − 20/108) = 1.5704 at the first unit.
            new("stock_shortage", 1570),
            new("buy_spread", 1150),
            new("price_ceiling", 3000),
        ];
        Assert.Equal(expectedBuy, buy.PriceReasons.ToArray());
        // Clamped at 3.00 × base: one constant run.
        Assert.Equal(new TradePriceStep(3, 3 * ice.BasePriceCredits!.Value), Assert.Single(buy.Curve));

        var sell = Request(engine, "reasons-sell", TradeCommandTypes.Sell, Ice, 2);
        AssertEnabled(sell, 2);
        Assert.Equal(expectedBuy[..5].Append(new TradePriceReason("sell_spread", 850)).ToArray(),
            sell.PriceReasons.Where(r => r.Code != "price_ceiling").ToArray());
    }
}
