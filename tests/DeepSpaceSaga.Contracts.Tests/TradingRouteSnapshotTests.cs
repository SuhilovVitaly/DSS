using System.Collections.Immutable;
using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class TradingRouteSnapshotTests
{
    [Theory]
    [InlineData(TradingRouteAvailability.Available, TradingRouteRisk.Safe)]
    [InlineData(TradingRouteAvailability.Restricted, TradingRouteRisk.Elevated)]
    [InlineData(TradingRouteAvailability.Unavailable, TradingRouteRisk.Elevated)]
    public void Routes_roundtrip_effective_base_values_stable_enums_reason_and_event_order(TradingRouteAvailability availability, TradingRouteRisk risk)
    {
        var route = new TradingRouteSnapshot("A", "B", "Medium", 3600000, 5400000, 1000, 1200,
            "risk.quarantine", risk, availability, "Карантин; blockade", ["second", "first"]);
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [], TradingRoutes: [route]);
        string json = JsonSerializer.Serialize(snapshot);
        Assert.Contains($"\"Availability\":\"{availability}\"", json);
        Assert.Contains($"\"Risk\":\"{risk}\"", json);
        var restored = JsonSerializer.Deserialize<AuthoritativeSnapshot>(json)!;
        var actual = Assert.Single(restored.TradingRoutes);
        Assert.Equal(route, actual with { ActiveEventIds = route.ActiveEventIds });
        Assert.Equal(route.ActiveEventIds.ToArray(), actual.ActiveEventIds.ToArray());
    }

    [Theory]
    [InlineData("")]
    [InlineData(",\"TradingRoutes\":null")]
    [InlineData(",\"TradingRoutes\":[]")]
    public void Legacy_absent_null_and_empty_arrays_remain_compatible(string field)
    {
        var snapshot = JsonSerializer.Deserialize<AuthoritativeSnapshot>(
            "{\"SnapshotSequence\":1,\"GameTimeMs\":0,\"CurrentSpeed\":0,\"Objects\":[]" + field + "}")!;
        Assert.True(snapshot.TradingRoutes.IsDefaultOrEmpty);
        Assert.NotNull(JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(snapshot)));
    }

    [Fact]
    public void Default_and_null_event_id_arrays_serialize_and_deserialize_safely()
    {
        var route = new TradingRouteSnapshot("A", "B", "Short", 1, 1, 1000, 1000,
            "risk.safe", TradingRouteRisk.Safe, TradingRouteAvailability.Available);
        string json = JsonSerializer.Serialize(route);
        Assert.Contains("\"ActiveEventIds\":[]", json);
        Assert.True(JsonSerializer.Deserialize<TradingRouteSnapshot>(json.Replace("\"ActiveEventIds\":[]", "\"ActiveEventIds\":null"))!.ActiveEventIds.IsDefaultOrEmpty);
        Assert.Null(JsonSerializer.Deserialize<TradingRouteSnapshot>(json)!.ReasonText);
        Assert.Equal("\"Safe\"", JsonSerializer.Serialize(TradingRouteRisk.Safe));
        Assert.Equal("\"Available\"", JsonSerializer.Serialize(TradingRouteAvailability.Available));
    }
}
