using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// EP-0029 US-0011 TK-0001: one starter carrier manifest is materialized into the world only when a
/// New Game scenario references it; saves carry the expanded state and never re-grant the manifest.
/// The fixture manifest duplicates the approved starter numbers independently of the content file
/// (which is authored in TK-0002), so this suite never depends on the not-yet-shipped manifest.
/// </summary>
public sealed class Ep0029Us0011Tk0001Tests : IDisposable
{
    private const string PlayerShipId = "SPC-0001";
    private const string ManifestReference = "Data/Ships/starter-carrier.json";

    private readonly string _root = Path.Combine(Path.GetTempPath(), $"dss-ep0029-us0011-tk0001-{Guid.NewGuid():N}");

    private string ManifestPath => Path.Combine(_root, "Data", "Ships", "starter-carrier.json");
    private string DefaultScenarioPath => Path.Combine(_root, "Scenarios", "Default", "scenario.json");
    private string TutorialScenarioPath => Path.Combine(_root, "Scenarios", "Tutorial", "scenario.json");

    public Ep0029Us0011Tk0001Tests()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ManifestPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(DefaultScenarioPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(TutorialScenarioPath)!);
        File.WriteAllText(ManifestPath, FixtureManifest);
        File.WriteAllText(DefaultScenarioPath, ScenarioStub("default"));
        File.WriteAllText(TutorialScenarioPath, ScenarioStub("tutorial", metadataTail: ", \"hidden\": true"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch (IOException) { /* best effort */ }
    }

    [Fact]
    public void DefaultAndTutorialResolveSameCarrier()
    {
        // Default passes the content root explicitly (the EngineContentLoader settings-directory path);
        // Tutorial infers the root by walking up from its own directory (the shipped layout).
        var defaultScenario = ScenarioLoader.LoadFromFile(DefaultScenarioPath, contentRoot: _root);
        var tutorialScenario = ScenarioLoader.LoadFromFile(TutorialScenarioPath);

        var defaultShip = PlayerShip(defaultScenario);
        var tutorialShip = PlayerShip(tutorialScenario);

        // The materialized world is byte-for-byte the same carrier; only scenario metadata differs.
        Assert.Equal(
            ScenarioLoader.Serialize(defaultScenario with { Metadata = tutorialScenario.Metadata }),
            ScenarioLoader.Serialize(tutorialScenario));

        Assert.Equal(24, defaultShip.HullLayout!.Cells.Count);
        Assert.Equal(9, defaultShip.Modules!.Count);
        Assert.Equal(6, defaultShip.Crew!.Count);
        var carrier = Assert.IsType<CarrierStateData>(defaultShip.CarrierState);
        Assert.Equal(4, carrier.OwnedDrones!.Count);

        // 12 + 12 + 6 + 6 loaded + 42 in the hold = 78.
        int loaded = carrier.OwnedDrones.Sum(d => d.AmmoCurrent);
        int inHold = defaultShip.Modules!.SelectMany(m => m.Cargo ?? [])
            .Where(c => c.ItemTypeId == "item.drone-torpedo.t1").Sum(c => (int)c.Quantity);
        Assert.Equal(36, loaded);
        Assert.Equal(42, inHold);
        Assert.Equal(78, loaded + inHold);

        // The live grant never leaves the loader in the returned metadata.
        Assert.Null(defaultScenario.Metadata.StarterCarrierManifest);
        Assert.Null(tutorialScenario.Metadata.StarterCarrierManifest);

        Assert.Equal(defaultShip.Modules!.Select(m => m.ModuleId), tutorialShip.Modules!.Select(m => m.ModuleId));
        Assert.Equal(defaultShip.Crew!.Select(c => c.CrewId), tutorialShip.Crew!.Select(c => c.CrewId));
        Assert.Equal(
            defaultShip.CarrierState!.OwnedDrones!.Select(d => (d.DroneId, d.AmmoCurrent, d.AmmoCapacity)),
            tutorialShip.CarrierState!.OwnedDrones!.Select(d => (d.DroneId, d.AmmoCurrent, d.AmmoCapacity)));
    }

    [Fact]
    public void LoadNeverReappliesManifest()
    {
        var expanded = ScenarioLoader.LoadFromFile(DefaultScenarioPath, contentRoot: _root);
        var ship = PlayerShip(expanded);

        // Battle losses: one heavy drone fired five torpedoes and five were spent from the hold.
        var carrier = ship.CarrierState!;
        var reducedCarrier = carrier with
        {
            Revision = 3,
            OwnedDrones = carrier.OwnedDrones!.Select(d =>
                d.DroneId == "DRN-PLAYER-01" ? d with { AmmoCurrent = 7 } : d).ToArray(),
        };
        var reducedModules = ship.Modules!.Select(m => m.Cargo is null
            ? m
            : m with
            {
                Cargo = m.Cargo.Select(c => c.ItemTypeId == "item.drone-torpedo.t1"
                    ? c with { Quantity = 5 }
                    : c).ToArray(),
            }).ToArray();

        var save = expanded with
        {
            SaveFormatVersion = SaveFormat.CurrentSaveFormatVersion,
            // A stale save that still names the manifest must not re-grant anything.
            Metadata = expanded.Metadata with { StarterCarrierManifest = ManifestReference },
            GameState = expanded.GameState with
            {
                MasterSeed = 1,
                SimulationTimeMs = 0,
                CatalogCompatibility = new CatalogCompatibilityData(1, EconomyTimeData.CurrentRulesVersion, new string('A', 64)),
                EconomyTime = new EconomyTimeData(),
                MarketKnowledge = [],
                TradingEconomyContinuation = new TradingEconomyContinuationData(1, new string('A', 64), 0, 0, 0),
                SpaceObjects = expanded.GameState.SpaceObjects.Select(o => o.ObjectId == PlayerShipId
                    ? o with
                    {
                        Modules = reducedModules
                            .Select(m => m.Cargo is null
                                ? m
                                : m with
                                {
                                    Cargo = m.Cargo
                                        .Select(c => c with { CostBasisCredits = 0, AcquisitionSources = ["bootstrap"] })
                                        .ToArray(),
                                })
                            .ToArray(),
                        CarrierState = reducedCarrier,
                    }
                    : o).ToArray(),
            },
        };

        var reloaded = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), allowNonZeroGameTime: true, isSave: true);
        var reloadedShip = PlayerShip(reloaded);

