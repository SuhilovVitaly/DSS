using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class TetrarchLoadoutTests
{
    private const string LauncherId = "module.torpedo.launcher.basic";
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        TestPaths.Repository, "src", "DeepSpaceSaga.Client"));

    [Theory]
    [InlineData("Default")]
    [InlineData("Default_500")]
    [InlineData("Docked")]
    [InlineData("Undocked")]
    [InlineData("MarketProfiles")]
    [InlineData("PlayerShipOnly")]
    public void Legacy_scenarios_load_with_new_crew_and_armor_schema(string name)
    {
        var path = Path.Combine(ClientRoot, "Scenarios", name, "scenario.json");
        var json = File.ReadAllText(path);
        Assert.DoesNotContain("\"torpedoSkill\"", json);
        Assert.DoesNotContain("\"countermeasureSkill\"", json);
        var scenario = ScenarioLoader.LoadFromFile(path);
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(Path.Combine(ClientRoot, "Settings.json"), path);
        Assert.NotNull(engine.CaptureSnapshot());
        bool starter = name is "Default" or "Tutorial";
        foreach (var ship in scenario.GameState.SpaceObjects.Where(o => o.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip))
        {
            bool starterShip = starter && ship.ObjectType == SpaceObjectType.PlayerShip;
            Assert.All(ship.Modules ?? [], module =>
            {
                // The starter manifest starts medium drone bays at SP 250, every other module at 150.
                int max = starterShip && module.ModuleTypeId == "module.drone-bay.medium" ? 250 : 150;
                Assert.InRange(module.StructurePoints, 0, max);
            });
            Assert.All(ship.Crew ?? [], member =>
            {
                if (starterShip && member.Role == "Pilot")
                {
                    // The permanent starter pilots carry the tactical 0..100 scale (US-0011).
                    Assert.InRange(member.AccuracySkill, 0, 100);
                    Assert.InRange(member.ManeuverabilitySkill, 0, 100);
                    return;
                }
                Assert.Equal(0, member.AccuracySkill);
                Assert.Equal(0, member.ManeuverabilitySkill);
            });
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Player_and_neutral_contact_keep_world_placement(bool output)
    {
        string root = output ? AppContext.BaseDirectory : ClientRoot;
        var scenario = AssertScenarioLoadout(root, "PlayerShipOnly");
        Assert.Null(scenario.GameState.MasterSeed);
        Assert.Equal(2, scenario.GameState.SpaceObjects.Count);
        var player = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == scenario.GameState.PlayerShipObjectId);
        var pirate = scenario.GameState.SpaceObjects.Single(o => o.ObjectType == SpaceObjectType.NpcShip);
        Assert.Equal((10000d, 10000d, 700d, 0d), (player.PositionX, player.PositionY, player.SpeedMps, player.DirectionDegrees));
        Assert.Equal((0d, 5000d, 400d, 120d), (pirate.PositionX, pirate.PositionY, pirate.SpeedMps, pirate.DirectionDegrees));
        Assert.Equal(PlayerRelation.Neutral, pirate.RelationToPlayer);
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
    public void Other_starts_keep_loadout(string name, bool output)
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
        Assert.DoesNotContain("autoDefenseEnabled", File.ReadAllText(path));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(Path.Combine(ClientRoot, relativePath))),
            JsonNode.Parse(File.ReadAllText(path))));
        var scenario = ScenarioLoader.LoadFromFile(path);
        var player = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == scenario.GameState.PlayerShipObjectId);
        Assert.Null(player.ShipClassId);
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
    }

    private static ScenarioFile AssertScenarioLoadout(string root, string name)
    {
        string relativePath = Path.Combine("Scenarios", name, "scenario.json");
        string path = Path.Combine(root, relativePath);
        Assert.DoesNotContain("autoDefenseEnabled", File.ReadAllText(path));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(Path.Combine(ClientRoot, relativePath))),
            JsonNode.Parse(File.ReadAllText(path))));
        var scenario = ScenarioLoader.LoadFromFile(path);
        var ships = scenario.GameState.SpaceObjects.Where(o => o.ObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip).ToArray();
        Assert.NotEmpty(ships);
        bool starter = name is "Default" or "Tutorial";
        foreach (var ship in ships)
            AssertLoadout(ship, stressLoadout: name == "Default_500",
                threeCabins: name == "PlayerShipOnly" && ship.ObjectType == SpaceObjectType.PlayerShip,
                tetrarchEngine: name == "PlayerShipOnly",
                starter: starter && ship.ObjectType == SpaceObjectType.PlayerShip);

        // Production bootstrap exercises content resolution, placement validation and snapshot projection.
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(Path.Combine(root, "Settings.json"), path);
        var snapshot = engine.CaptureSnapshot();
        foreach (var ship in ships)
        {
            var actual = Assert.Single(snapshot.Objects, o => o.ObjectId == ship.ObjectId);
            Assert.Equal(ship.IsDocked, actual.IsDocked);
            Assert.Equal(ship.DockedStationObjectId, actual.DockedStationObjectId);
        }
        Assert.Equal(ships.Single(o => o.ObjectType == SpaceObjectType.PlayerShip).Modules!.Count, snapshot.InstalledModules.Length);
        return scenario;
    }

    private static void AssertLoadout(SpaceObjectData ship, bool stressLoadout, bool threeCabins, bool tetrarchEngine,
        bool starter = false)
    {
        Assert.Equal("ship.tetrarch", ship.ShipClassId);
        Assert.NotNull(ship.HullLayout);
        Assert.Equal((9, 9), (ship.HullLayout.Width, ship.HullLayout.Height));
        var cells = ship.HullLayout.Cells.Select(c => (c.X, c.Y)).ToArray();
        if (starter)
        {
            // EP-0029 starter manifest (Documentation.md:102): 24 cells, 10 modules, 16 occupied (EP-0035 sensor).
            Assert.Equal(24, cells.Length);
            Assert.Equal(24, cells.Distinct().Count());
            Assert.NotNull(ship.Modules);
            Assert.Equal(10, ship.Modules.Count);
            Assert.Equal(ship.Modules.Count, ship.Modules.Select(m => m.ModuleId).Distinct(StringComparer.Ordinal).Count());
            var starterOccupied = new HashSet<(int X, int Y)>();
            foreach (var module in ship.Modules)
                foreach (var cell in module.OccupiedCells)
                {
                    Assert.Contains((cell.X, cell.Y), cells);
                    Assert.True(starterOccupied.Add((cell.X, cell.Y)), $"Module {module.ModuleId} overlaps another module.");
                }
            Assert.Equal(16, starterOccupied.Count);
            Assert.Equal([new HullCellCoordinate(3, 4)], ship.Modules.Single(m => m.ModuleId == "MOD-PLAYER-SENSOR-01").OccupiedCells);
            var quarters = ship.Modules.Where(m => m.ModuleTypeId == "living.quarters.mk1.four-cabins").ToArray();
            Assert.Equal(2, quarters.Length);
            Assert.Equal([new HullCellCoordinate(3, 1)],
                quarters.Single(m => m.ModuleId == "MOD-PLAYER-LIVING-QUARTERS-01").OccupiedCells);
            Assert.Equal([new HullCellCoordinate(5, 1)],
                quarters.Single(m => m.ModuleId == "MOD-PLAYER-LIVING-QUARTERS-02").OccupiedCells);
            var bays = ship.Modules.Where(m => m.ModuleTypeId == "module.drone-bay.medium").ToArray();
            Assert.Equal(2, bays.Length);
            Assert.Equal([new HullCellCoordinate(1, 2), new HullCellCoordinate(2, 2),
                new HullCellCoordinate(1, 3), new HullCellCoordinate(2, 3)],
                bays.Single(m => m.ModuleId == "MOD-PLAYER-BAY-01").OccupiedCells);
            Assert.Equal([new HullCellCoordinate(6, 2), new HullCellCoordinate(7, 2),
                new HullCellCoordinate(6, 3), new HullCellCoordinate(7, 3)],
                bays.Single(m => m.ModuleId == "MOD-PLAYER-BAY-02").OccupiedCells);
            Assert.Single(ship.Modules, m => m.ModuleId == "MOD-PLAYER-BRIDGE-01");
            Assert.Single(ship.Modules, m => m.ModuleId == "MOD-PLAYER-SCANNER-01");
            Assert.Single(ship.Modules, m => m.ModuleId == "MOD-PLAYER-GENERATOR-01");
            Assert.Single(ship.Modules, m => m.ModuleId == "MOD-PLAYER-ENGINE-01");
            var starterCargo = ship.Modules.Single(m => m.ModuleTypeId == "module.container.basic").Cargo!;
            Assert.Equal(3, starterCargo.Count);
            Assert.Equal(1_000, starterCargo.Single(c => c.ItemTypeId == "item.energy-cells").Quantity);
            Assert.Equal(200, starterCargo.Single(c => c.ItemTypeId == "item.food-rations").Quantity);
            Assert.Equal(42, starterCargo.Single(c => c.ItemTypeId == "item.drone-torpedo.t1").Quantity);
            Assert.Equal(6, ship.Crew!.Count);
            return;
        }
        Assert.Equal(12, cells.Length);
        Assert.Equal(cells.Length, cells.Distinct().Count());
        Assert.Equal(new[] { (4, 0), (3, 1), (4, 1), (5, 1), (4, 2), (4, 3), (4, 4), (3, 5), (4, 5), (5, 5), (3, 2) }, cells.Take(11));
        Assert.NotNull(ship.Modules);
        Assert.Equal(stressLoadout ? 3 : 6, ship.Modules.Count);
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
        var existing = ship.Modules.ToArray();
        (string Type, int Row)[] fullLoadout = [("module.bridge.navigation.computer.basic", 0), (threeCabins ? "living.quarters.mk1.three-cabins" : "living.quarters.mk1", 1),
            ("module.container.basic", 2), ("module.scanner.mk1", 3), ("module.generator.basic", 4), (tetrarchEngine ? "module.engine.tetrarch" : "module.engine.basic", 5)];
        var expected = stressLoadout ? fullLoadout.Where(m => m.Row is 1 or 2 or 5).ToArray() : fullLoadout;
        Assert.Equal(expected.Select(m => m.Type), existing.Select(m => m.ModuleTypeId));
        for (int i = 0; i < existing.Length; i++)
            Assert.Equal(new HullCellCoordinate(4, expected[i].Row), Assert.Single(existing[i].OccupiedCells));
        var cargo = existing.Single(m => m.ModuleTypeId == "module.container.basic").Cargo!;
        Assert.Equal(new[] { new CargoStackData("item.energy-cells", 1000), new CargoStackData("item.food-rations", 200) }, cargo);
    }
}
