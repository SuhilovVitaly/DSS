using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class StationResourceFieldContentTests
{
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
    private static readonly string SettingsPath = Path.Combine(ClientRoot, "Settings.json");
    private static readonly string[] MvpScenarios = ["Default", "Docked", "Undocked"];

    [Fact]
    public void Real_settings_load_resource_rules_and_registered_items()
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(SettingsPath, out _, out var settings);
        Assert.Equal("Data/World/station-resource-fields.json", settings.TypeData.StationResourceFields);
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(SettingsPath);
        var rules = Assert.IsType<StationResourceFieldsState>(Save(engine).StationResourceFields).Rules;
        Assert.Equal(1, rules.SchemaVersion);
        Assert.Equal(1000, rules.FirstObjectNumber);
        Assert.Equal(64, rules.MaxPlacementAttempts);
        Assert.Equal((2.5, 4.5, 2d, 0.1, 0.25),
            (rules.InnerRadiusKm, rules.OuterRadiusKm, rules.StationClearanceKm,
                rules.AsteroidSpacingKm, rules.CorridorHalfWidthKm));
        Assert.Equal(new ResourceSurveyRulesData(120, 60000, 85), rules.StructuralScan);
        Assert.Equal(5, rules.Roles.Count);
        foreach (var role in rules.Roles)
            Assert.True(registry.StationMarketProfiles.GetIndex(role.MarketProfileId) >= 0);

        var expectedVariants = new (string Kind, string Variant, string Composition, int Weight, string Fractions)[]
        {
            ("carbon", "carbon-mixed", "Silicate", 20, "item.carbon-ore:750,item.iron-ore:200,item.silicon:50"),
            ("carbon", "carbon-rich", "Silicate", 80, "item.carbon-ore:800,item.iron-ore:100,item.silicon:100"),
            ("ice", "ice-mixed", "Ice", 20, "item.carbon-ore:50,item.ice:850,item.iron-ore:100"),
            ("ice", "ice-rich", "Ice", 80, "item.carbon-ore:50,item.ice:950"),
            ("metal", "iron-rich", "Iron", 80, "item.carbon-ore:50,item.iron-ore:800,item.magnesium-ore:150"),
            ("metal", "magnesium-rich", "Iron", 20, "item.carbon-ore:50,item.iron-ore:650,item.magnesium-ore:300"),
            ("mixed", "mixed-common", "Silicate", 80, "item.carbon-ore:50,item.iron-ore:450,item.magnesium-ore:400,item.silicon:100"),
            ("mixed", "mixed-low-silicon", "Silicate", 20, "item.carbon-ore:50,item.iron-ore:500,item.magnesium-ore:400,item.silicon:50"),
        };
        Assert.Equal(expectedVariants, rules.FieldKinds.OrderBy(k => k.FieldKindId, StringComparer.Ordinal)
            .SelectMany(k => k.Variants.OrderBy(v => v.VariantId, StringComparer.Ordinal)
                .Select(v => (k.FieldKindId, v.VariantId, v.CompositionType, v.Weight,
                    string.Join(",", v.Resources.OrderBy(r => r.ItemTypeId, StringComparer.Ordinal)
                        .Select(r => $"{r.ItemTypeId}:{r.Permille}"))))));
        foreach (var kind in rules.FieldKinds)
        {
            Assert.Equal(100, kind.Variants.Sum(v => v.Weight));
            foreach (var variant in kind.Variants)
            {
                Assert.Equal(1000, variant.Resources.Sum(r => r.Permille));
                foreach (var resource in variant.Resources)
                {
                    var item = registry.ItemTypes.GetDefinition(registry.ItemTypes.GetIndex(resource.ItemTypeId));
                    Assert.Equal(TradeCategory.Resource, item.Category);
                    Assert.Equal(ItemStorageKind.Cargo, item.StorageKind);
                }
            }
        }
        Assert.Equal(
            new (string, long, long, int)[]
            {
                ("small", 1000000, 10000000, 60), ("medium", 10000001, 100000000, 30),
                ("large", 100000001, 500000000, 8), ("very-large", 500000001, 1000000000, 2),
            },
            rules.MassBands.OrderBy(b => b.MinKg).Select(b => (b.BandId, b.MinKg, b.MaxKg, b.Weight)));
    }

    [Fact]
    public void Profiles_produce_57_objects_with_carbon_at_two_stations()
    {
        using var fixture = new ContentFixture();
        using var engine = fixture.CreateEngine("Default", 123);
        var state = Save(engine);
        var fields = Assert.IsType<StationResourceFieldsState>(state.StationResourceFields);
        Assert.Equal(57, fields.Asteroids.Count);
        Assert.Equal(1057, fields.NextObjectNumber);
        var profiles = state.TradingMap!.Rules.Stations.ToDictionary(s => s.ObjectId, s => s.MarketProfileId);
        Assert.Equal(
            new[]
            {
                ("market.hydroponic", "ice", 12), ("market.industrial", "carbon", 3),
                ("market.industrial", "mixed", 9), ("market.mining", "carbon", 3),
                ("market.mining", "ice", 6), ("market.mining", "metal", 12),
                ("market.scientific-military", "mixed", 6), ("market.transit", "mixed", 6),
            },
            fields.Asteroids.GroupBy(a => (Profile: profiles[a.StationObjectId], a.FieldKindId))
                .OrderBy(g => g.Key.Profile, StringComparer.Ordinal).ThenBy(g => g.Key.FieldKindId, StringComparer.Ordinal)
                .Select(g => (g.Key.Profile, g.Key.FieldKindId, g.Count())));
        Assert.Equal(new[] { ("carbon", 6), ("ice", 18), ("metal", 12), ("mixed", 21) },
            fields.Asteroids.GroupBy(a => a.FieldKindId).OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => (g.Key, g.Count())));
        Assert.Equal(2, fields.Asteroids.Where(a => a.FieldKindId == "carbon")
            .Select(a => a.StationObjectId).Distinct().Count());
        Assert.Empty(fields.Surveys);
        Assert.All(fields.Asteroids, a => Assert.False(a.CompositionKnown));
        Assert.All(FieldObjects(state), obj =>
        {
            Assert.Equal("Asteroid", obj.ObjectType);
            Assert.Equal("Permanent", obj.PersistenceType);
            Assert.Equal("Stationary", obj.MovementType);
            Assert.True(obj.IsKnown);
            Assert.Equal(0, obj.SpeedMps);
            Assert.Equal(0, obj.DirectionDegrees);
            Assert.InRange(obj.MassKg!.Value, 1000000, 1000000000);
            Assert.Null(obj.Name);
            Assert.Empty(obj.Modules!);
        });
    }

    [Fact]
    public void Reordered_real_json_keeps_same_materialized_fields()
    {
        using var original = new ContentFixture();
        using var reordered = new ContentFixture(shuffle: true);
        foreach (ulong seed in new[] { 0UL, 123UL, ulong.MaxValue })
        {
            using var first = original.CreateEngine("Default", seed);
            using var second = reordered.CreateEngine("Default", seed, shuffle: true);
            var expected = Save(first);
            var actual = Save(second);
            Assert.Equal(Json(expected.StationResourceFields), Json(actual.StationResourceFields));
            Assert.Equal(Json(FieldObjects(expected)), Json(FieldObjects(actual)));
            Assert.Equal(Json(expected.TradingMap), Json(actual.TradingMap));
        }
    }

    [Fact]
    public void Every_trading_layout_fits_real_fields_for_seed_corpus()
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(SettingsPath, out _, out _);
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(SettingsPath);
        foreach (string name in MvpScenarios)
        {
            var source = ScenarioLoader.LoadFromFile(ScenarioPath(name));
            var request = Assert.IsType<TradingMapGenerationData>(source.GameState.TradingMapGeneration);
            Assert.NotEmpty(request.Templates);
            foreach (var layout in request.Templates)
            {
                foreach (ulong seed in Enumerable.Range(0, 256).Select(i => (ulong)i).Append(ulong.MaxValue))
                {
                    string context = $"scenario={name}, layout={layout.TemplateId}, seed={seed}";
                    var error = Record.Exception(() =>
                    {
                        // Restrict the request to each real layout so none depends on random selection coverage.
                        engine.LoadScenario(source with
                        {
                            GameState = source.GameState with
                            {
                                MasterSeed = seed,
                                TradingMapGeneration = request with { Templates = [layout] },
                            }
                        });
                        var state = Save(engine);
                        Assert.Equal(layout.TemplateId, state.TradingMap!.TemplateId);
                        Assert.Equal(57, state.StationResourceFields!.Asteroids.Count);
                        AssertGeometry(state);
                        Assert.Equal(Json(state.StationResourceFields),
                            Json(StationResourceFields.ValidateSaved(state, registry).StationResourceFields));
                    });
                    Assert.True(error is null, $"{context}: {error}");
                }
            }
        }
    }

    [Fact]
    public void Mvp_scenarios_share_resource_fields_for_same_master_seed()
    {
        using var fixture = new ContentFixture();
        using var disabled = new ContentFixture(enabled: false);
        foreach (ulong seed in new[] { 0UL, 123UL, ulong.MaxValue })
        {
            GameStateData? expected = null;
            foreach (string name in MvpScenarios)
            {
                using var engine = fixture.CreateEngine(name, seed);
                var state = Save(engine);
                Assert.NotNull(state.TradingMap);
                Assert.Equal(57, state.StationResourceFields!.Asteroids.Count);
                if (expected is not null)
                {
                    Assert.Equal(Json(expected.StationResourceFields), Json(state.StationResourceFields));
                    Assert.Equal(Json(FieldObjects(expected)), Json(FieldObjects(state)));
                }
                expected = state;

                using var baseline = disabled.CreateEngine(name, seed);
                var ids = state.StationResourceFields.Asteroids.Select(a => a.ObjectId).ToHashSet();
                // Full remaining state includes map RNG, markets, budgets, player Credits and cargo.
                Assert.Equal(Json(Save(baseline)), Json(state with
                {
                    SpaceObjects = state.SpaceObjects.Where(o => !ids.Contains(o.ObjectId)).ToArray(),
                    StationResourceFields = null,
                }));
            }
        }
    }

    [Fact]
    public void Default_500_and_legacy_without_map_remain_unchanged()
    {
        const string baselineHash = "8E31E0462D2B893A54A0778234C3044DC4230D0BC9F4D278ADB8EB32BBE6A526";
        Assert.Equal(baselineHash, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(ScenarioPath("Default_500")))));
        using var enabled = new ContentFixture();
        using var disabled = new ContentFixture(enabled: false);
        foreach (string name in new[] { "Default_500", "MarketProfiles" })
        {
            var source = ScenarioLoader.LoadFromFile(ScenarioPath(name));
            Assert.Null(source.GameState.TradingMapGeneration);
            Assert.Null(source.GameState.TradingMap);
            using var first = enabled.CreateEngine(name, 123);
            using var second = disabled.CreateEngine(name, 123);
            var state = Save(first);
            Assert.Null(state.StationResourceFields);
            Assert.Equal(source.GameState.SpaceObjects.Count, state.SpaceObjects.Count);
            if (name == "Default_500") Assert.Equal(502, state.SpaceObjects.Count);
            Assert.Equal(Json(Save(second)), Json(state));
        }
        Assert.Equal(baselineHash, Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(ScenarioPath("Default_500")))));
    }

    private static void AssertGeometry(GameStateData state)
    {
        var fields = state.StationResourceFields!;
        var rules = fields.Rules;
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId);
        foreach (var field in fields.Asteroids)
        {
            var obj = objects[field.ObjectId];
            Assert.InRange(DistanceKm(obj, objects[field.StationObjectId]), rules.InnerRadiusKm, rules.OuterRadiusKm);
            foreach (var station in state.SpaceObjects.Where(o => o.ObjectType == "Station"))
                Assert.True(DistanceKm(obj, station) >= Math.Max(rules.StationClearanceKm, state.TradingMap!.Rules.ClearanceKm),
                    $"{obj.ObjectId}: clearance from {station.ObjectId}");
            foreach (var other in state.SpaceObjects.Where(o => o.ObjectType == "Asteroid" && o.ObjectId != obj.ObjectId))
                Assert.True(DistanceKm(obj, other) >= rules.AsteroidSpacingKm, $"{obj.ObjectId}: spacing from {other.ObjectId}");
            foreach (var edge in state.TradingMap!.Edges)
            {
                var from = objects[edge.FromStationObjectId];
                var to = objects[edge.ToStationObjectId];
                double dx = to.PositionX - from.PositionX, dy = to.PositionY - from.PositionY;
                double ax = obj.PositionX - from.PositionX, ay = obj.PositionY - from.PositionY;
                double projection = ax * dx + ay * dy;
                double lengthSquared = dx * dx + dy * dy;
                // Independent oracle: endpoint distance or triangle area divided by its base.
                double distance = projection <= 0 ? DistanceKm(obj, from)
                    : projection >= lengthSquared ? DistanceKm(obj, to)
                    : Math.Abs(ax * dy - ay * dx) / Math.Sqrt(lengthSquared) / 10;
                Assert.True(distance >= rules.CorridorHalfWidthKm,
                    $"{obj.ObjectId}: corridor {from.ObjectId}/{to.ObjectId}");
            }
        }
    }

    private static double DistanceKm(SpaceObjectData a, SpaceObjectData b) =>
        Math.Sqrt(Math.Pow(a.PositionX - b.PositionX, 2) + Math.Pow(a.PositionY - b.PositionY, 2)) / 10;

    private static GameStateData Save(SimulationEngine engine) =>
        engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0).GameState;

    private static SpaceObjectData[] FieldObjects(GameStateData state)
    {
        var ids = state.StationResourceFields!.Asteroids.Select(a => a.ObjectId).ToHashSet();
        return state.SpaceObjects.Where(o => ids.Contains(o.ObjectId)).OrderBy(o => o.ObjectId, StringComparer.Ordinal).ToArray();
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
    private static string ScenarioPath(string name) => Path.Combine(ClientRoot, "Scenarios", name, "scenario.json");

    private static void ShuffleArrays(JsonNode? node, Random random)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj) ShuffleArrays(property.Value, random);
        }
        else if (node is JsonArray array)
        {
            foreach (var child in array) ShuffleArrays(child, random);
            var children = array.ToArray();
            array.Clear();
            random.Shuffle(children);
            foreach (var child in children) array.Add(child);
        }
    }

    private sealed class ContentFixture : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "DSS-resource-content-" + Guid.NewGuid().ToString("N"));
        private readonly string _settingsPath;

        public ContentFixture(bool enabled = true, bool shuffle = false)
        {
            Directory.CreateDirectory(_directory);
            _settingsPath = Path.Combine(_directory, "Settings.json");
            var settings = JsonNode.Parse(File.ReadAllText(SettingsPath))!;
            var paths = settings["typeData"]!.AsObject();
            foreach (var entry in paths.ToArray())
                paths[entry.Key] = Path.GetFullPath(Path.Combine(ClientRoot, entry.Value!.GetValue<string>()));
            settings["defaultScenario"] = ScenarioPath("Default");
            if (!enabled) paths.Remove("stationResourceFields");
            else if (shuffle)
            {
                var config = JsonNode.Parse(File.ReadAllText(paths["stationResourceFields"]!.GetValue<string>()))!;
                ShuffleArrays(config, new Random(173));
                string configPath = Path.Combine(_directory, "fields.json");
                File.WriteAllText(configPath, config.ToJsonString());
                paths["stationResourceFields"] = configPath;
            }
            File.WriteAllText(_settingsPath, settings.ToJsonString());
        }

        public SimulationEngine CreateEngine(string name, ulong seed, bool shuffle = false)
        {
            var scenario = JsonNode.Parse(File.ReadAllText(ScenarioPath(name)))!;
            scenario["gameState"]!["masterSeed"] = JsonValue.Create(seed);
            if (shuffle) ShuffleArrays(scenario, new Random(271));
            string path = Path.Combine(_directory, "scenario.json");
            File.WriteAllText(path, scenario.ToJsonString());
            return EngineContentLoader.CreateEngineFromScenarioFile(_settingsPath, path);
        }

        public void Dispose() => Directory.Delete(_directory, recursive: true);
    }
}
