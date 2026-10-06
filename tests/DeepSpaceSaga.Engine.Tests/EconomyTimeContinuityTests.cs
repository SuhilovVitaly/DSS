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
            if (hour == 12) Assert.Equal(0, RationScheduleTests.Food(actual));
        }

        var expected = explicitInterval.CaptureSnapshotForTests(2 * GameCalendar.DayMs,
            simulationTimeMs: 777);
        AssertEconomyEqual(expected, actual);
        Assert.Equal(expected.ShipEvents.ToArray(), regularEvents.ToArray());
        Assert.Equal(2 * GameCalendar.DayMs / 300, actual.SimulationTimeMs);
        Assert.Equal(777, expected.SimulationTimeMs);
        Assert.Equal(1800, expected.PlayerCredits);
        Assert.Equal(4, expected.MissingRations);
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
        if (legacyMotion)
        {
            save = TradingEconomySaveSchemaTests.WithoutNewContinuation(save, 5);
            save = save with { GameState = save.GameState with { SimulationTimeMs = null, CombatState = null } };
        }
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
        Assert.Equal(4, meal.MissingRations);
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
                MarketEventCatalogFingerprint = null,
                TradingMap = null,
                VoyageState = null,
                DialogueState = null,
                CombatState = save.GameState.CombatState! with { Launchers = [], Projectiles = [] },
                DefenseState = save.GameState.DefenseState! with { Launchers = [], Projectiles = [] },
                SpaceObjects = save.GameState.SpaceObjects
                    .Where(o => o.ObjectId == save.GameState.PlayerShipObjectId || o.ObjectId == "SPC-0002")
                    .Select(o => o with
                    {
                        // This fixture replaces the ship with a synthetic cargo-only economy object.
                        ShipClassId = null,
                        HullHitPoints = null,
                        HullHitPointsMax = null,
                        Modules = o.ObjectType == "PlayerShip"
                        ? [new("cargo", "module.test-cargo", [new(4, 2)], 100, "On", "Ready", null,
                        rations > 0 ? [new("item.food-rations", rations, 0, ["produced"])] : [])] : [],
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
        save = save with
        {
            GameState = save.GameState with
            { TradingEconomyContinuation = TradingEconomySaveMigration.ManifestFromPersistedFacts(save.GameState) }
        };
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
        Assert.Equal(196, RationScheduleTests.Food(after));
        Assert.Equal(2, loaded.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectType == "PlayerShip").Passengers!.Count);
        Assert.Equal(before.GameTimeMs, loaded.TravelStation(command).Snapshot.GameTimeMs);
        var end = loaded.CaptureSnapshotForTests(GameCalendar.DayMs);
        Assert.Equal(192, RationScheduleTests.Food(end));
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
                TradingEconomyContinuation = null,
                MarketKnowledge = null,
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
        FactoryTypeDefinition? extraFactory = null, IEnumerable<StationMarketEventDefinition>? marketEvents = null)
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
                .Select(source.StationMarketProfiles.GetDefinition).Where(p => p.TypeId != profile.TypeId).Append(profile),
            shipClasses: Enumerable.Range(0, source.ShipClasses.Count).Select(source.ShipClasses.GetDefinition), stationMarketEvents: marketEvents);
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
                TradingEconomyContinuation = null, // A fresh scenario has no continuation manifest.
                MarketKnowledge = null, // New fixture profile gets its own initial observation.
                TradingMap = null,
                VoyageState = null,
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

    [Theory]
    [InlineData("input", 0)]
    [InlineData("input", -1)]
    [InlineData("output", 0)]
    [InlineData("output", -1)]
    [InlineData("duration", 0)]
    [InlineData("duration", -1)]
    [InlineData("duplicate-input", 6)]
    public void Bounded_module_recipe_rejects_invalid_materials_or_duration_with_context_before_world_replacement(string field, long value)
    {
        var profile = BoundedProfile(StationMarketProductionSource.Modules);
        var factory = IceFactory();
        factory = factory with
        {
            Recipe = field switch
            {
                "input" => factory.Recipe with { Inputs = [new("item.water", value)] },
                "output" => factory.Recipe with { Outputs = [new("item.ice", value)] },
                "duplicate-input" => factory.Recipe with { Inputs = [new("item.water", value), new("item.water", value)] },
                _ => factory.Recipe with { CycleDurationMs = value },
            }
        };
        using var engine = new SimulationEngine(MarketRegistry(profile, factory));
        var valid = MarketTemplate(profile);
        engine.LoadScenario(valid);
        string before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        var invalid = MarketTemplate(profile, producingModules: [new StationProducingModuleData(factory.TypeId)]);
        var error = Assert.Throws<ScenarioException>(() => engine.LoadScenario(invalid));
        Assert.Contains(factory.TypeId, error.Message, StringComparison.Ordinal);
        Assert.Contains(profile.TypeId, error.Message, StringComparison.Ordinal);
        Assert.Contains("Station", error.Message, StringComparison.Ordinal);
        Assert.Contains("positive", error.Message, StringComparison.Ordinal);
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
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
        var buy = engine.GetTradeQuote(new("buy-ice-quote", ShipId(engine), ContainerModuleId(engine, registry),
            TradeCommandTypes.Buy, "item.ice", 5));
        Assert.Null(buy.DisabledReason);
        engine.ReceiveCommand(QuotedTradeExecutionTests.Bind("buy-ice", buy));
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
            Assert.Equal(SaveFormat.CurrentSaveFormatVersion, save.SaveFormatVersion);

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
        var buy = engine.GetTradeQuote(new("budget-buy-quote", ShipId(engine), ContainerModuleId(engine, registry),
            TradeCommandTypes.Buy, "item.ice", 5));
        Assert.Null(buy.DisabledReason);
        engine.ReceiveCommand(QuotedTradeExecutionTests.Bind("buy", buy));
        var bought = Assert.Single(engine.CaptureSnapshot().CommandResults);
        Assert.Equal(CommandResultStatus.Executed, bought.Status);
        Assert.Equal(buy.TotalCredits, bought.TradeReceipt!.TotalCredits);
        long income = bought.TradeReceipt.TotalCredits;
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
        var sell = engine.GetTradeQuote(new("capacity-sell-quote", ShipId(engine), moduleId,
            TradeCommandTypes.Sell, "item.ice", 5));
        Assert.Null(sell.DisabledReason);
        Assert.Equal(3, sell.ExecutableQuantity);
        Assert.Contains(CommandReasonCodes.StationCapacityExceeded, sell.LimitReasons);

        engine.ReceiveCommand(QuotedTradeExecutionTests.Bind("sell", sell));
        var result = Assert.Single(engine.CaptureSnapshot().CommandResults);

        Assert.Equal(CommandResultStatus.Executed, result.Status);
        Assert.Equal(3, result.ExecutedQuantity);
        Assert.Equal(stockBefore + 3, MarketStock(engine, registry, "item.ice"));
        Assert.Equal(2 * IceTarget, MarketStock(engine, registry, "item.ice"));
        Assert.Equal(sell.TotalCredits, result.TradeReceipt!.TotalCredits);
        Assert.Equal(playerBefore + result.TradeReceipt.TotalCredits, engine.PlayerCredits);
        Assert.Equal(budgetBefore - result.TradeReceipt.TotalCredits, MarketBudget(engine));

        // A completely full market refuses outright and changes nothing.
        long fullStock = MarketStock(engine, registry, "item.ice");
        long fullBudget = MarketBudget(engine);
        var full = engine.GetTradeQuote(new("full-sell-quote", ShipId(engine), moduleId,
            TradeCommandTypes.Sell, "item.ice", 1));
        Assert.Equal(CommandReasonCodes.StationCapacityExceeded, full.DisabledReason);
        Assert.Equal(0, full.ExecutableQuantity);
        Assert.Empty(full.QuoteId);
        engine.ReceiveCommand(QuotedTradeExecutionTests.Bind("sell-full", full));
        var rejected = Assert.Single(engine.CaptureSnapshot().CommandResults);
        Assert.Equal(CommandResultStatus.Rejected, rejected.Status);
        Assert.Equal(CommandReasonCodes.InvalidQuote, rejected.ReasonCode);
        Assert.Equal(0, rejected.TradeReceipt!.ExecutedQuantity);
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
        var save = engine.CaptureSaveStateForTests(GameCalendar.HourMs, SimulationSpeed.Speed0, 1);
        string before = ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(GameCalendar.HourMs, SimulationSpeed.Speed0, 1));

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
            Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(GameCalendar.HourMs, SimulationSpeed.Speed0, 1)));
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Explicit_inventory_marker_cannot_bypass_bounded_market_validation(bool isSave, bool missingTarget)
    {
        var profile = BoundedProfile();
        var (engine, _) = CreateMarketEngine(profile);
        using var lifetime = engine;
        var baseline = engine.CaptureSaveState();
        string before = ScenarioLoader.Serialize(baseline);
        var source = isSave ? baseline : MarketTemplate(profile);
        string badItem = missingTarget ? "item.energy-cells" : "item.ice";
        var candidate = source with
        {
            GameState = source.GameState with
            {
                SpaceObjects = source.GameState.SpaceObjects.Select(obj => obj.MarketProfileId is null ? obj : obj with
                {
                    ExplicitInventoryItemTypeIds = [badItem],
                    Inventory = missingTarget
                        ? (obj.Inventory ?? profile.InitialInventory.Select(stock => new StationInventoryItemData(stock.ItemTypeId, stock.Quantity)).ToArray())
                            .Append(new StationInventoryItemData(badItem, 1)).ToArray()
                        : (obj.Inventory ?? profile.InitialInventory.Select(stock => new StationInventoryItemData(stock.ItemTypeId, stock.Quantity)).ToArray())
                            .Select(stock => stock.ItemTypeId == badItem ? stock with { Quantity = 2 * IceTarget + 1 } : stock).ToArray(),
                }).ToArray(),
            },
        };
        var error = Assert.Throws<ScenarioException>(() => engine.LoadScenario(candidate, isSave));
        Assert.Contains(badItem, error.Message);
        Assert.Contains(missingTarget ? "has no stockTargets entry" : "is outside [0,", error.Message);
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    [Theory]
    [InlineData("Default")]
    [InlineData("Docked")]
    [InlineData("Undocked")]
    public void Shipping_explicit_cargo_is_preserved_bounded_and_validated_after_save_load(string scenarioName)
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));
        string scenarioPath = Path.Combine(root, "Scenarios", scenarioName, "scenario.json");
        var authored = ScenarioLoader.LoadFromFile(scenarioPath).GameState.SpaceObjects.Single(obj => obj.ObjectId == "SPC-0002");
        using var engine = SimulationEngine.CreateFromScenarioFile(Path.Combine(root, "Settings.json"), scenarioPath);
        var initial = engine.CaptureSaveState();
        var station = initial.GameState.SpaceObjects.Single(obj => obj.ObjectId == "SPC-0002");
        Assert.Equal("Large", station.StationSize);
        foreach (var item in authored.Inventory!)
            Assert.Equal(item.Quantity, station.Inventory!.Single(stock => stock.ItemTypeId == item.ItemTypeId).Quantity);
        var markets = engine.CaptureMarketDiagnosticsForTests();
        Assert.Equal(5, markets.Length);
        foreach (var market in markets)
            foreach (var item in market.Market.Items.Where(item => item.ItemTypeId != "item.fuel"))
            {
                Assert.NotNull(item.TargetStock);
                Assert.Equal(2 * item.TargetStock, item.MaxStock);
                Assert.InRange(item.StockQuantity, 0, item.MaxStock!.Value);
                Assert.Equal(item.MaxStock - item.StockQuantity, item.FreeStockCapacity);
            }
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
        using var restored = new SimulationEngine(registry);
        restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(initial), true), true);
        Assert.Equal(ScenarioLoader.Serialize(initial), ScenarioLoader.Serialize(restored.CaptureSaveState()));
        var ice = markets.Single(market => market.Market.StationObjectId == "SPC-0002").Market.Items.Single(item => item.ItemTypeId == "item.ice");
        var corrupted = initial with
        {
            GameState = initial.GameState with
            {
                SpaceObjects = initial.GameState.SpaceObjects.Select(obj => obj.ObjectId != "SPC-0002" ? obj : obj with
                {
                    Inventory = obj.Inventory!.Select(stock => stock.ItemTypeId == "item.ice" ? stock with { Quantity = ice.MaxStock!.Value + 1 } : stock).ToArray(),
                }).ToArray(),
            }
        };
        var error = Assert.Throws<ScenarioException>(() => restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(corrupted), true), true));
        Assert.Contains("item.ice", error.Message);
        Assert.Contains("is outside [0,", error.Message);
        Assert.Equal(ScenarioLoader.Serialize(initial), ScenarioLoader.Serialize(restored.CaptureSaveState()));
    }

    [Fact]
    public void Near_int64_capacity_skips_overfull_profile_batch_without_overflow_or_input_loss()
    {
        var profile = BoundedProfile();
        profile = profile with
        {
            Economy = profile.Economy! with
            {
                StockTargets = profile.Economy.StockTargets.Select(target => target.ItemTypeId == "item.ice"
                    ? target with { TargetStock = long.MaxValue / 4 } : target).ToImmutableArray(),
                HourlyOutputs = [new("item.ice", 1_000_000_000_000_000_000)],
            }
        };
        var (engine, registry) = CreateMarketEngine(profile,
            stock: [new("item.ice", long.MaxValue - 10), new("item.water", WaterTarget), new("item.steel", SteelTarget)],
            adjust: source => source with
            {
                GameState = source.GameState with
                {
                    SpaceObjects = source.GameState.SpaceObjects.Select(obj => obj.MarketProfileId is null ? obj : obj with
                    { StationSize = nameof(StationSize.Huge) }).ToArray(),
                }
            });
        using var lifetime = engine;
        var before = MarketState(engine, registry);
        engine.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 0);
        var after = MarketState(engine, registry);
        Assert.Equal(before.Ice, after.Ice);
        Assert.Equal(before.Water, after.Water);
        // Independent hourly steel consumption still applies when the production batch is skipped.
        Assert.Equal(before.Steel - 2, after.Steel);
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
    // US-0007: real calendar/market/save paths with deliberately forced, small catalogs.
    private static StationMarketEventDefinition MarketEventDefinition(string id = "event.reactor-accident", int chance = 1000, int priority = 10) =>
        new(id, id + ".Name", id + ".Description", id + ".Effect", priority, chance, 2, 2, [BoundedProfile().TypeId],
            [new("item.ice", 500, 1000, 2000), new("item.water", 1000, 1500, 1000, 24), new("item.steel", 1000, 1500, 1000)]);

    private static (SimulationEngine Engine, GameDataRegistry Registry) CreateEventEngine(IEnumerable<StationMarketEventDefinition> definitions,
        ulong seed = 1729, IReadOnlyList<StationInventoryItemData>? stock = null, SimulationClock? clock = null,
        Func<ScenarioFile, ScenarioFile>? adjust = null, StationMarketProfileDefinition? profile = null)
    {
        profile ??= BoundedProfile();
        var registry = MarketRegistry(profile, marketEvents: definitions);
        var scenario = MarketTemplate(profile, stock);
        scenario = scenario with { GameState = scenario.GameState with { MasterSeed = seed, MarketEventCatalogFingerprint = null } };
        if (adjust is not null) scenario = adjust(scenario);
        var engine = new SimulationEngine(registry, [], clock ?? new SimulationClock(SimulationSpeed.Speed0, () => 0));
        engine.LoadScenario(scenario);
        return (engine, registry);
    }

    private static string EventMarketState(SimulationEngine engine, long time) =>
        System.Text.Json.JsonSerializer.Serialize(engine.CaptureSnapshotForTests(time, simulationTimeMs: 0).DockedStationTrade);

    [Fact]
    public void MarketEvent_forced_activation_effects_delta_price_and_single_revision_are_authoritative()
    {
        var (engine, registry) = CreateEventEngine([MarketEventDefinition()]);
        using (engine)
        {
            var before = engine.CaptureSnapshotForTests(GameCalendar.HourMs - 1, simulationTimeMs: 0).DockedStationTrade!;
            Assert.Empty(before.ActiveEvents);
            long basePrice = before.Items.Single(i => i.ItemTypeId == "item.ice").UnitPriceCredits;
            var actual = engine.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 0).DockedStationTrade!;
            var evt = Assert.Single(actual.ActiveEvents);
            Assert.Equal("event.reactor-accident@SPC-0002@1", evt.EventId);
            Assert.Equal(3 * GameCalendar.HourMs, evt.EndsGameTimeMs);
            Assert.Equal(2 * GameCalendar.HourMs, evt.RemainingGameTimeMs);
            Assert.Equal(before.MarketRevision + 1, actual.MarketRevision);
            Assert.Equal((117L, 90L, 69L), (MarketStock(engine, registry, "item.ice"), MarketStock(engine, registry, "item.water"), MarketStock(engine, registry, "item.steel")));
            Assert.Equal(basePrice * 2, actual.Items.Single(i => i.ItemTypeId == "item.ice").UnitPriceCredits);
            Assert.Equal(actual.MarketRevision, engine.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 0).DockedStationTrade!.MarketRevision);
            var save = engine.CaptureSaveStateForTests(GameCalendar.HourMs, SimulationSpeed.Speed0, 0);
            Assert.Equal(registry.StationMarketEventCatalogFingerprint, save.GameState.MarketEventCatalogFingerprint);
            Assert.True(Assert.Single(save.GameState.SpaceObjects.Single(o => o.ObjectId == "SPC-0002").Events!).ActivationStockDeltaApplied);
        }
    }

    [Fact]
    public void MarketEvent_half_open_expiry_restores_prices_and_baseline_rates()
    {
        ulong seed = 1;
        ulong Roll(ulong s, long h) => DeepSpaceSaga.Engine.Rng.RngStreamSeedDerivation.DeriveStreamSeed(s, $"market-event:roll:SPC-0002:event.reactor-accident:{h}") % 1000;
        while (Roll(seed, 1) >= Roll(seed, 3)) seed++;
        var definition = MarketEventDefinition(chance: (int)Roll(seed, 1) + 1);
        var (engine, registry) = CreateEventEngine([definition], seed);
        using (engine)
        {
            long baseline = engine.CaptureSnapshot().DockedStationTrade!.Items.Single(i => i.ItemTypeId == "item.ice").UnitPriceCredits;
            var active = engine.CaptureSnapshotForTests(3 * GameCalendar.HourMs - 1, simulationTimeMs: 0).DockedStationTrade!;
            Assert.Equal(1, Assert.Single(active.ActiveEvents).RemainingGameTimeMs);
            long water = MarketStock(engine, registry, "item.water");
            long ice = MarketStock(engine, registry, "item.ice");
            var ended = engine.CaptureSnapshotForTests(3 * GameCalendar.HourMs, simulationTimeMs: 0).DockedStationTrade!;
            Assert.Empty(ended.ActiveEvents);
            Assert.Equal(baseline, ended.Items.Single(i => i.ItemTypeId == "item.ice").UnitPriceCredits);
            Assert.Equal(water - 4, MarketStock(engine, registry, "item.water"));
            Assert.Equal(ice + 18, MarketStock(engine, registry, "item.ice"));
            Assert.Equal(active.MarketRevision + 1, ended.MarketRevision);
        }
    }

    [Fact]
    public void MarketEvent_priority_and_id_cap_candidates_independently_of_input_order()
    {
        var definitions = new[] { MarketEventDefinition("event.quarantine", priority: 20), MarketEventDefinition("event.decompression", priority: 20), MarketEventDefinition(priority: 1) };
        var (first, _) = CreateEventEngine(definitions);
        var (second, _) = CreateEventEngine(definitions.AsEnumerable().Reverse(), adjust: s => s with { GameState = s.GameState with { SpaceObjects = s.GameState.SpaceObjects.Reverse().ToArray() } });
        using (first) using (second)
        {
            Assert.Equal(EventMarketState(first, GameCalendar.HourMs), EventMarketState(second, GameCalendar.HourMs));
            Assert.Equal(new[] { "event.decompression", "event.quarantine" }, first.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 0).DockedStationTrade!.ActiveEvents.Select(e => e.DefinitionId));
            // Two simultaneous 500 permille output multipliers: 18 * .5 * .5 = 4.5, rounded once to 5.
            Assert.Equal(113, first.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 0).DockedStationTrade!.Items.Single(i => i.ItemTypeId == "item.ice").StockQuantity);
        }
    }

    [Theory]
    [InlineData(0, -100, 0)]
    [InlineData(143, 24, 144)]
    public void MarketEvent_delta_clamps_once_and_survives_mid_event_save(long initial, long delta, long expected)
    {
        var definition = MarketEventDefinition() with { ItemEffects = [new("item.water", 1000, 0, 1000, delta)] };
        var (engine, registry) = CreateEventEngine([definition], stock: [new("item.ice", 108), new("item.water", initial), new("item.steel", 72)]);
        using (engine)
        {
            var save = engine.CaptureSaveStateForTests(GameCalendar.HourMs, SimulationSpeed.Speed0, 0);
            long clamped = Math.Clamp(initial + delta, 0, 144);
            Assert.Equal(expected, MarketStock(engine, registry, "item.water"));
            Assert.Equal(expected, clamped);
            using var loaded = new SimulationEngine(registry);
            loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true));
            Assert.Equal(EventMarketState(engine, 2 * GameCalendar.HourMs), EventMarketState(loaded, 2 * GameCalendar.HourMs));
            Assert.Equal(clamped, MarketStock(loaded, registry, "item.water"));
        }
    }

    [Theory]
    [InlineData("fingerprint")]
    [InlineData("missing-fingerprint")]
    [InlineData("effect")]
    [InlineData("key")]
    [InlineData("route")]
    [InlineData("unapplied")]
    [InlineData("duration")]
    [InlineData("duplicate")]
    [InlineData("three")]
    public void MarketEvent_invalid_save_rejects_without_world_replacement(string defect)
    {
        var (original, registry) = CreateEventEngine([MarketEventDefinition()]);
        using (original)
        {
            var save = original.CaptureSaveStateForTests(GameCalendar.HourMs, SimulationSpeed.Speed0, 0);
            var station = save.GameState.SpaceObjects.Single(o => o.ObjectId == "SPC-0002");
            var evt = Assert.Single(station.Events!);
            IReadOnlyList<StationEventData> events = defect switch
            {
                "effect" => [evt with { ItemEffects = [new("item.water", 1000, 1500, 1000, 25)] }],
                "key" => [evt with { DisplayNameKey = "Wrong" }],
                "route" => [evt with { RouteEffect = new("Restricted", 1, 1000, 1000) }],
                "unapplied" => [evt with { ActivationStockDeltaApplied = false }],
                "duration" => [evt with { DurationMs = 0 }],
                "duplicate" => [evt, evt],
                "three" => [evt, new("legacy1", "One", null, 0, null, []), new("legacy2", "Two", null, 0, null, [])],
                _ => [evt],
            };
            save = save with
            {
                GameState = save.GameState with
                {
                    MarketEventCatalogFingerprint = defect == "fingerprint" ? "WRONG" : defect == "missing-fingerprint" ? null : save.GameState.MarketEventCatalogFingerprint,
                    SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId == station.ObjectId ? o with { Events = events } : o).ToArray(),
                }
            };
            var (target, _) = CreateEventEngine([MarketEventDefinition()]);
            using (target)
            {
                string before = ScenarioLoader.Serialize(target.CaptureSaveState());
                Assert.Throws<ScenarioException>(() => target.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true)));
                Assert.Equal(before, ScenarioLoader.Serialize(target.CaptureSaveState()));
            }
        }
    }

    [Fact]
    public void MarketEvent_chunked_single_step_and_saved_continuation_are_identical()
    {
        var definition = MarketEventDefinition() with { MaxDurationHours = 4, ItemEffects = [new("item.ice", 500, 1000, 1200)] };
        var (single, registry) = CreateEventEngine([definition]);
        var (chunks, _) = CreateEventEngine([definition]);
        using (single) using (chunks)
        {
            for (long time = 100003; time < 5 * GameCalendar.HourMs; time += 100003) chunks.CaptureSnapshotForTests(time, simulationTimeMs: 0);
            var save = chunks.CaptureSaveStateForTests(5 * GameCalendar.HourMs + 123, SimulationSpeed.Speed0, 0);
            using var loaded = new SimulationEngine(registry);
            loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true));
            string expected = EventMarketState(single, 12 * GameCalendar.HourMs);
            Assert.Equal(expected, EventMarketState(chunks, 12 * GameCalendar.HourMs));
            Assert.Equal(expected, EventMarketState(loaded, 12 * GameCalendar.HourMs));
            Assert.Equal(0, loaded.CaptureSnapshotForTests(12 * GameCalendar.HourMs, simulationTimeMs: 0).MotionTimeMs);
        }
    }

    [Fact]
    public void MarketEvent_paused_clock_and_permanent_legacy_slots_do_not_roll()
    {
        long real = 0;
        var clock = new SimulationClock(SimulationSpeed.Speed0, () => real);
        var (engine, _) = CreateEventEngine([MarketEventDefinition()], clock: clock, adjust: s => s with
        {
            GameState = s.GameState with
            {
                SpaceObjects = s.GameState.SpaceObjects.Select(o => o.ObjectType != "Station" ? o : o with { Events = [new("legacy1", "One", "First", 0, null, []), new("legacy2", "Two", "Second", 0, null, [])] }).ToArray(),
            }
        });
        using (engine)
        {
            string before = EventMarketState(engine, 0);
            real = 10 * GameCalendar.DayMs;
            Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(engine.CaptureSnapshot(advanceClock: true).DockedStationTrade));
            var events = engine.CaptureSnapshotForTests(3 * GameCalendar.HourMs, simulationTimeMs: 0).DockedStationTrade!.ActiveEvents;
            Assert.Equal(2, events.Length);
            Assert.All(events, e => { Assert.Equal(long.MaxValue, e.RemainingGameTimeMs); Assert.Empty(e.DisplayNameKey); Assert.NotNull(e.LegacyDisplayName); });
        }
    }
    [Fact]
    public void MarketEvent_price_only_activation_invalidates_quote_without_stock_or_budget_change()
    {
        var profile = BoundedProfile() with { Economy = null };
        var definition = MarketEventDefinition() with { ItemEffects = [new("item.ice", 1000, 1000, 2000)] };
        var (engine, _) = CreateEventEngine([definition], profile: profile);
        using (engine)
        {
            var quote = engine.GetTradeQuote(new TradeQuoteRequest("before-event", "SPC-0001", "MOD-PLAYER-CARGO-01", TradeCommandTypes.Buy, "item.ice", 1));
            Assert.Null(quote.DisabledReason);
            var before = engine.CaptureSnapshot().DockedStationTrade!;
            var active = engine.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 0).DockedStationTrade!;
            Assert.Equal(before.Items.Select(i => i.StockQuantity), active.Items.Select(i => i.StockQuantity));
            Assert.Equal(before.MarketRevision + 1, active.MarketRevision);
            engine.ReceiveCommand(new PlayerCommand("stale-event", 1, "SPC-0001", "MOD-PLAYER-CARGO-01", TradeCommandTypes.Buy,
                ItemTypeId: "item.ice", Quantity: 1, QuoteId: quote.QuoteId, MarketRevision: quote.MarketRevision));
            var result = engine.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 0);
            Assert.Equal(CommandReasonCodes.StaleQuote, Assert.Single(result.CommandResults).ReasonCode);
            Assert.Equal(active.MarketRevision, result.DockedStationTrade!.MarketRevision);
            Assert.Equal(active.Items.ToArray(), result.DockedStationTrade.Items.ToArray());
        }
    }

    [Fact]
    public void MarketEvent_overflow_after_first_candidate_rolls_back_all_market_assignments()
    {
        var shortEvent = MarketEventDefinition(priority: 20) with { MinDurationHours = 1, MaxDurationHours = 1 };
        var longEvent = MarketEventDefinition("event.decompression", priority: 10) with
        {
            MinDurationHours = 168,
            MaxDurationHours = 168,
            EligibleMarketProfileIds = ["market.transit"],
            ItemEffects = [new("item.water", 1000, 1000, 1100, 24)],
        };
        var (engine, _) = CreateEventEngine([shortEvent, longEvent], adjust: s => s with
        {
            GameState = s.GameState with
            {
                SpaceObjects = s.GameState.SpaceObjects.Append(s.GameState.SpaceObjects.Single(o => o.ObjectType == "Station") with
                { ObjectId = "SPC-9999", PositionX = 100000, MarketProfileId = "market.transit" }).ToArray(),
            }
        });
        using (engine)
        {
            string before = ScenarioLoader.Serialize(engine.CaptureSaveState());
            long time = long.MaxValue - 2 * GameCalendar.HourMs;
            time -= time % GameCalendar.HourMs;
            var method = typeof(SimulationEngine).GetMethod("ApplyMarketEventAndHour", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            var error = Assert.Throws<System.Reflection.TargetInvocationException>(() => method.Invoke(engine, [time]));
            Assert.IsType<ScenarioException>(error.InnerException);
            Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
            Assert.Empty(engine.CaptureSnapshot().DockedStationTrade!.ActiveEvents);
        }
    }

    [Fact]
    public void MarketEvent_legacy_save_without_fingerprint_starts_future_rolls_without_replaying_past()
    {
        var definition = MarketEventDefinition();
        var (original, registry) = CreateEventEngine([definition]);
        using (original)
        {
            var save = original.CaptureSaveStateForTests(GameCalendar.HourMs + 123, SimulationSpeed.Speed0, 0);
            save = save with
            {
                SaveFormatVersion = 9,
                GameState = save.GameState with
                {
                    TradingEconomyContinuation = null,
                    MarketKnowledge = null,
                    MarketEventCatalogFingerprint = null,
                    SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectType == "Station" ? o with { Events = [] } : o).ToArray()
                }
            };
            using var loaded = new SimulationEngine(registry);
            loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true));
            Assert.Empty(loaded.CaptureSnapshot().DockedStationTrade!.ActiveEvents);
            var trade = loaded.CaptureSnapshotForTests(2 * GameCalendar.HourMs, simulationTimeMs: 0).DockedStationTrade!;
            Assert.Equal("event.reactor-accident@SPC-0002@2", Assert.Single(trade.ActiveEvents).EventId);
        }
    }

    [Fact]
    public void MarketEvent_accelerated_and_normal_clocks_have_identical_calendar_effects()
    {
        long realNormal = 0, realFast = 0;
        var normalClock = new SimulationClock(SimulationSpeed.Speed0, () => realNormal);
        var fastClock = new SimulationClock(SimulationSpeed.Speed0, () => realFast);
        var (normal, _) = CreateEventEngine([MarketEventDefinition()], clock: normalClock);
        var (fast, _) = CreateEventEngine([MarketEventDefinition()], clock: fastClock);
        using (normal) using (fast)
        {
            normalClock.SetSpeed(SimulationSpeed.Speed1);
            fastClock.SetSpeed(SimulationSpeed.Speed4);
            for (int hour = 1; hour <= 8; hour++)
            {
                realNormal = hour * GameCalendar.HourMs / 300;
                realFast = realNormal / (int)SimulationSpeed.Speed4;
                var left = normal.CaptureSnapshot(advanceClock: true);
                var right = fast.CaptureSnapshot(advanceClock: true);
                Assert.Equal(left.GameTimeMs, right.GameTimeMs);
                Assert.Equal(left.MotionTimeMs, right.MotionTimeMs);
                Assert.Equal(System.Text.Json.JsonSerializer.Serialize(left.DockedStationTrade), System.Text.Json.JsonSerializer.Serialize(right.DockedStationTrade));
            }
        }
    }
    [Theory]
    [InlineData("event.pirate-blockade", "Unavailable", 1000, 1400)]
    [InlineData("event.quarantine", "Restricted", 1500, 1200)]
    public void MarketEvent_route_descriptor_is_projected_and_persisted_without_route_application(string id, string availability, int travel, int fuel)
    {
        var definition = MarketEventDefinition(id) with { RouteEffect = new(availability, 1, travel, fuel, "risk." + id[6..]) };
        var (engine, registry) = CreateEventEngine([definition]);
        using (engine)
        {
            var trade = engine.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 0).DockedStationTrade!;
            var route = Assert.Single(trade.ActiveEvents).RouteEffect;
            Assert.Equal(new StationMarketRouteEffectSnapshot(availability, 1, travel, fuel, "risk." + id[6..]), route);
            var save = engine.CaptureSaveStateForTests(GameCalendar.HourMs, SimulationSpeed.Speed0, 0);
            Assert.Null(save.GameState.TradingMap);
            using var loaded = new SimulationEngine(registry);
            loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true));
            Assert.Equal(route, Assert.Single(loaded.CaptureSnapshot().DockedStationTrade!.ActiveEvents).RouteEffect);
        }
    }

    [Theory]
    [InlineData("event.reactor-accident")]
    [InlineData("event.decompression")]
    [InlineData("event.hydroponics-failure")]
    [InlineData("event.pirate-blockade")]
    [InlineData("event.cargo-convoy")]
    [InlineData("event.quarantine")]
    [InlineData("event.repair-boom")]
    [InlineData("event.scientific-contract")]
    public void MarketEvent_every_shipping_definition_runs_its_resolved_effects(string id)
    {
        var source = RealRegistry();
        var definition = source.StationMarketEvents.GetDefinition(source.StationMarketEvents.GetIndex(id)) with { ChancePermillePerHour = 1000 };
        var profile = source.StationMarketProfiles.GetDefinition(source.StationMarketProfiles.GetIndex(definition.EligibleMarketProfileIds[0]));
        var (engine, registry) = CreateEventEngine([definition], profile: profile);
        using (engine)
        {
            engine.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 0);
            var station = MarketStation(engine);
            var evt = Assert.Single(station.Events);
            Assert.Equal(id, evt.DefinitionId);
            Assert.True(evt.ActivationStockDeltaApplied);
            Assert.Equal(definition.ItemEffects.Select(e => (e.ItemTypeId, e.ProductionMultiplierPermille, e.DemandMultiplierPermille, e.PriceMultiplierPermille, e.ActivationStockDelta)),
                evt.ItemEffects.Select(e => (e.ItemTypeId, e.ProductionMultiplierPermille, e.DemandMultiplierPermille, e.PriceMultiplierPermille, e.ActivationStockDelta)));
            // Independent table arithmetic: apply activation delta, consumption and the all-or-nothing hourly batch.
            var stock = profile.InitialInventory.ToDictionary(e => e.ItemTypeId, e => e.Quantity);
            foreach (var effect in definition.ItemEffects)
                if (effect.ActivationStockDelta != 0) stock[effect.ItemTypeId] = Math.Clamp(stock[effect.ItemTypeId] + effect.ActivationStockDelta, 0,
                    profile.Economy!.StockTargets.Single(t => t.ItemTypeId == effect.ItemTypeId).TargetStock * 2);
            long Rate(StationMarketStockDefinition rate, bool output)
            {
                var effect = definition.ItemEffects.FirstOrDefault(e => e.ItemTypeId == rate.ItemTypeId);
                int multiplier = effect is null ? 1000 : output ? effect.ProductionMultiplierPermille : effect.DemandMultiplierPermille;
                return (long)decimal.Round(rate.Quantity * multiplier / 1000m, 0, MidpointRounding.AwayFromZero);
            }
            foreach (var rate in profile.Economy!.HourlyConsumption) stock[rate.ItemTypeId] = Math.Max(0, stock[rate.ItemTypeId] - Rate(rate, false));
            bool fits = profile.Economy.HourlyInputs.All(r => stock[r.ItemTypeId] >= Rate(r, false)) &&
                profile.Economy.HourlyOutputs.All(r => stock[r.ItemTypeId] + Rate(r, true) <= 2 * profile.Economy.StockTargets.Single(t => t.ItemTypeId == r.ItemTypeId).TargetStock);
            if (fits)
            {
                foreach (var rate in profile.Economy.HourlyInputs) stock[rate.ItemTypeId] -= Rate(rate, false);
                foreach (var rate in profile.Economy.HourlyOutputs) stock[rate.ItemTypeId] += Rate(rate, true);
            }
            foreach (var (item, quantity) in stock) Assert.Equal(quantity, MarketStock(engine, registry, item));
        }
    }
    [Fact]
    public void MarketEvent_future_authored_event_cannot_push_generated_set_over_two()
    {
        var definitions = new[] { MarketEventDefinition(), MarketEventDefinition("event.decompression") };
        var (engine, _) = CreateEventEngine(definitions, adjust: s => s with
        {
            GameState = s.GameState with
            {
                SpaceObjects = s.GameState.SpaceObjects.Select(o => o.ObjectType != "Station" ? o : o with
                { Events = [new("future", "Future authored event", null, GameCalendar.HourMs + 100, GameCalendar.HourMs, [])] }).ToArray(),
            }
        });
        using (engine)
        {
            var first = engine.CaptureSnapshotForTests(GameCalendar.HourMs, simulationTimeMs: 0).DockedStationTrade!;
            Assert.Single(first.ActiveEvents);
            var overlapping = engine.CaptureSnapshotForTests(GameCalendar.HourMs + 100, simulationTimeMs: 0).DockedStationTrade!;
            Assert.Equal(2, overlapping.ActiveEvents.Length);
            Assert.Contains(overlapping.ActiveEvents, e => e.EventId == "future");
            var save = engine.CaptureSaveStateForTests(GameCalendar.HourMs + 100, SimulationSpeed.Speed0, 0);
            using var loaded = new SimulationEngine(MarketRegistry(BoundedProfile(), marketEvents: definitions));
            loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true));
        }
    }

    [Fact]
    public void MarketEvent_authored_subhour_start_and_end_invalidate_quotes_exactly_once()
    {
        var profile = BoundedProfile() with { Economy = null };
        var (engine, _) = CreateEventEngine([], profile: profile, adjust: s => s with
        {
            GameState = s.GameState with
            {
                SpaceObjects = s.GameState.SpaceObjects.Select(o => o.ObjectType != "Station" ? o : o with
                { Events = [new("timed", "Timed", null, 500, 500, [new(null, "item.ice", 2000)])] }).ToArray(),
            }
        });
        using (engine)
        {
            var quote = engine.GetTradeQuote(new TradeQuoteRequest("before-timed", "SPC-0001", "MOD-PLAYER-CARGO-01", TradeCommandTypes.Buy, "item.ice", 1));
            Assert.Null(quote.DisabledReason);
            var before = engine.CaptureSnapshotForTests(499, simulationTimeMs: 0).DockedStationTrade!;
            var active = engine.CaptureSnapshotForTests(500, simulationTimeMs: 0).DockedStationTrade!;
            Assert.Equal(before.MarketRevision + 1, active.MarketRevision);
            Assert.Equal(active.MarketRevision, engine.CaptureSnapshotForTests(999, simulationTimeMs: 0).DockedStationTrade!.MarketRevision);
            engine.ReceiveCommand(new PlayerCommand("old-timed", 1, "SPC-0001", "MOD-PLAYER-CARGO-01", TradeCommandTypes.Buy,
                ItemTypeId: "item.ice", Quantity: 1, QuoteId: quote.QuoteId, MarketRevision: quote.MarketRevision));
            Assert.Equal(CommandReasonCodes.StaleQuote, Assert.Single(engine.CaptureSnapshotForTests(999, simulationTimeMs: 0).CommandResults).ReasonCode);
            var ended = engine.CaptureSnapshotForTests(1000, simulationTimeMs: 0).DockedStationTrade!;
            Assert.Equal(active.MarketRevision + 1, ended.MarketRevision);
            Assert.Empty(ended.ActiveEvents);
        }
    }
}
