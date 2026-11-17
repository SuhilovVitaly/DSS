using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

/// <summary>
/// EP-0034-US-0001-TK-0001: passenger capacity is an immutable, JSON-serializable projection inside the
/// demo transport section. Absent data never turns into free cabins.
/// </summary>
public sealed class DemoEp0034Us0001Tk0001Tests
{
    [Fact]
    public void Payload_RoundTrips()
    {
        var capacity = new PassengerCapacitySnapshot(TotalCabins: 12, CrewCabins: 6, PassengerCabins: 1,
            ReservedCabins: 2, FreeCabins: 3, LifeSupportReason: null);
        var snapshot = new DemoCampaignSnapshot(4, 9, Transport: new DemoTransportSnapshot { PassengerCapacity = capacity });

        var json = JsonSerializer.Serialize(snapshot);
        var loaded = JsonSerializer.Deserialize<DemoCampaignSnapshot>(json)!;

        Assert.Equal(capacity, loaded.Transport!.PassengerCapacity);
        Assert.Equal(snapshot.Transport, loaded.Transport);
        Assert.Equal(12, loaded.Transport.PassengerCapacity!.TotalCabins);
        Assert.Equal(3, loaded.Transport.PassengerCapacity.FreeCabins);
        Assert.Null(PassengerCapacityReasons.BoardingBlocker(loaded.Transport.PassengerCapacity));
    }

    [Fact]
    public void LegacyPayload_UsesSafeDefaults()
    {
        // EP-0032 snapshots carry no transport section; an empty transport carries no capacity.
        var legacy = JsonSerializer.Deserialize<DemoCampaignSnapshot>("""{"Revision":3,"CommitSequence":3}""")!;
        Assert.Null(legacy.Transport);
        Assert.Null(JsonSerializer.Deserialize<DemoTransportSnapshot>("{}")!.PassengerCapacity);

        // Missing capacity means "unavailable", never an implicit seat.
        Assert.Equal(DemoCampaignReasons.Unavailable, PassengerCapacityReasons.BoardingBlocker(null));
        Assert.Equal(DemoCampaignReasons.Unavailable,
            PassengerCapacityReasons.BoardingBlocker(new DemoTransportSnapshot().PassengerCapacity));
    }

    [Fact]
    public void ReasonCodes_AreStable()
    {
        Assert.Equal("no_free_cabins", PassengerCapacityReasons.NoFreeCabins);
        Assert.Equal("cabins_overcommitted", PassengerCapacityReasons.CabinsOvercommitted);
        Assert.Equal("insufficient_food_rations", PassengerCapacityReasons.InsufficientFoodRations);

        var full = new PassengerCapacitySnapshot(8, 6, 0, 2, 0);
        Assert.Equal(PassengerCapacityReasons.NoFreeCabins, PassengerCapacityReasons.BoardingBlocker(full));

        // A life-support blocker is reported before the free-seat count, so the real cause is shown.
        var starving = new PassengerCapacitySnapshot(12, 6, 0, 2, 4, PassengerCapacityReasons.InsufficientFoodRations);
        Assert.Equal(PassengerCapacityReasons.InsufficientFoodRations, PassengerCapacityReasons.BoardingBlocker(starving));
        var overcommitted = new PassengerCapacitySnapshot(4, 6, 0, 2, 0, PassengerCapacityReasons.CabinsOvercommitted);
        Assert.Equal(PassengerCapacityReasons.CabinsOvercommitted, PassengerCapacityReasons.BoardingBlocker(overcommitted));
    }
}
