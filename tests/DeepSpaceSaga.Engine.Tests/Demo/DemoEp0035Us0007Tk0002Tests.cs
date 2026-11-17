using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0035Us0007Tk0002Tests
{
    [Theory]
    [InlineData("null")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1e999")]
    [InlineData("\"NaN\"")]
    public void InvalidRadius_Rejected(string radius)
    {
        WithModule(radius, path => Assert.Throws<ContentException>(() =>
            EngineContentLoader.LoadModuleImplementations(path, [new("module.sensor", "Sensor", 1, [])])));
    }

    [Fact]
    public void PositiveRadius_ReachesRuntimeDefinition()
    {
        WithModule("2400", path => Assert.Equal(2400, Assert.Single(
            EngineContentLoader.LoadModuleImplementations(path, [new("module.sensor", "Sensor", 1, [])])).SensorRadiusWorldUnits));
    }

    [Fact]
    public void LegacyModules_LoadWithoutSensor()
    {
        var root = DemoEp0033Us0001Tk0003Tests.Root();
        var categories = EngineContentLoader.LoadModuleCategories(root + "/Data/module-types.json");
        var modules = EngineContentLoader.LoadModuleImplementations(root + "/Data/Modules/Scanner", categories);
        Assert.NotEmpty(modules);
        Assert.All(modules, module => Assert.Null(module.SensorRadiusWorldUnits));
    }

    private static void WithModule(string radius, Action<string> check)
    {
        string root = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT")
            ?? "D:/DeepSpaceSaga/artifacts/validation/ep-0035/schema-tests";
        string directory = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "sensor.json");
            File.WriteAllText(path, "{\"moduleImplementations\":[{\"typeId\":\"sensor.test\",\"displayName\":\"Sensor\",\"type\":\"module.sensor\",\"massKg\":10,\"structurePointsMax\":10,\"powerConsumptionW\":0,\"sensorRadiusWorldUnits\":" + radius + "}]}");
            check(path);
        }
        finally { Directory.Delete(directory, true); }
    }
}
