using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class StationMarketKnowledgeSnapshotTests
{
    [Theory]
    [InlineData(0UL, 0L, false)]
    [InlineData(ulong.MaxValue, long.MaxValue, true)]
    public void Market_knowledge_round_trips_without_exact_market_values(ulong revision, long time, bool stale)
    {
        var observations = ImmutableArray.Create(
            new StationMarketKnowledgeSnapshot("A", "Mining", true, time, revision, stale,
                [new("ice", StationMarketStockState.Surplus), new("water", StationMarketStockState.Shortage)]),
            new StationMarketKnowledgeSnapshot("B", "Industrial", false, 10, 5, !stale,
                [new("steel", StationMarketStockState.Normal)]));
        var snapshot = new AuthoritativeSnapshot(1, time, SimulationSpeed.Speed0, [], StationMarketKnowledge: observations);
        var actual = JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Assert.Equal(JsonSerializer.Serialize(observations), JsonSerializer.Serialize(actual.StationMarketKnowledge));
        Assert.Equal(revision, actual.StationMarketKnowledge[0].ObservedMarketRevision);
        Assert.Equal(time, actual.StationMarketKnowledge[0].ObservedAtGameTimeMs);
        Assert.Equal(stale, actual.StationMarketKnowledge[0].IsStale);
    }

    [Theory]
    [InlineData(StationMarketStockState.Shortage, "Shortage")]
    [InlineData(StationMarketStockState.Normal, "Normal")]
    [InlineData(StationMarketStockState.Surplus, "Surplus")]
    public void All_stock_bands_serialize_by_name(StationMarketStockState band, string name)
    {
        var json = JsonSerializer.Serialize(new StationMarketStockBandSnapshot("ice", band));
        Assert.Equal(name, JsonDocument.Parse(json).RootElement.GetProperty("StockState").GetString());
        Assert.Equal(band, JsonSerializer.Deserialize<StationMarketStockBandSnapshot>(json)!.StockState);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Authoritative_snapshot_market_knowledge_defaults_to_empty(bool explicitEmpty)
    {
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, []);
        Assert.True(snapshot.StationMarketKnowledge.IsDefaultOrEmpty);
        if (explicitEmpty) snapshot = snapshot with { StationMarketKnowledge = [] };
        Assert.True(JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(snapshot))!.StationMarketKnowledge.IsDefaultOrEmpty);
        var station = new StationMarketKnowledgeSnapshot("A", "Mining", true, 0, 0, false);
        Assert.True(JsonSerializer.Deserialize<StationMarketKnowledgeSnapshot>(JsonSerializer.Serialize(station))!.StockBands.IsDefaultOrEmpty);
    }

    [Fact]
    public void Legacy_snapshot_json_deserializes_without_market_knowledge()
    {
        var actual = JsonSerializer.Deserialize<AuthoritativeSnapshot>("""
            {"SnapshotSequence":1,"GameTimeMs":0,"CurrentSpeed":0,"Objects":[]}
            """)!;
        Assert.True(actual.StationMarketKnowledge.IsDefaultOrEmpty);
    }

    [Fact]
    public void Market_knowledge_contract_has_no_price_quantity_budget_or_quote_fields()
    {
        Assert.Equal(new[] { "StationObjectId", "StationRole", "IsAvailable", "ObservedAtGameTimeMs", "ObservedMarketRevision", "IsStale", "StockBands" },
            typeof(StationMarketKnowledgeSnapshot).GetProperties().Select(p => p.Name).ToArray());
        Assert.Equal(new[] { "ItemTypeId", "StockState" }, typeof(StationMarketStockBandSnapshot).GetProperties().Select(p => p.Name).ToArray());
        var json = JsonSerializer.Serialize(new StationMarketKnowledgeSnapshot("A", "Mining", true, 0, 0, false,
            [new("ice", StationMarketStockState.Normal)]));
        using var document = JsonDocument.Parse(json);
        Assert.Equal(7, document.RootElement.EnumerateObject().Count());
        Assert.Equal(2, document.RootElement.GetProperty("StockBands")[0].EnumerateObject().Count());
        foreach (var forbidden in new[] { "StockQuantity", "TargetStock", "MaxStock", "FreeStockCapacity", "UnitPriceCredits", "Credits", "Budget", "QuoteId", "Curve" })
            Assert.DoesNotContain(forbidden, json, StringComparison.OrdinalIgnoreCase);
    }
}
