using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class Ep0029Us0001Tk0002Tests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-ep0029-us0001-tk0002-{Guid.NewGuid():N}");
    private string SettingsPath => Path.Combine(_directory, "settings.json");
    private string ModulesPath => Path.Combine(_directory, "implementations.json");
    private string ClassesPath => Path.Combine(_directory, "ships.json");

    private const string ValidCategories = """
        {"moduleTypes":[
          {"typeId":"module.passive","displayName":"Passive","slotSize":1,"commandTypeIds":[]},
          {"typeId":"module.drone-bay","displayName":"Drone bay","slotSize":1,"commandTypeIds":[]},
          {"typeId":"living.quarters","displayName":"Living quarters","slotSize":1,"commandTypeIds":[]}
        ]}
        """;
    private const string ValidClasses = """{"shipClasses":[{"typeId":"ship.tetrarch","hullMax":400}]}""";

    public Ep0029Us0001Tk0002Tests()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "categories.json"), ValidCategories);
        File.WriteAllText(Path.Combine(_directory, "commands.json"), """{"commandDefinitions":[]}""");
        File.WriteAllText(Path.Combine(_directory, "items.json"),
            """{"itemTypes":[{"typeId":"item.ice","displayName":"Ice","unitMassKg":1}]}""");
        File.WriteAllText(ClassesPath, ValidClasses);
        File.WriteAllText(SettingsPath, """
            {"typeData":{"moduleTypes":"categories.json","moduleImplementations":"implementations.json",
              "commandDefinitions":"commands.json","itemTypes":"items.json","shipClasses":"ships.json"},
              "defaultScenario":"scenario.json"}
            """);
    }

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
    }

    private GameDataRegistry Load() => EngineContentLoader.LoadRegistryFromSettingsFile(SettingsPath, out _, out _);

    [Fact]
    public void RejectsInvalidCapacityAndFootprint()
    {
        string[] invalid =
        [
            // negative drone capacity
            """{"moduleImplementations":[{"typeId":"bay.neg","displayName":"B","type":"module.drone-bay","massKg":2500,"structurePointsMax":150,"powerConsumptionW":0,"droneCapacity":-1,"footprintWidth":1,"footprintHeight":2}]}""",
            // zero footprint width
            """{"moduleImplementations":[{"typeId":"bay.zeroW","displayName":"B","type":"module.drone-bay","massKg":2500,"structurePointsMax":150,"powerConsumptionW":0,"droneCapacity":1,"footprintWidth":0,"footprintHeight":2}]}""",
            // zero footprint height
            """{"moduleImplementations":[{"typeId":"bay.zeroH","displayName":"B","type":"module.drone-bay","massKg":2500,"structurePointsMax":150,"powerConsumptionW":0,"droneCapacity":1,"footprintWidth":1,"footprintHeight":0}]}""",
        ];

        foreach (var json in invalid)
        {
            File.WriteAllText(ModulesPath, json);
            Assert.Throws<ContentException>(() => Load());
        }
    }

    [Fact]
    public void HousingDoesNotChangeHullMax()
    {
        File.WriteAllText(ModulesPath, """
            {"moduleImplementations":[
              {"typeId":"living.quarters.four","displayName":"Living","type":"living.quarters","massKg":2000,"structurePointsMax":80,"powerConsumptionW":0,"cabines":4},
              {"typeId":"bay.m","displayName":"M","type":"module.drone-bay","massKg":4500,"structurePointsMax":250,"powerConsumptionW":0,"droneCapacity":2,"footprintWidth":2,"footprintHeight":2}
            ]}
            """);
        var registry = Load();

        var tetrarch = registry.ShipClasses.GetDefinition(registry.ShipClasses.GetIndex("ship.tetrarch"));
        Assert.Equal(400, tetrarch.HullMax);

        var living = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("living.quarters.four"));
        Assert.Equal(4, living.CabinesCount);
        // Hull is a class attribute — living quarters (four cabins) do not increase it.
        Assert.Equal(80, living.StructurePointsMax);
        Assert.Equal(400, tetrarch.HullMax);

        var bay = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("bay.m"));
        Assert.Equal(2, bay.DroneCapacity);
        Assert.Equal((2, 2), (bay.FootprintWidth, bay.FootprintHeight));
    }
}
