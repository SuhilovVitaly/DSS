using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class SpaceMapColorResolverTests
{
    [Fact]
    public void PlayerShip_is_DarkOliveGreen()
    {
        var c = SpaceMapColorResolver.GetColor(SpaceObjectType.PlayerShip, PlayerRelation.Self);
        Assert.Equal(85, c.Red);
        Assert.Equal(107, c.Green);
        Assert.Equal(47, c.Blue);
    }

    [Fact]
    public void NpcShip_neutral_is_DarkGray()
    {
        var c = SpaceMapColorResolver.GetColor(SpaceObjectType.NpcShip, PlayerRelation.Neutral);
        Assert.Equal(169, c.Red);
        Assert.Equal(169, c.Green);
        Assert.Equal(169, c.Blue);
    }

    [Fact]
    public void NpcShip_enemy_is_DarkRed()
    {
        var c = SpaceMapColorResolver.GetColor(SpaceObjectType.NpcShip, PlayerRelation.Enemy);
        Assert.Equal(139, c.Red);
        Assert.Equal(0, c.Green);
        Assert.Equal(0, c.Blue);
    }

    [Fact]
    public void NpcShip_friend_is_SeaGreen()
    {
        var c = SpaceMapColorResolver.GetColor(SpaceObjectType.NpcShip, PlayerRelation.Friend);
        Assert.Equal(46, c.Red);
        Assert.Equal(139, c.Green);
        Assert.Equal(87, c.Blue);
    }

    [Fact]
    public void NpcShip_null_relation_defaults_to_neutral()
    {
        var c = SpaceMapColorResolver.GetColor(SpaceObjectType.NpcShip, null);
        Assert.Equal(169, c.Red);
        Assert.Equal(169, c.Green);
        Assert.Equal(169, c.Blue);
    }

    [Fact]
    public void Asteroid_is_WhiteSmoke()
    {
        var c = SpaceMapColorResolver.GetColor(SpaceObjectType.Asteroid, null);
        Assert.Equal(245, c.Red);
        Assert.Equal(245, c.Green);
        Assert.Equal(245, c.Blue);
    }

    [Fact]
    public void Container_is_Gray()
    {
        var c = SpaceMapColorResolver.GetColor(SpaceObjectType.Container, null);
        Assert.Equal(128, c.Red);
        Assert.Equal(128, c.Green);
        Assert.Equal(128, c.Blue);
    }

    [Fact]
    public void Station_is_Orange()
    {
        var c = SpaceMapColorResolver.GetColor(SpaceObjectType.Station, null);
        Assert.Equal(255, c.Red);
        Assert.Equal(165, c.Green);
        Assert.Equal(0, c.Blue);
    }

    [Fact]
    public void Missile_returns_fallback()
    {
        var c = SpaceMapColorResolver.GetColor(SpaceObjectType.Missile, null);
        Assert.Equal(Fallback, c);
    }

    [Fact]
    public void Explosion_returns_fallback()
    {
        var c = SpaceMapColorResolver.GetColor(SpaceObjectType.Explosion, null);
        Assert.Equal(Fallback, c);
    }

    [Fact]
    public void UnknownSpaceObject_returns_fallback()
    {
        var c = SpaceMapColorResolver.GetColor(SpaceObjectType.UnknownSpaceObject, null);
        Assert.Equal(Fallback, c);
    }

    [Fact]
    public void Null_type_returns_fallback()
    {
        var c = SpaceMapColorResolver.GetColor(null, null);
        Assert.Equal(Fallback, c);
    }

    [Fact]
    public void Unknown_type_returns_fallback()
    {
        var c = SpaceMapColorResolver.GetColor("SomeUnknownType", null);
        Assert.Equal(Fallback, c);
    }

    [Fact]
    public void Planet_is_WhiteSmoke()
    {
        var c = SpaceMapColorResolver.GetColor(SpaceObjectType.Planet, null);
        Assert.Equal(245, c.Red);
        Assert.Equal(245, c.Green);
        Assert.Equal(245, c.Blue);
    }

    [Fact]
    public void Sun_is_Orange()
    {
        var c = SpaceMapColorResolver.GetColor(SpaceObjectType.Sun, null);
        Assert.Equal(255, c.Red);
        Assert.Equal(165, c.Green);
        Assert.Equal(0, c.Blue);
    }

    [Fact]
    public void Fallback_is_1E2D41()
    {
        Assert.Equal(30, SpaceMapColorResolver.FallbackColor.Red);
        Assert.Equal(45, SpaceMapColorResolver.FallbackColor.Green);
        Assert.Equal(65, SpaceMapColorResolver.FallbackColor.Blue);
    }

    [Fact]
    public void All_palette_colors_are_distinct()
    {
        var colors = new HashSet<SKColor>
        {
            SpaceMapColorResolver.PlayerShipColor,
            SpaceMapColorResolver.NpcNeutralColor,
            SpaceMapColorResolver.NpcEnemyColor,
            SpaceMapColorResolver.NpcFriendColor,
            SpaceMapColorResolver.AsteroidColor,
            SpaceMapColorResolver.ContainerColor,
            SpaceMapColorResolver.StationColor,
            SpaceMapColorResolver.FallbackColor
        };

        Assert.Equal(8, colors.Count); // No duplicates
    }

    private static SKColor Fallback => new(30, 45, 65);
}
