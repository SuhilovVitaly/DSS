using System.Collections.Immutable;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

string root = Path.GetFullPath(args[0]);
string client = Path.Combine(root, "src/DeepSpaceSaga.Client");
string settings = Path.Combine(client, "Settings.json");
var registry = EngineContentLoader.LoadRegistryFromSettingsFile(settings, out _, out _);
var config = EngineContentLoader.LoadSolarSystemGenerationConfig(settings)!;
var source = ScenarioLoader.LoadFromFile(Path.Combine(client, "Scenarios/Default/scenario.json"));
using var engine = new SimulationEngine(registry);
engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = 42, CurrentSpeed = "Speed0" } }, generation: config);
var save = engine.CaptureSaveState();
var cluster = save.GameState.ClusterMap!.Clusters.Last();
var a = save.GameState.SpaceObjects.Single(o => o.ObjectId == cluster.StationIds[0]);
var b = save.GameState.SpaceObjects.Single(o => o.ObjectId == cluster.StationIds[1]);
var altered = save with { GameState = save.GameState with {
    SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId == b.ObjectId ? o with {
        Orbit = a.Orbit, PositionX = a.PositionX, PositionY = a.PositionY } : o).ToArray(),
    SolarSystem = save.GameState.SolarSystem! with { Orbits = save.GameState.SolarSystem.Orbits.Select(o =>
        o.ObjectId == b.ObjectId ? o with { Elements = a.Orbit! } : o).ToImmutableArray() }
} };
using var loaded = new SimulationEngine(registry);
try {
    loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(altered), true), true);
    var snapshot = loaded.CaptureSnapshot();
    var pa = snapshot.Objects.Single(o => o.ObjectId == a.ObjectId);
    var pb = snapshot.Objects.Single(o => o.ObjectId == b.ObjectId);
    Console.WriteLine($"COLLAPSED_CLUSTER: ACCEPTED; {a.ObjectId}/{b.ObjectId}; separation={double.Hypot(pa.X-pb.X, pa.Y-pb.Y):R}");
} catch (Exception e) { Console.WriteLine($"COLLAPSED_CLUSTER: REJECTED; {e.GetType().Name}: {e.Message}"); }
foreach (var variant in new[] {
    config with { AsteroidsPerBelt = int.MaxValue },
    config with { MaxPlacementAttempts = int.MaxValue }
}) {
    try { SolarSystemGeneration.ValidateConfig(variant); Console.WriteLine($"UNBOUNDED_CONFIG: ACCEPTED asteroids={variant.AsteroidsPerBelt}, attempts={variant.MaxPlacementAttempts}; generation deliberately not executed"); }
    catch (Exception e) { Console.WriteLine($"UNBOUNDED_CONFIG: REJECTED {e.Message}"); }
}

static IEnumerable<T> All<T>(TypeRegistry<T> types) where T : ITypeDefinition => Enumerable.Range(0, types.Count).Select(types.GetDefinition);
var customRegistry = GameDataRegistry.Create(All(registry.ModuleCategories), All(registry.ModuleTypes).Select(m =>
    m.TypeId == "module.engine.basic" ? m with { TypeId = "module.engine.review", MaxSpeedMps = 1400 } : m),
    All(registry.ItemTypes), All(registry.CommandDefinitions), All(registry.FactoryTypes), All(registry.Recipes),
    All(registry.Dialogues), All(registry.Quests), registry.CatalogVersion, registry.LegacyCatalogFingerprint,
    All(registry.StationMarketProfiles), All(registry.ShipClasses), All(registry.StationMarketEvents));
var customSource = source with { GameState = source.GameState with { MasterSeed = 42, CurrentSpeed = "Speed0",
    SpaceObjects = source.GameState.SpaceObjects.Select(o => o with { Modules = o.Modules?.Select(m =>
        m.ModuleTypeId == "module.engine.basic" ? m with { ModuleTypeId = "module.engine.review" } : m).ToArray() }).ToArray()
} };
try {
    using var custom = new SimulationEngine(customRegistry);
    custom.LoadScenario(customSource, generation: config);
    var s = custom.CaptureSnapshot();
    var player = s.Objects.Single(o => o.ObjectId == customSource.GameState.PlayerShipObjectId);
    Console.WriteLine($"CUSTOM_ENGINE: generated; configuredMaxSpeedKmS=1.4; snapshotMaxSpeedKmS={player.MaxSpeedKmS?.ToString() ?? "null"}");
} catch (Exception e) { Console.WriteLine($"CUSTOM_ENGINE: REJECTED {e.GetType().Name}: {e.Message}"); }
