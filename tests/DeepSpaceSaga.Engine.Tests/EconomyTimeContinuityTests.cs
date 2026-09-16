using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public class EconomyTimeContinuityTests
{
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
        var economy = state.EconomyTime! with {
            RouteArrivalGameTimeMs = 20 * GameCalendar.HourMs,
            ActiveContracts = [new("contract", 11 * GameCalendar.HourMs, 750, "STATION-01", ["passenger"]) ]
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
        save = save with { SaveFormatVersion = 4, GameState = save.GameState with {
            EconomyTime = null,
            SpaceObjects = save.GameState.SpaceObjects.Select(o => o with {
                FirstPortFeeGameTimeMs = null, NextPortFeeDueGameTimeMs = null }).ToArray()
        }};
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(save));
    }

    [Fact]
    public void Missing_next_payment_is_derived_from_saved_first_payment()
    {
        using var engine = PortFeeScheduleTests.DockAt(8 * GameCalendar.HourMs + 30 * 60_000);
        var save = engine.CaptureSaveStateForTests(16 * GameCalendar.HourMs, SimulationSpeed.Speed0);
        long next = save.GameState.SpaceObjects.Single(o => o.IsDocked).NextPortFeeDueGameTimeMs!.Value;
        engine.LoadScenario(save with { GameState = save.GameState with { SpaceObjects = save.GameState.SpaceObjects
            .Select(o => o with { NextPortFeeDueGameTimeMs = null }).ToArray() } });
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
        save = save with { GameState = save.GameState with { SpaceObjects = save.GameState.SpaceObjects
            .Select(o => o.IsDocked ? o with { NextPortFeeDueGameTimeMs = next } : o).ToArray() } };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(save));
    }
}
