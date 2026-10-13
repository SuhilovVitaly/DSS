using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Combat;

internal static class CombatChanceMath
{
    internal static readonly CombatChanceRules DefaultRules = new(50, 0, 100);

    internal static int ChanceTenths(decimal attack, decimal defense, CombatChanceRules rules)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(attack);
        ArgumentOutOfRangeException.ThrowIfNegative(defense);
        ArgumentNullException.ThrowIfNull(rules);
        if (rules.Min is < 0 or > 100 || rules.Max is < 0 or > 100 || rules.Min > rules.Max ||
            rules.Base is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(rules));

        decimal chance;
        if (attack >= defense)
        {
            decimal difference = attack - defense;
            chance = difference >= 100 ? rules.Max : Math.Min(rules.Max, rules.Base + difference);
        }
        else
        {
            decimal difference = defense - attack;
            chance = difference >= 100 ? rules.Min : Math.Max(rules.Min, rules.Base - difference);
        }
        return (int)decimal.Round(Math.Clamp(chance, rules.Min, rules.Max) * 10,
            0, MidpointRounding.AwayFromZero);
    }
}
