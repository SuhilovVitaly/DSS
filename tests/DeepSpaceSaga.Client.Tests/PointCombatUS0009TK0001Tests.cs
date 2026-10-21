using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0009TK0001Tests
{
    private static readonly string Source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
    private const string Relative = "Scenarios/CombatMukha/scenario.json";
    private static AuthoritativeSnapshot At(SimulationEngine engine, long time = 0) => engine.CaptureSnapshotForTests(time * 4, simulationTimeMs: time);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Mukha_v3_loads_and_is_discoverable_with_exact_approved_geometry_and_catalog(bool deployed)
    {
        string root = deployed ? AppContext.BaseDirectory : Source;
        string path = Path.Combine(root, Relative);
        var entry = Assert.Single(ScenarioRepository.ListScenarios(Path.Combine(root, "Scenarios")), s => s.ScenarioId == "combat_mukha");
        Assert.Equal(Path.GetFullPath(path), entry.ScenarioPath); Assert.False(string.IsNullOrWhiteSpace(entry.Name));
        string raw = File.ReadAllText(path);
        Assert.DoesNotContain("autoDefenseEnabled", raw);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(Path.Combine(Source, Relative))), JsonNode.Parse(raw)));
        var scenario = ScenarioLoader.LoadFromFile(path);
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
        var player = Assert.Single(scenario.GameState.SpaceObjects, o => o.ObjectId == scenario.GameState.PlayerShipObjectId);
        Assert.Equal("Speed0", scenario.GameState.CurrentSpeed);
        Assert.Equal("ship.tetrarch", player.ShipClassId);
        Assert.Equal((0d, 0d, 3000d, 0d), (player.PositionX, player.PositionY, player.SpeedMps, player.DirectionDegrees));
        string[] types = ["module.bridge.navigation.computer.basic", "living.quarters.mk1.four-cabins", "module.container.basic",
            "module.scanner.mk1", "module.generator.basic", "module.engine.tetrarch", "module.torpedo.launcher.basic",
            "module.countermeasure.launcher.basic", "module.countermeasure.launcher.basic"];
        Assert.Equal(types, player.Modules!.Select(m => m.ModuleTypeId));
        Assert.Equal(new long[] { 4000, 2000, 20000, 2000, 12000, 5000, 2000, 2000, 2000 },
            player.Modules!.Select(m => registry.ModuleTypes.GetDefinition(registry.ModuleTypes.GetIndex(m.ModuleTypeId)).MassKg));
        Assert.All(player.Modules!, m => Assert.Equal("light", m.ArmorClass));
        Assert.Equal(4, player.Crew!.Count);
        Assert.Equal(4, player.Modules!.Where(m => m.OperatorCrewId is not null).Select(m => m.OperatorCrewId).Distinct().Count());
        var enemies = scenario.GameState.SpaceObjects.Where(o => o.ObjectType == SpaceObjectType.NpcShip).ToArray();
        Assert.Equal(3, enemies.Length);
        (double X, double Y)[] positions = [(-400, -750), (0, -850), (400, -750)];
        for (int i = 0; i < enemies.Length; i++)
        {
            var enemy = enemies[i];
            Assert.Equal($"SPC-MUKHA-0{i + 1}", enemy.ObjectId);
            Assert.Equal("ship.mukha", enemy.ShipClassId); Assert.Equal(PlayerRelation.Enemy, enemy.RelationToPlayer);
            Assert.Equal((positions[i].X, positions[i].Y, 6000d, 180d), (enemy.PositionX, enemy.PositionY, enemy.SpeedMps, enemy.DirectionDegrees));
            Assert.Equal(new AttackAiData(25, 8, (i - 1) * 35), enemy.Ai);
            Assert.Equal(new[] { "module.engine.strizh", "module.torpedo.launcher.light" }, enemy.Modules!.Select(m => m.ModuleTypeId));
            Assert.All(enemy.Modules!, m => { Assert.Equal("none", m.ArmorClass); Assert.Equal(150, m.StructurePoints); });
            Assert.Equal(2, enemy.Crew!.Count);
        }
        Assert.All(scenario.GameState.SpaceObjects.SelectMany(o => o.Crew!), c =>
        { Assert.Equal(20, c.AccuracySkill); Assert.Equal(20, c.ManeuverabilitySkill); });
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(Path.Combine(root, "Settings.json"), path);
        var snapshot = At(engine);
        Assert.Equal(9, snapshot.InstalledModules.Length);
        Assert.Equal(CombatEncounterState.Active, snapshot.Encounter!.State);
        Assert.All(snapshot.InstalledModules.Where(m => m.Defense is not null), m => Assert.False(m.Defense!.AutoEnabled));
        Assert.All(snapshot.Objects.Where(o => o.ObjectType == SpaceObjectType.NpcShip), o => Assert.Empty(o.ModuleHealth));
        engine.ReceiveCommand(new("scenario-point-launch", 1, player.ObjectId, "MOD-PLAYER-TORPEDO-01", CombatCommandTypes.Launch,
            TargetWorldX: 0, TargetWorldY: 600, ProfileId: "fast"));
        var launched = At(engine);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(launched.CommandResults).Status);
        var flight = Assert.Single(launched.Objects, o => o.OwnProjectile is not null).OwnProjectile!;
        Assert.Equal((16d, 2.5, 200), (flight.SpeedKmS, flight.BlastRadiusKm, flight.Damage));
    }

    public static IEnumerable<object[]> PursuitCases() =>
        Enumerable.Range(0, 3).SelectMany(i => new[] { 0d, 90d, 180d, 270d }.Select(direction => new object[] { i, direction }));

    [Theory]
    [MemberData(nameof(PursuitCases))]
    public void Actual_approved_spawn_enters_launch_range_against_maximum_speed_player_within_sixty_seconds(int index, double direction)
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(Source, "Settings.json"), out _, out _);
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(Source, Relative));
        string playerId = scenario.GameState.PlayerShipObjectId!;
        var enemy = scenario.GameState.SpaceObjects.Where(o => o.ObjectType == SpaceObjectType.NpcShip).ElementAt(index);
        // Isolate every real spawn/controller pair. Disable weapons only to prevent deaths from truncating the pursuit diagnostic.
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                MasterSeed = 42,
                SpaceObjects = scenario.GameState.SpaceObjects.Where(o => o.ObjectId == playerId || o.ObjectId == enemy.ObjectId)
                .Select(o => o with
                {
                    SpeedMps = o.ObjectId == playerId ? 5000 : o.SpeedMps,
                    DirectionDegrees = o.ObjectId == playerId ? direction : o.DirectionDegrees,
                    Modules = o.Modules!.Select(m => m.ModuleTypeId.Contains("torpedo", StringComparison.Ordinal) ? m with { PowerState = "Off" } : m).ToArray()
                }).ToArray()
            }
        };
        using var engine = new SimulationEngine(registry); engine.LoadScenario(scenario);
        long? reached = null;
        for (long time = 0; time <= 60000; time += 100)
        {
            var scene = At(engine, time);
            var player = scene.Objects.Single(o => o.ObjectId == playerId);
            var target = scene.Objects.Single(o => o.ObjectId == enemy.ObjectId);
            double dx = (target.X - player.X) / 10, dy = (target.Y - player.Y) / 10;
            if (dx * dx + dy * dy <= 70 * 70) { reached = time; break; }
        }
        Assert.True(reached.HasValue, $"Approved spawn {index}, player heading {direction}: no 70km range entry by 60 physical seconds.");
    }
}
