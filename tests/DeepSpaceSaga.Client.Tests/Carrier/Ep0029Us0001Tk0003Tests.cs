using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Client.Tests;

public sealed class Ep0029Us0001Tk0003Tests
{
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    private static GameDataRegistry Load() => EngineContentLoader.LoadRegistryFromSettingsFile(
        Path.Combine(ClientRoot, "Settings.json"), out _, out _);

    [Fact]
    public void TwoMediumBaysHaveFourPlaces()
    {
        var registry = Load();

        var medium = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.drone-bay.medium"));
        Assert.Equal(2, medium.DroneCapacity);
        Assert.Equal((2, 2), (medium.FootprintWidth, medium.FootprintHeight));
        // Two medium bays => 2 * 2 = 4 drone places.
        Assert.Equal(4, medium.DroneCapacity * 2);

        var small = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.drone-bay.small"));
        Assert.Equal(1, small.DroneCapacity);
        Assert.Equal((1, 2), (small.FootprintWidth, small.FootprintHeight));
    }

    [Fact]
    public void ClassProfileHasIndependentHull()
    {
        var registry = Load();

        var tetrarch = registry.ShipClasses.GetDefinition(registry.ShipClasses.GetIndex("ship.tetrarch"));
        Assert.Equal(400, tetrarch.HullMax);
        Assert.Equal(35, tetrarch.TacticalManeuver);
        Assert.Equal("carrier", tetrarch.AppearanceId);

        // Living quarters contribute cabins, not hull strength.
        var living = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("living.quarters.mk1.four-cabins"));
        Assert.Equal(4, living.CabinesCount);
        Assert.Equal(400, tetrarch.HullMax);
    }
}
