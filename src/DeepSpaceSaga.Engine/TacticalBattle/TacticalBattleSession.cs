using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.TacticalBattle.Ai;
using DeepSpaceSaga.Engine.TacticalBattle.Content;

namespace DeepSpaceSaga.Engine.TacticalBattle;

internal sealed class TacticalBattleSession
{
    private readonly List<TacticalBattleLogEvent> _log = [];
    private ulong _eventSequence;
    private ImmutableArray<TacticalMovementPath> _paths = [];
    private ImmutableArray<TacticalShotSnapshot> _shots = [];
    private ImmutableArray<string> _decoysLaunched = [];
    private ImmutableArray<TacticalUnitSnapshot> _beforeSalvo = [];
    private bool _fireSkipped;
    private ImmutableArray<TacticalFireQueueEntry> _fireQueue = [];
    private ImmutableArray<TacticalFireStepSnapshot> _fireSteps = [];
    public TacticalBattle Battle { get; }
    public string Id { get; }
    public string PlayerSide { get; }
    public ulong Revision { get; private set; } = 1;
    public TacticalBattlePhase Phase => Battle.Result is not null ? TacticalBattlePhase.Over
        : Battle.Phase == TacticalTurnPhase.Firing ? TacticalBattlePhase.Fire : TacticalBattlePhase.Move;

    private TacticalBattleSession(TacticalBattle battle, string id, string playerSide)
    {
        Battle = battle;
        Id = id;
        PlayerSide = playerSide;
        Add(TacticalBattleLogKind.Started, TacticalBattlePhase.Move);
        BeginTurn();
    }

    public static TacticalBattleSession? TryCreate(TacticalBattleCatalog catalog, StartTacticalBattleRequest request, out TacticalBattleRejectionCode code)
    {
        code = TacticalBattleRejectionCode.None;
        string scenarioId = string.IsNullOrWhiteSpace(request.ScenarioId) ? catalog.Scenarios.Entries[0].Id : request.ScenarioId;
        if (!catalog.Scenarios.Contains(scenarioId)) { code = TacticalBattleRejectionCode.UnknownScenario; return null; }
        var scenario = catalog.Scenarios[scenarioId];
        string playerSide = scenario.Goal?.PlayerSide ?? "A";
        string enemySide = playerSide == "A" ? "B" : "A";
        string ownTactic = string.IsNullOrWhiteSpace(request.OwnTacticId) ? scenario.Sides.First(s => s.Id == playerSide).Tactic : request.OwnTacticId;
        string enemyTactic = string.IsNullOrWhiteSpace(request.EnemyTacticId) ? scenario.Sides.First(s => s.Id == enemySide).Tactic : request.EnemyTacticId;
        if (!catalog.Ai.Tactics.Contains(ownTactic) || !catalog.Ai.Tactics.Contains(enemyTactic)) { code = TacticalBattleRejectionCode.UnknownTactic; return null; }
        var overlays = new Dictionary<string, TacticalLoadoutOverride>(StringComparer.Ordinal);
        if (!request.Loadout.IsDefault)
            foreach (var choice in request.Loadout)
            {
                if (choice is null) { code = TacticalBattleRejectionCode.InvalidRequest; return null; }
                var drone = scenario.Sides.First(s => s.Id == playerSide).Drones.FirstOrDefault(d => d.Id == choice.DroneId);
                if (drone is null) { code = TacticalBattleRejectionCode.InvalidDrone; return null; }
                if (overlays.ContainsKey(drone.Id)) { code = TacticalBattleRejectionCode.DuplicateDrone; return null; }
                if (choice.GunId is null || !catalog.Weapons.Contains(choice.GunId)) { code = TacticalBattleRejectionCode.UnknownWeapon; return null; }
                if (choice.PlateId is null || !catalog.Armor.Contains(choice.PlateId)) { code = TacticalBattleRejectionCode.UnknownArmor; return null; }
                string? shield = string.IsNullOrEmpty(choice.ShieldId) ? null : choice.ShieldId;
                if (shield is not null && !catalog.Shields.Contains(shield)) { code = TacticalBattleRejectionCode.UnknownShield; return null; }
                int slots = catalog.Frames[catalog.Layouts[drone.Layout].Class].ShieldSlots;
                if (shield is null ? choice.ShieldCount != 0 : choice.ShieldCount < 1 || choice.ShieldCount > slots)
                { code = TacticalBattleRejectionCode.InvalidShieldCount; return null; }
                overlays.Add(drone.Id, new(Weapon: choice.GunId, Armor: choice.PlateId, Shield: shield ?? "", ShieldCount: choice.ShieldCount));
            }
        var tactics = new Dictionary<string, string>(StringComparer.Ordinal) { [playerSide] = ownTactic, [enemySide] = enemyTactic };
        return new(TacticalBattle.Create(catalog, scenarioId, request.Seed, tactics, overlays), request.CommandId, playerSide);
    }

