using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class Ep0035Us17Tk2Tests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RepeatedPausedReportSnapshotPreservesFractionalMotion(int remainder)
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        engine.LoadScenario(DemoEp0035Us0010Tk0002Tests.ContactScenario(engine), true);
        Assert.NotNull(engine.CaptureSnapshot().DemoCampaign!.PendingSensorReport);
        var save = engine.CaptureSaveState();
        engine.LoadScenario(save with { GameState = save.GameState with { MotionQuarterRemainder = remainder } }, true);
        engine.CaptureSnapshot();
        Assert.Equal(remainder, engine.CaptureSaveState().GameState.MotionQuarterRemainder);
        engine.CaptureSnapshot();
        Assert.Equal(remainder, engine.CaptureSaveState().GameState.MotionQuarterRemainder);
    }

    [Fact]
    public void FastStepStopsAtSweptFirstDiscoveryAndSaveCannotAdvancePastPause()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var save = DemoEp0035Us0010Tk0002Tests.ContactScenario(engine);
        var group = save.GameState.DemoCampaign!.PirateWorld!.Groups[0];
        save = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(s =>
            s.ObjectId == save.GameState.PlayerShipObjectId ? s with { PositionY = group.Y + 2800, SpeedMps = 100000, DirectionDegrees = 0 } : s).ToArray()
            }
        };
        engine.LoadScenario(save, true);
        var snapshot = engine.CaptureSnapshotForTests(1200000, SimulationSpeed.Speed1, 4000);
        Assert.NotNull(snapshot.DemoCampaign!.PendingSensorReport);
        Assert.Equal(SimulationSpeed.Speed0, snapshot.CurrentSpeed);
        var frozen = engine.CaptureSaveState();
        Assert.InRange(frozen.GameState.MotionTimeMs, 1, 3999);
        Assert.Equal(frozen.GameState.MotionTimeMs * 300, frozen.GameState.GameTimeMs);
        var repeated = engine.CaptureSaveStateForTests(2400000, SimulationSpeed.Speed1, 8000);
        Assert.Equal(frozen.GameState.MotionTimeMs, repeated.GameState.MotionTimeMs);
        Assert.Equal(JsonSerializer.Serialize(frozen.GameState.DemoCampaign!.PirateWorld), JsonSerializer.Serialize(repeated.GameState.DemoCampaign!.PirateWorld));
        Assert.Equal(JsonSerializer.Serialize(frozen.GameState.SpaceObjects), JsonSerializer.Serialize(repeated.GameState.SpaceObjects));
    }

    [Fact]
    public void AlreadyInsideCaptureCommitsEncounterBeforeReportCanBeDismissed()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var save = DemoEp0035Us0010Tk0002Tests.ContactScenario(engine);
        var group = save.GameState.DemoCampaign!.PirateWorld!.Groups[0];
        save = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(s =>
            s.ObjectId == save.GameState.PlayerShipObjectId ? s with { PositionY = group.Y + 150 } : s).ToArray()
            }
        };
        engine.LoadScenario(save, true);
        var snapshot = engine.CaptureSnapshotForTests(300000, SimulationSpeed.Speed1, 1000);
        Assert.Equal(group.GroupId, snapshot.DemoCampaign!.WorldEncounter!.Binding.GroupId);
        Assert.NotNull(snapshot.DemoCampaign.PendingSensorReport);
        Assert.Equal(0, engine.CaptureSaveState().GameState.MotionTimeMs);
        Assert.Equal("encounter_captured", engine.TrySetSpeed(SimulationSpeed.Speed1));
    }
}
