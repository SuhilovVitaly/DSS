using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class Ep0035Us5Tk2Tests
{
    internal static SimulationEngine Docked()
    {
        var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var save = engine.CaptureSaveState();
        var station = engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == "SPC-0002");
        save = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(s =>
            s.ObjectId == save.GameState.PlayerShipObjectId ? s with
            {
                PositionX = station.X,
                PositionY = station.Y,
                SpeedMps = 0,
                IsDocked = true,
                DockedStationObjectId = station.ObjectId,
                FirstPortFeeGameTimeMs = 0,
                NextPortFeeDueGameTimeMs = GameCalendar.DayMs
            } : s).ToArray()
            }
        };
        engine.LoadScenario(save, true);
        return engine;
    }

    [Fact]
    public void DetourAvoidsObservedSensorCorridorWithoutReadingHiddenWorld()
    {
        ThreatRouteWaypoint start = new(-10000, 0), end = new(10000, 0);
        ThreatRouteWaypoint[] hazards = [new(0, 0)];
        var detour = SimulationEngine.ChooseThreatDetour(start, end, hazards);
        Assert.Equal(4000, SimulationEngine.ThreatExposure(start, [end], hazards), 6);
        Assert.Equal(0, SimulationEngine.ThreatExposure(start, detour, hazards));
        Assert.True(SimulationEngine.ThreatPathLength(start, detour) > 20000);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectionReservesOnceOnUndockAndRestoresRealNavigation(bool detour)
    {
        using var engine = Docked();
        var state = engine.CaptureSnapshot().DemoCampaign!;
        var board = state.Transport!.StationJobs!;
        var offer = board.Offers.First(o => o.OriginStationId == "SPC-0002" && StationJobTypes.CarriesPassengers(o.Type) &&
            StationJobReasons.AcceptBlocker(board, o.Id) is null);
        Assert.True(engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("passengers", state.Revision, offer.Id)).Applied);
        state = engine.CaptureSnapshot().DemoCampaign!;
        var contract = Assert.Single(state.Transport!.PassengerContracts!.Active);
        Assert.True(state.Transport.CargoContracts!.PlayerCargoMassKg > 0);
        var routes = state.ThreatRoutes!;
        var option = routes.Options.First(o => o.IsDetour == detour && o.DestinationStationId == offer.DestinationStationId && o.UnavailableReason is null);
        Assert.Equal(contract.DeadlineGameTimeMs - option.EtaGameTimeMs, option.DeadlineDeltaGameTimeMs);
        long credits = engine.PlayerCredits;
        var before = engine.CaptureSaveState().GameState.SpaceObjects.Single(s => s.ObjectId == "SPC-0001").Modules;
        var command = new SelectDemoRouteCommand("select", state.Revision, option.RouteId, routes.Revision);
        var selected = engine.ExecuteDemoCampaign(command);
        Assert.True(selected.Applied, selected.Reason);
        Assert.Equal(selected, engine.ExecuteDemoCampaign(command));
        Assert.Equal(credits, engine.PlayerCredits);
        Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(engine.CaptureSaveState().GameState.SpaceObjects.Single(s => s.ObjectId == "SPC-0001").Modules));
        var stale = engine.ExecuteDemoCampaign(new SelectDemoRouteCommand("stale", state.Revision, option.RouteId, routes.Revision));
        Assert.Equal(DemoCampaignReasons.StaleRevision, stale.Reason);
        var save = engine.CaptureSaveState();
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true), true);
        Assert.Equal(option.RouteId, engine.CaptureSnapshot().DemoCampaign!.ThreatRoutes!.SelectedRouteId);
        engine.ReceiveCommand(new("undock-route", 1, "SPC-0001", "MOD-PLAYER-BRIDGE-01", NavigationComputerCommandTypes.Undock, option.DestinationStationId));
        var departed = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        Assert.Equal(CommandResultStatus.Executed, departed.CommandResults.Single(r => r.CommandId == "undock-route").Status);
        Assert.Equal(option.FuelRequiredKg, departed.Voyage!.ReservedFuelKg);
        Assert.Equal(option.EtaGameTimeMs, engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0).GameState.VoyageState!.ArrivalGameTimeMs);
        engine.CaptureSnapshotForTests(9000000, SimulationSpeed.Speed1, 30000);
        var flying = engine.CaptureSaveStateForTests(9000000, SimulationSpeed.Speed0, 30000);
        Assert.Equal(option.RouteId, flying.GameState.VoyageState!.ThreatRoute!.Option.RouteId);
        Assert.True(flying.GameState.VoyageState.ThreatRoute.NavigationStarted);
        var ship = flying.GameState.SpaceObjects.Single(s => s.ObjectId == "SPC-0001");
        Assert.True(ship.SpeedMps > 0);
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(flying), true, true), true);
        Assert.Equal(JsonSerializer.Serialize(flying.GameState.VoyageState), JsonSerializer.Serialize(engine.CaptureSaveState().GameState.VoyageState));
        engine.ReceiveCommand(new("manual", 2, "SPC-0001", "MOD-PLAYER-ENGINE-01", ShipEngineCommandTypes.MaintainCourse));
        engine.CaptureSnapshotForTests(9000000, SimulationSpeed.Speed0, 30000);
        var manual = engine.CaptureSnapshotForTests(9300000, SimulationSpeed.Speed0, 31000);
        Assert.Equal(CommandResultStatus.Executed, manual.CommandResults.Single(r => r.CommandId == "manual").Status);
        var cancelled = engine.CaptureSaveStateForTests(9300000, SimulationSpeed.Speed0, 31000).GameState.VoyageState!.ThreatRoute!;
        Assert.True(cancelled.Suspended);
        Assert.Equal(flying.GameState.VoyageState.ThreatRoute.NextWaypoint, cancelled.NextWaypoint);
    }

    [Fact]
    public void ChangedRouteTermsRejectOldRouteRevisionEvenWithCurrentCampaignRevision()
    {
        using var engine = Docked();
        var first = engine.CaptureSnapshot().DemoCampaign!;
        var option = first.ThreatRoutes!.Options.First(o => o.UnavailableReason is null);
        var next = engine.CaptureSnapshotForTests(300, SimulationSpeed.Speed0, 1).DemoCampaign!;
        Assert.NotEqual(first.ThreatRoutes.Revision, next.ThreatRoutes!.Revision);
        var rejected = engine.ExecuteDemoCampaign(new SelectDemoRouteCommand("old-terms", next.Revision, option.RouteId, first.ThreatRoutes.Revision));
        Assert.Equal(DemoCampaignReasons.StaleRevision, rejected.Reason);
        Assert.Null(engine.CaptureSaveState().GameState.VoyageState!.ThreatRoute);
    }
}
