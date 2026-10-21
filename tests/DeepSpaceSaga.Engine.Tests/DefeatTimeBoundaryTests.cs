using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public class DefeatTimeBoundaryTests
{
    [Fact]
    public void No_combat_events_follow_module_defeat_inside_large_step()
    {
        using var engine = Create(PlayerDefeatTests.LethalScenario(150));
        var final = At(engine, 600000, 180000000);
        var death = Assert.Single(final.CombatJournal, e => e.Type == CombatEventType.ShipDestroyed);
        Assert.Equal((long)Math.Ceiling(death.MotionTimeMs), final.MotionTimeMs);
        Assert.DoesNotContain(final.CombatJournal, e => e.MotionTimeMs > death.MotionTimeMs);
        Assert.Equal(death.EventId, final.EffectiveOutcome.DeathEventId);
        Assert.Equal(SimulationSpeed.Speed0, final.CurrentSpeed);
        Assert.False(final.EffectiveOutcome.CanSave);
        var later = At(engine, 900000, 270000000);
        Assert.Equal(final.CombatJournal.ToArray(), later.CombatJournal.ToArray());
    }

    [Fact]
    public void Fractional_lethal_hit_stops_at_first_integer_not_before_death()
    {
        var scenario = PlayerDefeatTests.LethalScenario(150);
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o.ObjectId == Player
                    ? o : o with { PositionY = -155.0123 }).ToArray()
            }
        };
        using var whole = Create(scenario);
        using var split = Create(scenario);
        var expected = At(whole, 600000, 180000000);
        foreach (long time in new long[] { 7, 123, 1111, 4999, 5000 }) At(split, time, time * 300);
        var actual = At(split, 600000, 180000000);
        foreach (var final in new[] { expected, actual })
        {
            double death = final.EffectiveOutcome.DeathMotionTimeMs!.Value;
            Assert.InRange(death - Math.Floor(death), .1, .9);
            Assert.Equal((long)Math.Ceiling(death), final.MotionTimeMs);
            Assert.Equal(death, Assert.Single(final.CombatJournal, e => e.Type == CombatEventType.ShipDestroyed).MotionTimeMs);
            Assert.DoesNotContain(final.CombatJournal, e => e.MotionTimeMs > death);
            Assert.Equal(SimulationSpeed.Speed0, final.CurrentSpeed);
        }
        Assert.Equal(expected.MotionTimeMs, actual.MotionTimeMs);
        Assert.Equal(expected.GameTimeMs, actual.GameTimeMs);
        var later = At(whole, 900000, 270000000);
        Assert.Equal(expected.Outcome, later.Outcome);
        Assert.Equal(JsonSerializer.Serialize(expected.Objects), JsonSerializer.Serialize(later.Objects));
        Assert.Equal(expected.CombatJournal.ToArray(), later.CombatJournal.ToArray());
    }

    [Fact]
    public void Lethal_hit_in_large_step_stops_all_systems()
    {
        var source = Registry.Value;
        static IEnumerable<T> All<T>(TypeRegistry<T> types) where T : ITypeDefinition =>
            Enumerable.Range(0, types.Count).Select(types.GetDefinition);
        var recipe = new RecipeDefinition("recipe.defeat", "Food", [new("item.food-rations", 1)],
            [new("item.food-rations", 2)], 12 * GameCalendar.HourMs);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes),
            All(source.ItemTypes), All(source.CommandDefinitions), factoryTypes: [new("factory.defeat", "Food", recipe)],
            shipClasses: All(source.ShipClasses), stationMarketProfiles: All(source.StationMarketProfiles),
            stationMarketEvents: All(source.StationMarketEvents),
            combatRules: source.CombatRules);
        var scenario = PlayerDefeatTests.LethalScenario(150);
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Append(new SpaceObjectData("factory", SpaceObjectType.Station,
                "Permanent", "Factory", 100000, 100000, 0, 0, "Stationary", null, null, null,
                IsKnown: true, Credits: 1000, PriceCoefficient: 1000,
                Inventory: [new("item.food-rations", 100)], ProducingModules: [new("factory.defeat")])).ToArray()
            }
        };
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(scenario);
        At(engine, 1, 300);
        var captured = Assert.Single(engine.CaptureSaveStateForTests(300, SimulationSpeed.Speed0, 1).GameState.PointCombatState!.Projectiles).Flight;
        var final = At(engine, 2 * GameCalendar.DayMs / 300, 2 * GameCalendar.DayMs);
        Assert.Equal(SessionOutcomeState.Defeated, final.EffectiveOutcome.State);
        var launch = Assert.Single(final.CombatJournal, e => e.Type == CombatEventType.Launch);
        Assert.Equal(captured.LaunchMotionTimeMs, launch.MotionTimeMs);
        double expectedDeath = captured.DetonationMotionTimeMs;
        Assert.Equal(launch.MotionTimeMs + 155 / (captured.SpeedKmS / 100), expectedDeath, 8);
        double death = final.EffectiveOutcome.DeathMotionTimeMs!.Value;
        Assert.InRange(Math.Abs(death - expectedDeath), 0, .0001);
        Assert.Equal((long)Math.Ceiling(death), final.MotionTimeMs);
        // Calendar time floors fractional contacts rather than the rounded physical boundary.
        Assert.InRange(Math.Abs(final.GameTimeMs - expectedDeath * 300), 0, 1);
        Assert.Equal(0, final.MissingRations);
        Assert.Empty(final.ShipEvents);
        var factory = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == "factory");
        Assert.Equal(99, Assert.Single(factory.Inventory, i => i.ItemTypeIndex == registry.ItemTypes.GetIndex("item.food-rations")).StockQuantity);
        Assert.Equal(12 * GameCalendar.HourMs, Assert.Single(factory.ProducingModules).NextProductionDueGameTimeMs);
        var world = engine.RuntimeObjects.ToArray();
        var rngBefore = engine.CaptureLaunchScatterState();
        engine.SetSpeed(SimulationSpeed.Speed4);
        var later = At(engine, 100 * GameCalendar.DayMs, 300 * GameCalendar.DayMs);
        Assert.Equal(final.Outcome, later.Outcome);
        Assert.Equal(world, engine.RuntimeObjects.ToArray());
        Assert.Equal(rngBefore, engine.CaptureLaunchScatterState());
        Assert.Equal(final.CombatJournal.ToArray(), later.CombatJournal.ToArray());
        Assert.Empty(later.ShipEvents);
    }

    [Fact]
    public void Split_steps_have_same_defeat_boundary()
    {
        using var whole = Create(PlayerDefeatTests.LethalScenario(150));
        using var split = Create(PlayerDefeatTests.LethalScenario(150));
        var expected = At(whole, 600000, 180000000);
        foreach (long time in new long[] { 7, 123, 1111, 4999, 5000 }) At(split, time, time * 300);
        var actual = At(split, 600000, 180000000);
        Assert.Equal(expected.EffectiveOutcome.DeathEventId, actual.EffectiveOutcome.DeathEventId);
        Assert.Equal(expected.EffectiveOutcome.WreckObjectId, actual.EffectiveOutcome.WreckObjectId);
        Assert.Equal(expected.GameTimeMs, actual.GameTimeMs);
        Assert.Equal(expected.MotionTimeMs, actual.MotionTimeMs);
        Assert.InRange(Math.Abs(expected.EffectiveOutcome.DeathMotionTimeMs!.Value - actual.EffectiveOutcome.DeathMotionTimeMs!.Value), 0, .0001);
        Assert.Equal(expected.CombatJournal.Select(e => (e.Type, e.EventId)), actual.CombatJournal.Select(e => (e.Type, e.EventId)));
    }

    [Fact]
    public void Resume_and_modal_close_cannot_restart_defeated_world()
    {
        long now = 0;
        var clock = new SimulationClock(SimulationSpeed.Speed0, () => now);
        var scenario = PlayerDefeatTests.LethalScenario(150);
        using var engine = new SimulationEngine(Registry.Value, [], clock);
        engine.LoadScenario(scenario);
        engine.SetSpeed(SimulationSpeed.Speed1);
        now = 600000;
        var final = engine.CaptureSnapshot(advanceClock: true);
        Assert.Equal(SessionOutcomeState.Defeated, final.EffectiveOutcome.State);
        var stopped = clock.Capture();
        Assert.Equal(final.GameTimeMs, stopped.GameTimeMs);
        Assert.Equal(final.MotionTimeMs, stopped.MotionTimeMs);
        foreach (var speed in Enum.GetValues<SimulationSpeed>())
        {
            now += 10000;
            engine.SetSpeed(speed);
            clock.SetSpeed(speed); // A stale pause owner must not unlatch the underlying clock either.
            clock.ResetRealBaseline();
            now += 10000;
            clock.Update();
            Assert.Equal(stopped, clock.UpdateAndCapture());
            Assert.Equal(final.Outcome, engine.CaptureSnapshot(advanceClock: true).Outcome);
        }
        clock.StopAtTerminal(99999, 99999);
        Assert.Equal(stopped, clock.Capture());
        engine.LoadScenario(scenario);
        engine.SetSpeed(SimulationSpeed.Speed1);
        now += 100;
        var fresh = engine.CaptureSnapshot(advanceClock: true);
        Assert.Equal(SessionOutcomeState.Running, fresh.EffectiveOutcome.State);
        Assert.Equal(25, fresh.MotionTimeMs);
        Assert.Equal(7500, fresh.GameTimeMs);
    }
}