    public TacticalBattleResponse Move(MoveTacticalBattleRequest request)
    {
        var error = Check(request.BattleId, request.ExpectedRevision, TacticalBattlePhase.Move);
        if (error != TacticalBattleRejectionCode.None) return Reject(request.CommandId, error);
        var proposed = new List<(TacticalDrone Drone, TacticalOrder Order)>();
        if (!request.Orders.IsDefault)
            foreach (var order in request.Orders)
            {
                if (order is null) return Reject(request.CommandId, TacticalBattleRejectionCode.InvalidRequest);
                var drone = Own(order.DroneId);
                if (drone is null) return Reject(request.CommandId, TacticalBattleRejectionCode.InvalidDrone);
                if (proposed.Any(p => ReferenceEquals(p.Drone, drone))) return Reject(request.CommandId, TacticalBattleRejectionCode.DuplicateDrone);
                if (!double.IsFinite(order.Speed) || !double.IsFinite(order.Turn)) return Reject(request.CommandId, TacticalBattleRejectionCode.InvalidNumber);
                proposed.Add((drone, new(order.Speed, order.Turn)));
            }
        int events = _log.Count;
        foreach (var proposal in proposed) proposal.Drone.Order = proposal.Order;
        _shots = [];
        _decoysLaunched = [];
        _fireQueue = [];
        _fireSteps = [];
        _beforeSalvo = [];
        _fireSkipped = false;
        Battle.Movement(); // current Motion limits clamp all orders, including untouched AI suggestions
        _paths = Battle.LastMovement.Select(m => new TacticalMovementPath(Battle.Turn, m.DroneId,
            m.Path.Select(TacticalBattleSnapshotBuilder.Pose).ToImmutableArray())).ToImmutableArray();
        if (Battle.Result is not null) AddOutcome(TacticalBattlePhase.Move);
        else if (!Battle.ActiveDrones(PlayerSide).Any(d => !TacticalBattleSalvo.Candidates(Battle, d).IsEmpty)
            && !Battle.ActiveDrones(Battle.EnemySide(PlayerSide)).Any(d =>
                TacticalBattleSalvo.Candidates(Battle, d).Any(c => c.Target is TacticalDrone)))
        {
            _fireSkipped = true;
            Add(TacticalBattleLogKind.NoTargets, TacticalBattlePhase.Move);
            ResolveFire([]);
        }
        Revision++;
        return Accepted(request.CommandId, events);
    }

    public TacticalBattleResponse Fire(FireTacticalBattleRequest request)
    {
        var error = Check(request.BattleId, request.ExpectedRevision, TacticalBattlePhase.Fire);
        if (error != TacticalBattleRejectionCode.None) return Reject(request.CommandId, error);
        var targets = new List<(string DroneId, TacticalFirePlan Plan)>();
        if (!request.Targets.IsDefault)
            foreach (var target in request.Targets)
            {
                if (target is null) return Reject(request.CommandId, TacticalBattleRejectionCode.InvalidRequest);
                var drone = Own(target.DroneId);
                if (drone is null) return Reject(request.CommandId, TacticalBattleRejectionCode.InvalidDrone);
                if (targets.Any(t => t.DroneId == drone.Id)) return Reject(request.CommandId, TacticalBattleRejectionCode.DuplicateDrone);
                if (target.Decoy && target.Evade) return Reject(request.CommandId, TacticalBattleRejectionCode.InvalidRequest);
                if (target.Decoy && (drone.Decoy is null || drone.DecoyCharges < 1))
                    return Reject(request.CommandId, TacticalBattleRejectionCode.DecoyUnavailable);
                if (target.Evade && target.TargetId is not null) return Reject(request.CommandId, TacticalBattleRejectionCode.InvalidRequest);
                if (target.Evade && drone.Consciousness < Battle.Catalog.Rules.Evasion.MinConsciousness)
                    return Reject(request.CommandId, TacticalBattleRejectionCode.EvasionUnavailable);
                if (target.TargetId is not null && !TacticalBattleSalvo.Candidates(Battle, drone).Any(c => c.Target.Id == target.TargetId))
                    return Reject(request.CommandId, TacticalBattleRejectionCode.InvalidTarget);
                targets.Add((drone.Id, new(target.TargetId, target.Evade, target.Decoy)));
            }
        int events = _log.Count;
        ResolveFire(targets);
        Revision++;
        return Accepted(request.CommandId, events);
    }

