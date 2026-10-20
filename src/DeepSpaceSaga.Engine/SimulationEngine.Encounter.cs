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
    private bool UsesPointEncounters => _registry.CombatRules?.IsPointAim == true;

    private void ResetEncounter(long time)
    {
        _encounterState = CombatEncounterState.Idle;
        _encounterStartedAt = time;
        _encounterEndedAt = null;
        _encounterQuietSince = time;
        _encounterEnemyIds.Clear();
    }

    private bool EncounterEnemy(SpaceObjectRuntime ship) => IsHostileActor(ship) &&
        (ship.AttackAi is not null || _encounterEnemyIds.Contains(ship.InitialMotion.ObjectId));

    private void StartEncounter(long time, string? launchingOwner = null)
    {
        if (!UsesPointEncounters || _encounterState is CombatEncounterState.Active or CombatEncounterState.Lost || IsDefeated) return;
        _encounterState = CombatEncounterState.Active;
        _encounterStartedAt = time;
        _encounterEndedAt = null;
        _encounterEnemyIds.Clear();
        foreach (var enemy in _objects.Where(o => IsHostileActor(o) && o.AttackAi is not null)) _encounterEnemyIds.Add(enemy.InitialMotion.ObjectId);
        if (launchingOwner is not null) _encounterEnemyIds.Add(launchingOwner);
    }

    private void ObserveEncounterFact(CombatJournalEntry entry)
    {
        if (!UsesPointEncounters || entry.Type != CombatEventType.Launch || entry.ProjectileKind != ProjectileKind.Torpedo) return;
        var owner = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == entry.ActorObjectId && IsHostileActor(o));
        if (owner is null) return;
        long time = (long)Math.Ceiling(entry.MotionTimeMs);
        if (_encounterState != CombatEncounterState.Active) StartEncounter(time, owner.InitialMotion.ObjectId);
        _encounterEnemyIds.Add(owner.InitialMotion.ObjectId);
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
        }
    }

    private void FinishEncounterLost(long time)
    {
        if (!UsesPointEncounters || _encounterState == CombatEncounterState.Lost) return;
        if (_encounterState == CombatEncounterState.Idle) _encounterStartedAt = time;
        _encounterState = CombatEncounterState.Lost;
        _encounterEndedAt = time;
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
        _encounterState, _encounterStartedAt, _encounterEndedAt, new(),
        _encounterState == CombatEncounterState.Idle && _encounterEndedAt is null ? 0 :
            Math.Max(0, (_encounterEndedAt ?? time) - _encounterStartedAt) / 1000.0);
}
