using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0004TK0001Tests
{
    internal const string Defense = "MOD-PLAYER-COUNTERMEASURE-01", Defense2 = "MOD-PLAYER-COUNTERMEASURE-02";
    private static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition =>
        Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);

    internal static GameDataRegistry CounterRegistry(double scatter = 0.08, double torpedoScatter = 0.02)
    {
        var source = Registry(torpedoScatter);
        return GameDataRegistry.Create(All(source.ModuleCategories).Select(c => c.TypeId == "module.countermeasure.launcher"
                ? c with { CommandTypeIds = [DefenseCommandTypes.Launch] } : c),
            All(source.ModuleTypes).Select(m => m.CategoryTypeId != "module.countermeasure.launcher" ? m : m with
            {
                CommandTypeIds = [DefenseCommandTypes.Launch],
                Profiles = [new("wide", 15, 1), new("fast", 25, 0.5)],
                RangeKm = 30,
                ReloadMs = 12500,
                ScatterPercent = scatter,
                CountermeasureAccuracy = null,
                CountermeasureRangeKm = null,
                CountermeasureManualRangeKm = null,
                CountermeasureSpeedKmS = null,
                CountermeasureTurnRateDegPerSec = null,
                CountermeasureReloadMs = null,
                CountermeasureMaxFlightTimeMs = null,
                Barrels = null,
                BurstIntervalMs = null
            }), All(source.ItemTypes), All(source.CommandDefinitions)
                .Append(new(DefenseCommandTypes.Launch, "Launch countermissile", Target: "point", Type: "module.countermeasure.launcher"))
                .GroupBy(c => c.TypeId).Select(g => g.Last()),
            All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses),
            stationMarketEvents: All(source.StationMarketEvents), combatRules: source.CombatRules);
    }

    internal static SimulationEngine CreateCounter(Func<SpaceObjectData, SpaceObjectData>? configure = null,
        double scatter = 0.08, double torpedoScatter = 0.02)
    {
        var scenario = TorpedoImpactTests.Scenario(o =>
        {
            var value = o with { Ai = null, Crew = o.Crew?.Select(c => c with { AccuracySkill = 15 }).ToArray() };
            return configure?.Invoke(value) ?? value;
        });
        var engine = new SimulationEngine(CounterRegistry(scatter, torpedoScatter));
        engine.LoadScenario(scenario);
        return engine;
    }

    internal static PlayerCommand CounterLaunch(string id = "counter", double x = 300, string? profile = "wide", string module = Defense) =>
        new(id, 1, Player, module, DefenseCommandTypes.Launch, TargetWorldX: x, TargetWorldY: 0, ProfileId: profile);
    internal static AuthoritativeSnapshot At(SimulationEngine engine, long time = 0) => engine.CaptureSnapshotForTests(time * 4, simulationTimeMs: time);
}
