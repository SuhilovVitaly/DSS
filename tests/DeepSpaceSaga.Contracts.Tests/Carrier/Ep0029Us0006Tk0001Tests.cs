using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

/// <summary>
/// EP-0029 US-0006 TK-0001 — sortie settlement receipt contract.
/// </summary>
public class Ep0029Us0006Tk0001Tests
{
    [Fact]
    public void SettlementReceiptRoundTrip()
    {
        var settlement = new SortieSettlementSnapshot(
            BattleId: "B-1", ShipId: "SPC-0001", CarrierRevision: 3, ShipDestroyed: false,
            LostDroneIds: ImmutableArray.Create("DRN-PLAYER-02"),
            ConsumedTorpedoes: 5, ExitKind: SortieExitKind.Completed);

        var copy = JsonSerializer.Deserialize<SortieSettlementSnapshot>(JsonSerializer.Serialize(settlement))!;
        Assert.Equal("B-1", copy.BattleId);
        Assert.Equal("SPC-0001", copy.ShipId);
        Assert.Equal(3, copy.CarrierRevision);
        Assert.Equal(5, copy.ConsumedTorpedoes);
        Assert.Equal(SortieExitKind.Completed, copy.ExitKind);
        Assert.False(copy.ShipDestroyed);
        Assert.Single(copy.LostDroneIds);
        Assert.Equal("DRN-PLAYER-02", copy.LostDroneIds[0]);
    }

    [Fact]
    public void DestroyedIsNotNoDrones()
    {
        var destroyed = new SortieSettlementSnapshot("B-1", "SPC-0001", 1, ShipDestroyed: true,
            LostDroneIds: ImmutableArray<string>.Empty, ConsumedTorpedoes: 0, ExitKind: SortieExitKind.Completed);
        var intact = new SortieSettlementSnapshot("B-1", "SPC-0001", 1, ShipDestroyed: false,
            LostDroneIds: ImmutableArray<string>.Empty, ConsumedTorpedoes: 0, ExitKind: SortieExitKind.Completed);

        Assert.True(destroyed.ShipDestroyed);
        Assert.False(intact.ShipDestroyed);
        Assert.Empty(destroyed.LostDroneIds);
        Assert.Empty(intact.LostDroneIds);
    }
}
