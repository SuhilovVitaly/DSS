using DeepSpaceSaga.Contracts;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    internal string? LastManeuverCommandId { get; private set; }
    internal string? ManeuverFeedback { get; private set; }
    private (string Owner, string Module, TurnDirection Direction)? _heldTurn;
    private readonly HashSet<Key> _heldFullSpeed = [];
    private Task? _maneuverSendTask;

    private static string ManeuverError(string reason) => Localization.Combat("Panel.Reason." + reason);

    private bool ManeuverEngineReady(InstalledModuleSnapshot? engine) =>
        engine is { IsDestroyed: false, PowerState: "On", OperationalState: "Ready" } &&
        (engine.StructureMax > 0 ? engine.StructureCurrent > 0 : engine.StructurePoints > 0) &&
        !(engine.DisabledUntilMotionTimeMs is { } until && until > (_buffer.Presented?.Snapshot.MotionTimeMs ?? 0));

    private void RefreshManeuverInput()
    {
        if (_maneuverSendTask is { IsFaulted: true } or { IsCanceled: true })
        {
            _ = _maneuverSendTask.Exception;
            ManeuverFeedback = ManeuverError("send_failed");
            _maneuverSendTask = null;
        }
        if (LastManeuverCommandId is { } id &&
            _buffer.FindCommandResult(id) is { Status: CommandResultStatus.Rejected or CommandResultStatus.Failed } rejected)
            ManeuverFeedback = ManeuverError(rejected.ReasonCode ?? "command_rejected");
        if (_buffer.Latest?.Snapshot is not { ActiveDialogue: null } snapshot ||
            snapshot.Objects.Any(o => o.ObjectId == snapshot.PlayerShipObjectId && o.IsDocked))
        {
            CancelManeuverInput(sendReleases: false);
            return;
        }
        ReconcileManualAxes();
    }

    private bool HandleManeuverKey(Key key)
    {
        RefreshManeuverInput();
        if (key == Key.Escape)
        {
            CancelManeuverInput();
            return false;
        }
        return !IsCtrlDown && HandleManualAxisKey(key);
    }

    private void ReleaseManeuverKey(Key key)
    {
        _heldFullSpeed.Remove(key);
        if (_turnAxisKeys.Remove(key) | _speedAxisKeys.Remove(key)) ReconcileManualAxes();
    }

    private void SendHeldTurn(string owner, string module, TurnDirection direction, bool active)
    {
        if (_handle is null) return;
        try
        {
            _maneuverSendTask = _handle.SendTurnHoldAsync(owner, module, direction, active, out var id).AsTask();
            LastManeuverCommandId = id;
        }
        catch (Exception) { ManeuverFeedback = ManeuverError("send_failed"); }
    }

    private void CancelManeuverInput(bool sendReleases = true)
    {
        _heldFullSpeed.Clear();
        _turnAxisKeys.Clear();
        _speedAxisKeys.Clear();
        _speedLatchRequested = false;
        if (_heldTurn is { } turn && sendReleases) SendHeldTurn(turn.Owner, turn.Module, turn.Direction, false);
        if (_heldSpeed is { } speed && sendReleases) SendHeldSpeed(speed.Owner, speed.Module, speed.Direction, false, false);
        _heldTurn = null;
        _heldSpeed = null;
    }
}
