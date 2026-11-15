using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

/// <summary>
/// EP-0029 US-0004 TK-0001 — ammo DTO and no-ammo skip/rejection codes.
/// </summary>
public class Ep0029Us0004Tk0001Tests
{
    [Fact]
    public void NoAmmoEnumsAppendWithoutRenumbering()
    {
        Assert.Equal(10, (int)TacticalFireSkipReason.NoAmmo);
        Assert.Equal(3, (int)TacticalOutcomeReasonCode.NoCombatCapability);
        Assert.Equal(26, (int)TacticalBattleRejectionCode.NoAmmo);

        // Old numeric identities are preserved (append-only).
        Assert.Equal(0, (int)TacticalFireSkipReason.None);
        Assert.Equal(9, (int)TacticalFireSkipReason.InDust);
        Assert.Equal(0, (int)TacticalOutcomeReasonCode.CarrierDestroyed);
        Assert.Equal(1, (int)TacticalOutcomeReasonCode.NoDrones);
        Assert.Equal(2, (int)TacticalOutcomeReasonCode.Timeout);
        Assert.Equal(25, (int)TacticalBattleRejectionCode.InDust);
    }

    private static TacticalUnitSnapshot Unit(int? ammoCurrent) => new(
        Id: "u", Side: "A", Kind: TacticalUnitKind.Drone, FrameId: "frame",
        Pose: new TacticalPoseSnapshot(0, 0, 0), Speed: 10, Hull: 10, MaxHull: 10, Armor: 5, MaxArmor: 5,
        State: TacticalUnitState.Alive, RoleId: null, Jammed: false,
        Engines: ImmutableArray<TacticalEngineSnapshot>.Empty, Limits: null, PreviousTurn: 0, Build: null,
        Accuracy: 0, Maneuver: 0)
    {
        AmmoCurrent = ammoCurrent,
        AmmoCapacity = 6,
    };

    [Fact]
    public void StepAmmoRoundTrip()
    {
        foreach (int ammo in new[] { 2, 1, 0 })
        {
            var copy = JsonSerializer.Deserialize<TacticalUnitSnapshot>(JsonSerializer.Serialize(Unit(ammo)))!;
            Assert.Equal(ammo, copy.AmmoCurrent);
            Assert.Equal(6, copy.AmmoCapacity);
        }

        // Legacy payload (null ammo) stays null = unknown, not infinite and not 0.
        var legacyCopy = JsonSerializer.Deserialize<TacticalUnitSnapshot>(JsonSerializer.Serialize(Unit(null)))!;
        Assert.Null(legacyCopy.AmmoCurrent);
        Assert.Equal(6, legacyCopy.AmmoCapacity);
    }
}
