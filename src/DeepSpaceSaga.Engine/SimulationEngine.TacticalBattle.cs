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
    private readonly Dictionary<string, (string Signature, TacticalBattleResponse Response)> _tacticalReceipts = new(StringComparer.Ordinal);
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
            return response;
        });
    }

    private TacticalBattleResponse ExecuteTactical<T>(string commandId, T request, Func<TacticalBattleResponse> apply)
    {
        lock (_worldStateLock)
        {
            if (string.IsNullOrWhiteSpace(commandId)) return RejectTactical(commandId ?? "", TacticalBattleRejectionCode.InvalidIdentity);
            string signature = typeof(T).Name + ":" + JsonSerializer.Serialize(request, TacticalIdentityJson);
            if (_tacticalReceipts.TryGetValue(commandId, out var previous))
                return previous.Signature == signature ? previous.Response : RejectTactical(commandId, TacticalBattleRejectionCode.IdentityConflict);
            var response = apply();
            _tacticalReceipts.Add(commandId, (signature, response));
            return response;
        }
    }
    private TacticalBattleResponse RejectTactical(string commandId, TacticalBattleRejectionCode code) =>
        _tacticalBattleSession?.Reject(commandId, code) ?? new(commandId, false, code, null, null, []);
}
