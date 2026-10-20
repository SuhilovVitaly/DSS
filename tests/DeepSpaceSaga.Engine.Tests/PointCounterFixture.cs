using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>Actual point catalog; zero-scatter calibration isolates geometry, never balance acceptance.</summary>
internal static class PointCounterFixture
{
    internal const string Player = "SPC-0001", Enemy = "SPC-0002", Torpedo = "MOD-PLAYER-TORPEDO-01",
        EnemyTorpedo = "MOD-PIRATE-TORPEDO-01", Counter = "MOD-PLAYER-COUNTERMEASURE-01", Counter2 = "MOD-PLAYER-COUNTERMEASURE-02";
    internal static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));
    internal static GameDataRegistry Registry(double? scatter = 0, double? torpedoScatter = 0)
    {
        var source = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(Root, "Settings.json"), out _, out _);
        static IEnumerable<T> All<T>(TypeRegistry<T> r) where T : ITypeDefinition => Enumerable.Range(0, r.Count).Select(r.GetDefinition);
        return GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Select(m => m.CategoryTypeId switch
        {
            "module.countermeasure.launcher" when scatter is not null => m with { ScatterPercent = scatter },
            "module.torpedo.launcher" when torpedoScatter is not null => m with { ScatterPercent = torpedoScatter },
            _ => m
        }), All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents), combatRules: source.CombatRules);
    }
    internal static SimulationEngine Create(bool enemyArmed = false, Func<SpaceObjectData, SpaceObjectData>? configure = null,
        double? scatter = 0, double? torpedoScatter = 0, double enemyX = 200)
    {
        var source = ScenarioLoader.LoadFromFile(Path.Combine(Root, "Scenarios/PlayerShipOnly/scenario.json"));
        var registry = Registry(scatter, torpedoScatter);
        var scenario = source with
        {
            GameState = source.GameState with
            {
                MasterSeed = 42,
                CurrentSpeed = "Speed0",
                SpaceObjects = source.GameState.SpaceObjects.Select(o =>
                {
                    var value = o with
                    {
                        PositionX = o.ObjectId == Player ? 0 : enemyX,
                        PositionY = 0,
                        SpeedMps = 0,
                        DirectionDegrees = o.ObjectId == Player ? 90 : 270,
                        MovementType = "Stationary",
                        Ai = null,
                        Modules = o.Modules!.Select(m => m with
                        {
                            PowerState = m.ModuleTypeId.StartsWith("module.engine.", StringComparison.Ordinal) ||
                            o.ObjectId == Enemy && m.ModuleId == EnemyTorpedo && !enemyArmed ? "Off" : "On"
                        }).ToArray()
                    };
                    return configure?.Invoke(value) ?? value;
                }).ToArray()
            }
        };
        var engine = new SimulationEngine(registry, clock: new SimulationClock(SimulationSpeed.Speed0, () => 0)); engine.LoadScenario(scenario); return engine;
    }
    internal static PlayerCommand Shot(string id = "counter", string module = Counter, double x = 150, double y = 30, string? profile = "wide") =>
        new(id, 1, Player, module, DefenseCommandTypes.Launch, TargetWorldX: x, TargetWorldY: y, ProfileId: profile);
    internal static AuthoritativeSnapshot At(SimulationEngine engine, long time = 0) => engine.CaptureSnapshotForTests(time * 300, SimulationSpeed.Speed0, time);
    internal static ScenarioFile Save(SimulationEngine engine, long time = 0) => engine.CaptureSaveStateForTests(time * 300, SimulationSpeed.Speed0, time);
    internal static OwnProjectileSnapshot Flight(AuthoritativeSnapshot s, string module = Counter) =>
        Assert.Single(s.Objects, o => o.OwnProjectile?.LauncherModuleId == module).OwnProjectile!;
}
