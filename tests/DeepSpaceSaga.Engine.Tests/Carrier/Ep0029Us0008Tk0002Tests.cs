using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class Ep0029Us0008Tk0002Tests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-ep0029-us0008-tk0002-{Guid.NewGuid():N}");
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        TestPaths.Repository, "src", "DeepSpaceSaga.Client"));
    private string SettingsPath => Path.Combine(ClientRoot, "Settings.json");
    private string ScenarioPath => Path.Combine(_directory, "scenario.json");

    public Ep0029Us0008Tk0002Tests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
    }

    private static JsonObject HullLayout()
    {
        var cells = new JsonArray();
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
                cells.Add(new JsonObject { ["x"] = x, ["y"] = y });
        return new JsonObject { ["width"] = 5, ["height"] = 5, ["cells"] = cells };
    }

    private static JsonObject Bay(string moduleId, int ax)
    {
        var cells = new JsonArray();
        foreach (var (x, y) in new[] { (ax, 0), (ax + 1, 0), (ax, 1), (ax + 1, 1) })
            cells.Add(new JsonObject { ["x"] = x, ["y"] = y });
        return new JsonObject
        {
            ["moduleId"] = moduleId,
            ["moduleTypeId"] = "module.drone-bay.medium",
            ["occupiedCells"] = cells,
            ["structurePoints"] = 250,
            ["powerState"] = "On",
            ["operationalState"] = "Ready",
            ["cargo"] = new JsonArray(),
        };
    }

    private static JsonObject Cargo(string moduleId, int quantity)
    {
        var cells = new JsonArray { new JsonObject { ["x"] = 4, ["y"] = 2 } };
        return new JsonObject
        {
            ["moduleId"] = moduleId,
            ["moduleTypeId"] = "module.container.basic",
            ["occupiedCells"] = cells,
            ["structurePoints"] = 150,
            ["powerState"] = "On",
            ["operationalState"] = "Ready",
            ["cargo"] = new JsonArray(new JsonObject { ["itemTypeId"] = "item.drone-torpedo.t1", ["quantity"] = quantity }),
        };
    }

    private static JsonObject Crew(string id, string role) => new()
    {
        ["crewId"] = id,
        ["displayName"] = id,
        ["role"] = role,
        ["accuracySkill"] = 10,
        ["maneuverabilitySkill"] = 10,
        ["initiative"] = 55,
    };

    private void WriteScenario(int cargoQuantity = 42)
    {
        var ship = new JsonObject
        {
            ["objectId"] = "SPC-0001",
            ["objectType"] = "PlayerShip",
            ["shipClassId"] = "ship.tetrarch",
            ["persistenceType"] = "Permanent",
            ["name"] = "Tetrarch",
            ["positionX"] = 0,
            ["positionY"] = 0,
            ["speedMps"] = 0,
            ["directionDegrees"] = 0,
            ["movementType"] = "Stationary",
            ["isKnown"] = true,
            ["hullLayout"] = HullLayout(),
            ["modules"] = new JsonArray(Bay("BAY-01", 0), Bay("BAY-02", 2), Cargo("CARGO-01", cargoQuantity)),
            ["crew"] = new JsonArray(
                Crew("CHR-0001", "Captain"), Crew("CHR-PLAYER-DEFENSE", "Officer"),
                Crew("CHR-PILOT-1", "Pilot"), Crew("CHR-PILOT-2", "Pilot"),
                Crew("CHR-PILOT-3", "Pilot"), Crew("CHR-PILOT-4", "Pilot")),
        };
        var scenario = new JsonObject
        {
            ["scenarioMetadata"] = new JsonObject { ["scenarioId"] = "tk0002", ["name"] = "tk0002" },
            ["gameState"] = new JsonObject
            {
                ["masterSeed"] = 42,
                ["gameTimeMs"] = 0,
                ["currentSpeed"] = "Speed0",
                ["playerShipObjectId"] = "SPC-0001",
                ["spaceObjects"] = new JsonArray(ship),
            },
        };
        File.WriteAllText(ScenarioPath, scenario.ToJsonString());
    }

    private SimulationEngine Load(int cargoQuantity = 42)
    {
        WriteScenario(cargoQuantity);
        return EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, ScenarioPath);
    }

    private static OwnedDroneData Drone(string id, string crewId, string bay, int slot, int ammo, int capacity) => new(
        DroneId: id, HangarModuleId: bay, SlotIndex: slot, CrewId: crewId,
        FrameId: "heavy.sword.shield", WeaponId: "gun.sword.t1", ArmorId: "plate.medium",
        ShieldId: "shield.t1", ShieldCount: 2, ShieldSlots: 2,
        DecoyId: "decoy.t1", DecoyMaxCharges: 2, DecoyCharges: 2,
        CurrentHull: 100, MaxHull: 100, Armor: 50, MaxArmor: 50,
        AmmoCurrent: ammo, AmmoCapacity: capacity, EngineIds: ["drive.standard", "drive.standard"]);

    private SimulationEngine SetupWing(int[] ammo, int[] capacity, int cargoQuantity = 42)
    {
        var engine = Load(cargoQuantity);
        engine.SetOwnedWingForTests("SPC-0001", new[]
        {
            Drone("DRN-PLAYER-01", "CHR-PILOT-1", "BAY-01", 0, ammo[0], capacity[0]),
            Drone("DRN-PLAYER-02", "CHR-PILOT-2", "BAY-01", 1, ammo[1], capacity[1]),
            Drone("DRN-PLAYER-03", "CHR-PILOT-3", "BAY-02", 0, ammo[2], capacity[2]),
            Drone("DRN-PLAYER-04", "CHR-PILOT-4", "BAY-02", 1, ammo[3], capacity[3]),
        });
        return engine;
    }

    private static long CargoQuantity(SimulationEngine engine) =>
        engine.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectId == "SPC-0001")
            .Modules!.SelectMany(m => m.Cargo ?? []).Single(c => c.ItemTypeId == "item.drone-torpedo.t1").Quantity;

    private static int Ammo(SimulationEngine engine, string droneId) =>
        engine.CaptureSnapshot().CarrierOperations!.OwnedDrones.Single(d => d.Id == droneId).AmmoCurrent;

    [Fact]
    public void FortyTwoRefillsThirtySixLeavesSix()
    {
        var engine = SetupWing([0, 0, 0, 0], [12, 12, 6, 6], cargoQuantity: 42);

        Assert.True(engine.RearmDrone(new("r1", "SPC-0001", "DRN-PLAYER-01", 12, 0)).Accepted);
        Assert.True(engine.RearmDrone(new("r2", "SPC-0001", "DRN-PLAYER-02", 12, 1)).Accepted);
        Assert.True(engine.RearmDrone(new("r3", "SPC-0001", "DRN-PLAYER-03", 6, 2)).Accepted);
        Assert.True(engine.RearmDrone(new("r4", "SPC-0001", "DRN-PLAYER-04", 6, 3)).Accepted);

        Assert.Equal(12, Ammo(engine, "DRN-PLAYER-01"));
        Assert.Equal(12, Ammo(engine, "DRN-PLAYER-02"));
        Assert.Equal(6, Ammo(engine, "DRN-PLAYER-03"));
        Assert.Equal(6, Ammo(engine, "DRN-PLAYER-04"));
        Assert.Equal(6, CargoQuantity(engine)); // 42 - 36 conserved
    }

    [Fact]
    public void InsufficientCargoIsAtomic()
    {
        var engine = SetupWing([0, 0, 0, 0], [12, 12, 6, 6], cargoQuantity: 6);

        var response = engine.RearmDrone(new("r1", "SPC-0001", "DRN-PLAYER-01", 7, 0));
        Assert.False(response.Accepted);
        Assert.Equal("no_cargo", response.Reason);
        Assert.Equal(0, Ammo(engine, "DRN-PLAYER-01"));
        Assert.Equal(6, CargoQuantity(engine)); // nothing changed
    }

    [Fact]
    public void DuplicateIdentityNoDoubleTransfer()
    {
        var engine = SetupWing([1, 0, 0, 0], [6, 12, 12, 6], cargoQuantity: 42);

        Assert.True(engine.RearmDrone(new("cmd-1", "SPC-0001", "DRN-PLAYER-01", 3, 0)).Accepted);
        Assert.Equal(4, Ammo(engine, "DRN-PLAYER-01"));

        // Replay in-session: no double spend.
        var replay = engine.RearmDrone(new("cmd-1", "SPC-0001", "DRN-PLAYER-01", 3, 0));
        Assert.False(replay.Accepted);
        Assert.Equal("duplicate_command", replay.Reason);
        Assert.Equal(4, Ammo(engine, "DRN-PLAYER-01"));

        // Replay after a save/load roundtrip: still no double spend.
        string json = ScenarioLoader.Serialize(engine.CaptureSaveState());
        var restored = new SimulationEngine(SaveFixturesTests.Registry(), clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
        restored.LoadScenario(ScenarioLoader.LoadFromJson(json, allowNonZeroGameTime: true, isSave: true), isSave: true);
        var afterLoad = restored.RearmDrone(new("cmd-1", "SPC-0001", "DRN-PLAYER-01", 3, 0));
        Assert.False(afterLoad.Accepted);
        Assert.Equal("duplicate_command", afterLoad.Reason);
        Assert.Equal(4, Ammo(restored, "DRN-PLAYER-01"));
    }

    [Fact]
    public void BattleAndDestroyedReject()
    {
        // Battle active blocks rearm.
        var engine = SetupWing([1, 0, 0, 0], [6, 12, 12, 6], cargoQuantity: 42);
        var start = new StartTacticalBattleRequest("start", 1, "initial") { SourceShipId = "SPC-0001", ExpectedCarrierRevision = 0 };
        var startResponse = engine.StartTacticalBattle(start);
        var duringBattle = engine.RearmDrone(new("r1", "SPC-0001", "DRN-PLAYER-01", 2, 0));
        Assert.False(duringBattle.Accepted);
        Assert.Equal("battle_active", duringBattle.Reason);
        engine.ExitTacticalBattle(new("exit", startResponse.State!.BattleId));

        // A destroyed (tombstone) drone rejects rearm.
        var engine2 = SetupWing([1, 0, 0, 0], [6, 12, 12, 6], cargoQuantity: 42);
        engine2.SetOwnedWingForTests("SPC-0001", new[]
        {
            Drone("DRN-PLAYER-01", "CHR-PILOT-1", "BAY-01", 0, 1, 6) with { Destroyed = true },
            Drone("DRN-PLAYER-02", "CHR-PILOT-2", "BAY-01", 1, 0, 12),
            Drone("DRN-PLAYER-03", "CHR-PILOT-3", "BAY-02", 0, 0, 12),
            Drone("DRN-PLAYER-04", "CHR-PILOT-4", "BAY-02", 1, 0, 6),
        });
        var destroyed = engine2.RearmDrone(new("r2", "SPC-0001", "DRN-PLAYER-01", 2, 0));
        Assert.False(destroyed.Accepted);
        Assert.Equal("destroyed_drone", destroyed.Reason);
    }

    [Fact]
    public void PartialReloadAndBasisConserved()
    {
        var engine = SetupWing([1, 0, 0, 0], [6, 12, 12, 6], cargoQuantity: 42);

        var response = engine.RearmDrone(new("r1", "SPC-0001", "DRN-PLAYER-01", 3, 0));
        Assert.True(response.Accepted);
        Assert.Equal(3, response.Transferred);
        Assert.Equal(4, Ammo(engine, "DRN-PLAYER-01"));

        var cargo = engine.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectId == "SPC-0001")
            .Modules!.SelectMany(m => m.Cargo ?? []).Single(c => c.ItemTypeId == "item.drone-torpedo.t1");
        Assert.Equal(39, cargo.Quantity);
        Assert.Equal(39 * 60, cargo.CostBasisCredits); // basis conserved, no fabricated profit
    }
}
