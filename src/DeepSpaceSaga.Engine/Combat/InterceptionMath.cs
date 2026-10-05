namespace DeepSpaceSaga.Engine.Combat;

public static class InterceptionMath
{
    public static int ChanceTenths(decimal defenseRating, decimal torpedoRating)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(defenseRating);
        ArgumentOutOfRangeException.ThrowIfNegative(torpedoRating);
        decimal difference = defenseRating - torpedoRating;
        if (difference <= -50) return 0;
        if (difference >= 50) return 1000;
        return (int)decimal.Round((50 + difference) * 10, 0, MidpointRounding.AwayFromZero);
    }
}
