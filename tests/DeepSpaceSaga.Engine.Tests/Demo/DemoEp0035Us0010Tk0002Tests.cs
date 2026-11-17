using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0035Us0010Tk0002Tests
{
    [Theory]
    [InlineData(1000, 0, -10, 0, 320)]
    [InlineData(-1000, 200, 10, 0, 400)]
    [InlineData(200, 0, 10, 0, 0)]
    public void EtaIsEarliestCircleEntryInRealSecondsAt1X(double x, double y, double vx, double vy, double expected) =>
        Assert.Equal(expected, SimulationEngine.SensorEtaRealSeconds(x, y, vx, vy)!.Value, 6);

    [Fact]
    public void NoInterceptIsNotZeroEta()
    {
        Assert.Null(SimulationEngine.SensorEtaRealSeconds(1000, 0, 0, 0));
        Assert.Null(SimulationEngine.SensorEtaRealSeconds(1000, 0, 10, 0));
        Assert.Null(SimulationEngine.SensorEtaRealSeconds(-1000, 201, 10, 0));
    }

    internal static ScenarioFile ContactScenario(SimulationEngine engine, bool script = false)
    {
        var save = engine.CaptureSaveState();
        var group = save.GameState.DemoCampaign!.PirateWorld!.Groups[0];
        return save with { GameState = save.GameState with
        {
            SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId != save.GameState.PlayerShipObjectId ? s : s with
            {
                PositionX = group.X, PositionY = group.Y + 1500, SpeedMps = 0, Orbit = null, IsDocked = false, DockedStationObjectId = null,
                Modules = s.Modules!.Select(m => script && m.ModuleTypeId == "module.sensor.player-start" ? m with { OperatorCrewId = null } : m).ToArray()
            }).ToArray()
        } };
    }

    [Fact]
    public void AcquisitionPersistsFrozenReportAndEstimateWithoutRepeatOrReroll()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        engine.LoadScenario(ContactScenario(engine), true);
        var snapshot = engine.CaptureSnapshot();
        var batch = Assert.IsType<SensorContactReportBatch>(snapshot.DemoCampaign!.PendingSensorReport);
        var report = batch.Reports[0];
        Assert.Equal("CHR-PLAYER-DEFENSE", report.OperatorCrewId);
        Assert.Equal(0, report.BearingDegrees);
        Assert.Equal(1500, report.RangeWorldUnits);
        Assert.Null(report.EtaRealSecondsAt1X);
        var save = engine.CaptureSaveState();
        int total = save.GameState.DemoCampaign!.PirateCompositions.Where(c => c.GroupId == report.GroupId).Sum(c => c.Drones.Length);
        Assert.InRange(report.ApproximateDroneCount, total - report.SignatureTypeIds.Length, total + report.SignatureTypeIds.Length);
        string frozen = JsonSerializer.Serialize(batch);
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true), true);
        Assert.Equal(frozen, JsonSerializer.Serialize(engine.CaptureSnapshot().DemoCampaign!.PendingSensorReport));
        Assert.Equal(frozen, JsonSerializer.Serialize(engine.CaptureSnapshot().DemoCampaign!.PendingSensorReport));
    }
}
