using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class VoyageLifecycleTests
{
    private const string ShipId = "SPC-0001";
    private const string BridgeId = "MOD-PLAYER-BRIDGE-01";

    internal static ScenarioFile DockedScenario(long debt = 0)
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(root, "Scenarios", "Docked", "scenario.json"));
        return scenario with
        {
            GameState = scenario.GameState with
            {
                MasterSeed = 1,
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o.ObjectId == ShipId
                    ? o with { PortFeeDebt = debt } : o).ToArray(),
            }
        };
    }

    internal static SimulationEngine CreateEngine(long debt = 0)
    {
        var engine = new SimulationEngine(QuotedTradeExecutionTests.RealRegistry());
        engine.LoadScenario(DockedScenario(debt));
        return engine;
    }

    internal static PlayerCommand Undock(string id, string? destination) =>
        new(id, 1, ShipId, BridgeId, NavigationComputerCommandTypes.Undock,
            TargetObjectId: destination);

    [Fact]
    public void Map_backed_undock_requires_adjacent_destination_and_publishes_options()
    {
        using var engine = CreateEngine();
        var before = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        Assert.Equal(VoyagePhases.Docked, before.Voyage?.Phase);
        var options = before.Voyage!.RouteOptions;
        Assert.NotEmpty(options);
        Assert.Equal(options.OrderBy(o => o.DestinationStationObjectId, StringComparer.Ordinal), options);

        engine.ReceiveCommand(Undock("missing", null));
        var missing = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        Assert.Equal(CommandReasonCodes.VoyageDestinationRequired,
            Assert.Single(missing.CommandResults).ReasonCode);
        Assert.True(missing.Objects.Single(o => o.ObjectId == ShipId).IsDocked);
        Assert.Equal(CommandReasonCodes.VoyageDestinationRequired, missing.Voyage?.BlockReasonCode);

        engine.ReceiveCommand(Undock("unknown", "not-a-station"));
        var unavailable = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        Assert.Equal(CommandReasonCodes.VoyageDestinationUnavailable,
            Assert.Single(unavailable.CommandResults).ReasonCode);

        string destination = options.First(o => o.IsAvailable).DestinationStationObjectId;
        engine.ReceiveCommand(Undock("voyage-1", destination));
        var departed = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(departed.CommandResults).Status);
        Assert.False(departed.Objects.Single(o => o.ObjectId == ShipId).IsDocked);
        Assert.Null(departed.DockedStationTrade);
        Assert.Equal(VoyagePhases.Undocking, departed.ActiveVoyage?.State);
        Assert.Equal("voyage-1", departed.ActiveVoyage?.VoyageId);
        Assert.Equal(destination, departed.ActiveVoyage?.DestinationStationObjectId);

        engine.ReceiveCommand(Undock("voyage-1", destination));
        var retry = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        Assert.Equal("voyage-1", retry.ActiveVoyage?.VoyageId);
        Assert.Single(retry.CommandResults);
    }

    [Fact]
    public void Debt_blocks_departure_without_changing_stay_or_creating_voyage()
    {
        using var engine = CreateEngine(debt: 7);
        var before = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        string destination = before.Voyage!.RouteOptions[0].DestinationStationObjectId;
        engine.ReceiveCommand(Undock("debt", destination));
        var after = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        Assert.Equal(CommandReasonCodes.VoyageOutstandingDebt, Assert.Single(after.CommandResults).ReasonCode);
        Assert.True(after.Objects.Single(o => o.ObjectId == ShipId).IsDocked);
        Assert.Null(after.ActiveVoyage);
        Assert.Equal(before.PortFees, after.PortFees);
        Assert.Equal(before.PlayerCredits, after.PlayerCredits);
    }
    [Fact]
    public void Undock_preserves_motion_and_wrong_destination_cannot_dock()
    {
        using var voyage = TradingVoyageFixture.Create();
        var before = voyage.Snapshot.Objects.Single(o => o.ObjectId == ShipId);
        voyage.Send(BridgeId, NavigationComputerCommandTypes.Undock, target: voyage.Destination);
        var after = voyage.Snapshot.Objects.Single(o => o.ObjectId == ShipId);
        Assert.Equal((before.X, before.Y, before.SpeedKmS, before.Direction, before.ApproachRoute),
            (after.X, after.Y, after.SpeedKmS, after.Direction, after.ApproachRoute));
        var (_, denied) = voyage.Send(BridgeId, NavigationComputerCommandTypes.Dock, target: voyage.Origin);
        Assert.Equal(CommandReasonCodes.VoyageWrongDestination, denied?.ReasonCode);
        var (_, alreadyActive) = voyage.Send(BridgeId, NavigationComputerCommandTypes.Undock, target: voyage.Destination);
        Assert.Equal(CommandReasonCodes.VoyageAlreadyActive, alreadyActive?.ReasonCode);
        Assert.Equal(VoyagePhases.Undocking, voyage.Snapshot.ActiveVoyage!.Phase);
        voyage.Advance(1);
        Assert.Equal(VoyagePhases.InTransit, voyage.Snapshot.ActiveVoyage!.Phase);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Docking_save_continues_real_dialogue_and_abort_reconciles_to_transit(bool explicitSaveFlag)
    {
        using var voyage = TradingVoyageFixture.Create();
        voyage.Send(BridgeId, NavigationComputerCommandTypes.Undock, target: voyage.Destination);
        SimulationEngine? restored = null;
        try
        {
            voyage.FinishFlightTo(voyage.Destination, beforeDialogue: current =>
            {
                Assert.Equal(VoyagePhases.Docking, current.Snapshot.ActiveVoyage!.Phase);
                var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(current.Save()), true);
                restored = new SimulationEngine(QuotedTradeExecutionTests.RealRegistry());
                restored.LoadScenario(save, isSave: explicitSaveFlag);
                var snapshot = restored.CaptureSnapshotForTests(current.Snapshot.GameTimeMs, SimulationSpeed.Speed0, current.MotionTime);
                Assert.Equal(current.Snapshot.ActiveVoyage, snapshot.ActiveVoyage);
                var active = snapshot.ActiveDialogue!;
                restored.ReceiveDialogueCommand(new("abort-saved-dialogue", DialogueAction.Abort,
                    active.InstanceId, active.Revision));
                snapshot = restored.CaptureSnapshotForTests(current.Snapshot.GameTimeMs, SimulationSpeed.Speed0, current.MotionTime);
                Assert.Equal(VoyagePhases.InTransit, snapshot.ActiveVoyage!.Phase);
                Assert.Null(snapshot.DockedStationTrade);
                Assert.False(snapshot.Objects.Single(o => o.ObjectId == ShipId).IsDocked);
            });
            Assert.Equal(VoyagePhases.Docked, voyage.Snapshot.Voyage!.Phase);
            Assert.Equal(voyage.Destination, voyage.Snapshot.DockedStationTrade!.StationObjectId);
        }
        finally
        {
            restored?.Dispose();
        }
    }
    [Fact]
    public void Progress_stays_monotonic_after_course_change_and_flying_away()
    {
        using var voyage = TradingVoyageFixture.Create(calendarRatio: 1);
        const string engineModule = "MOD-PLAYER-ENGINE-01";
        voyage.Send(BridgeId, NavigationComputerCommandTypes.Undock, target: voyage.Destination);
        voyage.Send(engineModule, ShipEngineCommandTypes.Accelerate);
        for (int i = 0; i < 10 && voyage.Snapshot.Objects.Single(o => o.ObjectId == ShipId).SpeedKmS == 0; i++)
            voyage.Advance(1000);
        Assert.True(voyage.Snapshot.Objects.Single(o => o.ObjectId == ShipId).SpeedKmS > 0);
        voyage.Send(engineModule, NavigationComputerCommandTypes.Approach, target: voyage.Destination);
        for (int i = 0; i < 10 && voyage.Snapshot.Objects.Single(o => o.ObjectId == ShipId).ApproachRoute is null; i++)
            voyage.Advance(1000);
        var route = Assert.IsType<ApproachRoute>(voyage.Snapshot.Objects.Single(o => o.ObjectId == ShipId).ApproachRoute);
        voyage.Advance((long)Math.Ceiling(route.DurationMs / 2));
        int progress = voyage.Snapshot.ActiveVoyage!.ProgressPermille;
        Assert.InRange(progress, 1, 1000);
        voyage.Send(engineModule, ShipEngineCommandTypes.CancelAll);
        voyage.Send(engineModule, ShipEngineCommandTypes.TurnRightStep);
        voyage.Advance(10000);
        var before = voyage.Snapshot.Objects.Single(o => o.ObjectId == ShipId);
        var station = voyage.Snapshot.Objects.Single(o => o.ObjectId == voyage.Destination);
        double Distance(ObjectMotionSnapshot ship) => Math.Sqrt(Math.Pow(ship.X - station.X, 2) + Math.Pow(ship.Y - station.Y, 2));
        voyage.Advance((long)Math.Ceiling(route.DurationMs * 4));
        Assert.True(Distance(voyage.Snapshot.Objects.Single(o => o.ObjectId == ShipId)) > Distance(before));
        Assert.True(voyage.Snapshot.ActiveVoyage!.ProgressPermille >= progress);
        Assert.Equal(VoyagePhases.InTransit, voyage.Snapshot.ActiveVoyage.Phase);
    }
}
