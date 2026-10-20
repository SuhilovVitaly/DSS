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

    [Theory]
    [InlineData("CombatMukha", "combat_mukha", 3)]
    [InlineData("CombatMukhaSingle", "combat_mukha_single", 1)]
    public void Mukha_scenarios_are_listed_and_load_the_current_point_catalog(string folder, string id, int enemies)
    {
        var entry = Assert.Single(ScenarioRepository.ListScenarios(Path.Combine(ClientRoot, "Scenarios")),
            item => item.ScenarioId == id);
        string path = Path.Combine(ClientRoot, "Scenarios", folder, "scenario.json");
        Assert.Equal(path, entry.ScenarioPath);
        Assert.False(string.IsNullOrWhiteSpace(entry.Name));
        Assert.False(string.IsNullOrWhiteSpace(entry.Description));
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(Path.Combine(ClientRoot, "Settings.json"), path);
        var snapshot = engine.CaptureSnapshotForTests();
        Assert.Equal(enemies + 1, snapshot.Objects.Count(o => o.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip));
        var fighting = engine.CaptureSnapshotForTests(1, simulationTimeMs: 1);
        Assert.Equal(enemies, fighting.Objects.Count(o => o.HostileCombat?.DesiredRadiusKm == 25));
        Assert.Equal(CombatEncounterState.Active, snapshot.Encounter!.State);
        Assert.Equal(SimulationSpeed.Speed0, snapshot.CurrentSpeed);
        Assert.All(snapshot.InstalledModules.Where(m => m.LauncherCombat is not null), m =>
            Assert.Equal(new[] { "slow", "medium", "fast" }, m.LauncherCombat!.Profiles!.Value.Select(p => p.ProfileId)));
        Assert.All(snapshot.InstalledModules.Where(m => m.Defense is not null), m =>
        { Assert.False(m.Defense!.AutoEnabled); Assert.Equal(new[] { "wide", "fast" }, m.Defense.Profiles!.Value.Select(p => p.ProfileId)); });
    }

    [Fact]
    public void Positions_ai_armor_and_skills_match_specification()
    {
        var scene = ScenarioLoader.LoadFromFile(ScenarioPath);
        Assert.Equal("Speed0", scene.GameState.CurrentSpeed);
        var player = scene.GameState.SpaceObjects.Single(o => o.ObjectId == scene.GameState.PlayerShipObjectId);
        Assert.Equal("ship.tetrarch", player.ShipClassId);
        Assert.Equal((0d, 0d, 3000d, 90d), (player.PositionX, player.PositionY, player.SpeedMps, player.DirectionDegrees));
        Assert.Equal(9, player.Modules!.Count);
        Assert.All(player.Modules, m => Assert.Equal("light", m.ArmorClass));
        Assert.Equal(4, player.Crew!.Count);
        Assert.Contains(player.Modules, m => m.ModuleTypeId == "living.quarters.mk1.four-cabins");
        Assert.Equal(4, player.Modules.Count(m => m.OperatorCrewId is not null));
        Assert.Contains(player.Modules, m => m.ModuleTypeId == "module.engine.tetrarch" && m.OperatorCrewId == "CHR-PLAYER-PILOT");
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(ClientRoot, "Settings.json"), out _, out _);
        Assert.Equal(4, player.Modules.Sum(m => registry.ModuleTypes.GetDefinition(
            registry.ModuleTypes.GetIndex(m.ModuleTypeId)).CabinesCount ?? 0));
        var hostiles = scene.GameState.SpaceObjects.Where(o => o.ObjectType == SpaceObjectType.NpcShip).ToArray();
        Assert.Equal(3, hostiles.Length);
        var expected = new (double X, double Y)[] { (-400, 750), (0, 850), (400, 750) };
        for (int i = 0; i < hostiles.Length; i++)
        {
            var ship = hostiles[i];
            Assert.Equal("ship.mukha", ship.ShipClassId);
            Assert.Equal(PlayerRelation.Enemy, ship.RelationToPlayer);
            Assert.Equal(expected[i].X, ship.PositionX);
            Assert.Equal(6000, ship.SpeedMps);
            Assert.Equal(270, ship.DirectionDegrees);
            var launcher = registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex("module.torpedo.launcher.light"));
            Assert.Equal(expected[i].Y, ship.PositionY);
            Assert.True(Math.Sqrt(ship.PositionX * ship.PositionX + ship.PositionY * ship.PositionY) / 10 <= 100);
            Assert.Equal(70, launcher.RangeKm); Assert.Equal(7250, launcher.ReloadMs);
            Assert.Equal(new AttackAiData(25, 8, 35), ship.Ai);
            Assert.Equal(new[] { "module.engine.strizh", "module.torpedo.launcher.light" },
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
