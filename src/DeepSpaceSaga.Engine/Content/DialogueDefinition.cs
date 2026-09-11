using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Content;

internal sealed record DialogueDefinition(string DialogueId, string DisplayName, string EntryNodeId,
    bool CanAbort, ImmutableArray<DialogueNode> Nodes, bool AllowManualStart = false,
    ImmutableArray<DialogueCondition> Conditions = default) : ITypeDefinition
{
    public string TypeId => DialogueId;
    public DialogueNode Node(string id) => Nodes.Single(n => n.NodeId == id);
}

internal sealed record DialogueNode(string NodeId, string SpeakerRole, string TextKey,
    ImmutableArray<DialogueChoice> Choices);
internal sealed record DialogueChoice(string ChoiceId, string TextKey, string? NextNodeId = null,
    ImmutableArray<DialogueEffect> Effects = default, ImmutableArray<DialogueCondition> Conditions = default);
internal sealed record DialogueCondition(string Type, string Key, long Value = 0, string? QuestId = null);
internal sealed record DialogueEffect(string Type, long Amount = 0, string? AmountSource = null,
    string? ItemTypeId = null, long Quantity = 0, string? Attribute = null,
    string? Flag = null, string? QuestId = null, string? ObjectiveId = null,
    string? StationScope = null, string? IncidentType = null);
internal sealed record QuestDefinition(string QuestId, ImmutableArray<string> Objectives) : ITypeDefinition
{
    public string TypeId => QuestId;
}
