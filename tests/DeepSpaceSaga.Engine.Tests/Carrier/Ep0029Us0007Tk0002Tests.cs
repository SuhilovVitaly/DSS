using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class Ep0029Us0007Tk0002Tests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-ep0029-us0007-tk0002-{Guid.NewGuid():N}");
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        TestPaths.Repository, "src", "DeepSpaceSaga.Client"));
    private string SettingsPath => Path.Combine(ClientRoot, "Settings.json");
    private string ScenarioPath => Path.Combine(_directory, "scenario.json");

    public Ep0029Us0007Tk0002Tests() => Directory.CreateDirectory(_directory);

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
            ["modules"] = new JsonArray(Bay("BAY-01", 0), Bay("BAY-02", 2), Cargo("CARGO-01", 42)),
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

    private static SimulationEngine Restore(ScenarioFile save)
    {
        string json = ScenarioLoader.Serialize(save);
        var engine = new SimulationEngine(SaveFixturesTests.Registry(), clock: new SimulationClock(SimulationSpeed.Speed0, () => 0));
        engine.LoadScenario(ScenarioLoader.LoadFromJson(json, allowNonZeroGameTime: true, isSave: true), isSave: true);
        return engine;
    }

    [Fact]
    public void RoundTripAfterLossAndFiveShots()
    {
        var engine = StartBattle(out var battleId);
        var session = engine.TacticalBattleSessionForTests!;
        var lost = session.Battle.Drones.First(d => d.Side == "A" && d.Id == "DRN-PLAYER-02");
        lost.Status = TacticalDroneStatus.Destroyed;
        var shooter = session.Battle.Drones.First(d => d.Side == "A" && d.Id == "DRN-PLAYER-01");
        shooter.AmmoCurrent = 7; // 12 - 5 fired
        session.Battle.Carriers.First(c => c.Side == "A").Hull = 380;

        var exit = engine.ExitTacticalBattle(new("exit", battleId));
        Assert.Equal(5, exit.Settlement!.ConsumedTorpedoes);
        Assert.Contains("DRN-PLAYER-02", exit.Settlement.LostDroneIds);

        var save = engine.CaptureSaveState();
        var restored = Restore(save);

        var carrier = restored.CaptureSnapshot().CarrierOperations!;
        Assert.Equal(1, restored.GetCarrierRevision("SPC-0001"));
        Assert.Equal(380, carrier.HullCurrent);
        Assert.Equal(400, carrier.HullMax);
        Assert.False(carrier.IsDestroyed);

        var wing = carrier.OwnedDrones;
        Assert.Equal(7, wing.Single(d => d.Id == "DRN-PLAYER-01").AmmoCurrent);
        Assert.Contains("destroyed", wing.Single(d => d.Id == "DRN-PLAYER-02").ReadinessReasons);
        Assert.Equal(100, wing.Single(d => d.Id == "DRN-PLAYER-03").CurrentHull);

        // Hold cargo is untouched by the battle: 42 torpedoes still in the hold.
        var cargo = restored.CaptureSaveState().GameState.SpaceObjects
            .Single(o => o.ObjectId == "SPC-0001").Modules!
            .SelectMany(m => m.Cargo ?? [])
            .Single(c => c.ItemTypeId == "item.drone-torpedo.t1");
        Assert.Equal(42, cargo.Quantity);

        // Settlement receipt roundtrips (battleId + consumed torpedoes + exit kind).
        var savedCarrier = save.GameState.SpaceObjects.Single(o => o.ObjectId == "SPC-0001").CarrierState!;
        Assert.Equal(battleId, savedCarrier.LastSettlement!.BattleId);
        Assert.Equal(5, savedCarrier.LastSettlement.ConsumedTorpedoes);
        Assert.Equal("Aborted", savedCarrier.LastSettlement.ExitKind);
    }

    [Fact]
    public void TerminalDestroyedRoundTrip()
    {
        var engine = StartBattle(out var battleId);
        var session = engine.TacticalBattleSessionForTests!;
        var carrier = session.Battle.Carriers.First(c => c.Side == "A");
        carrier.Hull = 0;

        var exit = engine.ExitTacticalBattle(new("exit", battleId));
        Assert.True(exit.Settlement!.ShipDestroyed);

        var restored = Restore(engine.CaptureSaveState());
        var snapshot = restored.CaptureSnapshot();
        Assert.True(snapshot.CarrierOperations!.IsDestroyed);
        Assert.Equal(0, snapshot.CarrierOperations.HullCurrent);
        Assert.True(snapshot.Objects.Single(o => o.ObjectType == SpaceObjectType.PlayerShip).IsDestroyed);

        // A destroyed ship never accepts a new sortie.
        var start = new StartTacticalBattleRequest("start2", 1, "initial") { SourceShipId = "SPC-0001", ExpectedCarrierRevision = 1 };
        Assert.Equal(TacticalBattleRejectionCode.CarrierNotReady, restored.StartTacticalBattle(start).Code);
    }

    [Fact]
    public void SettlementReplayAfterLoadNoOp()
    {
        var engine = StartBattle(out var battleId);
        var session = engine.TacticalBattleSessionForTests!;
        var shooter = session.Battle.Drones.First(d => d.Side == "A" && d.Id == "DRN-PLAYER-01");
        shooter.AmmoCurrent = 7; // 12 - 5 fired

        engine.ExitTacticalBattle(new("exit", battleId));
        var restored = Restore(engine.CaptureSaveState());

        // The settlement delta is not re-applied on load: ammo stays at the post-settlement value.
        Assert.Equal(7, restored.CaptureSnapshot().CarrierOperations!.OwnedDrones.Single(d => d.Id == "DRN-PLAYER-01").AmmoCurrent);
        Assert.Equal(1, restored.GetCarrierRevision("SPC-0001"));

        // Replaying the exit after load is a no-op: the battle session is not persisted.
        Assert.Equal(TacticalBattleRejectionCode.NoBattle, restored.ExitTacticalBattle(new("exit", battleId)).Code);
        Assert.Equal(7, restored.CaptureSnapshot().CarrierOperations!.OwnedDrones.Single(d => d.Id == "DRN-PLAYER-01").AmmoCurrent);
        Assert.Equal(1, restored.GetCarrierRevision("SPC-0001"));
    }

    [Fact]
    public void DuringBattleSaveStillRejected()
    {
        var engine = StartBattle(out var battleId);
        var error = Assert.Throws<InvalidOperationException>(() => engine.CaptureSaveState());
        Assert.Contains(SimulationEngine.TacticalBattleActiveReason, error.Message);

        engine.ExitTacticalBattle(new("exit", battleId));
        Assert.NotNull(engine.CaptureSaveState());
    }
}
