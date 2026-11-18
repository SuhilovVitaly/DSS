using System.Collections.Immutable;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Engine.TacticalBattle;

internal enum TacticalTurnPhase { Planning, Movement, Firing, Complete }

internal sealed partial class TacticalBattle
{
    public TacticalTurnPhase Phase { get; private set; } = TacticalTurnPhase.Planning;
    public TacticalBattleResult? Result { get; private set; }
    public ImmutableArray<TacticalMovement> LastMovement { get; private set; } = [];
    public TacticalSalvoResult? LastSalvo { get; private set; }

    public void Planning(Action<TacticalBattle> planner)
    {
        ArgumentNullException.ThrowIfNull(planner);
        Require(TacticalTurnPhase.Planning);
        Turn++;
        if (Turn > 1)
            foreach (var drone in Drones.Where(d => d.IsActive))
                drone.Consciousness = (int)PyMath.Min(Catalog.Rules.Consciousness.Max,
                    (long)drone.Consciousness + Catalog.Rules.Consciousness.RecoveryPerTurn);
        LastMovement = [];
        LastSalvo = null;
        planner(this);
        Phase = TacticalTurnPhase.Movement;
    }

    public void Movement()
    {
        Require(TacticalTurnPhase.Movement);
        LastMovement = TacticalBattleMovement.Move(this);
        Phase = TacticalTurnPhase.Firing;
        CheckVictory();
    }

    public void Firing(Func<TacticalDrone, IReadOnlyList<TacticalCandidate>, TacticalFirePlan>? targetsFor = null,
        Func<TacticalDrone, bool>? aiDecoysFor = null)
    {
        Require(TacticalTurnPhase.Firing);
        LastSalvo = TacticalBattleSalvo.Fire(this, targetsFor, aiDecoysFor);
        if (Result is null) Phase = TacticalTurnPhase.Planning;
        CheckVictory();
    }

    public TacticalBattleResult? CheckVictory()
    {
        if (Result is not null) return Result;
        foreach (var carrier in Carriers.Where(c => c.Side == SelfDestructEnemySide && c.IsActive))
        {
            var wing = Drones.Where(d => d.OriginCarrierId == carrier.Id && d.Side == carrier.Side).ToArray();
            if (wing.Length == 0 || wing.Any(d => d.Status == TacticalDroneStatus.Air)) continue;
            carrier.Hull = 0;
            SelfDestructedCarrierIds.Add(carrier.Id);
        }
        var reasons = ImmutableArray.CreateBuilder<TacticalOutcomeReason>();
        var defeated = new List<string>();
        foreach (string side in TacticalBattleConstants.SideOrder)
        {
            var carriers = Carriers.Where(c => c.Side == side).ToArray();
            if (carriers.All(c => !c.IsActive))
            {
                // Loss of command disables surviving drones; this is not hull damage or pilot death.
                foreach (var drone in Drones.Where(d => d.Side == side && d.Status != TacticalDroneStatus.Destroyed))
                {
                    if (drone.Status == TacticalDroneStatus.Air) Statistics.Disabled++;
                    drone.Status = TacticalDroneStatus.Disabled;
                    drone.Speed = 0;
                    drone.Order = new(0, 0);
                }
                defeated.Add(side);
                reasons.AddRange(carriers.Select(c => new TacticalCarrierDestroyed(c.Id)));
            }
            else if (!ActiveDrones(side).Any())
            {
                defeated.Add(side);
                reasons.AddRange(carriers.Where(c => c.IsActive).Select(c => new TacticalNoDrones(c.Id)));
            }
        }
        if (defeated.Count == 0)
        {
            // No normal defeat: if neither side has an active, un-jammed drone with ammo, the
            // battle ends in a draw with NoCombatCapability (dust/distance/geometry are not exhaustion).
            bool sideACanFire = ActiveDrones("A").Any(d => !d.Jammed && d.AmmoCurrent > 0);
            bool sideBCanFire = ActiveDrones("B").Any(d => !d.Jammed && d.AmmoCurrent > 0);
            if (!sideACanFire && !sideBCanFire)
            {
                Result = CaptureResult(TacticalOutcomeKind.Draw, [new TacticalNoCombatCapability()]);
                Phase = TacticalTurnPhase.Complete;
                return Result;
            }
            return null;
        }
        TacticalOutcomeKind kind = defeated.Count == 2 ? TacticalOutcomeKind.Draw
            : defeated[0] == "A" ? TacticalOutcomeKind.B : TacticalOutcomeKind.A;
        Result = CaptureResult(kind, reasons.ToImmutable());
        Phase = TacticalTurnPhase.Complete;
        return Result;
    }

    public TacticalBattleResult? Step(Action<TacticalBattle> planner)
    {
        Planning(planner);
        Movement();
        if (Result is null) Firing();
        return Result;
    }

    public TacticalBattleResult Run(Action<TacticalBattle> planner, int? cap = null)
    {
        ArgumentNullException.ThrowIfNull(planner);
        int limit = cap is null or 0 ? Catalog.Rules.SeriesTurnCap : cap.Value;
        if (limit < 0) throw new ArgumentOutOfRangeException(nameof(cap));
        if (Result is not null) return Result;
        Require(TacticalTurnPhase.Planning);
        while (Turn < limit && Result is null) Step(planner);
        return Result ?? EndWithTimeout(limit);
    }

    public TacticalBattleResult EndWithTimeout(int cap)
    {
        if (Result is not null) return Result;
        var result = CaptureResult(TacticalOutcomeKind.Timeout, [new TacticalTimeout(cap)]);
        Result = result;
        Phase = TacticalTurnPhase.Complete;
        return result;
    }

    public TacticalBattleTotals CaptureStatistics() => new(Statistics.Shots, Statistics.Hits, Statistics.RearHits,
        Statistics.RearKills, Statistics.Systems, Statistics.Disabled, Statistics.Hull, Statistics.Armor,
        Statistics.CarrierHull, Statistics.FirstLoss, RoleChanges,
        Statistics.Weapons.Select(w => new TacticalWeaponTotals(w.WeaponId, w.Shots, w.Hits, w.RearHits, w.RearKills, w.Hull)).ToImmutableArray());

    private TacticalBattleResult CaptureResult(TacticalOutcomeKind kind, ImmutableArray<TacticalOutcomeReason> reasons) => new(
        ScenarioId, Seed, kind, reasons, Turn,
        TacticalBattleConstants.SideOrder.Select(side => Drones.Count(d => d.Side == side && !d.IsActive)).ToImmutableArray(),
        Carriers.Select(c => c.Hull).ToImmutableArray(), CaptureStatistics(), Random.Calls);

    private void Require(TacticalTurnPhase phase)
    {
        if (Result is not null || Phase != phase) throw new InvalidOperationException($"Expected tactical phase {phase}, current {Phase}.");
    }
}
