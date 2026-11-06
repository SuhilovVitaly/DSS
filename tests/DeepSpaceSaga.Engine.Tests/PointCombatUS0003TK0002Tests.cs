using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Combat;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0003TK0002Tests
{
    internal const string Player = TorpedoImpactTests.Player, Target = TorpedoImpactTests.Target, Launcher = TorpedoImpactTests.Launcher;
    private static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition =>
        Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);

    internal static GameDataRegistry Registry(double scatter = 0.02)
    {
        var source = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(TorpedoImpactTests.ClientRoot, "Settings.json"), out _, out _);
        return GameDataRegistry.Create(
            All(source.ModuleCategories).Select(c => c.TypeId == "module.torpedo.launcher" ? c with { CommandTypeIds = [CombatCommandTypes.Launch, CombatCommandTypes.Detonate] } : c),
            All(source.ModuleTypes).Select(m => m.CategoryTypeId != "module.torpedo.launcher" ? m : m with
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
                ScatterPercent = scatter,
                Profiles = [new("slow", 6, 9), new("medium", 10, 5), new("fast", 16, 2.5)]
            }),
            All(source.ItemTypes), All(source.CommandDefinitions)
                .Append(new(CombatCommandTypes.Launch, "Launch", Target: "point", Type: "module.torpedo.launcher"))
                .Append(new(CombatCommandTypes.Detonate, "Detonate", Target: "object", Type: "module.torpedo.launcher"))
                .GroupBy(c => c.TypeId).Select(g => g.Last()),
            All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses),
            stationMarketEvents: All(source.StationMarketEvents), combatRules: source.CombatRules! with
            { Chance = null, Blast = new(0.3), Scatter = new(15, 0.0333, 0.3), Npc = new(1400, 30, 200), CombatStartRangeKm = 100 });
    }

    internal static SimulationEngine Create(Func<SpaceObjectData, SpaceObjectData>? configure = null, double scatter = 0.02, ulong seed = 42)
    {
        var scenario = TorpedoImpactTests.Scenario(o =>
        {
            var value = o with { Ai = null };
            return configure?.Invoke(value) ?? value;
        });
        var engine = new SimulationEngine(Registry(scatter));
        engine.LoadScenario(scenario with { GameState = scenario.GameState with { MasterSeed = seed } });
        return engine;
    }

    internal static PlayerCommand Launch(string id = "launch", double x = 700, double y = 0, string? profile = "slow") =>
        new(id, 1, Player, Launcher, CombatCommandTypes.Launch, TargetWorldX: x, TargetWorldY: y, ProfileId: profile);

    internal static ObjectMotionSnapshot Flight(AuthoritativeSnapshot snapshot) => Assert.Single(snapshot.Objects, o => o.OwnProjectile is not null);

    [Fact]
    public void Scatter_normal_pair_has_fixed_sequence_restorable_state_and_distribution()
    {
        var random = new LaunchScatterRng(0, 0);
        var first = random.NextPair();
        Assert.Equal(-0.45275774021745824, first.X, 14);
        Assert.Equal(0.20776603893419182, first.Y, 14);
        Assert.Equal(4354685564936845354UL, random.State);
        Assert.Equal(2UL, random.Counter);
        var restored = new LaunchScatterRng(random.State, random.Counter);
        Assert.Equal(random.NextPair(), restored.NextPair());
        double sumX = 0, sumY = 0, squaresX = 0, squaresY = 0, product = 0;
        for (int i = 0; i < 10000; i++)
        {
            var pair = random.NextPair();
            sumX += pair.X; sumY += pair.Y; squaresX += pair.X * pair.X; squaresY += pair.Y * pair.Y; product += pair.X * pair.Y;
        }
        Assert.InRange(sumX / 10000, -0.04, 0.04);
        Assert.InRange(sumY / 10000, -0.04, 0.04);
        Assert.InRange(squaresX / 10000, 0.94, 1.06);
        Assert.InRange(squaresY / 10000, 0.94, 1.06);
        Assert.InRange(product / 10000, -0.04, 0.04);
        Assert.Equal(20004UL, random.Counter);
    }
}
