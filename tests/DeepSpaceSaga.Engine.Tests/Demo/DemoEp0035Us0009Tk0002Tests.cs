using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0035Us0009Tk0002Tests
{
    internal static ScenarioFile Assign(ScenarioFile save, string? crewId) => save with
    {
        GameState = save.GameState with
        {
            SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId != save.GameState.PlayerShipObjectId ? s : s with
            {
                Modules = s.Modules!.Select(m => m.ModuleId == "MOD-PLAYER-SENSOR-01"
                    ? m with { OperatorCrewId = crewId } : m).ToArray()
            }).ToArray()
        }
    };

    [Fact]
    public void SaveLoad_PreservesAssignment_WithoutDuplicatingCrew()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var before = engine.CaptureSnapshot().CarrierOperations!;
        var assigned = Assign(engine.CaptureSaveState(), "CHR-PLAYER-DEFENSE");
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(assigned), true, true), true);
        var snapshot = engine.CaptureSnapshot();
        Assert.Equal("CHR-PLAYER-DEFENSE", snapshot.DemoCampaign!.SensorOperator!.CrewId);
        Assert.Null(snapshot.DemoCampaign.SensorOperator.AvailabilityReason);
        Assert.Equal(before.Crew.Length, snapshot.CarrierOperations!.Crew.Length);
        Assert.Equal(before.CabinReservation, snapshot.CarrierOperations.CabinReservation);
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()), true, true), true);
        Assert.Equal(snapshot.DemoCampaign.SensorOperator, engine.CaptureSnapshot().DemoCampaign!.SensorOperator);
    }

    [Fact]
    public void InvalidReferences_RejectBeforePublish()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var invalid = Assign(engine.CaptureSaveState(), "missing-crew");
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(invalid), true, true));
    }

    [Fact]
    public void MissingAssignment_IsExplicit()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        engine.LoadScenario(Assign(engine.CaptureSaveState(), null), true);
        Assert.Equal(SensorOperatorReasons.Unassigned, engine.CaptureSnapshot().DemoCampaign!.SensorOperator!.AvailabilityReason);
    }
}
