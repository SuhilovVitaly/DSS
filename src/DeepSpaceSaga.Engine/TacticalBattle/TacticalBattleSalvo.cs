using System.Collections.Immutable;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Engine.TacticalBattle;

internal sealed record TacticalCandidate(TacticalUnit Target, double Distance, double Angle, TacticalHitTerms Terms);
internal sealed record TacticalShot(TacticalDrone Shooter, TacticalUnit Target, double Distance, double Angle,
    double Chance, bool Hit, int Damage, bool Rear, bool Kill, int SoloHullLoss);
internal sealed record TacticalDamage(string TargetId, int Hull, int Armor, bool RearKill);
internal sealed record TacticalSystemChange(string TargetId, string Effect, int? EngineIndex = null);
internal sealed record TacticalSalvoResult(ImmutableArray<TacticalShot> Shots, ImmutableArray<TacticalDamage> Damage,
    ImmutableArray<TacticalSystemChange> Systems);

internal static class TacticalBattleSalvo
{
    public static ImmutableArray<TacticalCandidate> Candidates(TacticalBattle battle, TacticalDrone shooter)
    {
        if (!shooter.IsActive || shooter.Jammed) return [];
        var result = ImmutableArray.CreateBuilder<TacticalCandidate>();
        string enemy = battle.EnemySide(shooter.Side);
        var tuning = battle.Catalog.Rules;
        var hit = tuning.Hit;
        var rules = new TacticalHitRules(hit.Base, hit.MinChance, hit.MaxChance, hit.RangeBonus, hit.AnglePenalty, hit.RearBonus, hit.RearBonusArcDeg);
        foreach (var target in battle.Drones.Cast<TacticalUnit>().Concat(battle.Carriers))
        {
            if (target.Side != enemy || !target.IsActive) continue;
            double distance = TacticalGeometry.Dist(shooter.Pose.X, shooter.Pose.Y, target.Pose.X, target.Pose.Y);
            double angle = TacticalGeometry.Norm(TacticalGeometry.Bearing(shooter.Pose.X, shooter.Pose.Y, target.Pose.X, target.Pose.Y) - shooter.Pose.Course);
            if (distance > shooter.Weapon.Range || Math.Abs(angle) > shooter.Weapon.ArcDeg) continue;
            var drone = target as TacticalDrone;
            var input = new TacticalShotInput(shooter.Accuracy, target.Maneuver, shooter.Weapon.HitBonus,
                shooter.Weapon.Range, shooter.Weapon.ArcDeg, distance, angle, TacticalHitChance.RearAngle(shooter.Pose, target.Pose),
                drone is not null, drone?.Shield?.Defense ?? 0, drone?.ShieldCount ?? 0, tuning.Rear.ArcHalfDeg, shooter.Weapon.RangeBonus);
            result.Add(new(target, distance, angle, TacticalHitChance.Terms(rules, input)));
        }
        return result.ToImmutable();
    }

    public static TacticalCandidate? DefaultTarget(TacticalDrone shooter, IReadOnlyList<TacticalCandidate> candidates) => candidates
        .OrderBy(c => (shooter.Role == "striker") == (c.Target is TacticalCarrier) ? 0 : 1).ThenBy(c => c.Distance).FirstOrDefault();

