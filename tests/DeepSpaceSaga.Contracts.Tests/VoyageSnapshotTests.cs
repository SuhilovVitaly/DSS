using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class VoyageSnapshotTests
{
    [Fact]
    public void Legacy_snapshot_defaults_voyage_to_null()
    {
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, []);
        var restored = JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Assert.Null(restored.Voyage);
        Assert.Null(restored.ActiveVoyage);
    }

    [Theory]
    [InlineData(VoyagePhases.Docked)]
    [InlineData(VoyagePhases.Undocking)]
    [InlineData(VoyagePhases.InTransit)]
    [InlineData(VoyagePhases.Docking)]
    public void Voyage_snapshot_roundtrip_preserves_all_phases_route_options_progress_and_blocker(string phase)
    {
        var voyage = new VoyageSnapshot(phase, "leg-1", "station-a", "station-b", "Beta", 750,
            CommandReasonCodes.VoyageOutstandingDebt,
            [new VoyageRouteOptionSnapshot("station-b", "Beta", 3600000, "Short", false,
                CommandReasonCodes.VoyageInsufficientFuel)], ReservedFuelKg: 42);
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [], Voyage: voyage);
        var restored = JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Assert.NotNull(restored.Voyage);
        Assert.Equal(voyage with { RouteOptions = default }, restored.Voyage with { RouteOptions = default });
        Assert.Equal(voyage.RouteOptions.ToArray(), restored.Voyage.RouteOptions.ToArray());
        Assert.Equal(phase == VoyagePhases.Docked, restored.ActiveVoyage is null);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Default_and_empty_route_options_roundtrip_safely(bool useDefault)
    {
        var voyage = new VoyageSnapshot(VoyagePhases.Docked,
            RouteOptions: useDefault ? default : ImmutableArray<VoyageRouteOptionSnapshot>.Empty);
        var json = JsonSerializer.Serialize(voyage);
        var restored = JsonSerializer.Deserialize<VoyageSnapshot>(json)!;
        Assert.Contains("\"RouteOptions\":[]", json);
        Assert.False(restored.RouteOptions.IsDefault);
        Assert.Empty(restored.RouteOptions);
        Assert.True(JsonSerializer.Deserialize<VoyageSnapshot>("{\"Phase\":\"Docked\"}")!.RouteOptions.IsDefaultOrEmpty);
    }

    [Fact]
    public void Voyage_reason_codes_are_stable_snake_case_values()
    {
        Assert.Equal("voyage_destination_required", CommandReasonCodes.VoyageDestinationRequired);
        Assert.Equal("voyage_destination_unavailable", CommandReasonCodes.VoyageDestinationUnavailable);
        Assert.Equal("voyage_already_active", CommandReasonCodes.VoyageAlreadyActive);
        Assert.Equal("voyage_wrong_destination", CommandReasonCodes.VoyageWrongDestination);
        Assert.Equal("voyage_outstanding_debt", CommandReasonCodes.VoyageOutstandingDebt);
        Assert.Equal("voyage_insufficient_fuel", CommandReasonCodes.VoyageInsufficientFuel);
    }

    [Fact]
    public void Undock_player_command_roundtrip_preserves_destination_target()
    {
        var command = new PlayerCommand("departure", 1, "ship", "nav", NavigationComputerCommandTypes.Undock,
            TargetObjectId: "station-b");
        Assert.Equal(command, JsonSerializer.Deserialize<PlayerCommand>(JsonSerializer.Serialize(command)));
    }
}
