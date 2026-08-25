using System.Collections.Immutable;

namespace DeepSpaceSaga.Engine.Content;

/// <summary>
/// One input or output material of a recipe: an item type and an integer quantity.
/// Pure type data — never copied into runtime state; the registry owns definitions.
/// </summary>
/// <param name="NeedCoefficient">
/// Fixed-point (1000 == 1.0x) need coefficient for this material (requirements §59
/// "Производящие модули станции": "числовой коэффициент потребности производящего модуля
/// сохраняется в данных и должен быть доступен будущей производственной системе"). Only
/// meaningful on Inputs — the requirement defines it as a per-input "need" coefficient;
/// Outputs simply carry the neutral default (1000) unread by anything today. The exact
/// formula for how this coefficient influences consumption volume/price is an explicit open
/// balance decision (§59 "Открытые решения") deferred to a future production-system unit —
/// this record only stores and validates the value.
/// </param>
internal sealed record RecipeMaterial(string ItemTypeId, long Count, int NeedCoefficient = 1000);

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
