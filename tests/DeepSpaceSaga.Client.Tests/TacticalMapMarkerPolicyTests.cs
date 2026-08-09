using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using Xunit;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// ТЗ-10: marker sizes are fixed screen-space pixels (10/25/50), independent
/// of zoom — proven by the policy having no zoom parameter — and scale
/// visibility (Small/Combat vs Medium/Large/System) is applied client-side.
/// </summary>
public class TacticalMapMarkerPolicyTests
{
    // ── Sizes (screen px, zoom-independent) ──────────────────────

    [Theory]
    [InlineData(SpaceObjectType.UnknownSpaceObject)]
    [InlineData(SpaceObjectType.Asteroid)]
    [InlineData(SpaceObjectType.Station)]
    [InlineData(SpaceObjectType.NpcShip)]
    [InlineData(SpaceObjectType.PlayerShip)]
    public void Regular_types_are_10px(string renderType)
    {
        Assert.Equal(10f, TacticalMapMarkerPolicy.GetMarkerSizePx(renderType));
    }

    [Fact]
    public void Null_render_type_is_10px()
    {
        Assert.Equal(10f, TacticalMapMarkerPolicy.GetMarkerSizePx(null));
    }

    [Fact]
    public void Planet_is_25px()
    {
        Assert.Equal(25f, TacticalMapMarkerPolicy.GetMarkerSizePx(SpaceObjectType.Planet));
    }

    [Fact]
    public void Sun_is_50px()
    {
        Assert.Equal(50f, TacticalMapMarkerPolicy.GetMarkerSizePx(SpaceObjectType.Sun));
    }

    [Theory]
    [InlineData(SpaceObjectType.Sun, 25f)]
    [InlineData(SpaceObjectType.Planet, 12.5f)]
    [InlineData(SpaceObjectType.Station, 5f)]
    [InlineData(SpaceObjectType.Asteroid, 5f)]
    [InlineData(SpaceObjectType.NpcShip, 5f)]
    [InlineData(SpaceObjectType.PlayerShip, 5f)]
    [InlineData(SpaceObjectType.UnknownSpaceObject, 5f)]
    public void Radius_is_half_the_size(string renderType, float expectedRadius)
    {
        Assert.Equal(expectedRadius, TacticalMapMarkerPolicy.GetMarkerRadiusPx(renderType));
    }

    [Fact]
    public void Null_radius_is_half_of_10px()
    {
        Assert.Equal(5f, TacticalMapMarkerPolicy.GetMarkerRadiusPx(null));
    }

    // ── Scale visibility matrix: type × PPU {2.0, 1.0, 0.1, 0.01, 0.001} ──

    private const double FullPpu1 = 2.0;
    private const double FullPpu2 = 1.0;
    private const double SmallPpu1 = 0.1;
    private const double SmallPpu2 = 0.01;
    private const double SmallPpu3 = 0.001;

    private static readonly string[] SmallScaleHiddenTypes =
        { SpaceObjectType.Asteroid, SpaceObjectType.UnknownSpaceObject, SpaceObjectType.NpcShip };

    private static readonly string[] SmallScaleVisibleTypes =
        { SpaceObjectType.PlayerShip, SpaceObjectType.Station, SpaceObjectType.Planet, SpaceObjectType.Sun };

    [Theory]
    [InlineData(FullPpu1)]
    [InlineData(FullPpu2)]
    public void At_full_scale_all_7_types_are_visible(double ppu)
    {
        foreach (var type in SmallScaleHiddenTypes)
            Assert.True(TacticalMapMarkerPolicy.ShouldRenderAtScale(type, ppu), $"{type} visible at PPU={ppu}");
        foreach (var type in SmallScaleVisibleTypes)
            Assert.True(TacticalMapMarkerPolicy.ShouldRenderAtScale(type, ppu), $"{type} visible at PPU={ppu}");
        Assert.True(TacticalMapMarkerPolicy.ShouldRenderAtScale(null, ppu), "null visible at PPU=" + ppu);
    }

    [Theory]
    [InlineData(SmallPpu1)]
    [InlineData(SmallPpu2)]
    [InlineData(SmallPpu3)]
    public void At_small_scale_asteroid_unknown_and_npc_are_hidden(double ppu)
    {
        foreach (var type in SmallScaleHiddenTypes)
            Assert.False(TacticalMapMarkerPolicy.ShouldRenderAtScale(type, ppu), $"{type} hidden at PPU={ppu}");
        Assert.False(TacticalMapMarkerPolicy.ShouldRenderAtScale(null, ppu), "null hidden at PPU=" + ppu);
    }

    [Theory]
    [InlineData(SmallPpu1)]
    [InlineData(SmallPpu2)]
    [InlineData(SmallPpu3)]
    public void At_small_scale_player_station_planet_sun_stay_visible(double ppu)
    {
        foreach (var type in SmallScaleVisibleTypes)
            Assert.True(TacticalMapMarkerPolicy.ShouldRenderAtScale(type, ppu), $"{type} visible at PPU={ppu}");
    }
}
