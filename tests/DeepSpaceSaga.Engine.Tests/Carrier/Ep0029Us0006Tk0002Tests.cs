using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class Ep0029Us0006Tk0002Tests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-ep0029-us0006-tk0002-{Guid.NewGuid():N}");
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
    private string SettingsPath => Path.Combine(ClientRoot, "Settings.json");
    private string ScenarioPath => Path.Combine(_directory, "scenario.json");

    public Ep0029Us0006Tk0002Tests() => Directory.CreateDirectory(_directory);

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
    public void FinishThenExitSettlesOnce()
    {
        var engine = StartBattle(out var battleId);
        var before = engine.GetCarrierRevision("SPC-0001");

        var exit = engine.ExitTacticalBattle(new("exit", battleId));
        Assert.NotNull(exit.Settlement);
        Assert.Equal(before + 1, engine.GetCarrierRevision("SPC-0001"));
        Assert.Equal(SortieExitKind.Aborted, exit.Settlement!.ExitKind); // no result yet -> abort
    }

    [Fact]
    public void LostDroneAmmoNotRecovered()
    {
        var engine = StartBattle(out var battleId);
        var session = engine.TacticalBattleSessionForTests!;
        var lost = session.Battle.Drones.First(d => d.Side == "A" && d.Id == "DRN-PLAYER-02");
        lost.Status = TacticalDroneStatus.Destroyed;

        var exit = engine.ExitTacticalBattle(new("exit", battleId));
        Assert.Contains("DRN-PLAYER-02", exit.Settlement!.LostDroneIds);

        var wing = engine.CaptureSnapshot().CarrierOperations!.OwnedDrones;
        var tomb = wing.Single(d => d.Id == "DRN-PLAYER-02");
        Assert.Contains("destroyed", tomb.ReadinessReasons); // tombstone, ammo lost with magazine
    }

    [Fact]
    public void DisabledReturnsDamaged()
    {
        var engine = StartBattle(out var battleId);
        var session = engine.TacticalBattleSessionForTests!;
        var drone = session.Battle.Drones.First(d => d.Side == "A" && d.Id == "DRN-PLAYER-03");
        drone.Hull = 40;
        drone.AmmoCurrent = 2;
        drone.Jammed = true;

        engine.ExitTacticalBattle(new("exit", battleId));

        var wing = engine.CaptureSnapshot().CarrierOperations!.OwnedDrones;
        var restored = wing.Single(d => d.Id == "DRN-PLAYER-03");
        Assert.Equal(40, restored.CurrentHull);
        Assert.Equal(2, restored.AmmoCurrent);
        Assert.True(restored.Jammed);
    }

    [Fact]
    public void AbortPreservesSpentAmmo()
    {
        var engine = StartBattle(out var battleId);
        var session = engine.TacticalBattleSessionForTests!;
        var drone = session.Battle.Drones.First(d => d.Side == "A" && d.Id == "DRN-PLAYER-01");
        drone.AmmoCurrent = 7; // 12 - 5 fired

        var exit = engine.ExitTacticalBattle(new("exit", battleId));
        Assert.Equal(5, exit.Settlement!.ConsumedTorpedoes);
        Assert.Equal(7, engine.CaptureSnapshot().CarrierOperations!.OwnedDrones.Single(d => d.Id == "DRN-PLAYER-01").AmmoCurrent);
    }

    [Fact]
    public void HullZeroMarksWorldDestroyed()
    {
        var engine = StartBattle(out var battleId);
        var session = engine.TacticalBattleSessionForTests!;
        var carrier = session.Battle.Carriers.First(c => c.Side == "A");
        carrier.Hull = 0;

        var exit = engine.ExitTacticalBattle(new("exit", battleId));
        Assert.True(exit.Settlement!.ShipDestroyed);
        Assert.True(engine.CaptureSnapshot().CarrierOperations!.IsDestroyed);
        Assert.True(engine.CaptureSnapshot().Objects.Single(o => o.ObjectType == SpaceObjectType.PlayerShip).IsDestroyed);
    }

    private static OwnedDroneData SaveWingDrone(SimulationEngine engine, string droneId) =>
        engine.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectId == "SPC-0001")
            .CarrierState!.OwnedDrones!.Single(d => d.DroneId == droneId);

    [Fact]
    public void SettlementPersistsDisabledAndEngineDamage()
    {
        var engine = StartBattle(out var battleId);
        var session = engine.TacticalBattleSessionForTests!;
        var drone = session.Battle.Drones.First(d => d.Side == "A" && d.Id == "DRN-PLAYER-03");
        drone.Status = TacticalDroneStatus.Disabled;
        drone.Engines[1].Factor = 0.5; // rules.Systems.EngineDamagedFactor literal

        engine.ExitTacticalBattle(new("exit", battleId));

        // Saved carrierState carries the disabled flag and per-engine factors (12/12/6/6 wing).
        var persisted = SaveWingDrone(engine, "DRN-PLAYER-03");
        Assert.True(persisted.Disabled);
        Assert.False(persisted.Destroyed);
        Assert.Equal([1.0, 0.5], persisted.EngineFactors!.ToArray());

        var snapshot = engine.CaptureSnapshot().CarrierOperations!.OwnedDrones.Single(d => d.Id == "DRN-PLAYER-03");
        Assert.Contains("disabled", snapshot.ReadinessReasons);
        Assert.DoesNotContain("destroyed", snapshot.ReadinessReasons);
        Assert.Equal(2, snapshot.EngineDamage.Length);
        Assert.Equal((0, "drive.standard", "Ok"),
            (snapshot.EngineDamage[0].Index, snapshot.EngineDamage[0].EngineId, snapshot.EngineDamage[0].State));
        Assert.Equal((1, "drive.standard", "Damaged"),
            (snapshot.EngineDamage[1].Index, snapshot.EngineDamage[1].EngineId, snapshot.EngineDamage[1].State));
    }

    [Fact]
    public void DisabledDroneSurvivesRoundTrip()
    {
        var engine = StartBattle(out var battleId);
        var session = engine.TacticalBattleSessionForTests!;
        var drone = session.Battle.Drones.First(d => d.Side == "A" && d.Id == "DRN-PLAYER-03");
        drone.Status = TacticalDroneStatus.Disabled;
        drone.Engines[1].Factor = 0.5;

        engine.ExitTacticalBattle(new("exit", battleId));
        var before = engine.CaptureSnapshot().CarrierOperations!.OwnedDrones.Single(d => d.Id == "DRN-PLAYER-03");

        string json = ScenarioLoader.Serialize(engine.CaptureSaveState());
        using var restored = new SimulationEngine(SaveFixturesTests.Registry(),
            clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
        restored.LoadScenario(ScenarioLoader.LoadFromJson(json, allowNonZeroGameTime: true, isSave: true), isSave: true);

        var after = restored.CaptureSnapshot().CarrierOperations!.OwnedDrones.Single(d => d.Id == "DRN-PLAYER-03");
        Assert.Contains("disabled", after.ReadinessReasons);
        Assert.Equal(
            before.EngineDamage.Select(e => (e.Index, e.EngineId, e.State)),
            after.EngineDamage.Select(e => (e.Index, e.EngineId, e.State)));

        var persisted = SaveWingDrone(restored, "DRN-PLAYER-03");
        Assert.True(persisted.Disabled);
        Assert.Equal([1.0, 0.5], persisted.EngineFactors!.ToArray());

        // Loading never re-settles: the revision and the receipt survive unchanged.
        Assert.Equal(1, restored.GetCarrierRevision("SPC-0001"));
        Assert.Equal(battleId, restored.CaptureSaveState().GameState.SpaceObjects
            .Single(o => o.ObjectId == "SPC-0001").CarrierState!.LastSettlement!.BattleId);
    }

    [Fact]
    public void WingRejectsInvalidEngineFactors()
    {
        using var engine = Load();
        var drone = Drone("DRN-PLAYER-01", "CHR-PILOT-1", "BAY-01", 0);
        engine.SetOwnedWingForTests("SPC-0001", new[] { drone });

        // Two installed engines: a shorter list is allowed, factors must stay in (0, 1].
        engine.SetOwnedWingForTests("SPC-0001", new[] { drone with { EngineFactors = [1.0, 0.5] } });
        Assert.Throws<ScenarioException>(() =>
            engine.SetOwnedWingForTests("SPC-0001", new[] { drone with { EngineFactors = [1.0, 0.0] } }));
        Assert.Throws<ScenarioException>(() =>
            engine.SetOwnedWingForTests("SPC-0001", new[] { drone with { EngineFactors = [1.0, 1.5] } }));
        Assert.Throws<ScenarioException>(() =>
            engine.SetOwnedWingForTests("SPC-0001", new[] { drone with { EngineFactors = [1.0, 1.0, 1.0] } }));
    }
}
