using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatScenariosContentTests
{
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
    private static readonly string ScenarioPath = Path.Combine(ClientRoot, "Scenarios", "CombatMukha", "scenario.json");

    [Fact]
    public void Mukha_scenario_is_listed_and_loads()
    {
        var entry = Assert.Single(ScenarioRepository.ListScenarios(Path.Combine(ClientRoot, "Scenarios")),
            item => item.ScenarioId == "combat_mukha");
        Assert.Equal(ScenarioPath, entry.ScenarioPath);
        Assert.False(string.IsNullOrWhiteSpace(entry.Name));
        Assert.False(string.IsNullOrWhiteSpace(entry.Description));
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(Path.Combine(ClientRoot, "Settings.json"), ScenarioPath);
        var snapshot = engine.CaptureSnapshotForTests();
        Assert.Equal(4, snapshot.Objects.Count(o => o.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip));
        var fighting = engine.CaptureSnapshotForTests(1, simulationTimeMs: 1);
        Assert.Equal(3, fighting.Objects.Count(o => o.HostileCombat?.DesiredRadiusKm == 50));
    }

    [Fact]
    public void Positions_ai_armor_and_skills_match_specification()
    {
        var scene = ScenarioLoader.LoadFromFile(ScenarioPath);
        Assert.Equal("Speed0", scene.GameState.CurrentSpeed);
        var player = scene.GameState.SpaceObjects.Single(o => o.ObjectId == scene.GameState.PlayerShipObjectId);
        Assert.Equal("ship.tetrarch", player.ShipClassId);
        Assert.Equal((0d, 0d, 2000d, 0d), (player.PositionX, player.PositionY, player.SpeedMps, player.DirectionDegrees));
        Assert.Equal(9, player.Modules!.Count);
        Assert.All(player.Modules, m => Assert.Equal("light", m.ArmorClass));
        Assert.Equal(4, player.Crew!.Count);
        Assert.Contains(player.Modules, m => m.ModuleTypeId == "living.quarters.mk1.four-cabins");
        Assert.Equal(4, player.Modules.Count(m => m.OperatorCrewId is not null));
        Assert.Contains(player.Modules, m => m.ModuleTypeId == "module.engine.basic" && m.OperatorCrewId == "CHR-PLAYER-PILOT");
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(ClientRoot, "Settings.json"), out _, out _);
        Assert.Equal(4, player.Modules.Sum(m => registry.ModuleTypes.GetDefinition(
            registry.ModuleTypes.GetIndex(m.ModuleTypeId)).CabinesCount ?? 0));
        var hostiles = scene.GameState.SpaceObjects.Where(o => o.ObjectType == SpaceObjectType.NpcShip).ToArray();
        Assert.Equal(3, hostiles.Length);
        var expected = new (double X, double Y, double Flank)[] { (-400, 1500, -35), (0, 1650, 0), (400, 1500, 35) };
        for (int i = 0; i < hostiles.Length; i++)
        {
            var ship = hostiles[i];
            Assert.Equal("ship.mukha", ship.ShipClassId);
            Assert.Equal(PlayerRelation.Enemy, ship.RelationToPlayer);
            Assert.Equal((expected[i].X, expected[i].Y, 3000d, 180d),
                (ship.PositionX, ship.PositionY, ship.SpeedMps, ship.DirectionDegrees));
            Assert.Equal(new AttackAiData(50, 10, expected[i].Flank), ship.Ai);
            Assert.Equal(new[] { "module.engine.strizh", "module.torpedo.launcher.basic" },
                ship.Modules!.Select(m => m.ModuleTypeId));
            Assert.All(ship.Modules!, m => Assert.Equal("none", m.ArmorClass));
            Assert.Equal(2, ship.Crew!.Count);
        }
        Assert.All(scene.GameState.SpaceObjects.SelectMany(o => o.Crew ?? []), crew =>
        {
            Assert.Equal(20, crew.AccuracySkill);
            Assert.Equal(20, crew.ManeuverabilitySkill);
        });
    }
}
