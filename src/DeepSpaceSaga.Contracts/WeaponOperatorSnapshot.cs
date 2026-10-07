namespace DeepSpaceSaga.Contracts;

/// <summary>The independently trained skill used by a weapon's assigned operator.</summary>
public enum WeaponSkillType
{
    TorpedoAttack,
    CountermeasureDefense
}

/// <summary>
/// Authoritative operator identity and skill metadata (0..100). A null operator means
/// no assignment; an assigned zero-skill operator remains an operator. This DTO does
/// not compute weapon performance. BaseRating and EffectiveRating are retained transport
/// fields for the predecessor runtime; EP-0008 consumers use the explicit module-owned
/// accuracy/maneuverability fields, independently of Skill.
/// </summary>
public sealed record WeaponOperatorSnapshot(
    string CrewId,
    string DisplayName,
    WeaponSkillType SkillType,
    int Skill,
    decimal BaseRating,
    decimal EffectiveRating);
