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

    // ── Marker style selection (glint vs spherical) ──────────────

    [Theory]
    [InlineData(SpaceObjectType.Asteroid)]
    [InlineData(SpaceObjectType.UnknownSpaceObject)]
    public void UsesGlintMarker_is_true_for_asteroids_and_unknown_objects(string renderType)
    {
        Assert.True(TacticalMapMarkerPolicy.UsesGlintMarker(renderType));
    }

    [Theory]
    [InlineData(SpaceObjectType.NpcShip)]
    [InlineData(SpaceObjectType.Station)]
    [InlineData(SpaceObjectType.Planet)]
    [InlineData(SpaceObjectType.Sun)]
    [InlineData(SpaceObjectType.PlayerShip)]
    [InlineData(null)]
    public void UsesGlintMarker_is_false_for_other_types(string? renderType)
    {
        Assert.False(TacticalMapMarkerPolicy.UsesGlintMarker(renderType));
    }
}
