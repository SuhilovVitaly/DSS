using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Engine.Content;

internal sealed record CombatRulesDefinition(
    [property: JsonPropertyName("chance")] CombatChanceRules? Chance,
    [property: JsonPropertyName("damage"), JsonRequired] CombatDamageRules Damage,
    [property: JsonPropertyName("shipDestruction"), JsonRequired] ShipDestructionRules ShipDestruction,
    [property: JsonPropertyName("armorClasses"), JsonRequired] IReadOnlyDictionary<string, int> ArmorClasses,
    [property: JsonPropertyName("blast")] CombatBlastRules? Blast = null,
    [property: JsonPropertyName("scatter")] CombatScatterRules? Scatter = null,
    [property: JsonPropertyName("npc")] CombatNpcRules? Npc = null,
    [property: JsonPropertyName("combatStartRangeKm")] double? CombatStartRangeKm = null)
{
    [JsonIgnore]
    internal bool IsPointAim => Blast is not null || Scatter is not null || Npc is not null || CombatStartRangeKm.HasValue;
}

internal sealed record CombatBlastRules(
    [property: JsonPropertyName("coreFraction"), JsonRequired] double CoreFraction);

internal sealed record CombatScatterRules(
    [property: JsonPropertyName("skillPivot"), JsonRequired] int SkillPivot,
    [property: JsonPropertyName("skillSlope"), JsonRequired] double SkillSlope,
    [property: JsonPropertyName("minMultiplier"), JsonRequired] double MinMultiplier);

internal sealed record CombatNpcRules(
    [property: JsonPropertyName("reactionBaseMs"), JsonRequired] long ReactionBaseMs,
    [property: JsonPropertyName("reactionPerSkillMs"), JsonRequired] long ReactionPerSkillMs,
    [property: JsonPropertyName("reactionMinMs"), JsonRequired] long ReactionMinMs,
    [property: JsonPropertyName("dodgeChance")] double DodgeChance = 0.1);

internal sealed record CombatChanceRules(
    [property: JsonPropertyName("base"), JsonRequired] decimal Base,
    [property: JsonPropertyName("min"), JsonRequired] decimal Min,
    [property: JsonPropertyName("max"), JsonRequired] decimal Max);

internal sealed record CombatDamageRules(
    [property: JsonPropertyName("hitDisableMs"), JsonRequired] long HitDisableMs,
    [property: JsonPropertyName("hitLocationWeighting"), JsonRequired] string HitLocationWeighting);

internal sealed record ShipDestructionRules(
    [property: JsonPropertyName("criticalRoles"), JsonRequired] ImmutableArray<string> CriticalRoles,
    [property: JsonPropertyName("destroyedModuleFraction"), JsonRequired] decimal DestroyedModuleFraction);

internal sealed record FireModeDefinition(
    [property: JsonPropertyName("modeId"), JsonRequired] string ModeId,
    [property: JsonPropertyName("displayName"), JsonRequired] string DisplayName,
    [property: JsonPropertyName("accuracyBonus"), JsonRequired] decimal AccuracyBonus,
    [property: JsonPropertyName("maneuverabilityBonus"), JsonRequired] decimal ManeuverabilityBonus,
    [property: JsonPropertyName("breakdownChancePercent"), JsonRequired] decimal BreakdownChancePercent,
    [property: JsonPropertyName("reloadMultiplier"), JsonRequired] decimal ReloadMultiplier,
    [property: JsonPropertyName("appliesTo"), JsonRequired] ImmutableArray<string> AppliesTo);

internal sealed record FireModesFile(
    [property: JsonPropertyName("fireModes"), JsonRequired] ImmutableArray<FireModeDefinition> FireModes);

