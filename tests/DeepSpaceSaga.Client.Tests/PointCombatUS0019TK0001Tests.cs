using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0019TK0001Tests
{
    private static readonly string Source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void All_shipped_combat_scenarios_load_and_single_stays_single(bool deployed)
    {
        string root = deployed ? AppContext.BaseDirectory : Source;
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
        var entries = ScenarioRepository.ListScenarios(Path.Combine(root, "Scenarios"));
        var expectedCounts = new Dictionary<string, (int Objects, int Enemies)>
        {
            ["combat_mukha"] = (4, 3),
            ["combat_mukha_single"] = (2, 1),
            ["default"] = (4, 0),
            ["default_500"] = (502, 0),
            ["docked"] = (4, 0),
            ["market-profiles"] = (6, 0),
            ["player_ship_only"] = (2, 1),
            ["undocked"] = (4, 0)
        };
        Assert.Equal(expectedCounts.Keys.Order(StringComparer.Ordinal), entries.Select(s => s.ScenarioId).Order(StringComparer.Ordinal));
        foreach (var entry in entries)
        {
            string raw = File.ReadAllText(entry.ScenarioPath);
            Assert.DoesNotContain("autoDefenseEnabled", raw);
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(Path.Combine(Source, Path.GetRelativePath(root, entry.ScenarioPath)))), JsonNode.Parse(raw)));
            var scenario = ScenarioLoader.LoadFromFile(entry.ScenarioPath);
            Assert.Equal(expectedCounts[entry.ScenarioId].Objects, scenario.GameState.SpaceObjects.Count);
            Assert.Equal(expectedCounts[entry.ScenarioId].Enemies, scenario.GameState.SpaceObjects.Count(o => o.ObjectType == SpaceObjectType.NpcShip));
            using var engine = EngineContentLoader.CreateEngineFromScenarioFile(Path.Combine(root, "Settings.json"), entry.ScenarioPath);
            var actual = engine.CaptureSnapshotForTests();
            Assert.NotNull(engine.PlayerShipObjectId); Assert.NotNull(actual.Encounter);
            Assert.All(actual.InstalledModules.Where(m => m.Defense is not null), m => Assert.NotNull(m.Defense));
            Assert.All(scenario.GameState.SpaceObjects.SelectMany(o => o.Modules ?? []).Where(m => m.ModuleTypeId.Contains("torpedo.launcher", StringComparison.Ordinal)),
                m => Assert.True(registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex(m.ModuleTypeId)).IsPointLauncher));
        }
        var single = ScenarioLoader.LoadFromFile(Path.Combine(root, "Scenarios/CombatMukhaSingle/scenario.json"));
        var mukha = Assert.Single(single.GameState.SpaceObjects, o => o.ObjectType == SpaceObjectType.NpcShip);
        Assert.Equal("SPC-MUKHA-01", mukha.ObjectId);
        Assert.Equal(new[] { "module.engine.strizh", "module.torpedo.launcher.light" }, mukha.Modules!.Select(m => m.ModuleTypeId));
        Assert.Equal((-400d, -750d, 6000d, 180d), (mukha.PositionX, mukha.PositionY, mukha.SpeedMps, mukha.DirectionDegrees));
        Assert.Equal(new AttackAiData(25, 8, -35), mukha.Ai);
        var singlePlayer = single.GameState.SpaceObjects.Single(o => o.ObjectId == single.GameState.PlayerShipObjectId);
        Assert.Equal(9, singlePlayer.Modules!.Count);
        Assert.Equal((0d, 0d, 3000d, 0d), (singlePlayer.PositionX, singlePlayer.PositionY, singlePlayer.SpeedMps, singlePlayer.DirectionDegrees));

        var pirate = ScenarioLoader.LoadFromFile(Path.Combine(root, "Scenarios/PlayerShipOnly/scenario.json"));
        var player = pirate.GameState.SpaceObjects.Single(o => o.ObjectType == SpaceObjectType.PlayerShip);
        var enemy = Assert.Single(pirate.GameState.SpaceObjects, o => o.ObjectType == SpaceObjectType.NpcShip);
        Assert.Equal((10000d, 10000d, 700d, 0d), (player.PositionX, player.PositionY, player.SpeedMps, player.DirectionDegrees));
        Assert.Equal((0d, 5000d, 400d, 120d), (enemy.PositionX, enemy.PositionY, enemy.SpeedMps, enemy.DirectionDegrees));
        Assert.Equal((9, 8, 3, 2), (player.Modules!.Count, enemy.Modules!.Count, player.Crew!.Count, enemy.Crew!.Count));
        foreach (var ship in pirate.GameState.SpaceObjects)
        {
            Assert.Contains(ship.Modules!, m => m.ModuleTypeId == "module.engine.tetrarch");
            Assert.Contains(ship.Modules!, m => m.ModuleTypeId == "module.torpedo.launcher.basic");
        }
        Assert.Equal(new AttackAiData(25, 8, 35), enemy.Ai);
        using var control = new SimulationEngine(registry); control.LoadScenario(pirate);
        control.ReceiveCommand(new("default-tetrarch-hold", 1, player.ObjectId, "MOD-PLAYER-ENGINE-01", ShipEngineCommandTypes.TurnHold,
            TurnDirection: TurnDirection.Right, Active: true));
        var started = control.CaptureSnapshotForTests(0, simulationTimeMs: 0);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(started.CommandResults).Status);
        Assert.Equal(20, started.Objects.Single(o => o.ObjectId == player.ObjectId).TurnHoldRateDegPerSec);
        Assert.Equal(20, control.CaptureSnapshotForTests(4000, simulationTimeMs: 1000).Objects.Single(o => o.ObjectId == player.ObjectId).Direction, 8);
    }
}
