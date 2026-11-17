using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Scenario;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// EP-0029 US-0013 TK-0003: content acceptance for the starter carrier assets and the finite-ammo
/// budget. Oracle values are literals from Documentation.md: 24 hull cells / 15 occupied / two
/// four-cabin quarters (8 beds) / 6 crew / 2 reserved / 4 drones / 12+12+6+6 loaded + 42 hold = 78.
/// The 1..100 seed matrix is a measurement report — it never asserts a win probability.
/// </summary>
public sealed class Ep0029Us0013Tk0003Tests
{
    private const string TorpedoItemTypeId = "item.drone-torpedo.t1";
    private const string ManifestReference = "Data/Ships/starter-carrier.json";

    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        TestPaths.Repository, "src", "DeepSpaceSaga.Client"));

    private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "Settings.json");
    private static string TutorialPath => Path.Combine(AppContext.BaseDirectory, "Scenarios", "Tutorial", "scenario.json");
    private static string DefaultPath => Path.Combine(AppContext.BaseDirectory, "Scenarios", "Default", "scenario.json");

    private static string ManifestPath(string root) => Path.Combine(root, "Data", "Ships", "starter-carrier.json");

    private static JsonElement PlayerShip(JsonElement root) => root.GetProperty("gameState").GetProperty("spaceObjects")
        .EnumerateArray().Single(ship => ship.GetProperty("objectId").GetString() ==
            root.GetProperty("gameState").GetProperty("playerShipObjectId").GetString());

    private static string ReportDirectory(string ticket)
    {
        string root = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? "";
        if (string.IsNullOrWhiteSpace(root))
            root = Path.Combine(Path.GetTempPath(), "dss-validation-ep0029");
        string directory = Path.Combine(root, ticket);
        Directory.CreateDirectory(directory);
        return directory;
    }

    [Fact]
    public void ManifestAndPortraitAssetsResolve()
    {
        // Both New Game stubs reference the single starter manifest and never inline a carrier.
        foreach (string scenarioPath in new[] { DefaultPath, TutorialPath })
        {
            using var scenario = JsonDocument.Parse(File.ReadAllText(scenarioPath));
            var metadata = scenario.RootElement.GetProperty("scenarioMetadata");
            Assert.Equal(ManifestReference, metadata.GetProperty("starterCarrierManifest").GetString());
            var ship = PlayerShip(scenario.RootElement);
            Assert.False(ship.TryGetProperty("hullLayout", out _));
            Assert.False(ship.TryGetProperty("modules", out _));
            Assert.False(ship.TryGetProperty("carrierState", out _));
        }

        // The same manifest and every referenced portrait resolve from the source tree and from
        // the shipped client output; each PNG decodes to a real bitmap.
        string[] roots = [ClientRoot, AppContext.BaseDirectory];
        Dictionary<string, string> sourcePortraits = new(StringComparer.Ordinal);
        foreach (string root in roots)
        {
            string manifestPath = ManifestPath(root);
            Assert.True(File.Exists(manifestPath), $"missing starter manifest at {manifestPath}");
            using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
            Assert.Equal(1, manifest.RootElement.GetProperty("schemaVersion").GetInt32());
            Assert.Equal("ship.tetrarch", manifest.RootElement.GetProperty("shipClassId").GetString());

            var portraits = manifest.RootElement.GetProperty("crew").EnumerateArray()
                .Where(member => member.TryGetProperty("portraitAssetId", out _))
                .ToDictionary(member => member.GetProperty("crewId").GetString()!,
                    member => member.GetProperty("portraitAssetId").GetString()!, StringComparer.Ordinal);
            Assert.Equal(5, portraits.Count);
            if (sourcePortraits.Count == 0) sourcePortraits = portraits;
            else
            {
                Assert.Equal(
                    sourcePortraits.OrderBy(pair => pair.Key, StringComparer.Ordinal),
                    portraits.OrderBy(pair => pair.Key, StringComparer.Ordinal));
            }

            foreach (string relative in portraits.Values.Append(CaptainPortrait()))
            {
                string path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
                Assert.True(File.Exists(path), $"missing portrait asset {relative} under {root}");
                using var bitmap = SKBitmap.Decode(path);
                Assert.NotNull(bitmap);
                Assert.True(bitmap.Width > 0 && bitmap.Height > 0, $"undecodable portrait {relative}");
            }
        }

        // The four tactical pilot aliases share the persistent crew portraits in both locales;
        // locale files themselves resolve the tutorial text (names/portraits are locale-neutral).
        using var pilots = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Data", "UI", "tactical-battle-pilots.json")));
        var aliasPortraits = pilots.RootElement.GetProperty("pilots").EnumerateObject()
            .Select(alias => "Images/Persons/" + alias.Value.GetProperty("portrait").GetString() + ".png")
            .ToArray();
        Assert.Equal(4, aliasPortraits.Length);
        Assert.All(aliasPortraits, portrait => Assert.Contains(portrait, sourcePortraits.Values));
        foreach (string language in new[] { "Russian", "English" })
        {
            var locale = Assert.IsType<Dictionary<string, string>>(Localization.LoadLocaleFile(language));
            foreach (string key in new[] { "Tutorial.Line.Intro", "Tutorial.Line.OverVictory", "Tutorial.Line.OverDefeat" })
            {
                Assert.True(locale.TryGetValue(key, out string? text) && !string.IsNullOrWhiteSpace(text));
                Assert.False(string.Equals(key, text, StringComparison.Ordinal));
            }
        }
    }

    private static string CaptainPortrait()
    {
        // The scenarios keep the explicit captain identity; the portrait is not a crew entry.
        using var scenario = JsonDocument.Parse(File.ReadAllText(DefaultPath));
        return PlayerShip(scenario.RootElement).GetProperty("captainPortraitImage").GetString()!;
    }

    [Fact]
    public void StarterTotalsIndependentOracle()
    {
        // The source and shipped manifests are the same content file.
        Assert.True(JsonNode.DeepEquals(
            JsonNode.Parse(File.ReadAllText(ManifestPath(ClientRoot))),
            JsonNode.Parse(File.ReadAllText(ManifestPath(AppContext.BaseDirectory)))));

        using var manifest = JsonDocument.Parse(File.ReadAllText(ManifestPath(ClientRoot)));
        var hull = manifest.RootElement.GetProperty("hullLayout");
        Assert.Equal(9, hull.GetProperty("width").GetInt32());
        Assert.Equal(9, hull.GetProperty("height").GetInt32());
        var hullCells = hull.GetProperty("cells").EnumerateArray()
            .Select(cell => (X: cell.GetProperty("x").GetInt32(), Y: cell.GetProperty("y").GetInt32())).ToArray();
        Assert.Equal(24, hullCells.Length);
        Assert.Equal(24, hullCells.Distinct().Count());

        var modules = manifest.RootElement.GetProperty("modules").EnumerateArray().ToArray();
        Assert.Equal(9, modules.Length);
        var occupied = modules.SelectMany(module => module.GetProperty("occupiedCells").EnumerateArray()
            .Select(cell => (X: cell.GetProperty("x").GetInt32(), Y: cell.GetProperty("y").GetInt32()))).ToArray();
        Assert.Equal(15, occupied.Distinct().Count());
        Assert.All(occupied, cell => Assert.Contains(cell, hullCells));

        var quarters = modules.Where(module =>
            module.GetProperty("moduleTypeId").GetString() == "living.quarters.mk1.four-cabins").ToArray();
        Assert.Equal(2, quarters.Length);
        Assert.Equal(8, quarters.Length * ShippedCabins(ClientRoot, "living.quarters.mk1.four-cabins"));
        Assert.Equal(2, manifest.RootElement.GetProperty("carrierState").GetProperty("reservedCabins").GetInt32());

        var crew = manifest.RootElement.GetProperty("crew").EnumerateArray().ToArray();
        Assert.Equal(6, crew.Length);
        Assert.Equal(
            ["CHR-0001", "CHR-PLAYER-DEFENSE", "CHR-PLAYER-EVELYN", "CHR-PLAYER-AUDREY",
                "CHR-PLAYER-CLAIRE", "CHR-PLAYER-RACHEL"],
            crew.Select(member => member.GetProperty("crewId").GetString()));

        var drones = manifest.RootElement.GetProperty("carrierState").GetProperty("ownedDrones")
            .EnumerateArray().ToArray();
        Assert.Equal(4, drones.Length);
        Assert.Equal(new[] { 12, 12, 6, 6 }, drones.Select(drone => drone.GetProperty("ammoCurrent").GetInt32()));
        int loaded = drones.Sum(drone => drone.GetProperty("ammoCurrent").GetInt32());
        int hold = modules.Where(module => module.GetProperty("moduleId").GetString() == "MOD-PLAYER-CARGO-01")
            .SelectMany(module => module.GetProperty("cargo").EnumerateArray())
            .Where(stack => stack.GetProperty("itemTypeId").GetString() == TorpedoItemTypeId)
            .Sum(stack => stack.GetProperty("quantity").GetInt32());
        Assert.Equal(36, loaded);
        Assert.Equal(42, hold);
        Assert.Equal(78, loaded + hold);

        // The production loader materializes the same totals for both New Game stubs.
        foreach (string scenarioPath in new[] { DefaultPath, TutorialPath })
        {
            var scenario = ScenarioLoader.LoadFromFile(scenarioPath, contentRoot: AppContext.BaseDirectory);
            var ship = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == scenario.GameState.PlayerShipObjectId);
            Assert.Equal(24, ship.HullLayout!.Cells.Count);
            Assert.Equal(9, ship.Modules!.Count);
            Assert.Equal(6, ship.Crew!.Count);
            Assert.Equal(2, ship.CarrierState!.ReservedCabins);
            Assert.Equal(78, ship.CarrierState.OwnedDrones!.Sum(drone => drone.AmmoCurrent) +
                ship.Modules.SelectMany(module => module.Cargo ?? [])
                    .Where(stack => stack.ItemTypeId == TorpedoItemTypeId).Sum(stack => (int)stack.Quantity));
        }
    }

    private static int ShippedCabins(string root, string moduleTypeId)
    {
        string path = Path.Combine(root, "Data", "Modules", "LivingQuarters", "modules-livingquarters.json");
        using var file = JsonDocument.Parse(File.ReadAllText(path));
        return file.RootElement.GetProperty("moduleImplementations").EnumerateArray()
            .Single(module => module.GetProperty("typeId").GetString() == moduleTypeId)
            .GetProperty("cabines").GetInt32();
    }

    [Fact, Trait("Category", "Balance")]
    public void FiniteAmmoSeedMatrixProducesReport()
    {
        var lines = new List<string>(100);
        for (long seed = 1; seed <= 100; seed++)
        {
            using var engine = SimulationEngine.CreateFromScenarioFile(SettingsPath, TutorialPath);
            engine.SetSpeed(SimulationSpeed.Speed0);
            var carrier = engine.CaptureSnapshot().CarrierOperations!;
            int loadedBefore = carrier.OwnedDrones.Sum(drone => drone.AmmoCurrent);
            var start = engine.StartTacticalBattle(new StartTacticalBattleRequest($"matrix-start-{seed}", seed, "initial")
            {
                SourceShipId = carrier.ShipId,
                ExpectedCarrierRevision = carrier.Revision,
            });
            Assert.True(start.Accepted, $"seed {seed}: owned tutorial start rejected: {start.Code}");
            var state = start.State!;
            // rules.json seriesTurnCap=100; each turn is one Move + one Fire phase (with slack).
            for (int step = 0; step < 240 && state.Outcome is null; step++)
            {
                TacticalBattleResponse response;
                if (state.Phase == TacticalBattlePhase.Move)
                {
                    response = engine.MoveTacticalBattle(
                        new MoveTacticalBattleRequest($"matrix-move-{seed}-{step}", state.BattleId, state.Revision));
                }
                else if (state.Phase == TacticalBattlePhase.Fire)
                {
                    response = engine.FireTacticalBattle(new FireTacticalBattleRequest(
                        $"matrix-fire-{seed}-{step}", state.BattleId, state.Revision,
                        [.. state.Targets.Select(target => new TacticalFireTarget(target.DroneId, target.DefaultTargetId))]));
                }
                else break;
                Assert.True(response.Accepted, $"seed {seed}: turn {state.Turn} {state.Phase} rejected: {response.Code}");
                state = response.State!;
            }
            var outcome = state.Outcome;
            Assert.NotNull(outcome);
            var exit = engine.ExitTacticalBattle(new ExitTacticalBattleRequest($"matrix-exit-{seed}", state.BattleId));
            Assert.True(exit.Accepted, $"seed {seed}: exit rejected: {exit.Code}");
            var settlement = exit.Settlement!;
            Assert.InRange(settlement.ConsumedTorpedoes, 0, loadedBefore);
            string losses = string.Join(",", state.Losses.Select(loss => $"{loss.Side}:{loss.Count}"));
            string lost = string.Join(",", settlement.LostDroneIds);
            lines.Add($"seed={seed} outcome={outcome!.Kind} turn={state.Turn} exit={settlement.ExitKind} " +
                $"shipDestroyed={settlement.ShipDestroyed} losses=[{losses}] lost=[{lost}] consumed={settlement.ConsumedTorpedoes}");
        }

        Assert.Equal(100, lines.Count);
        string path = Path.Combine(ReportDirectory("US-0013-TK-0003"), "seed-matrix-1-100.txt");
        File.WriteAllLines(path, lines);
        Assert.True(File.Exists(path));
        Assert.Contains("seed=1 ", lines[0], StringComparison.Ordinal);
        Assert.Contains("seed=100 ", lines[^1], StringComparison.Ordinal);
        Assert.All(lines, line => Assert.Contains("outcome=", line, StringComparison.Ordinal));
        Assert.All(lines, line => Assert.Contains("consumed=", line, StringComparison.Ordinal));
    }
}
