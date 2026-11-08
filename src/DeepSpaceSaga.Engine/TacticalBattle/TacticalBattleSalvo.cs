using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Engine.TacticalBattle;

internal sealed record TacticalCandidate(TacticalUnit Target, double Distance, double Angle, TacticalHitTerms Terms);
internal sealed record TacticalFirePlan(string? TargetId, bool Evade, bool Decoy = false);
/// <summary>SoloHullLoss is the actual hull loss credited at this sequential shot; LethalImpact belongs to this shot only.</summary>
internal sealed record TacticalShot(TacticalDrone Shooter, TacticalUnit Target, double Distance, double Angle,
    double Chance, bool Hit, int Damage, bool Rear, bool Kill, int SoloHullLoss, bool LethalImpact = false)
{
    public bool TargetEvading { get; init; }
    public bool Decoyed { get; init; }
    public double? ChanceWithoutDecoy { get; init; }
}
internal sealed record TacticalDamage(string TargetId, int Hull, int Armor, bool RearKill);
internal sealed record TacticalSystemChange(string TargetId, string Effect, int? EngineIndex = null);
internal sealed record TacticalSalvoResult(ImmutableArray<TacticalShot> Shots, ImmutableArray<TacticalDamage> Damage,
    ImmutableArray<TacticalSystemChange> Systems)
{
    public ImmutableArray<string> DecoysLaunched { get; init; } = [];
    public ImmutableArray<TacticalSalvoStep> Steps { get; init; } = [];
}
internal sealed record TacticalSalvoStep(int Index, string ShooterId, string? TargetId, TacticalFireSkipReason SkipReason,
    TacticalShot? Shot, TacticalDamage? Damage, ImmutableArray<TacticalSystemChange> Systems,
    ImmutableArray<TacticalUnitSnapshot> UnitsAfter, TacticalBattleResult? Outcome);

internal static class TacticalBattleSalvo
{
    public static ImmutableArray<TacticalCandidate> Candidates(TacticalBattle battle, TacticalDrone shooter, IReadOnlySet<string>? evading = null)
    {
        if (!shooter.IsActive || shooter.Jammed) return [];
        var result = ImmutableArray.CreateBuilder<TacticalCandidate>();
        string enemy = battle.EnemySide(shooter.Side);
        var tuning = battle.Catalog.Rules;
        var hit = tuning.Hit;
        var rules = new TacticalHitRules(hit.Base, hit.MinChance, hit.MaxChance, hit.RangeBonus, hit.AnglePenalty, hit.RearBonus, hit.RearBonusArcDeg, tuning.Evasion.HitPenalty);
        foreach (var target in battle.Drones.Cast<TacticalUnit>().Concat(battle.Carriers))
        {
            if (target.Side != enemy || !target.IsActive) continue;
            double distance = TacticalGeometry.Dist(shooter.Pose.X, shooter.Pose.Y, target.Pose.X, target.Pose.Y);
            double angle = TacticalGeometry.Norm(TacticalGeometry.Bearing(shooter.Pose.X, shooter.Pose.Y, target.Pose.X, target.Pose.Y) - shooter.Pose.Course);
            if (distance > shooter.Weapon.Range || Math.Abs(angle) > shooter.Weapon.ArcDeg) continue;
            var drone = target as TacticalDrone;
            var input = new TacticalShotInput(shooter.Accuracy, target.Maneuver, shooter.Weapon.HitBonus,
                shooter.Weapon.Range, shooter.Weapon.ArcDeg, distance, angle, TacticalHitChance.RearAngle(shooter.Pose, target.Pose),
                drone is not null, drone?.Shield?.Defense ?? 0, drone?.ShieldCount ?? 0, tuning.Rear.ArcHalfDeg, shooter.Weapon.RangeBonus,
                evading?.Contains(target.Id) == true);
            result.Add(new(target, distance, angle, TacticalHitChance.Terms(rules, input)));
        }
        return result.ToImmutable();
    }

    public static TacticalCandidate? DefaultTarget(TacticalDrone shooter, IReadOnlyList<TacticalCandidate> candidates) => candidates
        .OrderBy(c => (shooter.Role == "striker") == (c.Target is TacticalCarrier) ? 0 : 1).ThenBy(c => c.Distance).FirstOrDefault();

    public static TacticalFirePlan DefaultPlan(TacticalBattle battle, TacticalDrone drone, IReadOnlyList<TacticalCandidate> candidates)
    {
        var rules = battle.Catalog.Rules.Evasion;
        return drone.Hull < rules.AiHullFraction * drone.MaxHull && drone.Consciousness >= rules.MinConsciousness
            ? new(null, true) : new(DefaultTarget(drone, candidates)?.Target.Id, false);
    }

    public static IReadOnlyDictionary<string, TacticalFirePlan> AiDecoys(TacticalBattle battle,
        IReadOnlyDictionary<string, TacticalFirePlan> frozen, Func<TacticalDrone, bool> aiFor)
    {
        var result = frozen.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
        var evading = frozen.Where(p => p.Value.Evade).Select(p => p.Key).ToHashSet(StringComparer.Ordinal);
        foreach (var drone in battle.CaptureFireOrder())
        {
            if (!aiFor(drone) || !frozen.TryGetValue(drone.Id, out var own) || own.Evade
                || drone.Decoy is null || drone.DecoyCharges < 1) continue;
            bool threat = battle.CaptureFireOrder().Any(shooter => shooter.Side != drone.Side
                && frozen.TryGetValue(shooter.Id, out var enemy) && !enemy.Evade && enemy.TargetId == drone.Id
                && Candidates(battle, shooter, evading).Any(c => c.Target.Id == drone.Id
                    && c.Terms.Chance >= battle.Catalog.Rules.Decoy.AiThreatChance));
            if (threat) result[drone.Id] = own with { Decoy = true };
        }
        return result;
    }

