namespace DeepSpaceSaga.Engine.Content;

/// <summary>
/// Type data for a factory. The embedded <see cref="Recipe"/> is factory type data and is
/// not registered in the Recipes registry (factory↔recipe cross-linking is a future iteration).
/// Pure type data — never copied into runtime state; the registry owns definitions.
/// </summary>
internal sealed record FactoryTypeDefinition(
    string TypeId,
    string DisplayName,
    RecipeDefinition Recipe) : ITypeDefinition;
