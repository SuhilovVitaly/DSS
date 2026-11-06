using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0008TK0002Tests
{
    private static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition => Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);

    internal static SimulationEngine Battle(SimulationClock clock, SimulationSpeed initial = SimulationSpeed.Speed3,
        double distance = 40, double npcSpeed = 0, int remainder = 3, bool station = false)
    {
        var source = Registry(scatter: 0);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes), All(source.ItemTypes),
            All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents),
            combatRules: source.CombatRules! with
            { ShipDestruction = source.CombatRules.ShipDestruction with { DestroyedModuleFraction = .01m } });
        var scenario = TorpedoImpactTests.Scenario(o => o with
        {
            PositionX = o.ObjectId == Player ? 0 : distance * 10,
            PositionY = 0,
            SpeedMps = o.ObjectId == Player ? 0 : npcSpeed * 1000,
            DirectionDegrees = 270,
            MovementType = "Linear",
            Ai = o.ObjectId == Target ? new(25, 8, 35) : null,
            Modules = o.Modules!.Select(m => m with { StructurePoints = 100, Armor = 0, ArmorClass = "none" }).ToArray()
        });
        var objects = scenario.GameState.SpaceObjects;
        if (station) objects = objects.Append(new SpaceObjectData("station", SpaceObjectType.Station, "Permanent", "Station", 1, 0, 0, 270,
            "Stationary", null, null, null, IsKnown: true, Credits: 10000,
            StationCrew: [new("dock-operator", StationCrewRoles.DockOperator, "Operator")],
            PortFeeCreditsPerDay: 100, SecurityZoneRadiusKm: 200, PiracyWarningGracePeriodMs: 60000)).ToArray();
        var engine = new SimulationEngine(registry, clock: clock);
        engine.LoadScenario(scenario with
        {
            GameState = scenario.GameState with
            { CurrentSpeed = initial.ToString(), MotionQuarterRemainder = remainder, SpaceObjects = objects }
        });
        return engine;
    }
}
