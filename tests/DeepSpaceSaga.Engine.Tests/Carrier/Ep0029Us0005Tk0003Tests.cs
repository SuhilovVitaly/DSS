using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class Ep0029Us0005Tk0003Tests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-ep0029-us0005-tk0003-{Guid.NewGuid():N}");
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
    private string SettingsPath => Path.Combine(ClientRoot, "Settings.json");
    private string ScenarioPath => Path.Combine(_directory, "scenario.json");

    public Ep0029Us0005Tk0003Tests() => Directory.CreateDirectory(_directory);

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

    private static JsonObject Bay(string moduleId, int ax, string powerState = "On")
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
            ["powerState"] = powerState,
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

    private void WriteScenario(string bay1Power = "On", string bay2Power = "On")
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
            ["modules"] = new JsonArray(Bay("BAY-01", 0, bay1Power), Bay("BAY-02", 2, bay2Power)),
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

    private SimulationEngine Load(string bay1Power = "On", string bay2Power = "On")
    {
        WriteScenario(bay1Power, bay2Power);
        return EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, ScenarioPath);
    }

    private static OwnedDroneData Drone(string id, string crewId, string bay, int slot, bool destroyed = false) => new(
        DroneId: id, HangarModuleId: bay, SlotIndex: slot, CrewId: crewId,
        FrameId: "heavy.sword.shield", WeaponId: "gun.sword.t1", ArmorId: "plate.medium",
        ShieldId: "shield.t1", ShieldCount: 2, ShieldSlots: 2,
        DecoyId: "decoy.t1", DecoyMaxCharges: 2, DecoyCharges: 2,
        CurrentHull: 100, MaxHull: 100, Armor: 50, MaxArmor: 50,
        AmmoCurrent: 12, AmmoCapacity: 12, EngineIds: ["drive.standard", "drive.standard"], Destroyed: destroyed);

    private static OwnedDroneData[] FourWing(bool allDestroyed = false) =>
    [
        Drone("DRN-PLAYER-01", "CHR-PILOT-1", "BAY-01", 0, destroyed: allDestroyed),
        Drone("DRN-PLAYER-02", "CHR-PILOT-2", "BAY-01", 1, destroyed: allDestroyed),
        Drone("DRN-PLAYER-03", "CHR-PILOT-3", "BAY-02", 0, destroyed: allDestroyed),
        Drone("DRN-PLAYER-04", "CHR-PILOT-4", "BAY-02", 1, destroyed: allDestroyed),
    ];

    private static StartTacticalBattleRequest Owned(string commandId, string shipId = "SPC-0001", int revision = 0) =>
        new(commandId, 1, "initial") { SourceShipId = shipId, ExpectedCarrierRevision = revision };

    [Fact]
    public void WrongShipAndStaleRevisionReject()
    {
        using var engine = Load();
        engine.SetOwnedWingForTests("SPC-0001", FourWing());

        var wrongShip = engine.StartTacticalBattle(Owned("c1", shipId: "WRONG"));
        Assert.Equal(TacticalBattleRejectionCode.WrongShip, wrongShip.Code);

        var stale = engine.StartTacticalBattle(Owned("c2", revision: 5));
        Assert.Equal(TacticalBattleRejectionCode.StaleCarrierState, stale.Code);

        Assert.Null(engine.TacticalBattleSessionForTests); // no battle binding was created
    }

    [Fact]
    public void OwnedLoadoutOverrideRejects()
    {
        using var engine = Load();
        engine.SetOwnedWingForTests("SPC-0001", FourWing());
        var request = Owned("c1");
        request = request with { Loadout = new System.Collections.Immutable.ImmutableArray<TacticalLoadoutChoice>() };
        var response = engine.StartTacticalBattle(new("c1", 1, "initial")
        {
            SourceShipId = "SPC-0001",
            ExpectedCarrierRevision = 0,
            Loadout = System.Collections.Immutable.ImmutableArray.Create(new TacticalLoadoutChoice("DRN-PLAYER-01", "gun.spear.t1", "plate.medium", "shield.t1", 1)),
        });
        Assert.Equal(TacticalBattleRejectionCode.InvalidRequest, response.Code);
    }

    [Fact]
    public void DuplicateStartReturnsReceipt()
    {
        using var engine = Load();
        engine.SetOwnedWingForTests("SPC-0001", FourWing());
        var first = engine.StartTacticalBattle(Owned("dup"));
        Assert.True(first.Accepted);
        Assert.NotNull(engine.TacticalBattleSessionForTests);

        var second = engine.StartTacticalBattle(Owned("dup"));
        Assert.True(second.Accepted); // same identity returns the cached receipt
        Assert.Equal(first.State?.BattleId, second.State?.BattleId);
    }

    [Fact]
    public void NoReadyDronesNoFallback()
    {
        using var engine = Load();
        engine.SetOwnedWingForTests("SPC-0001", FourWing(allDestroyed: true));
        var response = engine.StartTacticalBattle(Owned("c1"));
        Assert.Equal(TacticalBattleRejectionCode.NoReadyDrones, response.Code);
        Assert.Null(engine.TacticalBattleSessionForTests);
    }

    private static string[] PlayerDroneIds(SimulationEngine engine) =>
        engine.TacticalBattleSessionForTests!.Battle.Drones
            .Where(d => d.Side == "A").Select(d => d.Id).Order(StringComparer.Ordinal).ToArray();

    [Fact]
    public void OfflineHangarDronesCannotLaunch()
    {
        // BAY-01 is Off: only the powered/ready BAY-02 launches its two drones.
        using var engine = Load(bay1Power: "Off");
        engine.SetOwnedWingForTests("SPC-0001", FourWing());
        var response = engine.StartTacticalBattle(Owned("c1"));
        Assert.True(response.Accepted);
        Assert.Equal(new[] { "DRN-PLAYER-03", "DRN-PLAYER-04" }, PlayerDroneIds(engine));
        engine.ExitTacticalBattle(new("exit", response.State!.BattleId));

        // Both bays Off: the wing is present but has no ready hangar — refuse, never fall back.
        using var offline = Load(bay1Power: "Off", bay2Power: "Off");
        offline.SetOwnedWingForTests("SPC-0001", FourWing());
        var rejected = offline.StartTacticalBattle(Owned("c1"));
        Assert.Equal(TacticalBattleRejectionCode.NoReadyDrones, rejected.Code);
        Assert.Null(offline.TacticalBattleSessionForTests);
    }

    [Fact]
    public void DisabledDroneIsNotDeployed()
    {
        using var engine = Load();
        var wing = FourWing();
        wing[1] = wing[1] with { Disabled = true };
        engine.SetOwnedWingForTests("SPC-0001", wing);

        var response = engine.StartTacticalBattle(Owned("c1"));
        Assert.True(response.Accepted);
        Assert.Equal(new[] { "DRN-PLAYER-01", "DRN-PLAYER-03", "DRN-PLAYER-04" }, PlayerDroneIds(engine));
    }
}
