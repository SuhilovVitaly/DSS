using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.OrbitalSynchronizationTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class OrbitalDockingDepartureTests
{
    internal static long ApproachAndSynchronize(SimulationEngine engine)
    {
        Send(engine, NavigationComputerCommandTypes.Approach, "approach");
        At(engine, 0);
        long time;
        for (time = 1000; time <= 10 * GameCalendar.DayMs / 300; time += 1000)
            if (Distance(At(engine, time)) < 100) break;
        Assert.True(time <= 10 * GameCalendar.DayMs / 300);
        foreach (string type in new[] { ShipEngineCommandTypes.SpeedSynchronization, ShipEngineCommandTypes.DirectionSynchronization })
        {
            Send(engine, type, type);
            At(engine, time);
            var cycle = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == Ship).Modules.Single(m => m.ModuleId == EngineId).ActiveCycle!;
            time += Math.Max(1, cycle.DurationMs);
            At(engine, time);
        }
        return time;
    }

    internal static AuthoritativeSnapshot Dock(SimulationEngine engine, long time)
    {
        engine.ReceiveCommand(new("dock", 2, Ship, "MOD-PLAYER-BRIDGE-01", NavigationComputerCommandTypes.Dock, TargetObjectId: Station));
        var snapshot = At(engine, time);
        Assert.True(snapshot.ActiveDialogue is not null, string.Join("; ", snapshot.CommandResults));
        foreach (string choice in new[] { "truthful_id", "accept_fee", "continue" })
            snapshot = Choose(engine, time, choice);
        Assert.True(Player(snapshot).IsDocked);
        return snapshot;
    }

    internal static AuthoritativeSnapshot Choose(SimulationEngine engine, long time, string choice)
    {
        var dialogue = At(engine, time).ActiveDialogue!;
        engine.ReceiveDialogueCommand(new(choice, DialogueAction.Choose, dialogue.InstanceId, dialogue.Revision, choice));
        var result = At(engine, time);
        engine.ReceiveDialogueCommand(new(choice, DialogueAction.Choose, dialogue.InstanceId, dialogue.Revision, choice));
        var replay = At(engine, time);
        Assert.Equal(result.PlayerCredits, replay.PlayerCredits);
        Assert.Equal(Player(result).IsDocked, Player(replay).IsDocked);
        return replay;
    }

    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    [InlineData(42UL)]
    public void DialogueDockAndUndockOrbitContinuity(ulong seed)
    {
        using var engine = Create(seed);
        long time = ApproachAndSynchronize(engine);
        var docked = Dock(engine, time);
        Assert.Equal(Target(docked).X + 1, Player(docked).X, 6);
        Assert.Equal(Target(docked).Y + 1, Player(docked).Y, 6);
        Assert.Equal(Target(docked).Orbit, Player(docked).Orbit);
        time += 50000;
        var before = At(engine, time);
        engine.ReceiveCommand(new("undock", 3, Ship, "MOD-PLAYER-BRIDGE-01", NavigationComputerCommandTypes.Undock,
            TargetObjectId: before.Voyage!.RouteOptions.First(o => o.IsAvailable).DestinationStationObjectId));
        var released = At(engine, time);
        Assert.True(!Player(released).IsDocked, string.Join("; ", released.CommandResults));
        Assert.Null(Player(released).Orbit);
        Assert.Equal(Player(before).X, Player(released).X);
        Assert.Equal(Player(before).Y, Player(released).Y);
        Assert.Equal(Player(before).SpeedKmS, Player(released).SpeedKmS);
        Assert.Equal(Player(before).Direction, Player(released).Direction);
        var free = At(engine, time + 1000);
        var baseline = Player(released);
        Assert.Equal(baseline.X + Math.Sin(baseline.Direction * Math.PI / 180) * baseline.SpeedKmS * 10, Player(free).X, 6);
        Assert.Equal(baseline.Y - Math.Cos(baseline.Direction * Math.PI / 180) * baseline.SpeedKmS * 10, Player(free).Y, 6);
    }

    [Fact]
    public void DockedPauseTravelResume()
    {
        using var engine = Create(42);
        long time = ApproachAndSynchronize(engine);
        var docked = Dock(engine, time);
        var paused = engine.CaptureSnapshot();
        Assert.Equal(Player(docked).X, Player(paused).X);
        var travel = engine.TravelStation(new("market", StationDistrict.Market));
        Assert.Equal(paused.SimulationTimeMs + GameCalendar.HourMs / 300, travel.Snapshot.SimulationTimeMs);
        Assert.Equal(paused.GameTimeMs + GameCalendar.HourMs, travel.Snapshot.GameTimeMs);
        Assert.Equal(Target(travel.Snapshot).X + 1, Player(travel.Snapshot).X, 6);
        Assert.Equal(Target(travel.Snapshot).Y + 1, Player(travel.Snapshot).Y, 6);
        var resumed = engine.CaptureSnapshotForTests(travel.Snapshot.GameTimeMs + 300000, SimulationSpeed.Speed4, travel.Snapshot.SimulationTimeMs + 1000);
        Assert.NotEqual(Player(paused).X, Player(resumed).X);
        Assert.Equal(Target(resumed).X + 1, Player(resumed).X, 6);
    }

    [Fact]
    public void AbortedVisitDoesNotCharge()
    {
        using var engine = Create(2);
        long time = ApproachAndSynchronize(engine);
        long credits = At(engine, time).PlayerCredits;
        engine.ReceiveCommand(new("abort-dock", 2, Ship, "MOD-PLAYER-BRIDGE-01", NavigationComputerCommandTypes.Dock, TargetObjectId: Station));
        var active = At(engine, time).ActiveDialogue!;
        engine.ReceiveDialogueCommand(new("abort", DialogueAction.Abort, active.InstanceId, active.Revision));
        var cancelled = At(engine, time);
        Assert.Null(cancelled.ActiveDialogue);
        Assert.False(Player(cancelled).IsDocked);
        Assert.Equal(credits, cancelled.PlayerCredits);
    }
    [Fact]
    public void DockHeadingWrapAndValidationRace()
    {
        using var wrap = DockCommandTests.CreateEngine();
        var save = wrap.CaptureSaveState();
        wrap.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o =>
            o with { DirectionDegrees = o.ObjectType == "Station" ? 0.0000002 : 359.9999998 }).ToArray()
            }
        });
        wrap.ReceiveCommand(new("seam", 1, Ship, "MOD-NAV-01", NavigationComputerCommandTypes.Dock, TargetObjectId: "STATION-01"));
        Assert.NotNull(wrap.CaptureSnapshot().ActiveDialogue);

        using var engine = Create(42);
        long time = ApproachAndSynchronize(engine);
        engine.ReceiveCommand(new("dock", 2, Ship, "MOD-PLAYER-BRIDGE-01", NavigationComputerCommandTypes.Dock, TargetObjectId: Station));
        At(engine, time);
        var fee = Choose(engine, time, "truthful_id");
        save = engine.CaptureSaveState();
        // Negative restored-dialogue context: a changed heading must fail the effect
        // transaction and roll back the preceding fee transfer.
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o =>
            o.ObjectId == Ship ? o with { DirectionDegrees = (o.DirectionDegrees + 1) % 360 } : o).ToArray()
            }
        }, isSave: true);
        long stationCredits = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == Station).Credits;
        var rejected = Choose(engine, time, "accept_fee");
        Assert.False(Player(rejected).IsDocked);
        Assert.Equal(fee.PlayerCredits, rejected.PlayerCredits);
        Assert.Equal(stationCredits, engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == Station).Credits);
        Assert.Contains(rejected.DialogueEvents, e => e.EventCode == CommandReasonCodes.DockNotSynchronized);
    }
}



