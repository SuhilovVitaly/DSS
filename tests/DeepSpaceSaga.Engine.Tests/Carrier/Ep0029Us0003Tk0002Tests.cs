using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class Ep0029Us0003Tk0002Tests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-ep0029-us0003-tk0002-{Guid.NewGuid():N}");
    private string SettingsPath => Path.Combine(_directory, "settings.json");
    private string ScenarioPath => Path.Combine(_directory, "scenario.json");
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    public Ep0029Us0003Tk0002Tests()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "categories.json"),
            """{"moduleTypes":[{"typeId":"module.drone-bay","displayName":"Drone bay","slotSize":1,"commandTypeIds":[]}]}""");
        File.WriteAllText(Path.Combine(_directory, "implementations.json"),
            """{"moduleImplementations":[{"typeId":"module.drone-bay.medium","displayName":"M","type":"module.drone-bay","massKg":4500,"structurePointsMax":250,"powerConsumptionW":0,"droneCapacity":2,"footprintWidth":2,"footprintHeight":2}]}""");
        File.WriteAllText(Path.Combine(_directory, "commands.json"), """{"commandDefinitions":[]}""");
        File.WriteAllText(Path.Combine(_directory, "items.json"),
            """{"itemTypes":[{"typeId":"item.ice","displayName":"Ice","unitMassKg":1}]}""");
        File.WriteAllText(Path.Combine(_directory, "ships.json"),
            """{"shipClasses":[{"typeId":"ship.tetrarch","hullMax":400}]}""");
        File.WriteAllText(SettingsPath, """
            {"typeData":{"moduleTypes":"categories.json","moduleImplementations":"implementations.json",
              "commandDefinitions":"commands.json","itemTypes":"items.json","shipClasses":"ships.json"},
              "defaultScenario":"scenario.json"}
            """);
    }

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
    }

    private SimulationEngine Load() => EngineContentLoader.CreateEngineFromSettingsFile(SettingsPath);

    private static JsonObject HullLayout()
    {
        var cells = new JsonArray();
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
                cells.Add(new JsonObject { ["x"] = x, ["y"] = y });
        return new JsonObject { ["width"] = 5, ["height"] = 5, ["cells"] = cells };
    }

    private static JsonObject Bay2x2(string moduleId, int ax, int ay)
    {
        var cells = new JsonArray();
        foreach (var (x, y) in new[] { (ax, ay), (ax + 1, ay), (ax, ay + 1), (ax + 1, ay + 1) })
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

    private static JsonObject CrewMember(string id, string name, string role) => new()
    {
        ["crewId"] = id,
        ["displayName"] = name,
        ["role"] = role,
        ["accuracySkill"] = 0,
        ["maneuverabilitySkill"] = 0,
    };

    private static OwnedDroneData Drone(string id, string bay, int slot, string crewId, bool destroyed = false,
        string frameId = "heavy.sword.shield", string weaponId = "weapon.sword", string armorId = "plate.medium",
        string? shieldId = "shield", int shieldCount = 2, int shieldSlots = 2,
        string? decoyId = "decoy.t1", int decoyMaxCharges = 2, int decoyCharges = 2,
        int currentHull = 100, int maxHull = 100, int armor = 50, int maxArmor = 50,
        int ammoCurrent = 12, int ammoCapacity = 12, IReadOnlyList<string>? engineIds = null) => new(
        DroneId: id, HangarModuleId: bay, SlotIndex: slot, CrewId: crewId,
        FrameId: frameId, WeaponId: weaponId, ArmorId: armorId,
        ShieldId: shieldId, ShieldCount: shieldCount, ShieldSlots: shieldSlots,
        DecoyId: decoyId, DecoyMaxCharges: decoyMaxCharges, DecoyCharges: decoyCharges,
        CurrentHull: currentHull, MaxHull: maxHull, Armor: armor, MaxArmor: maxArmor,
        AmmoCurrent: ammoCurrent, AmmoCapacity: ammoCapacity,
        EngineIds: engineIds, Destroyed: destroyed);

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
            ["modules"] = new JsonArray(Bay2x2("BAY-01", 0, 0), Bay2x2("BAY-02", 2, 0)),
            ["crew"] = new JsonArray(
                CrewMember("CHR-0001", "Dunkan Su", "Captain"),
                CrewMember("CHR-PLAYER-DEFENSE", "Irina Volskaya", "Officer"),
                CrewMember("CHR-PILOT-1", "Evelyn Ward", "Pilot"),
                CrewMember("CHR-PILOT-2", "Audrey Bennett", "Pilot"),
                CrewMember("CHR-PILOT-3", "Claire Morgan", "Pilot"),
                CrewMember("CHR-PILOT-4", "Rachel Hayes", "Pilot")),
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

    [Fact]
    public void FifthDroneRejectedWithFourSlots()
    {
        WriteScenario();
        using var engine = Load();

        var four = new[]
        {
            Drone("DRN-1", "BAY-01", 0, "CHR-PILOT-1"),
            Drone("DRN-2", "BAY-01", 1, "CHR-PILOT-2"),
            Drone("DRN-3", "BAY-02", 0, "CHR-PILOT-3"),
            Drone("DRN-4", "BAY-02", 1, "CHR-PILOT-4"),
        };
        engine.SetOwnedWingForTests("SPC-0001", four);

        var five = four.Concat(new[] { Drone("DRN-5", "BAY-01", 2, "CHR-PILOT-1") }).ToArray();
        Assert.Throws<ScenarioException>(() => engine.SetOwnedWingForTests("SPC-0001", five));
    }

    [Fact]
    public void DuplicatePilotOrSlotRejected()
    {
        WriteScenario();
        using var engine = Load();

        var duplicatePilot = new[]
        {
            Drone("DRN-1", "BAY-01", 0, "CHR-PILOT-1"),
            Drone("DRN-2", "BAY-01", 1, "CHR-PILOT-1"),
        };
        Assert.Throws<ScenarioException>(() => engine.SetOwnedWingForTests("SPC-0001", duplicatePilot));

        var duplicateSlot = new[]
        {
            Drone("DRN-1", "BAY-01", 0, "CHR-PILOT-1"),
            Drone("DRN-2", "BAY-01", 0, "CHR-PILOT-2"),
        };
        Assert.Throws<ScenarioException>(() => engine.SetOwnedWingForTests("SPC-0001", duplicateSlot));
    }

    [Fact]
    public void DestroyedDroneRetainsCrew()
    {
        WriteScenario();
        using var engine = Load();

        var wing = new[]
        {
            Drone("DRN-1", "BAY-01", 0, "CHR-PILOT-1"),
            Drone("DRN-2", "BAY-01", 1, "CHR-PILOT-2", destroyed: true),
            Drone("DRN-3", "BAY-02", 0, "CHR-PILOT-3"),
            Drone("DRN-4", "BAY-02", 1, "CHR-PILOT-4"),
        };
        engine.SetOwnedWingForTests("SPC-0001", wing);

        var snapshot = engine.CaptureSnapshot();
        var carrier = snapshot.CarrierOperations!;
        Assert.Equal(4, carrier.OwnedDrones.Length);
        Assert.Contains(carrier.OwnedDrones, d => d.Id == "DRN-2" && d.ReadinessReasons.Contains("destroyed"));
        Assert.Equal(6, carrier.Crew.Length);
    }

    /// <summary>Loads the engine from the real settings file so the tactical content catalog exists.</summary>
    private SimulationEngine LoadRealContent()
    {
        WriteScenario();
        return EngineContentLoader.CreateEngineFromScenarioFile(Path.Combine(ClientRoot, "Settings.json"), ScenarioPath);
    }

    /// <summary>Four drones shaped exactly like the shipped starter wing (Documentation starter tables).</summary>
    private static OwnedDroneData[] StarterShapedWing() =>
    [
        Drone("DRN-PLAYER-01", "BAY-01", 0, "CHR-PILOT-1", frameId: "heavy.sword.shield",
            weaponId: "gun.sword.t1", armorId: "plate.medium", shieldId: "shield.t1",
            shieldCount: 2, shieldSlots: 2, decoyId: "decoy.t1", decoyMaxCharges: 2, decoyCharges: 2,
            currentHull: 65, maxHull: 65, armor: 80, maxArmor: 80, ammoCurrent: 12, ammoCapacity: 12,
            engineIds: ["drive.standard", "drive.standard"]),
        Drone("DRN-PLAYER-02", "BAY-01", 1, "CHR-PILOT-2", frameId: "heavy.spear.shield",
            weaponId: "gun.spear.t1", armorId: "plate.medium", shieldId: "shield.t1",
            shieldCount: 2, shieldSlots: 2, decoyId: "decoy.t1", decoyMaxCharges: 2, decoyCharges: 2,
            currentHull: 65, maxHull: 65, armor: 80, maxArmor: 80, ammoCurrent: 12, ammoCapacity: 12,
            engineIds: ["drive.standard", "drive.standard"]),
        Drone("DRN-PLAYER-03", "BAY-02", 0, "CHR-PILOT-3", frameId: "light.sword.shield",
            weaponId: "gun.sword.t1", armorId: "plate.medium", shieldId: "shield.t1",
            shieldCount: 1, shieldSlots: 1, decoyId: "decoy.t1", decoyMaxCharges: 2, decoyCharges: 2,
            currentHull: 55, maxHull: 55, armor: 80, maxArmor: 80, ammoCurrent: 6, ammoCapacity: 6,
            engineIds: ["drive.standard", "drive.standard"]),
        Drone("DRN-PLAYER-04", "BAY-02", 1, "CHR-PILOT-4", frameId: "light.spear",
            weaponId: "gun.spear.t1", armorId: "plate.medium", shieldId: "shield.t1",
            shieldCount: 1, shieldSlots: 1, decoyId: "decoy.t1", decoyMaxCharges: 2, decoyCharges: 2,
            currentHull: 55, maxHull: 55, armor: 80, maxArmor: 80, ammoCurrent: 6, ammoCapacity: 6,
            engineIds: ["drive.standard", "drive.standard"]),
    ];

    private static void AssertRejected(SimulationEngine engine, OwnedDroneData[] wing,
        Func<OwnedDroneData, OwnedDroneData> mutate)
    {
        var invalid = wing.Select((d, i) => i == 0 ? mutate(d) : d).ToArray();
        Assert.Throws<ScenarioException>(() => engine.SetOwnedWingForTests("SPC-0001", invalid));
    }

    [Fact]
    public void WingRejectsUnknownLayoutWeaponEngine()
    {
        using var engine = LoadRealContent();
        var wing = StarterShapedWing();
        engine.SetOwnedWingForTests("SPC-0001", wing); // the valid baseline is accepted

        AssertRejected(engine, wing, d => d with { FrameId = "heavy.unknown" });
        AssertRejected(engine, wing, d => d with { WeaponId = "gun.unknown" });
        AssertRejected(engine, wing, d => d with { ArmorId = "plate.unknown" });
        AssertRejected(engine, wing, d => d with { ShieldId = "shield.unknown" });
        AssertRejected(engine, wing, d => d with { DecoyId = "decoy.unknown" });
        AssertRejected(engine, wing, d => d with { EngineIds = ["drive.standard", "drive.unknown"] });
    }

    [Fact]
    public void WingRejectsOverCapacityMagazineAndSlots()
    {
        using var engine = LoadRealContent();
        var wing = StarterShapedWing();
        engine.SetOwnedWingForTests("SPC-0001", wing);

        // Heavy frame: ammoCapacity 12, shieldSlots 2, engineSlots 2. Decoy t1 carries 2 charges.
        AssertRejected(engine, wing, d => d with { AmmoCurrent = 13, AmmoCapacity = 13 });
        AssertRejected(engine, wing, d => d with { ShieldCount = 3, ShieldSlots = 3 });
        AssertRejected(engine, wing, d => d with { ShieldCount = 2, ShieldSlots = 3 });
        AssertRejected(engine, wing, d => d with { DecoyMaxCharges = 3, DecoyCharges = 3 });
        AssertRejected(engine, wing, d => d with { EngineIds = ["drive.standard", "drive.standard", "drive.standard"] });
    }

    [Fact]
    public void ValidStarterWingPassesCatalogValidation()
    {
        using var engine = LoadRealContent();
        engine.SetOwnedWingForTests("SPC-0001", StarterShapedWing());

        var wing = engine.CaptureSnapshot().CarrierOperations!.OwnedDrones;
        Assert.Equal(4, wing.Length);
        Assert.All(wing, d => Assert.Empty(d.ReadinessReasons));
    }

    [Fact]
    public void NullOrBlankEquipmentIdsRejectCleanly()
    {
        using var engine = LoadRealContent();
        var wing = StarterShapedWing();
        engine.SetOwnedWingForTests("SPC-0001", wing);

        // Null ids must be a domain rejection, never an ArgumentNullException from a dictionary probe.
        AssertRejected(engine, wing, d => d with { HangarModuleId = null! });
        AssertRejected(engine, wing, d => d with { FrameId = null! });
        AssertRejected(engine, wing, d => d with { WeaponId = null! });
        AssertRejected(engine, wing, d => d with { ArmorId = null! });
        AssertRejected(engine, wing, d => d with { EngineIds = ["drive.standard", null!] });

        // Blank ids are unknown references under the same ScenarioException contract.
        AssertRejected(engine, wing, d => d with { HangarModuleId = "  " });
        AssertRejected(engine, wing, d => d with { FrameId = "  " });
        AssertRejected(engine, wing, d => d with { ShieldId = "  " });
        AssertRejected(engine, wing, d => d with { DecoyId = "  " });

        // Null optional equipment means "none" and stays valid (no over-rejection).
        engine.SetOwnedWingForTests("SPC-0001",
            wing.Select((d, i) => i == 0
                ? d with
                {
                    ShieldId = null,
                    ShieldCount = 0,
                    ShieldSlots = 0,
                    DecoyId = null,
                    DecoyMaxCharges = 0,
                    DecoyCharges = 0,
                }
                : d).ToArray());
    }
}
