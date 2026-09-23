using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public class EconomyTimeContinuityTests
{
    private static readonly GameDataRegistry IntervalRegistry = CreateIntervalRegistry();

    [Fact]
    public void Regular_snapshots_and_long_explicit_interval_have_identical_economy_and_event_identities()
    {
        long realMs = 0;
        var clock = new SimulationClock(SimulationSpeed.Speed0, () => realMs);
        using var regular = CreateIntervalEngine(clock, rations: 4);
        using var explicitInterval = CreateIntervalEngine(rations: 4);
        Assert.Equal(4, RationScheduleTests.Food(regular.CaptureSnapshot()));
        clock.SetSpeed(SimulationSpeed.Speed1);
        var regularEvents = new List<ShipEvent>();
        AuthoritativeSnapshot actual = regular.CaptureSnapshot();
        // Include snapshots between boundaries as well as coincident meal/fee/deadline times.
        for (int hour = 1; hour <= 48; hour++)
        {
            realMs = hour * GameCalendar.HourMs / 300;
            actual = regular.CaptureSnapshot(advanceClock: true);
            regularEvents.AddRange(actual.ShipEvents);
            if (hour == 12) Assert.Equal(1, RationScheduleTests.Food(actual));
        }

        var expected = explicitInterval.CaptureSnapshotForTests(2 * GameCalendar.DayMs,
            simulationTimeMs: 777);
        AssertEconomyEqual(expected, actual);
        Assert.Equal(expected.ShipEvents.ToArray(), regularEvents.ToArray());
        Assert.Equal(2 * GameCalendar.DayMs / 300, actual.SimulationTimeMs);
        Assert.Equal(777, expected.SimulationTimeMs);
        Assert.Equal(1800, expected.PlayerCredits);
        Assert.Equal(3, expected.MissingRations);
        Assert.Equal(0, RationScheduleTests.Food(expected));
        Assert.True(Assert.Single(expected.ActiveContracts).DeadlineMissed);
        Assert.Equal(8, ProducedFood(explicitInterval));
        Assert.Equal(ProducedFood(explicitInterval), ProducedFood(regular));
        Assert.Equal(ProductionDue(explicitInterval), ProductionDue(regular));
    }

    [Fact]
    public void Coincident_boundary_orders_meal_fee_and_deadline_once_and_completes_production()
    {
        using var engine = CreateIntervalEngine();
        var atBoundary = engine.CaptureSnapshotForTests(GameCalendar.DayMs, simulationTimeMs: 123);
        var atMidnight = atBoundary.ShipEvents.Where(e => e.GameTimeMs == GameCalendar.DayMs).ToArray();
        Assert.Equal(new[] { "rations_shortage", "port_fee_renewed", "contract_deadline_missed" },
            atMidnight.Select(e => e.EventType).ToArray());
        Assert.Equal("insufficient_rations", atMidnight[0].ReasonCode);
        Assert.Equal(4, ProducedFood(engine));
        Assert.Equal(123, atBoundary.SimulationTimeMs);

        var repeated = engine.CaptureSnapshotForTests(GameCalendar.DayMs, simulationTimeMs: 123);
        AssertEconomyEqual(atBoundary, repeated);
        Assert.Empty(repeated.ShipEvents);
        Assert.Equal(4, ProducedFood(engine));
        var justAfter = engine.CaptureSnapshotForTests(GameCalendar.DayMs + 1, simulationTimeMs: 124);
        AssertEconomyEqual(atBoundary with { GameTimeMs = GameCalendar.DayMs + 1 }, justAfter);
        Assert.Empty(justAfter.ShipEvents);
        Assert.Equal(4, ProducedFood(engine));
        Assert.Equal(GameCalendar.DayMs + 12 * GameCalendar.HourMs, ProductionDue(engine));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Save_load_at_boundary_does_not_replay_effects_and_preserves_explicit_or_legacy_motion(bool legacyMotion)
    {
        var clock = new SimulationClock(SimulationSpeed.Speed0, () => 0);
        using var original = CreateIntervalEngine(clock);
        var boundary = original.CaptureSnapshotForTests(GameCalendar.DayMs, simulationTimeMs: 321);
        clock.Reset(GameCalendar.DayMs, SimulationSpeed.Speed0, 321);
        var save = original.CaptureSaveState();
        if (legacyMotion) save = save with { SaveFormatVersion = 5, GameState = save.GameState with { SimulationTimeMs = null } };
        using var loaded = CreateIntervalEngine();
        loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true));
        var restored = loaded.CaptureSnapshot();
        AssertEconomyEqual(boundary, restored);
        Assert.Equal(legacyMotion ? GameCalendar.DayMs : 321, restored.MotionTimeMs);
        Assert.Empty(restored.ShipEvents);
        Assert.Equal(4, ProducedFood(loaded));
        Assert.Empty(loaded.CaptureSnapshotForTests(GameCalendar.DayMs,
            simulationTimeMs: restored.MotionTimeMs).ShipEvents);

        var expected = original.CaptureSnapshotForTests(2 * GameCalendar.DayMs, simulationTimeMs: 1000);
        var continued = loaded.CaptureSnapshotForTests(2 * GameCalendar.DayMs,
            simulationTimeMs: restored.MotionTimeMs + 679);
        AssertEconomyEqual(expected, continued);
        // Event counters restart on load; compare semantic identities across this boundary.
        Assert.Equal(expected.ShipEvents.Select(e => (e.ObjectId, e.ModuleId, e.EventType, e.ReasonCode, e.GameTimeMs)),
            continued.ShipEvents.Select(e => (e.ObjectId, e.ModuleId, e.EventType, e.ReasonCode, e.GameTimeMs)));
        Assert.Equal(8, ProducedFood(loaded));
        Assert.Equal(ProductionDue(original), ProductionDue(loaded));
    }

    [Fact]
    public void Production_reserves_inputs_at_interval_start_and_completes_only_at_its_calendar_boundary()
    {
        using var engine = CreateIntervalEngine();
        engine.CaptureSnapshotForTests(1, simulationTimeMs: 100);
        Assert.Equal(9, StationQuantity(engine, "item.test-input"));
        Assert.Equal(0, ProducedFood(engine));
        Assert.Equal(12 * GameCalendar.HourMs, ProductionDue(engine));

        engine.CaptureSnapshotForTests(12 * GameCalendar.HourMs - 1, simulationTimeMs: 200);
        Assert.Equal(9, StationQuantity(engine, "item.test-input"));
        Assert.Equal(0, ProducedFood(engine));
        engine.CaptureSnapshotForTests(12 * GameCalendar.HourMs, simulationTimeMs: 201);
        Assert.Equal(2, ProducedFood(engine));
        Assert.Equal(9, StationQuantity(engine, "item.test-input"));
        Assert.Null(ProductionDue(engine));
    }

    [Fact]
    public void Calendar_boundary_is_independent_of_explicit_motion_target()
    {
        using var engine = CreateIntervalEngine();
        var before = engine.CaptureSnapshotForTests(12 * GameCalendar.HourMs - 1,
            simulationTimeMs: 10 * GameCalendar.DayMs);
        Assert.Equal(0, before.MissingRations);
        Assert.Empty(before.ShipEvents);
        Assert.Equal(0, ProducedFood(engine));
        var meal = engine.CaptureSnapshotForTests(12 * GameCalendar.HourMs,
            simulationTimeMs: 10 * GameCalendar.DayMs);
        Assert.Equal(3, meal.MissingRations);
        Assert.Equal(12 * GameCalendar.HourMs, Assert.Single(meal.ShipEvents).GameTimeMs);
        Assert.Equal(10 * GameCalendar.DayMs, meal.SimulationTimeMs);
        Assert.Equal(2, ProducedFood(engine));
    }

    private static GameDataRegistry CreateIntervalRegistry()
    {
        var recipe = new RecipeDefinition("recipe.test-food", "Food", [new("item.test-input", 1)],
            [new("item.food-rations", 2)], 12 * GameCalendar.HourMs);
        return GameDataRegistry.Create([],
            [new("module.test-cargo", "Cargo", 1, 1, 100, 0, [], CargoCapacityKg: 100)],
            [new("item.test-input", "Input", 1, 10), new("item.food-rations", "Food", 1, 10, TradeUnit: TradeUnit.Ration)],
            commandDefinitions: [], factoryTypes: [new("factory.test-food", "Food", recipe)]);
    }

    private static SimulationEngine CreateIntervalEngine(SimulationClock? clock = null, long rations = 0)
    {
        using var template = RationScheduleTests.CreateEngine(0, passengers: 2, rations: 0);
        var save = template.CaptureSaveState();
        save = save with
        {
            GameState = save.GameState with
            {
                SimulationTimeMs = 0,
                MasterSeed = 17,
                CatalogCompatibility = IntervalRegistry.CatalogCompatibility,
                TradingMap = null,
                DialogueState = null,
                SpaceObjects = save.GameState.SpaceObjects
                    .Where(o => o.ObjectId == save.GameState.PlayerShipObjectId || o.ObjectId == "SPC-0002")
                    .Select(o => o with
                    {
                        Modules = o.ObjectType == "PlayerShip"
                        ? [new("cargo", "module.test-cargo", [new(4, 2)], 100, "On", "Ready", null,
                        rations > 0 ? [new("item.food-rations", rations)] : [])] : [],
                        Credits = o.ObjectType == "Station" ? 10_000 : null,
                        PriceCoefficient = o.ObjectType == "Station" ? 1000 : null,
                        MarketProfileId = null,
                        MarketProfileFingerprint = null,
                        MarketBudgetCredits = null,
                        MarketRevision = null,
                        StationCrew = [],
                        Inventory = o.ObjectType == "Station" ? [new("item.test-input", 10), new("item.food-rations", 0)] : null,
                        ProducingModules = o.ObjectType == "Station" ? [new("factory.test-food")] : null
                    }).ToArray(),
                EconomyTime = save.GameState.EconomyTime! with
                {
                    ActiveContracts = [new("contract.test", GameCalendar.DayMs, 750, "SPC-0002", ["P0"])]
                }
            }
        };
        var engine = new SimulationEngine(IntervalRegistry, [], clock ?? new SimulationClock(SimulationSpeed.Speed0, () => 0));
        engine.LoadScenario(save);
        return engine;
    }

    private static long ProducedFood(SimulationEngine engine) => StationQuantity(engine, "item.food-rations");

    private static long StationQuantity(SimulationEngine engine, string itemTypeId) => engine.RuntimeObjects
        .Where(o => o.ObjectType == "Station").SelectMany(o => o.Inventory)
        .Where(i => i.ItemTypeIndex == IntervalRegistry.ItemTypes.GetIndex(itemTypeId)).Sum(i => i.StockQuantity);

    private static long? ProductionDue(SimulationEngine engine) => engine.RuntimeObjects
        .Where(o => o.ObjectType == "Station").SelectMany(o => o.ProducingModules)
        .Single().NextProductionDueGameTimeMs;

    private static void AssertEconomyEqual(AuthoritativeSnapshot expected, AuthoritativeSnapshot actual)
    {
        Assert.Equal(expected.GameTimeMs, actual.GameTimeMs);
        Assert.Equal(expected.PlayerCredits, actual.PlayerCredits);
        Assert.Equal(expected.PortFees, actual.PortFees);
        Assert.Equal(expected.MissingRations, actual.MissingRations);
        Assert.Equal(RationScheduleTests.Food(expected), RationScheduleTests.Food(actual));
        Assert.Equal(expected.ActiveContracts.Select(c => (c.ContractId, c.DeadlineGameTimeMs, c.ExpectedPayout,
            c.DestinationStationObjectId, c.DeadlineMissed)), actual.ActiveContracts.Select(c => (c.ContractId,
            c.DeadlineGameTimeMs, c.ExpectedPayout, c.DestinationStationObjectId, c.DeadlineMissed)));
        Assert.Equal(expected.ActiveContracts.SelectMany(c => c.PassengerIds), actual.ActiveContracts.SelectMany(c => c.PassengerIds));
    }

    [Fact]
    public void Save_after_travel_restores_time_passengers_meals_and_receipts_without_replaying()
    {
        using var original = RationScheduleTests.CreateEngine(11 * GameCalendar.HourMs, passengers: 2);
        var command = new StationTravelCommand("travel-before-save", StationDistrict.Market);
        var before = original.TravelStation(command).Snapshot;
        var serialized = ScenarioLoader.Serialize(original.CaptureSaveState());
        using var loaded = RationScheduleTests.CreateEngine();
        loaded.LoadScenario(ScenarioLoader.LoadFromJson(serialized, true));
        var after = loaded.CaptureSnapshot();
        Assert.Equal(before.GameTimeMs, after.GameTimeMs);
        Assert.Equal(before.CurrentStationDistrict, after.CurrentStationDistrict);
        Assert.Equal(197, RationScheduleTests.Food(after));
        Assert.Equal(2, loaded.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectType == "PlayerShip").Passengers!.Count);
        Assert.Equal(before.GameTimeMs, loaded.TravelStation(command).Snapshot.GameTimeMs);
        var end = loaded.CaptureSnapshotForTests(GameCalendar.DayMs);
        Assert.Equal(194, RationScheduleTests.Food(end));
        Assert.Equal(1900, end.PlayerCredits);
        Assert.Equal(original.CaptureSnapshotForTests(GameCalendar.DayMs).PortFees, end.PortFees);
    }

    [Fact]
    public void Saved_eta_contract_deadline_and_expected_payment_are_absolute()
    {
        using var engine = StationTravelTests.DockedEngine(10 * GameCalendar.HourMs);
        var save = engine.CaptureSaveState();
        var state = save.GameState;
        var economy = state.EconomyTime! with
        {
            RouteArrivalGameTimeMs = 20 * GameCalendar.HourMs,
            ActiveContracts = [new("contract", 11 * GameCalendar.HourMs, 750, "STATION-01", ["passenger"])]
        };
        save = save with { GameState = state with { EconomyTime = economy } };
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true));
        var result = engine.TravelStation(new("deadline", StationDistrict.Market));
        Assert.True(Assert.Single(result.Snapshot.ActiveContracts).DeadlineMissed);
        var roundTrip = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true);
        Assert.Equal(economy.RouteArrivalGameTimeMs, roundTrip.GameState.EconomyTime!.RouteArrivalGameTimeMs);
        Assert.Equal(750, Assert.Single(roundTrip.GameState.EconomyTime.ActiveContracts!).ExpectedPayout);
        engine.LoadScenario(roundTrip);
        Assert.Empty(engine.CaptureSnapshot().ShipEvents);
    }

    [Fact]
    public async Task Closed_application_time_is_not_added_when_loop_starts()
    {
        using var engine = StationTravelTests.DockedEngine(10 * GameCalendar.HourMs);
        engine.TravelStation(new("before-loop", StationDistrict.Market));
        await Task.Delay(30);
        using var cancel = new CancellationTokenSource();
        await using var snapshots = engine.RunAsync(cancel.Token).GetAsyncEnumerator();
        Assert.True(await snapshots.MoveNextAsync());
        Assert.Equal(11 * GameCalendar.HourMs, snapshots.Current.GameTimeMs);
        cancel.Cancel();
    }

    [Fact]
    public void Incompatible_economic_rules_fail_without_modifying_save_file()
    {
        using var engine = StationTravelTests.DockedEngine();
        var save = engine.CaptureSaveState();
        save = save with { GameState = save.GameState with { EconomyTime = save.GameState.EconomyTime! with { RulesVersion = 999 } } };
        string path = Path.Combine(Path.GetTempPath(), $"dss-incompatible-{Guid.NewGuid():N}.json");
        string text = ScenarioLoader.Serialize(save);
        File.WriteAllText(path, text);
        try
        {
            var error = Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromFile(path, true));
            Assert.Contains("economic rules", error.Message);
            Assert.Equal(text, File.ReadAllText(path));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Legacy_docked_save_without_payment_anchor_is_not_silently_rebased()
    {
        using var engine = StationTravelTests.DockedEngine();
        var save = engine.CaptureSaveState();
        save = save with
        {
            SaveFormatVersion = 4,
            GameState = save.GameState with
            {
                EconomyTime = null,
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o with
                {
                    FirstPortFeeGameTimeMs = null,
                    NextPortFeeDueGameTimeMs = null
                }).ToArray()
            }
        };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(save));
    }

    [Fact]
    public void Current_save_without_payment_anchor_is_rejected_without_rebasing_or_mutating_world()
    {
        using var engine = PortFeeScheduleTests.DockAt(0);
        var save = engine.CaptureSaveStateForTests(8 * GameCalendar.HourMs, SimulationSpeed.Speed0);
        engine.LoadScenario(save);
        string before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        var incomplete = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.IsDocked ? o with
                {
                    FirstPortFeeGameTimeMs = null,
                    NextPortFeeDueGameTimeMs = null
                } : o).ToArray()
            }
        };

        var error = Assert.Throws<ScenarioException>(() => engine.LoadScenario(incomplete));
        Assert.Contains("first port payment time is missing", error.Message);
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(incomplete), true));
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Loaded_payment_schedule_continues_from_anchor_after_processed_renewals(int paidDays)
    {
        long first = 8 * GameCalendar.HourMs + 30 * 60_000;
        using var engine = PortFeeScheduleTests.DockAt(first);
        var save = engine.CaptureSaveStateForTests(first + paidDays * GameCalendar.DayMs,
            SimulationSpeed.Speed0);
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true));
        Assert.Equal(900 - paidDays * 100, engine.CaptureSnapshot().PlayerCredits);
        Assert.Equal(first + (paidDays + 1) * GameCalendar.DayMs,
            engine.CaptureSnapshot().PortFees!.NextPortFeeDueGameTimeMs);

        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o with
                {
                    NextPortFeeDueGameTimeMs = null
                }).ToArray()
            }
        });
        var after = engine.CaptureSnapshotForTests(first + (paidDays + 1) * GameCalendar.DayMs);
        Assert.Equal(800 - paidDays * 100, after.PlayerCredits);
        Assert.Equal(first, after.PortFees!.FirstPortFeeGameTimeMs);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(8, 3)]
    [InlineData(24, 3)]
    public void Saved_payment_schedule_cannot_skip_the_nearest_renewal(int elapsedHours, int nextDay)
    {
        long first = 8 * GameCalendar.HourMs + 30 * 60_000;
        using var engine = PortFeeScheduleTests.DockAt(first);
        var save = engine.CaptureSaveStateForTests(first + elapsedHours * GameCalendar.HourMs,
            SimulationSpeed.Speed0);
        save = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.IsDocked ? o with
                {
                    NextPortFeeDueGameTimeMs = first + nextDay * GameCalendar.DayMs
                } : o).ToArray()
            }
        };

        Assert.Throws<ScenarioException>(() => engine.LoadScenario(save));
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true));
    }

    [Fact]
    public void Missing_next_payment_is_derived_from_saved_first_payment()
    {
        using var engine = PortFeeScheduleTests.DockAt(8 * GameCalendar.HourMs + 30 * 60_000);
        var save = engine.CaptureSaveStateForTests(16 * GameCalendar.HourMs, SimulationSpeed.Speed0);
        long next = save.GameState.SpaceObjects.Single(o => o.IsDocked).NextPortFeeDueGameTimeMs!.Value;
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects
            .Select(o => o with { NextPortFeeDueGameTimeMs = null }).ToArray()
            }
        });
        Assert.Equal(next, engine.CaptureSnapshot().PortFees!.NextPortFeeDueGameTimeMs);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(3_600_000)]
    public void Invalid_port_schedule_is_rejected(long next)
    {
        using var engine = StationTravelTests.DockedEngine();
        var save = engine.CaptureSaveState();
        save = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects
            .Select(o => o.IsDocked ? o with { NextPortFeeDueGameTimeMs = next } : o).ToArray()
            }
        };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(save));
    }

    // ---------------------------------------------------------------------------------------
    // US-0002 TK-0003 — bounded station market: hourly flow, stock caps, pending output, budget.
    // Every fixture below opts one station into an economy profile; a station without one keeps
    // the pre-US-0002 behaviour, which Existing_legacy_production_and_motion_are_unchanged pins.
    // ---------------------------------------------------------------------------------------

    private const string MarketProfileId = "market.bounded";
    private const long IceTarget = 108;
    private const long WaterTarget = 72;
    private const long SteelTarget = 72;
    private const long MarketInitialCredits = 9600;
    private const long MarketMaxBudget = 2 * MarketInitialCredits;

    private static ImmutableDictionary<StationSize, int> MarketSizeFactors =>
        new Dictionary<StationSize, int>
        {
            [StationSize.Outpost] = 500,
            [StationSize.Medium] = 1000,
            [StationSize.Large] = 1500,
            [StationSize.Huge] = 2000,
        }.ToImmutableDictionary();

    /// <summary>
    /// Profile-driven market: one hourly batch (4 water -&gt; 18 ice) plus 2 steel of demand that
    /// exists regardless of production. Targets equal the starting stock, so MaxStock is exactly
    /// twice it and the Medium size factor keeps every number identical to its base value.
    /// </summary>
    private static StationMarketProfileDefinition BoundedProfile(
        StationMarketProductionSource source = StationMarketProductionSource.Profile,
        int divisorPerDay = 24,
        long initialCredits = MarketInitialCredits,
        ImmutableArray<StationMarketStockDefinition>? hourlyInputs = null,
        ImmutableArray<StationMarketStockDefinition>? hourlyOutputs = null,
        ImmutableArray<StationMarketStockDefinition>? hourlyConsumption = null,
        ImmutableArray<string>? supply = null,
        string typeId = MarketProfileId) =>
        new(
            TypeId: typeId,
            DisplayName: typeId,
            SupplyItemTypeIds: supply ?? ["item.ice"],
            DemandItemTypeIds: ["item.water", "item.steel"],
            InitialInventory:
            [
                new("item.ice", IceTarget), new("item.water", WaterTarget), new("item.steel", SteelTarget),
            ],
            InitialCredits: initialCredits,
            RefuelStockKg: 200,
            SizeFactors: MarketSizeFactors,
            Economy: new StationMarketEconomyDefinition(
                ProductionSource: source,
                HourlyInputs: hourlyInputs ?? (source == StationMarketProductionSource.Profile
                    ? [new("item.water", 4)] : []),
                HourlyOutputs: hourlyOutputs ?? (source == StationMarketProductionSource.Profile
                    ? [new("item.ice", 18)] : []),
                HourlyConsumption: hourlyConsumption ?? [new("item.steel", 2)],
                StockTargets:
                [
                    new("item.ice", IceTarget), new("item.water", WaterTarget), new("item.steel", SteelTarget),
                ],
                ShortageThresholdPermille: 500,
                SurplusThresholdPermille: 1500,
                BudgetRegenerationDivisorPerDay: divisorPerDay));

    /// <summary>Pure-consumption Transit shape: nothing is produced, so both hourly batch lists stay empty.</summary>
    private static StationMarketProfileDefinition TransitProfile() => new(
        TypeId: "market.transit-bounded",
        DisplayName: "market.transit-bounded",
        SupplyItemTypeIds: [],
        DemandItemTypeIds: ["item.water", "item.steel"],
        InitialInventory: [new("item.water", WaterTarget), new("item.steel", SteelTarget)],
        InitialCredits: MarketInitialCredits,
        RefuelStockKg: 200,
        SizeFactors: MarketSizeFactors,
        Economy: new StationMarketEconomyDefinition(
            ProductionSource: StationMarketProductionSource.Profile,
            HourlyInputs: [],
            HourlyOutputs: [],
            HourlyConsumption: [new("item.water", 4), new("item.steel", 2)],
            StockTargets: [new("item.water", WaterTarget), new("item.steel", SteelTarget)],
            ShortageThresholdPermille: 500,
            SurplusThresholdPermille: 1500,
            BudgetRegenerationDivisorPerDay: 24));

    private static GameDataRegistry RealRegistry()
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
        return EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
    }

    /// <summary>
    /// The shipped registry with one extra economy profile (and optionally one extra factory type)
    /// spliced in — real items, module types and trade commands, so Buy/Sell behave exactly as
    /// they do in the game while the market under test is fully controlled by the fixture.
    /// </summary>
    private static GameDataRegistry MarketRegistry(
        StationMarketProfileDefinition profile,
        FactoryTypeDefinition? extraFactory = null)
    {
        var source = RealRegistry();
        var factories = Enumerable.Range(0, source.FactoryTypes.Count).Select(source.FactoryTypes.GetDefinition);
        return GameDataRegistry.Create(
            Enumerable.Range(0, source.ModuleCategories.Count).Select(source.ModuleCategories.GetDefinition),
            Enumerable.Range(0, source.ModuleTypes.Count).Select(source.ModuleTypes.GetDefinition),
            Enumerable.Range(0, source.ItemTypes.Count).Select(source.ItemTypes.GetDefinition),
            Enumerable.Range(0, source.CommandDefinitions.Count).Select(source.CommandDefinitions.GetDefinition),
            extraFactory is null ? factories : factories.Append(extraFactory),
            Enumerable.Range(0, source.Recipes.Count).Select(source.Recipes.GetDefinition),
            legacyCatalogFingerprint: source.LegacyCatalogFingerprint,
            stationMarketProfiles: Enumerable.Range(0, source.StationMarketProfiles.Count)
                .Select(source.StationMarketProfiles.GetDefinition).Append(profile));
    }

    /// <summary>
    /// A docked New Game world whose single station runs <paramref name="profile"/>. The save
    /// format stays 0 on purpose: that is the New Game path, where the budget is derived rather
    /// than restored. Port fees are switched off so the only thing moving station Credits is the
    /// market itself.
    /// </summary>
    private static (SimulationEngine Engine, GameDataRegistry Registry) CreateMarketEngine(
        StationMarketProfileDefinition? profile = null,
        FactoryTypeDefinition? extraFactory = null,
        IReadOnlyList<StationInventoryItemData>? stock = null,
        IReadOnlyList<StationProducingModuleData>? producingModules = null,
        SimulationClock? clock = null,
        Func<ScenarioFile, ScenarioFile>? adjust = null)
    {
        profile ??= BoundedProfile();
        var registry = MarketRegistry(profile, extraFactory);
        var save = MarketTemplate(profile, stock, producingModules);
        if (adjust is not null) save = adjust(save);
        var engine = new SimulationEngine(registry, [], clock ?? new SimulationClock(SimulationSpeed.Speed0, () => 0));
        engine.LoadScenario(save);
        return (engine, registry);
    }

    private static ScenarioFile MarketTemplate(
        StationMarketProfileDefinition profile,
        IReadOnlyList<StationInventoryItemData>? stock = null,
        IReadOnlyList<StationProducingModuleData>? producingModules = null)
    {
        using var template = RationScheduleTests.CreateEngine(0, passengers: 0, rations: 200);
        var save = template.CaptureSaveState();
        var gs = save.GameState;
        string stationId = gs.SpaceObjects.Single(o => o.ObjectId == gs.PlayerShipObjectId).DockedStationObjectId!;
        return save with
        {
            SaveFormatVersion = 0,
            GameState = gs with
            {
                TradingMap = null,
                SpaceObjects = gs.SpaceObjects
                    .Where(o => o.ObjectId == gs.PlayerShipObjectId || o.ObjectId == stationId)
                    .Select(o => o.ObjectId != stationId ? o : o with
                    {
                        MarketProfileId = profile.TypeId,
                        MarketProfileFingerprint = null,
                        ExplicitInventoryItemTypeIds = null,
                        StationSize = nameof(StationSize.Medium),
                        Credits = null,
                        // Null lets the profile define the stock; an explicit list overrides it.
                        Inventory = stock,
                        ProducingModules = producingModules,
                        Events = null,
                        PortFeeCreditsPerDay = null,
                    }).ToArray(),
            },
        };
    }

    private static SpaceObjectRuntime MarketStation(SimulationEngine engine) =>
        engine.RuntimeObjects.Single(o => o.ObjectType == "Station" && o.MarketProfileId is not null);

    private static long MarketStock(SimulationEngine engine, GameDataRegistry registry, string itemTypeId)
    {
        int index = registry.ItemTypes.GetIndex(itemTypeId);
        var entry = MarketStation(engine).Inventory.FirstOrDefault(i => i.ItemTypeIndex == index);
        return entry?.StockQuantity ?? 0;
    }

    private static long MarketBudget(SimulationEngine engine) => MarketStation(engine).MarketBudgetCredits ?? -1;

    private static (long Ice, long Water, long Steel, long Budget) MarketState(
        SimulationEngine engine, GameDataRegistry registry) =>
        (MarketStock(engine, registry, "item.ice"), MarketStock(engine, registry, "item.water"),
            MarketStock(engine, registry, "item.steel"), MarketBudget(engine));

    private static string MarketPending(SimulationEngine engine, GameDataRegistry registry) =>
        string.Join(" | ", MarketStation(engine).ProducingModules.Select(module =>
            string.Join(",", (module.PendingOutput.IsDefault
                    ? ImmutableArray<StationInventoryItemRuntime>.Empty
                    : module.PendingOutput)
                .OrderBy(item => item.ItemTypeIndex)
                .Select(item => $"{registry.ItemTypes.GetDefinition(item.ItemTypeIndex).TypeId}={item.StockQuantity}"))));

    /// <summary>
    /// Everything AC-04 requires to be identical for the same elapsed game time: stock, budget and
    /// the pending remainder, plus the production schedule that drives the remainder.
    /// </summary>
    private static (long Ice, long Water, long Steel, long Budget, string Pending, string Due) MarketFingerprint(
        SimulationEngine engine, GameDataRegistry registry)
    {
        var (ice, water, steel, budget) = MarketState(engine, registry);
        string due = string.Join(",", MarketStation(engine).ProducingModules
            .Select(module => module.NextProductionDueGameTimeMs?.ToString() ?? "-"));
        return (ice, water, steel, budget, MarketPending(engine, registry), due);
    }

    /// <summary>
    /// Modules-driven market whose ice is both produced by a recipe and consumed hourly: output
    /// overflows into PendingOutput and then drains again as demand frees room, so the remainder
    /// actually changes from hour to hour instead of sitting still.
    /// </summary>
    private static StationMarketProfileDefinition PendingProfile() => new(
        TypeId: "market.pending-bounded",
        DisplayName: "market.pending-bounded",
        SupplyItemTypeIds: [],
        DemandItemTypeIds: ["item.ice", "item.water", "item.steel"],
        InitialInventory:
        [
            new("item.ice", IceTarget), new("item.water", WaterTarget), new("item.steel", SteelTarget),
        ],
        InitialCredits: MarketInitialCredits,
        RefuelStockKg: 200,
        SizeFactors: MarketSizeFactors,
        Economy: new StationMarketEconomyDefinition(
            ProductionSource: StationMarketProductionSource.Modules,
            HourlyInputs: [],
            HourlyOutputs: [],
            // Ice is a recipe OUTPUT, so consuming it hourly never double-spends a recipe input.
            HourlyConsumption: [new("item.ice", 5), new("item.steel", 2)],
            StockTargets:
            [
                new("item.ice", IceTarget), new("item.water", WaterTarget), new("item.steel", SteelTarget),
            ],
            ShortageThresholdPermille: 500,
            SurplusThresholdPermille: 1500,
            BudgetRegenerationDivisorPerDay: 24));

    /// <summary>A market that starts with ice at its cap, so the first finished batch has nowhere to go.</summary>
    private static (SimulationEngine Engine, GameDataRegistry Registry) CreatePendingEngine(SimulationClock? clock = null) =>
        CreateMarketEngine(
            PendingProfile(),
            extraFactory: IceFactory(),
            stock: [new("item.ice", 2 * IceTarget), new("item.water", WaterTarget), new("item.steel", SteelTarget)],
            producingModules: [new StationProducingModuleData("factory.market-test")],
            clock: clock);

    private static string ShipId(SimulationEngine engine) => engine.PlayerShipObjectId!;

    private static string ContainerModuleId(SimulationEngine engine, GameDataRegistry registry) =>
        engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == ShipId(engine)).Modules
            .First(m => registry.ModuleTypes.GetDefinition(m.ModuleTypeIndex).CargoCapacityKg is > 0).ModuleId;

    [Fact]
    public void Hour_boundary_applies_profile_batch_and_consumption_once()
    {
        var (engine, registry) = CreateMarketEngine();
        using var _ = engine;

        engine.CaptureSnapshotForTests(GameCalendar.HourMs - 1, simulationTimeMs: 1);
        Assert.Equal((IceTarget, WaterTarget, SteelTarget, MarketInitialCredits), MarketState(engine, registry));

        // One whole hour: independent demand takes 2 steel, then the batch spends 4 water for 18 ice.
        engine.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 2);
        long firstHourBudget = MarketInitialCredits + MarketMaxBudget / 24 / 24;
        Assert.Equal((126, 68, 70, firstHourBudget), MarketState(engine, registry));

        // Re-capturing the very same game time must not repeat the hour.
        engine.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 2);
        Assert.Equal((126, 68, 70, firstHourBudget), MarketState(engine, registry));

        // Still inside the same hour — nothing new.
        engine.CaptureSnapshotForTests(GameCalendar.HourMs + 1, simulationTimeMs: 3);
        Assert.Equal((126, 68, 70, firstHourBudget), MarketState(engine, registry));

        engine.CaptureSnapshotForTests(2 * GameCalendar.HourMs, simulationTimeMs: 4);
        Assert.Equal(144, MarketStock(engine, registry, "item.ice"));
        Assert.Equal(64, MarketStock(engine, registry, "item.water"));
        Assert.Equal(68, MarketStock(engine, registry, "item.steel"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Missing_input_or_output_capacity_skips_entire_profile_batch(bool missingInput)
    {
        // Either too little water to spend, or no room left for the ice that would come out.
        var stock = missingInput
            ? new StationInventoryItemData[] { new("item.ice", IceTarget), new("item.water", 3), new("item.steel", SteelTarget) }
            : [new("item.ice", 2 * IceTarget), new("item.water", WaterTarget), new("item.steel", SteelTarget)];
        var (engine, registry) = CreateMarketEngine(stock: stock);
        using var _ = engine;

        engine.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 1);

        // Nothing of the batch happened — no input was spent and no output appeared. Independent
        // consumption is not part of the batch and still applies.
        Assert.Equal(missingInput ? 3 : WaterTarget, MarketStock(engine, registry, "item.water"));
        Assert.Equal(missingInput ? IceTarget : 2 * IceTarget, MarketStock(engine, registry, "item.ice"));
        Assert.Equal(SteelTarget - 2, MarketStock(engine, registry, "item.steel"));
    }

    [Fact]
    public void Transit_consumes_only_available_stock()
    {
        var (engine, registry) = CreateMarketEngine(
            TransitProfile(),
            stock: [new("item.water", 3), new("item.steel", SteelTarget)]);
        using var _ = engine;

        engine.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 1);

        // The 4/hour demand cannot take more than the 3 that exist, and never goes negative.
        Assert.Equal(0, MarketStock(engine, registry, "item.water"));
        Assert.Equal(SteelTarget - 2, MarketStock(engine, registry, "item.steel"));

        engine.CaptureSnapshotForTests(2 * GameCalendar.HourMs, simulationTimeMs: 2);
        Assert.Equal(0, MarketStock(engine, registry, "item.water"));
    }

    private static FactoryTypeDefinition IceFactory(long outputCount = 18) => new(
        "factory.market-test", "Market test",
        new RecipeDefinition("recipe.market-test", "Market test",
            [new("item.water", 4)], [new("item.ice", outputCount)], GameCalendar.HourMs));

    [Theory]
    [InlineData(1)]
    [InlineData(GameCalendar.HourMs - 1)]
    [InlineData(GameCalendar.HourMs)]
    [InlineData(GameCalendar.HourMs + 1)]
    public void Production_market_revision_is_independent_of_snapshot_and_save_boundaries(long splitAt)
    {
        const long horizon = 3 * GameCalendar.HourMs;
        var (direct, registry) = CreateMarketEngine(
            BoundedProfile(StationMarketProductionSource.Modules),
            extraFactory: IceFactory(),
            producingModules: [new StationProducingModuleData("factory.market-test")]);
        using var _ = direct;
        using var split = new SimulationEngine(registry, [], new SimulationClock(SimulationSpeed.Speed0, () => 0));
        split.LoadScenario(direct.CaptureSaveState(), isSave: true);

        var quote = split.GetTradeQuote(new TradeQuoteRequest("before-production", ShipId(split),
            ContainerModuleId(split, registry), TradeCommandTypes.Buy, "item.ice", 1));
        Assert.Null(quote.DisabledReason);
        split.CaptureSnapshotForTests(splitAt);
        Assert.False(split.IsQuoteIssuedForTests(quote.QuoteId));
        var save = ScenarioLoader.LoadFromJson(
            ScenarioLoader.Serialize(split.CaptureSaveStateForTests(splitAt, SimulationSpeed.Speed0)), true);
        using var restored = new SimulationEngine(registry, [], new SimulationClock(SimulationSpeed.Speed0, () => 0));
        restored.LoadScenario(save, isSave: true);

        var expected = direct.CaptureSnapshotForTests(horizon);
        Assert.Equal(7, expected.DockedStationTrade!.MarketRevision); // Three starts and three hourly completions.
        foreach (var engine in new[] { split, restored })
        {
            var actual = engine.CaptureSnapshotForTests(horizon);
            Assert.Equal(MarketFingerprint(direct, registry), MarketFingerprint(engine, registry));
            Assert.Equal(expected.DockedStationTrade.MarketRevision, actual.DockedStationTrade!.MarketRevision);
            Assert.Equal(expected.DockedStationTrade.MarketRevision,
                engine.CaptureSaveStateForTests(horizon, SimulationSpeed.Speed0).GameState.SpaceObjects
                    .Single(o => o.ObjectType == "Station").MarketRevision);
        }
    }

    [Fact]
    public void Modules_source_completes_once_without_profile_double_count()
    {
        var modulesProfile = BoundedProfile(StationMarketProductionSource.Modules);
        var (engine, registry) = CreateMarketEngine(
            modulesProfile,
            extraFactory: IceFactory(),
            producingModules: [new StationProducingModuleData("factory.market-test")]);
        using var _ = engine;

        // The recipe is the only producer: one cycle spends 4 water and yields 18 ice, while the
        // profile batch stays silent for a Modules-source market.
        engine.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 1);
        Assert.Equal(IceTarget + 18, MarketStock(engine, registry, "item.ice"));
        Assert.Equal(WaterTarget - 4, MarketStock(engine, registry, "item.water"));
        Assert.Equal(SteelTarget - 2, MarketStock(engine, registry, "item.steel"));

        // Declaring a profile batch AND a live producing module is a configuration error, caught
        // before the candidate world can replace the running one.
        var conflict = Assert.Throws<ScenarioException>(() => CreateMarketEngine(
            BoundedProfile(),
            extraFactory: IceFactory(),
            producingModules: [new StationProducingModuleData("factory.market-test")]));
        Assert.Contains("productionSource Profile conflicts", conflict.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Full_recipe_output_becomes_pending_and_blocks_next_cycle()
    {
        var modulesProfile = BoundedProfile(StationMarketProductionSource.Modules);
        var (engine, registry) = CreateMarketEngine(
            modulesProfile,
            extraFactory: IceFactory(),
            // Ice already at its cap, so a finished batch has nowhere to go.
            stock: [new("item.ice", 2 * IceTarget), new("item.water", WaterTarget), new("item.steel", SteelTarget)],
            producingModules: [new StationProducingModuleData("factory.market-test")]);
        using var _ = engine;

        engine.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 1);
        Assert.Equal(2 * IceTarget, MarketStock(engine, registry, "item.ice"));
        var module = MarketStation(engine).ProducingModules.Single();
        Assert.Equal(18, Assert.Single(module.PendingOutput).StockQuantity);
        // A blocked remainder also blocks the next cycle, so inputs are not spent again.
        Assert.Null(module.NextProductionDueGameTimeMs);
        Assert.Equal(WaterTarget - 4, MarketStock(engine, registry, "item.water"));

        // Repeating the same snapshot must not duplicate the output.
        engine.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 1);
        Assert.Equal(18, Assert.Single(MarketStation(engine).ProducingModules.Single().PendingOutput).StockQuantity);

        // The player buys 5 ice, which is the only room that appears.
        engine.ReceiveCommand(new PlayerCommand("buy-ice", 1, ShipId(engine), ContainerModuleId(engine, registry),
            TradeCommandTypes.Buy, ItemTypeId: "item.ice", Quantity: 5));
        engine.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 1);
        Assert.Equal(2 * IceTarget - 5, MarketStock(engine, registry, "item.ice"));

        // The next calendar step unloads exactly what fits and keeps the rest pending.
        engine.CaptureSnapshotForTests(2 * GameCalendar.HourMs, simulationTimeMs: 2);
        Assert.Equal(2 * IceTarget, MarketStock(engine, registry, "item.ice"));
        Assert.Equal(13, Assert.Single(MarketStation(engine).ProducingModules.Single().PendingOutput).StockQuantity);
    }

    [Fact]
    public void Pending_output_and_budget_survive_save_load_at_hour_boundary()
    {
        const long horizon = 5 * GameCalendar.HourMs;

        // Saving just before, exactly on, and just after an hour boundary must all restore the
        // same world the uninterrupted run reaches — a cursor that replayed or skipped the tick
        // would only show up on one of these three. The last point is taken after the remainder
        // has already partially unloaded.
        foreach (long saveAt in new[]
        {
            GameCalendar.HourMs - 1, GameCalendar.HourMs, GameCalendar.HourMs + 1, 2 * GameCalendar.HourMs + 1,
        })
        {
            var (reference, registry) = CreatePendingEngine();
            using var __ = reference;
            reference.CaptureSnapshotForTests(saveAt, simulationTimeMs: saveAt / 300);
            var expectedAtSave = MarketFingerprint(reference, registry);

            var clock = new SimulationClock(SimulationSpeed.Speed0, () => 0);
            var (original, _2) = CreatePendingEngine(clock);
            using var ___ = original;
            original.CaptureSnapshotForTests(saveAt, simulationTimeMs: saveAt / 300);

            // CaptureSnapshotForTests advances the world but not the clock; the save must carry
            // the time the world actually reached, otherwise the reload would replay the hour.
            clock.Reset(saveAt, SimulationSpeed.Speed0, saveAt / 300);
            var save = original.CaptureSaveState();
            Assert.Equal(9, save.SaveFormatVersion);

            using var loaded = new SimulationEngine(MarketRegistry(PendingProfile(), IceFactory()));
            loaded.LoadScenario(
                ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), allowNonZeroGameTime: true), isSave: true);
            Assert.Equal(expectedAtSave, MarketFingerprint(loaded, registry));

            // Continuing from the restored world lands exactly where the uninterrupted run does.
            reference.CaptureSnapshotForTests(horizon, simulationTimeMs: horizon / 300);
            loaded.CaptureSnapshotForTests(horizon, simulationTimeMs: horizon / 300);
            Assert.Equal(MarketFingerprint(reference, registry), MarketFingerprint(loaded, registry));
        }

        // The pending remainder genuinely moves across this window, so the comparisons above are
        // not quietly comparing an always-empty value.
        var (probe, probeRegistry) = CreatePendingEngine();
        using var _ = probe;
        probe.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 1);
        string afterFirstHour = MarketPending(probe, probeRegistry);
        probe.CaptureSnapshotForTests(2 * GameCalendar.HourMs, simulationTimeMs: 2);
        Assert.NotEqual(afterFirstHour, MarketPending(probe, probeRegistry));
        Assert.Contains("item.ice=", afterFirstHour, StringComparison.Ordinal);
    }

    private static (long Ice, long Water, long Steel, long Budget) MarketStateAfter(
        SimulationEngine engine, GameDataRegistry registry, long gameTimeMs)
    {
        engine.CaptureSnapshotForTests(gameTimeMs, simulationTimeMs: gameTimeMs / 300);
        return MarketState(engine, registry);
    }

    [Fact]
    public void Normal_accelerated_chunked_and_manual_time_produce_identical_markets()
    {
        // Run the whole equivalence matrix twice: once on a profile-batch market, and once on the
        // Modules market whose PendingOutput actually grows and drains — so the pending part of
        // AC-04 is compared, not just stock and budget.
        AssertTimeAdvanceEquivalence(clock => CreateMarketEngine(clock: clock));
        AssertTimeAdvanceEquivalence(CreatePendingEngine);
    }

    private static void AssertTimeAdvanceEquivalence(
        Func<SimulationClock?, (SimulationEngine Engine, GameDataRegistry Registry)> create)
    {
        const long horizon = 6 * GameCalendar.HourMs;

        // One long explicit interval.
        var (chunkedOnce, registry) = create(null);
        using var _ = chunkedOnce;
        chunkedOnce.CaptureSnapshotForTests(horizon, simulationTimeMs: horizon / 300);
        var single = MarketFingerprint(chunkedOnce, registry);
        // The fixtures must actually exercise pending, otherwise this proves nothing about it.
        Assert.NotNull(single.Pending);

        // Many small explicit intervals over the same horizon.
        var (chunked, _2) = create(null);
        using var __ = chunked;
        for (long t = GameCalendar.HourMs / 4; t <= horizon; t += GameCalendar.HourMs / 4)
            chunked.CaptureSnapshotForTests(t, simulationTimeMs: t / 300);
        Assert.Equal(single, MarketFingerprint(chunked, registry));

        // Normal and accelerated real-time playback of the same game interval.
        foreach (var speed in new[] { SimulationSpeed.Speed1, SimulationSpeed.Speed4 })
        {
            long realMs = 0;
            var clock = new SimulationClock(SimulationSpeed.Speed0, () => realMs);
            var (played, _3) = create(clock);
            using var ___ = played;
            clock.SetSpeed(speed);
            long realHorizon = horizon / speed.GameTimeMultiplier();
            for (long step = 1; step <= 24; step++)
            {
                realMs = step * realHorizon / 24;
                played.CaptureSnapshot(advanceClock: true);
            }
            Assert.Equal(single, MarketFingerprint(played, registry));
        }

        // The explicitly allowed manual advance: six docked TravelStation hops of one game hour
        // each. Districts alternate because travelling to the district you are already in is
        // refused and would not move time at all.
        var (manual, _4) = create(null);
        using var ____ = manual;
        for (int hop = 0; hop < 6; hop++)
        {
            var destination = hop % 2 == 0 ? StationDistrict.Market : StationDistrict.Dock;
            Assert.True(manual.TravelStation(new StationTravelCommand($"hop-{hop}", destination)).Accepted);
        }
        Assert.Equal(horizon, manual.CaptureSnapshot().GameTimeMs);
        Assert.Equal(single, MarketFingerprint(manual, registry));
    }

    [Fact]
    public void Paused_real_time_does_not_advance_market()
    {
        long realMs = 0;
        var clock = new SimulationClock(SimulationSpeed.Speed0, () => realMs);
        var (engine, registry) = CreateMarketEngine(clock: clock);
        using var _ = engine;

        var initial = MarketState(engine, registry);
        for (int i = 1; i <= 10; i++)
        {
            realMs = i * GameCalendar.DayMs;
            engine.CaptureSnapshot(advanceClock: true);
            Assert.Equal(initial, MarketState(engine, registry));
        }

        // The one explicitly allowed exception moves the market by exactly one hour.
        engine.TravelStation(new StationTravelCommand("manual", StationDistrict.Market));
        Assert.Equal(GameCalendar.HourMs, engine.CaptureSnapshot().GameTimeMs);
        Assert.Equal(126, MarketStock(engine, registry, "item.ice"));
    }

    [Fact]
    public void Daily_budget_grant_is_not_multiplied_twenty_four_times()
    {
        var (engine, registry) = CreateMarketEngine();
        using var _ = engine;
        Assert.Equal(MarketInitialCredits, MarketBudget(engine));

        // maxBudget 19200 over divisor 24 is 800 per day — not 800 per hour.
        long dailyGrant = MarketMaxBudget / 24;
        Assert.Equal(800, dailyGrant);
        engine.CaptureSnapshotForTests(GameCalendar.DayMs, simulationTimeMs: 1);
        Assert.Equal(MarketInitialCredits + dailyGrant, MarketBudget(engine));

        // A daily grant that does not divide evenly by 24 is still distributed as whole credits
        // and still sums to exactly one day's worth.
        var (uneven, _2) = CreateMarketEngine(BoundedProfile(divisorPerDay: 192));
        using var __ = uneven;
        long unevenDaily = MarketMaxBudget / 192;
        Assert.Equal(100, unevenDaily);
        long previous = MarketBudget(uneven);
        for (int hour = 1; hour <= 24; hour++)
        {
            uneven.CaptureSnapshotForTests(hour * GameCalendar.HourMs, simulationTimeMs: hour);
            long granted = MarketBudget(uneven) - previous;
            Assert.InRange(granted, 4, 5);
            previous = MarketBudget(uneven);
        }
        Assert.Equal(MarketInitialCredits + unevenDaily, MarketBudget(uneven));

        // Grants that would cross the cap are discarded, never carried over.
        var (capped, _3) = CreateMarketEngine(BoundedProfile(initialCredits: MarketInitialCredits),
            adjust: save => WithStationCredits(save, MarketMaxBudget));
        using var ___ = capped;
        Assert.Equal(MarketMaxBudget, MarketBudget(capped));
        capped.CaptureSnapshotForTests(2 * GameCalendar.DayMs, simulationTimeMs: 1);
        Assert.Equal(MarketMaxBudget, MarketBudget(capped));
    }

    private static ScenarioFile WithStationCredits(ScenarioFile save, long credits) => save with
    {
        GameState = save.GameState with
        {
            SpaceObjects = save.GameState.SpaceObjects
                .Select(o => o.MarketProfileId is null ? o : o with { Credits = credits }).ToArray(),
        },
    };

    [Fact]
    public void Trade_budget_preserves_revenue_and_fee_reserve()
    {
        var (engine, registry) = CreateMarketEngine(
            adjust: save => WithStationCredits(save, MarketMaxBudget - 100));
        using var _ = engine;
        var station = MarketStation(engine);
        Assert.Equal(MarketMaxBudget - 100, station.Credits);
        Assert.Equal(MarketMaxBudget - 100, station.MarketBudgetCredits);

        // The player buys: the station keeps every credit of the price, while the budget may only
        // climb to its cap — the surplus stays as Credits instead of being destroyed.
        long priceEach = engine.CaptureSnapshot().DockedStationTrade!.Items
            .Single(i => i.ItemTypeId == "item.ice").UnitPriceCredits;
        engine.ReceiveCommand(new PlayerCommand("buy", 1, ShipId(engine), ContainerModuleId(engine, registry),
            TradeCommandTypes.Buy, ItemTypeId: "item.ice", Quantity: 5));
        engine.CaptureSnapshot();
        long income = priceEach * 5;
        station = MarketStation(engine);
        Assert.Equal(MarketMaxBudget - 100 + income, station.Credits);
        Assert.Equal(Math.Min(MarketMaxBudget, MarketMaxBudget - 100 + income), station.MarketBudgetCredits);
        Assert.True(station.Credits >= station.MarketBudgetCredits);

        // An hourly refill first makes the cash the station already holds available again, rather
        // than minting more: with Credits well above the budget, only the budget moves.
        var save = engine.CaptureSaveState();
        var lowered = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects
                    .Select(o => o.MarketProfileId is null ? o : o with { MarketBudgetCredits = 1000 }).ToArray(),
            },
        };
        using var reserved = new SimulationEngine(MarketRegistry(BoundedProfile()));
        reserved.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(lowered), allowNonZeroGameTime: true), isSave: true);
        long creditsBefore = MarketStation(reserved).Credits;
        Assert.Equal(1000, MarketBudget(reserved));

        reserved.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 1);
        Assert.Equal(creditsBefore, MarketStation(reserved).Credits);
        Assert.Equal(1000 + MarketMaxBudget / 24 / 24, MarketBudget(reserved));
    }

    /// <summary>Seeds the ship's container module with cargo the player did not buy from this station.</summary>
    private static ScenarioFile WithShipCargo(ScenarioFile save, string itemTypeId, long quantity) => save with
    {
        GameState = save.GameState with
        {
            SpaceObjects = save.GameState.SpaceObjects
                .Select(o => o.ObjectId != save.GameState.PlayerShipObjectId ? o : o with
                {
                    Modules = o.Modules!.Select(m => m.Cargo is not { Count: > 0 } cargo ? m : m with
                    {
                        Cargo = cargo.Any(c => c.ItemTypeId == itemTypeId)
                            ? cargo.Select(c => c.ItemTypeId == itemTypeId ? c with { Quantity = quantity } : c).ToArray()
                            : cargo.Append(new CargoStackData(itemTypeId, quantity)).ToArray(),
                    }).ToArray(),
                }).ToArray(),
        },
    };

    [Fact]
    public void Sell_respects_stock_headroom_and_reports_partial_quantity()
    {
        // Three units of room for ice, and the player already carries five — bought elsewhere, so
        // the station's own stock was never reduced to make space for them.
        var (engine, registry) = CreateMarketEngine(
            stock: [new("item.ice", 2 * IceTarget - 3), new("item.water", WaterTarget), new("item.steel", SteelTarget)],
            adjust: save => WithShipCargo(save, "item.ice", 5));
        using var _ = engine;
        string moduleId = ContainerModuleId(engine, registry);

        long stockBefore = MarketStock(engine, registry, "item.ice");
        long budgetBefore = MarketBudget(engine);
        long playerBefore = engine.PlayerCredits;
        long priceEach = engine.CaptureSnapshot().DockedStationTrade!.Items
            .Single(i => i.ItemTypeId == "item.ice").UnitPriceCredits;

        engine.ReceiveCommand(new PlayerCommand("sell", 2, ShipId(engine), moduleId,
            TradeCommandTypes.Sell, ItemTypeId: "item.ice", Quantity: 5));
        var result = Assert.Single(engine.CaptureSnapshot().CommandResults);

        Assert.Equal(CommandResultStatus.Executed, result.Status);
        Assert.Equal(3, result.ExecutedQuantity);
        Assert.Equal(stockBefore + 3, MarketStock(engine, registry, "item.ice"));
        Assert.Equal(2 * IceTarget, MarketStock(engine, registry, "item.ice"));
        Assert.Equal(playerBefore + 3 * priceEach, engine.PlayerCredits);
        Assert.Equal(budgetBefore - 3 * priceEach, MarketBudget(engine));

        // A completely full market refuses outright and changes nothing.
        long fullStock = MarketStock(engine, registry, "item.ice");
        long fullBudget = MarketBudget(engine);
        engine.ReceiveCommand(new PlayerCommand("sell-full", 3, ShipId(engine), moduleId,
            TradeCommandTypes.Sell, ItemTypeId: "item.ice", Quantity: 1));
        var rejected = Assert.Single(engine.CaptureSnapshot().CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, rejected.Status);
        Assert.Equal("station_stock_full", rejected.ReasonCode);
        Assert.Equal(fullStock, MarketStock(engine, registry, "item.ice"));
        Assert.Equal(fullBudget, MarketBudget(engine));
    }

    [Theory]
    [InlineData(49, "Shortage")]
    [InlineData(50, "Normal")]
    [InlineData(150, "Normal")]
    [InlineData(151, "Surplus")]
    [InlineData(200, "Surplus")]
    public void Snapshot_stock_bands_and_limits_match_authoritative_state(long stock, string expectedState)
    {
        // A target of exactly 100 makes the permille thresholds land on whole units.
        var profile = BoundedProfile() with
        {
            InitialInventory = [new("item.ice", 100), new("item.water", WaterTarget), new("item.steel", SteelTarget)],
        };
        profile = profile with
        {
            Economy = profile.Economy! with
            {
                StockTargets = [new("item.ice", 100), new("item.water", WaterTarget), new("item.steel", SteelTarget)],
            },
        };
        var (engine, _registry) = CreateMarketEngine(profile,
            stock: [new("item.ice", stock), new("item.water", WaterTarget), new("item.steel", SteelTarget)]);
        using var _ = engine;

        var trade = engine.CaptureSnapshot().DockedStationTrade!;
        var ice = trade.Items.Single(i => i.ItemTypeId == "item.ice");
        Assert.Equal(100, ice.TargetStock);
        Assert.Equal(200, ice.MaxStock);
        Assert.Equal(200 - stock, ice.FreeStockCapacity);
        Assert.Equal(stock, ice.StockQuantity);
        Assert.Equal(expectedState, ice.StockState!.Value.ToString());
        Assert.True(ice.MaxSellableQuantity <= ice.FreeStockCapacity);

        // Fuel never takes part in cargo flow, so it keeps the legacy null shape.
        var fuel = trade.Items.Single(i => i.ItemTypeId == "item.fuel");
        Assert.Null(fuel.TargetStock);
        Assert.Null(fuel.MaxStock);
        Assert.Null(fuel.FreeStockCapacity);
        Assert.Null(fuel.StockState);
    }

    [Fact]
    public void Invalid_economy_save_does_not_replace_loaded_world()
    {
        var modulesProfile = BoundedProfile(StationMarketProductionSource.Modules);
        var factory = IceFactory();
        var (engine, registry) = CreateMarketEngine(
            modulesProfile, extraFactory: factory,
            producingModules: [new StationProducingModuleData("factory.market-test")]);
        using var _ = engine;
        engine.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 1);
        var save = engine.CaptureSaveState();
        string before = ScenarioLoader.Serialize(engine.CaptureSaveState());

        ScenarioFile Corrupt(Func<SpaceObjectData, SpaceObjectData> change) => save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects
                    .Select(o => o.MarketProfileId is null ? o : change(o)).ToArray(),
            },
        };

        // Each case asserts the message it is meant to trigger, so none of them can pass for an
        // unrelated reason — the over-cap case in particular keeps the rest of the inventory
        // intact, otherwise the loader would reject it for the missing items instead.
        (string Name, ScenarioFile Save, string Expected)[] corrupted =
        [
            ("profile stamp", Corrupt(o => o with { MarketProfileFingerprint = "not-the-profile" }),
                "marketProfileFingerprint does not match"),
            ("missing budget", Corrupt(o => o with { MarketBudgetCredits = null }),
                "marketBudgetCredits is required"),
            ("budget above cap", Corrupt(o => o with { MarketBudgetCredits = MarketMaxBudget + 1 }),
                "marketBudgetCredits must be within"),
            ("negative budget", Corrupt(o => o with { MarketBudgetCredits = -1 }),
                "marketBudgetCredits must be within"),
            ("stock above cap", Corrupt(o => o with
            {
                Inventory = o.Inventory!
                    .Select(i => i.ItemTypeId == "item.ice" ? i with { Quantity = 2 * IceTarget + 1 } : i).ToArray(),
            }), "is outside [0,"),
            ("non-positive pending", Corrupt(o => o with
            {
                ProducingModules = [new StationProducingModuleData("factory.market-test",
                    PendingOutput: [new StationInventoryItemData("item.ice", 0)])],
            }), "quantity must be positive"),
            ("pending outside the recipe", Corrupt(o => o with
            {
                ProducingModules = [new StationProducingModuleData("factory.market-test",
                    PendingOutput: [new StationInventoryItemData("item.steel", 1)])],
            }), "is not a recipe output"),
            ("pending above one batch", Corrupt(o => o with
            {
                ProducingModules = [new StationProducingModuleData("factory.market-test",
                    PendingOutput: [new StationInventoryItemData("item.ice", 19)])],
            }), "exceeds one recipe batch"),
            ("pending together with a due cycle", Corrupt(o => o with
            {
                ProducingModules = [new StationProducingModuleData("factory.market-test",
                    NextProductionDueGameTimeMs: 9 * GameCalendar.HourMs,
                    PendingOutput: [new StationInventoryItemData("item.ice", 1)])],
            }), "both pendingOutput and nextProductionDueGameTimeMs"),
        ];

        foreach (var (name, candidate, expected) in corrupted)
        {
            var error = Assert.Throws<ScenarioException>(() => engine.LoadScenario(candidate, isSave: true));
            Assert.True(error.Message.Contains(expected, StringComparison.Ordinal), $"{name}: {error.Message}");
            Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
        }
    }

    [Fact]
    public void Existing_legacy_production_and_motion_are_unchanged()
    {
        // The no-economy path must stay byte-identical: the same recipe timing, the same
        // unbounded output and the same calendar-to-motion mapping as before US-0002.
        using var engine = CreateIntervalEngine();
        var boundary = engine.CaptureSnapshotForTests(GameCalendar.DayMs, simulationTimeMs: 123);
        Assert.Equal(4, ProducedFood(engine));
        Assert.Equal(123, boundary.SimulationTimeMs);
        // The next cycle is scheduled on the following pass, exactly as before US-0002.
        engine.CaptureSnapshotForTests(GameCalendar.DayMs + 1, simulationTimeMs: 124);
        Assert.Equal(GameCalendar.DayMs + 12 * GameCalendar.HourMs, ProductionDue(engine));

        var station = engine.RuntimeObjects.Single(o => o.ObjectType == "Station");
        Assert.Null(station.MarketBudgetCredits);
        Assert.True(station.ProducingModules.Single().PendingOutput.IsDefaultOrEmpty);

        // And the docked station of a real New Game scenario stays unbounded too.
        using var real = RationScheduleTests.CreateEngine();
        var trade = real.CaptureSnapshot().DockedStationTrade!;
        Assert.All(trade.Items, item =>
        {
            Assert.Null(item.TargetStock);
            Assert.Null(item.MaxStock);
            Assert.Null(item.StockState);
        });
    }
}
