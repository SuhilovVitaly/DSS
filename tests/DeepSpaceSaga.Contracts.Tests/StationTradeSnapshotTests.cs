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
                    MaxSellableQuantity: 40),
                new StationInventoryItemSnapshot(
                    ItemTypeId: "item.fuel",
                    StockQuantity: 300,
                    UnitPriceCredits: 200,
                    MaxSellableQuantity: 0)));

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

        var second = roundTripped.Items[1];
        Assert.Equal("item.fuel", second.ItemTypeId);
        Assert.Equal(0, second.MaxSellableQuantity);
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
}
