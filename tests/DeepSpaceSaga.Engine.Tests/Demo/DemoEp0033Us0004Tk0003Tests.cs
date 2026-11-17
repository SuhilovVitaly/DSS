using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0033Us0004Tk0003Tests
{
    internal static void MakeRoom(SimulationEngine engine)
    {
        DemoEp0033Us0001Tk0003Tests.Damage(engine, 5000);
        var save = engine.CaptureSaveState();
        var ship = save.GameState.SpaceObjects.Single(s => s.ObjectId == save.GameState.PlayerShipObjectId);
        string departed = ship.Crew![0].CrewId;
        var changed = ship with { Crew = ship.Crew!.Where(c => c.CrewId != departed).ToArray(),
            CarrierState = ship.CarrierState! with { OwnedDrones = ship.CarrierState!.OwnedDrones!.Select(d => d.CrewId == departed ? d with { CrewId = "" } : d).ToArray() } };
        engine.LoadScenario(save with { GameState = save.GameState with { SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId == ship.ObjectId ? changed : s).ToArray() } }, isSave: true);
    }
    [Fact]
    public void StableOffersAndPaidHireSurviveLoadWithoutDuplicateCrew()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        MakeRoom(engine);
        var before = engine.CaptureSnapshot().DemoCampaign!.Company!.Recruitment!;
        Assert.Equal(3, before.Offers.Length);
        Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(engine.CaptureSnapshot().DemoCampaign!.Company!.Recruitment));
        var offer = before.Offers[0];
        var command = new HirePilotCommand("hire", 0, offer.Id);
        var result = engine.ExecuteDemoCampaign(command);
        Assert.True(result.Applied, result.Reason);
        Assert.Equal(4700, engine.PlayerCredits);
        Assert.Equal(2, result.Recruitment!.Offers.Length);
        var saved = ScenarioLoader.Serialize(engine.CaptureSaveState());
        engine.LoadScenario(ScenarioLoader.LoadFromJson(saved, true, true), isSave: true);
        engine.ExecuteDemoCampaign(command);
        Assert.Equal(4700, engine.PlayerCredits);
        Assert.Single(engine.CaptureSnapshot().CarrierOperations!.Crew, c => c.Id == offer.CrewId);
        Assert.Equal(offer.DailyWage, engine.CaptureSaveState().GameState.DemoCampaign!.Employment.Single(e => e.CrewId == offer.CrewId).DailyWage);
        Assert.False(engine.ExecuteDemoCampaign(new HirePilotCommand("again", 1, offer.Id)).Applied);
    }
    [Fact]
    public void AssignmentIsExclusiveAndPersistsRealCrewIdentity()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        MakeRoom(engine);
        var snapshot = engine.CaptureSnapshot();
        var offer = snapshot.DemoCampaign!.Company!.Recruitment!.Offers[0];
        Assert.True(engine.ExecuteDemoCampaign(new HirePilotCommand("hire", 0, offer.Id)).Applied);
        var drones = snapshot.CarrierOperations!.OwnedDrones;
        Assert.True(engine.ExecuteDemoCampaign(new AssignPilotCommand("assign", 1, offer.CrewId, drones[0].Id)).Applied);
        Assert.True(engine.ExecuteDemoCampaign(new AssignPilotCommand("move", 2, offer.CrewId, drones[1].Id)).Applied);
        var after = engine.CaptureSnapshot().CarrierOperations!.OwnedDrones;
        Assert.Equal(drones[1].Id, Assert.Single(after, d => d.CrewId == offer.CrewId).Id);
        Assert.Equal("", after.Single(d => d.Id == drones[0].Id).CrewId);
    }
    [Fact]
    public void PassengerOccupancyAndReservePreventHiring()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        MakeRoom(engine);
        var snapshot = engine.CaptureSnapshot();
        int free = snapshot.CarrierOperations!.CabinReservation!.FreeCabins;
        var save = engine.CaptureSaveState();
        engine.LoadScenario(save with { GameState = save.GameState with { SpaceObjects = save.GameState.SpaceObjects.Select(s =>
            s.ObjectId == save.GameState.PlayerShipObjectId ? s with { Passengers = Enumerable.Range(0, free).Select(i => new ShipPassengerData("p" + i, "Passenger")).ToArray() } : s).ToArray() } }, isSave: true);
        var state = engine.CaptureSnapshot().DemoCampaign!.Company!.Recruitment!;
        Assert.Equal(0, state.FreeCabins);
        Assert.False(engine.ExecuteDemoCampaign(new HirePilotCommand("full", 0, state.Offers[0].Id)).Applied);
        Assert.Equal(5000, engine.PlayerCredits);
    }
}


