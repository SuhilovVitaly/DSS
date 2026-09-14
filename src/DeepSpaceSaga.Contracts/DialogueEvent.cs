using System.Collections.Immutable;

namespace DeepSpaceSaga.Contracts;

public sealed record DialogueEvent(string EventId, string InstanceId, string EventCode,
    long EffectiveGameTimeMs, ImmutableDictionary<string, string> Parameters,
    string? CommandId = null, string? TextKey = null);