        Assert.Null(reloaded.Metadata.StarterCarrierManifest);
        Assert.Equal(7, reloadedShip.CarrierState!.OwnedDrones!.Single(d => d.DroneId == "DRN-PLAYER-01").AmmoCurrent);
        Assert.Equal(3, reloadedShip.CarrierState.Revision);
        Assert.Equal(5, reloadedShip.Modules!.SelectMany(m => m.Cargo ?? [])
            .Single(c => c.ItemTypeId == "item.drone-torpedo.t1").Quantity);
        Assert.Equal(9, reloadedShip.Modules!.Count);
        Assert.Equal(400, reloadedShip.CarrierState.HullCurrent);
    }

    [Fact]
    public void ConflictingInlineAndManifestReject()
    {
        const string inlineModules = ", \"modules\": [" +
            "{ \"moduleId\": \"MOD-INLINE\", \"moduleTypeId\": \"module.container.basic\", " +
            "\"occupiedCells\": [ { \"x\": 4, \"y\": 2 } ], \"structurePoints\": 150, " +
            "\"powerState\": \"On\", \"operationalState\": \"Ready\", \"cargo\": [] } ]";
        File.WriteAllText(DefaultScenarioPath, ScenarioStub("default", shipTail: inlineModules));

        var exception = Assert.Throws<ScenarioException>(() =>
            ScenarioLoader.LoadFromFile(DefaultScenarioPath, contentRoot: _root));
        Assert.Contains("starterCarrierManifest", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TraversalRejected()
    {
        File.WriteAllText(DefaultScenarioPath, ScenarioStub("default", "../outside/starter.json"));

        var exception = Assert.Throws<ScenarioException>(() =>
            ScenarioLoader.LoadFromFile(DefaultScenarioPath, contentRoot: _root));
        Assert.Contains("starterCarrierManifest", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RootedManifestPathRejected()
    {
        File.WriteAllText(DefaultScenarioPath, ScenarioStub("default", ManifestPath));

        Assert.Throws<ScenarioException>(() =>
            ScenarioLoader.LoadFromFile(DefaultScenarioPath, contentRoot: _root));
    }

    [Fact]
    public void MissingManifestFileRejected()
    {
        File.WriteAllText(DefaultScenarioPath, ScenarioStub("default", "Data/Ships/missing.json"));

        var exception = Assert.Throws<ScenarioException>(() =>
            ScenarioLoader.LoadFromFile(DefaultScenarioPath, contentRoot: _root));
        Assert.Contains("starterCarrierManifest", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadFromJsonRejectsNewGameManifest()
    {
        string json = File.ReadAllText(DefaultScenarioPath);

        var exception = Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(json));
        Assert.Contains("starterCarrierManifest", exception.Message, StringComparison.Ordinal);
    }

    private static SpaceObjectData PlayerShip(ScenarioFile scenario) =>
        scenario.GameState.SpaceObjects.Single(o => o.ObjectId == scenario.GameState.PlayerShipObjectId);

    private static string ScenarioStub(string id, string? manifest = ManifestReference,
        string metadataTail = "", string shipTail = "") => $$"""
    {
      "scenarioMetadata": { "scenarioId": "{{id}}", "name": "{{id}}"{{(manifest is null ? "" : $", \"starterCarrierManifest\": \"{manifest}\"")}}{{metadataTail}} },
      "gameState": {
        "gameTimeMs": 0, "currentSpeed": "Speed0", "playerShipObjectId": "SPC-0001",
        "spaceObjects": [
          { "objectId": "SPC-0001", "objectType": "PlayerShip", "shipClassId": "ship.tetrarch",
            "persistenceType": "Permanent", "name": "Skitalets",
            "positionX": 10000, "positionY": 10000, "speedMps": 700, "directionDegrees": 0,
            "movementType": "Linear"{{shipTail}} }
        ]
      }
    }
    """;

    private const string FixtureManifest = """
    {
      "schemaVersion": 1,
      "shipClassId": "ship.tetrarch",
      "hullLayout": {
        "width": 9,
        "height": 9,
        "cells": [
          { "x": 4, "y": 0 },
          { "x": 3, "y": 1 }, { "x": 4, "y": 1 }, { "x": 5, "y": 1 },
          { "x": 1, "y": 2 }, { "x": 2, "y": 2 }, { "x": 3, "y": 2 }, { "x": 4, "y": 2 },
          { "x": 5, "y": 2 }, { "x": 6, "y": 2 }, { "x": 7, "y": 2 },
          { "x": 1, "y": 3 }, { "x": 2, "y": 3 }, { "x": 3, "y": 3 }, { "x": 4, "y": 3 },
          { "x": 5, "y": 3 }, { "x": 6, "y": 3 }, { "x": 7, "y": 3 },
          { "x": 3, "y": 4 }, { "x": 4, "y": 4 }, { "x": 5, "y": 4 },
          { "x": 3, "y": 5 }, { "x": 4, "y": 5 }, { "x": 5, "y": 5 }
        ]
      },
      "modules": [
        { "moduleId": "MOD-PLAYER-BRIDGE-01", "moduleTypeId": "module.bridge.navigation.computer.basic",
          "occupiedCells": [ { "x": 4, "y": 0 } ], "structurePoints": 150, "powerState": "On",
          "operationalState": "Ready", "activeCycle": null, "cargo": [] },
        { "moduleId": "MOD-PLAYER-LIVING-QUARTERS-01", "moduleTypeId": "living.quarters.mk1.four-cabins",
          "occupiedCells": [ { "x": 3, "y": 1 } ], "structurePoints": 150, "powerState": "On",
          "operationalState": "Ready", "activeCycle": null, "cargo": [] },
        { "moduleId": "MOD-PLAYER-LIVING-QUARTERS-02", "moduleTypeId": "living.quarters.mk1.four-cabins",
          "occupiedCells": [ { "x": 5, "y": 1 } ], "structurePoints": 150, "powerState": "On",
          "operationalState": "Ready", "activeCycle": null, "cargo": [] },
        { "moduleId": "MOD-PLAYER-CARGO-01", "moduleTypeId": "module.container.basic",
          "occupiedCells": [ { "x": 4, "y": 2 } ], "structurePoints": 150, "powerState": "On",
          "operationalState": "Ready", "activeCycle": null,
          "cargo": [
            { "itemTypeId": "item.energy-cells", "quantity": 1000 },
            { "itemTypeId": "item.food-rations", "quantity": 200 },
            { "itemTypeId": "item.drone-torpedo.t1", "quantity": 42 }
          ] },
        { "moduleId": "MOD-PLAYER-SCANNER-01", "moduleTypeId": "module.scanner.mk1",
          "occupiedCells": [ { "x": 4, "y": 3 } ], "structurePoints": 150, "powerState": "On",
          "operationalState": "Ready", "activeCycle": null, "cargo": [] },
        { "moduleId": "MOD-PLAYER-GENERATOR-01", "moduleTypeId": "module.generator.basic",
          "occupiedCells": [ { "x": 4, "y": 4 } ], "structurePoints": 150, "powerState": "On",
          "operationalState": "Ready", "activeCycle": null, "cargo": [] },
        { "moduleId": "MOD-PLAYER-ENGINE-01", "moduleTypeId": "module.engine.basic",
          "occupiedCells": [ { "x": 4, "y": 5 } ], "structurePoints": 150, "powerState": "On",
          "operationalState": "Ready", "activeCycle": null, "cargo": [], "fuelAmountKg": 750 },
        { "moduleId": "MOD-PLAYER-BAY-01", "moduleTypeId": "module.drone-bay.medium",
          "occupiedCells": [ { "x": 1, "y": 2 }, { "x": 2, "y": 2 }, { "x": 1, "y": 3 }, { "x": 2, "y": 3 } ],
          "structurePoints": 250, "powerState": "On", "operationalState": "Ready",
          "activeCycle": null, "cargo": [] },
        { "moduleId": "MOD-PLAYER-BAY-02", "moduleTypeId": "module.drone-bay.medium",
          "occupiedCells": [ { "x": 6, "y": 2 }, { "x": 7, "y": 2 }, { "x": 6, "y": 3 }, { "x": 7, "y": 3 } ],
          "structurePoints": 250, "powerState": "On", "operationalState": "Ready",
          "activeCycle": null, "cargo": [] }
      ],
      "crew": [
        { "crewId": "CHR-0001", "displayName": "Dunkan Su", "accuracySkill": 0,
          "maneuverabilitySkill": 0, "gender": "Male", "role": "Captain", "initiative": 0 },
        { "crewId": "CHR-PLAYER-DEFENSE", "displayName": "Irina Volskaya", "accuracySkill": 0,
          "maneuverabilitySkill": 0, "gender": "Female",
          "portraitAssetId": "Images/Persons/W4/Portraits/CHR-20260915-104309-12BAB0.png",
          "role": "Officer", "initiative": 0 },
        { "crewId": "CHR-PLAYER-EVELYN", "displayName": "Evelyn Ward", "accuracySkill": 0,
          "maneuverabilitySkill": 0, "gender": "Female",
          "portraitAssetId": "Images/Persons/W4/Portraits/CHR-20260915-102445-3C5C5D.png",
          "role": "Pilot", "initiative": 55 },
        { "crewId": "CHR-PLAYER-AUDREY", "displayName": "Audrey Bennett", "accuracySkill": 0,
          "maneuverabilitySkill": 0, "gender": "Female",
          "portraitAssetId": "Images/Persons/W4/Portraits/CHR-20260915-102448-E2757F.png",
          "role": "Pilot", "initiative": 53 },
        { "crewId": "CHR-PLAYER-CLAIRE", "displayName": "Claire Morgan", "accuracySkill": 0,
          "maneuverabilitySkill": 0, "gender": "Female",
          "portraitAssetId": "Images/Persons/W4/Portraits/CHR-20260915-104315-4DF131.png",
          "role": "Pilot", "initiative": 59 },
        { "crewId": "CHR-PLAYER-RACHEL", "displayName": "Rachel Hayes", "accuracySkill": 0,
          "maneuverabilitySkill": 0, "gender": "Female",
          "portraitAssetId": "Images/Persons/W4/Portraits/CHR-20260915-102447-5F3DEF.png",
          "role": "Pilot", "initiative": 50 }
      ],
      "carrierState": {
        "schemaVersion": 1,
        "revision": 0,
        "hullCurrent": 400,
        "reservedCabins": 2,
        "ownedDrones": [
          { "droneId": "DRN-PLAYER-01", "hangarModuleId": "MOD-PLAYER-BAY-01", "slotIndex": 0,
            "crewId": "CHR-PLAYER-EVELYN", "frameId": "heavy.sword.shield", "weaponId": "gun.sword.t1",
            "armorId": "plate.medium", "shieldId": "shield.t1", "shieldCount": 2, "shieldSlots": 2,
            "decoyId": "decoy.t1", "decoyMaxCharges": 2, "decoyCharges": 2,
            "currentHull": 65, "maxHull": 65, "armor": 80, "maxArmor": 80,
            "ammoCurrent": 12, "ammoCapacity": 12, "jammed": false,
            "engineIds": [ "drive.standard", "drive.standard" ], "destroyed": false },
          { "droneId": "DRN-PLAYER-02", "hangarModuleId": "MOD-PLAYER-BAY-01", "slotIndex": 1,
            "crewId": "CHR-PLAYER-AUDREY", "frameId": "heavy.spear.shield", "weaponId": "gun.spear.t1",
            "armorId": "plate.medium", "shieldId": "shield.t1", "shieldCount": 2, "shieldSlots": 2,
            "decoyId": "decoy.t1", "decoyMaxCharges": 2, "decoyCharges": 2,
            "currentHull": 65, "maxHull": 65, "armor": 80, "maxArmor": 80,
            "ammoCurrent": 12, "ammoCapacity": 12, "jammed": false,
            "engineIds": [ "drive.standard", "drive.standard" ], "destroyed": false },
          { "droneId": "DRN-PLAYER-03", "hangarModuleId": "MOD-PLAYER-BAY-02", "slotIndex": 0,
            "crewId": "CHR-PLAYER-CLAIRE", "frameId": "light.sword.shield", "weaponId": "gun.sword.t1",
            "armorId": "plate.medium", "shieldId": "shield.t1", "shieldCount": 1, "shieldSlots": 1,
            "decoyId": "decoy.t1", "decoyMaxCharges": 2, "decoyCharges": 2,
            "currentHull": 55, "maxHull": 55, "armor": 80, "maxArmor": 80,
            "ammoCurrent": 6, "ammoCapacity": 6, "jammed": false,
            "engineIds": [ "drive.standard", "drive.standard" ], "destroyed": false },
          { "droneId": "DRN-PLAYER-04", "hangarModuleId": "MOD-PLAYER-BAY-02", "slotIndex": 1,
            "crewId": "CHR-PLAYER-RACHEL", "frameId": "light.spear", "weaponId": "gun.spear.t1",
            "armorId": "plate.medium", "shieldId": "shield.t1", "shieldCount": 1, "shieldSlots": 1,
            "decoyId": "decoy.t1", "decoyMaxCharges": 2, "decoyCharges": 2,
            "currentHull": 55, "maxHull": 55, "armor": 80, "maxArmor": 80,
            "ammoCurrent": 6, "ammoCapacity": 6, "jammed": false,
            "engineIds": [ "drive.standard", "drive.standard" ], "destroyed": false }
        ]
      }
    }
    """;
}
