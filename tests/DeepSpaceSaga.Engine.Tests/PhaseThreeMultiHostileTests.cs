using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
using static DeepSpaceSaga.Engine.Tests.ManualCountermeasureFireTests;

namespace DeepSpaceSaga.Engine.Tests;

public class PhaseThreeMultiHostileTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Multiple_hostiles_share_no_player_authority(int accuracy)
    {
        using var whole = CreateBattle(accuracy);
        using var split = CreateBattle(accuracy);
        foreach (var engine in new[] { whole, split })
        {
            // Manual-defense fixture establishes both threats independently of automatic AI reaction.
            engine.TryStartHostileTorpedoFire(Target, "MOD-PIRATE-TORPEDO-01", 1);
            engine.TryStartHostileTorpedoFire("enemy-b", "extra-MOD-PIRATE-TORPEDO-01", 1);
            var snapshot = At(engine, 1);
            var torpedoes = snapshot.Objects.Where(o => o.Torpedo is not null).OrderBy(o => o.Torpedo!.OwnerObjectId, StringComparer.Ordinal).ToArray();
            Assert.Equal(2, torpedoes.Length);
            Assert.Equal(new[] { Target, "enemy-b" }, torpedoes.Select(o => o.Torpedo!.OwnerObjectId));
            Assert.All(torpedoes, o => Assert.Equal(1, o.Torpedo!.LaunchMotionTimeMs));
            Assert.Empty(snapshot.CommandResults);
            Assert.Empty(engine.CaptureSaveStateForTests(1, SimulationSpeed.Speed0).GameState.CombatState!.ProcessedLaunchCommandIds);
            engine.ReceiveCommand(Manual("first", torpedoes[0].ObjectId, Defense1));
            engine.ReceiveCommand(Manual("second", torpedoes[1].ObjectId, Defense2));
            snapshot = At(engine, 1);
            Assert.Equal(2, snapshot.Objects.Count(o => o.Countermeasure is not null));
            Assert.All(snapshot.CommandResults, r => { Assert.Equal(Player, r.ObjectId); Assert.Equal(CommandResultStatus.Executed, r.Status); });
            Assert.All(snapshot.Objects.Where(o => o.Countermeasure is not null), o =>
            {
                var flight = o.Countermeasure!;
                Assert.Equal(accuracy * 10, flight.FrozenChanceTenths);
                Assert.Equal(LaunchMode.Manual, flight.Mode);
                Assert.Equal(9, flight.SpeedKmS);
                Assert.Equal(45, flight.TurnRateDegPerSec);
                Assert.Equal(8000, flight.MaxFlightTimeMs);
                Assert.Equal(8001, flight.ExpiresAtMotionTimeMs);
            });
        }
        var expected = At(whole, 15000);
        foreach (long time in new long[] { 2, 37, 100, 501, 1000, 4001, 7000, 8001, 12000, 15000 }) At(split, time);
        var actual = At(split, 15000);
        Assert.Equal(JsonSerializer.Serialize(expected.CombatJournal), JsonSerializer.Serialize(actual.CombatJournal));
        Assert.Equal(JsonSerializer.Serialize(expected.Objects), JsonSerializer.Serialize(actual.Objects));
        var resolutions = expected.CombatJournal.Where(e => e.Roll is not null).ToArray();
        Assert.Equal(2, resolutions.Length);
        Assert.All(resolutions, e => Assert.Equal(accuracy == 0 ? CombatEventType.Miss : CombatEventType.Intercept, e.Type));
        Assert.Equal(2ul, whole.CaptureSaveStateForTests(15000, SimulationSpeed.Speed0).GameState.DefenseState!.RngCounter);
    }

    private static SimulationEngine CreateBattle(int accuracy)
    {
        var source = AutomaticDefenseTests.WeaponRegistry(accuracy, 0);
        static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition => Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Select(m => m.CountermeasureAccuracy is null ? m : m with
        {
            CountermeasureSpeedKmS = 9,
            CountermeasureTurnRateDegPerSec = 45,
            CountermeasureManualRangeKm = 111,
            CountermeasureMaxFlightTimeMs = 8000,
            CountermeasureReloadMs = 7000
        }), All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents),
            combatRules: source.CombatRules, fireModes: source.FireModes);
        var scenario = HostileTorpedoFireTests.HostileScenario(600);
        var enemy = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == Target);
        var extra = enemy with
        {
            ObjectId = "enemy-b",
            PositionX = 100,
            PositionY = -600,
            DirectionDegrees = 180,
            Crew = enemy.Crew!.Select(c => c with { CrewId = "extra-" + c.CrewId }).ToArray(),
            Modules = enemy.Modules!.Select(m => m with { ModuleId = "extra-" + m.ModuleId, OperatorCrewId = m.OperatorCrewId is null ? null : "extra-" + m.OperatorCrewId }).ToArray()
        };
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o.ObjectId == Target ? o with { DirectionDegrees = 180 } : o).Append(extra).ToArray()
            }
        };
        var engine = new SimulationEngine(registry);
        engine.LoadScenario(scenario);
        return engine;
    }
}
