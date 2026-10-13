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
          "typeId":"module.torpedo.launcher.basic","displayName":"Launcher","type":"module.torpedo.launcher",
          "massKg":2000,"structurePointsMax":60,"powerConsumptionW":0,"baseCycleTimeMs":1000,
          "torpedoDamage":150,"torpedoSpeedKmS":3,"torpedoTurnRateDegPerSec":90,
          "torpedoManeuverability":5,"torpedoRangeKm":200
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
              {"typeId":"module.passive","displayName":"Passive","slotSize":1,"commandTypeIds":[]},
              {"typeId":"module.countermeasure.launcher","displayName":"Defense","slotSize":1,"commandTypeIds":[]},
              {"typeId":"module.engine","displayName":"Engine","slotSize":1,"commandTypeIds":[]}
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
    public void Launcher_new_fields_validated_for_launcher_only()
    {
        var root = Modules();
        var module = Module(root);
        module["accuracy"] = 40;
        module["armorPiercingPercent"] = 20;
        module["armorDamagePercent"] = 100;
        module["launcherReloadMs"] = 15000;
        module["torpedoMaxFlightTimeMs"] = 120000;
        WriteModules(root);
        var launcher = Launcher(Load());
        Assert.Equal(40m, launcher.Accuracy);
        Assert.Equal(20m, launcher.ArmorPiercingPercent);
        Assert.Equal(100m, launcher.ArmorDamagePercent);
        Assert.Equal(15000, launcher.LauncherReloadMs);
        Assert.Equal(120000, launcher.TorpedoMaxFlightTimeMs);

        foreach (string field in new[] { "accuracy", "armorPiercingPercent", "armorDamagePercent", "launcherReloadMs", "torpedoMaxFlightTimeMs" })
        {
            var nonLauncher = Modules();
            var other = Module(nonLauncher);
            other["type"] = "module.passive";
            foreach (string oldField in new[] { "torpedoDamage", "torpedoSpeedKmS", "torpedoTurnRateDegPerSec", "torpedoManeuverability", "torpedoRangeKm" })
                other.Remove(oldField);
            other[field] = module[field]!.DeepClone();
            WriteModules(nonLauncher);
            var error = Assert.Throws<ContentException>(() => Load());
            Assert.Contains(ModulesPath, error.Message, StringComparison.Ordinal);
            Assert.Contains(field, error.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Barrels_zero_rejected()
    {
        var root = Modules();
        var module = Module(root);
        module["type"] = "module.countermeasure.launcher";
        foreach (string field in new[] { "torpedoDamage", "torpedoSpeedKmS", "torpedoTurnRateDegPerSec", "torpedoManeuverability", "torpedoRangeKm" })
            module.Remove(field);
        module["countermeasureSpeedKmS"] = 20;
        module["countermeasureTurnRateDegPerSec"] = 90;
        module["countermeasureRangeKm"] = 150;
        module["countermeasureReloadMs"] = 14500;
        module["countermeasureAccuracy"] = 58;
        module["countermeasureManualRangeKm"] = 150;
        module["countermeasureMaxFlightTimeMs"] = 30000;
        module["barrels"] = 2;
        module["burstIntervalMs"] = 2000;
        WriteModules(root);
        var loaded = Launcher(Load());
        Assert.Equal(2, loaded.Barrels);
        Assert.Equal(2000, loaded.BurstIntervalMs);
        module["barrels"] = 0;
        WriteModules(root);
        Assert.Contains("barrels", Assert.Throws<ContentException>(() => Load()).Message, StringComparison.Ordinal);
        module["barrels"] = 1;
        module["burstIntervalMs"] = -1;
        WriteModules(root);
        Assert.Contains("burstIntervalMs", Assert.Throws<ContentException>(() => Load()).Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("armorPiercingPercent", 101)]
    [InlineData("armorDamagePercent", 0)]
    [InlineData("launcherReloadMs", 0)]
    [InlineData("torpedoMaxFlightTimeMs", 0)]
    [InlineData("accuracy", 101)]
    public void Piercing_over_100_rejected(string field, int value)
    {
        var root = Modules();
        Module(root)[field] = value;
        WriteModules(root);
        var error = Assert.Throws<ContentException>(() => Load());
        Assert.Contains(ModulesPath, error.Message, StringComparison.Ordinal);
        Assert.Contains(field, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Engine_maneuverability_negative_rejected()
    {
        var root = Modules();
        var module = Module(root);
        module["type"] = "module.engine";
        foreach (string field in new[] { "torpedoDamage", "torpedoSpeedKmS", "torpedoTurnRateDegPerSec", "torpedoManeuverability", "torpedoRangeKm" })
            module.Remove(field);
        module["maxSpeedMps"] = 6000;
        module["turnStepDegrees"] = 45;
        module["linearInertiaMps2"] = 100;
        module["angularInertiaDegPerSec"] = 90;
        module["fuelCapacityKg"] = 1000;
        module["maneuverability"] = 10;
        WriteModules(root);
        Assert.Equal(10m, Launcher(Load()).Maneuverability);
        module["maneuverability"] = -1;
        WriteModules(root);
        Assert.Contains("maneuverability", Assert.Throws<ContentException>(() => Load()).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Combat_role_rejects_unknown_values()
    {
        var root = Modules();
        Module(root)["combatRole"] = "bridge";
        WriteModules(root);
        Assert.Equal("bridge", Launcher(Load()).CombatRole);
        Module(root)["combatRole"] = "unknown";
        WriteModules(root);
        var error = Assert.Throws<ContentException>(() => Load());
        Assert.Contains(ModulesPath, error.Message, StringComparison.Ordinal);
        Assert.Contains("combatRole", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Combat_content_loads_ship_and_launcher_values()
    {
        var registry = Load();
        Assert.Equal("ship.tetrarch", Ship(registry).TypeId);
        Assert.False(registry.ShipClasses.Contains("Tetrarch"));
        Assert.False(registry.ShipClasses.Contains("SHIP.TETRARCH"));
        var launcher = Launcher(registry);
        Assert.Equal(150, launcher.TorpedoDamage);
        Assert.Equal(3, launcher.TorpedoSpeedKmS);
        Assert.Equal(90, launcher.TorpedoTurnRateDegPerSec);
        Assert.Equal(5m, launcher.TorpedoManeuverability);
        Assert.Equal(200, launcher.TorpedoRangeKm);
        Assert.Equal(CombatCommandTypes.Fire, Assert.Single(launcher.CommandTypeIds));
        Assert.Equal(1, launcher.SlotSize);

        // A fresh load observes edits on disk, without changing the economic catalog identity.
        File.WriteAllText(ClassesPath, """{"shipClasses":[{"typeId":"ship.tetrarch"},{"typeId":"ship.mukha"}]}""");
        var modules = Modules();
        Module(modules)["torpedoDamage"] = 75;
        Module(modules)["torpedoSpeedKmS"] = 2.5;
        Module(modules)["torpedoTurnRateDegPerSec"] = 45.5;
        WriteModules(modules);
        var reloaded = Load();
        Assert.True(reloaded.ShipClasses.Contains("ship.mukha"));
        Assert.Equal(75, Launcher(reloaded).TorpedoDamage);
        Assert.Equal(2.5, Launcher(reloaded).TorpedoSpeedKmS);
        Assert.Equal(45.5, Launcher(reloaded).TorpedoTurnRateDegPerSec);
        Assert.Equal(registry.CatalogCompatibility, reloaded.CatalogCompatibility);
        Assert.False(registry.ShipClasses.Contains("ship.mukha"));
        Assert.Equal(150, Launcher(registry).TorpedoDamage);
    }

    [Theory]
    [InlineData("torpedoDamage", null)]
    [InlineData("torpedoManeuverability", null)]
    [InlineData("torpedoManeuverability", "null")]
    [InlineData("torpedoManeuverability", "-0.1")]
    [InlineData("torpedoManeuverability", "100.1")]
    [InlineData("torpedoManeuverability", "1e999")]
    [InlineData("torpedoRangeKm", null)]
    [InlineData("torpedoRangeKm", "null")]
    [InlineData("torpedoRangeKm", "0")]
    [InlineData("torpedoRangeKm", "-1")]
    [InlineData("torpedoRangeKm", "1e999")]
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
        foreach (var field in new[] { "torpedoDamage", "torpedoSpeedKmS", "torpedoTurnRateDegPerSec", "torpedoManeuverability", "torpedoRangeKm" })
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
            File.WriteAllText(ClassesPath, invalidClass ? "{\"shipClasses\":[{\"typeId\":\"ship.tetrarch\"},{\"typeId\":\"ship.tetrarch\"}]}" : ValidClasses);
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

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Weapon_schema_rejects_invalid_values(int validManeuverability)
    {
        var root = Modules();
        Module(root)["torpedoManeuverability"] = validManeuverability;
        WriteModules(root);
        var module = Launcher(Load());
        Assert.Equal((decimal)validManeuverability, module.TorpedoManeuverability);
        foreach (var invalid in new[]
        {
            module with { TorpedoManeuverability = null },
            module with { TorpedoManeuverability = -1 },
            module with { TorpedoManeuverability = 100.1m },
            module with { TorpedoRangeKm = double.NaN },
            module with { TorpedoRangeKm = double.PositiveInfinity },
            module with { TorpedoRangeKm = 0 }
        })
            Assert.Throws<ContentException>(() => GameDataRegistry.Create([], [invalid], [], []));
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
