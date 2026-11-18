using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Content;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    public const string TacticalBattleActiveReason = "tactical_battle_active";
    private void ThrowIfTacticalBattleActive()
    {
        if (_tacticalBattleSession is not null) throw new InvalidOperationException(TacticalBattleActiveReason);
    }
    private TacticalBattleSession? _tacticalBattleSession;
    private string? _tacticalBattleSourceShipId;
    // ReceiveCommand's view of the battle, guarded by _commandGate so sending a world command never
    // waits for _worldStateLock. Written only together with _tacticalBattleSession (world lock held).
    private bool _tacticalBattleCommandBlock;
    private long _tacticalBattleCommandTimeMs;
    // World commands refused while the battle is active. Their results are recorded under the world
    // lock by the next ApplyPendingCommands, even if the battle has ended in between.
    private readonly List<(PlayerCommand Command, long TimeMs)> _tacticalBattleRefusedCommands = new();
    private const int TacticalResponseLimit = 16;
    // Keep identities for the Engine lifetime, independently of the snapshots needed for recent retries.
    private readonly Dictionary<string, (Type RequestType, string Fingerprint)> _tacticalIdentities = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TacticalBattleResponse> _tacticalResponses = new(StringComparer.Ordinal);
    private readonly Queue<string> _tacticalResponseOrder = new();
    private static readonly JsonSerializerOptions TacticalIdentityJson = new() { NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals };
    public bool IsTacticalBattleActive { get { lock (_worldStateLock) return _tacticalBattleSession is not null; } }
    internal TacticalBattleSession? TacticalBattleSessionForTests => _tacticalBattleSession;

    public TacticalBattleOptions GetTacticalBattleOptions()
    {
        lock (_worldStateLock)
        {
            var availability = _registry.TacticalBattle;
            if (availability.Catalog is { } catalog) return TacticalBattleSnapshotBuilder.Options(catalog);
            var error = availability.Error;
            return new(new(false, error is null ? null : new(error.File, error.Field, error.Message)), [], [], [], [], []);
        }
    }
    public TacticalBattleSnapshot? GetTacticalBattleState()
    {
        lock (_worldStateLock) return _tacticalBattleSession?.Capture() ?? (LootOpen ? _demoCampaign.LootSession!.Map : null);
    }

    public TacticalBattleResponse StartTacticalBattle(StartTacticalBattleRequest request)
    {
        if (request is null) { lock (_worldStateLock) return RejectTactical("", TacticalBattleRejectionCode.InvalidRequest); }
        return ExecuteTactical(request.CommandId, request, () =>
        {
            if (CombatOrLootOpen) return RejectTactical(request.CommandId, TacticalBattleRejectionCode.AlreadyActive);
            if (HasCapturedEncounter || _encounterStoragePending) return RejectTactical(request.CommandId, TacticalBattleRejectionCode.WrongPhase);
            if (_registry.TacticalBattle.Catalog is not { } catalog)
            {
                var error = _registry.TacticalBattle.Error;
                return new(request.CommandId, false, TacticalBattleRejectionCode.Unavailable, null, null, [],
                    error is null ? null : new(error.File, error.Field, error.Message));
            }
            if (_clock.Speed != SimulationSpeed.Speed0)
                return RejectTactical(request.CommandId, TacticalBattleRejectionCode.WorldNotPaused);
            if (request.SourceShipId is not null)
                return StartOwnedTacticalBattle(request, catalog);
            var session = TacticalBattleSession.TryCreate(catalog, request, out var code);
            if (session is null) return RejectTactical(request.CommandId, code);
            _tacticalBattleSession = session;
            SetTacticalBattleCommandBlock(true);
            return session.Initial(request.CommandId);
        });
    }
    private TacticalBattleResponse StartOwnedTacticalBattle(StartTacticalBattleRequest request, TacticalBattleCatalog catalog)
    {
        string sourceShipId = request.SourceShipId!; // routing guarantees non-null here
        // Ownership: the owned sortie must launch from the world's actual player ship.
        if (sourceShipId != PlayerShipObjectId)
            return RejectTactical(request.CommandId, TacticalBattleRejectionCode.WrongShip);
        // Optimistic concurrency: the caller must present the current carrier revision (0 is not a wildcard).
        if (request.ExpectedCarrierRevision != GetCarrierRevision(sourceShipId))
            return RejectTactical(request.CommandId, TacticalBattleRejectionCode.StaleCarrierState);
        // Ship must be alive.
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == sourceShipId);
        if (ship is null || ship.IsDestroyed)
            return RejectTactical(request.CommandId, TacticalBattleRejectionCode.CarrierNotReady);
        // An owned sortie never accepts a public equipment override.
        if (!request.Loadout.IsDefaultOrEmpty)
            return RejectTactical(request.CommandId, TacticalBattleRejectionCode.InvalidRequest);

        string scenarioId = string.IsNullOrWhiteSpace(request.ScenarioId) ? catalog.Scenarios.Entries[0].Id : request.ScenarioId;
        if (!catalog.Scenarios.Contains(scenarioId))
            return RejectTactical(request.CommandId, TacticalBattleRejectionCode.UnknownScenario);
        var scenario = catalog.Scenarios[scenarioId];
        string playerSide = scenario.Goal?.PlayerSide ?? "A";
        string enemySide = playerSide == "A" ? "B" : "A";
        string ownTactic = string.IsNullOrWhiteSpace(request.OwnTacticId) ? scenario.Sides.First(s => s.Id == playerSide).Tactic : request.OwnTacticId;
        string enemyTactic = string.IsNullOrWhiteSpace(request.EnemyTacticId) ? scenario.Sides.First(s => s.Id == enemySide).Tactic : request.EnemyTacticId;
        if (!catalog.Ai.Tactics.Contains(ownTactic) || !catalog.Ai.Tactics.Contains(enemyTactic))
            return RejectTactical(request.CommandId, TacticalBattleRejectionCode.UnknownTactic);

        // Ready wing: a destroyed/tombstoned or disabled drone is never deployed, and a drone whose
        // hangar is not structurally intact, powered on and operationally ready cannot launch. The
        // assembly receives the FULL wing in stable DroneId order plus the deployable set, so an
        // unavailable drone leaves its encounter slot unused instead of shifting survivors onto
        // another pilot's alias. An empty deployable set refuses entry (never a fallback).
        var wing = GetOwnedWing(sourceShipId).OrderBy(d => d.DroneId, StringComparer.Ordinal).ToImmutableArray();
        var deployable = wing
            .Where(d => !d.Destroyed && !d.Disabled && ship.Crew.Any(c => c.Id == d.CrewId) && IsHangarReady(d.HangarModuleId))
            .Select(d => d.DroneId)
            .ToImmutableHashSet(StringComparer.Ordinal);
        if (deployable.IsEmpty)
            return RejectTactical(request.CommandId, TacticalBattleRejectionCode.NoReadyDrones);

        int hullMax = GetCarrierHullMax(sourceShipId);
        int hullCurrent = GetCarrierHullCurrent(sourceShipId, hullMax);
        int maneuver = GetShipManeuver(sourceShipId);
        var battle = OwnedBattleAssembly.Create(catalog, sourceShipId, hullCurrent, maneuver, wing,
            ship.Crew, scenario, scenarioId, request.Seed, playerSide, deployable);
        var session = TacticalBattleSession.Create(battle, request.CommandId, playerSide);
        _tacticalBattleSession = session;
        _tacticalBattleSourceShipId = sourceShipId;
        SetTacticalBattleCommandBlock(true);
        return session.Initial(request.CommandId);
    }
    public TacticalBattleResponse MoveTacticalBattle(MoveTacticalBattleRequest request)
    {
        if (request is null) { lock (_worldStateLock) return RejectTactical("", TacticalBattleRejectionCode.InvalidRequest); }
        return ExecuteTactical(request.CommandId, request, () => _tacticalBattleSession?.Move(request)
            ?? RejectTactical(request.CommandId, TacticalBattleRejectionCode.NoBattle));
    }
    public TacticalBattleResponse FireTacticalBattle(FireTacticalBattleRequest request)
    {
        if (request is null) { lock (_worldStateLock) return RejectTactical("", TacticalBattleRejectionCode.InvalidRequest); }
        return ExecuteTactical(request.CommandId, request, () => _tacticalBattleSession?.Fire(request)
            ?? RejectTactical(request.CommandId, TacticalBattleRejectionCode.NoBattle));
    }
    /// <summary>
    /// Debug-console force destroy of one drone (EP-0030 US-0003 TK-0002). Idempotent by CommandId
    /// like every tactical command; validation and the destroy rule live in <see cref="TacticalBattleSession.DebugDestroy"/>.
    /// </summary>
    public TacticalBattleResponse DebugDestroyTacticalUnit(DebugDestroyTacticalUnitRequest request)
    {
        if (request is null) { lock (_worldStateLock) return RejectTactical("", TacticalBattleRejectionCode.InvalidRequest); }
        return ExecuteTactical(request.CommandId, request, () => _tacticalBattleSession?.DebugDestroy(request)
            ?? RejectTactical(request.CommandId, TacticalBattleRejectionCode.NoBattle));
    }
    public TacticalBattleResponse ExitTacticalBattle(ExitTacticalBattleRequest request)
    {
        if (request is null) { lock (_worldStateLock) return RejectTactical("", TacticalBattleRejectionCode.InvalidRequest); }
        return ExecuteTactical(request.CommandId, request, () =>
        {
            if (LootOpen) return RejectTactical(request.CommandId, request.BattleId == _demoCampaign.LootSession!.BattleId
                ? TacticalBattleRejectionCode.WrongPhase : TacticalBattleRejectionCode.WrongBattle);
            if (_tacticalBattleSession is null) return RejectTactical(request.CommandId, TacticalBattleRejectionCode.NoBattle);
            if (_tacticalBattleSession.Id != request.BattleId) return RejectTactical(request.CommandId, TacticalBattleRejectionCode.WrongBattle);
            if (BeginPostBattleLoot() is { } newlySettled)
                return new TacticalBattleResponse(request.CommandId, true, TacticalBattleRejectionCode.None,
                    TacticalBattlePhase.Over, _demoCampaign.LootSession!.Map, [])
                { Settlement = newlySettled };
            var session = _tacticalBattleSession;
            if (HasCapturedEncounter && session.Battle.Result is null)
                return RejectTactical(request.CommandId, TacticalBattleRejectionCode.WrongPhase);
            SortieSettlementSnapshot? settlement = null;
            if (_tacticalBattleSourceShipId is not null)
            {
                var kind = session.Battle.Result is { } result
                    ? MapSortieExitKind(result.Kind, result.Reasons)
                    : SortieExitKind.Aborted;
                settlement = CommitSortieOutcome(session, kind);
            }
            var response = session.Exit(request.CommandId) with { Settlement = settlement };
            _tacticalBattleSession = null;
            _tacticalBattleSourceShipId = null;
            SetTacticalBattleCommandBlock(false);
            _tacticalResponses.Clear();
            _tacticalResponseOrder.Clear();
            return response;
        });
    }

    private TacticalBattleResponse ExecuteTactical<T>(string commandId, T request, Func<TacticalBattleResponse> apply)
    {
        lock (_worldStateLock)
        {
            if (string.IsNullOrWhiteSpace(commandId)) return RejectTactical(commandId ?? "", TacticalBattleRejectionCode.InvalidIdentity);
            string fingerprint = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(request, TacticalIdentityJson)));
            var identity = (typeof(T), fingerprint);
            if (_tacticalIdentities.TryGetValue(commandId, out var previous))
            {
                if (identity != previous) return RejectTactical(commandId, TacticalBattleRejectionCode.IdentityConflict);
                if (_tacticalResponses.TryGetValue(commandId, out var cached)) return cached;
                return new(commandId, false, TacticalBattleRejectionCode.ReceiptExpired, _tacticalBattleSession?.Phase, null, []);
            }
            var response = apply();
            if (BeginPostBattleLoot() is { } settlement)
                response = response with { Settlement = settlement, State = _demoCampaign.LootSession!.Map };
            _tacticalIdentities.Add(commandId, identity);
            if (_tacticalResponseOrder.Count == TacticalResponseLimit)
                _tacticalResponses.Remove(_tacticalResponseOrder.Dequeue());
            _tacticalResponses.Add(commandId, response);
            _tacticalResponseOrder.Enqueue(commandId);
            return response;
        }
    }
    // Caller holds _worldStateLock (lock order _worldStateLock -> _commandGate). The world is paused
    // for the whole battle, so the processed calendar time captured here stays current until exit.
    private void SetTacticalBattleCommandBlock(bool active)
    {
        lock (_commandGate)
        {
            _tacticalBattleCommandBlock = active;
            _tacticalBattleCommandTimeMs = _processedWorldTimeMs;
        }
    }

    // Caller holds _worldStateLock.
    private void RecordTacticalBattleRefusedCommands()
    {
        (PlayerCommand Command, long TimeMs)[] refused;
        lock (_commandGate)
        {
            if (_tacticalBattleRefusedCommands.Count == 0) return;
            refused = [.. _tacticalBattleRefusedCommands];
            _tacticalBattleRefusedCommands.Clear();
        }
        foreach (var (command, timeMs) in refused)
            RecordCommandResult(command, CommandResultStatus.Rejected, timeMs, TacticalBattleActiveReason);
    }

    private TacticalBattleResponse RejectTactical(string commandId, TacticalBattleRejectionCode code) =>
        _tacticalBattleSession?.Reject(commandId, code) ?? new(commandId, false, code,
            LootOpen ? TacticalBattlePhase.Over : null, LootOpen ? _demoCampaign.LootSession!.Map : null, []);
}
