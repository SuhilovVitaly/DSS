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
        {"shipClasses":[{"typeId":"ship.tetrarch","hullHitPointsMax":450}]}
        """;
    private const string ValidModules = """
        {"moduleImplementations":[{
          "typeId":"module.torpedo.launcher.basic","displayName":"Launcher","type":"module.torpedo.launcher",
          "massKg":2000,"structurePointsMax":60,"powerConsumptionW":0,"baseCycleTimeMs":1000,
          "torpedoDamage":150,"torpedoSpeedKmS":3,"torpedoTurnRateDegPerSec":90
        }]}
        """;

    public CombatContentLoaderTests()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(ClassesPath, ValidClasses);
        File.WriteAllText(ModulesPath, ValidModules);
        File.WriteAllText(Path.Combine(_directory, "categories.json"), """
            {"moduleTypes":[
              {"typeId":"module.torpedo.launcher","displayName":"Launcher","slotSize":1,"commandTypeIds":["torpedo.fire"]},
              {"typeId":"module.passive","displayName":"Passive","slotSize":1,"commandTypeIds":[]}
            ]}
            """);
        File.WriteAllText(Path.Combine(_directory, "commands.json"), """
            {"commandDefinitions":[{"typeId":"torpedo.fire","displayName":"Fire","type":"module.torpedo.launcher","target":"object"}]}
            """);
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
    private static ModuleTypeDefinition Launcher(GameDataRegistry registry) =>
        registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.torpedo.launcher.basic"));
    private static JsonNode Modules() => JsonNode.Parse(ValidModules)!;
    private static JsonObject Module(JsonNode root) => root["moduleImplementations"]![0]!.AsObject();
    private void WriteModules(JsonNode root) => File.WriteAllText(ModulesPath, root.ToJsonString());

    [Fact]
    public void Combat_content_loads_ship_and_launcher_values()
    {
        var registry = Load();
        Assert.Equal(450, Ship(registry).HullHitPointsMax);
        Assert.False(registry.ShipClasses.Contains("Tetrarch"));
        Assert.False(registry.ShipClasses.Contains("SHIP.TETRARCH"));
        var launcher = Launcher(registry);
        Assert.Equal(150, launcher.TorpedoDamage);
        Assert.Equal(3, launcher.TorpedoSpeedKmS);
        Assert.Equal(90, launcher.TorpedoTurnRateDegPerSec);
        Assert.Equal(CombatCommandTypes.Fire, Assert.Single(launcher.CommandTypeIds));
        Assert.Equal(1, launcher.SlotSize);

        // A fresh load observes edits on disk, without changing the economic catalog identity.
        File.WriteAllText(ClassesPath, ValidClasses.Replace("450", "900", StringComparison.Ordinal));
        var modules = Modules();
        Module(modules)["torpedoDamage"] = 75;
        Module(modules)["torpedoSpeedKmS"] = 2.5;
        Module(modules)["torpedoTurnRateDegPerSec"] = 45.5;
        WriteModules(modules);
        var reloaded = Load();
        Assert.Equal(900, Ship(reloaded).HullHitPointsMax);
        Assert.Equal(75, Launcher(reloaded).TorpedoDamage);
        Assert.Equal(2.5, Launcher(reloaded).TorpedoSpeedKmS);
        Assert.Equal(45.5, Launcher(reloaded).TorpedoTurnRateDegPerSec);
        Assert.Equal(registry.CatalogCompatibility, reloaded.CatalogCompatibility);
        Assert.Equal(450, Ship(registry).HullHitPointsMax);
        Assert.Equal(150, Launcher(registry).TorpedoDamage);
    }

    [Theory]
    [InlineData("torpedoDamage", null)]
    [InlineData("torpedoSpeedKmS", null)]
    [InlineData("torpedoTurnRateDegPerSec", null)]
    [InlineData("torpedoDamage", "null")]
    [InlineData("torpedoSpeedKmS", "null")]
    [InlineData("torpedoTurnRateDegPerSec", "null")]
    [InlineData("torpedoDamage", "0")]
    [InlineData("torpedoDamage", "-1")]
    [InlineData("torpedoDamage", "1.5")]
    [InlineData("torpedoDamage", "2147483648")]
    [InlineData("torpedoSpeedKmS", "0")]
    [InlineData("torpedoSpeedKmS", "-1")]
    [InlineData("torpedoSpeedKmS", "1e999")]
    [InlineData("torpedoSpeedKmS", "\"NaN\"")]
    [InlineData("torpedoSpeedKmS", "\"Infinity\"")]
    [InlineData("torpedoTurnRateDegPerSec", "0")]
    [InlineData("torpedoTurnRateDegPerSec", "-1")]
    [InlineData("torpedoTurnRateDegPerSec", "1e999")]
    [InlineData("torpedoTurnRateDegPerSec", "\"NaN\"")]
    [InlineData("torpedoTurnRateDegPerSec", "\"-Infinity\"")]
    public void Partial_nonfinite_or_nonpositive_weapon_config_is_rejected(string field, string? jsonValue)
    {
        var root = Modules();
        if (jsonValue is null) Module(root).Remove(field);
        else Module(root)[field] = JsonNode.Parse(jsonValue);
        WriteModules(root);

        var error = Assert.Throws<ContentException>(() => Load());
        Assert.Contains(ModulesPath, error.Message, StringComparison.Ordinal);
        Assert.Contains(field, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Launcher_without_any_weapon_parameters_is_rejected()
    {
        var root = Modules();
        foreach (var field in new[] { "torpedoDamage", "torpedoSpeedKmS", "torpedoTurnRateDegPerSec" })
            Module(root).Remove(field);
        WriteModules(root);
        var error = Assert.Throws<ContentException>(() => Load());
        Assert.Contains(ModulesPath, error.Message, StringComparison.Ordinal);
        Assert.Contains("torpedoDamage", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Weapon_parameters_on_non_launcher_category_are_rejected()
    {
        var root = Modules();
        Module(root)["type"] = "module.passive";
        WriteModules(root);
        var error = Assert.Throws<ContentException>(() => Load());
        Assert.Contains(ModulesPath, error.Message, StringComparison.Ordinal);
        Assert.Contains("module.torpedo.launcher", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Legacy_registry_without_ship_classes_is_unchanged()
    {
        var original = Load();
        var settings = JsonNode.Parse(File.ReadAllText(SettingsPath))!;
        settings["typeData"]!.AsObject().Remove("shipClasses");
        File.WriteAllText(SettingsPath, settings.ToJsonString());
        File.Delete(ClassesPath);
        var root = Modules();
        Module(root)["type"] = "module.passive";
        foreach (var field in new[] { "torpedoDamage", "torpedoSpeedKmS", "torpedoTurnRateDegPerSec" })
            Module(root).Remove(field);
        WriteModules(root);

        var legacy = Load();
        Assert.Equal(0, legacy.ShipClasses.Count);
        Assert.Equal(0, GameDataRegistry.Empty.ShipClasses.Count);
        Assert.Equal(0, GameDataRegistry.Create([], [], [], []).ShipClasses.Count);
        Assert.Null(Launcher(legacy).TorpedoDamage);
        Assert.Null(Launcher(legacy).TorpedoSpeedKmS);
        Assert.Null(Launcher(legacy).TorpedoTurnRateDegPerSec);
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
    [InlineData("{\"shipClasses\":[{\"typeId\":\"ship.tetrarch\"}]}")]
    [InlineData("{\"shipClasses\":[{\"typeId\":\"\",\"hullHitPointsMax\":450}]}")]
    [InlineData("{\"shipClasses\":[{\"typeId\":\"ship.tetrarch\",\"hullHitPointsMax\":0}]}")]
    [InlineData("{\"shipClasses\":[{\"typeId\":\"ship.tetrarch\",\"hullHitPointsMax\":-1}]}")]
    [InlineData("{\"shipClasses\":[{\"typeId\":\"ship.tetrarch\",\"hullHitPointsMax\":2147483648}]}")]
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
    public void New_game_explicit_scenario_and_save_bootstrap_share_combat_validation()
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
            File.WriteAllText(ClassesPath, invalidClass ? ValidClasses.Replace("450", "0", StringComparison.Ordinal) : ValidClasses);
            File.WriteAllText(ModulesPath, invalidClass ? ValidModules : ValidModules.Replace("\"torpedoSpeedKmS\":3", "\"torpedoSpeedKmS\":0", StringComparison.Ordinal));
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
    public void Direct_registry_creation_validates_class_hp_and_ids()
    {
        Assert.Throws<ContentException>(() => GameDataRegistry.Create([], [], [], [],
            shipClasses: [new ShipClassDefinition("ship.tetrarch", 0)]));
        Assert.Throws<ContentException>(() => GameDataRegistry.Create([], [], [], [],
            shipClasses: [new ShipClassDefinition("ship.tetrarch", 450), new ShipClassDefinition("ship.tetrarch", 900)]));
        var registry = GameDataRegistry.Create([], [], [], [],
            shipClasses: [new ShipClassDefinition("ship.tetrarch", int.MaxValue)]);
        Assert.Equal(int.MaxValue, Ship(registry).HullHitPointsMax);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
