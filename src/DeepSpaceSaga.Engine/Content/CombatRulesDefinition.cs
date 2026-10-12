using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Engine.Content;

internal sealed record CombatRulesDefinition(
    [property: JsonPropertyName("chance"), JsonRequired] CombatChanceRules Chance,
    [property: JsonPropertyName("damage"), JsonRequired] CombatDamageRules Damage,
    [property: JsonPropertyName("shipDestruction"), JsonRequired] ShipDestructionRules ShipDestruction,
    [property: JsonPropertyName("armorClasses"), JsonRequired] IReadOnlyDictionary<string, int> ArmorClasses);

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
        if (rules.Chance is null || rules.Chance.Base is < 0 or > 100 ||
            rules.Chance.Min is < 0 or > 100 || rules.Chance.Max is < 0 or > 100 ||
            rules.Chance.Min > rules.Chance.Max)
            throw new ContentException("combatRules.chance: base/min/max invalid.");
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

    internal static ImmutableArray<FireModeDefinition> Modes(FireModesFile? file)
    {
        if (file is null || file.FireModes.IsDefaultOrEmpty || file.FireModes.Any(mode => mode is null))
            throw new ContentException("fireModes must be a nonempty array.");
        var modes = file.FireModes;
        if (modes.Select(mode => mode.ModeId).Distinct(StringComparer.Ordinal).Count() != modes.Length ||
            !modes.Any(mode => mode.ModeId == "standard"))
            throw new ContentException("fireModes.modeId must be unique and include standard.");
        foreach (var mode in modes)
        {
            if (string.IsNullOrWhiteSpace(mode.ModeId) || string.IsNullOrWhiteSpace(mode.DisplayName) ||
                mode.BreakdownChancePercent is < 0 or > 100 || mode.ReloadMultiplier <= 0 ||
                mode.AppliesTo.IsDefaultOrEmpty ||
                mode.AppliesTo.Distinct(StringComparer.Ordinal).Count() != mode.AppliesTo.Length ||
                mode.AppliesTo.Any(category => category is not ("module.torpedo.launcher" or "module.countermeasure.launcher")) ||
                (mode.ModeId == "standard" && (!mode.AppliesTo.Contains("module.torpedo.launcher") ||
                    !mode.AppliesTo.Contains("module.countermeasure.launcher"))) ||
                (mode.ModeId != "standard" && mode.AppliesTo.Contains("module.countermeasure.launcher")))
                throw new ContentException($"fireModes.{mode.ModeId}: invalid mode or appliesTo category.");
        }
        return modes;
    }
}
