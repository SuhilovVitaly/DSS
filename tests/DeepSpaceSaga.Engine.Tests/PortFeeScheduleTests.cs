using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public class PortFeeScheduleTests
{
    internal static SimulationEngine DockAt(long time, long credits = 1000)
    {
        var engine = DockCommandTests.CreateEngine(playerCredits: credits);
        var save = engine.CaptureSaveState();
        engine.LoadScenario(save with { GameState = save.GameState with { GameTimeMs = time } });
        engine.ReceiveCommand(new("dock", 1, "SPC-0001", "MOD-NAV-01", NavigationComputerCommandTypes.Dock,
            TargetObjectId: "STATION-01"));
        engine.CaptureSnapshot();
        DialogueTests.Choose(engine, "truthful_id", time: time);
        DialogueTests.Choose(engine, "accept_fee", time: time);
        DialogueTests.Choose(engine, "continue", time: time);
        return engine;
    }

    [Fact]
    public void Renewal_is_24_hours_from_first_payment_not_midnight()
    {
        long start = 2 * GameCalendar.DayMs + 8 * GameCalendar.HourMs + 30 * 60_000;
        using var engine = DockAt(start);
        var snapshot = engine.CaptureSnapshot();
        Assert.Equal(900, snapshot.PlayerCredits);
        Assert.Equal(start, snapshot.PortFees!.FirstPortFeeGameTimeMs);
        Assert.Equal(start + GameCalendar.DayMs, snapshot.PortFees.NextPortFeeDueGameTimeMs);
        Assert.Equal(900, engine.CaptureSnapshotForTests(3 * GameCalendar.DayMs).PlayerCredits);
        Assert.Equal(900, engine.CaptureSnapshotForTests(start + GameCalendar.DayMs - 1).PlayerCredits);
        Assert.Equal(800, engine.CaptureSnapshotForTests(start + GameCalendar.DayMs).PlayerCredits);
        Assert.Equal(800, engine.CaptureSnapshotForTests(start + GameCalendar.DayMs).PlayerCredits);
        Assert.Equal(600, engine.CaptureSnapshotForTests(start + 3 * GameCalendar.DayMs).PlayerCredits);
    }

    [Fact]
    public void Insufficient_initial_fee_creates_debt_and_only_transfers_existing_balance()
    {
        using var engine = DockAt(0, credits: 25);
        var snapshot = engine.CaptureSnapshot();
        Assert.Equal(0, snapshot.PlayerCredits);
        Assert.Equal(75, snapshot.PortFees!.Debt);
        Assert.Equal(10025, engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "STATION-01").Credits);
        snapshot = engine.CaptureSnapshotForTests(GameCalendar.DayMs);
        Assert.Equal(175, snapshot.PortFees!.Debt);
        Assert.Equal(0, snapshot.PlayerCredits);
    }

    [Fact]
    public void Hourly_travel_only_charges_when_crossing_due_time_and_repeat_dock_is_free()
    {
        using var engine = DockAt(0);
        engine.ReceiveCommand(new("dock-again", 2, "SPC-0001", "MOD-NAV-01", NavigationComputerCommandTypes.Dock,
            TargetObjectId: "STATION-01"));
        Assert.Equal(900, engine.CaptureSnapshot().PlayerCredits);
        for (int hour = 1; hour <= 24; hour++)
        {
            var result = engine.TravelStation(new($"hour-{hour}", hour % 2 == 1 ? StationDistrict.Market : StationDistrict.Dock));
            Assert.True(result.Accepted);
            Assert.Equal(hour == 24 ? 800 : 900, result.Snapshot.PlayerCredits);
        }
        Assert.Equal(800, engine.CaptureSnapshot().PlayerCredits);
    }

    [Fact]
    public void Save_retains_first_payment_and_due_time()
    {
        using var engine = DockAt(8 * GameCalendar.HourMs + 30 * 60_000);
        var before = engine.CaptureSnapshot();
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true);
        using var loaded = new SimulationEngine(DockCommandTests.CreateRegistry(200));
        loaded.LoadScenario(save);
        Assert.Equal(before.PortFees, loaded.CaptureSnapshot().PortFees);
        Assert.Equal(800, loaded.CaptureSnapshotForTests(before.PortFees!.NextPortFeeDueGameTimeMs).PlayerCredits);
    }
}
