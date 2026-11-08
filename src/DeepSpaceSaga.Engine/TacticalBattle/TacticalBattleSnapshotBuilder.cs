using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Engine.TacticalBattle;

internal static class TacticalBattleSnapshotBuilder
{
    public static ImmutableArray<TacticalAnomalySnapshot> Anomalies(TacticalBattle battle) => battle.Anomalies.Select(a =>
        new TacticalAnomalySnapshot(a.Id, (TacticalAnomalyKindCode)a.Kind, a.Clumps.Select(c =>
            new TacticalClumpSnapshot(c.X, c.Y, c.Amplitude, c.SigmaX, c.SigmaY, c.Rotation)).ToImmutableArray())).ToImmutableArray();

    public static ImmutableArray<TacticalFireQueueEntry> FireQueue(TacticalBattle battle) => battle.CaptureFireOrder()
        .Select((drone, index) => new TacticalFireQueueEntry(index, drone.Id, drone.Initiative)).ToImmutableArray();

    public static TacticalShotSnapshot Shot(TacticalShot shot, int turn) => new(turn, shot.Shooter.Id, shot.Target.Id,
        Pose(shot.Shooter.Pose), Pose(shot.Target.Pose), shot.Chance, shot.Hit, shot.Damage, shot.Rear, shot.Kill, shot.LethalImpact)
    { Absorption = shot.Absorption, Absorbed = shot.Absorbed, LethalImpact = shot.LethalImpact, TargetEvading = shot.TargetEvading, Decoyed = shot.Decoyed, ChanceWithoutDecoy = shot.ChanceWithoutDecoy };

    public static ImmutableArray<TacticalUnitSnapshot> Units(TacticalBattle battle)
    {
        var units = ImmutableArray.CreateBuilder<TacticalUnitSnapshot>();
        foreach (var d in battle.Drones)
        {
            var w = d.Weapon;
            var build = new TacticalBuildSnapshot(d.Frame.Name, d.PilotId, battle.Catalog.Pilots[d.PilotId].Name, w.Id, w.Name,
                w.DamageMin, w.DamageMax, w.Piercing, w.HitBonus, w.Range, w.ArcDeg, w.RangeBonus, w.RearKillChance,
                d.Plates.Id, d.Plates.Name, d.Shield?.Id, d.Shield?.Name, d.Shield?.Defense ?? 0, d.ShieldCount, d.Frame.ShieldSlots,
                d.Frame.FrameMassKg, d.Frame.ReferenceMassKg, w.MassKg, d.Plates.MassKg, d.Shield?.MassKg ?? 0)
            { DecoyId = d.Decoy?.Id, DecoyName = d.Decoy?.Name, DecoyMaxCharges = d.Decoy?.Charges ?? 0 };
            var limits = d.Limits;
            units.Add(new(d.Id, d.Side, TacticalUnitKind.Drone, d.Frame.Id, Pose(d.Pose), d.Speed, d.Hull, d.MaxHull, d.Armor, d.MaxArmor,
                d.Status switch { TacticalDroneStatus.Air => TacticalUnitState.Air, TacticalDroneStatus.Disabled => TacticalUnitState.Disabled, _ => TacticalUnitState.Destroyed },
                d.Role, d.Jammed, d.Engines.Select(e => new TacticalEngineSnapshot(e.Definition.Id, e.Definition.Name, e.Factor,
                    e.Definition.MassKg, e.Definition.SpeedBonus, e.Definition.SpeedChange, e.Definition.TurnDeg)).ToImmutableArray(),
                new(limits.Mass, limits.MassFactor, limits.MinSpeed, limits.MaxSpeed, limits.Change, limits.Turn), d.PreviousTurn, build, d.Accuracy, d.Maneuver,
                battle.Catalog.Ai.Roles[d.Role].Name)
            { InDust = TacticalBattleSalvo.InDust(battle, d), Initiative = d.Initiative, Consciousness = d.Consciousness, DecoyCharges = d.DecoyCharges });
        }
        foreach (var c in battle.Carriers)
            units.Add(new(c.Id, c.Side, TacticalUnitKind.Carrier, TacticalBattleConstants.CarrierFrame, Pose(c.Pose), 0,
                c.Hull, c.MaxHull, c.Armor, c.MaxArmor, c.IsActive ? TacticalUnitState.Alive : TacticalUnitState.Destroyed,
                null, false, [], null, 0, null, 0, c.Maneuver));
        return units.ToImmutable();
    }

