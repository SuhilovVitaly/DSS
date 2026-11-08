using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Engine.TacticalBattle;

internal sealed record TacticalCandidate(TacticalUnit Target, double Distance, double Angle, TacticalHitTerms Terms);
/// <summary>SoloHullLoss is the actual hull loss credited at this sequential shot; LethalImpact belongs to this shot only.</summary>
internal sealed record TacticalShot(TacticalDrone Shooter, TacticalUnit Target, double Distance, double Angle,
    double Chance, bool Hit, int Damage, bool Rear, bool Kill, int SoloHullLoss, bool LethalImpact = false);
internal sealed record TacticalDamage(string TargetId, int Hull, int Armor, bool RearKill);
internal sealed record TacticalSystemChange(string TargetId, string Effect, int? EngineIndex = null);
internal sealed record TacticalSalvoResult(ImmutableArray<TacticalShot> Shots, ImmutableArray<TacticalDamage> Damage,
    ImmutableArray<TacticalSystemChange> Systems)
{
    public ImmutableArray<TacticalSalvoStep> Steps { get; init; } = [];
}
internal sealed record TacticalSalvoStep(int Index, string ShooterId, string? TargetId, TacticalFireSkipReason SkipReason,
    TacticalShot? Shot, TacticalDamage? Damage, ImmutableArray<TacticalSystemChange> Systems,
    ImmutableArray<TacticalUnitSnapshot> UnitsAfter, TacticalBattleResult? Outcome);

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
        // Freeze and validate every plan before draws, Dry or statistics mutate.
        var plans = battle.CaptureFireOrder().Select(shooter =>
        {
            var candidates = Candidates(battle, shooter);
            string? id = targetsFor is null ? DefaultTarget(shooter, candidates)?.Target.Id : targetsFor(shooter, candidates);
            if (id is not null && !candidates.Any(c => c.Target.Id == id))
                throw new ArgumentException("Selected target is not an admissible candidate: " + id, nameof(targetsFor));
            return (Shooter: shooter, TargetId: id, NoCandidates: candidates.IsEmpty);
        }).ToArray();
        foreach (var plan in plans) plan.Shooter.Dry = plan.NoCandidates ? plan.Shooter.Dry + 1 : 0;
        var shots = ImmutableArray.CreateBuilder<TacticalShot>();
        var applications = ImmutableArray.CreateBuilder<TacticalDamage>();
        var allChanges = ImmutableArray.CreateBuilder<TacticalSystemChange>();
        var steps = ImmutableArray.CreateBuilder<TacticalSalvoStep>(plans.Length);
        foreach (var plan in plans)
        {
            var shooter = plan.Shooter;
            var reason = battle.Result is not null ? TacticalFireSkipReason.BattleEnded
                : shooter.Status == TacticalDroneStatus.Destroyed ? TacticalFireSkipReason.ShooterDestroyed
                : shooter.Status == TacticalDroneStatus.Disabled ? TacticalFireSkipReason.ShooterDisabled
                : shooter.Jammed ? TacticalFireSkipReason.WeaponJammed
                : plan.TargetId is null ? plan.NoCandidates ? TacticalFireSkipReason.NoTarget : TacticalFireSkipReason.Withheld
                : TacticalFireSkipReason.None;
            TacticalCandidate? candidate = null;
            if (reason == TacticalFireSkipReason.None)
            {
                candidate = Candidates(battle, shooter).FirstOrDefault(c => c.Target.Id == plan.TargetId);
                if (candidate is null) reason = TacticalFireSkipReason.TargetUnavailable;
            }
            TacticalShot? shot = null;
            TacticalDamage? application = null;
            var changes = ImmutableArray.CreateBuilder<TacticalSystemChange>();
            var priorOutcome = battle.Result;
            if (candidate is not null)
            {
                var target = candidate.Target;
                bool wasActive = target.IsActive;
                bool hit = battle.Random.Random() < candidate.Terms.Chance;
                int damage = hit ? battle.Random.RandInt(shooter.Weapon.DamageMin, shooter.Weapon.DamageMax) : 0;
                bool rear = candidate.Terms.IsRear;
                bool kill = hit && rear && battle.Random.Random() < shooter.Weapon.RearKillChance;
                var split = !hit ? (Hull: 0, Armor: 0) : rear ? (Hull: damage, Armor: 0)
                    : Split(damage, shooter.Weapon.Piercing, target.Armor);
                int actualHull = kill ? target.Hull : Math.Min(split.Hull, target.Hull);
                target.Hull -= actualHull;
                target.Armor -= split.Armor;
                bool lethal = hit && target.Hull <= 0;
                shot = new(shooter, target, candidate.Distance, candidate.Angle, candidate.Terms.Chance,
                    hit, damage, rear, kill, actualHull, lethal);
                shots.Add(shot);
                var weapon = battle.Statistics.ForWeapon(shooter.Weapon.Id);
                battle.Statistics.Shots++; weapon.Shots++;
                if (hit)
                {
                    battle.Statistics.Hits++; weapon.Hits++;
                    if (rear) { battle.Statistics.RearHits++; weapon.RearHits++; }
                    if (kill) { battle.Statistics.RearKills++; weapon.RearKills++; }
                    application = new(target.Id, actualHull, split.Armor, kill);
                    applications.Add(application);
                    battle.Statistics.Hull += actualHull;
                    battle.Statistics.Armor += split.Armor;
                    weapon.Hull += actualHull;
                    if (target is TacticalCarrier) battle.Statistics.CarrierHull += actualHull;
                    if (target is TacticalDrone drone)
                    {
                        if (drone.Hull <= 0) { drone.Status = TacticalDroneStatus.Destroyed; drone.Speed = 0; }
                        else if (drone.Hull <= PyMath.FloorNonNeg(battle.Catalog.Rules.Disabled.HullFraction * drone.MaxHull))
                        {
                            drone.Status = TacticalDroneStatus.Disabled; drone.Speed = 0; battle.Statistics.Disabled++;
                        }
                        if (drone.IsActive && actualHull >= battle.Catalog.Rules.Systems.MinHullLoss)
                        {
                            var systems = battle.Catalog.Rules.Systems;
                            battle.Statistics.Systems++;
                            string effect = battle.Random.Weighted(systems.Table.Select(e => (e.Effect, (double)e.Weight)).ToArray());
                            if (effect == "weaponJammed")
                            {
                                if (!drone.Jammed) { drone.Jammed = true; changes.Add(new(drone.Id, effect)); }
                            }
                            else
                            {
                                var intact = Enumerable.Range(0, drone.Engines.Length)
                                    .Where(i => drone.Engines[i].Factor == TacticalBattleConstants.IntactEngineFactor).ToArray();
                                if (intact.Length > 0)
                                {
                                    int index = battle.Random.Choice(intact);
                                    drone.Engines[index].Factor = systems.EngineDamagedFactor;
                                    changes.Add(new(drone.Id, "engineDamaged", index));
                                }
                            }
                        }
                    }
                    if (wasActive && !target.IsActive) battle.Statistics.FirstLoss ??= target.Side;
                }
            }
            // A completed step includes statistics and system damage before victory is captured.
            var outcome = priorOutcome is null ? battle.CheckVictory() : null;
            allChanges.AddRange(changes);
            steps.Add(new(steps.Count, shooter.Id, plan.TargetId, reason, shot, application, changes.ToImmutable(),
                TacticalBattleSnapshotBuilder.Units(battle), outcome));
        }
        return new(shots.ToImmutable(), applications.ToImmutable(), allChanges.ToImmutable()) { Steps = steps.MoveToImmutable() };
    }

    public static (int Hull, int Armor) Split(int damage, int piercing, int armor)
    {
        int direct = (int)PyMath.Min(damage, piercing);
        int remaining = damage - direct;
        int absorbed = (int)PyMath.Min(PyMath.Max(armor, 0), remaining);
        return (direct + remaining - absorbed, absorbed);
    }
}
