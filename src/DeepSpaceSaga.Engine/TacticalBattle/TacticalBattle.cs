using System.Collections.Immutable;

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
                drone.Consciousness = (int)Math.Min(Catalog.Rules.Consciousness.Max,
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

    public void Firing(Func<TacticalDrone, IReadOnlyList<TacticalCandidate>, TacticalFirePlan>? targetsFor = null)
    {
        Require(TacticalTurnPhase.Firing);
        LastSalvo = TacticalBattleSalvo.Fire(this, targetsFor);
        if (Result is null) Phase = TacticalTurnPhase.Planning;
        CheckVictory();
    }

    public TacticalBattleResult? CheckVictory()
    {
        if (Result is not null) return Result;
        var reasons = ImmutableArray.CreateBuilder<TacticalOutcomeReason>();
        var defeated = new List<string>();
        foreach (string side in TacticalBattleConstants.SideOrder)
        {
            var carrier = Carrier(side);
            TacticalOutcomeReason? reason = carrier.Hull <= 0 ? new TacticalCarrierDestroyed(carrier.Id)
                : !ActiveDrones(side).Any() ? new TacticalNoDrones(carrier.Id) : null;
            if (reason is null) continue;
            defeated.Add(side);
            reasons.Add(reason);
        }
        if (defeated.Count == 0) return null;
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
        if (Result is null)
        {
            Result = CaptureResult(TacticalOutcomeKind.Timeout, [new TacticalTimeout(limit)]);
            Phase = TacticalTurnPhase.Complete;
        }
        return Result;
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
