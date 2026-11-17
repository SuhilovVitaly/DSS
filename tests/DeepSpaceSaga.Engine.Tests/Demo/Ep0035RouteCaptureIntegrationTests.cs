using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class Ep0035RouteCaptureIntegrationTests
{
    [Theory]
    [InlineData(2500)]
    [InlineData(5000)]
    public void PassengerVoyageDirectCapturesWhileObservedDetourPasses(int stepMs)
    {
        using var engine = Ep0035Us5Tk2Tests.Docked();
        var state = engine.CaptureSnapshot().DemoCampaign!;
        var offer = state.Transport!.StationJobs!.Offers.First(o => o.OriginStationId == "SPC-0002" &&
            o.DestinationStationId != "SPC-0005" && StationJobTypes.CarriesPassengers(o.Type) && StationJobReasons.AcceptBlocker(state.Transport.StationJobs, o.Id) is null);
        Assert.True(engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("pax", state.Revision, offer.Id)).Applied);
        var baseline = engine.CaptureSaveState();
        var ship = baseline.GameState.SpaceObjects.Single(s => s.ObjectId == "SPC-0001");
        var destination = engine.CaptureSnapshot().Objects.Single(o => o.ObjectId == offer.DestinationStationId);
        double dx = destination.X - ship.PositionX, dy = destination.Y - ship.PositionY;
        double length = Math.Sqrt(dx * dx + dy * dy);
        Assert.True(length > 15000, $"Route length {length}; destination {offer.DestinationStationId}");
        double x = ship.PositionX + dx / length * 12000, y = ship.PositionY + dy / length * 12000;
        var campaign = baseline.GameState.DemoCampaign!;
        var world = campaign.PirateWorld!;
        var groups = world.Groups.Select((g, i) =>
        {
            double gx = i == 0 ? x : x + 100000 + i * 10000, gy = i == 0 ? y : y + 100000;
            return g with
            {
                X = gx,
                Y = gy,
                SpeedKmS = 0,
                MotionPlan = null,
                PatrolPoints = [new(gx + 100, gy + 100), new(gx + 100, gy - 100), new(gx - 100, gy - 100), new(gx - 100, gy + 100)],
                Members = g.Members.Select(m => m with
                {
                    Engine = m.Engine with { StructurePoints = 0 },
                    Sensor = i == 0 ? m.Sensor : m.Sensor with { PowerState = "Off" }
                }).ToImmutableArray()
            };
        }).ToImmutableArray();
        // Observe through the real sensor/report path, then continue from the station.
        var observedWorld = baseline with
        {
            GameState = baseline.GameState with
            {
                VoyageState = null,
                DemoCampaign = campaign with { PirateWorld = world with { Groups = groups } },
                SpaceObjects = baseline.GameState.SpaceObjects.Select(s => s.ObjectId == ship.ObjectId ? ship with
                {
                    PositionX = x,
                    PositionY = y + 1500,
                    IsDocked = false,
                    DockedStationObjectId = null,
                    FirstPortFeeGameTimeMs = null,
                    NextPortFeeDueGameTimeMs = null
                } : s).ToArray()
            }
        };
        engine.LoadScenario(observedWorld, true);
        var detected = engine.CaptureSnapshot().DemoCampaign!;
        Assert.True(engine.ExecuteDemoCampaign(new DismissSensorContactReportRequest("ack", detected.Revision, detected.PendingSensorReport!.BatchId)).Applied);
        var ready = engine.CaptureSaveState();
        ready = ready with
        {
            GameState = ready.GameState with
            {
                VoyageState = baseline.GameState.VoyageState,
                SpaceObjects = ready.GameState.SpaceObjects.Select(s => s.ObjectId == ship.ObjectId ? ship : s).ToArray()
            }
        };
        foreach (bool detour in new[] { true, false })
        {
            engine.LoadScenario(ready, true);
            var snapshot = engine.CaptureSnapshot();
            var routes = snapshot.DemoCampaign!.ThreatRoutes!;
            var option = routes.Options.Single(o => o.IsDetour == detour && o.DestinationStationId == offer.DestinationStationId);
            Assert.Equal(detour ? 0 : 4000, option.KnownExposureWorldUnits, 4);
            Assert.True(snapshot.DemoCampaign.Transport!.CargoContracts!.PlayerCargoMassKg > 0);
            Assert.NotEmpty(snapshot.DemoCampaign.Transport.PassengerContracts!.Active);
            Assert.True(engine.ExecuteDemoCampaign(new SelectDemoRouteCommand("select", snapshot.DemoCampaign.Revision, option.RouteId, routes.Revision)).Applied);
            engine.ReceiveCommand(new("depart", 1, ship.ObjectId, "MOD-PLAYER-BRIDGE-01", NavigationComputerCommandTypes.Undock, offer.DestinationStationId));
            engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
            long motion = 0;
            bool captured = false, completed = false;
            for (int step = 0; motion < 100_000_000; step++)
            {
                motion += stepMs;
                snapshot = engine.CaptureSnapshotForTests(motion * 300, SimulationSpeed.Speed1, motion);
                captured = snapshot.DemoCampaign!.WorldEncounter is not null;
                if (captured) break;
                var saved = engine.CaptureSaveStateForTests(motion * 300, SimulationSpeed.Speed0, motion);
                var route = saved.GameState.VoyageState!.ThreatRoute!;
                Assert.False(route.Suspended);
                if (route.NextWaypoint == option.Waypoints.Length) { completed = true; break; }
                if (step % 7 == 0)
                {
                    var paused = engine.CaptureSnapshotForTests(motion * 300, SimulationSpeed.Speed0, motion);
                    Assert.Equal(System.Text.Json.JsonSerializer.Serialize(snapshot.Objects.Single(o => o.ObjectId == ship.ObjectId)), System.Text.Json.JsonSerializer.Serialize(paused.Objects.Single(o => o.ObjectId == ship.ObjectId)));
                }
                if (step == 5) engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(saved), true, true), true);
            }
            Assert.Equal(!detour, captured);
            Assert.Equal(detour, completed);
        }
    }
}