    public static TacticalSalvoResult Fire(TacticalBattle battle,
        Func<TacticalDrone, IReadOnlyList<TacticalCandidate>, TacticalFirePlan>? targetsFor = null,
        Func<TacticalDrone, bool>? aiDecoysFor = null)
    {
        // Freeze and validate every plan before draws, Dry or statistics mutate.
        var plans = battle.CaptureFireOrder().Select(shooter =>
        {
            var candidates = Candidates(battle, shooter);
            var plan = targetsFor is null ? DefaultPlan(battle, shooter, candidates) : targetsFor(shooter, candidates);
            if (plan is null) throw new ArgumentException("Fire plan must not be null.", nameof(targetsFor));
            string? id = plan.TargetId;
            if (plan.Evade && (id is not null || shooter.Consciousness < battle.Catalog.Rules.Evasion.MinConsciousness))
                throw new ArgumentException("Evasion requires no target and sufficient consciousness.", nameof(targetsFor));
            if (plan.Decoy && (plan.Evade || shooter.Decoy is null || shooter.DecoyCharges < 1))
                throw new ArgumentException("Decoy requires a charged module and no evasion.", nameof(targetsFor));
            if (id is not null && !candidates.Any(c => c.Target.Id == id))
                throw new ArgumentException("Selected target is not an admissible candidate: " + id, nameof(targetsFor));
            return (Shooter: shooter, TargetId: id, plan.Evade, plan.Decoy, NoCandidates: candidates.IsEmpty);
        }).ToArray();
        if (targetsFor is null || aiDecoysFor is not null)
        {
            var selected = AiDecoys(battle, plans.ToDictionary(p => p.Shooter.Id,
                p => new TacticalFirePlan(p.TargetId, p.Evade, p.Decoy), StringComparer.Ordinal), aiDecoysFor ?? (_ => true));
            for (int i = 0; i < plans.Length; i++) plans[i].Decoy = selected[plans[i].Shooter.Id].Decoy;
        }
        var launched = plans.Where(p => p.Decoy).Select(p => p.Shooter.Id).ToImmutableArray();
        var lured = launched.ToHashSet(StringComparer.Ordinal);
        foreach (var plan in plans.Where(p => p.Decoy))
        {
            plan.Shooter.DecoyCharges--;
            battle.Statistics.DecoysLaunched++;
        }
        var evading = plans.Where(p => p.Evade).Select(p => p.Shooter.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var plan in plans.Where(p => p.Evade))
        {
            plan.Shooter.Consciousness -= battle.Catalog.Rules.Evasion.Cost;
            battle.Statistics.Evasions++;
        }
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
                : plan.Evade ? TacticalFireSkipReason.Evading
                : shooter.Jammed ? TacticalFireSkipReason.WeaponJammed
                : plan.TargetId is null ? plan.NoCandidates ? TacticalFireSkipReason.NoTarget : TacticalFireSkipReason.Withheld
                : TacticalFireSkipReason.None;
            TacticalCandidate? candidate = null;
            if (reason == TacticalFireSkipReason.None)
            {
                candidate = Candidates(battle, shooter, evading).FirstOrDefault(c => c.Target.Id == plan.TargetId);
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
                bool decoyed = lured.Remove(target.Id);
                double chance = decoyed ? TacticalHitChance.WithDecoy(candidate.Terms.Chance,
                    ((TacticalDrone)target).Decoy!.MissFactor, battle.Catalog.Rules.Hit.MinChance / 100.0) : candidate.Terms.Chance;
                bool hit = battle.Random.Random() < chance;
                int damage = hit ? battle.Random.RandInt(shooter.Weapon.DamageMin, shooter.Weapon.DamageMax) : 0;
                bool rear = candidate.Terms.IsRear;
                bool kill = hit && rear && battle.Random.Random() < shooter.Weapon.RearKillChance;
                var split = !hit ? (Hull: 0, Armor: 0) : rear ? (Hull: damage, Armor: 0)
                    : Split(damage, shooter.Weapon.Piercing, target.Armor);
                int actualHull = kill ? target.Hull : Math.Min(split.Hull, target.Hull);
                target.Hull -= actualHull;
                target.Armor -= split.Armor;
                bool lethal = hit && target.Hull <= 0;
                shot = new(shooter, target, candidate.Distance, candidate.Angle, chance,
                    hit, damage, rear, kill, actualHull, lethal)
                {
                    TargetEvading = evading.Contains(target.Id),
                    Decoyed = decoyed,
                    ChanceWithoutDecoy = decoyed ? candidate.Terms.Chance : null
                };
                if (decoyed)
                {
                    battle.Statistics.ShotsDecoyed++;
                    battle.Statistics.DecoyChanceBefore += candidate.Terms.Chance;
                    battle.Statistics.DecoyChanceAfter += chance;
                    if (hit) battle.Statistics.HitsDecoyed++;
                }
                if (shot.TargetEvading)
                {
                    battle.Statistics.ShotsAtEvading++;
                    if (hit) battle.Statistics.HitsOnEvading++;
                }
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
        return new(shots.ToImmutable(), applications.ToImmutable(), allChanges.ToImmutable()) { Steps = steps.MoveToImmutable(), DecoysLaunched = launched };
    }

    public static (int Hull, int Armor) Split(int damage, int piercing, int armor)
    {
        int direct = (int)PyMath.Min(damage, piercing);
        int remaining = damage - direct;
        int absorbed = (int)PyMath.Min(PyMath.Max(armor, 0), remaining);
        return (direct + remaining - absorbed, absorbed);
    }
}
