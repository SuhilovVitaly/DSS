using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0008TK0001Tests
{
    internal const string NpcLauncher = "MOD-PIRATE-TORPEDO-01";
    internal static AuthoritativeSnapshot At(SimulationEngine engine, long time = 0) => engine.CaptureSnapshotForTests(time * 4, simulationTimeMs: time);
    private static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition => Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);
    internal static SimulationEngine Battle(double distanceKm = 40, bool ai = true, bool enemyLauncherOn = false,
        double enemySpeed = 0, bool extraEnemy = false, bool extraHasAi = true)
    {
        var source = Registry(scatter: 0);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes), All(source.ItemTypes),
            All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents),
            combatRules: source.CombatRules! with
            { ShipDestruction = source.CombatRules.ShipDestruction with { DestroyedModuleFraction = .01m } });
        var scenario = TorpedoImpactTests.Scenario(o => o with
        {
            Ai = o.ObjectId == Target && ai ? new(25, 8, 35) : null,
            PositionX = o.ObjectId == Player ? 0 : distanceKm * 10,
            PositionY = 0,
            SpeedMps = o.ObjectId == Player ? 0 : enemySpeed * 1000,
            DirectionDegrees = 270,
            MovementType = "Linear",
            Modules = o.Modules!.Select(m => m with
            {
                StructurePoints = 100,
                Armor = 0,
                ArmorClass = "none",
                PowerState = o.ObjectId == Target && m.ModuleId == NpcLauncher && enemyLauncherOn ? "On" : m.PowerState
            }).ToArray()
        });
        if (extraEnemy)
        {
            var npc = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == Target);
            var extra = npc with
            {
                ObjectId = "other-enemy",
                PositionX = 2000,
                PositionY = 0,
                Ai = extraHasAi ? new(25, 8, 35) : null,
                Crew = npc.Crew!.Select(c => c with { CrewId = "other-" + c.CrewId }).ToArray(),
                Modules = npc.Modules!.Select(m => m with
                {
                    ModuleId = "other-" + m.ModuleId,
                    OperatorCrewId = m.OperatorCrewId is null ? null : "other-" + m.OperatorCrewId,
                    PowerState = m.ModuleId == NpcLauncher ? "On" : m.PowerState
                }).ToArray()
            };
            scenario = scenario with { GameState = scenario.GameState with { SpaceObjects = scenario.GameState.SpaceObjects.Append(extra).ToArray() } };
        }
        var engine = new SimulationEngine(registry);
        engine.LoadScenario(scenario);
        return engine;
    }
}
