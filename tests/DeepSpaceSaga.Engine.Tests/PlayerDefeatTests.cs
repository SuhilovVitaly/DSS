using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public class PlayerDefeatTests
{
    internal static ScenarioFile LethalScenario(int hp = 450) => TorpedoImpactTests.Scenario(o => o with
    {
        PositionY = o.ObjectId == Player ? 0 : -155,
        DirectionDegrees = o.ObjectId == Player ? 0 : 180,
        Modules = o.Modules!.Where(m => o.ObjectId != Player || hp != 150 || m.ModuleTypeId == "module.engine.basic")
            .Select(m => m with
            {
                AutoDefenseEnabled = false,
                PowerState = m.ModuleTypeId == "module.engine.basic" ? "Off" : "On"
            }).ToArray()
    });

    [Fact]
    public void Three_actual_hits_defeat_player_once_and_final_snapshot_has_wreck_identity()
    {
        using var engine = Create(LethalScenario());
        engine.SetObjectInteractionState(Player, Player);
        Assert.Single(At(engine, 6251).CombatJournal, e => e.Type == CombatEventType.ModuleDestroyed);
        Assert.Equal(2, At(engine, 22502).CombatJournal.Count(e => e.Type == CombatEventType.ModuleDestroyed));
        var final = At(engine, 38753);
        Assert.Equal(SessionOutcomeState.Defeated, final.EffectiveOutcome.State);
        Assert.Equal(Player, final.EffectiveOutcome.PlayerObjectId);
        Assert.Equal(3, final.CombatJournal.Count(e => e.Type == CombatEventType.Hit));
        var death = Assert.Single(final.CombatJournal, e => e.Type == CombatEventType.ShipDestroyed);
        Assert.Equal("threshold:3/9", death.DestroyReason);
        Assert.Equal((long)Math.Ceiling(death.MotionTimeMs), final.MotionTimeMs);
        Assert.Equal(death.EventId, final.EffectiveOutcome.DeathEventId);
        Assert.Equal(death.MotionTimeMs, final.EffectiveOutcome.DeathMotionTimeMs);
        Assert.Equal(death.Result, final.EffectiveOutcome.WreckObjectId);
        Assert.Single(final.Objects, o => o.ObjectId == death.Result && o.ObjectType == SpaceObjectType.Wreck);
        Assert.DoesNotContain(final.Objects, o => o.ObjectId == Player);
        Assert.Empty(final.InstalledModules);
        Assert.Null(final.ActiveObjectId); Assert.Null(final.SelectedObjectId);
        Assert.Null(final.DockedStationTrade); Assert.Equal(0, final.PlayerCrewCount);
        Assert.False(final.EffectiveOutcome.CanSave);
        string facts = JsonSerializer.Serialize(final.CombatJournal);
        engine.SetSpeed(SimulationSpeed.Speed4);
        var later = At(engine, 1000000);
        Assert.Equal(final.Outcome, later.Outcome);
        Assert.Equal(final.GameTimeMs, later.GameTimeMs);
        Assert.Equal(final.MotionTimeMs, later.MotionTimeMs);
        Assert.Equal(SimulationSpeed.Speed0, later.CurrentSpeed);
        Assert.Equal(facts, JsonSerializer.Serialize(later.CombatJournal));
        Assert.Equal(JsonSerializer.Serialize(final.Objects), JsonSerializer.Serialize(later.Objects));
    }

    [Fact]
    public void Critical_engine_death_transitions_once_to_ep8_defeat()
    {
        var source = Registry.Value;
        static IEnumerable<T> All<T>(TypeRegistry<T> types) where T : ITypeDefinition =>
            Enumerable.Range(0, types.Count).Select(types.GetDefinition);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories),
            All(source.ModuleTypes).Select(module => module.TypeId == "module.engine.basic"
                ? module with { CombatRole = "engine" } : module),
            All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes),
            All(source.Dialogues), All(source.Quests), catalogVersion: source.CatalogVersion,
            legacyCatalogFingerprint: source.LegacyCatalogFingerprint,
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses),
            stationMarketEvents: All(source.StationMarketEvents), combatRules: source.CombatRules,
            fireModes: source.FireModes);
        using var engine = Create(LethalScenario(150), registry);
        var final = At(engine, 600000, 180000000);
        var death = Assert.Single(final.CombatJournal, e => e.Type == CombatEventType.ShipDestroyed);
        Assert.Equal("critical:MOD-PLAYER-ENGINE-01", death.DestroyReason);
        Assert.Equal(death.EventId, final.EffectiveOutcome.DeathEventId);
        Assert.Equal(SessionOutcomeState.Defeated, final.EffectiveOutcome.State);
        Assert.False(final.EffectiveOutcome.CanSave);
        var later = At(engine, 900000, 270000000);
        Assert.Equal(final.EffectiveOutcome, later.EffectiveOutcome);
        Assert.Equal(final.MotionTimeMs, later.MotionTimeMs);
        Assert.Equal(final.GameTimeMs, later.GameTimeMs);
        Assert.Equal(final.CombatJournal.ToArray(), later.CombatJournal.ToArray());
    }

    [Fact]
    public void Defeated_world_rejects_all_gameplay_and_dialogue_commands_and_save_entrypoints()
    {
        using var engine = Create(LethalScenario(150));
        var final = At(engine, 6000);
        long credits = final.PlayerCredits;
        foreach (string type in new[] { CombatCommandTypes.Fire, DefenseCommandTypes.Fire, DefenseCommandTypes.Enable,
            NavigationComputerCommandTypes.Dock, ScannerCommandTypes.StructuralScan, TradeCommandTypes.Buy, "unknown" })
            engine.ReceiveCommand(new(type, 1, Player, Launcher, type, Target));
        engine.ReceiveDialogueCommand(new("dialogue-after-death", DialogueAction.Start, "none", 0,
            DialogueDefinitionId: "missing", ParticipantId: Target));
        var rejected = At(engine, 10000);
        Assert.Equal(7, rejected.CommandResults.Length);
        Assert.All(rejected.CommandResults, r =>
        {
            Assert.Equal(CommandResultStatus.Rejected, r.Status);
            Assert.Equal(CommandReasonCodes.SessionDefeated, r.ReasonCode);
        });
        Assert.Contains(rejected.DialogueEvents, e => e.CommandId == "dialogue-after-death" && e.EventCode == CommandReasonCodes.SessionDefeated);
        Assert.Equal(credits, rejected.PlayerCredits);
        var travel = engine.TravelStation(new("travel-after-death", StationDistrict.Market));
        Assert.False(travel.Accepted);
        Assert.Equal(CommandReasonCodes.SessionDefeated, travel.Error);
        Assert.Equal(final.CurrentStationDistrict, travel.Snapshot.CurrentStationDistrict);
        Assert.Equal(CommandReasonCodes.SessionDefeated, Assert.Throws<InvalidOperationException>(() => engine.CaptureSaveState()).Message);
        Assert.Equal(CommandReasonCodes.SessionDefeated, Assert.Throws<InvalidOperationException>(() => engine.CaptureSaveStateForTests(20000, SimulationSpeed.Speed4)).Message);
        Assert.Equal(final.CombatJournal.ToArray(), rejected.CombatJournal.ToArray());
    }

    [Fact]
    public void Save_that_advances_into_lethal_hit_fails_before_returning_a_save()
    {
        using var engine = Create(LethalScenario(150));
        Assert.True(At(engine, 1).EffectiveOutcome.CanSave);
        Assert.Equal(CommandReasonCodes.SessionDefeated,
            Assert.Throws<InvalidOperationException>(() => engine.CaptureSaveStateForTests(6000, SimulationSpeed.Speed0)).Message);
        Assert.Equal(SessionOutcomeState.Defeated, At(engine, 6000).EffectiveOutcome.State);
    }

    [Fact]
    public void Invalid_load_preserves_defeat_and_valid_new_game_resets_it()
    {
        var scenario = LethalScenario(150);
        using var engine = Create(scenario);
        var defeat = At(engine, 6000).Outcome;
        var invalid = scenario with
        {
            GameState = scenario.GameState with
            { SpaceObjects = scenario.GameState.SpaceObjects.Append(scenario.GameState.SpaceObjects[0]).ToArray() }
        };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(invalid));
        Assert.Equal(defeat, engine.CaptureSnapshot().Outcome);
        engine.LoadScenario(scenario);
        var fresh = At(engine, 0);
        Assert.Equal(SessionOutcomeSnapshot.Running, fresh.Outcome);
        Assert.True(fresh.EffectiveOutcome.CanSave);
        Assert.Empty(fresh.CombatJournal);
        Assert.NotNull(engine.CaptureSaveState());
    }

    [Fact]
    public void Destroying_enemy_does_not_end_the_session()
    {
        using var engine = Create();
        for (int shot = 0; shot < 3; shot++)
        {
            engine.ReceiveCommand(Fire("shot-" + shot));
            At(engine, shot * 15000);
            At(engine, shot * 15000 + 10000);
        }
        var final = At(engine, 40000);
        Assert.DoesNotContain(final.Objects, o => o.ObjectId == Target);
        Assert.Equal(SessionOutcomeState.Running, final.EffectiveOutcome.State);
        Assert.True(final.EffectiveOutcome.CanSave);
        Assert.NotNull(engine.CaptureSaveStateForTests(40000, SimulationSpeed.Speed0));
    }
}
