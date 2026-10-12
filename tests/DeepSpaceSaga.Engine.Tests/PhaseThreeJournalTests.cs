using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
using static DeepSpaceSaga.Engine.Tests.ManualCountermeasureFireTests;

namespace DeepSpaceSaga.Engine.Tests;

public class PhaseThreeJournalTests
{
    private static GameDataRegistry JournalRegistry(decimal accuracy, long lifetime)
    {
        var source = AutomaticDefenseTests.WeaponRegistry(accuracy, 0);
        static IEnumerable<T> All<T>(TypeRegistry<T> r) where T : ITypeDefinition => Enumerable.Range(0, r.Count).Select(r.GetDefinition);
        return GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Select(m =>
            m.CountermeasureAccuracy is null ? m : m with { CountermeasureMaxFlightTimeMs = lifetime }),
            All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes),
            All(source.Dialogues), All(source.Quests), stationMarketProfiles: All(source.StationMarketProfiles),
            shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents),
            combatRules: source.CombatRules, fireModes: source.FireModes);
    }

    private static SimulationEngine Start(decimal accuracy = 0, long lifetime = 60000, double distance = 155, bool two = false, bool fragile = false)
    {
        var engine = new SimulationEngine(JournalRegistry(accuracy, lifetime));
        var scenario = HostileTorpedoFireTests.HostileScenario(distance);
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o =>
                o.ObjectId == Target ? o with { DirectionDegrees = 180 } :
                fragile && o.ObjectId == Player ? FragileForOneHit(o) : o).ToArray()
            }
        };
        engine.LoadScenario(scenario);
        string target = LaunchEnemy(engine);
        engine.ReceiveCommand(Manual("one", target));
        if (two) engine.ReceiveCommand(Manual("two", target, Defense2));
        At(engine, 1);
        return engine;
    }

    [Fact]
    public void Zero_percent_contact_records_one_roll_miss_and_captured_module_facts()
    {
        using var engine = Start();
        var launch = Assert.Single(At(engine, 1).CombatJournal, e => e.LaunchMode == LaunchMode.Manual);
        Assert.Equal(Defense1, launch.LauncherModuleId);
        Assert.Equal(0m, launch.Accuracy);
        Assert.Equal(0m, launch.TargetManeuverability);
        Assert.Equal(0, launch.ChanceTenths);
        Assert.Null(launch.Roll);
        var snapshot = At(engine, 1100);
        var miss = Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.Miss);
        Assert.Equal(801, miss.Roll);
        Assert.Equal(0, miss.ChanceTenths);
        Assert.Equal(launch.LauncherModuleId, miss.LauncherModuleId);
        Assert.Equal(launch.Accuracy, miss.Accuracy);
        Assert.Equal(launch.TargetManeuverability, miss.TargetManeuverability);
        Assert.Equal(launch.LaunchMode, miss.LaunchMode);
        Assert.Null(miss.TerminationKind);
        Assert.Equal(snapshot.CombatJournal.ToArray(), At(engine, 1100).CombatJournal.ToArray());
        var coast = Assert.Single(At(engine, 3100).CombatJournal, e => e.Type == CombatEventType.Expired);
        Assert.Equal("missed_coast_expired", coast.Result);
        Assert.Equal(CountermeasureTerminationKind.MissedCoastExpired, coast.TerminationKind);
        Assert.Null(coast.Roll);
    }

    [Fact]
    public void Expiry_is_not_miss_or_damage_and_journal_roundtrips_without_duplication()
    {
        using var engine = Start(lifetime: 1000, distance: 1000);
        var snapshot = At(engine, 1001);
        var expiry = Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.Expired);
        Assert.Equal(1001, expiry.MotionTimeMs);
        Assert.Equal("lifetime_expired", expiry.Result);
        Assert.Equal(CountermeasureTerminationKind.LifetimeExpired, expiry.TerminationKind);
        Assert.Equal(Defense1, expiry.LauncherModuleId);
        Assert.Equal(LaunchMode.Manual, expiry.LaunchMode);
        Assert.Null(expiry.Roll);
        Assert.Equal(0, expiry.Damage);
        Assert.DoesNotContain(snapshot.CombatJournal, e => e.Type is CombatEventType.Miss or CombatEventType.Hit or CombatEventType.ShipDestroyed);
        var save = engine.CaptureSaveStateForTests(1001, SimulationSpeed.Speed0);
        using var resumed = new SimulationEngine(JournalRegistry(0, 1000));
        resumed.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), isSave: true);
        Assert.Equal(JsonSerializer.Serialize(snapshot.CombatJournal), JsonSerializer.Serialize(At(resumed, 1001).CombatJournal));
        Assert.Single(At(resumed, 11001).CombatJournal, e => e.Type == CombatEventType.Expired);
    }

    [Fact]
    public void Concurrent_intercepts_record_single_target_termination_and_target_loss_without_roll()
    {
        using var engine = Start(100, two: true);
        var snapshot = At(engine, 1100);
        var intercept = Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.Intercept);
        var lost = Assert.Single(snapshot.CombatJournal, e => e.Type == CombatEventType.TargetLost);
        Assert.Equal(1000, intercept.ChanceTenths);
        Assert.Equal(CountermeasureTerminationKind.Intercept, intercept.TerminationKind);
        Assert.Equal(CountermeasureTerminationKind.TargetLost, lost.TerminationKind);
        Assert.Equal("target_lost", lost.Result);
        Assert.Equal(Defense2, lost.LauncherModuleId);
        Assert.Null(lost.Roll);
        Assert.Single(snapshot.CombatJournal, e => e.Roll is not null);
        Assert.Equal(TorpedoTerminationKind.Intercept, Assert.Single(snapshot.CombatImpacts).TerminationKind);
        Assert.DoesNotContain(snapshot.Objects, o => o.Countermeasure is not null || o.ObjectId == intercept.TargetObjectId);
        Assert.Equal(snapshot.CombatJournal.ToArray(), At(engine, 1100).CombatJournal.ToArray());
    }

    [Fact]
    public void Contact_on_deadline_records_miss_then_expiry_at_same_time()
    {
        using var engine = Start(lifetime: 1000);
        var events = At(engine, 1001).CombatJournal.Where(e => e.Type is CombatEventType.Miss or CombatEventType.Expired).ToArray();
        Assert.Equal(2, events.Length);
        Assert.Equal(CombatEventType.Miss, events[0].Type);
        Assert.Equal(CombatEventType.Expired, events[1].Type);
        Assert.InRange(Math.Abs(events[0].MotionTimeMs - events[1].MotionTimeMs), 0, 1e-6);
        Assert.True(events[0].EventId < events[1].EventId);
        Assert.Equal(CountermeasureTerminationKind.LifetimeExpired, events[1].TerminationKind);
    }

    [Fact]
    public void Destroyed_owner_is_distinct_from_target_loss_without_an_extra_roll()
    {
        using var engine = Start(fragile: true);
        string target = At(engine, 4901).Objects.Single(o => o.Torpedo is not null).ObjectId;
        engine.ReceiveCommand(Manual("late-second", target, Defense2));
        At(engine, 4901);
        var snapshot = At(engine, 5100);
        Assert.DoesNotContain(snapshot.Objects, o => o.ObjectId == Player || o.Countermeasure is not null);
        var lost = Assert.Single(snapshot.CombatJournal, e => e.TerminationKind == CountermeasureTerminationKind.OwnerLost);
        Assert.Equal("owner_lost", lost.Result);
        Assert.Equal(Defense2, lost.LauncherModuleId);
        Assert.Null(lost.Roll);
        Assert.Equal(2, snapshot.CombatJournal.Count(e => e.Roll is not null));
    }
}