    private void ResolveFire(IReadOnlyList<(string DroneId, TacticalFirePlan Plan)> targets)
    {
        _beforeSalvo = TacticalBattleSnapshotBuilder.Units(Battle);
        _fireQueue = TacticalBattleSnapshotBuilder.FireQueue(Battle);
        int turn = Battle.Turn;
        Battle.Firing((drone, candidates) => drone.Side == PlayerSide
            ? targets.FirstOrDefault(t => t.DroneId == drone.Id).Plan ?? new(null, false)
            : TacticalBattleSalvo.DefaultPlan(Battle, drone, candidates), drone => drone.Side != PlayerSide);
        _decoysLaunched = Battle.LastSalvo!.DecoysLaunched;
        foreach (string droneId in _decoysLaunched)
            Add(TacticalBattleLogKind.DecoyLaunched, TacticalBattlePhase.Fire, actor: droneId,
                amount: Battle.Drones.Single(d => d.Id == droneId).DecoyCharges);
        var steps = ImmutableArray.CreateBuilder<TacticalFireStepSnapshot>();
        var previous = _beforeSalvo;
        foreach (var step in Battle.LastSalvo!.Steps)
        {
            int events = _log.Count;
            TacticalShotSnapshot? shot = step.Shot is { } actual ? TacticalBattleSnapshotBuilder.Shot(actual, turn) : null;
            if (shot is not null)
                Add(shot.Kill ? TacticalBattleLogKind.RearKill : shot.Hit ? TacticalBattleLogKind.ShotHit : TacticalBattleLogKind.ShotMissed,
                    TacticalBattlePhase.Fire, shot.ShooterId, shot.TargetId, amount: shot.Damage,
                    shot: new TacticalLogShot(shot.Chance, shot.Rear) { Decoyed = shot.Decoyed }, fireStepIndex: step.Index);
            else
                Add(TacticalBattleLogKind.FireSkipped, TacticalBattlePhase.Fire, step.ShooterId, step.TargetId,
                    skipReason: step.SkipReason, fireStepIndex: step.Index);
            if (step.Damage is { } damage)
            {
                var before = previous.First(u => u.Id == damage.TargetId);
                var now = step.UnitsAfter.First(u => u.Id == damage.TargetId);
                var unit = new TacticalLogUnit(now.Kind, now.Hull, now.MaxHull);
                Add(TacticalBattleLogKind.DamageApplied, TacticalBattlePhase.Fire, target: now.Id,
                    damage: new(damage.Hull, damage.Armor, 1, now.Hull), fireStepIndex: step.Index);
                if (now.State == TacticalUnitState.Disabled && before.State == TacticalUnitState.Air)
                    Add(TacticalBattleLogKind.Disabled, TacticalBattlePhase.Fire, target: now.Id, unit: unit, fireStepIndex: step.Index);
                else if (now.Hull <= 0 && before.Hull > 0)
                    Add(TacticalBattleLogKind.Destroyed, TacticalBattlePhase.Fire, target: now.Id, unit: unit, fireStepIndex: step.Index);
            }
            foreach (var change in step.Systems)
            {
                var unit = step.UnitsAfter.First(u => u.Id == change.TargetId);
                var engine = change.EngineIndex is { } index ? unit.Engines[index] : null;
                Add(change.Effect == "weaponJammed" ? TacticalBattleLogKind.WeaponJammed : TacticalBattleLogKind.EngineDamaged,
                    TacticalBattlePhase.Fire, target: unit.Id, engine: change.EngineIndex,
                    unit: new(unit.Kind, unit.Hull, unit.MaxHull, engine?.Name, engine?.Factor), fireStepIndex: step.Index);
            }
            var outcome = TacticalBattleSnapshotBuilder.Outcome(step.Outcome);
            if (outcome is not null)
                _log.Add(new TacticalBattleLogEvent(++_eventSequence, turn, TacticalBattlePhase.Fire,
                    TacticalBattleLogKind.Outcome, Outcome: outcome)
                { FireStepIndex = step.Index });
            steps.Add(new(step.Index, step.ShooterId, step.TargetId, step.SkipReason, shot, step.UnitsAfter,
                _log.Skip(events).ToImmutableArray(), outcome));
            previous = step.UnitsAfter;
        }
        _fireSteps = steps.ToImmutable();
        _shots = _fireSteps.Where(s => s.Shot is not null).Select(s => s.Shot!).ToImmutableArray();
        if (Battle.Result is null) BeginTurn();
    }

