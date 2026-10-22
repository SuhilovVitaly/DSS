using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private CombatEncounterState _encounterState;
    private long _encounterStartedAt;
    private long? _encounterEndedAt;
    private long _encounterQuietSince;
    private readonly HashSet<string> _encounterEnemyIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _encounterProjectileIds = new(StringComparer.Ordinal);
    private CombatEncounterStats _encounterStats = new();
    private SimulationSpeed? _encounterResumeSpeed;
    private bool _advancingPointClock;
    private bool UsesPointEncounters => _registry.CombatRules?.IsPointAim == true;

    private void ResetEncounter(long time)
    {
        _encounterState = CombatEncounterState.Idle;
        _encounterStartedAt = time;
        _encounterEndedAt = null;
        _encounterQuietSince = time;
        _encounterEnemyIds.Clear();
        _encounterProjectileIds.Clear();
        _encounterStats = new();
        _encounterResumeSpeed = null;
    }

    private bool EncounterEnemy(SpaceObjectRuntime ship) => IsHostileActor(ship) &&
        (ship.AttackAi is not null || _encounterEnemyIds.Contains(ship.InitialMotion.ObjectId));

    private void StartEncounter(long time, string? launchingOwner = null)
    {
        if (!UsesPointEncounters || _encounterState is CombatEncounterState.Active or CombatEncounterState.Lost || IsDefeated) return;
        _encounterState = CombatEncounterState.Active;
        _encounterStartedAt = time;
        _encounterEndedAt = null;
        _encounterStats = new();
        _encounterProjectileIds.Clear();
        foreach (var flight in _objects.Where(o => o.InitialMotion.OwnProjectile is not null))
            _encounterProjectileIds.Add(flight.InitialMotion.ObjectId);
        _encounterResumeSpeed = _dialogue.Active is not null ? _dialogue.ResumeSpeed ?? _clock.Speed : _clock.Speed;
        if (_encounterResumeSpeed > SimulationSpeed.Speed1)
        {
            if (_dialogue.Active is not null) _dialogue.ResumeSpeed = SimulationSpeed.Speed1;
            else _clock.SetSpeedAtProcessedBoundary(SimulationSpeed.Speed1);
        }
        _encounterEnemyIds.Clear();
        foreach (var enemy in _objects.Where(o => IsHostileActor(o) && o.AttackAi is not null)) _encounterEnemyIds.Add(enemy.InitialMotion.ObjectId);
        if (launchingOwner is not null) _encounterEnemyIds.Add(launchingOwner);
    }

    private void ObserveEncounterFact(CombatJournalEntry entry)
    {
        if (!UsesPointEncounters) return;
        if (entry.Type == CombatEventType.Launch && entry.ProjectileKind == ProjectileKind.Torpedo)
        {
            var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == entry.ActorObjectId && IsHostileActor(o));
            if (owner is not null)
            {
                if (_encounterState != CombatEncounterState.Active) StartEncounter((long)Math.Ceiling(entry.MotionTimeMs), owner.InitialMotion.ObjectId);
                _encounterEnemyIds.Add(owner.InitialMotion.ObjectId);
            }
        }
        ObserveEncounterStats(entry);
    }

    private bool HasLiveEncounterFlights => _objects.Any(o => o.InitialMotion.OwnProjectile is not null &&
        _encounterProjectileIds.Contains(o.InitialMotion.ObjectId));

    private bool TracksEncounterFact(string projectileId) => UsesPointEncounters &&
        (_encounterState == CombatEncounterState.Active || _encounterProjectileIds.Contains(projectileId));

    private void ObserveEncounterStats(CombatJournalEntry entry)
    {
        bool launch = entry.Type == CombatEventType.Launch;
        if (!TracksEncounterFact(entry.ProjectileObjectId) && !(launch && HasLiveEncounterFlights)) return;
        var stats = _encounterStats;
        checked
        {
            if (launch)
            {
                _encounterProjectileIds.Add(entry.ProjectileObjectId);
                if (entry.ActorObjectId == PlayerShipObjectId)
                {
                    if (entry.ProjectileKind == ProjectileKind.Torpedo) stats = stats with { TorpedoesLaunched = stats.TorpedoesLaunched + 1 };
                    else if (entry.ProjectileKind == ProjectileKind.Countermissile) stats = stats with { CountermissilesLaunched = stats.CountermissilesLaunched + 1 };
                }
                else if (entry.ProjectileKind == ProjectileKind.Torpedo && _encounterEnemyIds.Contains(entry.ActorObjectId))
                    stats = stats with { EnemyTorpedoesLaunched = stats.EnemyTorpedoesLaunched + 1 };
            }
            else if (entry.Type == CombatEventType.Hit)
            {
                if (entry.TargetObjectId == PlayerShipObjectId && entry.Damage is > 0)
                    stats = stats with { PlayerHitsReceived = stats.PlayerHitsReceived + 1 };
                else if (entry.ActorObjectId == PlayerShipObjectId &&
                    _objects.Any(o => o.InitialMotion.ObjectId == entry.TargetObjectId && IsHostileActor(o)))
                    stats = stats with { EnemyHitsDealt = stats.EnemyHitsDealt + 1 };
            }
            else if (entry.Type == CombatEventType.ModuleHit && entry.TargetObjectId == PlayerShipObjectId)
                stats = stats with
                {
                    ArmorDamageTaken = stats.ArmorDamageTaken + Math.Max(0L, (long)(entry.ArmorBefore ?? 0) - (entry.ArmorAfter ?? 0)),
                    StructureDamageTaken = stats.StructureDamageTaken + Math.Max(0L, (long)(entry.StructureBefore ?? 0) - (entry.StructureAfter ?? 0))
                };
            else if (entry.Type == CombatEventType.ShotDown && entry.ActorObjectId == PlayerShipObjectId && entry.ProjectileKind == ProjectileKind.Torpedo)
            {
                var victim = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == entry.ProjectileObjectId);
                string? owner = victim?.InitialMotion.OwnProjectile?.OwnerObjectId;
                string? relation = victim?.InitialMotion.OwnProjectile?.OwnerPlayerRelationAtLaunch;
                bool hostile = relation is not null ? relation == PlayerRelation.Enemy : owner is not null &&
                    (_encounterEnemyIds.Contains(owner) || _objects.Any(o => o.InitialMotion.ObjectId == owner && IsHostileActor(o)));
                if (hostile)
                    stats = stats with { TorpedoesIntercepted = stats.TorpedoesIntercepted + 1 };
            }
        }
        _encounterStats = stats;
    }

    private void RecordEncounterEnemyDeath(SpaceObjectRuntime ship, string? projectileId)
    {
        if (TracksEncounterFact(projectileId ?? string.Empty) && IsHostileActor(ship))
            _encounterStats = _encounterStats with { EnemiesDestroyed = checked(_encounterStats.EnemiesDestroyed + 1) };
    }

    private void RecordEncounterEvasion(string threatId)
    {
        if (TracksEncounterFact(threatId))
            _encounterStats = _encounterStats with { PlayerEvasions = checked(_encounterStats.PlayerEvasions + 1) };
    }

    private void EvaluateEncounter(long time)
    {
        if (!UsesPointEncounters || _encounterState == CombatEncounterState.Lost) return;
        if (IsDefeated) { FinishEncounterLost((long)Math.Ceiling(_sessionOutcome.DeathMotionTimeMs!.Value)); return; }
        var alive = _objects.Where(EncounterEnemy).ToArray();
        var player = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId && !o.IsDestroyed);
        if (player is null) return;
        double range = _registry.CombatRules!.CombatStartRangeKm!.Value;
        bool near = alive.Any(o => CombatRangeMath.IsWithinRange(RuntimeMotion.At(player, time), RuntimeMotion.At(o, time), range));
        if (_encounterState is CombatEncounterState.Idle or CombatEncounterState.Won)
        {
            if (near) { StartEncounter(time); return; }
            if (_encounterState == CombatEncounterState.Won)
            {
                foreach (var enemy in alive)
                    if (_encounterEnemyIds.Add(enemy.InitialMotion.ObjectId)) _encounterQuietSince = time;
                if (time - _encounterQuietSince >= 60000) _encounterState = CombatEncounterState.Idle;
            }
        }
        else if (alive.Length == 0)
        {
            _encounterState = CombatEncounterState.Won;
            _encounterEndedAt = time;
            _encounterQuietSince = time;
            RestoreEncounterSpeed();
        }
    }

    private void FinishEncounterLost(long time)
    {
        if (!UsesPointEncounters || _encounterState == CombatEncounterState.Lost) return;
        if (_encounterState == CombatEncounterState.Idle) _encounterStartedAt = time;
        _encounterState = CombatEncounterState.Lost;
        _encounterEndedAt = time;
        _encounterResumeSpeed = null;
    }

    private void RestoreEncounterSpeed()
    {
        if (_encounterResumeSpeed is not { } speed || IsDefeated) return;
        if (_dialogue.Active is not null) _dialogue.ResumeSpeed = speed;
        else if (!_automaticPresentationPause) _clock.SetSpeedAtProcessedBoundary(speed);
        _encounterResumeSpeed = null;
    }

    private SimulationClockState AdvancePointClockWorld(long? realDeadline = null)
    {
        if (_advancingPointClock) return _clock.Capture();
        _advancingPointClock = true;
        try
        {
            long deadline = Math.Min(realDeadline ?? long.MaxValue, _clock.CaptureRealTime());
            EvaluateEncounter(_combatProcessedTimeMs);
            bool more;
            do
            {
                // Only an encounter-capable enemy can require a mid-interval speed clamp.
                // With none present, World advancement still processes calendar/motion deadlines,
                // and a long peaceful real-time gap must not become millions of1ms updates.
                long limit = _encounterState != CombatEncounterState.Active && _clock.Speed > SimulationSpeed.Speed1 && _objects.Any(EncounterEnemy) ? 1 : long.MaxValue;
                // A blast can end the encounter inside this interval and restore the previous pace.
                // Stop the clock interval at that event so its remaining real time uses the restored speed.
                if (_clock.Speed != SimulationSpeed.Speed0 && _encounterState == CombatEncounterState.Active)
                {
                    var current = _clock.Capture();
                    double untilBlast = Math.Ceiling(((NextPointDetonationTime() - current.MotionTimeMs) * 4 - current.MotionQuarterRemainder) / (int)_clock.Speed);
                    if (double.IsFinite(untilBlast) && untilBlast < long.MaxValue) limit = Math.Max(1, (long)untilBlast);
                }
                var next = _clock.AdvanceBounded(deadline, limit);
                more = next.More;
                // Explicit test snapshots can be ahead of the injected clock; never rewind a processed world.
                if (next.State.MotionTimeMs >= _combatProcessedTimeMs && next.State.GameTimeMs >= _processedWorldTimeMs)
                    AdvanceWorldTo(next.State.GameTimeMs, next.State.MotionTimeMs, processPlayerAtEndpoint: !more);
                EvaluateEncounter(_combatProcessedTimeMs);
            } while (more && !IsDefeated);
            return _clock.Capture();
        }
        finally { _advancingPointClock = false; }
    }

    private long NextEncounterBoundary(long from, long to)
    {
        if (!UsesPointEncounters) return to;
        if (_encounterState == CombatEncounterState.Won && _encounterQuietSince <= long.MaxValue - 60000)
            to = Math.Min(to, _encounterQuietSince + 60000);
        if (_encounterState is not (CombatEncounterState.Idle or CombatEncounterState.Won)) return to;
        var player = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId && !o.IsDestroyed);
        if (player is null) return to;
        foreach (var enemy in _objects.Where(EncounterEnemy))
        {
            var entry = TorpedoCollisionMath.FirstContact(CombatPath(player), CombatPath(enemy), from, to,
                _registry.CombatRules!.CombatStartRangeKm!.Value * 10);
            if (entry is { } contact && contact.MotionTimeMs > from && contact.MotionTimeMs < long.MaxValue)
                to = Math.Min(to, (long)Math.Ceiling(contact.MotionTimeMs));
        }
        return to;
    }

    private CommandStartOutcome AcknowledgeEncounter(PlayerCommand command, long time)
    {
        if (command.ObjectId != PlayerShipObjectId) return CommandStartOutcome.Rejected(CommandReasonCodes.UnknownObject);
        EvaluateEncounter(time);
        if (_encounterState == CombatEncounterState.Won) _encounterState = CombatEncounterState.Idle;
        RecordCommandResult(command, CommandResultStatus.Executed, time);
        return CommandStartOutcome.Started;
    }

    private CombatEncounterSnapshot? CaptureEncounter(long time) => !UsesPointEncounters ? null : new(
        _encounterState, _encounterStartedAt, _encounterEndedAt, _encounterStats,
        _encounterState == CombatEncounterState.Idle && _encounterEndedAt is null ? 0 :
            Math.Max(0, (_encounterEndedAt ?? time) - _encounterStartedAt) / 1000.0);
}