    public static ImmutableArray<TacticalTargetGroupSnapshot> Targets(TacticalBattle battle, string side) => battle.ActiveDrones(side).Select(d =>
    {
        var candidates = TacticalBattleSalvo.Candidates(battle, d);
        return new TacticalTargetGroupSnapshot(d.Id, candidates.Select(c => new TacticalTargetCandidateSnapshot(c.Target.Id,
            c.Distance, c.Angle, c.Terms.IsRear, d.Weapon.RearKillChance, c.Chance)
        { Absorption = c.Absorption }).ToImmutableArray(),
            TacticalBattleSalvo.DefaultTarget(d, candidates)?.Target.Id);
    }).ToImmutableArray();

    public static ImmutableArray<TacticalIncomingShotSnapshot> Incoming(TacticalBattle battle, string playerSide)
    {
        var rows = ImmutableArray.CreateBuilder<TacticalIncomingShotSnapshot>();
        var first = new HashSet<string>(StringComparer.Ordinal);
        foreach (var d in battle.CaptureFireOrder().Where(d => d.Side == battle.EnemySide(playerSide)))
        {
            var candidate = TacticalBattleSalvo.DefaultTarget(d, TacticalBattleSalvo.Candidates(battle, d));
            if (candidate is not null)
            {
                var adjusted = CandidatesIfEvading(battle, d, candidate.Target.Id);
                rows.Add(new(d.Id, candidate.Target.Id, candidate.Chance, candidate.Distance,
                    candidate.Angle, candidate.Terms.IsRear, d.Weapon.RearKillChance)
                {
                    Absorption = candidate.Absorption,
                    ChanceIfEvading = adjusted,
                    ChanceIfDecoy = candidate.Target is TacticalDrone { Decoy: { } decoy } && first.Add(candidate.Target.Id)
                        ? TacticalHitChance.WithDecoy(candidate.Terms.Chance, decoy.MissFactor, battle.Catalog.Rules.Hit.MinChance / 100.0) * (1 - candidate.Absorption) : null
                });
            }
        }
        return rows.ToImmutable();
    }

    private static double CandidatesIfEvading(TacticalBattle battle, TacticalDrone shooter, string targetId) =>
        TacticalBattleSalvo.Candidates(battle, shooter, new HashSet<string>(StringComparer.Ordinal) { targetId })
            .Single(c => c.Target.Id == targetId).Chance;

    public static ImmutableArray<TacticalLossCount> Losses(TacticalBattle battle) => TacticalBattleConstants.SideOrder
        .Select(side => new TacticalLossCount(side, battle.Drones.Count(d => d.Side == side && !d.IsActive))).ToImmutableArray();
    public static TacticalForecastRules Rules(TacticalRules r) => new(
        new(r.Hit.Base, r.Hit.MinChance, r.Hit.MaxChance, r.Hit.RangeBonus, r.Hit.AnglePenalty, r.Hit.RearBonus, r.Hit.RearBonusArcDeg),
        r.TurnAtMaxSpeedFraction, r.Rear.ArcHalfDeg, r.Substeps)
    {
        Evasion = new(r.Evasion.HitPenalty, r.Evasion.Cost, r.Evasion.MinConsciousness, r.Consciousness.RecoveryPerTurn, r.Consciousness.Max),
        Decoy = new(r.Decoy.AiThreatChance),
        Anomalies = new(r.Anomalies.ZoneEdge, r.Anomalies.Background, r.Anomalies.AbsorbSamples)
    };
    public static TacticalPoseSnapshot Pose(TacticalPose pose) => new(pose.X, pose.Y, pose.Course);
    public static TacticalOutcomeSnapshot? Outcome(TacticalBattleResult? result)
    {
        if (result is null) return null;
        var s = result.Statistics;
        return new(result.Kind switch
        {
            TacticalOutcomeKind.A => TacticalBattleOutcomeCode.A,
            TacticalOutcomeKind.B => TacticalBattleOutcomeCode.B,
            TacticalOutcomeKind.Draw => TacticalBattleOutcomeCode.Draw,
            _ => TacticalBattleOutcomeCode.Timeout
        },
            result.Reasons.Select(reason => reason switch
            {
                TacticalCarrierDestroyed c => new TacticalOutcomeReasonSnapshot(TacticalOutcomeReasonCode.CarrierDestroyed, c.CarrierId),
                TacticalNoDrones d => new TacticalOutcomeReasonSnapshot(TacticalOutcomeReasonCode.NoDrones, d.CarrierId),
                TacticalTimeout t => new TacticalOutcomeReasonSnapshot(TacticalOutcomeReasonCode.Timeout, Cap: t.Cap),
                _ => throw new InvalidOperationException("Unknown tactical outcome reason.")
            }).ToImmutableArray(), result.Turns,
            TacticalBattleConstants.SideOrder.Select((side, i) => new TacticalLossCount(side, result.Losses[i])).ToImmutableArray(),
            new(s.Shots, s.Hits, s.RearHits, s.RearKills, s.Systems, s.Disabled, s.Hull, s.Armor, s.CarrierHull, s.FirstLoss, s.RoleChanges,
                s.Weapons.Select(w => new TacticalWeaponTotalsSnapshot(w.WeaponId, w.Shots, w.Hits, w.RearHits, w.RearKills, w.Hull)).ToImmutableArray()), result.RandomCalls);
    }

