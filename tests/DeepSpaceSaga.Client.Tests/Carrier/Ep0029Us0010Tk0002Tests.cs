using System.Collections.Immutable;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.Ship;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

[Collection("InterfaceLog")]
public sealed class Ep0029Us0010Tk0002Tests
{
    private static TacticalUnitSnapshot Unit(TacticalUnitKind kind, string frameId) => new(
        Id: "u", Side: "A", Kind: kind, FrameId: frameId, Pose: new TacticalPoseSnapshot(0, 0, 0), Speed: 10,
        Hull: 10, MaxHull: 10, Armor: 10, MaxArmor: 10, State: TacticalUnitState.Alive, RoleId: null,
        Jammed: false, Engines: ImmutableArray<TacticalEngineSnapshot>.Empty, Limits: null, PreviousTurn: 0,
        Build: null, Accuracy: 0, Maneuver: 0);

    [Fact]
    public void SameAppearanceKeyAcrossWorldAndBattle()
    {
        // The world appearance id and the tactical carrier model resolve to the same resource key.
        Assert.Equal("carrier", CarrierAppearance.ResourceKey("carrier"));
        Assert.Equal("carrier", CarrierAppearance.ResourceKey(null));
        Assert.Equal("carrier", CarrierAppearance.ResourceKey(""));

        var carrier = Unit(TacticalUnitKind.Carrier, "carrier");
        Assert.Equal("carrier", TacticalShipSprites.ModelFor(carrier, null));
        Assert.Equal("carrier", TacticalShipSprites.ModelFor(carrier, ""));
        Assert.Equal("carrier", TacticalShipSprites.ModelFor(carrier, "carrier"));
        Assert.Equal("ship.tetrarch", TacticalShipSprites.ModelFor(carrier, "ship.tetrarch"));
        Assert.Equal(CarrierAppearance.ResourceKey("ship.tetrarch"), TacticalShipSprites.ModelFor(carrier, "ship.tetrarch"));

        // Drones keep their frame-id logic regardless of the carrier appearance.
        foreach (var (frame, expected) in new[] { ("light", "light"), ("heavy", "heavy"), ("thug", "thug"), ("custom", "heavy") })
            Assert.Equal(expected, TacticalShipSprites.ModelFor(Unit(TacticalUnitKind.Drone, frame), "ship.tetrarch"));
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

    [Fact]
    public void UnknownCarrierAppearanceFallsBackToDefaultCarrierSheet()
    {
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        using var scope = TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic("tabletop"));
        var carrier = Unit(TacticalUnitKind.Carrier, "carrier");

        // The tabletop pack has no "field.unit.sprite.ship.tetrarch.*" role, so a known-unknown key
        // must render exactly like the default "carrier" sheet instead of disappearing.
        byte[] defaultSheet = RenderCarrier(carrier, "carrier");
        byte[] unknownSheet = RenderCarrier(carrier, "ship.tetrarch");
        Assert.Contains(defaultSheet.Where((_, i) => i % 4 == 3), alpha => alpha > 0); // a real sprite, not blank
        Assert.Equal(defaultSheet, unknownSheet);
    }

    [Fact]
    public async Task CapturedFrameCarriesTheWorldCarrierAppearance()
    {
        await using var scene = await Ep0019Scenes.Create("1-move-no-hover");
        Assert.Equal("carrier", scene.Screen.Flow.Session.CarrierOperations?.AppearanceId);
        Assert.Equal(scene.Screen.Flow.Session.CarrierOperations?.AppearanceId,
            scene.Screen.CaptureFrame(1920, 1080)!.CarrierAppearanceId);
    }

    private static byte[] RenderCarrier(TacticalUnitSnapshot carrier, string? appearance)
    {
        using var bitmap = new SKBitmap(128, 128);
        using var canvas = new SKCanvas(bitmap);
        Assert.True(TacticalShipSprites.Draw(canvas, carrier, new SKPoint(64, 64), true, 0,
            shadow: false, carrierAppearance: appearance));
        return bitmap.Bytes.ToArray();
    }
}
