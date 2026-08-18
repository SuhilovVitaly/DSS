using System.Collections.Immutable;

namespace DeepSpaceSaga.Engine.Content;

internal sealed record ModuleCategoryDefinition(
    string TypeId,
    string DisplayName,
    int SlotSize,
    ImmutableArray<string> CommandTypeIds) : ITypeDefinition;
