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
        return scenario with { GameState = scenario.GameState with
        {
            MasterSeed = 1,
            SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o.ObjectId == ShipId
                ? o with { PortFeeDebt = debt } : o).ToArray(),
        } };
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
}
