using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.QuotedTradeExecutionTests;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// EP-0001-US-0015-TK-0003 — market revision lifecycle: one increment per effective market transaction
/// (trade, one hourly pass, an event change), none for rejections and no-ops; saved for profile markets
/// only (D-U2: a profile-less station keeps an internal revision that is never published or saved).
/// Uses the bounded-market fixture of <see cref="QuotedTradeExecutionTests"/>.
/// </summary>
public class MarketRevisionTests
{
    private const string LegacyShipId = "SPC-0001";
    private const string LegacyStationId = "STATION-01";
    private const string LegacyCargoModuleId = "MOD-CARGO-01";
    private const string LegacyIce = "item.ice";

    /// <summary>A second station with the same bounded profile as the docked one.</summary>
    private static ScenarioFile WithSecondProfileStation(ScenarioFile save) => save with
    {
        GameState = save.GameState with
        {
            SpaceObjects = save.GameState.SpaceObjects.Append(
                save.GameState.SpaceObjects.Single(o => o.ObjectId == StationId) with
                {
                    ObjectId = OtherStationId,
                    Name = "Second Market",
                }).ToArray(),
        },
    };

    private static ScenarioFile WithPlainStation(ScenarioFile save, string objectId) => save with
    {
        GameState = save.GameState with
        {
            SpaceObjects = save.GameState.SpaceObjects.Append(
                save.GameState.SpaceObjects.Single(o => o.ObjectId == StationId) with
                {
                    ObjectId = objectId,
                    Name = "Plain Station",
                    MarketProfileId = null,
                    Credits = 100_000,
                    Inventory = [new(Ice, 500)],
                }).ToArray(),
        },
    };

