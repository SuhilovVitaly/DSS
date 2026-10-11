using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
using static DeepSpaceSaga.Engine.Tests.ManualCountermeasureFireTests;

namespace DeepSpaceSaga.Engine.Tests;

public class CountermeasureLifetimeTests
{
    private static GameDataRegistry LifetimeRegistry(long lifetime, decimal accuracy = 0, bool slow = false)
    {
        var source = AutomaticDefenseTests.WeaponRegistry(accuracy, 0);
        static IEnumerable<T> All<T>(TypeRegistry<T> r) where T : ITypeDefinition => Enumerable.Range(0, r.Count).Select(r.GetDefinition);
        return GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Select(m =>
            m.CountermeasureAccuracy is not null ? m with
            {
                CountermeasureMaxFlightTimeMs = lifetime,
                CountermeasureSpeedKmS = slow ? 0.001 : m.CountermeasureSpeedKmS
            } :
            slow && m.TorpedoSpeedKmS is not null ? m with { TorpedoSpeedKmS = 0.001 } : m),
            All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes),
            All(source.Dialogues), All(source.Quests), stationMarketProfiles: All(source.StationMarketProfiles),
            shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents));
    }

    private static SimulationEngine Start(long lifetime, double distance, decimal accuracy = 0, bool obstacle = false)
    {
        var engine = new SimulationEngine(LifetimeRegistry(lifetime, accuracy));
        var scenario = HostileTorpedoFireTests.HostileScenario(distance);
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o =>
            o.ObjectId == Target ? o with { DirectionDegrees = 180 } : o).ToArray()
            }
        };
        if (obstacle) scenario = scenario with
        {
            GameState = scenario.GameState with
            { SpaceObjects = scenario.GameState.SpaceObjects.Append(Obstacle("hull-tie", 0, -120)).ToArray() }
        };
        engine.LoadScenario(scenario);
        engine.ReceiveCommand(Manual("manual", LaunchEnemy(engine)));
        At(engine, 1);
        return engine;
    }

    private static DefenseSnapshot Defense(AuthoritativeSnapshot snapshot) => snapshot.InstalledModules.Single(m => m.ModuleId == Defense1).Defense!;

    [Fact]
    public void Guiding_timeout_starts_reload_without_rng_and_large_step_matches_partitions()
    {
        using var large = Start(1000, 1000);
        using var small = Start(1000, 1000);
        var before = At(small, 1000);
        Assert.Single(before.Objects, o => o.Countermeasure is not null);
        var timeout = At(small, 1001);
        Assert.DoesNotContain(timeout.Objects, o => o.Countermeasure is not null);
        Assert.Equal(11001, Defense(timeout).ReloadDueMotionTimeMs);
        Assert.Equal(DefenseState.Reloading, Defense(timeout).State);
        Assert.Equal(DefenseState.Reloading, Defense(At(small, 11000)).State);
        var ready = At(small, 11001);
        var whole = At(large, 11001);
        Assert.Equal(DefenseState.Ready, Defense(ready).State);
        Assert.Equal(Defense(whole), Defense(ready));
        Assert.Empty(whole.CombatJournal.Where(e => e.Roll is not null));
        Assert.Empty(ready.CombatJournal.Where(e => e.Roll is not null));
        Assert.Equal(0ul, small.CaptureSaveStateForTests(11001, SimulationSpeed.Speed0).GameState.DefenseState!.RngCounter);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Contact_at_expiry_rolls_before_timeout(decimal accuracy)
    {
        using var engine = Start(1000, 155, accuracy);
        Assert.Empty(At(engine, 1000).CombatJournal.Where(e => e.Roll is not null));
        var after = At(engine, 1001);
        var resolution = Assert.Single(after.CombatJournal, e => e.Roll is not null);
        Assert.InRange(Math.Abs(resolution.MotionTimeMs - 1001), 0, 1e-6);
        Assert.Equal(accuracy == 0 ? CombatEventType.Miss : CombatEventType.Intercept, resolution.Type);
        Assert.DoesNotContain(after.Objects, o => o.Countermeasure is not null);
        Assert.Equal(11001, Defense(after).ReloadDueMotionTimeMs!.Value, 5);
        Assert.Equal(1ul, engine.CaptureSaveStateForTests(1001, SimulationSpeed.Speed0).GameState.DefenseState!.RngCounter);
    }

    [Fact]
    public void Contact_after_expiry_cannot_draw()
    {
        using var engine = Start(1000, 155.1);
        var after = At(engine, 1002);
        Assert.DoesNotContain(after.Objects, o => o.Countermeasure is not null);
        Assert.Empty(after.CombatJournal.Where(e => e.Roll is not null));
    }

    [Fact]
    public void Lifetime_caps_missed_coast()
    {
        using var engine = Start(1500, 155);
        var missed = Assert.Single(At(engine, 1100).Objects, o => o.Countermeasure is not null).Countermeasure!;
        Assert.Equal(CountermeasurePhase.MissedCoast, missed.Phase);
        Assert.Equal(1501, missed.MissExpiresAtMotionTimeMs);
        Assert.Single(At(engine, 1500).Objects, o => o.Countermeasure is not null);
        var timeout = At(engine, 1501);
        Assert.DoesNotContain(timeout.Objects, o => o.Countermeasure is not null);
        Assert.Equal(11501, Defense(timeout).ReloadDueMotionTimeMs);
        Assert.Single(timeout.CombatJournal, e => e.Roll is not null);
    }

    [Fact]
    public void Pause_toggle_and_replan_never_reset_deadline()
    {
        using var engine = Start(1000, 1000);
        for (int t = 100; t <= 900; t += 100)
        {
            engine.ReceiveCommand(new("toggle-" + t, (ulong)t, Player, Defense1,
                t % 200 == 0 ? DefenseCommandTypes.Enable : DefenseCommandTypes.Disable));
            var flight = Assert.Single(At(engine, t).Objects, o => o.Countermeasure is not null).Countermeasure!;
            Assert.Equal(1001, flight.ExpiresAtMotionTimeMs);
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(flight), System.Text.Json.JsonSerializer.Serialize(
                Assert.Single(At(engine, t, 9000000).Objects, o => o.Countermeasure is not null).Countermeasure));
        }
        Assert.DoesNotContain(At(engine, 1001).Objects, o => o.Countermeasure is not null);
    }

    [Fact]
    public void Automatic_launch_requires_contact_within_lifetime()
    {
        using var engine = new SimulationEngine(LifetimeRegistry(100, 100));
        engine.LoadScenario(AutomaticDefenseTests.DefenseScenario());
        engine.ReceiveCommand(Fire("fire"));
        At(engine, 0);
        Assert.DoesNotContain(At(engine, 1000).Objects, o => o.Countermeasure is not null);
    }

    [Fact]
    public void Hull_at_contact_wins_without_draw()
    {
        using var engine = Start(1000, 155, 100, obstacle: true);
        var snapshot = At(engine, 1001);
        Assert.DoesNotContain(snapshot.Objects, o => o.Countermeasure is not null);
        Assert.Empty(snapshot.CombatJournal.Where(e => e.Roll is not null));
        Assert.Equal("hull-tie", Assert.Single(snapshot.CombatImpacts).HitObjectId);
    }

    [Fact]
    public void Capped_missed_coast_survives_serializer_loader_and_expires_on_same_deadline()
    {
        using var original = Start(1500, 155, 50);
        At(original, 1100);
        var save = original.CaptureSaveStateForTests(1100, SimulationSpeed.Speed0);
        var loaded = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true);
        using var resumed = new SimulationEngine(LifetimeRegistry(1500, 50));
        resumed.LoadScenario(loaded, isSave: true);
        Assert.Equal(1501, Assert.Single(At(resumed, 1500).Objects, o => o.Countermeasure is not null).Countermeasure!.MissExpiresAtMotionTimeMs);
        Assert.Equal(Defense(At(original, 1501)), Defense(At(resumed, 1501)));
        Assert.DoesNotContain(At(resumed, 1501).Objects, o => o.Countermeasure is not null);
    }

    [Fact]
    public void Full_sixty_second_lifetime_is_physical_and_not_extended_by_guidance()
    {
        using var engine = new SimulationEngine(LifetimeRegistry(60000, slow: true));
        engine.LoadScenario(HostileTorpedoFireTests.HostileScenario());
        engine.ReceiveCommand(Manual("slow", LaunchEnemy(engine)));
        var flight = Assert.Single(At(engine, 1).Objects, o => o.Countermeasure is not null).Countermeasure!;
        Assert.Equal(60001, flight.ExpiresAtMotionTimeMs);
        Assert.Single(At(engine, 60000).Objects, o => o.Countermeasure is not null);
        var expired = At(engine, 60001);
        Assert.DoesNotContain(expired.Objects, o => o.Countermeasure is not null);
        Assert.Equal(70001, Defense(expired).ReloadDueMotionTimeMs);
        Assert.Empty(expired.CombatJournal.Where(e => e.Roll is not null));
        Assert.Equal(DefenseState.Ready, Defense(At(engine, 70001)).State);
    }
}
