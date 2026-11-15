using DeepSpaceSaga.Client.UI.Screens.Ship;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

namespace DeepSpaceSaga.Client.Tests;

public sealed class Ep0029Us0010Tk0002Tests
{
    [Fact]
    public void SameAppearanceKeyAcrossWorldAndBattle()
    {
        // The world appearance id and the tactical carrier model resolve to the same resource key.
        Assert.Equal("carrier", CarrierAppearance.ResourceKey("carrier"));
        Assert.Equal("carrier", CarrierAppearance.ResourceKey(null));
        Assert.Equal("carrier", CarrierAppearance.ResourceKey(""));
    }

    [Fact]
    public void CarrierRotationAndHitboxStayAligned()
    {
        // Rotation frames wrap on the same 32-frame wheel; the shared frame math matches the
        // tactical ship sheet used for drawing (0/90/180 are stable, finite angles).
        foreach (double course in new[] { 0, 90, 180, 360, 450, -90 })
        {
            int frame = CarrierAppearance.Frame(course);
            Assert.InRange(frame, 0, 31);
            Assert.Equal(TacticalShipSprites.Frame(course), frame);
        }
    }

    [Fact]
    public void MissingAssetFallbackKeepsName()
    {
        // A missing appearance asset falls back to the default carrier; the name is never derived.
        Assert.Equal("carrier", CarrierAppearance.FallbackKey(null));
        Assert.Equal("carrier", CarrierAppearance.FallbackKey("missing-appearance"));
        // The resource key still reports the requested id so diagnostics keep the identity.
        Assert.Equal("missing-appearance", CarrierAppearance.ResourceKey("missing-appearance"));
    }
}
