using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.TacticalBattle;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    public const string TacticalBattleActiveReason = "tactical_battle_active";
    private void ThrowIfTacticalBattleActive()
    {
        if (_tacticalBattleSession is not null) throw new InvalidOperationException(TacticalBattleActiveReason);
    }
    private TacticalBattleSession? _tacticalBattleSession;
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
        lock (_worldStateLock) return _tacticalBattleSession?.Capture();
    }

    public TacticalBattleResponse StartTacticalBattle(StartTacticalBattleRequest request)
    {
        if (request is null) { lock (_worldStateLock) return RejectTactical("", TacticalBattleRejectionCode.InvalidRequest); }
        return ExecuteTactical(request.CommandId, request, () =>
        {
            if (_tacticalBattleSession is not null) return RejectTactical(request.CommandId, TacticalBattleRejectionCode.AlreadyActive);
            if (_registry.TacticalBattle.Catalog is not { } catalog)
            {
                var error = _registry.TacticalBattle.Error;
                return new(request.CommandId, false, TacticalBattleRejectionCode.Unavailable, null, null, [],
                    error is null ? null : new(error.File, error.Field, error.Message));
            }
            if (_clock.Speed != SimulationSpeed.Speed0)
                return RejectTactical(request.CommandId, TacticalBattleRejectionCode.WorldNotPaused);
            var session = TacticalBattleSession.TryCreate(catalog, request, out var code);
            if (session is null) return RejectTactical(request.CommandId, code);
            _tacticalBattleSession = session;
            SetTacticalBattleCommandBlock(true);
            return session.Initial(request.CommandId);
        });
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
    public TacticalBattleResponse ExitTacticalBattle(ExitTacticalBattleRequest request)
    {
        if (request is null) { lock (_worldStateLock) return RejectTactical("", TacticalBattleRejectionCode.InvalidRequest); }
        return ExecuteTactical(request.CommandId, request, () =>
        {
            if (_tacticalBattleSession is null) return RejectTactical(request.CommandId, TacticalBattleRejectionCode.NoBattle);
            if (_tacticalBattleSession.Id != request.BattleId) return RejectTactical(request.CommandId, TacticalBattleRejectionCode.WrongBattle);
            var response = _tacticalBattleSession.Exit(request.CommandId);
            _tacticalBattleSession = null;
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
        _tacticalBattleSession?.Reject(commandId, code) ?? new(commandId, false, code, null, null, []);
}
