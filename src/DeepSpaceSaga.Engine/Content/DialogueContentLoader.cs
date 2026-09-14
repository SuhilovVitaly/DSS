using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Engine.Content;

internal static class DialogueContentLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    private sealed record DialogueFile(ImmutableArray<DialogueDefinition> Dialogues);
    private sealed record QuestFile(ImmutableArray<QuestDefinition> Quests);
    private static readonly HashSet<string> Effects = new(StringComparer.Ordinal)
    {
        "ModifyCharacterAttribute", "AddCargoItem", "RemoveCargoItem", "AddCredits", "RemoveCredits",
        "AddStationCredits", "SetFlag", "ClearFlag", "StartQuest", "CompleteQuestObjective",
        "FailQuestObjective", "GrantStationAccess", "DenyStationAccess", "ArmSecurityIncident",
        "DockPlayerToStation", "EndDialogue"
    };
    private static IEnumerable<T> Read<T>(string path)
    {
        var files = Directory.Exists(path)
            ? Directory.GetFiles(path, "*.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToArray()
            : new[] { path };
        if (files.Length == 0) throw new ContentException($"Empty content directory: {path}");
        foreach (var file in files)
        {
            T result;
            try { result = JsonSerializer.Deserialize<T>(File.ReadAllText(file), Options)
                    ?? throw new ContentException($"Null content: {file}"); }
            catch (Exception ex) when (ex is JsonException or IOException)
            { throw new ContentException($"Invalid content: {file}", ex); }
            yield return result;
        }
    }
    public static DialogueDefinition[] Load(string path)
    {
        var files = Read<DialogueFile>(path).ToArray();
        if (files.Any(f => f.Dialogues.IsDefault)) throw new ContentException("Missing dialogues array.");
        var definitions = files.SelectMany(f => f.Dialogues).ToArray();
        foreach (var definition in definitions) Validate(definition);
        return definitions;
    }
    public static QuestDefinition[] LoadQuests(string path)
    {
        var files = Read<QuestFile>(path).ToArray();
        if (files.Any(f => f.Quests.IsDefault)) throw new ContentException("Missing quests array.");
        var quests = files.SelectMany(f => f.Quests).ToArray();
        foreach (var quest in quests)
            if (string.IsNullOrWhiteSpace(quest.QuestId) || quest.Objectives.IsDefaultOrEmpty ||
                quest.Objectives.Any(string.IsNullOrWhiteSpace) || quest.Objectives.Distinct().Count() != quest.Objectives.Length)
                throw new ContentException($"Invalid quest: {quest.QuestId}");
        return quests;
    }
    public static void Validate(DialogueDefinition definition)
    {
        if (definition is null || definition.Nodes.IsDefaultOrEmpty || string.IsNullOrWhiteSpace(definition.DialogueId))
            throw new ContentException("Dialogue requires an id and nodes.");
        var ids = definition.Nodes.Select(n => n.NodeId).ToHashSet(StringComparer.Ordinal);
        if (ids.Count != definition.Nodes.Length || !ids.Contains(definition.EntryNodeId))
            throw new ContentException($"Invalid dialogue nodes: {definition.DialogueId}");
        foreach (var node in definition.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.NodeId) || string.IsNullOrWhiteSpace(node.TextKey) || node.Choices.IsDefault)
                throw new ContentException($"Invalid node: {node.NodeId}");
            if (node.Choices.Select(c => c.ChoiceId).Distinct().Count() != node.Choices.Length)
                throw new ContentException($"Duplicate choices: {node.NodeId}");
            foreach (var choice in node.Choices)
            {
                if (string.IsNullOrWhiteSpace(choice.ChoiceId) || string.IsNullOrWhiteSpace(choice.TextKey) ||
                    (choice.NextNodeId is not null && !ids.Contains(choice.NextNodeId)))
                    throw new ContentException($"Invalid choice: {choice.ChoiceId}");
                foreach (var effect in choice.Effects.IsDefault ? [] : choice.Effects)
                {
                    if (!Effects.Contains(effect.Type) || effect.AmountSource is not (null or "station.portFeeCreditsPerDay") ||
                        effect.StationScope is not (null or "current"))
                        throw new ContentException($"Invalid dialogue effect: {effect.Type}");
                    if ((effect.Type is "AddCargoItem" or "RemoveCargoItem") &&
                        (string.IsNullOrWhiteSpace(effect.ItemTypeId) || effect.Quantity <= 0))
                        throw new ContentException("Cargo effect requires item and positive quantity.");
                    if (effect.Type.Contains("Credits", StringComparison.Ordinal) && effect.Amount < 0)
                        throw new ContentException("Credits effect amount must be non-negative.");
                    if (effect.Type == "ModifyCharacterAttribute" && string.IsNullOrWhiteSpace(effect.Attribute) ||
                        effect.Type is "SetFlag" or "ClearFlag" && string.IsNullOrWhiteSpace(effect.Flag) ||
                        effect.Type == "ArmSecurityIncident" && string.IsNullOrWhiteSpace(effect.IncidentType) ||
                        effect.Type.Contains("Quest", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(effect.QuestId) ||
                        effect.Type.EndsWith("Objective", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(effect.ObjectiveId))
                        throw new ContentException($"Missing effect argument: {effect.Type}");
                }
                ValidateConditions(choice.Conditions);
            }
        }
        ValidateConditions(definition.Conditions);
    }
    private static void ValidateConditions(ImmutableArray<DialogueCondition> conditions)
    {
        foreach (var c in conditions.IsDefault ? [] : conditions)
            if (c.Type is not ("FlagSet" or "FlagClear" or "AttributeAtLeast" or "QuestActive") || string.IsNullOrWhiteSpace(c.Key))
                throw new ContentException($"Invalid dialogue condition: {c.Type}");
    }
}
