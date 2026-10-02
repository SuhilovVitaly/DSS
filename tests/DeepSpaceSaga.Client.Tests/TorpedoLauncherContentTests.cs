using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class TorpedoLauncherContentTests
{
    private const string CategoryId = "module.torpedo.launcher";
    private const string LauncherId = "module.torpedo.launcher.basic";
    private const string ModulePath = "Data/Modules/TorpedoLauncher/modules-torpedolauncher.json";
    private const string CommandPath = "Data/Commands/TorpedoLauncher/commands.json";
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    private static string Root(bool output) => output ? AppContext.BaseDirectory : ClientRoot;
    private static GameDataRegistry Load(string root) =>
        EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
    private static ModuleTypeDefinition Launcher(GameDataRegistry registry) =>
        registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex(LauncherId));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Real_launcher_content_exposes_one_targeted_command(bool output)
    {
        var registry = Load(Root(output));
        var category = registry.ModuleCategories.GetDefinition(registry.ModuleCategories.GetIndex(CategoryId));
        var launcher = Launcher(registry);
        Assert.Equal(1, category.SlotSize);
        Assert.Equal(1, launcher.SlotSize);
        Assert.Equal(CombatCommandTypes.Fire, Assert.Single(category.CommandTypeIds));
        Assert.Equal(CombatCommandTypes.Fire, Assert.Single(launcher.CommandTypeIds));
        var command = registry.CommandDefinitions.GetDefinition(registry.CommandDefinitions.GetIndex(CombatCommandTypes.Fire));
        Assert.Equal(CategoryId, command.Type);
        Assert.Equal("object", command.Target);
        Assert.Single(Enumerable.Range(0, registry.CommandDefinitions.Count)
            .Select(registry.CommandDefinitions.GetDefinition), c => c.Type == CategoryId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Launcher_parameters_are_150_3_90_and_no_ammo_cost(bool output)
    {
        var registry = Load(Root(output));
        var launcher = Launcher(registry);
        Assert.Equal(150, launcher.TorpedoDamage);
        Assert.Equal(3, launcher.TorpedoSpeedKmS);
        Assert.Equal(90, launcher.TorpedoTurnRateDegPerSec);
        Assert.Equal(2000, launcher.MassKg);
        Assert.Equal(60, launcher.StructurePointsMax);
        Assert.Equal(0, launcher.PowerConsumptionW);
        Assert.Null(launcher.FuelCapacityKg);
        Assert.Null(launcher.CargoCapacityKg);
        Assert.Equal(100, launcher.BaseSuccessChancePercent);
        // Required positive loader metadata (epic A10), not a combat cooldown.
        Assert.Equal(1000, launcher.BaseCycleTimeMs);

        var command = registry.CommandDefinitions.GetDefinition(registry.CommandDefinitions.GetIndex(CombatCommandTypes.Fire));
        Assert.Equal(0, command.ActivationEnergyCellsCost);
        Assert.Equal(CommandDefinition.Neutral, command.TimeFactor);
        Assert.Equal(CommandDefinition.Neutral, command.ComplexityFactor);
        Assert.Equal(CommandDefinition.Neutral, command.ConsumptionFactor);
        Assert.Null(command.RangeKm);
        Assert.Null(command.TrailDistanceKm);
    }

    [Fact]
    public void Shipped_launcher_json_matches_source_and_loads_from_output_settings()
    {
        foreach (string relativePath in new[] { "Data/module-types.json", ModulePath, CommandPath })
        {
            string outputPath = Path.Combine(AppContext.BaseDirectory, relativePath);
            Assert.True(File.Exists(outputPath), $"Missing shipped content: {outputPath}");
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(Path.Combine(ClientRoot, relativePath))),
                JsonNode.Parse(File.ReadAllText(outputPath))), $"Shipped content differs: {relativePath}");
        }
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(Path.Combine(AppContext.BaseDirectory, "Settings.json"));
        Assert.NotNull(engine.PlayerShipObjectId);
    }

    [Fact]
    public void Fresh_load_reads_edited_weapon_values_from_temporary_content_copy()
    {
        using var temporary = new TemporaryContent();
        File.Copy(Path.Combine(ClientRoot, "Settings.json"), Path.Combine(temporary.Path, "Settings.json"));
        foreach (string file in Directory.EnumerateFiles(Path.Combine(ClientRoot, "Data"), "*.json", SearchOption.AllDirectories))
        {
            string destination = Path.Combine(temporary.Path, Path.GetRelativePath(ClientRoot, file));
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }
        var original = Load(temporary.Path);
        string modulePath = Path.Combine(temporary.Path, ModulePath);
        var json = JsonNode.Parse(File.ReadAllText(modulePath))!;
        var module = json["moduleImplementations"]![0]!;
        module["torpedoDamage"] = 175;
        module["torpedoSpeedKmS"] = 4.5;
        module["torpedoTurnRateDegPerSec"] = 60;
        File.WriteAllText(modulePath, json.ToJsonString());

        var reloaded = Load(temporary.Path);
        Assert.Equal(175, Launcher(reloaded).TorpedoDamage);
        Assert.Equal(4.5, Launcher(reloaded).TorpedoSpeedKmS);
        Assert.Equal(60, Launcher(reloaded).TorpedoTurnRateDegPerSec);
        Assert.Equal(original.CatalogCompatibility, reloaded.CatalogCompatibility);
        Assert.Equal(150, Launcher(original).TorpedoDamage);
        Assert.Equal(150, Launcher(Load(ClientRoot)).TorpedoDamage);
    }

    [Fact]
    public void Fire_metadata_does_not_start_generic_module_cycle_or_consume_rng()
    {
        using var temporary = new TemporaryContent();
        // Install only in a temporary scenario fixture; production loadouts belong to later tickets.
        var scenario = JsonNode.Parse(File.ReadAllText(Path.Combine(ClientRoot, "Scenarios/Default/scenario.json")))!;
        var state = scenario["gameState"]!;
        string playerId = state["playerShipObjectId"]!.GetValue<string>();
        var player = state["spaceObjects"]!.AsArray().Single(o => o!["objectId"]!.GetValue<string>() == playerId)!;
        var modules = player["modules"]!.AsArray();
        var launcher = modules.FirstOrDefault(m => m!["moduleTypeId"]!.GetValue<string>() == LauncherId);
        if (launcher is null)
        {
            player["hullLayout"]!["cells"]!.AsArray().Add(new JsonObject { ["x"] = 3, ["y"] = 2 });
            launcher = JsonNode.Parse("""
                {"moduleId":"MOD-TEST-LAUNCHER","moduleTypeId":"module.torpedo.launcher.basic",
                 "occupiedCells":[{"x":3,"y":2}],"structurePoints":60,"powerState":"On","operationalState":"Ready"}
                """)!;
            modules.Add(launcher);
        }
        string launcherModuleId = launcher["moduleId"]!.GetValue<string>();
        string scenarioPath = Path.Combine(temporary.Path, "scenario.json");
        File.WriteAllText(scenarioPath, scenario.ToJsonString());
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(Path.Combine(ClientRoot, "Settings.json"), scenarioPath);
        var initial = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0);
        string targetId = initial.Objects.First(o => o.ObjectId != playerId).ObjectId;
        var before = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0).GameState;

        engine.ReceiveCommand(new PlayerCommand("test-fire", 1, playerId, launcherModuleId, CombatCommandTypes.Fire, targetId));
        var snapshot = engine.CaptureSnapshotForTests(1000, SimulationSpeed.Speed1);
        Assert.Contains(snapshot.CommandResults, result => result.CommandId == "test-fire");
        var after = engine.CaptureSaveStateForTests(1000, SimulationSpeed.Speed0).GameState;
        var installed = after.SpaceObjects.Single(o => o.ObjectId == playerId).Modules!.Single(m => m.ModuleId == launcherModuleId);
        Assert.Null(installed.ActiveCycle);
        Assert.Equal(60, installed.StructurePoints);
        Assert.Null(installed.FuelAmountKg);
        Assert.True(installed.Cargo is null || installed.Cargo.Count == 0);
        Assert.Equal(JsonSerializer.Serialize(before.TradingMap!.RngStreams), JsonSerializer.Serialize(after.TradingMap!.RngStreams));
        Assert.Equal(JsonSerializer.Serialize(before.StationResourceFields!.RngStreams), JsonSerializer.Serialize(after.StationResourceFields!.RngStreams));
    }

    private sealed class TemporaryContent : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("dss-launcher-content-").FullName;
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
