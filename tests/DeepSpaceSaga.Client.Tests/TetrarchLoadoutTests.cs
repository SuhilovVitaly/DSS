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
        Assert.Equal(2, pirate.Crew!.Count);
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

    [Theory]
    [InlineData("PlayerShipOnly", false)]
    [InlineData("Default", false)]
    [InlineData("Default_500", false)]
    [InlineData("Docked", false)]
    [InlineData("Undocked", false)]
    [InlineData("PlayerShipOnly", true)]
    [InlineData("Default", true)]
    [InlineData("Default_500", true)]
    [InlineData("Docked", true)]
    [InlineData("Undocked", true)]
    public void Standard_tetrarch_loadouts_remain_valid(string name, bool output)
    {
        var scenario = AssertScenarioLoadout(output ? AppContext.BaseDirectory : ClientRoot, name);
        var player = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == scenario.GameState.PlayerShipObjectId);
        Assert.Equal(2000, scenario.GameState.PlayerTokens);
        Assert.Null(scenario.GameState.MasterSeed);
        Assert.Equal(0, player.DirectionDegrees);
        Assert.Equal(name == "Docked", player.IsDocked);
        Assert.Equal(name == "Docked" ? "SPC-0002" : null, player.DockedStationObjectId);
        var expectedMotion = name switch
        {
            "Docked" => (11001d, 11001d, 0d),
            "Undocked" => (11002.5d, 11000d, 0d),
            _ => (10000d, 10000d, 700d)
        };
        Assert.Equal(expectedMotion, (player.PositionX, player.PositionY, player.SpeedMps));
        Assert.Equal("CHR-0001", player.Crew![0].CrewId);
        if (name == "Default_500")
            Assert.Equal(500, scenario.GameState.SpaceObjects.Count(o => o.ObjectType == SpaceObjectType.Asteroid));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Market_profiles_remains_unclassified(bool output)
    {
        string root = output ? AppContext.BaseDirectory : ClientRoot;
        string relativePath = Path.Combine("Scenarios", "MarketProfiles", "scenario.json");
        string path = Path.Combine(root, relativePath);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(Path.Combine(ClientRoot, relativePath))),
            JsonNode.Parse(File.ReadAllText(path))));
        var scenario = ScenarioLoader.LoadFromFile(path);
        var player = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == scenario.GameState.PlayerShipObjectId);
        Assert.Null(player.ShipClassId);
        Assert.Null(player.HullHitPoints);
        Assert.Equal(3, player.HullLayout!.Cells.Count);
        Assert.Equal(new[] { (4, 0), (4, 1), (4, 2) }, player.HullLayout.Cells.Select(c => (c.X, c.Y)));
        Assert.Equal(3, player.Modules!.Count);
        Assert.DoesNotContain(player.Modules, m => m.ModuleTypeId == LauncherId);
        Assert.Equal(100000, scenario.GameState.PlayerTokens);
        Assert.Equal(5, scenario.GameState.SpaceObjects.Count(o => o.ObjectType == SpaceObjectType.Station));
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(Path.Combine(root, "Settings.json"), path);
        var snapshot = engine.CaptureSnapshot();
        var ship = snapshot.Objects.Single(o => o.ObjectId == snapshot.PlayerShipObjectId);
        Assert.Equal("Images/CelestialObjects/Spacecraft/ship-tetrarch-class.png", ship.Image);
        Assert.Null(ship.HullCombat);
        Assert.All(snapshot.InstalledModules, m => Assert.Null(m.LauncherCombat));
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
            AssertLoadout(ship, stressLoadout: name == "Default_500", defense: true);

        // Production bootstrap exercises content resolution, placement validation and snapshot projection.
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(Path.Combine(root, "Settings.json"), path);
        var snapshot = engine.CaptureSnapshot();
        foreach (var ship in ships)
        {
            var actual = Assert.Single(snapshot.Objects, o => o.ObjectId == ship.ObjectId);
            Assert.Equal(new HullCombatSnapshot("ship.tetrarch", 450, 450), actual.HullCombat);
            Assert.Equal(ship.IsDocked, actual.IsDocked);
            Assert.Equal(ship.DockedStationObjectId, actual.DockedStationObjectId);
        }
        var launcher = Assert.Single(snapshot.InstalledModules, m => m.ModuleTypeId == LauncherId);
        Assert.NotNull(launcher.LauncherCombat);
        Assert.Equal(new LauncherCombatSnapshot(null, 3, 90, 150), launcher.LauncherCombat with { Operator = null });
        var assignedOperator = Assert.IsType<WeaponOperatorSnapshot>(launcher.LauncherCombat.Operator);
        Assert.Equal("CHR-0001", assignedOperator.CrewId);
        Assert.Equal(50, assignedOperator.Skill);
        Assert.Equal(30m, assignedOperator.EffectiveRating);
        Assert.Equal("Ready", launcher.OperationalState);
        Assert.Equal("On", launcher.PowerState);
        Assert.Equal(new[] { CombatCommandTypes.Fire, CombatCommandTypes.SelfDestruct }, launcher.CommandTypeIds);
        Assert.Null(launcher.ActiveCommandType);
        Assert.DoesNotContain(snapshot.Objects, o => o.Torpedo is not null);
        return scenario;
    }

    private static void AssertLoadout(SpaceObjectData ship, bool stressLoadout, bool defense)
    {
        Assert.Equal("ship.tetrarch", ship.ShipClassId);
        Assert.Null(ship.HullHitPoints); // New games resolve the maximum from class content.
        Assert.NotNull(ship.HullLayout);
        Assert.Equal((9, 9), (ship.HullLayout.Width, ship.HullLayout.Height));
        var cells = ship.HullLayout.Cells.Select(c => (c.X, c.Y)).ToArray();
        Assert.Equal(defense ? 12 : 11, cells.Length);
        Assert.Equal(cells.Length, cells.Distinct().Count());
        Assert.Equal(new[] { (4, 0), (3, 1), (4, 1), (5, 1), (4, 2), (4, 3), (4, 4), (3, 5), (4, 5), (5, 5), (3, 2) }, cells.Take(11));
        Assert.NotNull(ship.Modules);
        Assert.Equal((stressLoadout ? 4 : 7) + (defense ? 1 : 0), ship.Modules.Count);
        Assert.Equal(ship.Modules.Count, ship.Modules.Select(m => m.ModuleId).Distinct(StringComparer.Ordinal).Count());
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

        if (defense)
        {
            var defender = Assert.Single(ship.Modules, m => m.ModuleTypeId == "module.countermeasure.launcher.basic");
            Assert.Equal(new HullCellCoordinate(5, 2), Assert.Single(defender.OccupiedCells));
            Assert.True(defender.AutoDefenseEnabled);
            Assert.Equal(2, ship.Crew!.Count);
            Assert.NotEqual(launcher.OperatorCrewId, defender.OperatorCrewId);
            Assert.Contains(ship.Crew, c => c.CrewId == launcher.OperatorCrewId);
            Assert.Contains(ship.Crew, c => c.CrewId == defender.OperatorCrewId);
            Assert.All(ship.Crew, c => { Assert.Equal(50, c.TorpedoSkill); Assert.Equal(50, c.CountermeasureSkill); });
        }
        var existing = ship.Modules.Where(m => m.ModuleTypeId != LauncherId && m.ModuleTypeId != "module.countermeasure.launcher.basic").ToArray();
        (string Type, int Row)[] fullLoadout = [("module.bridge.navigation.computer.basic", 0), ("living.quarters.mk1", 1),
            ("module.container.basic", 2), ("module.scanner.mk1", 3), ("module.generator.basic", 4), ("module.engine.basic", 5)];
        var expected = stressLoadout ? fullLoadout.Where(m => m.Row is 1 or 2 or 5).ToArray() : fullLoadout;
        Assert.Equal(expected.Select(m => m.Type), existing.Select(m => m.ModuleTypeId));
        for (int i = 0; i < existing.Length; i++)
            Assert.Equal(new HullCellCoordinate(4, expected[i].Row), Assert.Single(existing[i].OccupiedCells));
        var cargo = existing.Single(m => m.ModuleTypeId == "module.container.basic").Cargo!;
        Assert.Equal(new[] { new CargoStackData("item.energy-cells", 1000), new CargoStackData("item.food-rations", 200) }, cargo);
    }
}
