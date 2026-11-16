using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class Ep0029Us0007Tk0001Tests
{
    private static SpaceObjectData Ship(params ShipModuleData[] modules) =>
        new("SPC-0001", "PlayerShip", "Permanent", "Tetrarch", 0, 0, 0, 0, "Stationary",
            MassKg: null, CompositionType: null, ShipClassId: "ship.tetrarch", Modules: modules.Length == 0 ? null : modules);

    private static GameStateData State(params SpaceObjectData[] ships) =>
        new(0, "Speed0", "SPC-0001", null, ships, MasterSeed: 42);

    [Fact]
    public void Save20UpgradesWithoutGrant()
    {
        var v20 = new ScenarioFile(new ScenarioMetadata("s", "s"), State(Ship()), SaveFormatVersion: 20);
        var v21 = SaveMigration21.Upgrade(v20);
        Assert.Equal(21, v21.SaveFormatVersion);
        Assert.Null(v21.GameState.SpaceObjects[0].CarrierState); // legacy ship: no carrier surface, no granted assets
    }

    [Fact]
    public void NewTorpedoesSurviveSave21()
    {
        var cargo = new[] { new CargoStackData("item.drone-torpedo.t1", 42) };
        var module = new ShipModuleData("MOD-CARGO", "module.container.basic", new[] { new HullCellCoordinate(4, 2) },
            60, "On", "Ready", null, cargo);
        var world = SaveMigration20.WorldOnly(State(Ship(module)));
        Assert.Contains(world.SpaceObjects[0].Modules![0].Cargo!, c => c.ItemTypeId == "item.drone-torpedo.t1" && c.Quantity == 42);
    }

    [Fact]
    public void Save18And19StillRemoveLegacyProjectiles()
    {
        var legacy = new[] { new CargoStackData("item.ammo.torpedo.t1", 5) };
        var module = new ShipModuleData("MOD-CARGO", "module.container.basic", new[] { new HullCellCoordinate(4, 2) },
            60, "On", "Ready", null, legacy);
        var world = SaveMigration20.WorldOnly(State(Ship(module)));
        Assert.DoesNotContain(world.SpaceObjects[0].Modules![0].Cargo!, c => c.ItemTypeId.StartsWith("item.ammo.torpedo", StringComparison.Ordinal));
    }

    [Fact]
    public void FutureVersionAndInvalidRefsReject()
    {
        // Unknown version is rejected before any session is published.
        Assert.Throws<ScenarioException>(() => ScenarioLoader.RequireSupportedSaveVersion(23));
        // Invalid reference: playerShipObjectId points at an object absent from spaceObjects.
        var invalidRefs = new ScenarioFile(
            new ScenarioMetadata("s", "s"),
            new GameStateData(0, "Speed0", "MISSING-SHIP", null, new[] { Ship() },
                MasterSeed: 42,
                SimulationTimeMs: 0,
                CatalogCompatibility: new CatalogCompatibilityData(1, 1, "placeholder")),
            SaveFormatVersion: 21);
        Assert.Throws<ScenarioException>(() => ScenarioLoader.ValidateAndNormalize(invalidRefs, allowNonZeroGameTime: true));
    }
}
