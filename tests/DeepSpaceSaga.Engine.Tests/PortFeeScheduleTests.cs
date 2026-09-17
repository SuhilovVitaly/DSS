using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public class PortFeeScheduleTests
{
    [Fact]
    public void Docking_dialogue_and_daily_fee_use_calendar_time_while_motion_keeps_its_pace()
    {
        long realMs = 0;
        using var engine = DockCommandTests.CreateEngine(clock:
            new SimulationClock(SimulationSpeed.Speed1, () => realMs));
        engine.SetSpeed(SimulationSpeed.Speed1);
        realMs = 1000;
        engine.ReceiveCommand(new("dock", 1, "SPC-0001", "MOD-NAV-01", NavigationComputerCommandTypes.Dock,
            TargetObjectId: "STATION-01"));
        var snapshot = engine.CaptureSnapshot(advanceClock: true);
        Assert.NotNull(snapshot.ActiveDialogue);
        Assert.Equal(300_000, snapshot.GameTimeMs);
        Assert.Equal(1000, snapshot.SimulationTimeMs);
        realMs = 31_000;
        snapshot = engine.CaptureSnapshot(advanceClock: true);
        Assert.Equal(300_000, snapshot.GameTimeMs);
        foreach (string choice in new[] { "truthful_id", "accept_fee", "continue" })
        {
            var active = snapshot.ActiveDialogue!;
            engine.ReceiveDialogueCommand(new(choice, DialogueAction.Choose, active.InstanceId, active.Revision, choice));
            snapshot = engine.CaptureSnapshot();
        }
        Assert.Equal(300_000, snapshot.PortFees!.FirstPortFeeGameTimeMs);
        Assert.Equal(300_000 + GameCalendar.DayMs, snapshot.PortFees.NextPortFeeDueGameTimeMs);
        Assert.Equal(900, snapshot.PlayerCredits);
        Assert.Equal(SimulationSpeed.Speed1, snapshot.CurrentSpeed);
        realMs += 288_000; // 24 calendar hours at five minutes per real second.
        snapshot = engine.CaptureSnapshot(advanceClock: true);
        Assert.Equal(300_000 + GameCalendar.DayMs, snapshot.GameTimeMs);
        Assert.Equal(289_000, snapshot.SimulationTimeMs);
        Assert.Equal(800, snapshot.PlayerCredits);
        Assert.Equal(800, engine.CaptureSnapshot().PlayerCredits);
    }
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
    public void Renewal_only_bills_the_controlled_ship_and_ignores_other_ship_deadlines()
    {
        long start = 8 * GameCalendar.HourMs;
        using var engine = DockAt(start);
        var save = engine.CaptureSaveState();
        var player = save.GameState.SpaceObjects.Single(o => o.ObjectId == save.GameState.PlayerShipObjectId);
        var other = player with { ObjectId = "OTHER-SHIP", Modules = [], HullLayout = null,
            Crew = [], Passengers = [], FirstPortFeeGameTimeMs = 0,
            NextPortFeeDueGameTimeMs = GameCalendar.DayMs, PortFeeDebt = 75 };
        engine.LoadScenario(save with { GameState = save.GameState with {
            SpaceObjects = save.GameState.SpaceObjects.Append(other).ToArray() } });

        // The other ship's deadline is earlier: it must neither bill the player nor
        // hold the event loop at an unprocessed deadline after it is ignored.
        var end = engine.CaptureSaveStateForTests(start + 2 * GameCalendar.DayMs, SimulationSpeed.Speed0);
        Assert.Equal(700, end.GameState.PlayerTokens);
        Assert.Equal(10300, end.GameState.SpaceObjects.Single(o => o.ObjectId == "STATION-01").Credits);
        var otherAfter = end.GameState.SpaceObjects.Single(o => o.ObjectId == other.ObjectId);
        Assert.Equal(other.PortFeeDebt, otherAfter.PortFeeDebt);
        Assert.Equal(other.NextPortFeeDueGameTimeMs, otherAfter.NextPortFeeDueGameTimeMs);

        // Ignoring another ship's timer must not make our own saves unloadable.
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(end), true));
        Assert.Equal(700, engine.CaptureSnapshot().PlayerCredits);
        Assert.Equal(start + 3 * GameCalendar.DayMs, engine.CaptureSnapshot().PortFees!.NextPortFeeDueGameTimeMs);
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
