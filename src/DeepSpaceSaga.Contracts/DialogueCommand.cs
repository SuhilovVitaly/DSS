namespace DeepSpaceSaga.Contracts;

public enum DialogueAction { Start, Choose, Abort }

/// <summary>Start uses definition/participant context; Choose and Abort require the current revision.</summary>
public sealed record DialogueCommand(
    string CommandId,
    DialogueAction Action,
    string DialogueInstanceId,
    long ExpectedRevision,
    string? ChoiceId = null,
    string? DialogueDefinitionId = null,
    string? ParticipantId = null,
    string? StationObjectId = null);