    private static ScenarioFile WithObject(ScenarioFile save, string objectId, Func<SpaceObjectData, SpaceObjectData> update) => save with
    {
        GameState = save.GameState with
        {
            SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId == objectId ? update(o) : o).ToArray(),
        },
    };

    private static ScenarioFile RoundTrip(ScenarioFile save) =>
        ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true);

    private static SimulationEngine LoadInto(ScenarioFile save)
    {
        var engine = new SimulationEngine(Registry, [], new SimulationClock(SimulationSpeed.Speed0, () => 0));
        engine.LoadScenario(save, isSave: true);
        return engine;
    }

    private static long LegacyRevision(SimulationEngine engine) =>
        engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == LegacyStationId).MarketRevision;

    private static PlayerCommand UnquotedBuy(string commandId, long quantity) =>
        new(commandId, 1, ShipId, CargoModuleId, TradeCommandTypes.Buy, ItemTypeId: Ice, Quantity: quantity);

    // --- AC-05: initial values and publication ------------------------------------------

    [Fact]
    public void Profile_market_starts_at_one_and_legacy_market_projects_null()
    {
        using (var engine = CreateMarketEngine())
        {
            Assert.Equal(1, Revision(engine));
            Assert.Equal(1, engine.CaptureSnapshot().DockedStationTrade!.MarketRevision);
            Assert.Equal(1, engine.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectId == StationId).MarketRevision);
            Assert.All(engine.CaptureSaveState().GameState.SpaceObjects.Where(o => o.ObjectId != StationId),
                o => Assert.Null(o.MarketRevision));
        }

        // A profile-less station publishes and saves no revision, but keeps an internal one that every
        // committed trade advances (D-U2).
        using var legacy = TradeCommandTests.CreateEngine();
        Assert.Equal(1, LegacyRevision(legacy));
        var before = legacy.CaptureSnapshot().DockedStationTrade!;
        Assert.Equal(LegacyStationId, before.StationObjectId);
        Assert.Null(before.MarketRevision);

        legacy.ReceiveCommand(new PlayerCommand("legacy-buy", 1, LegacyShipId, LegacyCargoModuleId,
            TradeCommandTypes.Buy, ItemTypeId: LegacyIce, Quantity: 5));
        var snapshot = legacy.CaptureSnapshot();
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(snapshot.CommandResults).Status);
        Assert.Equal(2, LegacyRevision(legacy));
        Assert.Null(snapshot.DockedStationTrade!.MarketRevision);
        Assert.All(legacy.CaptureSaveState().GameState.SpaceObjects, o => Assert.Null(o.MarketRevision));
    }

    // --- AC-04: what advances the revision ----------------------------------------------

    [Fact]
    public void Successful_trade_increments_once_but_rejected_trade_and_noop_do_not()
    {
        using var engine = CreateMarketEngine(adjust: save => WithCargo(save, Ice, 20));
        Assert.Equal(1, Revision(engine));

        // One legacy trade moves stock, Credits and budget together: exactly one increment.
        Assert.Equal(CommandResultStatus.Executed, Apply(engine, UnquotedBuy("legacy-ok", 5)).Status);
        Assert.Equal(2, Revision(engine));

        // Rejections change nothing.
        Assert.Equal(CommandResultStatus.Rejected, Apply(engine, UnquotedBuy("legacy-too-many", 10_000)).Status);
        Assert.Equal(2, Revision(engine));

        // A quoted trade: one increment, and the receipt names it.
        var quote = Quote(engine, TradeCommandTypes.Sell, Ice, 3);
        AssertEnabled(quote, 3);
        Assert.Equal(2, quote.MarketRevision);
        var executed = Apply(engine, Bind("quoted-ok", quote));
        Assert.Equal(CommandResultStatus.Executed, executed.Status);
        Assert.Equal(3, executed.TradeReceipt!.ResultMarketRevision);
        Assert.Equal(3, Revision(engine));

        // Replaying the consumed quote under a new CommandId is a zero-effect rejection.
        var replay = Apply(engine, Bind("quoted-replay", quote));
        Assert.Equal(CommandResultStatus.Rejected, replay.Status);
        Assert.Equal(3, Revision(engine));

        // No-ops: repeated snapshots at the same time and a duplicate CommandId.
        engine.CaptureSnapshot();
        engine.CaptureSnapshot();
        engine.ReceiveCommand(UnquotedBuy("legacy-ok", 5));
        engine.CaptureSnapshot();
        Assert.Equal(3, Revision(engine));
        Assert.Equal(3, engine.CaptureSnapshot().DockedStationTrade!.MarketRevision);
    }

    [Fact]
    public void Hourly_multi_item_stock_and_budget_change_increments_once_per_station()
    {
        using var engine = CreateMarketEngine(adjust: save => WithPlainStation(WithSecondProfileStation(save), "SPC-0998"));
        Assert.Equal(1, Revision(engine));
        Assert.Equal(1, Revision(engine, OtherStationId));
        Assert.Equal(1, Revision(engine, "SPC-0998"));
        long iceBefore = Stock(engine, Ice);
        long steelBefore = Stock(engine, Steel);
        long budgetBefore = Station(engine).MarketBudgetCredits!.Value;

        // One hour: consumption, the profile batch (several rows) and the budget grant — one increment.
        engine.CaptureSnapshotForTests(GameCalendar.HourMs);
        Assert.NotEqual(iceBefore, Stock(engine, Ice));
        Assert.NotEqual(steelBefore, Stock(engine, Steel));
        Assert.NotEqual(budgetBefore, Station(engine).MarketBudgetCredits);
        Assert.Equal(2, Revision(engine));
        Assert.Equal(2, Revision(engine, OtherStationId));
        // Nothing changes at a profile-less station without production.
        Assert.Equal(1, Revision(engine, "SPC-0998"));

        // A single jump over several hours still counts each boundary once.
        engine.CaptureSnapshotForTests(11 * GameCalendar.HourMs);
        Assert.Equal(12, Revision(engine));
        Assert.Equal(12, Revision(engine, OtherStationId));

        // Hour 12 also carries the meal boundary: still one increment.
        engine.CaptureSnapshotForTests(12 * GameCalendar.HourMs);
        Assert.Equal(13, Revision(engine));
        Assert.Equal(13, engine.CaptureSnapshotForTests(12 * GameCalendar.HourMs).DockedStationTrade!.MarketRevision);
        Assert.Equal(1, Revision(engine, "SPC-0998"));
    }

    [Fact]
    public void Production_without_effect_does_not_increment_revision()
    {
        // Steel and water gone: no consumption and a blocked batch; the budget already sits at its cap.
        using var engine = CreateMarketEngine(
            [new(Ice, IceTarget), new(Water, 0), new(Steel, 0)],
            save => WithStationCredits(save, 2 * MarketInitialCredits));
        Assert.Equal(2 * MarketInitialCredits, Station(engine).MarketBudgetCredits);
        var quote = Quote(engine, TradeCommandTypes.Buy, Ice, 1);
        AssertEnabled(quote, 1);

        engine.CaptureSnapshotForTests(3 * GameCalendar.HourMs);
        Assert.Equal(IceTarget, Stock(engine, Ice));
        Assert.Equal(1, Revision(engine));
        Assert.True(engine.IsQuoteIssuedForTests(quote.QuoteId));

        // An empty interval changes nothing either.
        engine.CaptureSnapshotForTests(3 * GameCalendar.HourMs);
        Assert.Equal(1, Revision(engine));
        Assert.Equal(CommandResultStatus.Executed,
            ApplyAt(engine, Bind("still-fresh", quote), 3 * GameCalendar.HourMs).Status);
    }

    [Fact]
    public void Prepared_revision_overflow_leaves_world_unchanged()
    {
        using var engine = CreateMarketEngine(adjust: save => WithCargo(save, Ice, 20));
        var atMaximum = WithObject(engine.CaptureSaveState(), StationId, o => o with { MarketRevision = long.MaxValue });
        engine.LoadScenario(RoundTrip(atMaximum), isSave: true);
        Assert.Equal(long.MaxValue, Revision(engine));

        // A legacy trade cannot advance the revision: zero-effect value_overflow.
        string before = WorldProjection(engine);
        var legacy = Apply(engine, UnquotedBuy("legacy-overflow", 5));
        Assert.Equal(CommandResultStatus.Rejected, legacy.Status);
        Assert.Equal("value_overflow", legacy.ReasonCode);
        Assert.Equal(before, WorldProjection(engine));

        // The event seam fails before any assignment as well.
        Assert.Throws<OverflowException>(() => engine.CommitEventMarketChangeForTests(StationId));
        Assert.Equal(before, WorldProjection(engine));

        // An hourly change keeps the saturated revision but still invalidates the station's quotes.
        var quote = Quote(engine, TradeCommandTypes.Buy, Ice, 1);
        AssertEnabled(quote, 1);
        Assert.True(engine.IsQuoteIssuedForTests(quote.QuoteId));
        long stockBefore = Stock(engine, Ice);
        engine.CaptureSnapshotForTests(GameCalendar.HourMs);
        Assert.NotEqual(stockBefore, Stock(engine, Ice));
        Assert.Equal(long.MaxValue, Revision(engine));
        Assert.False(engine.IsQuoteIssuedForTests(quote.QuoteId));
    }

    // --- AC-05: save/load ------------------------------------------------------------------

    [Fact]
    public void Revision_round_trips_save_load_but_runtime_invalidation_state_does_not()
    {
        using var engine = CreateMarketEngine(adjust: save => WithPlainStation(save, "SPC-0998"));
        Assert.Equal(CommandResultStatus.Executed, Apply(engine, UnquotedBuy("legacy", 5)).Status);
        engine.CaptureSnapshotForTests(GameCalendar.HourMs);
        // One legacy trade (no receipt) and one hourly pass: the journal knows neither.
        Assert.Equal(3, Revision(engine));
        var quote = Quote(engine, TradeCommandTypes.Buy, Ice, 1);
        AssertEnabled(quote, 1);

        var save = engine.CaptureSaveStateForTests(GameCalendar.HourMs, SimulationSpeed.Speed0);
        string json = ScenarioLoader.Serialize(save);
        Assert.Equal(3, save.GameState.SpaceObjects.Single(o => o.ObjectId == StationId).MarketRevision);
        Assert.Null(save.GameState.SpaceObjects.Single(o => o.ObjectId == "SPC-0998").MarketRevision);
        Assert.Contains("\"marketRevision\": 3", json);
        Assert.DoesNotContain(quote.QuoteId, json);

        using var restored = LoadInto(ScenarioLoader.LoadFromJson(json, true));
        Assert.Equal(3, Revision(restored));
        Assert.Equal(1, Revision(restored, "SPC-0998"));
        Assert.Equal(3, restored.CaptureSnapshotForTests(GameCalendar.HourMs).DockedStationTrade!.MarketRevision);

        // The quote cache is session state: the pre-save quote is unknown after the load.
        Assert.False(restored.IsQuoteIssuedForTests(quote.QuoteId));
        var stale = ApplyAt(restored, Bind("preload", quote), GameCalendar.HourMs);
        Assert.Equal(CommandReasonCodes.StaleQuote, stale.ReasonCode);
        Assert.Equal(3, Revision(restored));

        // A second save of the restored world carries the same revision.
        Assert.Equal(3, restored.CaptureSaveStateForTests(GameCalendar.HourMs, SimulationSpeed.Speed0)
            .GameState.SpaceObjects.Single(o => o.ObjectId == StationId).MarketRevision);
    }

    [Fact]
    public void Legacy_profile_save_missing_revision_migrates_to_one_and_invalid_values_are_rejected()
    {
        using var engine = CreateMarketEngine(adjust: save => WithPlainStation(WithCargo(save, Ice, 20), "SPC-0998"));
        Assert.Equal(CommandResultStatus.Executed, Apply(engine, UnquotedBuy("legacy", 5)).Status);
        Assert.Equal(2, Revision(engine));
        var save = engine.CaptureSaveState();
        var missing = WithObject(save, StationId, o => o with { MarketRevision = null });

        // No field and no receipt: the profile market starts over at 1.
        using (var migrated = LoadInto(RoundTrip(missing)))
            Assert.Equal(1, Revision(migrated));

        // No field but a trade receipt: max(1, newest receipt).
        var quote = Quote(engine, TradeCommandTypes.Sell, Ice, 2);
        var executed = Apply(engine, Bind("quoted", quote));
        Assert.Equal(3, executed.TradeReceipt!.ResultMarketRevision);
        var withReceipt = WithObject(engine.CaptureSaveState(), StationId, o => o with { MarketRevision = null });
        using (var migrated = LoadInto(RoundTrip(withReceipt)))
            Assert.Equal(3, Revision(migrated));

        // A saved value below the newest receipt never rewinds the revision: max(saved, receipts).
        var behind = WithObject(engine.CaptureSaveState(), StationId, o => o with { MarketRevision = 1 });
        using (var migrated = LoadInto(RoundTrip(behind)))
            Assert.Equal(3, Revision(migrated));

        var invalid = new (string Name, ScenarioFile Save)[]
        {
            ("zero profile revision", WithObject(save, StationId, o => o with { MarketRevision = 0 })),
            ("negative profile revision", WithObject(save, StationId, o => o with { MarketRevision = -4 })),
            ("revision on profile-less station", WithObject(save, "SPC-0998", o => o with { MarketRevision = 1 })),
            ("revision on ship", WithObject(save, ShipId, o => o with { MarketRevision = 1 })),
        };
        string worldBefore = WorldProjection(engine);
        long revisionBefore = Revision(engine);
        foreach (var (name, bad) in invalid)
        {
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(bad, isSave: true));
            Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(bad), true));
            Assert.True(worldBefore == WorldProjection(engine), name);
            Assert.Equal(revisionBefore, Revision(engine));
        }
    }

    // --- AC-04 / AC-07: event seam and profile-less quoting ------------------------------

    [Fact]
    public void Event_change_seam_commits_one_revision_without_implementing_event_lifecycle()
    {
        using var engine = CreateMarketEngine(adjust: save => WithPlainStation(save, "SPC-0998"));
        var quote = Quote(engine, TradeCommandTypes.Buy, Ice, 1);
        AssertEnabled(quote, 1);
        long stockBefore = Stock(engine, Ice);
        long playerBefore = engine.PlayerCredits;

        engine.CommitEventMarketChangeForTests(StationId);

        Assert.Equal(2, Revision(engine));
        Assert.Equal(1, Revision(engine, "SPC-0998"));
        Assert.Equal(stockBefore, Stock(engine, Ice));
        Assert.Equal(playerBefore, engine.PlayerCredits);
        Assert.False(engine.IsQuoteIssuedForTests(quote.QuoteId));
        var stale = Apply(engine, Bind("after-event", quote));
        Assert.Equal(CommandReasonCodes.StaleQuote, stale.ReasonCode);
        Assert.Equal(2, Revision(engine));

        // A fresh quote binds the new revision and executes.
        var fresh = Quote(engine, TradeCommandTypes.Buy, Ice, 1);
        Assert.Equal(2, fresh.MarketRevision);
        Assert.Equal(CommandResultStatus.Executed, Apply(engine, Bind("fresh", fresh)).Status);
        Assert.Equal(3, Revision(engine));

        // The profile-less station accepts the seam too (internal revision only).
        engine.CommitEventMarketChangeForTests("SPC-0998");
        Assert.Equal(2, Revision(engine, "SPC-0998"));
        Assert.Equal(3, Revision(engine));
    }

    [Fact]
    public void Port_fee_and_dialogue_credit_changes_do_not_increment_revision()
    {
        // 1. Port fee: due two minutes after the start, away from any hour boundary.
        const long start = GameCalendar.DayMs + 29 * 60_000;
        using (var engine = CreateMarketEngine(adjust: save => save with
        {
            GameState = save.GameState with
            {
                GameTimeMs = start,
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
        }))
        {
            engine.CaptureSnapshotForTests(start);
            var quote = Quote(engine, TradeCommandTypes.Buy, Ice, 1);
            AssertEnabled(quote, 1);
            long revision = Revision(engine);
            long stationCredits = Station(engine).Credits;
            long budget = Station(engine).MarketBudgetCredits!.Value;
            long player = engine.PlayerCredits;
            long stock = Stock(engine, Ice);

            long now = start + 2 * 60_000;
            engine.CaptureSnapshotForTests(now);
            Assert.Equal(player - 100, engine.PlayerCredits);
            Assert.Equal(stationCredits + 100, Station(engine).Credits);

            // Only Credits moved: no market transaction, no revision, no commit-hook invalidation.
            Assert.Equal(budget, Station(engine).MarketBudgetCredits);
            Assert.Equal(stock, Stock(engine, Ice));
            Assert.Equal(revision, Revision(engine));
            Assert.Equal(revision, engine.CaptureSnapshotForTests(now).DockedStationTrade!.MarketRevision);
            Assert.True(engine.IsQuoteIssuedForTests(quote.QuoteId));

            // The quote is still refused: its QuoteContext (player and station Credits) no longer matches.
            string before = WorldProjection(engine);
            var stale = ApplyAt(engine, Bind("after-fee", quote), now);
            Assert.Equal(CommandReasonCodes.StaleQuote, stale.ReasonCode);
            Assert.Equal(revision, stale.TradeReceipt!.ResultMarketRevision);
            Assert.Equal(before, WorldProjection(engine));
        }

        // 2. Dialogue docking fee paid to the profile station: again only Credits move.
        using (var engine = CreateMarketEngine(adjust: save => save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects
                    .Select(o => o.ObjectId == StationId ? o with { PortFeeCreditsPerDay = 10 } : o).ToArray(),
            },
        }))
        {
            var quote = Quote(engine, TradeCommandTypes.Buy, Ice, 1);
            AssertEnabled(quote, 1);
            long revision = Revision(engine);
            long budget = Station(engine).MarketBudgetCredits!.Value;
            long stock = Stock(engine, Ice);

            Assert.Equal(CommandResultStatus.Executed, Apply(engine,
                new PlayerCommand("undock", 1, ShipId, BridgeModuleId, NavigationComputerCommandTypes.Undock)).Status);
            Assert.Equal(CommandResultStatus.Executed, Apply(engine,
                new PlayerCommand("dock", 2, ShipId, BridgeModuleId, NavigationComputerCommandTypes.Dock,
                    TargetObjectId: StationId)).Status);
            long stationCredits = Station(engine).Credits;
            long player = engine.PlayerCredits;
            Assert.NotNull(engine.CaptureSnapshot().ActiveDialogue);

            var finished = DialogueTests.PayAndFinish(engine);
            Assert.Null(finished.ActiveDialogue);
            Assert.True(engine.PlayerCredits < player);
            Assert.Equal(stationCredits + (player - engine.PlayerCredits), Station(engine).Credits);
            Assert.Equal(StationId, engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == ShipId).DockedStationObjectId);

            Assert.Equal(budget, Station(engine).MarketBudgetCredits);
            Assert.Equal(stock, Stock(engine, Ice));
            Assert.Equal(revision, Revision(engine));
            Assert.Equal(revision, engine.CaptureSnapshot().DockedStationTrade!.MarketRevision);
            // No commit hook ran; the pre-undock quote is only stale through its QuoteContext (player Credits).
            Assert.True(engine.IsQuoteIssuedForTests(quote.QuoteId));
            Assert.Equal(CommandReasonCodes.StaleQuote, Apply(engine, Bind("after-dialogue", quote)).ReasonCode);
            Assert.Equal(revision, Revision(engine));
        }
    }

    [Fact]
    public void NoProfile_quoted_trade_works_with_constant_curve_and_null_published_revision()
    {
        using var engine = TradeCommandTests.CreateEngine(playerCredits: 50_000);
        var quote = engine.GetTradeQuote(new TradeQuoteRequest(
            "req-plain", LegacyShipId, LegacyCargoModuleId, TradeCommandTypes.Buy, LegacyIce, 10));
        Assert.Null(quote.DisabledReason);
        Assert.Equal(LegacyStationId, quote.StationObjectId);
        Assert.Equal(1, quote.MarketRevision);
        var step = Assert.Single(quote.Curve);
        Assert.Equal(10, step.Quantity);
        Assert.Equal(10 * step.UnitPriceCredits, quote.TotalCredits);

        engine.ReceiveCommand(new PlayerCommand("plain-quoted", 1, LegacyShipId, LegacyCargoModuleId,
            TradeCommandTypes.Buy, ItemTypeId: LegacyIce, Quantity: 10, QuoteId: quote.QuoteId,
            MarketRevision: quote.MarketRevision));
        var snapshot = engine.CaptureSnapshot();
        var result = Assert.Single(snapshot.CommandResults);
        Assert.Equal(CommandResultStatus.Executed, result.Status);
        Assert.Equal(1, result.TradeReceipt!.QuotedMarketRevision);
        Assert.Equal(2, result.TradeReceipt.ResultMarketRevision);
        Assert.Equal(quote.TotalCredits, result.TradeReceipt.TotalCredits);
        Assert.Equal(50_000 - quote.TotalCredits, engine.PlayerCredits);
        Assert.Equal(2, LegacyRevision(engine));
        Assert.Null(snapshot.DockedStationTrade!.MarketRevision);
        Assert.All(engine.CaptureSaveState().GameState.SpaceObjects, o => Assert.Null(o.MarketRevision));
    }
}
