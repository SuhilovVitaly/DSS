using DeepSpaceSaga.Contracts;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private readonly List<Key> _turnAxisKeys = [], _speedAxisKeys = [];
    private bool _speedLatchRequested;
    private (string Owner, string Module, SpeedDirection Direction, bool Latched, string CommandId)? _heldSpeed;

    private bool HandleManualAxisKey(Key key)
    {
        if (CombatInputs.FullSpeed.Contains(key))
        {
            if (_heldFullSpeed.Add(key) && _heldFullSpeed.Count == 1)
            {
                _speedAxisKeys.Clear(); _speedLatchRequested = true;
                _heldEvade = null; CurrentEvadePreview = null; ReconcileManualAxes();
            }
            return true;
        }
        if (CombatInputs.TurnLeft.Contains(key) || CombatInputs.TurnRight.Contains(key))
        {
            if (!_turnAxisKeys.Contains(key)) _turnAxisKeys.Add(key);
            _heldEvade = null; CurrentEvadePreview = null; ReconcileManualAxes(); return true;
        }
        if (CombatInputs.Accelerate.Contains(key) || CombatInputs.Brake.Contains(key))
        {
            if (!_speedAxisKeys.Contains(key))
            {
                _speedAxisKeys.Add(key); _speedLatchRequested = false;
                _heldEvade = null; CurrentEvadePreview = null; ReconcileManualAxes();
            }
            return true;
        }
        return false;
    }

    private void ReconcileManualAxes()
    {
        if (_turnAxisKeys.Count == 0 && _speedAxisKeys.Count == 0 && !_speedLatchRequested && _heldTurn is null && _heldSpeed is null) return;
        if (_handle is null || _handle.Failure is not null || _buffer.Latest?.Snapshot is not { ActiveDialogue: null } snapshot ||
            snapshot.EffectiveOutcome.State == SessionOutcomeState.Defeated || snapshot.PlayerShipObjectId is not { } owner) return;
        if (snapshot.Objects.IsDefaultOrEmpty || snapshot.InstalledModules.IsDefaultOrEmpty) return;
        var ship = snapshot.Objects.FirstOrDefault(o => o.ObjectId == owner);
        if (ship is null || ship.IsDocked || ship.IsDestroyed) return;
        var engine = snapshot.InstalledModules.FirstOrDefault(m => m.ModuleTypeId.StartsWith("module.engine.", StringComparison.Ordinal));
        if (!ManeuverEngineReady(engine))
        {
            _heldTurn = null; _heldSpeed = null;
            if (_heldFullSpeed.Count == 0) _speedLatchRequested = false;
            return;
        }
        if (_speedLatchRequested && _heldSpeed is { Latched: true } pending &&
            _buffer.FindCommandResult(pending.CommandId) is { Status: CommandResultStatus.Executed } &&
            !ship.SpeedHoldLatched && ship.MaxSpeedKmS is { } maximum && ship.SpeedKmS >= maximum - 1e-9)
        { _speedLatchRequested = false; _heldSpeed = null; }
        TurnDirection? turn = _turnAxisKeys.Count == 0 ? null : CombatInputs.TurnLeft.Contains(_turnAxisKeys[^1]) ? TurnDirection.Left : TurnDirection.Right;
        if (_heldTurn?.Direction != turn)
        {
            if (turn is { } direction)
            {
                _heldTurn = (owner, engine!.ModuleId, direction); SendHeldTurn(owner, engine.ModuleId, direction, true);
            }
            else if (_heldTurn is { } previous)
            {
                _heldTurn = null; SendHeldTurn(previous.Owner, previous.Module, previous.Direction, false);
            }
        }
        SpeedDirection? speed = _speedLatchRequested ? SpeedDirection.Accelerate : _speedAxisKeys.Count == 0 ? null :
            CombatInputs.Accelerate.Contains(_speedAxisKeys[^1]) ? SpeedDirection.Accelerate : SpeedDirection.Brake;
        if (_heldSpeed?.Direction == speed && (_heldSpeed is null || _heldSpeed.Value.Latched == _speedLatchRequested)) return;
        if (speed is { } speedDirection)
            SendHeldSpeed(owner, engine!.ModuleId, speedDirection, true, _speedLatchRequested);
        else if (_heldSpeed is { } previous)
        {
            _heldSpeed = null; SendHeldSpeed(previous.Owner, previous.Module, previous.Direction, false, false);
        }
    }

    private void SendHeldSpeed(string owner, string module, SpeedDirection direction, bool active, bool latched)
    {
        if (_handle is null || _maneuverSession != _buffer.PresentationSessionId) return;
        try
        {
            _maneuverSendTask = _handle.SendSpeedHoldAsync(owner, module, direction, active, latched, out string id).AsTask();
            LastManeuverCommandId = id;
            if (active) _heldSpeed = (owner, module, direction, latched, id);
        }
        catch (Exception) { ManeuverFeedback = PointLaunchReason("send_failed"); }
    }
}
