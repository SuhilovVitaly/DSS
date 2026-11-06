using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatContentLoaderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-combat-content-{Guid.NewGuid():N}");
    private string SettingsPath => Path.Combine(_directory, "settings.json");
    private string ClassesPath => Path.Combine(_directory, "ships.json");
    private string ModulesPath => Path.Combine(_directory, "implementations.json");
    private string ScenarioPath => Path.Combine(_directory, "scenario.json");
    private const string ValidClasses = """
        {"shipClasses":[{"typeId":"ship.tetrarch"}]}
        """;
    private const string ValidModules = """
        {"moduleImplementations":[{
          "typeId":"module.passive.basic","displayName":"Passive","type":"module.passive",
          "massKg":2000,"structurePointsMax":60,"powerConsumptionW":0
        }]}
        """;

    public CombatContentLoaderTests()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(ClassesPath, ValidClasses);
        File.WriteAllText(ModulesPath, ValidModules);
        File.WriteAllText(Path.Combine(_directory, "categories.json"), """
            {"moduleTypes":[{"typeId":"module.passive","displayName":"Passive","slotSize":1,"commandTypeIds":[]}]}
            """);
        File.WriteAllText(Path.Combine(_directory, "commands.json"), """{"commandDefinitions":[]}""");
        File.WriteAllText(Path.Combine(_directory, "items.json"), """
            {"itemTypes":[{"typeId":"item.ice","displayName":"Ice","unitMassKg":1,"basePriceCredits":10}]}
            """);
        File.WriteAllText(SettingsPath, """
            {"typeData":{"moduleTypes":"categories.json","moduleImplementations":"implementations.json",
              "commandDefinitions":"commands.json","itemTypes":"items.json","shipClasses":"ships.json"},
              "defaultScenario":"scenario.json"}
            """);
        File.WriteAllText(ScenarioPath, """
            {"scenarioMetadata":{"scenarioId":"combat-loader","name":"Combat loader"},"gameState":{
              "masterSeed":42,"gameTimeMs":0,"currentSpeed":"Speed0","playerShipObjectId":"SPC-0001",
              "spaceObjects":[{"objectId":"SPC-0001","objectType":"PlayerShip","persistenceType":"Permanent",
                "name":"Tetrarch","positionX":100,"positionY":200,"speedMps":0,"directionDegrees":0,
                "movementType":"Stationary"}]}}
            """);
    }

    private GameDataRegistry Load() => EngineContentLoader.LoadRegistryFromSettingsFile(SettingsPath, out _, out _);
    private static ShipClassDefinition Ship(GameDataRegistry registry) =>
        registry.ShipClasses.GetDefinition(registry.ShipClasses.GetIndex("ship.tetrarch"));

    [Fact]
    public void Legacy_registry_without_ship_classes_is_unchanged()
    {
        var original = Load();
        var settings = JsonNode.Parse(File.ReadAllText(SettingsPath))!;
        settings["typeData"]!.AsObject().Remove("shipClasses");
        File.WriteAllText(SettingsPath, settings.ToJsonString());
        File.Delete(ClassesPath);
        var legacy = Load();
        Assert.Equal(0, legacy.ShipClasses.Count);
        Assert.Equal(0, GameDataRegistry.Empty.ShipClasses.Count);
        Assert.Equal(0, GameDataRegistry.Create([], [], [], []).ShipClasses.Count);
        Assert.Equal(original.CatalogCompatibility, legacy.CatalogCompatibility);
        var engine = EngineContentLoader.CreateEngineFromSettingsFile(SettingsPath);
        Assert.Null(Assert.Single(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0).Objects).HullCombat);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("missing.json")]
    public void Declared_ship_class_path_cannot_be_null_empty_or_missing(string? path)
    {
        var settings = JsonNode.Parse(File.ReadAllText(SettingsPath))!;
        settings["typeData"]!["shipClasses"] = path;
        File.WriteAllText(SettingsPath, settings.ToJsonString());
        var error = Assert.Throws<ContentException>(() => Load());
        Assert.Contains(string.IsNullOrWhiteSpace(path) ? SettingsPath : Path.Combine(_directory, path),
            error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"shipClasses\":null}")]
    [InlineData("{\"shipClasses\":[]}")]
    [InlineData("{\"shipClasses\":[null]}")]
    [InlineData("{\"shipClasses\":[{}]}")]
    [InlineData("{\"shipClasses\":[{\"typeId\":\"\"}]}")]
    public void Invalid_ship_class_file_is_rejected_with_path(string json)
    {
        File.WriteAllText(ClassesPath, json);
        var error = Assert.Throws<ContentException>(() => Load());
        Assert.Contains(ClassesPath, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Duplicate_class_and_module_ids_are_rejected_with_source_path()
    {
        var classes = JsonNode.Parse(ValidClasses)!;
        classes["shipClasses"]!.AsArray().Add(classes["shipClasses"]![0]!.DeepClone());
        File.WriteAllText(ClassesPath, classes.ToJsonString());
        var error = Assert.Throws<ContentException>(() => Load());
        Assert.Contains(ClassesPath, error.Message, StringComparison.Ordinal);
        Assert.Contains("duplicate", error.Message, StringComparison.Ordinal);
        File.WriteAllText(ClassesPath, ValidClasses);

        // Recursive loading must catch duplicate implementations across different files.
        string modulesDirectory = Path.Combine(_directory, "modules");
        Directory.CreateDirectory(modulesDirectory);
        File.WriteAllText(Path.Combine(modulesDirectory, "a.json"), ValidModules);
        File.WriteAllText(Path.Combine(modulesDirectory, "b.json"), ValidModules);
        var settings = JsonNode.Parse(File.ReadAllText(SettingsPath))!;
        settings["typeData"]!["moduleImplementations"] = "modules";
        File.WriteAllText(SettingsPath, settings.ToJsonString());
        error = Assert.Throws<ContentException>(() => Load());
        Assert.Contains(modulesDirectory, error.Message, StringComparison.Ordinal);
        Assert.Contains("duplicate", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void New_game_explicit_scenario_and_save_bootstrap_share_world_catalog_validation()
    {
        var engine = EngineContentLoader.CreateEngineFromSettingsFile(SettingsPath);
        string savePath = Path.Combine(_directory, "save.json");
        File.WriteAllText(savePath, ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(5000, SimulationSpeed.Speed0)));
        Assert.Equal(engine.PlayerShipObjectId,
            EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, ScenarioPath).PlayerShipObjectId);
        var restored = EngineContentLoader.CreateEngineFromSaveFile(SettingsPath, savePath);
        Assert.Equal(engine.PlayerShipObjectId, restored.PlayerShipObjectId);
        Assert.Equal(5000, restored.CaptureSaveState().GameState.GameTimeMs);

        foreach (bool invalidClass in new[] { true, false })
        {
            File.WriteAllText(ClassesPath, invalidClass ? "{\"shipClasses\":[{\"typeId\":\"ship.tetrarch\"},{\"typeId\":\"ship.tetrarch\"}]}" : ValidClasses);
            File.WriteAllText(ModulesPath, invalidClass ? ValidModules : ValidModules.Replace("\"module.passive\",", "\"module.missing\",", StringComparison.Ordinal));
            foreach (Func<SimulationEngine> bootstrap in new Func<SimulationEngine>[]
            {
                () => EngineContentLoader.CreateEngineFromSettingsFile(SettingsPath),
                () => EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, ScenarioPath),
                () => EngineContentLoader.CreateEngineFromSaveFile(SettingsPath, savePath)
            })
            {
                var error = Assert.Throws<ContentException>(() => bootstrap());
                Assert.Contains(invalidClass ? ClassesPath : ModulesPath, error.Message, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Direct_registry_creation_validates_class_ids()
    {
        Assert.Throws<ContentException>(() => GameDataRegistry.Create([], [], [], [],
            shipClasses: [new ShipClassDefinition("")]));
        Assert.Throws<ContentException>(() => GameDataRegistry.Create([], [], [], [],
            shipClasses: [new ShipClassDefinition("ship.tetrarch"), new ShipClassDefinition("ship.tetrarch")]));
        var registry = GameDataRegistry.Create([], [], [], [],
            shipClasses: [new ShipClassDefinition("ship.tetrarch")]);
        Assert.Equal("ship.tetrarch", Ship(registry).TypeId);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
