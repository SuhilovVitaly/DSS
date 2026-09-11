using System.Collections.Immutable;

namespace DeepSpaceSaga.Contracts;

public sealed record DialogueState(
    string InstanceId,
    string DialogueDefinitionId,
    string? StationObjectId,
    string ParticipantId,
    string CurrentNodeId,
    long Revision,
    bool CanAbort,
    ImmutableArray<DialogueChoiceSnapshot> Choices,
    string TextKey,
    string SpeakerRole,
    string? SpeakerDisplayName,
    string? SpeakerPortraitImage,
    ImmutableDictionary<string, string> Parameters);

public sealed record PlayerCharacterState(ImmutableDictionary<string, int> Attributes);
public sealed record QuestState(string QuestId, string Status,
    ImmutableDictionary<string, string> ObjectiveStates, ImmutableHashSet<string> Flags);
public sealed record StationAccessState(string StationObjectId, bool AccessDenied);
public sealed record SecurityIncident(string IncidentId, string StationObjectId, string IncidentType,
    long StartedGameTimeMs, long DeadlineGameTimeMs, bool Completed = false);
