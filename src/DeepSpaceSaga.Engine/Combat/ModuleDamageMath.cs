namespace DeepSpaceSaga.Engine.Combat;

internal readonly record struct ModuleDamageResult(int ArmorAfter, int StructureAfter, int ArmorLost, int StructureLost);

internal static class ModuleDamageMath
{
    internal static ModuleDamageResult Apply(int damage, decimal armorPiercingPercent, decimal armorDamagePercent,
        int armor, int structure)
    {
        if (damage < 0 || armorPiercingPercent is < 0 or > 100 || armorDamagePercent <= 0 || armor < 0 || structure < 0)
            throw new ArgumentOutOfRangeException(nameof(damage));

        decimal direct = damage * armorPiercingPercent / 100m;
        decimal rest = damage - direct;
        decimal armorFactor = armorDamagePercent / 100m;
        decimal absorbed = Math.Min(armor, rest * armorFactor);
        decimal overflow = rest - absorbed / armorFactor;
        int armorLost = Math.Min(armor, decimal.ToInt32(Math.Round(absorbed, MidpointRounding.AwayFromZero)));
        int structureLost = Math.Min(structure,
            decimal.ToInt32(Math.Round(direct + overflow, MidpointRounding.AwayFromZero)));
        return new(armor - armorLost, structure - structureLost, armorLost, structureLost);
    }
}
