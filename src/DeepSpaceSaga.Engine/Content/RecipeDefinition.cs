using System.Collections.Immutable;

namespace DeepSpaceSaga.Engine.Content;

/// <summary>
/// One input or output material of a recipe: an item type and an integer quantity.
/// Pure type data — never copied into runtime state; the registry owns definitions.
/// </summary>
internal sealed record RecipeMaterial(string ItemTypeId, long Count);

/// <summary>
/// Type data for a production recipe. Pure type data — never copied into runtime state;
/// production looks recipes up from the registry by index. The registry owns definitions.
/// </summary>
internal sealed record RecipeDefinition(
    string TypeId,
    string DisplayName,
    ImmutableArray<RecipeMaterial> Inputs,
    ImmutableArray<RecipeMaterial> Outputs,
    long CycleDurationMs) : ITypeDefinition;
