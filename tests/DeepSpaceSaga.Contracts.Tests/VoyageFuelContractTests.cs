using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class VoyageFuelContractTests
{
    [Fact]
    public void Active_voyage_round_trips_reserved_projected_fuel_fields()
    {
        var voyage = new VoyageSnapshot(VoyagePhases.InTransit, "leg", "A", "B", "Beta", 500,
            ReservedFuelKg: 21, ProjectedConsumedFuelKg: 11, ProjectedRouteFuelCostCredits: 1234567890);
        var snapshot = new AuthoritativeSnapshot(1, 1000, SimulationSpeed.Speed0, [], Voyage: voyage);
        var actual = JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Assert.Equal(voyage with { RouteOptions = default }, actual.ActiveVoyage! with { RouteOptions = default });
        Assert.Equal(21, actual.ActiveVoyage.ReservedFuelKg);
        Assert.Equal(11, actual.ActiveVoyage.ProjectedConsumedFuelKg);
        Assert.Equal(1234567890, actual.ActiveVoyage.ProjectedRouteFuelCostCredits);
    }

    [Fact]
    public void Fuel_settlement_round_trips_exact_int64_values()
    {
        var settlement = new VoyageFuelSettlementSnapshot("leg", long.MaxValue, long.MaxValue - 1, 1, long.MaxValue);
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [], LastVoyageFuelSettlement: settlement);
        Assert.Equal(settlement, JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(snapshot))!.LastVoyageFuelSettlement);
        Assert.Equal(settlement, JsonSerializer.Deserialize<VoyageFuelSettlementSnapshot>(JsonSerializer.Serialize(settlement)));
    }

    [Fact]
    public void Legacy_snapshot_defaults_voyage_fuel_fields_to_null()
    {
        var actual = JsonSerializer.Deserialize<AuthoritativeSnapshot>("""
            {"SnapshotSequence":1,"GameTimeMs":0,"CurrentSpeed":0,"Objects":[],
             "Voyage":{"Phase":"InTransit","VoyageId":"legacy","OriginStationObjectId":"A","DestinationStationObjectId":"B"}}
            """)!;
        Assert.NotNull(actual.ActiveVoyage);
        Assert.Null(actual.ActiveVoyage.ReservedFuelKg);
        Assert.Null(actual.ActiveVoyage.ProjectedConsumedFuelKg);
        Assert.Null(actual.ActiveVoyage.ProjectedRouteFuelCostCredits);
        Assert.Null(actual.LastVoyageFuelSettlement);
    }

    [Theory]
    [InlineData("{\"Phase\":\"InTransit\"}")]
    [InlineData("{\"Phase\":\"InTransit\",\"ReservedFuelKg\":null,\"ProjectedConsumedFuelKg\":null,\"ProjectedRouteFuelCostCredits\":null}")]
    public void Missing_or_explicit_null_active_fuel_fields_remain_null_not_zero(string json)
    {
        var actual = JsonSerializer.Deserialize<VoyageSnapshot>(json)!;
        Assert.Null(actual.ReservedFuelKg);
        Assert.Null(actual.ProjectedConsumedFuelKg);
        Assert.Null(actual.ProjectedRouteFuelCostCredits);
        Assert.Null(new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, []).LastVoyageFuelSettlement);
    }

    [Fact]
    public void Voyage_fuel_reason_codes_are_stable_snake_case_values()
    {
        Assert.Equal("insufficient_voyage_fuel", CommandReasonCodes.InsufficientVoyageFuel);
        Assert.Equal("fuel_efficiency_unavailable", CommandReasonCodes.FuelEfficiencyUnavailable);
    }
}
