using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

// Deliberately limited hand-case oracle: guaranteed non-rear hit/miss, zero piercing,
// fixed damage, optional guaranteed weapon jam. No Engine Salvo/CheckVictory/math calls.
internal static class InitiativeReferenceOracle
{
    internal sealed record State(string Id, int Hull, int Armor, bool Jammed);
    internal sealed record Step(string ShooterId, TacticalFireSkipReason Reason, bool Fired, State[] Units);
    internal sealed record Result(int Calls, int[] Hull, Step[] Steps);

    public static Result Resolve(InitiativeReferenceCase input)
    {
        var units = input.Units.ToDictionary(u => u.Id, u => new State(u.Id, u.Hull, u.Armor, false), StringComparer.Ordinal);
        bool Active(string id) => units[id].Hull > input.Units.Single(u => u.Id == id).Hull / 5;
        bool ended = false;
        int calls = 0;
        var steps = new List<Step>();
        foreach (var unit in input.Units.OrderByDescending(u => u.Initiative))
        {
            var shooter = units[unit.Id];
            var reason = ended ? TacticalFireSkipReason.BattleEnded
                : shooter.Hull == 0 ? TacticalFireSkipReason.ShooterDestroyed
                : !Active(unit.Id) ? TacticalFireSkipReason.ShooterDisabled
                : shooter.Jammed ? TacticalFireSkipReason.WeaponJammed
                : unit.TargetId is null ? TacticalFireSkipReason.Withheld
                : !Active(unit.TargetId) ? TacticalFireSkipReason.TargetUnavailable : TacticalFireSkipReason.None;
            bool fired = reason == TacticalFireSkipReason.None;
            if (fired)
            {
                calls++;
                if (input.Hit)
                {
                    calls++;
                    var target = units[unit.TargetId!];
                    int absorbed = Math.Min(input.Damage, target.Armor);
                    int hull = Math.Min(target.Hull, input.Damage - absorbed);
                    target = target with { Hull = target.Hull - hull, Armor = target.Armor - absorbed };
                    units[target.Id] = target;
                    if (input.Jam && hull >= 1 && Active(target.Id))
                    {
                        calls++;
                        units[target.Id] = target with { Jammed = true };
                    }
                }
            }
            ended = new[] { "A", "B" }.Any(side => !input.Units.Any(u => u.Side == side && Active(u.Id)));
            steps.Add(new(unit.Id, reason, fired, input.Units.Select(u => units[u.Id]).ToArray()));
        }
        return new(calls, input.Units.Select(u => units[u.Id].Hull).ToArray(), steps.ToArray());
    }
}
