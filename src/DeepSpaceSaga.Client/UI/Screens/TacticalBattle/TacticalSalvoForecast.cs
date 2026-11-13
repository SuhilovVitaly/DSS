using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed record TacticalSalvoForecast(int Shots, double DisableChance, int MinDamage, int MaxDamage)
{
    // Conditional on every assigned shooter reaching its place in the queue. Enemy orders are hidden.
    internal static TacticalSalvoForecast? Calculate(TacticalBattleSnapshot battle, TacticalFirePlan plan)
    {
        if (plan.Selected?.Target is not { } selected ||
            battle.Units.FirstOrDefault(unit => unit.Id == selected.TargetId) is not { } target)
            return null;

        int threshold = 0;
        if (target.Kind == TacticalUnitKind.Drone)
        {
            if (battle.Rules.DisabledHullFraction is not { } fraction) return null;
            threshold = TacticalDamageMath.DisabledHull(target.MaxHull, fraction);
        }

        var shooters = plan.Rows.Where(row => !row.Evade && !row.Drone.Jammed && !row.Drone.InDust &&
                row.Target?.TargetId == target.Id && row.Drone.Build is not null)
            .ToDictionary(row => row.Drone.Id, StringComparer.Ordinal);
        var queue = battle.FireQueue.Where(entry => shooters.ContainsKey(entry.ShooterId))
            .OrderBy(entry => entry.Index).ToArray();
        if (queue.Length == 0) return null;

        // States retain both pools because earlier shots can strip armor before later shots arrive.
        Dictionary<(int Hull, int Armor), double> states = new() { [(target.Hull, target.Armor)] = 1 };
        foreach (var entry in queue)
        {
            var row = shooters[entry.ShooterId];
            var build = row.Drone.Build!;
            var shot = row.Target!;
            double rearKill = shot.Rear ? shot.RearKillChance : 0;
            double damageProbability = shot.Chance * (1 - rearKill) / (build.DamageMax - build.DamageMin + 1);
            Dictionary<(int Hull, int Armor), double> next = new();
            foreach (var (health, probability) in states)
            {
                if (health.Hull <= threshold)
                {
                    Add(health, probability);
                    continue;
                }
                Add(health, probability * (1 - shot.Chance));
                Add((0, health.Armor), probability * shot.Chance * rearKill);
                if (damageProbability <= 0) continue;
                for (int damage = build.DamageMin; damage <= build.DamageMax; damage++)
                {
                    var (hull, armor) = shot.Rear ? (damage, 0) : TacticalDamageMath.Split(damage, build.Piercing, health.Armor);
                    Add((Math.Max(0, health.Hull - hull), health.Armor - armor), probability * damageProbability);
                }
            }
            states = next;

            void Add((int Hull, int Armor) health, double probability)
            {
                if (probability > 0) next[health] = next.GetValueOrDefault(health) + probability;
            }
        }

        double disabled = states.Where(state => state.Key.Hull <= threshold).Sum(state => state.Value);
        int Damage((int Hull, int Armor) health) => target.Hull - health.Hull + target.Armor - health.Armor;
        return new(queue.Length, Math.Clamp(disabled, 0, 1), states.Keys.Min(Damage), states.Keys.Max(Damage));
    }
}
