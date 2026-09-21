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
                DialogueState = null,
                SpaceObjects = save.GameState.SpaceObjects.Where(o => o.ObjectType is "PlayerShip" or "Station").Select(o => o with
                {
                    Modules = o.ObjectType == "PlayerShip"
                        ? [new("cargo", "module.test-cargo", [new(4, 2)], 100, "On", "Ready", null,
                        rations > 0 ? [new("item.food-rations", rations)] : [])] : [],
                    Credits = o.ObjectType == "Station" ? 10_000 : null,
                    PriceCoefficient = o.ObjectType == "Station" ? 1000 : null,
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
}
