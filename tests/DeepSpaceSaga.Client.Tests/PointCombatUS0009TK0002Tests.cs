using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0009TK0002Tests
{
    private static readonly string Source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Shmel_scenario_is_absent_and_single_launcher_unreferenced_while_other_scenarios_load(bool deployed)
    {
        string root = deployed ? AppContext.BaseDirectory : Source;
        const string retired = "module.countermeasure.launcher.single";
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
        Assert.True(registry.ShipClasses.Contains("ship.shmel"));
        Assert.False(registry.ModuleTypes.Contains(retired));
        Assert.All(Enumerable.Range(0, registry.ModuleTypes.Count).Select(registry.ModuleTypes.GetDefinition)
            .Where(m => m.CategoryTypeId is "module.torpedo.launcher" or "module.countermeasure.launcher"), m => Assert.True(m.IsPointLauncher));
        Assert.False(File.Exists(Path.Combine(root, "Scenarios/CombatShmel/scenario.json")));
        var scenarios = ScenarioRepository.ListScenarios(Path.Combine(root, "Scenarios"));
        Assert.Equal(new[] { "combat_mukha", "combat_mukha_single", "default", "default_500", "docked", "market-profiles", "player_ship_only", "undocked" },
            scenarios.Select(s => s.ScenarioId).Order(StringComparer.Ordinal));
        foreach (var entry in scenarios)
        {
            var scenario = ScenarioLoader.LoadFromFile(entry.ScenarioPath);
            Assert.DoesNotContain(scenario.GameState.SpaceObjects.SelectMany(o => o.Modules ?? []), m => m.ModuleTypeId == retired);
            using var engine = EngineContentLoader.CreateEngineFromScenarioFile(Path.Combine(root, "Settings.json"), entry.ScenarioPath);
            Assert.NotNull(engine.PlayerShipObjectId);
            Assert.NotNull(engine.CaptureSnapshotForTests().Encounter);
            if (entry.ScenarioId == "combat_mukha_single")
                Assert.Single(scenario.GameState.SpaceObjects, o => o.ObjectType == SpaceObjectType.NpcShip);
        }
        foreach (string path in Directory.GetFiles(Path.Combine(root, "Data"), "*.json", SearchOption.AllDirectories))
            Assert.DoesNotContain(retired, File.ReadAllText(path));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(Path.Combine(Source, "Data/Ships/ship-classes.json"))),
            JsonNode.Parse(File.ReadAllText(Path.Combine(root, "Data/Ships/ship-classes.json")))));
    }
}
