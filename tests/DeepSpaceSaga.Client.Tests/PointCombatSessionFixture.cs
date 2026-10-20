using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>Real point executor and in-process bridge; only catalog configuration and wall clock are controlled.</summary>
internal sealed class PointCombatSessionFixture : IAsyncDisposable
{
    internal const string Player = "SPC-0001", Enemy = "SPC-0002", Torpedo = "MOD-PLAYER-TORPEDO-01";
    private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
    internal SimulationEngine Engine { get; }
    internal LocalGameSessionConnection Connection { get; }
    private long _realMs;

    private static IEnumerable<T> All<T>(TypeRegistry<T> source) where T : ITypeDefinition => Enumerable.Range(0, source.Count).Select(source.GetDefinition);
    internal static GameDataRegistry Registry(double scatterPercent = 0, CombatRulesDefinition? rules = null)
    {
        var source = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(Root, "Settings.json"), out _, out _);
        var commands = new[]
        {
            new CommandDefinition(CombatCommandTypes.Launch, "Launch", Target: "point", Type: "module.torpedo.launcher"),
            new CommandDefinition(CombatCommandTypes.Detonate, "Detonate", Target: "object", Type: "module.torpedo.launcher"),
            new CommandDefinition(DefenseCommandTypes.Launch, "Launch counter", Target: "point", Type: "module.countermeasure.launcher")
        };
        return GameDataRegistry.Create(All(source.ModuleCategories).Select(c => c.TypeId switch
            {
                "module.torpedo.launcher" => c with { CommandTypeIds = [CombatCommandTypes.Launch, CombatCommandTypes.Detonate] },
                "module.countermeasure.launcher" => c with { CommandTypeIds = [DefenseCommandTypes.Launch] },
                _ => c
            }),
            All(source.ModuleTypes).Select(m => m.CategoryTypeId switch
            {
                "module.torpedo.launcher" => m with
                {
                    CommandTypeIds = [CombatCommandTypes.Launch, CombatCommandTypes.Detonate],
                    TorpedoManeuverability = null,
                    TorpedoRangeKm = null,
                    TorpedoSpeedKmS = null,
                    TorpedoTurnRateDegPerSec = null,
                    Accuracy = null,
                    LauncherReloadMs = null,
                    TorpedoMaxFlightTimeMs = null,
                    TorpedoDamage = 200,
                    ArmorPiercingPercent = 20,
                    ArmorDamagePercent = 100,
                    RangeKm = 70,
                    ReloadMs = 10000,
                    ScatterPercent = scatterPercent,
                    Profiles = [new("slow", 6, 9), new("medium", 10, 5), new("fast", 16, 2.5)]
                },
                "module.countermeasure.launcher" => m with
                {
                    CommandTypeIds = [DefenseCommandTypes.Launch],
                    RangeKm = 30,
                    ReloadMs = 12500,
                    ScatterPercent = 0,
                    Profiles = [new("wide", 15, 1), new("fast", 25, .5)],
                    CountermeasureAccuracy = null,
                    CountermeasureRangeKm = null,
                    CountermeasureManualRangeKm = null,
                    CountermeasureSpeedKmS = null,
                    CountermeasureTurnRateDegPerSec = null,
                    CountermeasureReloadMs = null,
                    CountermeasureMaxFlightTimeMs = null,
                    Barrels = null,
                    BurstIntervalMs = null
                },
                _ => m
            }), All(source.ItemTypes), All(source.CommandDefinitions).Concat(commands).GroupBy(c => c.TypeId).Select(g => g.Last()),
            All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents),
            combatRules: rules ?? source.CombatRules! with { Chance = null, Blast = new(.3), Scatter = new(15, .0333, .3), Npc = new(1400, 30, 200), CombatStartRangeKm = 100 });
    }

    internal PointCombatSessionFixture(GameDataRegistry? registry = null)
    {
        var source = ScenarioLoader.LoadFromFile(Path.Combine(Root, "Scenarios", "PlayerShipOnly", "scenario.json"));
        Engine = new(registry ?? Registry(), clock: new SimulationClock(SimulationSpeed.Speed0, () => Interlocked.Read(ref _realMs)));
        Engine.LoadScenario(source with
        {
            GameState = source.GameState with
            {
                MasterSeed = 42,
                CurrentSpeed = "Speed0",
                SpaceObjects = source.GameState.SpaceObjects.Select(o => o with
                {
                    PositionX = o.ObjectId == Player ? 0 : 400,
                    PositionY = 0,
                    SpeedMps = 0,
                    MovementType = "Stationary",
                    Ai = null,
                    IsKnown = true,
                    Modules = o.Modules!.Select(m => m with
                    {
                        AutoDefenseEnabled = false,
                        PowerState = m.ModuleTypeId == "module.engine.basic" ? "Off" : "On"
                    }).ToArray()
                }).ToArray()
            }
        });
        Engine.SetPresentationTimestampForTests(() => 0);
        Assert.Equal(CommandStartDisposition.Started, Engine.TryStartHostilePointLaunch(Enemy, "MOD-PIRATE-TORPEDO-01", 700, 0, "slow", 0).Disposition);
        Connection = new(Engine);
    }

    internal void SetReal(long milliseconds) => Interlocked.Exchange(ref _realMs, milliseconds);
    internal static PlayerCommand Launch(string id = "point-launch") => new(id, 1, Player, Torpedo, CombatCommandTypes.Launch,
        TargetWorldX: 600, TargetWorldY: 0, ProfileId: "fast");
    public async ValueTask DisposeAsync() => await Connection.DisposeAsync();
}
