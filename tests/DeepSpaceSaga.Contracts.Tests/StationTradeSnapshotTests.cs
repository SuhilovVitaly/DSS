using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

/// <summary>
/// Tests for <see cref="StationTradeSnapshot"/>/<see cref="StationInventoryItemSnapshot"/>
/// and the trade-related trailing fields on <see cref="AuthoritativeSnapshot"/>.
/// </summary>
public class StationTradeSnapshotTests
{
    [Fact]
    public void StationTradeSnapshot_round_trips_via_json()
    {
        var snapshot = new StationTradeSnapshot(
            StationObjectId: "station-1",
            Items: ImmutableArray.Create(
                new StationInventoryItemSnapshot(
                    ItemTypeId: "item.ice",
                    StockQuantity: 120,
                    UnitPriceCredits: 15,
                    MaxSellableQuantity: 40,
                    Category: TradeItemCategories.Resource),
                new StationInventoryItemSnapshot(
                    ItemTypeId: "item.fuel",
                    StockQuantity: 300,
                    UnitPriceCredits: 200,
                    MaxSellableQuantity: 0,
                    Category: TradeItemCategories.Good)));

        var json = JsonSerializer.Serialize(snapshot);
        var roundTripped = JsonSerializer.Deserialize<StationTradeSnapshot>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal("station-1", roundTripped!.StationObjectId);
        Assert.False(roundTripped.Items.IsDefault);
        Assert.Equal(2, roundTripped.Items.Length);

        var first = roundTripped.Items[0];
        Assert.Equal("item.ice", first.ItemTypeId);
        Assert.Equal(120, first.StockQuantity);
        Assert.Equal(15, first.UnitPriceCredits);
        Assert.Equal(40, first.MaxSellableQuantity);
        Assert.Equal(TradeItemCategories.Resource, first.Category);

        var second = roundTripped.Items[1];
        Assert.Equal("item.fuel", second.ItemTypeId);
        Assert.Equal(0, second.MaxSellableQuantity);
        Assert.Equal(TradeItemCategories.Good, second.Category);
    }

    [Fact]
    public void StationInventoryItemSnapshot_category_defaults_to_good_when_not_specified()
    {
        var item = new StationInventoryItemSnapshot(
            ItemTypeId: "item.legacy",
            StockQuantity: 1,
            UnitPriceCredits: 1,
            MaxSellableQuantity: 1);

        Assert.Equal(TradeItemCategories.Good, item.Category);
    }

    [Fact]
    public void StationTradeSnapshot_items_default_to_default_or_empty()
    {
        var snapshot = new StationTradeSnapshot(StationObjectId: "station-1");

        Assert.True(snapshot.Items.IsDefaultOrEmpty);
    }

    [Fact]
    public void AuthoritativeSnapshot_docked_station_trade_defaults_to_null()
    {
        var objects = ImmutableArray.Create(
            new ObjectMotionSnapshot("obj-1", 100, 200, SpeedKmS: 5, Direction: 90));

        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 1000,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: objects);