    public static TacticalSalvoResult Fire(TacticalBattle battle, Func<TacticalDrone, IReadOnlyList<TacticalCandidate>, string?>? targetsFor = null)
    {
        var plans = new List<(TacticalDrone Shooter, ImmutableArray<TacticalCandidate> Candidates, TacticalCandidate? Selected)>();
        var previouslyActive = battle.Drones.Cast<TacticalUnit>().Concat(battle.Carriers).Where(u => u.IsActive).ToArray();
        foreach (var shooter in battle.Drones)
        {
            if (!shooter.IsActive) continue;
            var candidates = Candidates(battle, shooter);
            string? id = targetsFor is null ? DefaultTarget(shooter, candidates)?.Target.Id : targetsFor(shooter, candidates);
            var selected = id is null ? null : candidates.FirstOrDefault(c => c.Target.Id == id);
            if (id is not null && selected is null) throw new ArgumentException("Selected target is not an admissible candidate: " + id, nameof(targetsFor));
            plans.Add((shooter, candidates, selected));
        }
        foreach (var plan in plans) plan.Shooter.Dry = plan.Candidates.IsEmpty ? plan.Shooter.Dry + 1 : 0;
        var shots = ImmutableArray.CreateBuilder<TacticalShot>();
        foreach (var plan in plans)
        {
            if (plan.Selected is not { } candidate) continue;
            var shooter = plan.Shooter;
            bool hit = battle.Random.Random() < candidate.Terms.Chance;
            int damage = hit ? battle.Random.RandInt(shooter.Weapon.DamageMin, shooter.Weapon.DamageMax) : 0;
            bool rear = candidate.Terms.IsRear;
            bool kill = hit && rear && battle.Random.Random() < shooter.Weapon.RearKillChance;
            int soloHull = hit ? rear ? damage : Split(damage, shooter.Weapon.Piercing, candidate.Target.Armor).Hull : 0;
            var shot = new TacticalShot(shooter, candidate.Target, candidate.Distance, candidate.Angle, candidate.Terms.Chance, hit, damage, rear, kill, soloHull);
            shots.Add(shot);
            var weapon = battle.Statistics.ForWeapon(shooter.Weapon.Id);
            battle.Statistics.Shots++;
            weapon.Shots++;
            if (!hit) continue;
            battle.Statistics.Hits++;
            weapon.Hits++;
            if (rear) { battle.Statistics.RearHits++; weapon.RearHits++; }
            if (kill) { battle.Statistics.RearKills++; weapon.RearKills++; }
        }
        var applications = ImmutableArray.CreateBuilder<TacticalDamage>();
        foreach (var target in PyMath.OrderedUnique(shots.Where(s => s.Hit).Select(s => s.Target)))
        {
            var hits = shots.Where(s => s.Hit && ReferenceEquals(s.Target, target)).ToArray();
            long hullLoss = 0;
            int armorLoss = 0;
            foreach (var shot in hits)
            {
                if (shot.Rear) hullLoss += shot.Damage;
                else
                {
                    var split = Split(shot.Damage, shot.Shooter.Weapon.Piercing, target.Armor - armorLoss);
                    hullLoss += split.Hull;
                    armorLoss += split.Armor;
                }
            }
            bool rearKill = hits.Any(s => s.Kill);
            if (rearKill) hullLoss = target.Hull;
            int actualHull = (int)PyMath.Min(hullLoss, target.Hull);
            target.Hull -= actualHull;
            target.Armor -= armorLoss;
            applications.Add(new(target.Id, actualHull, armorLoss, rearKill));
            battle.Statistics.Hull += actualHull;
            battle.Statistics.Armor += armorLoss;
            if (target is TacticalCarrier) battle.Statistics.CarrierHull += actualHull;
            foreach (var shot in hits)
                battle.Statistics.ForWeapon(shot.Shooter.Weapon.Id).Hull += shot.Kill ? actualHull : (int)PyMath.Min(shot.SoloHullLoss, actualHull);
        }
        foreach (var drone in battle.Drones)
        {
            if (!drone.IsActive) continue;
            if (drone.Hull <= 0) { drone.Status = TacticalDroneStatus.Destroyed; drone.Speed = 0; }
            else if (drone.Hull <= PyMath.FloorNonNeg(battle.Catalog.Rules.Disabled.HullFraction * drone.MaxHull))
            {
                drone.Status = TacticalDroneStatus.Disabled;
                drone.Speed = 0;
                battle.Statistics.Disabled++;
            }
        }
        battle.Statistics.FirstLoss ??= previouslyActive.FirstOrDefault(u => !u.IsActive)?.Side;
        var changes = ImmutableArray.CreateBuilder<TacticalSystemChange>();
        foreach (var shot in shots)
        {
            if (!shot.Hit || shot.Target is not TacticalDrone target || !target.IsActive || shot.SoloHullLoss < battle.Catalog.Rules.Systems.MinHullLoss) continue;
            var systems = battle.Catalog.Rules.Systems;
            battle.Statistics.Systems++;
            string effect = battle.Random.Weighted(systems.Table.Select(e => (e.Effect, (double)e.Weight)).ToArray());
            if (effect == "weaponJammed")
            {
                if (!target.Jammed) { target.Jammed = true; changes.Add(new(target.Id, effect)); }
            }
            else
            {
                var intact = Enumerable.Range(0, target.Engines.Length).Where(i => target.Engines[i].Factor == TacticalBattleConstants.IntactEngineFactor).ToArray();
                if (intact.Length == 0) continue;
                int index = battle.Random.Choice(intact);
                target.Engines[index].Factor = systems.EngineDamagedFactor;
                changes.Add(new(target.Id, "engineDamaged", index));
            }
        }
        return new(shots.ToImmutable(), applications.ToImmutable(), changes.ToImmutable());
    }

    public static (int Hull, int Armor) Split(int damage, int piercing, int armor)
    {
        int direct = (int)PyMath.Min(damage, piercing);
        int remaining = damage - direct;
        int absorbed = (int)PyMath.Min(PyMath.Max(armor, 0), remaining);
        return (direct + remaining - absorbed, absorbed);
    }
}
