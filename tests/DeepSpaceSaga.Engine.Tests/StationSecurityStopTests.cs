using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;

namespace DeepSpaceSaga.Engine.Tests;

// Review findings #2 and #6: a station-security stop that fires inside the late
// UpdateStationSecurity pass (after AdvanceWorldTo) must stop the save and the snapshot too.
public sealed class StationSecurityStopTests
{
    private const string Ship = "SPC-0001";

    // A zero grace period arms the incident with its deadline at the choice time, so the
    // ship is destroyed by the UpdateStationSecurity call that follows the dialogue commands.
    private static SimulationEngine CreateZeroGraceEngine(SimulationClock? clock = null)
    {
        var engine = DockCommandTests.CreateEngine(clock: clock);
        var save = engine.CaptureSaveState();
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o =>
                    o.ObjectId == "STATION-01" ? o with { PiracyWarningGracePeriodMs = 0 } : o).ToArray()
            }
        });
        return engine;
    }

    // Dock request, false ID and acknowledgement are all handled by one world pass.
    private static void QueueFalseIdDocking(SimulationEngine engine)
    {
        engine.ReceiveCommand(new("dock", 1, Ship, "MOD-NAV-01", NavigationComputerCommandTypes.Dock, TargetObjectId: "STATION-01"));
        engine.ReceiveDialogueCommand(new("false-id", DialogueAction.Choose, "dialogue-1", 0, "false_id"));
        engine.ReceiveDialogueCommand(new("continue", DialogueAction.Choose, "dialogue-1", 1, "continue"));
    }

    [Fact]
    public void Save_that_triggers_the_station_security_stop_is_refused()
    {
        using var engine = CreateZeroGraceEngine();
        QueueFalseIdDocking(engine);

        var error = Assert.Throws<InvalidOperationException>(() => engine.CaptureSaveState());

        Assert.Equal("player_destroyed", error.Message);
        Assert.True(Assert.Single(engine.CaptureSnapshot().Objects, o => o.ObjectId == Ship).IsDestroyed);
        Assert.Equal("player_destroyed", Assert.Throws<InvalidOperationException>(() => engine.CaptureSaveState()).Message);
    }

    [Fact]
    public async Task Quicksave_that_triggers_the_station_security_stop_writes_no_file()
    {
        string directory = Path.Combine(Path.GetTempPath(), "security-stop-save-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var engine = CreateZeroGraceEngine();
            await using var connection = new LocalGameSessionConnection(engine, directory);
            QueueFalseIdDocking(engine);

            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.SaveAsync("quicksave").AsTask());

            Assert.Equal("player_destroyed", error.Message);
            Assert.False(Directory.Exists(directory));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void Snapshot_that_stops_inside_station_security_publishes_the_stop_clock()
    {
        long realTimeMs = 0;
        using var engine = CreateZeroGraceEngine(new SimulationClock(SimulationSpeed.Speed0, () => realTimeMs));
        engine.SetSpeed(SimulationSpeed.Speed2);
        realTimeMs += 1000;
        QueueFalseIdDocking(engine);

        var stopped = engine.CaptureSnapshot(advanceClock: true);
        realTimeMs += 1000;
        var next = engine.CaptureSnapshot(advanceClock: true);

        var stoppedShip = Assert.Single(stopped.Objects, o => o.ObjectId == Ship);
        var nextShip = Assert.Single(next.Objects, o => o.ObjectId == Ship);
        Assert.True(stoppedShip.IsDestroyed);
        Assert.True(stopped.MotionTimeMs > 0);
        Assert.Equal(SimulationSpeed.Speed0, stopped.CurrentSpeed);
        Assert.Equal(
            (next.GameTimeMs, next.MotionTimeMs, next.CurrentSpeed),
            (stopped.GameTimeMs, stopped.MotionTimeMs, stopped.CurrentSpeed));
        Assert.Equal((nextShip.X, nextShip.Y, nextShip.SpeedKmS), (stoppedShip.X, stoppedShip.Y, stoppedShip.SpeedKmS));
    }
}
