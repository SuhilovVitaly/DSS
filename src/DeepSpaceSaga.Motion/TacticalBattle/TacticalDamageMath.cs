namespace DeepSpaceSaga.Motion.TacticalBattle;

public static class TacticalDamageMath
{
    public static int DisabledHull(int maxHull, double fraction) => (int)PyMath.FloorNonNeg(fraction * maxHull);

    public static (int Hull, int Armor) Split(int damage, int piercing, int armor)
    {
        int direct = Math.Min(damage, piercing);
        int remaining = damage - direct;
        int absorbed = Math.Min(Math.Max(armor, 0), remaining);
        return (damage - absorbed, absorbed);
    }
}
