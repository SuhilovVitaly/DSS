using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class VoyageFinanceContractTests
{
    private static VoyageFinanceSnapshot Report(long? net = 37, bool unknown = false) => new(
        "leg", "A", "B", 10, 20, VoyageFinanceStates.AwaitingRealization, 100,
        unknown ? null : 20, unknown, 10, 15, 5, 10, 3, 7, 22, unknown ? null : net,
        [new("item.ice", long.MaxValue, long.MaxValue), new("item.water", 1, null)]);

    [Theory]
    [InlineData(37L)]
    [InlineData(-37L)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    [InlineData(null)]
    public void Voyage_finance_round_trips_all_components_negative_net_and_unsold_cargo(long? net)
    {
        var expected = Report(net, net is null);
        var snapshot = new AuthoritativeSnapshot(1, 20, SimulationSpeed.Speed0, [], VoyageFinances: [expected]);
        var actual = JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(Assert.Single(actual.VoyageFinances)));
        Assert.Equal(net, actual.VoyageFinances[0].NetProfitCredits);
        Assert.Equal(net is null, actual.VoyageFinances[0].HasUnknownCostOfGoodsSold);
        Assert.Equal(100, actual.VoyageFinances[0].GrossSalesCredits);
    }

    [Fact]
    public void Legacy_snapshot_defaults_voyage_finances_to_empty()
    {
        var legacy = JsonSerializer.Deserialize<AuthoritativeSnapshot>("""
            {"SnapshotSequence":1,"GameTimeMs":0,"CurrentSpeed":0,"Objects":[]}
            """)!;
        Assert.True(legacy.VoyageFinances.IsDefaultOrEmpty);
        var constructed = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, []);
        Assert.True(JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(constructed))!.VoyageFinances.IsDefaultOrEmpty);
        var empty = constructed with { VoyageFinances = ImmutableArray<VoyageFinanceSnapshot>.Empty };
        Assert.Empty(JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(empty))!.VoyageFinances);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Report_order_and_empty_or_default_unsold_are_safe(bool empty)
    {
        var report = Report() with { UnsoldCargo = empty ? [] : default };
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [],
            VoyageFinances: [report with { VoyageId = "old" }, report with { VoyageId = "new" }]);
        var actual = JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Assert.Equal(new[] { "old", "new" }, actual.VoyageFinances.Select(v => v.VoyageId));
        Assert.All(actual.VoyageFinances, v => Assert.True(v.UnsoldCargo.IsDefaultOrEmpty));
    }

    [Fact]
    public void Voyage_finance_states_have_exact_wire_values()
    {
        Assert.Equal("in_transit", VoyageFinanceStates.InTransit);
        Assert.Equal("awaiting_realization", VoyageFinanceStates.AwaitingRealization);
        Assert.Equal("finalized", VoyageFinanceStates.Finalized);
        Assert.Equal("interrupted", VoyageFinanceStates.Interrupted);
    }
}
