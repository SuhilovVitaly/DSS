using System.Text.Json;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// EP-0029 US-0011 TK-0002: the shipped starter carrier manifest (24-cell hull, 9 modules,
/// 6 crew + 2 reserved cabins, 4 drones, 12/12/6/6 loaded + 42 in the hold = 78 torpedoes) and the
/// two New Game scenario stubs that materialize it. Expected numbers are literal oracles from
/// Documentation.md ("Стартовый корабль и размещение", "Экипаж и портреты", "Крыло и торпеды").
/// </summary>
public sealed class Ep0029Us0011Tk0002Tests
{
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    private static string BaseDir => AppContext.BaseDirectory;
    private static string ManifestPath => Path.Combine(BaseDir, "Data", "Ships", "starter-carrier.json");

    internal static string TutorialScenarioPath => Path.Combine(BaseDir, "Scenarios", "Tutorial", "scenario.json");
    internal static string DefaultScenarioPath => Path.Combine(BaseDir, "Scenarios", "Default", "scenario.json");

    /// <summary>The approved 24-cell hull, rows y0..y5 (Documentation.md:102).</summary>
    private static readonly (int X, int Y)[] ApprovedHullCells =
    [
        (4, 0),
        (3, 1), (4, 1), (5, 1),
        (1, 2), (2, 2), (3, 2), (4, 2), (5, 2), (6, 2), (7, 2),
        (1, 3), (2, 3), (3, 3), (4, 3), (5, 3), (6, 3), (7, 3),
        (3, 4), (4, 4), (5, 4),
        (3, 5), (4, 5), (5, 5),
    ];

    private static readonly Dictionary<string, (int X, int Y)[]> ApprovedFootprints = new(StringComparer.Ordinal)
    {
        ["MOD-PLAYER-BRIDGE-01"] = [(4, 0)],
        ["MOD-PLAYER-LIVING-QUARTERS-01"] = [(3, 1)],
        ["MOD-PLAYER-LIVING-QUARTERS-02"] = [(5, 1)],
        ["MOD-PLAYER-CARGO-01"] = [(4, 2)],
        ["MOD-PLAYER-SCANNER-01"] = [(4, 3)],
        ["MOD-PLAYER-GENERATOR-01"] = [(4, 4)],
        ["MOD-PLAYER-ENGINE-01"] = [(4, 5)],
        ["MOD-PLAYER-BAY-01"] = [(1, 2), (2, 2), (1, 3), (2, 3)],
        ["MOD-PLAYER-BAY-02"] = [(6, 2), (7, 2), (6, 3), (7, 3)],
    };

