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
    public void Real_launcher_content_exposes_point_launch_and_owned_detonation(bool output)
    {
        var registry = Load(Root(output));
        var category = registry.ModuleCategories.GetDefinition(registry.ModuleCategories.GetIndex(CategoryId));
        var launcher = Launcher(registry);
        Assert.Equal(1, category.SlotSize);
        Assert.Equal(1, launcher.SlotSize);
        Assert.Equal(new[] { CombatCommandTypes.Launch, CombatCommandTypes.Detonate }, category.CommandTypeIds);
        Assert.Equal(new[] { CombatCommandTypes.Launch, CombatCommandTypes.Detonate }, launcher.CommandTypeIds);
        var command = registry.CommandDefinitions.GetDefinition(registry.CommandDefinitions.GetIndex(CombatCommandTypes.Launch));
        Assert.Equal(CategoryId, command.Type);
        Assert.Equal("point", command.Target);
        Assert.Equal(2, Enumerable.Range(0, registry.CommandDefinitions.Count)
            .Select(registry.CommandDefinitions.GetDefinition).Count(c => c.Type == CategoryId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Launcher_catalog_matches_ep12(bool output)
    {
        var registry = Load(Root(output));
        var launcher = Launcher(registry);
        Assert.Equal(200, launcher.TorpedoDamage);
        Assert.Null(launcher.TorpedoSpeedKmS);
        Assert.Equal(new[] { 6d, 10d, 16d }, launcher.Profiles.Select(p => p.SpeedKmS));
        Assert.Equal(new[] { 9d, 5d, 2.5 }, launcher.Profiles.Select(p => p.BlastRadiusKm));
        Assert.Null(launcher.TorpedoTurnRateDegPerSec);
        Assert.Null(launcher.TorpedoManeuverability);
        Assert.Null(launcher.TorpedoRangeKm);
        Assert.Equal(70, launcher.RangeKm);
        Assert.Null(launcher.Accuracy);
        Assert.Equal(.02, launcher.ScatterPercent);
        Assert.Equal(20m, launcher.ArmorPiercingPercent);
        Assert.Equal(100m, launcher.ArmorDamagePercent);
        Assert.Null(launcher.LauncherReloadMs);
        Assert.Equal(10000, launcher.ReloadMs);
        Assert.Null(launcher.TorpedoMaxFlightTimeMs);
        Assert.Equal(2000, launcher.MassKg);
        Assert.Equal(150, launcher.StructurePointsMax);
        Assert.Equal(0, launcher.PowerConsumptionW);
        Assert.Null(launcher.FuelCapacityKg);
        Assert.Null(launcher.CargoCapacityKg);
        Assert.Equal(100, launcher.BaseSuccessChancePercent);
        // Required positive loader metadata (epic A10), not a combat cooldown.
        Assert.Equal(1000, launcher.BaseCycleTimeMs);

        var command = registry.CommandDefinitions.GetDefinition(registry.CommandDefinitions.GetIndex(CombatCommandTypes.Launch));
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
        module["profiles"]![0]!["speedKmS"] = 4.5;
        module["profiles"]![0]!["blastRadiusKm"] = 6;
        File.WriteAllText(modulePath, json.ToJsonString());

        var reloaded = Load(temporary.Path);
        Assert.Equal(175, Launcher(reloaded).TorpedoDamage);
        Assert.Equal(4.5, Launcher(reloaded).Profiles[0].SpeedKmS);
        Assert.Equal(6, Launcher(reloaded).Profiles[0].BlastRadiusKm);
        Assert.Equal(original.CatalogCompatibility, reloaded.CatalogCompatibility);
        Assert.Equal(200, Launcher(original).TorpedoDamage);
        Assert.Equal(200, Launcher(Load(ClientRoot)).TorpedoDamage);
    }

    private sealed class TemporaryContent : IDisposable
    {
        public string Path { get; } = Directory.CreateTempSubdirectory("dss-launcher-content-").FullName;
        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
