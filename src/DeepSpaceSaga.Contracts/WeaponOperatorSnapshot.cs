namespace DeepSpaceSaga.Contracts;

/// <summary>The independently trained skill used by a weapon's assigned operator.</summary>
public enum WeaponSkillType
{
    TorpedoAttack,
    CountermeasureDefense
}

/// <summary>
/// Authoritative operator and rating breakdown. Skill is in 0..100; ratings use decimal
/// arithmetic without intermediate rounding (base rating * skill / 50). A null operator
/// means no assignment, whereas a present operator with zero skill has zero rating.
/// </summary>
public sealed record WeaponOperatorSnapshot(
    string CrewId,
    string DisplayName,
    WeaponSkillType SkillType,
    int Skill,
    decimal BaseRating,
    decimal EffectiveRating);
