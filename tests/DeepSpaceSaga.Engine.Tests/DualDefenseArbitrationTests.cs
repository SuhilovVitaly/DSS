using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
using static DeepSpaceSaga.Engine.Tests.ManualCountermeasureFireTests;

namespace DeepSpaceSaga.Engine.Tests;

public class DualDefenseArbitrationTests
{
    private static SimulationEngine CreateDual(decimal firstAccuracy = 55, decimal secondAccuracy = 55,
        double firstSpeed = 12, double secondSpeed = 12, bool reverse = false, string? unavailable = null,
        bool auto = true, bool extraThreat = false)
    {
        var source = AutomaticDefenseTests.WeaponRegistry(firstAccuracy, 5);
        static IEnumerable<T> All<T>(TypeRegistry<T> r) where T : ITypeDefinition => Enumerable.Range(0, r.Count).Select(r.GetDefinition);
        var variant = All(source.ModuleTypes).Single(m => m.CountermeasureAccuracy is not null) with
        {
            TypeId = "module.countermeasure.launcher.arbitration",
            CountermeasureAccuracy = secondAccuracy,
            CountermeasureSpeedKmS = secondSpeed,
            CountermeasureMaxFlightTimeMs = secondSpeed < 1 ? 1000 : 60000
        };
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Select(m =>
            m.CountermeasureAccuracy is null ? m : m with
            {
                CountermeasureSpeedKmS = firstSpeed,
                CountermeasureMaxFlightTimeMs = firstSpeed < 1 ? 1000 : 60000
            }).Append(variant),
            All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes),
            All(source.Dialogues), All(source.Quests), stationMarketProfiles: All(source.StationMarketProfiles),
            shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents));
        var scenario = HostileTorpedoFireTests.HostileScenario(800);
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o =>
            o.ObjectId != Player ? o with { DirectionDegrees = 180 } : o with
            {
                Modules = o.Modules!.Select(m => m with
                {
                    ModuleTypeId = m.ModuleId == Defense2 ? variant.TypeId : m.ModuleTypeId,
                    AutoDefenseEnabled = auto,
                    OperatorCrewId = unavailable == "operator" && m.ModuleId is Defense1 or Defense2 ? null : m.OperatorCrewId,
                    PowerState = unavailable == "off" && m.ModuleId is Defense1 or Defense2 ? "Off" : m.PowerState
                }).OrderBy(m =>
                        reverse ? -Array.FindIndex(o.Modules!.ToArray(), old => old.ModuleId == m.ModuleId) :
                        Array.FindIndex(o.Modules!.ToArray(), old => old.ModuleId == m.ModuleId)).ToArray()
            }).ToArray()
            }
        };
        if (extraThreat)
        {
            var sourceEnemy = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == Target);
            var extra = sourceEnemy with
            {
                ObjectId = "enemy-b",
                PositionY = -500,
                Crew = sourceEnemy.Crew!.Select(c => c with { CrewId = "extra-" + c.CrewId }).ToArray(),
                Modules = sourceEnemy.Modules!.Select(m => m with
                {
                    ModuleId = "extra-" + m.ModuleId,
                    OperatorCrewId = m.OperatorCrewId is null ? null : "extra-" + m.OperatorCrewId
                }).ToArray()
            };
            scenario = scenario with
            {
                GameState = scenario.GameState with
                { SpaceObjects = scenario.GameState.SpaceObjects.Append(extra).ToArray() }
            };
        }
        var engine = new SimulationEngine(registry);
        engine.LoadScenario(scenario);
        return engine;
    }

    [Fact]
    public void Earlier_encounter_wins_over_higher_chance()
    {
        using var engine = CreateDual(95, 15, 12, 24);
        var snapshot = At(engine, 1);
        var flight = Assert.Single(snapshot.Objects, o => o.Countermeasure is not null).Countermeasure!;
        Assert.Equal(Defense2, flight.LauncherModuleId);
        Assert.Equal(100, flight.FrozenChanceTenths);
        Assert.Equal(24, flight.SpeedKmS);
        Assert.True(Assert.Single(snapshot.Objects, o => o.Torpedo is not null).CountermeasureAttempted);
        Assert.Single(At(engine, 100).Objects, o => o.Countermeasure is not null);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Equal_encounter_prefers_chance_then_ordinal_module_id(bool reverse)
    {
        using var higher = CreateDual(55, 75, reverse: reverse);
        Assert.Equal(Defense2, Assert.Single(At(higher, 1).Objects, o => o.Countermeasure is not null).Countermeasure!.LauncherModuleId);
        using var equal = CreateDual(reverse: reverse);
        Assert.Equal(Defense1, Assert.Single(At(equal, 1).Objects, o => o.Countermeasure is not null).Countermeasure!.LauncherModuleId);
    }

    [Fact]
    public void No_solution_candidate_does_not_hide_valid_second_module()
    {
        using var engine = CreateDual(firstSpeed: 0.001);
        Assert.Equal(Defense2, Assert.Single(At(engine, 1).Objects, o => o.Countermeasure is not null).Countermeasure!.LauncherModuleId);
    }

    [Theory]
    [InlineData("zero")]
    [InlineData("operator")]
    [InlineData("off")]
    [InlineData("solution")]
    public void Rejected_candidates_do_not_reserve_attempt_or_draw(string reason)
    {
        using var engine = CreateDual(reason == "zero" ? 0 : 55, reason == "zero" ? 0 : 55,
            reason == "solution" ? 0.001 : 12, reason == "solution" ? 0.001 : 12, unavailable: reason);
        var snapshot = At(engine, 100);
        Assert.DoesNotContain(snapshot.Objects, o => o.Countermeasure is not null);
        Assert.False(Assert.Single(snapshot.Objects, o => o.Torpedo is not null).CountermeasureAttempted);
        Assert.Equal(0ul, engine.CaptureSaveStateForTests(100, SimulationSpeed.Speed0).GameState.DefenseState!.RngCounter);
    }

    [Fact]
    public void Manual_first_consumes_shared_auto_attempt_but_allows_additional_manual()
    {
        using var engine = CreateDual(auto: false);
        string target = LaunchEnemy(engine);
        engine.ReceiveCommand(new("auto1", 2, Player, Defense1, DefenseCommandTypes.Enable));
        engine.ReceiveCommand(new("auto2", 3, Player, Defense2, DefenseCommandTypes.Enable));
        engine.ReceiveCommand(Manual("manual-first", target));
        var first = At(engine, 1);
        Assert.Equal(LaunchMode.Manual, Assert.Single(first.Objects, o => o.Countermeasure is not null).Countermeasure!.Mode);
        Assert.Single(At(engine, 2).Objects, o => o.Countermeasure is not null);
        engine.ReceiveCommand(Manual("manual-second", target, Defense2));
        var second = At(engine, 2);
        Assert.Equal(2, second.Objects.Count(o => o.Countermeasure is not null));
        Assert.All(second.Objects.Where(o => o.Countermeasure is not null), o => Assert.Equal(LaunchMode.Manual, o.Countermeasure!.Mode));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Simultaneous_manual_contacts_draw_once_per_resolved_projectile(decimal accuracy)
    {
        using var engine = CreateDual(accuracy, accuracy, auto: false);
        string target = LaunchEnemy(engine);
        engine.ReceiveCommand(Manual("one", target));
        engine.ReceiveCommand(Manual("two", target, Defense2));
        At(engine, 1);
        var snapshot = At(engine, 8000);
        int expected = accuracy == 0 ? 2 : 1; // 95% beats seeded801, so the other projectile loses its target.
        Assert.Equal(expected, snapshot.CombatJournal.Count(e => e.Roll is not null));
        Assert.Equal((ulong)expected, engine.CaptureSaveStateForTests(8000, SimulationSpeed.Speed0).GameState.DefenseState!.RngCounter);
    }

    [Fact]
    public void Earliest_hull_threat_retains_priority_before_module_arbitration()
    {
        using var engine = CreateDual(auto: false, extraThreat: true);
        var before = At(engine, 1);
        string nearest = before.Objects.Single(o => o.Torpedo?.OwnerObjectId == "enemy-b").ObjectId;
        engine.ReceiveCommand(new("enable1", 2, Player, Defense1, DefenseCommandTypes.Enable));
        engine.ReceiveCommand(new("enable2", 3, Player, Defense2, DefenseCommandTypes.Enable));
        At(engine, 1);
        var after = At(engine, 2);
        var launches = after.CombatJournal.Where(e => e.Type == CombatEventType.Launch && e.LaunchMode == LaunchMode.Auto).ToArray();
        Assert.Equal(2, launches.Length);
        Assert.Equal(nearest, launches[0].TargetObjectId);
        Assert.Equal(2, after.Objects.Count(o => o.Countermeasure is not null));
    }
}
