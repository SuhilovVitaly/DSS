using DeepSpaceSaga.Contracts;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    internal string? LastManeuverCommandId { get; private set; }
    internal string? ManeuverFeedback { get; private set; }
    private (string Owner, string Module, TurnDirection Direction)? _heldTurn;
    private readonly HashSet<Key> _heldFullSpeed = [];
    private Guid? _maneuverSession;
    private bool _maneuverInitialized;
    private Task? _maneuverSendTask;

    private static string ManeuverError(string reason) => Localization.Combat("Panel.Reason." + reason);

    private bool ManeuverEngineReady(InstalledModuleSnapshot? engine) =>
        engine is { IsDestroyed: false, PowerState: "On", OperationalState: "Ready" } &&
        (engine.StructureMax > 0 ? engine.StructureCurrent > 0 : engine.StructurePoints > 0) &&
        !(engine.DisabledUntilMotionTimeMs is { } until && until > (_buffer.Presented?.Snapshot.MotionTimeMs ?? 0));

    private void RefreshManeuverInput()
    {
        var session = _buffer.PresentationSessionId;
        if (!_maneuverInitialized || session != _maneuverSession)
        {
            CancelManeuverInput(sendReleases: false);
            LastManeuverCommandId = null;
            ManeuverFeedback = null;
            _maneuverSession = session;
            _maneuverInitialized = true;
            _ = _maneuverSendTask?.Exception;
            _maneuverSendTask = null;
        }
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
            snapshot.EffectiveOutcome.State == SessionOutcomeState.Defeated ||
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
        if (_handle is null || _maneuverSession != _buffer.PresentationSessionId) return;
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
