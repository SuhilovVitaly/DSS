namespace DeepSpaceSaga.Contracts;

public sealed record DialogueChoiceSnapshot(string ChoiceId, string TextKey, bool Enabled, string? DisabledReasonKey = null);
