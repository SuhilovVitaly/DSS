using System.Text.Json;
using System.Text.Json.Serialization;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client;

public sealed partial class GameSessionHandle
{
    private static readonly JsonSerializerOptions TacticalLogJson = new() { NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals };

    public async ValueTask<TacticalBattleOptions> GetTacticalBattleOptionsAsync(CancellationToken cancellationToken = default)
    {
        try { return await _connection.GetTacticalBattleOptionsAsync(cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception error)
        {
            WriteDiagnostic($"TACTICAL options failed: {error}");
            return new(new(false, new("", "connection", error.Message)), [], [], [], [], []);
        }
    }
    public async ValueTask<TacticalBattleSnapshot?> GetTacticalBattleStateAsync(CancellationToken cancellationToken = default)
    {
        try { return await _connection.GetTacticalBattleStateAsync(cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception error) { WriteDiagnostic($"TACTICAL state failed: {error}"); return null; }
    }
    /// <summary>
    /// Wait for the first authoritative carrier surface on the snapshot stream. The owned
    /// Tutorial/F8 entry needs the player's own <c>ShipId</c>/revision before it may build a start
    /// request. A producer without the carrier surface never publishes one, so the bounded wait
    /// returns null instead of hanging or fabricating an owned wing.
    /// </summary>
    internal async ValueTask<CarrierOperationsSnapshot?> WaitForCarrierOperationsAsync(
        TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        long deadline = Environment.TickCount64 + Math.Max(0, (long)timeout.TotalMilliseconds);
        while (true)
        {
            if (CarrierOperations is { } carrier) return carrier;
            if (Failure is not null) return null;
            long remaining = deadline - Environment.TickCount64;
            if (remaining <= 0) return null;
            await Task.Delay((int)Math.Min(remaining, 25), cancellationToken).ConfigureAwait(false);
        }
    }
    public ValueTask<TacticalBattleResponse> StartTacticalBattleAsync(StartTacticalBattleRequest request, CancellationToken cancellationToken = default) =>
        SendTactical("start", request.CommandId, request, () => _connection.StartTacticalBattleAsync(request, cancellationToken), cancellationToken);
    public ValueTask<TacticalBattleResponse> MoveTacticalBattleAsync(MoveTacticalBattleRequest request, CancellationToken cancellationToken = default) =>
        SendTactical("move", request.CommandId, request, () => _connection.MoveTacticalBattleAsync(request, cancellationToken), cancellationToken);
    public ValueTask<TacticalBattleResponse> FireTacticalBattleAsync(FireTacticalBattleRequest request, CancellationToken cancellationToken = default) =>
        SendTactical("fire", request.CommandId, request, () => _connection.FireTacticalBattleAsync(request, cancellationToken), cancellationToken);
    public ValueTask<TacticalBattleResponse> ExitTacticalBattleAsync(ExitTacticalBattleRequest request, CancellationToken cancellationToken = default) =>
        SendTactical("exit", request.CommandId, request, () => _connection.ExitTacticalBattleAsync(request, cancellationToken), cancellationToken);

    private async ValueTask<TacticalBattleResponse> SendTactical<T>(string operation, string id, T request,
        Func<ValueTask<TacticalBattleResponse>> send, CancellationToken cancellationToken)
    {
        WriteDiagnostic($"TACTICAL send {operation}: {JsonSerializer.Serialize(request, TacticalLogJson)}");
        TacticalBattleResponse response;
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            response = await send().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            WriteDiagnostic($"TACTICAL canceled {operation}: command={id}");
            throw;
        }
        catch (Exception error)
        {
            WriteDiagnostic($"TACTICAL transport failed {operation}: command={id}, {error}");
            response = new(id, false, TacticalBattleRejectionCode.Unavailable, null, null, [], new("", "connection", error.Message));
        }
        WriteDiagnostic($"TACTICAL {(response.Accepted ? "accepted" : "rejected")} {operation}: command={response.CommandId}, code={response.Code}, phase={response.CurrentPhase?.ToString() ?? "none"}");
        return response;
    }

    public static string TacticalBattleRefusalText(TacticalBattleRejectionCode code) => code == TacticalBattleRejectionCode.None ? ""
        : Localization.Get("TacticalBattle.Refusal." + (Enum.IsDefined(code) ? code : TacticalBattleRejectionCode.Unavailable));
}
