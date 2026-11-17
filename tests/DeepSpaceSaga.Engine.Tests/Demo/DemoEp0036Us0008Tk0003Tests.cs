using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0036Us0008Tk0003Tests
{
    [Fact]
    public void EngineerProjection_UsesSameIdentityAndOrdinaryPeopleAccounting()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        engine.LoadScenario(DemoEp0036Us0008Tk0002Tests.WithEngineer(engine.CaptureSaveState()), true);
        var before = engine.CaptureSnapshot();
        engine.LoadScenario(DemoEp0036Us0008Tk0002Tests.WithEngineer(engine.CaptureSaveState(),
            new ShipCrewMemberData("engineer", "Alex", Gender: "Male", Role: "Engineer", SalvageBonusPercentagePoints: 14)), true);
        var snapshot = engine.CaptureSnapshot();
        var member = Assert.Single(snapshot.CarrierOperations!.Crew.Where(c => c.Role == CrewRole.Engineer));
        Assert.Equal("engineer", member.Id);
        Assert.Equal(14, member.SalvageBonusPercentagePoints);
        Assert.Equal(0.14, engine.GetAssignedEngineerBonusFraction());
        Assert.Equal(before.CarrierOperations!.CabinReservation!.OccupiedCabins + 1,
            snapshot.CarrierOperations.CabinReservation!.OccupiedCabins);
        Assert.Equal(before.DemoCampaign!.Company!.Upkeep!.PeopleFed + 1, snapshot.DemoCampaign!.Company!.Upkeep!.PeopleFed);
        Assert.Single(engine.CaptureSaveState().GameState.DemoCampaign!.Employment.Where(e => e.CrewId == "engineer"));
    }
}