        Assert.Null(snapshot.DockedStationTrade);
    }

    [Fact]
    public void AuthoritativeSnapshot_docked_station_trade_round_trips_via_json()
    {
        var objects = ImmutableArray.Create(
            new ObjectMotionSnapshot("obj-1", 100, 200, SpeedKmS: 5, Direction: 90));

        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 1000,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: objects,
            DockedStationTrade: new StationTradeSnapshot(
                StationObjectId: "station-1",
                Items: ImmutableArray.Create(
                    new StationInventoryItemSnapshot("item.ice", 120, 15, 40))));

        var json = JsonSerializer.Serialize(snapshot);
        var roundTripped = JsonSerializer.Deserialize<AuthoritativeSnapshot>(json);

        Assert.NotNull(roundTripped);
        Assert.NotNull(roundTripped!.DockedStationTrade);
        Assert.Equal("station-1", roundTripped.DockedStationTrade!.StationObjectId);
        Assert.Single(roundTripped.DockedStationTrade.Items);
        Assert.Equal("item.ice", roundTripped.DockedStationTrade.Items[0].ItemTypeId);
    }

    [Fact]
    public void AuthoritativeSnapshot_player_credits_defaults_to_zero()
    {
        var objects = ImmutableArray.Create(
            new ObjectMotionSnapshot("obj-1", 100, 200, SpeedKmS: 5, Direction: 90));

        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 1000,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: objects);

        Assert.Equal(0, snapshot.PlayerCredits);
    }

    [Fact]
    public void AuthoritativeSnapshot_player_credits_round_trips_via_json_with_explicit_value()
    {
        var objects = ImmutableArray.Create(
            new ObjectMotionSnapshot("obj-1", 100, 200, SpeedKmS: 5, Direction: 90));

        var snapshot = new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 1000,
            CurrentSpeed: SimulationSpeed.Speed1,
            Objects: objects,
            PlayerCredits: 4250);

        var json = JsonSerializer.Serialize(snapshot);
        var roundTripped = JsonSerializer.Deserialize<AuthoritativeSnapshot>(json);

        Assert.NotNull(roundTripped);
        Assert.Equal(4250, roundTripped!.PlayerCredits);
    }

    [Fact]
    public void Market_stock_fields_roundtrip_without_losing_zero_capacity()
    {
        var snapshot = new StationTradeSnapshot(
            StationObjectId: "station-1",
            Items: ImmutableArray.Create(
                new StationInventoryItemSnapshot(
                    ItemTypeId: "item.steel",
                    StockQuantity: 200,
                    UnitPriceCredits: 30,
                    MaxSellableQuantity: 0,
                    Category: TradeItemCategories.Good,
                    UnitMassKg: 5,
                    TargetStock: 100,
                    MaxStock: 200,
                    FreeStockCapacity: 0,
                    StockState: StationMarketStockState.Surplus)));

        var json = JsonSerializer.Serialize(snapshot);
        var roundTripped = JsonSerializer.Deserialize<StationTradeSnapshot>(json);

        Assert.NotNull(roundTripped);
        var item = Assert.Single(roundTripped!.Items);
        Assert.Equal((long?)100, item.TargetStock);
        Assert.Equal((long?)200, item.MaxStock);
        // A full market must report 0 free capacity, not a dropped/null field.
        Assert.Equal((long?)0, item.FreeStockCapacity);
        Assert.Equal(StationMarketStockState.Surplus, item.StockState);
    }

    [Fact]
    public void Every_stock_state_serializes_as_named_string()
    {
        foreach (var state in Enum.GetValues<StationMarketStockState>())
        {
            var item = new StationInventoryItemSnapshot(
                ItemTypeId: "item.ice",
                StockQuantity: 1,
                UnitPriceCredits: 1,
                MaxSellableQuantity: 1,
                StockState: state);

            var json = JsonSerializer.Serialize(item);

            Assert.Contains($"\"StockState\":\"{state}\"", json);
            Assert.Equal(state, JsonSerializer.Deserialize<StationInventoryItemSnapshot>(json)!.StockState);
        }
    }

    [Fact]
    public void Legacy_trade_json_has_null_market_fields()
    {
        const string legacyJson =
            "{\"StationObjectId\":\"station-1\",\"Items\":[{\"ItemTypeId\":\"item.ice\"," +
            "\"StockQuantity\":120,\"UnitPriceCredits\":15,\"MaxSellableQuantity\":40," +
            "\"Category\":\"Resource\",\"UnitMassKg\":3}]}";

        var snapshot = JsonSerializer.Deserialize<StationTradeSnapshot>(legacyJson);

        Assert.NotNull(snapshot);
        var item = Assert.Single(snapshot!.Items);

        Assert.Equal("item.ice", item.ItemTypeId);
        Assert.Equal(120, item.StockQuantity);
        Assert.Equal(15, item.UnitPriceCredits);
        Assert.Equal(40, item.MaxSellableQuantity);
        Assert.Equal(TradeItemCategories.Resource, item.Category);
        Assert.Equal(3, item.UnitMassKg);

        Assert.Null(item.TargetStock);
        Assert.Null(item.MaxStock);
        Assert.Null(item.FreeStockCapacity);
        Assert.Null(item.StockState);
    }

    [Fact]
    public void Legacy_positional_constructor_remains_valid()
    {
        var item = new StationInventoryItemSnapshot("item.ice", 120, 15, 40);

        Assert.Equal("item.ice", item.ItemTypeId);
        Assert.Equal(120, item.StockQuantity);
        Assert.Equal(15, item.UnitPriceCredits);
        Assert.Equal(40, item.MaxSellableQuantity);
        Assert.Equal(TradeItemCategories.Good, item.Category);
        Assert.Equal(1, item.UnitMassKg);

        Assert.Null(item.TargetStock);
        Assert.Null(item.MaxStock);
        Assert.Null(item.FreeStockCapacity);
        Assert.Null(item.StockState);
    }

    [Fact]
    public void Snapshot_does_not_expose_station_budget()
    {
        var snapshot = new StationTradeSnapshot(
            "station-1",
            ImmutableArray.Create(
                new StationInventoryItemSnapshot(
                    "item.ice", 120, 15, 40, TradeItemCategories.Resource, 3,
                    TargetStock: 100,
                    MaxStock: 200,
                    FreeStockCapacity: 80,
                    StockState: StationMarketStockState.Normal)));

        var json = JsonSerializer.Serialize(snapshot);
        using var document = JsonDocument.Parse(json);

        var rootProperties = document.RootElement.EnumerateObject()
            .Select(property => property.Name)
            .ToArray();
        Assert.DoesNotContain(rootProperties, name =>
            name.Contains("Budget", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Credit", StringComparison.OrdinalIgnoreCase));

        var itemProperties = document.RootElement.GetProperty("Items")[0].EnumerateObject()
            .Select(property => property.Name)
            .ToArray();
        Assert.DoesNotContain(itemProperties, name =>
            name.Contains("Budget", StringComparison.OrdinalIgnoreCase));
        // UnitPriceCredits is the only Credits-named field; the station's Credits and its
        // market budget cap stay Engine-internal (AC-05).
        Assert.DoesNotContain(itemProperties, name =>
            name.Contains("Credit", StringComparison.OrdinalIgnoreCase) &&
            name != nameof(StationInventoryItemSnapshot.UnitPriceCredits));
    }
}
