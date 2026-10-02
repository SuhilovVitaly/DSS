using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private bool _torpedoSubmitPending;
    private string? _pendingTorpedoCommandId;
    private Task? _torpedoSendTask;
    private bool _torpedoSendFailed;

    private void RefreshTorpedoSubmission()
    {
        if (_torpedoSendTask is { IsFaulted: true }) _ = _torpedoSendTask.Exception;
        if (!_torpedoSubmitPending) return;
        var result = _pendingTorpedoCommandId is { } id ? _buffer.FindCommandResult(id) : null;
        if (result is { Status: not CommandResultStatus.Deferred })
        {
            _torpedoSubmitPending = false;
            return;
        }
        if (_torpedoSendTask is { IsFaulted: true } or { IsCanceled: true })
        {
            _ = _torpedoSendTask.Exception;
            _torpedoSendFailed = true;
            _torpedoSubmitPending = false;
        }
    }

    private void SendTorpedoFire()
    {
        if (_handle is null || !IsTorpedoFireEnabled()) return;
        var snapshot = _buffer.Latest!.Snapshot;
        string moduleId = FindLauncher()!.ModuleId;
        string? targetId = _selectedObjectId;
        _torpedoSubmitPending = true;
        _torpedoSendFailed = false;
        _pendingTorpedoCommandId = null;
        _torpedoSendTask = null;
        try
        {
            _torpedoSendTask = _handle.SendCommandAsync(snapshot.PlayerShipObjectId!, moduleId,
                CombatCommandTypes.Fire, out _pendingTorpedoCommandId, targetId).AsTask();
        }
        catch (Exception)
        {
            _torpedoSendFailed = true;
            _torpedoSubmitPending = false;
        }
    }

    internal string? HoveredCommandTypeId => _commandsPanel.HoveredCommandTypeId;

    private InstalledModuleSnapshot? FindLauncher()
    {
        string? moduleId = ResolveModuleId(CombatCommandTypes.Fire);
        var modules = _buffer.Latest?.Snapshot.InstalledModules;
        return moduleId is null || modules is null || modules.Value.IsDefaultOrEmpty
            ? null : modules.Value.FirstOrDefault(m => m.ModuleId == moduleId);
    }

    private bool IsTorpedoFireEnabled()
    {
        RefreshTorpedoSubmission();
        if (_torpedoSubmitPending) return false;
        var snapshot = _buffer.Latest?.Snapshot;
        if (snapshot is null || snapshot.ActiveDialogue is not null ||
            FindPlayerShipMotion(snapshot) is not { IsDestroyed: false } ||
            _selectedObjectId is null || _selectedObjectId == snapshot.PlayerShipObjectId)
            return false;
        return snapshot.Objects.Any(o => o.ObjectId == _selectedObjectId && !o.IsDestroyed) &&
            FindLauncher() is
            {
                PowerState: "On", OperationalState: "Ready", StructurePoints: > 0,
                ActiveCommandType: null, LauncherCombat.ActiveTorpedoObjectId: null
            } launcher && !launcher.Commands.IsDefaultOrEmpty &&
            launcher.Commands.Any(c => c.CommandTypeId == CombatCommandTypes.Fire && c.Target == "object");
    }

    private string? GetLauncherStatus()
    {
        RefreshTorpedoSubmission();
        if (_torpedoSubmitPending) return "Sending...";
        if (_torpedoSendFailed) return "Send failed";
        return FindLauncher() switch
        {
            { LauncherCombat.ActiveTorpedoObjectId: not null } => "Guiding",
            { PowerState: "On", OperationalState: "Ready", StructurePoints: > 0, LauncherCombat: not null } => "Ready",
            { LauncherCombat: not null } => "Unavailable",
            _ => null
        };
    }
}
