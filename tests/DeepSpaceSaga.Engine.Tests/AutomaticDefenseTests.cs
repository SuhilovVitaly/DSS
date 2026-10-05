using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;
namespace DeepSpaceSaga.Engine.Tests;

public class AutomaticDefenseTests
{
    [Theory]
    [InlineData(45)]
    [InlineData(120)]
    public void Replanning_preserves_configured_countermeasure_speed_and_turn_rate(double turnRate)
    {
        var source = Registry.Value;
        static IEnumerable<T> All<T>(DeepSpaceSaga.Engine.Content.TypeRegistry<T> registry)
            where T : DeepSpaceSaga.Engine.Content.ITypeDefinition => Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);
        var registry = DeepSpaceSaga.Engine.Content.GameDataRegistry.Create(All(source.ModuleCategories),
            All(source.ModuleTypes).Select(m => m.CountermeasureBaseRating is null ? m :
                m with { CountermeasureSpeedKmS = 8, CountermeasureTurnRateDegPerSec = turnRate }),
            All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes),
            All(source.Dialogues), All(source.Quests), stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses));
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(DefenseScenario());
        engine.ReceiveCommand(Fire("fire")); At(engine, 0); At(engine, 1000);
        var save = engine.CaptureSaveStateForTests(1000, SimulationSpeed.Speed0);
        var defense = save.GameState.DefenseState!;
        Assert.Single(defense.Projectiles);
        // A stale target-plan baseline requires an authoritative replan on the next guidance boundary.
        save = save with
        {
            GameState = save.GameState with
            {
                DefenseState = defense with
                { Projectiles = defense.Projectiles.Select(p => p with { TargetPlanStartMotionTimeMs = 1 }).ToArray() }
            }
        };
        using var resumed = new SimulationEngine(registry);
        resumed.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), isSave: true);
        var pr = Assert.Single(At(resumed, 1100).Objects, o => o.Countermeasure is not null);
        Assert.Equal(1100, pr.Countermeasure!.Route.StartMotionTimeMs);
        Assert.All(pr.Countermeasure.Route.Segments, s =>
        {
            Assert.Equal(8, s.SpeedKmS);
            Assert.InRange(Math.Abs(s.AngularVelocityDegPerSec), 0, turnRate);
        });
        var replanned = resumed.CaptureSaveStateForTests(1100, SimulationSpeed.Speed0);
        using var roundtripped = new SimulationEngine(registry);
        roundtripped.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(replanned), true), isSave: true);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(At(resumed, 25000).CombatJournal),
            System.Text.Json.JsonSerializer.Serialize(At(roundtripped, 25000).CombatJournal));
    }

    internal static ScenarioFile DefenseScenario(double distance = 600, int attack = 50, int defense = 50) => TorpedoImpactTests.Scenario(o => o with
    {
        PositionY = o.ObjectId == Player ? 0 : -distance,
        DirectionDegrees = o.ObjectId == Player ? 0 : 180,
        Modules = o.Modules!.Select(m => m with { AutoDefenseEnabled = true }).ToArray(),
        Crew = o.Crew!.Select(c => c with { TorpedoSkill = attack, CountermeasureSkill = defense }).ToArray()
    });
    [Fact]
    public void Defense_fires_only_after_time_advances_and_reserves_once()
    {
        using var engine = Create(DefenseScenario());
        engine.ReceiveCommand(Fire("fire"));
        Assert.DoesNotContain(At(engine, 0).Objects, o => o.Countermeasure is not null);
        Assert.DoesNotContain(At(engine, 0, 1000).Objects, o => o.Countermeasure is not null);
        var snapshot = At(engine, 100, 1100);
        var pr = Assert.Single(snapshot.Objects, o => o.Countermeasure is not null);
        Assert.Equal(500, pr.Countermeasure!.FrozenChanceTenths);
        Assert.True(Assert.Single(snapshot.Objects, o => o.Torpedo is not null).CountermeasureAttempted);
        Assert.Equal(12, pr.SpeedKmS);
        Assert.Equal(DefenseState.Guiding, snapshot.Objects.Single(o => o.ObjectId == Target).Defense!.State);
    }
    [Fact]
    public void Range_entry_is_found_inside_large_interval()
    {
        using var engine = Create(DefenseScenario(1500));
        engine.ReceiveCommand(Fire("fire")); At(engine, 0);
        Assert.DoesNotContain(At(engine, 16000).Objects, o => o.Countermeasure is not null);
        var pr = Assert.Single(At(engine, 18000).Objects, o => o.Countermeasure is not null);
        Assert.Equal(16667, pr.Countermeasure!.LaunchMotionTimeMs);
    }
    [Fact]
    public void Zero_chance_does_not_consume_attempt()
    {
        using var engine = Create(DefenseScenario(600, 100, 0));
        engine.ReceiveCommand(Fire("fire")); At(engine, 0);
        var snapshot = At(engine, 1000);
        Assert.DoesNotContain(snapshot.Objects, o => o.Countermeasure is not null);
        Assert.False(Assert.Single(snapshot.Objects, o => o.Torpedo is not null).CountermeasureAttempted);
    }
    [Fact]
    public void Disable_blocks_future_launches()
    {
        using var engine = Create(DefenseScenario());
        var module = Assert.Single(At(engine, 0).InstalledModules, m => m.Defense is not null);
        engine.ReceiveCommand(new("disable", 1, Player, module.ModuleId, DefenseCommandTypes.Disable));
        Assert.False(At(engine, 0).InstalledModules.Single(m => m.ModuleId == module.ModuleId).Defense!.AutoEnabled);
    }
    [Fact]
    public void Direct_fire_at_countermeasure_is_rejected_before_side_effects()
    {
        using var engine = Create(DefenseScenario());
        engine.ReceiveCommand(Fire("fire")); At(engine, 0);
        var snapshot = At(engine, 100);
        string pr = Assert.Single(snapshot.Objects, o => o.Countermeasure is not null).ObjectId;
        // The launcher is also busy; either guard leaves the original flight and attempt intact.
        engine.ReceiveCommand(Fire("illegal", pr));
        snapshot = At(engine, 100);
        Assert.Equal(CommandResultStatus.Rejected, Assert.Single(snapshot.CommandResults).Status);
        Assert.Single(snapshot.Objects, o => o.Torpedo is not null);
    }
}