    public static TacticalBattleOptions Options(TacticalBattleCatalog catalog)
    {
        var scenarios = catalog.Scenarios.Entries.Select(entry =>
        {
            var scenario = entry.Value;
            string playerSide = scenario.Goal?.PlayerSide ?? "A";
            var drones = scenario.Sides.SelectMany(side => side.Drones.Select(start =>
            {
                var layout = catalog.Layouts[start.Layout];
                var frame = catalog.Frames[layout.Class];
                var pilot = catalog.Pilots[start.Pilot];
                return new TacticalDroneSetupOption(start.Id, side.Id, frame.Id, frame.Name, start.Pilot, pilot.Name,
                    new(pilot.Accuracy.Minimum, pilot.Accuracy.Maximum, pilot.Accuracy.IsRange),
                    new(pilot.Maneuver.Minimum, pilot.Maneuver.Maximum, pilot.Maneuver.IsRange), layout.Hull ?? frame.Hull,
                    layout.Weapon!, layout.Armor, layout.Shield, layout.Shield is null ? 0 : layout.ShieldCount ?? TacticalBattleConstants.DefaultShieldCount, frame.ShieldSlots)
                { Initiative = new(pilot.Initiative.Minimum, pilot.Initiative.Maximum, pilot.Initiative.IsRange) };
            })).ToImmutableArray();
            var goal = scenario.Goal;
            return new TacticalScenarioOption(entry.Id, scenario.Name, scenario.Description, playerSide,
                scenario.Sides.First(s => s.Id == playerSide).Tactic, scenario.Sides.First(s => s.Id != playerSide).Tactic,
                goal is null ? null : new(goal.PlayerSide, goal.WinMaxOneLoss, goal.WinNoLoss, goal.Tolerance, goal.Seeds), drones);
        }).ToImmutableArray();
        return new(new(true), scenarios,
            catalog.Weapons.Entries.Select(e => new TacticalWeaponOption(e.Id, e.Value.Name, e.Value.Tier, e.Value.ArcDeg, e.Value.Range,
                e.Value.DamageMin, e.Value.DamageMax, e.Value.Piercing, e.Value.HitBonus, e.Value.RangeBonus, e.Value.RearKillChance, e.Value.MassKg)).ToImmutableArray(),
            catalog.Armor.Entries.Select(e => new TacticalArmorOption(e.Id, e.Value.Name, e.Value.Tier, e.Value.Armor, e.Value.MassKg)).ToImmutableArray(),
            catalog.Shields.Entries.Select(e => new TacticalShieldOption(e.Id, e.Value.Name, e.Value.Tier, e.Value.Defense, e.Value.MassKg)).ToImmutableArray(),
            catalog.Ai.Tactics.Entries.Select(e => new TacticalTacticOption(e.Id, e.Value.Name, e.Value.Adaptive)).ToImmutableArray(), catalog.Rules.Version);
    }
}
