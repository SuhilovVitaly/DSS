using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Dialogue;

/// <summary>Immutable narrative state; transactions replace it only after every effect succeeds.</summary>
public sealed record DialogueProgressState(
    PlayerCharacterState PlayerCharacter,
    ImmutableDictionary<string, QuestState> Quests,
    ImmutableHashSet<string> Flags,
    ImmutableDictionary<string, StationAccessState> StationAccessStates,
    ImmutableArray<SecurityIncident> SecurityIncidents)
{
    public static DialogueProgressState Empty { get; } = new(
        new(ImmutableDictionary<string, int>.Empty), ImmutableDictionary<string, QuestState>.Empty,
        ImmutableHashSet<string>.Empty, ImmutableDictionary<string, StationAccessState>.Empty, []);
}

public sealed record DialogueSaveState(DialogueState? ActiveDialogue, DialogueProgressState Progress,
    ImmutableHashSet<string> ProcessedCommandIds, ulong NextInstanceId, ulong NextEventId,
    SimulationSpeed? ResumeSpeed);

internal sealed class DialogueRuntime
{
    public DialogueState? Active { get; set; }
    public DialogueProgressState Progress { get; set; } = DialogueProgressState.Empty;
    public ImmutableHashSet<string> ProcessedCommandIds { get; set; } = ImmutableHashSet<string>.Empty;
    public ulong NextInstanceId { get; set; }
    public ulong NextEventId { get; set; }
    public SimulationSpeed? ResumeSpeed { get; set; }
    // Keep a bounded event history so a fast snapshot consumer cannot lose an acknowledgement.
    public List<DialogueEvent> Events { get; } = [];
    public void Emit(string instance, string code, long time, string? commandId = null,
        string? textKey = null, ImmutableDictionary<string, string>? parameters = null)
    {
        Events.Add(new($"dialogue-event-{++NextEventId}", instance, code, time,
            parameters ?? ImmutableDictionary<string, string>.Empty, commandId, textKey));
        if (Events.Count > 128) Events.RemoveAt(0);
    }
    public DialogueSaveState Save() => new(Active, Progress, ProcessedCommandIds, NextInstanceId, NextEventId, ResumeSpeed);
    public void Load(DialogueSaveState? save)
    {
        Active = save?.ActiveDialogue;
        Progress = save?.Progress ?? DialogueProgressState.Empty;
        ProcessedCommandIds = save?.ProcessedCommandIds ?? ImmutableHashSet<string>.Empty;
        NextInstanceId = save?.NextInstanceId ?? 0;
        NextEventId = save?.NextEventId ?? 0;
        ResumeSpeed = save?.ResumeSpeed;
        Events.Clear();
    }
}

internal static class DialogueChoiceValidator
{
    public static string? Validate(ImmutableArray<DialogueCondition> conditions, DialogueProgressState state)
    {
        foreach (var c in conditions.IsDefault ? [] : conditions)
        {
            bool allowed = c.Type switch
            {
                "FlagSet" => state.Flags.Contains(c.Key),
                "FlagClear" => !state.Flags.Contains(c.Key),
                "AttributeAtLeast" => state.PlayerCharacter.Attributes.GetValueOrDefault(c.Key) >= c.Value,
                "QuestActive" => state.Quests.TryGetValue(c.Key, out var q) && q.Status == "active",
                _ => false
            };
            if (!allowed) return "dialogue_condition_not_met";
        }
        return null;
    }
}

internal static class QuestStateStore
{
    public static QuestState Start(QuestDefinition definition) => new(definition.QuestId, "active",
        definition.Objectives.ToImmutableDictionary(id => id, _ => "active"), ImmutableHashSet<string>.Empty);
    public static QuestState SetObjective(QuestState quest, string objective, string status)
    {
        var objectives = quest.ObjectiveStates.SetItem(objective, status);
        return quest with { ObjectiveStates = objectives,
            Status = objectives.Values.Any(s => s == "failed") ? "failed" :
                objectives.Values.All(s => s == "completed") ? "completed" : "active" };
    }
}
