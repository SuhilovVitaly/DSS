using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class SpaceMapColorResolverTests
{
    [Theory]
    [InlineData(SpaceObjectType.PlayerShip, PlayerRelation.Self, 140, 185, 224, 255)]
    [InlineData(SpaceObjectType.NpcShip, PlayerRelation.Enemy, 240, 128, 136, 255)]
    [InlineData(SpaceObjectType.NpcShip, PlayerRelation.Friend, 101, 201, 165, 255)]
    [InlineData(SpaceObjectType.NpcShip, null, 186, 196, 206, 255)]
    [InlineData(SpaceObjectType.NpcShip, PlayerRelation.Neutral, 186, 196, 206, 255)]
    [InlineData(SpaceObjectType.Asteroid, null, 165, 180, 194, 217)]
    [InlineData(SpaceObjectType.Container, null, 180, 154, 224, 230)]
    [InlineData(SpaceObjectType.Station, null, 201, 173, 125, 255)]
    [InlineData(SpaceObjectType.Planet, null, 201, 173, 125, 230)]
    [InlineData(SpaceObjectType.Sun, null, 255, 209, 138, 255)]
    [InlineData(SpaceObjectType.Missile, PlayerRelation.Enemy, 237, 184, 106, 255)]
    [InlineData(SpaceObjectType.Countermeasure, PlayerRelation.Friend, 118, 213, 220, 255)]
    [InlineData(SpaceObjectType.Explosion, null, 238, 167, 122, 255)]
    [InlineData(SpaceObjectType.UnknownSpaceObject, PlayerRelation.Enemy, 165, 175, 196, 230)]
    [InlineData(null, null, 165, 175, 196, 230)]
    [InlineData("unsupported", null, 165, 175, 196, 230)]
    public void PaletteRoleMapping(string? type, string? relation, byte r, byte g, byte b, byte a) =>
        Assert.Equal(new SKColor(r, g, b, a), SpaceMapColorResolver.GetColor(type, relation));

    [Fact]
    public void Unknown_does_not_reveal_relation() =>
        Assert.Equal(SpaceMapColorResolver.GetColor(SpaceObjectType.UnknownSpaceObject, PlayerRelation.Enemy),
            SpaceMapColorResolver.GetColor(SpaceObjectType.UnknownSpaceObject, PlayerRelation.Friend));
}
