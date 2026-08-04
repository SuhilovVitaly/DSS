namespace DeepSpaceSaga.Engine.Content;

internal sealed record ItemTypeDefinition(
    string TypeId,
    string DisplayName,
    long UnitMassKg) : ITypeDefinition;
