using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public class DialogueEffectTests
{
    private static (SimulationEngine Engine, GameDataRegistry Registry) Create(ImmutableArray<DialogueEffect> effects,
        bool canAbort = true, ImmutableArray<DialogueCondition> conditions = default)
    {
        var baseRegistry = DockCommandTests.CreateRegistry(200);
        var definition = new DialogueDefinition("test", "Test", "entry", canAbort,
            [new("entry", "Captain", "test.entry", [new("choose", "test.choose", Effects: effects, Conditions: conditions)])], AllowManualStart: true);
        var registry = GameDataRegistry.Create(
            Enumerable.Range(0, baseRegistry.ModuleCategories.Count).Select(baseRegistry.ModuleCategories.GetDefinition),
            [baseRegistry.ModuleTypes.GetDefinition(0) with { CargoCapacityKg = 20 }],
            [new ItemTypeDefinition("item.test", "Test", 2)],
            Enumerable.Range(0, baseRegistry.CommandDefinitions.Count).Select(baseRegistry.CommandDefinitions.GetDefinition),
            dialogues: [definition], quests: [new("quest.test", ["first", "second"])]);
        using var original = DockCommandTests.CreateEngine();
        var engine = new SimulationEngine(registry);
        engine.LoadScenario(original.CaptureSaveState());
        engine.ReceiveDialogueCommand(new("start", DialogueAction.Start, "", 0,
            DialogueDefinitionId: "test", ParticipantId: "operator", StationObjectId: "STATION-01"));
        Assert.NotNull(engine.CaptureSnapshotForTests().ActiveDialogue);
        return (engine, registry);
    }
    [Fact]
    public void Universal_effects_update_character_cargo_flags_and_quests_and_round_trip()
    {
        var (engine, registry) = Create([
            new("AddCredits", Amount: 25), new("RemoveCredits", Amount: 5),
            new("ModifyCharacterAttribute", Attribute: "engineering", Amount: 4),
            new("AddCargoItem", ItemTypeId: "item.test", Quantity: 10),
            new("RemoveCargoItem", ItemTypeId: "item.test", Quantity: 3),
            new("SetFlag", Flag: "keep"), new("SetFlag", Flag: "remove"), new("ClearFlag", Flag: "remove"),
            new("StartQuest", QuestId: "quest.test"),
            new("CompleteQuestObjective", QuestId: "quest.test", ObjectiveId: "first"),
            new("FailQuestObjective", QuestId: "quest.test", ObjectiveId: "second"),
            new("DenyStationAccess"), new("GrantStationAccess"), new("EndDialogue")]);
        using (engine)
        {
            var snapshot = DialogueTests.Choose(engine, "choose");
            Assert.Null(snapshot.ActiveDialogue);
            Assert.Equal(1020, snapshot.PlayerCredits);
            Assert.Equal(4, snapshot.PlayerCharacter!.Attributes["engineering"]);
            Assert.Equal("failed", Assert.Single(snapshot.Quests).Status);
            var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(engine.CaptureSaveState()));
            using var loaded = new SimulationEngine(registry); loaded.LoadScenario(save);
            var saved = loaded.CaptureSaveState().GameState;
            Assert.Equal(7, Assert.Single(saved.SpaceObjects.Single(o => o.ObjectId == "SPC-0001").Modules![0].Cargo!).Quantity);
            Assert.Contains("keep", saved.DialogueState!.Progress.Flags);
            Assert.DoesNotContain("remove", saved.DialogueState.Progress.Flags);
            Assert.False(saved.DialogueState.Progress.StationAccessStates["STATION-01"].AccessDenied);
            Assert.Equal(4, loaded.CaptureSnapshotForTests().PlayerCharacter!.Attributes["engineering"]);
            Assert.Equal("completed", saved.DialogueState.Progress.Quests["quest.test"].ObjectiveStates["first"]);
        }
    }
    [Theory]
    [InlineData("AddCargoItem", 11, "cargo_capacity_exceeded")]
    [InlineData("RemoveCargoItem", 1, "insufficient_cargo_quantity")]
    public void Cargo_failure_rolls_back_all_earlier_effects(string type, long quantity, string reason)
    {
        var (engine, _) = Create([new("RemoveCredits", Amount: 100), new("ModifyCharacterAttribute", Attribute: "agility", Amount: 2),
            new("SetFlag", Flag: "changed"), new(type, ItemTypeId: "item.test", Quantity: quantity)]);
        using (engine)
        {
            var snapshot = DialogueTests.Choose(engine, "choose");
            Assert.Equal(1000, snapshot.PlayerCredits);
            Assert.Empty(snapshot.PlayerCharacter!.Attributes);
            Assert.Empty(engine.CaptureSaveState().GameState.DialogueState!.Progress.Flags);
            Assert.Contains(snapshot.DialogueEvents, e => e.EventCode == reason);
        }
    }
    [Fact]
    public void Conditions_and_unabortable_dialogues_are_enforced_by_engine()
    {
        var (engine, _) = Create([new("RemoveCredits", Amount: 1)], canAbort: false,
            conditions: [new("FlagSet", "missing")]);
        using (engine)
        {
            var active = engine.CaptureSnapshotForTests().ActiveDialogue!;
            Assert.False(Assert.Single(active.Choices).Enabled);
            var snapshot = DialogueTests.Choose(engine, "choose");
            engine.ReceiveDialogueCommand(new("abort", DialogueAction.Abort, active.InstanceId, active.Revision));
            snapshot = engine.CaptureSnapshotForTests();
            Assert.NotNull(snapshot.ActiveDialogue);
            Assert.Equal(1000, snapshot.PlayerCredits);
            Assert.Contains(snapshot.DialogueEvents, e => e.EventCode == "dialogue_abort_not_allowed");
        }
    }
    [Fact]
    public void Content_rejects_broken_node_links_and_unknown_effects()
    {
        var invalidLink = new DialogueDefinition("test", "Test", "entry", true,
            [new("entry", "operator", "text", [new("choose", "text", "missing")])]);
        Assert.Throws<ContentException>(() => DialogueContentLoader.Validate(invalidLink));
        var invalidEffect = invalidLink with { Nodes = [new("entry", "operator", "text", [new("choose", "text", Effects: [new("Unrecognized")])])] };
        Assert.Throws<ContentException>(() => DialogueContentLoader.Validate(invalidEffect));
    }
}
