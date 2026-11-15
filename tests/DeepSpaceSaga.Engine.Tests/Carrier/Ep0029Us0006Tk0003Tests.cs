using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class Ep0029Us0006Tk0003Tests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-ep0029-us0006-tk0003-{Guid.NewGuid():N}");
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
    private string SettingsPath => Path.Combine(ClientRoot, "Settings.json");
    private string ScenarioPath => Path.Combine(_directory, "scenario.json");

    public Ep0029Us0006Tk0003Tests() => Directory.CreateDirectory(_directory);

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

    private static JsonObject Crew(string id, string role) => new()
    {
        ["crewId"] = id,
        ["displayName"] = id,
        ["role"] = role,
        ["accuracySkill"] = 10,
        ["maneuverabilitySkill"] = 10,
        ["initiative"] = 55,
    };

    private void WriteScenario()
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
            ["modules"] = new JsonArray(Bay("BAY-01", 0), Bay("BAY-02", 2)),
            ["crew"] = new JsonArray(
                Crew("CHR-0001", "Captain"), Crew("CHR-PLAYER-DEFENSE", "Officer"),
                Crew("CHR-PILOT-1", "Pilot"), Crew("CHR-PILOT-2", "Pilot"),
                Crew("CHR-PILOT-3", "Pilot"), Crew("CHR-PILOT-4", "Pilot")),
        };
        var scenario = new JsonObject
        {
            ["scenarioMetadata"] = new JsonObject { ["scenarioId"] = "tk0003", ["name"] = "tk0003" },
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

    private SimulationEngine Load()
    {
        WriteScenario();
        return EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, ScenarioPath);
    }

    private static OwnedDroneData Drone(string id, string crewId, string bay, int slot, int ammo = 12) => new(
        DroneId: id, HangarModuleId: bay, SlotIndex: slot, CrewId: crewId,
        FrameId: "heavy.sword.shield", WeaponId: "gun.sword.t1", ArmorId: "plate.medium",
        ShieldId: "shield.t1", ShieldCount: 2, ShieldSlots: 2,
        DecoyId: "decoy.t1", DecoyMaxCharges: 2, DecoyCharges: 2,
        CurrentHull: 100, MaxHull: 100, Armor: 50, MaxArmor: 50,
        AmmoCurrent: ammo, AmmoCapacity: 12, EngineIds: ["drive.standard", "drive.standard"]);

    private SimulationEngine StartBattle(out string battleId)
    {
        var engine = Load();
        engine.SetOwnedWingForTests("SPC-0001", new[]
        {
            Drone("DRN-PLAYER-01", "CHR-PILOT-1", "BAY-01", 0),
            Drone("DRN-PLAYER-02", "CHR-PILOT-2", "BAY-01", 1),
            Drone("DRN-PLAYER-03", "CHR-PILOT-3", "BAY-02", 0),
            Drone("DRN-PLAYER-04", "CHR-PILOT-4", "BAY-02", 1),
        });
        var start = new StartTacticalBattleRequest("start", 1, "initial") { SourceShipId = "SPC-0001", ExpectedCarrierRevision = 0 };
        var response = engine.StartTacticalBattle(start);
        battleId = response.State!.BattleId;
        return engine;
    }

    [Fact]
    public void WorldCoordinatesUnchangedAfterSortie()
    {
        var engine = StartBattle(out var battleId);
        var before = engine.CaptureSnapshot().Objects.Single(o => o.ObjectType == SpaceObjectType.PlayerShip);
        engine.ExitTacticalBattle(new("exit", battleId));
        var after = engine.CaptureSnapshot().Objects.Single(o => o.ObjectType == SpaceObjectType.PlayerShip);
        Assert.Equal(before.X, after.X);
        Assert.Equal(before.Y, after.Y);
    }

    [Fact]
    public void WorldSnapshotMatchesTacticalFinal()
    {
        var engine = StartBattle(out var battleId);
        var session = engine.TacticalBattleSessionForTests!;
        var carrier = session.Battle.Carriers.First(c => c.Side == "A");
        carrier.Hull = 300;
        var drone = session.Battle.Drones.First(d => d.Side == "A" && d.Id == "DRN-PLAYER-03");
        drone.Hull = 40;
        drone.AmmoCurrent = 2;
        engine.ExitTacticalBattle(new("exit", battleId));

        var ops = engine.CaptureSnapshot().CarrierOperations!;
        Assert.Equal(300, ops.HullCurrent);
        Assert.Equal(6, ops.Crew.Length);
        var restored = ops.OwnedDrones.Single(d => d.Id == "DRN-PLAYER-03");
        Assert.Equal(40, restored.CurrentHull);
        Assert.Equal(2, restored.AmmoCurrent);
    }

    [Fact]
    public void DestroyedShipCannotResumeCommands()
    {
        var engine = StartBattle(out var battleId);
        var session = engine.TacticalBattleSessionForTests!;
        session.Battle.Carriers.First(c => c.Side == "A").Hull = 0;
        engine.ExitTacticalBattle(new("exit", battleId));

        var retry = new StartTacticalBattleRequest("retry", 1, "initial") { SourceShipId = "SPC-0001", ExpectedCarrierRevision = 1 };
        var response = engine.StartTacticalBattle(retry);
        Assert.Equal(TacticalBattleRejectionCode.CarrierNotReady, response.Code);
    }
}
