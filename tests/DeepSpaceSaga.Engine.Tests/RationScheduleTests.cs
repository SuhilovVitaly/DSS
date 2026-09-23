using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public class RationScheduleTests
{
    internal static SimulationEngine CreateEngine(long time = 0, int passengers = 0, long rations = 200)
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));
        var engine = SimulationEngine.CreateFromScenarioFile(Path.Combine(root, "Settings.json"),
            Path.Combine(root, "Scenarios/Docked/scenario.json"));
        var save = engine.CaptureSaveState();
        engine.LoadScenario(save with { GameState = save.GameState with {
            GameTimeMs = time,
            TradingMap = null,
            SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectType != "Station" ? o : o with {
                MarketProfileId = null,
                MarketProfileFingerprint = null,
                MarketBudgetCredits = null,
                MarketRevision = null,
            }).Select(o => o.ObjectId != save.GameState.PlayerShipObjectId ? o : o with {
                Passengers = Enumerable.Range(0, passengers).Select(i => new ShipPassengerData($"P{i}", $"Passenger {i}")).ToArray(),
                Modules = o.Modules!.Select(m => m with { Cargo = m.Cargo?.Select(c => c.ItemTypeId == "item.food-rations"
                    ? c with { Quantity = rations } : c).ToArray() }).ToArray()
            }).ToArray()
        }});
        return engine;
    }

    internal static long Food(AuthoritativeSnapshot snapshot) => snapshot.InstalledModules
        .SelectMany(m => m.Cargo).Where(c => c.ItemTypeId == "item.food-rations").Sum(c => c.Quantity);

    [Fact]
    public void Noon_and_midnight_charge_everyone_exactly_once()
    {
        using var engine = CreateEngine(passengers: 2);
        Assert.Equal(200, Food(engine.CaptureSnapshotForTests(12 * GameCalendar.HourMs - 1)));
        Assert.Equal(197, Food(engine.CaptureSnapshotForTests(12 * GameCalendar.HourMs)));
        Assert.Equal(197, Food(engine.CaptureSnapshotForTests(12 * GameCalendar.HourMs)));
        Assert.Equal(194, Food(engine.CaptureSnapshotForTests(GameCalendar.DayMs)));
        Assert.Equal(194, Food(engine.CaptureSnapshotForTests(GameCalendar.DayMs)));
    }

    [Fact]
    public void Hourly_travel_crosses_meal_once_and_pause_does_not_consume()
    {
        using var engine = CreateEngine(time: 11 * GameCalendar.HourMs);
        var result = engine.TravelStation(new("travel", StationDistrict.Market));
        Assert.Equal(199, Food(result.Snapshot));
        Assert.Equal(199, Food(engine.TravelStation(new("travel", StationDistrict.Market)).Snapshot));
        Assert.Equal(199, Food(engine.CaptureSnapshot()));
    }

    [Fact]
    public void Boarding_after_meal_has_no_retroactive_charge_and_disembarked_people_are_excluded()
    {
        using var engine = CreateEngine(time: 12 * GameCalendar.HourMs + 1, passengers: 1);
        Assert.Equal(200, Food(engine.CaptureSnapshot()));
        var save = engine.CaptureSaveStateForTests(23 * GameCalendar.HourMs, SimulationSpeed.Speed0);
        engine.LoadScenario(save with { GameState = save.GameState with {
            TradingMap = null,
            SpaceObjects = save.GameState.SpaceObjects.Select(o => o with { Passengers = [] }).ToArray()
        }});
        Assert.Equal(199, Food(engine.CaptureSnapshotForTests(GameCalendar.DayMs)));
    }

    [Fact]
    public void Shortage_consumes_only_available_stock_and_emits_warning()
    {
        using var engine = CreateEngine(passengers: 2, rations: 1);
        var snapshot = engine.CaptureSnapshotForTests(12 * GameCalendar.HourMs);
        Assert.Equal(0, Food(snapshot));
        Assert.Contains(snapshot.ShipEvents, e => e.EventType == ShipEventTypes.RationsShortage);
        Assert.DoesNotContain(engine.CaptureSnapshotForTests(12 * GameCalendar.HourMs).ShipEvents,
            e => e.EventType == ShipEventTypes.RationsShortage);
    }
}
