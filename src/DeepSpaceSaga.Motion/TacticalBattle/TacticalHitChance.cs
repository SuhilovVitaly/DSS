namespace DeepSpaceSaga.Motion.TacticalBattle;

public readonly record struct TacticalHitRules(double Base, double MinChance, double MaxChance, double RangeBonus,
    double AnglePenalty, double RearBonus, double RearBonusArc, double EvasionPenalty = 0);
public readonly record struct TacticalShotInput(int Accuracy, int TargetManeuver, int WeaponHitBonus,
    double Range, double Arc, double Distance, double Angle, double RearAngle, bool TargetIsDrone,
    int ShieldDefense, int ShieldCount, double RearHalfArc, double? WeaponRangeBonus = null, bool TargetEvading = false);
public readonly record struct TacticalHitTerms(double Accuracy, double Maneuver, double Base, double Weapon,
    double Range, double Angle, double Rear, double Shield, double Raw, double Chance, bool IsRear, bool HasRearBonus, double Evasion = 0);

/// <summary>Deterministic hit terms without RNG, V4 Combat §§6.2–6.3.</summary>
public static class TacticalHitChance
{
    public static double RearAngle(TacticalPose shooter, TacticalPose target) => Math.Abs(TacticalGeometry.Norm(
        TacticalGeometry.Bearing(target.X, target.Y, shooter.X, shooter.Y) - (target.Course + 180.0)));

    public static TacticalHitTerms Terms(TacticalHitRules rules, TacticalShotInput input)
    {
        bool rear = input.TargetIsDrone && input.RearAngle <= input.RearHalfArc;
        bool bonus = input.TargetIsDrone && input.RearAngle <= rules.RearBonusArc;
        double accuracy = input.Accuracy;
        double maneuver = -(double)input.TargetManeuver;
        double weapon = input.WeaponHitBonus;
        double range = (input.WeaponRangeBonus ?? rules.RangeBonus) * PyMath.Max(0, 1.0 - input.Distance / (double)input.Range);
        double angle = -((Math.Abs(input.Angle) / (double)input.Arc) * rules.AnglePenalty);
        double rearBonus = bonus ? rules.RearBonus : 0;
        double shield = input.TargetIsDrone && !rear ? -((double)input.ShieldDefense * input.ShieldCount) : 0;
        double sum = 0;
        sum += accuracy;
        sum += maneuver;
        sum += rules.Base;
        sum += weapon;
        sum += range;
        sum += angle;
        sum += rearBonus;
        sum += shield;
        double evasion = input.TargetIsDrone && input.TargetEvading ? -rules.EvasionPenalty : 0;
        sum += evasion;
        return new(accuracy, maneuver, rules.Base, weapon, range, angle, rearBonus, shield, sum,
            PyMath.Clamp(sum, rules.MinChance, rules.MaxChance) / 100.0, rear, bonus, evasion);
    }
    /// <summary>Probabilities use [0,1]; applied after the normal hit-chance clamp.</summary>
    public static double WithDecoy(double chance, double missFactor, double minChance) =>
        PyMath.Max(minChance, 1.0 - missFactor * (1.0 - chance));

    public static double Chance(TacticalHitRules rules, TacticalShotInput input) => Terms(rules, input).Chance;
}