    private void BeginTurn()
    {
        Battle.Planning(TacticalBattleAi.Plan);
        foreach (var entry in Battle.AiEvents)
        {
            if (entry is TacticalRoleChanged role) Add(TacticalBattleLogKind.RoleChanged, TacticalBattlePhase.Move,
                role.DroneId, role: role.Role, noAttackers: role.NoAttackers);
            else Add(TacticalBattleLogKind.FighterBreak, TacticalBattlePhase.Move, entry.DroneId);
        }
    }
    private static TacticalLogUnit Describe(TacticalUnit unit) =>
        new(unit is TacticalCarrier ? TacticalUnitKind.Carrier : TacticalUnitKind.Drone, unit.Hull, unit.MaxHull);
    private void AddOutcome(TacticalBattlePhase phase) => _log.Add(new(++_eventSequence, Battle.Turn, phase,
        TacticalBattleLogKind.Outcome, Outcome: TacticalBattleSnapshotBuilder.Outcome(Battle.Result)));
    private void Add(TacticalBattleLogKind kind, TacticalBattlePhase phase, string? actor = null, string? target = null,
        string? role = null, int? amount = null, int? engine = null, bool noAttackers = false,
        TacticalLogShot? shot = null, TacticalLogDamage? damage = null, TacticalLogUnit? unit = null,
        TacticalFireSkipReason? skipReason = null, int? fireStepIndex = null) =>
        _log.Add(new(++_eventSequence, Battle.Turn, phase, kind, actor, target, role, amount, engine, noAttackers,
            Shot: shot, Damage: damage, Unit: unit,
            RoleName: role is null ? null : Battle.Catalog.Ai.Roles[role].Name)
        { SkipReason = skipReason, FireStepIndex = fireStepIndex });
    private TacticalDrone? Own(string id) => Battle.Drones.FirstOrDefault(d => d.Id == id && d.Side == PlayerSide && d.IsActive);
    private TacticalBattleRejectionCode Check(string id, ulong revision, TacticalBattlePhase phase) => id != Id ? TacticalBattleRejectionCode.WrongBattle
        : revision != Revision ? TacticalBattleRejectionCode.StaleRevision : Phase != phase ? TacticalBattleRejectionCode.WrongPhase : TacticalBattleRejectionCode.None;

    public TacticalBattleSnapshot Capture() => new(Id, Revision, Phase, Battle.Turn, PlayerSide, Battle.Seed, Battle.ScenarioId,
        Battle.Catalog.Scenarios[Battle.ScenarioId].Name,
        TacticalBattleConstants.SideOrder.Select((side, i) => new TacticalSideTactic(side, Battle.Tactics[i])).ToImmutableArray(),
        TacticalBattleSnapshotBuilder.Units(Battle), Phase == TacticalBattlePhase.Move ? Battle.ActiveDrones(PlayerSide)
            .Select(d => new TacticalDroneOrder(d.Id, d.Order.Speed, d.Order.Turn)).ToImmutableArray() : [], _paths,
        Phase == TacticalBattlePhase.Fire ? TacticalBattleSnapshotBuilder.Targets(Battle, PlayerSide) : [],
        Phase == TacticalBattlePhase.Fire ? TacticalBattleSnapshotBuilder.Incoming(Battle, PlayerSide) : [],
        _shots, _beforeSalvo, _fireSkipped, TacticalBattleSnapshotBuilder.Rules(Battle.Catalog.Rules),
        TacticalBattleSnapshotBuilder.Losses(Battle), TacticalBattleSnapshotBuilder.Outcome(Battle.Result), _log.ToImmutableArray())
    {
        FireQueue = !_fireSteps.IsEmpty ? _fireQueue : Phase == TacticalBattlePhase.Fire ? TacticalBattleSnapshotBuilder.FireQueue(Battle) : [],
        FireSteps = _fireSteps,
        DecoysLaunched = _decoysLaunched,
        Anomalies = TacticalBattleSnapshotBuilder.Anomalies(Battle)
    };
    public TacticalBattleResponse Initial(string commandId) => Accepted(commandId, 0);
    public TacticalBattleResponse Reject(string commandId, TacticalBattleRejectionCode code) => new(commandId, false, code, Phase, Capture(), []);
    public TacticalBattleResponse Exit(string commandId)
    {
        int events = _log.Count;
        Add(TacticalBattleLogKind.Exited, Phase);
        return new(commandId, true, TacticalBattleRejectionCode.None, Phase, null, _log.Skip(events).ToImmutableArray());
    }
    private TacticalBattleResponse Accepted(string commandId, int events) => new(commandId, true, TacticalBattleRejectionCode.None,
        Phase, Capture(), _log.Skip(events).ToImmutableArray());
}