internal static class CombatContentValidation
{
    internal static CombatRulesDefinition Rules(CombatRulesDefinition? rules)
    {
        if (rules is null) throw new ContentException("combatRules: missing root.");
        ValidatePointAimRules(rules);
        if (rules.Damage is null || rules.Damage.HitDisableMs < 0)
            throw new ContentException("combatRules.damage.hitDisableMs must be nonnegative.");
        if (rules.Damage.HitLocationWeighting != "mass")
            throw new ContentException("combatRules.damage.hitLocationWeighting must be mass.");
        if (rules.ShipDestruction is null || rules.ShipDestruction.DestroyedModuleFraction is <= 0 or > 1)
            throw new ContentException("combatRules.shipDestruction.destroyedModuleFraction outside (0,1].");
        var roles = rules.ShipDestruction.CriticalRoles;
        if (roles.IsDefaultOrEmpty || roles.Distinct(StringComparer.Ordinal).Count() != roles.Length ||
            roles.Any(role => role is not ("bridge" or "engine")))
            throw new ContentException("combatRules.shipDestruction.criticalRoles contains unknown/duplicate role.");
        if (rules.ArmorClasses is null || !rules.ArmorClasses.TryGetValue("none", out int noArmor) || noArmor != 0 ||
            rules.ArmorClasses.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value < 0))
            throw new ContentException("combatRules.armorClasses contains missing/nonzero none or negative class.");
        return rules;
    }

    private static void ValidatePointAimRules(CombatRulesDefinition rules)
    {
        if (rules.Chance is not null)
            throw new ContentException("combatRules.chance is not supported with point-aim rules.");
        if (rules.Blast is null || !double.IsFinite(rules.Blast.CoreFraction) || rules.Blast.CoreFraction is <= 0 or >= 1)
            throw new ContentException("combatRules.blast.coreFraction must be finite and inside (0,1).");
        if (rules.Scatter is null)
            throw new ContentException("combatRules.scatter is required.");
        if (rules.Scatter.SkillPivot is < 0 or > 30)
            throw new ContentException("combatRules.scatter.skillPivot must be inside [0,30].");
        if (!double.IsFinite(rules.Scatter.SkillSlope) || rules.Scatter.SkillSlope < 0)
            throw new ContentException("combatRules.scatter.skillSlope must be finite and nonnegative.");
        if (!double.IsFinite(rules.Scatter.MinMultiplier) || rules.Scatter.MinMultiplier is <= 0 or > 1)
            throw new ContentException("combatRules.scatter.minMultiplier must be finite and inside (0,1].");
        if (rules.Npc is null)
            throw new ContentException("combatRules.npc is required.");
        if (rules.Npc.ReactionBaseMs <= 0)
            throw new ContentException("combatRules.npc.reactionBaseMs must be positive.");
        if (rules.Npc.ReactionPerSkillMs < 0 || rules.Npc.ReactionPerSkillMs > long.MaxValue / 30)
            throw new ContentException("combatRules.npc.reactionPerSkillMs must be nonnegative and safe for skill 30.");
        if (rules.Npc.ReactionMinMs < 0 || rules.Npc.ReactionMinMs > rules.Npc.ReactionBaseMs)
            throw new ContentException("combatRules.npc.reactionMinMs must be between zero and reactionBaseMs.");
        if (!double.IsFinite(rules.Npc.DodgeChance) || rules.Npc.DodgeChance is < 0 or > 1 ||
            Math.Abs(rules.Npc.DodgeChance * 1_000_000 - Math.Round(rules.Npc.DodgeChance * 1_000_000)) > 1e-8)
            throw new ContentException("combatRules.npc.dodgeChance must be in [0,1] with at most six decimal places.");
        if (rules.CombatStartRangeKm is not > 0 || !double.IsFinite(rules.CombatStartRangeKm.Value))
            throw new ContentException("combatRules.combatStartRangeKm must be positive and finite.");
    }

    internal static ImmutableArray<FireModeDefinition> Modes(FireModesFile? file) =>
        throw new ContentException("fireModes is retired; point launchers use captured profiles.");
}
