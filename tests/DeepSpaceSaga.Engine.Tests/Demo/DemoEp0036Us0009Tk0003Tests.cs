using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0036Us0009Tk0003Tests
{
    [Fact]
    public void UnboardedObligationsReserveSameCabinsAcrossScreens()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var save = engine.CaptureSaveState();
        var campaign = save.GameState.DemoCampaign!;
        var jobs = campaign.StationJobs!;
        var offer = jobs.Offers.First(o => StationJobTypes.CarriesPassengers(o.Type));
        var obligation = new JobObligation("contract:" + offer.Id, offer.Id, offer.Type, offer.OriginStationId, offer.DestinationStationId,
            1, 0, offer.Payout, save.GameState.GameTimeMs, save.GameState.GameTimeMs + offer.DeadlineDurationMs, StationJobStatuses.Active, offer.RiskBand);
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                DemoCampaign = campaign with
                { StationJobs = jobs with { Obligations = jobs.Obligations.Add(obligation), Offers = jobs.Offers.Remove(offer) } }
            }
        }, true);
        var snapshot = engine.CaptureSnapshot();
        var cabin = snapshot.CarrierOperations!.CabinReservation!;
        var capacity = snapshot.DemoCampaign!.Transport!.PassengerCapacity!;
        Assert.Equal(1, cabin.ReservedCabins);
        Assert.Equal(cabin.TotalCabins - cabin.OccupiedCabins - 1, cabin.FreeCabins);
        Assert.Equal(cabin.FreeCabins, capacity.FreeCabins);
        Assert.Equal(cabin.FreeCabins, snapshot.DemoCampaign.Company!.Recruitment!.FreeCabins);
    }

    internal static SimulationEngine Captured()
    {
        var engine = DemoEp0036Us0007Tk0003Tests.Won();
        var state = engine.CaptureSnapshot().DemoCampaign!;
        var loot = state.Rewards!.Loot!;
        Assert.True(engine.ExecuteDemoCampaign(new ArrestPilotCommand("arrest", state.Revision, loot.BattleId,
            loot.Objects.First(s => s.Pilot is not null).ObjectId)).Applied);
        Assert.True(engine.ExecuteDemoCampaign(new FinishBattleLootCommand("finish", state.Revision + 1, loot.BattleId, true)).Applied);
        return engine;
    }
    [Fact]
    public void AllPeople_ShareCabinsWithoutReserve()
    {
        using var engine = Captured();
        var snapshot = engine.CaptureSnapshot();
        var capacity = snapshot.DemoCampaign!.Transport!.PassengerCapacity!;
        var cabin = snapshot.CarrierOperations!.CabinReservation!;
        Assert.Equal(0, cabin.ReservedCabins);
        Assert.Equal(1, cabin.PrisonerCount);
        Assert.Equal(1, capacity.PrisonerCabins);
        Assert.Equal(capacity.CrewCabins + capacity.PassengerCabins + 1, cabin.OccupiedCabins);
        Assert.Equal(capacity.TotalCabins - cabin.OccupiedCabins, capacity.FreeCabins);
        Assert.Equal(capacity.FreeCabins, snapshot.DemoCampaign.Company!.Recruitment!.FreeCabins);
        Assert.Equal(cabin.OccupiedCabins, snapshot.DemoCampaign.Company.Upkeep!.PeopleFed);
    }
    [Fact]
    public void RationsAndWages_SlicingAndLoadAreStable()
    {
        using var engine = Captured();
        var seed = engine.CaptureSaveState();
        using var sliced = DemoEp0033Us0001Tk0003Tests.Demo();
        sliced.LoadScenario(seed, true);
        int crew = engine.CaptureSnapshot().CarrierOperations!.Crew.Length;
        long food = RationScheduleTests.Food(engine.CaptureSnapshot());
        long now = seed.GameState.GameTimeMs;
        long day = (now / GameCalendar.DayMs + 1) * GameCalendar.DayMs;
        var whole = engine.CaptureSaveStateForTests(day, SimulationSpeed.Speed0, simulationTimeMs: seed.GameState.SimulationTimeMs);
        long half = day - GameCalendar.DayMs / 2;
        var halfSave = half > now ? sliced.CaptureSaveStateForTests(half, SimulationSpeed.Speed0, simulationTimeMs: seed.GameState.SimulationTimeMs) : seed;
        sliced.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(halfSave), true, true), true);
        var split = sliced.CaptureSaveStateForTests(day, SimulationSpeed.Speed0, simulationTimeMs: seed.GameState.SimulationTimeMs);
        Assert.Equal(whole.GameState.DemoCampaign!.Company!.Upkeep, split.GameState.DemoCampaign!.Company!.Upkeep);
        Assert.Equal(RationScheduleTests.Food(engine.CaptureSnapshot()), RationScheduleTests.Food(sliced.CaptureSnapshot()));
        Assert.Equal(crew * 20, whole.GameState.DemoCampaign.Company!.Upkeep!.Accrued);
        Assert.DoesNotContain(whole.GameState.DemoCampaign.Employment, e => e.CrewId.StartsWith("prisoner:", StringComparison.Ordinal));
        long after = RationScheduleTests.Food(sliced.CaptureSnapshot());
        Assert.True(after < food);
        sliced.CaptureSaveStateForTests(day, SimulationSpeed.Speed0, simulationTimeMs: seed.GameState.SimulationTimeMs);
        Assert.Equal(after, RationScheduleTests.Food(sliced.CaptureSnapshot()));
    }
}
