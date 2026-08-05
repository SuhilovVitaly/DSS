namespace DeepSpaceSaga.Engine.Content;

internal sealed record CommandDefinition(
    string TypeId,
    string DisplayName) : ITypeDefinition;
