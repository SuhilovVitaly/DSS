using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class TetrarchLoadoutTests
{
    private const string LauncherId = "module.torpedo.launcher.basic";
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Player_and_pirate_have_one_launcher_in_new_room(bool output)
    {
        string root = output ? AppContext.BaseDirectory : ClientRoot;
        var scenario = AssertScenarioLoadout(root, "PlayerShipOnly");
        Assert.Null(scenario.GameState.MasterSeed);
        Assert.Equal(2, scenario.GameState.SpaceObjects.Count);
        var player = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == scenario.GameState.PlayerShipObjectId);
        var pirate = scenario.GameState.SpaceObjects.Single(o => o.ObjectType == SpaceObjectType.NpcShip);
        Assert.Equal((10000d, 10000d, 700d, 0d), (player.PositionX, player.PositionY, player.SpeedMps, player.DirectionDegrees));
        Assert.Equal((0d, 5000d, 400d, 120d), (pirate.PositionX, pirate.PositionY, pirate.SpeedMps, pirate.DirectionDegrees));
        Assert.Equal(PlayerRelation.Enemy, pirate.RelationToPlayer);
        Assert.True(pirate.IsKnown);
        Assert.Null(pirate.Name);
        Assert.Null(pirate.CaptainDisplayName);
        Assert.All(pirate.Modules!, m => Assert.Null(m.ActiveCycle));
        Assert.Empty(pirate.Crew!);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Default_tetrarch_loadout_has_no_overlaps(bool output)
    {
        var scenario = AssertScenarioLoadout(output ? AppContext.BaseDirectory : ClientRoot, "Default");
        Assert.Null(scenario.GameState.MasterSeed);
        Assert.Equal(2000, scenario.GameState.PlayerTokens);
        var player = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == scenario.GameState.PlayerShipObjectId);
        Assert.False(player.IsDocked);
        Assert.Equal((10000d, 10000d, 700d, 0d), (player.PositionX, player.PositionY, player.SpeedMps, player.DirectionDegrees));
    }

    private static ScenarioFile AssertScenarioLoadout(string root, string name)
    {
        string relativePath = Path.Combine("Scenarios", name, "scenario.json");
        string path = Path.Combine(root, relativePath);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(Path.Combine(ClientRoot, relativePath))),
            JsonNode.Parse(File.ReadAllText(path))));
        var scenario = ScenarioLoader.LoadFromFile(path);
        var ships = scenario.GameState.SpaceObjects.Where(o => o.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip).ToArray();
        Assert.NotEmpty(ships);
        foreach (var ship in ships)
            AssertLoadout(ship);

        // Production bootstrap exercises content resolution, placement validation and snapshot projection.
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(Path.Combine(root, "Settings.json"), path);
        var snapshot = engine.CaptureSnapshot();
        foreach (var ship in ships)
        {
            var actual = Assert.Single(snapshot.Objects, o => o.ObjectId == ship.ObjectId);
            Assert.Equal(new HullCombatSnapshot("ship.tetrarch", 450, 450), actual.HullCombat);
        }
        var launcher = Assert.Single(snapshot.InstalledModules, m => m.ModuleTypeId == LauncherId);
        Assert.Equal(new LauncherCombatSnapshot(null, 3, 90, 150), launcher.LauncherCombat);
        Assert.Equal("Ready", launcher.OperationalState);
        Assert.Equal("On", launcher.PowerState);
        Assert.Equal(new[] { CombatCommandTypes.Fire }, launcher.CommandTypeIds);
        Assert.Null(launcher.ActiveCommandType);
        Assert.DoesNotContain(snapshot.Objects, o => o.Torpedo is not null);
        return scenario;
    }

    private static void AssertLoadout(SpaceObjectData ship)
    {
        Assert.Equal("ship.tetrarch", ship.ShipClassId);
        Assert.Null(ship.HullHitPoints); // New games resolve the maximum from class content.
        Assert.NotNull(ship.HullLayout);
        Assert.Equal((9, 9), (ship.HullLayout.Width, ship.HullLayout.Height));
        var cells = ship.HullLayout.Cells.Select(c => (c.X, c.Y)).ToArray();
        Assert.Equal(11, cells.Length);
        Assert.Equal(11, cells.Distinct().Count());
        Assert.Equal(new[] { (4, 0), (3, 1), (4, 1), (5, 1), (4, 2), (4, 3), (4, 4), (3, 5), (4, 5), (5, 5), (3, 2) }, cells);
        Assert.NotNull(ship.Modules);
        Assert.Equal(7, ship.Modules.Count);
        Assert.Equal(7, ship.Modules.Select(m => m.ModuleId).Distinct(StringComparer.Ordinal).Count());
        var occupied = new HashSet<(int X, int Y)>();
        foreach (var module in ship.Modules)
        {
            foreach (var cell in module.OccupiedCells)
            {
                Assert.Contains((cell.X, cell.Y), cells);
                Assert.True(occupied.Add((cell.X, cell.Y)), $"Module {module.ModuleId} overlaps another module.");
            }
        }
        var launcher = Assert.Single(ship.Modules, m => m.ModuleTypeId == LauncherId);
        Assert.Equal(new HullCellCoordinate(3, 2), Assert.Single(launcher.OccupiedCells));
        Assert.Equal(60, launcher.StructurePoints);
        Assert.Null(launcher.ActiveCycle);
        Assert.Empty(launcher.Cargo!);

        var existing = ship.Modules.Where(m => m.ModuleTypeId != LauncherId).ToArray();
        Assert.Equal(new[] { "module.bridge.navigation.computer.basic", "living.quarters.mk1", "module.container.basic",
            "module.scanner.mk1", "module.generator.basic", "module.engine.basic" }, existing.Select(m => m.ModuleTypeId));
        for (int i = 0; i < existing.Length; i++)
            Assert.Equal(new HullCellCoordinate(4, i), Assert.Single(existing[i].OccupiedCells));
        var cargo = existing.Single(m => m.ModuleTypeId == "module.container.basic").Cargo!;
        Assert.Equal(new[] { new CargoStackData("item.energy-cells", 1000), new CargoStackData("item.food-rations", 200) }, cargo);
    }
}
