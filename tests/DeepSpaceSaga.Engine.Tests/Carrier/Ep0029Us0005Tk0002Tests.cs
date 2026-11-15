using System.Collections.Immutable;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using DeepSpaceSaga.Engine.Tests.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class Ep0029Us0005Tk0002Tests : IDisposable
{
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-ep0029-us0005-tk0002-{Guid.NewGuid():N}");

    public Ep0029Us0005Tk0002Tests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
    }

    private static OwnedDroneData Drone(string id, string crewId, string frame, int ammo, int currentHull, int maxHull,
        bool destroyed = false, string bay = "BAY", int slot = 0)
    {
        bool heavy = frame.StartsWith("heavy");
        bool spear = frame.Contains("spear");
        return new OwnedDroneData(
            DroneId: id, HangarModuleId: bay, SlotIndex: slot, CrewId: crewId,
            FrameId: frame,
            WeaponId: spear ? "gun.spear.t1" : "gun.sword.t1",
            ArmorId: "plate.medium", ShieldId: "shield.t1",
            ShieldCount: heavy ? 2 : 1, ShieldSlots: heavy ? 2 : 1,
            DecoyId: "decoy.t1", DecoyMaxCharges: 2, DecoyCharges: 2,
            CurrentHull: currentHull, MaxHull: maxHull, Armor: 50, MaxArmor: 50,
            AmmoCurrent: ammo, AmmoCapacity: heavy ? 12 : 6,
            EngineIds: ["drive.standard", "drive.standard"],
            Destroyed: destroyed);
    }

    private static CrewMemberRuntime Pilot(string id, int accuracy = 55, int maneuver = 52, int initiative = 55,
        string? displayName = null, string? gender = null, string? portrait = null, string? role = "Pilot") =>
        new(id, displayName ?? id, accuracy, maneuver,
            Gender: gender, PortraitAssetId: portrait, Role: role, Initiative: initiative);

    private static ImmutableArray<OwnedDroneData> FourWing(bool twoDestroyed = false) => ImmutableArray.Create(
        Drone("DRN-PLAYER-01", "CHR-PILOT-1", "heavy.sword.shield", 12, 100, 100, destroyed: twoDestroyed),
        Drone("DRN-PLAYER-02", "CHR-PILOT-2", "heavy.spear.shield", 12, 100, 100, destroyed: twoDestroyed),
        Drone("DRN-PLAYER-03", "CHR-PILOT-3", "light.sword.shield", 6, 100, 100),
        Drone("DRN-PLAYER-04", "CHR-PILOT-4", "light.spear", 6, 100, 100));

    private static ImmutableArray<CrewMemberRuntime> FourCrew() => ImmutableArray.Create(
        Pilot("CHR-PILOT-1", 55, 52, 55),
        Pilot("CHR-PILOT-2", 53, 55, 53),
        Pilot("CHR-PILOT-3", 59, 54, 59),
        Pilot("CHR-PILOT-4", 50, 51, 50));

    private static TacticalBattleCatalog Catalog() => AssemblyTests.Catalog();

    [Fact]
    public void TwoSurvivorsStartAsTwo()
    {
        var battle = OwnedBattleAssembly.Create(Catalog(), "SPC-0001", 400, 35,
            FourWing(twoDestroyed: true), FourCrew(), Catalog().Scenarios["initial"], "initial", 1);
        Assert.Equal(2, battle.Drones.Count(d => d.Side == "A"));
        Assert.Equal(6, battle.Drones.Count(d => d.Side == "B")); // enemy unchanged
    }

    [Fact]
    public void DamagedShipAndAmmoPersistIntoBattle()
    {
        var wing = FourWing();
        var battle = OwnedBattleAssembly.Create(Catalog(), "SPC-0001", 317, 35, wing, FourCrew(),
            Catalog().Scenarios["initial"], "initial", 1);

        Assert.Equal(317, battle.Carrier("A").Hull);
        var light = battle.Drones.Single(d => d.Side == "A" && d.Id == "DRN-PLAYER-03");
        Assert.Equal(6, light.AmmoCapacity);
        Assert.Equal(6, light.AmmoCurrent);

        // Damage a drone's ammo and hull, then rebuild.
        var damaged = wing.Select(d => d.DroneId == "DRN-PLAYER-03" ? d with { AmmoCurrent = 2, CurrentHull = 40 } : d).ToImmutableArray();
        var damagedBattle = OwnedBattleAssembly.Create(Catalog(), "SPC-0001", 317, 35, damaged, FourCrew(),
            Catalog().Scenarios["initial"], "initial", 1);
        var rebuiltLight = damagedBattle.Drones.Single(d => d.Side == "A" && d.Id == "DRN-PLAYER-03");
        Assert.Equal(2, rebuiltLight.AmmoCurrent);
        Assert.Equal(40, rebuiltLight.Hull);
    }

    [Fact]
    public void RenameDoesNotReassignDeployment()
    {
        // Same DroneIds, different display names — deployment is keyed by stable id, not name.
        var crewA = FourCrew();
        var crewB = ImmutableArray.Create(
            Pilot("CHR-PILOT-1", 55, 52, 55), Pilot("CHR-PILOT-2", 53, 55, 53),
            Pilot("CHR-PILOT-3", 59, 54, 59), Pilot("CHR-PILOT-4", 50, 51, 50));

        var a = OwnedBattleAssembly.Create(Catalog(), "SPC-0001", 400, 35, FourWing(), crewA, Catalog().Scenarios["initial"], "initial", 1);
        var b = OwnedBattleAssembly.Create(Catalog(), "SPC-0001", 400, 35, FourWing(), crewB, Catalog().Scenarios["initial"], "initial", 1);
        Assert.Equal(a.Drones.Where(d => d.Side == "A").Select(d => d.Id),
            b.Drones.Where(d => d.Side == "A").Select(d => d.Id));
    }

    [Fact]
    public void EnemyGenerationKeepsSeedDiscipline()
    {
        var catalog = Catalog();
        var encounter = catalog.Scenarios["initial"];
        var fixedCrew = ImmutableArray.Create(
            Pilot("CHR-PILOT-1", 10, 10, 10), Pilot("CHR-PILOT-2", 10, 10, 10),
            Pilot("CHR-PILOT-3", 10, 10, 10), Pilot("CHR-PILOT-4", 10, 10, 10));
        var otherCrew = ImmutableArray.Create(
            Pilot("CHR-PILOT-1", 90, 90, 90), Pilot("CHR-PILOT-2", 90, 90, 90),
            Pilot("CHR-PILOT-3", 90, 90, 90), Pilot("CHR-PILOT-4", 90, 90, 90));

        var a = OwnedBattleAssembly.Create(catalog, "SPC-0001", 400, 35, FourWing(), fixedCrew, encounter, "initial", 1);
        var b = OwnedBattleAssembly.Create(catalog, "SPC-0001", 400, 35, FourWing(), otherCrew, encounter, "initial", 1);

        var enemyA = a.Drones.Where(d => d.Side == "B").Select(d => (d.Accuracy, d.Maneuver, d.Initiative)).ToArray();
        var enemyB = b.Drones.Where(d => d.Side == "B").Select(d => (d.Accuracy, d.Maneuver, d.Initiative)).ToArray();
        Assert.Equal(enemyA, enemyB); // fixed player crew never shifts the enemy RNG stream
    }

    [Fact]
    public void OwnedPilotIdentityComesFromCrewId()
    {
        // Crew values are deliberately distinct from the catalog aliases (vera: 55/52/55).
        var crew = ImmutableArray.Create(
            new CrewMemberRuntime("CHR-CREW-A", "Nadia Petrova", 71, 66, Gender: "Female",
                PortraitAssetId: "Images/Persons/W4/Portraits/pilot-a.png", Role: "Pilot", Initiative: 77),
            new CrewMemberRuntime("CHR-CREW-B", "Viktor Hale", 64, 73, Gender: "Male",
                PortraitAssetId: "Images/Persons/W4/Portraits/pilot-b.png", Role: "Pilot", Initiative: 61));
        var wing = ImmutableArray.Create(
            Drone("DRN-PLAYER-01", "CHR-CREW-A", "heavy.sword.shield", 12, 100, 100),
            Drone("DRN-PLAYER-02", "CHR-CREW-B", "heavy.spear.shield", 12, 100, 100));

        var battle = OwnedBattleAssembly.Create(Catalog(), "SPC-0001", 400, 35, wing, crew,
            Catalog().Scenarios["initial"], "initial", 1);
        var units = TacticalBattleSnapshotBuilder.Units(battle)
            .Where(u => u.Kind == TacticalUnitKind.Drone && u.Side == "A")
            .ToDictionary(u => u.Id, StringComparer.Ordinal);

        var a = units["DRN-PLAYER-01"];
        Assert.Equal("CHR-CREW-A", a.Build!.PilotId);
        Assert.Equal("Nadia Petrova", a.Build.PilotName);
        Assert.Equal(CrewGender.Female, a.Build.PilotGender);
        Assert.Equal("Images/Persons/W4/Portraits/pilot-a.png", a.Build.PilotPortraitAssetId);
        Assert.Equal(CrewRole.Pilot, a.Build.PilotRole);
        Assert.Equal(71, a.Accuracy);
        Assert.Equal(66, a.Maneuver);
        Assert.Equal(77, a.Initiative);

        var b = units["DRN-PLAYER-02"];
        Assert.Equal("CHR-CREW-B", b.Build!.PilotId);
        Assert.Equal("Viktor Hale", b.Build.PilotName);
        Assert.Equal(CrewGender.Male, b.Build.PilotGender);
        Assert.Equal(CrewRole.Pilot, b.Build.PilotRole);
        Assert.Equal(64, b.Accuracy);
        Assert.Equal(73, b.Maneuver);
        Assert.Equal(61, b.Initiative);
    }

    [Fact]
    public void LegacyWingFallsBackToCatalogPilotValues()
    {
        // A legacy wing whose crew record has both skills zero keeps the encounter alias's values.
        var crew = ImmutableArray.Create(Pilot("CHR-PILOT-1", accuracy: 0, maneuver: 0, initiative: 0));
        var wing = ImmutableArray.Create(Drone("DRN-PLAYER-01", "CHR-PILOT-1", "heavy.sword.shield", 12, 100, 100));

        var battle = OwnedBattleAssembly.Create(Catalog(), "SPC-0001", 400, 35, wing, crew,
            Catalog().Scenarios["initial"], "initial", 1);
        var unit = TacticalBattleSnapshotBuilder.Units(battle).Single(u => u.Id == "DRN-PLAYER-01");

        Assert.Equal(55, unit.Accuracy);  // pilots.json vera
        Assert.Equal(52, unit.Maneuver);
        Assert.Equal(55, unit.Initiative);
        Assert.Equal("CHR-PILOT-1", unit.Build!.PilotId); // identity is still the persistent CrewId
        Assert.Equal("CHR-PILOT-1", unit.Build.PilotName);
    }

    [Fact]
    public void DestroyedEarlierDroneDoesNotShiftPilot()
    {
        using var engine = LoadEngine();
        engine.SetOwnedWingForTests("SPC-0001", EngineWing());

        var first = engine.StartTacticalBattle(Owned("sortie-1"));
        Assert.True(first.Accepted);
        engine.TacticalBattleSessionForTests!.Battle.Drones
            .First(d => d.Side == "A" && d.Id == "DRN-PLAYER-01").Status = TacticalDroneStatus.Destroyed;
        engine.ExitTacticalBattle(new("exit-1", first.State!.BattleId));

        var second = engine.StartTacticalBattle(Owned("sortie-2", revision: engine.GetCarrierRevision("SPC-0001")));
        Assert.True(second.Accepted);
        var units = second.State!.Units
            .Where(u => u.Kind == TacticalUnitKind.Drone && u.Side == "A")
            .ToDictionary(u => u.Id, StringComparer.Ordinal);

        Assert.Equal(new[] { "DRN-PLAYER-02", "DRN-PLAYER-03", "DRN-PLAYER-04" },
            units.Keys.Order(StringComparer.Ordinal).ToArray());
        Assert.Equal("CHR-PLAYER-AUDREY", units["DRN-PLAYER-02"].Build!.PilotId);
        Assert.Equal("Audrey Bennett", units["DRN-PLAYER-02"].Build!.PilotName);
        Assert.Equal("CHR-PLAYER-CLAIRE", units["DRN-PLAYER-03"].Build!.PilotId);
        Assert.Equal("CHR-PLAYER-RACHEL", units["DRN-PLAYER-04"].Build!.PilotId);
    }

    [Fact]
    public void DisabledAndEngineDamageRestoredOnNextSortie()
    {
        var wing = ImmutableArray.Create(
            Drone("DRN-PLAYER-01", "CHR-PILOT-1", "heavy.sword.shield", 12, 100, 100) with
            {
                Disabled = true,
                EngineFactors = [1.0, 0.5],
            });

        var battle = OwnedBattleAssembly.Create(Catalog(), "SPC-0001", 400, 35, wing, FourCrew(),
            Catalog().Scenarios["initial"], "initial", 1);
        var drone = battle.Drones.Single(d => d.Id == "DRN-PLAYER-01");

        Assert.Equal(TacticalDroneStatus.Disabled, drone.Status);
        Assert.Equal(0, drone.Speed);
        Assert.Equal(1.0, drone.Engines[0].Factor);
        Assert.Equal(0.5, drone.Engines[1].Factor);
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

    private static JsonObject Crew(string id, string name, string role, int accuracy, int maneuver, int initiative)
    {
        var member = new JsonObject
        {
            ["crewId"] = id,
            ["displayName"] = name,
            ["role"] = role,
            ["accuracySkill"] = accuracy,
            ["maneuverabilitySkill"] = maneuver,
            ["initiative"] = initiative,
            ["gender"] = "Female",
        };
        if (role == "Pilot") member["portraitAssetId"] = $"Images/Persons/W4/Portraits/{id}.png";
        return member;
    }

    private SimulationEngine LoadEngine()
    {
        var ship = new JsonObject
        {
            ["objectId"] = "SPC-0001",
            ["objectType"] = "PlayerShip",
            ["shipClassId"] = "ship.tetrarch",
            ["persistenceType"] = "Permanent",
            ["name"] = "Skitalets",
            ["positionX"] = 0,
            ["positionY"] = 0,
            ["speedMps"] = 0,
            ["directionDegrees"] = 0,
            ["movementType"] = "Stationary",
            ["isKnown"] = true,
            ["hullLayout"] = HullLayout(),
            ["modules"] = new JsonArray(Bay("BAY-01", 0), Bay("BAY-02", 2)),
            ["crew"] = new JsonArray(
                Crew("CHR-0001", "Dunkan Su", "Captain", 0, 0, 0),
                Crew("CHR-PLAYER-DEFENSE", "Irina Volskaya", "Officer", 0, 0, 0),
                Crew("CHR-PLAYER-EVELYN", "Evelyn Ward", "Pilot", 55, 52, 55),
                Crew("CHR-PLAYER-AUDREY", "Audrey Bennett", "Pilot", 53, 55, 53),
                Crew("CHR-PLAYER-CLAIRE", "Claire Morgan", "Pilot", 59, 54, 59),
                Crew("CHR-PLAYER-RACHEL", "Rachel Hayes", "Pilot", 50, 51, 50)),
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
        string scenarioPath = Path.Combine(_directory, "scenario.json");
        File.WriteAllText(scenarioPath, scenario.ToJsonString());
        return EngineContentLoader.CreateEngineFromScenarioFile(Path.Combine(ClientRoot, "Settings.json"), scenarioPath);
    }

    private static OwnedDroneData[] EngineWing() =>
    [
        Drone("DRN-PLAYER-01", "CHR-PLAYER-EVELYN", "heavy.sword.shield", 12, 65, 65, bay: "BAY-01", slot: 0),
        Drone("DRN-PLAYER-02", "CHR-PLAYER-AUDREY", "heavy.spear.shield", 12, 65, 65, bay: "BAY-01", slot: 1),
        Drone("DRN-PLAYER-03", "CHR-PLAYER-CLAIRE", "light.sword.shield", 6, 55, 55, bay: "BAY-02", slot: 0),
        Drone("DRN-PLAYER-04", "CHR-PLAYER-RACHEL", "light.spear", 6, 55, 55, bay: "BAY-02", slot: 1),
    ];

    private static StartTacticalBattleRequest Owned(string commandId, string shipId = "SPC-0001", int revision = 0) =>
        new(commandId, 1, "initial") { SourceShipId = shipId, ExpectedCarrierRevision = revision };
}