    [Fact]
    public void StarterHasExactlySeventyEightTorpedoes()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(ManifestPath));
        var drones = manifest.RootElement.GetProperty("carrierState").GetProperty("ownedDrones").EnumerateArray().ToArray();
        Assert.Equal(4, drones.Length);

        // Literal per-drone magazine oracle: two heavy (12) and two light (6).
        var expectedAmmo = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["DRN-PLAYER-01"] = 12,
            ["DRN-PLAYER-02"] = 12,
            ["DRN-PLAYER-03"] = 6,
            ["DRN-PLAYER-04"] = 6,
        };
        foreach (var drone in drones)
        {
            string id = drone.GetProperty("droneId").GetString()!;
            Assert.True(expectedAmmo.ContainsKey(id), $"unexpected drone {id}");
            Assert.Equal(expectedAmmo[id], drone.GetProperty("ammoCurrent").GetInt32());
            Assert.Equal(expectedAmmo[id], drone.GetProperty("ammoCapacity").GetInt32());
        }

        int loaded = drones.Sum(d => d.GetProperty("ammoCurrent").GetInt32());
        int inHold = TorpedoesInHold(manifest.RootElement);
        Assert.Equal(36, loaded);
        Assert.Equal(42, inHold);
        Assert.Equal(78, loaded + inHold);

        // The production loader materializes the same quantities into both New Game stubs.
        foreach (string scenarioPath in new[] { TutorialScenarioPath, DefaultScenarioPath })
        {
            var scenario = ScenarioLoader.LoadFromFile(scenarioPath);
            var ship = PlayerShip(scenario);
            Assert.Equal(loaded + inHold, ShipTorpedoTotal(ship));
        }
    }

    [Fact]
    public void EightCabinsSixCrewTwoReserved()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(ManifestPath));
        var quarters = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Where(m => m.GetProperty("moduleTypeId").GetString() == "living.quarters.mk1.four-cabins")
            .ToArray();
        Assert.Equal(2, quarters.Length);

        // Cabin capacity comes from the shipped module type, not from the manifest under test.
        int cabinesPerModule = ShippedCabines("living.quarters.mk1.four-cabins");
        Assert.Equal(4, cabinesPerModule);
        Assert.Equal(8, quarters.Length * cabinesPerModule);

        var crew = manifest.RootElement.GetProperty("crew").EnumerateArray().ToArray();
        Assert.Equal(6, crew.Length);
        Assert.Equal(
            new[] { "CHR-0001", "CHR-PLAYER-DEFENSE", "CHR-PLAYER-EVELYN", "CHR-PLAYER-AUDREY", "CHR-PLAYER-CLAIRE", "CHR-PLAYER-RACHEL" },
            crew.Select(c => c.GetProperty("crewId").GetString()).ToArray());
        Assert.Equal(2, manifest.RootElement.GetProperty("carrierState").GetProperty("reservedCabins").GetInt32());

        var scenario = ScenarioLoader.LoadFromFile(TutorialScenarioPath);
        var ship = PlayerShip(scenario);
        Assert.Equal(6, ship.Crew!.Count);
        Assert.Equal(2, ship.CarrierState!.ReservedCabins);
        Assert.Equal(2, ship.Modules!.Count(m => m.ModuleTypeId == "living.quarters.mk1.four-cabins"));
    }

    [Fact]
    public void StarterFootprintsDoNotOverlap()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(ManifestPath));
        var hull = manifest.RootElement.GetProperty("hullLayout");
        Assert.Equal(9, hull.GetProperty("width").GetInt32());
        Assert.Equal(9, hull.GetProperty("height").GetInt32());
        var hullCells = hull.GetProperty("cells").EnumerateArray()
            .Select(c => (X: c.GetProperty("x").GetInt32(), Y: c.GetProperty("y").GetInt32())).ToArray();
        Assert.Equal(24, hullCells.Length);
        Assert.Equal(
            ApprovedHullCells.OrderBy(c => c.Y).ThenBy(c => c.X),
            hullCells.OrderBy(c => c.Y).ThenBy(c => c.X));

        var modules = manifest.RootElement.GetProperty("modules").EnumerateArray().ToArray();
        Assert.Equal(9, modules.Length);
        Assert.Equal(ApprovedFootprints.Keys.Order(StringComparer.Ordinal),
            modules.Select(m => m.GetProperty("moduleId").GetString()!).Order(StringComparer.Ordinal));

        var hullSet = hullCells.ToHashSet();
        var occupied = new HashSet<(int X, int Y)>();
        foreach (var module in modules)
        {
            string moduleId = module.GetProperty("moduleId").GetString()!;
            var cells = module.GetProperty("occupiedCells").EnumerateArray()
                .Select(c => (X: c.GetProperty("x").GetInt32(), Y: c.GetProperty("y").GetInt32())).ToArray();
            Assert.Equal(
                ApprovedFootprints[moduleId].OrderBy(c => c.Y).ThenBy(c => c.X),
                cells.OrderBy(c => c.Y).ThenBy(c => c.X));

            foreach (var cell in cells)
            {
                Assert.Contains(cell, hullSet);
                Assert.True(occupied.Add(cell), $"{moduleId} overlaps cell ({cell.X},{cell.Y})");
            }
        }

        Assert.Equal(15, occupied.Count);
        Assert.Equal(9, hullSet.Count - occupied.Count);
    }

    [Fact]
    public void SameCrewPortraitsAsApproved()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(ManifestPath));
        var crew = manifest.RootElement.GetProperty("crew").EnumerateArray().ToArray();

        var approved = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CHR-PLAYER-DEFENSE"] = "Images/Persons/W4/Portraits/CHR-20260915-104309-12BAB0.png",
            ["CHR-PLAYER-EVELYN"] = "Images/Persons/W4/Portraits/CHR-20260915-102445-3C5C5D.png",
            ["CHR-PLAYER-AUDREY"] = "Images/Persons/W4/Portraits/CHR-20260915-102448-E2757F.png",
            ["CHR-PLAYER-CLAIRE"] = "Images/Persons/W4/Portraits/CHR-20260915-104315-4DF131.png",
            ["CHR-PLAYER-RACHEL"] = "Images/Persons/W4/Portraits/CHR-20260915-102447-5F3DEF.png",
        };
        foreach (var member in crew)
        {
            string id = member.GetProperty("crewId").GetString()!;
            if (approved.TryGetValue(id, out string? expected))
            {
                Assert.Equal(expected, member.GetProperty("portraitAssetId").GetString());
                Assert.True(File.Exists(Path.Combine(ClientRoot, expected.Replace('/', Path.DirectorySeparatorChar))),
                    $"missing portrait asset {expected}");
                Assert.True(File.Exists(Path.Combine(BaseDir, expected.Replace('/', Path.DirectorySeparatorChar))),
                    $"portrait asset not copied to the client output: {expected}");
            }
            else
            {
                // The captain keeps the scenario's explicit captain portrait, not a crew portrait entry.
                Assert.Equal("CHR-0001", id);
                Assert.False(member.TryGetProperty("portraitAssetId", out _));
            }
        }

        // The scenario keeps the explicit Dunkan Su identity and the default-male captain portrait.
        foreach (string scenarioPath in new[] { TutorialScenarioPath, DefaultScenarioPath })
        {
            var scenario = ScenarioLoader.LoadFromFile(scenarioPath);
            var ship = PlayerShip(scenario);
            Assert.Equal("Skitalets", ship.Name);
            Assert.Equal("Dunkan Su", ship.CaptainDisplayName);
            Assert.Equal("Images/Persons/M4/Portraits/CHR-20260915-105947-6DC202.png", ship.CaptainPortraitImage);
        }
    }

    private static SpaceObjectData PlayerShip(ScenarioFile scenario) =>
        scenario.GameState.SpaceObjects.Single(o => o.ObjectId == scenario.GameState.PlayerShipObjectId);

    private static int TorpedoesInHold(JsonElement manifest) =>
        manifest.GetProperty("modules").EnumerateArray()
            .Where(m => m.GetProperty("moduleId").GetString() == "MOD-PLAYER-CARGO-01")
            .SelectMany(m => m.GetProperty("cargo").EnumerateArray())
            .Where(c => c.GetProperty("itemTypeId").GetString() == "item.drone-torpedo.t1")
            .Sum(c => c.GetProperty("quantity").GetInt32());

    private static int ShipTorpedoTotal(SpaceObjectData ship) =>
        ship.CarrierState!.OwnedDrones!.Sum(d => d.AmmoCurrent) +
        ship.Modules!.SelectMany(m => m.Cargo ?? [])
            .Where(c => c.ItemTypeId == "item.drone-torpedo.t1").Sum(c => (int)c.Quantity);

    private static int ShippedCabines(string moduleTypeId)
    {
        string path = Path.Combine(ClientRoot, "Data", "Modules", "LivingQuarters", "modules-livingquarters.json");
        using var file = JsonDocument.Parse(File.ReadAllText(path));
        return file.RootElement.GetProperty("moduleImplementations").EnumerateArray()
            .Single(m => m.GetProperty("typeId").GetString() == moduleTypeId)
            .GetProperty("cabines").GetInt32();
    }
}
