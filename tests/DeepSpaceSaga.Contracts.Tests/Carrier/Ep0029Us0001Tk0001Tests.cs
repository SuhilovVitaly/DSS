using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

/// <summary>
/// EP-0029 US-0001 TK-0001 — carrier hull and hangar-capacity projection.
/// </summary>
public class Ep0029Us0001Tk0001Tests
{
    [Fact]
    public void OptionalSnapshotRoundTrips()
    {
        // Legacy world snapshot predates CarrierOperations: must read without a fabricated wing.
        const string oldWorldJson =
            """{"SnapshotSequence":1,"GameTimeMs":0,"CurrentSpeed":1,"Objects":[]}""";
        var legacy = JsonSerializer.Deserialize<AuthoritativeSnapshot>(oldWorldJson)!;
        Assert.Null(legacy.CarrierOperations);

        // Legacy installed-module snapshot predates DroneCapacity.
        const string oldModuleJson =
            """{"ModuleId":"m","ModuleTypeId":"t","DisplayName":"M","Position":0,"CommandTypeIds":[]}""";
        var legacyModule = JsonSerializer.Deserialize<InstalledModuleSnapshot>(oldModuleJson)!;
        Assert.Null(legacyModule.DroneCapacity);

        // Round-trip through the current shape leaves an absent carrier block null.
        var present = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed1, ImmutableArray<ObjectMotionSnapshot>.Empty);
        var presentCopy = JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(present))!;
        Assert.Null(presentCopy.CarrierOperations);
    }

    [Fact]
    public void CapacityAndIdentityRoundTrip()
    {
        var hangars = ImmutableArray.Create(
            new HangarSlotSnapshot("MOD-PLAYER-BAY-01", 0),
            new HangarSlotSnapshot("MOD-PLAYER-BAY-01", 1),
            new HangarSlotSnapshot("MOD-PLAYER-BAY-02", 0),
            new HangarSlotSnapshot("MOD-PLAYER-BAY-02", 1));

        var carrier = new CarrierOperationsSnapshot(
            ShipId: "SPC-0001",
            ShipClassId: "ship.tetrarch",
            Name: "Skitalets",
            Revision: 7,
            HullCurrent: 400,
            HullMax: 400,
            IsDestroyed: false,
            AppearanceId: "carrier",
            Hangars: hangars);

        var json = JsonSerializer.Serialize(carrier);
        var copy = JsonSerializer.Deserialize<CarrierOperationsSnapshot>(json)!;

        Assert.Equal("SPC-0001", copy.ShipId);
        Assert.Equal("ship.tetrarch", copy.ShipClassId);
        Assert.Equal(7, copy.Revision);
        Assert.Equal((400, 400), (copy.HullCurrent, copy.HullMax));
        Assert.False(copy.IsDestroyed);
        Assert.False(copy.Hangars.IsDefault);
        Assert.Equal(4, copy.Hangars.Length);
        Assert.Equal("MOD-PLAYER-BAY-01", copy.Hangars[0].HangarModuleId);
        Assert.Equal(1, copy.Hangars[1].SlotIndex);
        Assert.Equal("MOD-PLAYER-BAY-02", copy.Hangars[3].HangarModuleId);
    }
}
