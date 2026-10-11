using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private sealed class DefenseSubmission
    {
        public string? CommandId;
        public string? TargetId;
        public Task? SendTask;
    }

    private readonly Dictionary<(string Owner, string Module), DefenseSubmission> _defenseSubmissions = new();
    private readonly Dictionary<(string Owner, string Module), string> _defenseSubmissionErrors = new();

    private void RefreshDefenseSubmissions()
    {
        foreach (var (key, submission) in _defenseSubmissions.ToArray())
        {
            var result = submission.CommandId is { } id ? _buffer.FindCommandResult(id) : null;
            if (result is { Status: not CommandResultStatus.Deferred } && result.ObjectId == key.Owner && result.ModuleId == key.Module)
            {
                _defenseSubmissions.Remove(key);
                if (result.Status != CommandResultStatus.Executed) _defenseSubmissionErrors[key] = result.ReasonCode ?? "command_rejected";
                continue;
            }
            if (submission.SendTask is { IsFaulted: true } or { IsCanceled: true })
            {
                _ = submission.SendTask.Exception;
                _defenseSubmissions.Remove(key);
                _defenseSubmissionErrors[key] = "send_failed";
            }
        }
    }

    private InstalledModuleSnapshot? DefenseModule(WeaponPanelInstance weapon) =>
        _buffer.Latest?.Snapshot is { InstalledModules.IsDefaultOrEmpty: false } snapshot &&
        snapshot.PlayerShipObjectId == weapon.OwnerObjectId
            ? snapshot.InstalledModules.FirstOrDefault(m => m.ModuleId == weapon.ModuleId && m.Defense is not null) : null;

    private bool IsDefenseCommandEnabled(WeaponPanelInstance weapon, string commandType)
    {
        RefreshDefenseSubmissions();
        var snapshot = _buffer.Latest?.Snapshot;
        if (snapshot is null || snapshot.ActiveDialogue is not null || FindPlayerShipMotion(snapshot) is not { IsDestroyed: false } ||
            _defenseSubmissions.ContainsKey((weapon.OwnerObjectId, weapon.ModuleId)) ||
            DefenseModule(weapon) is not { Defense: { } defense } module || module.Commands.IsDefaultOrEmpty) return false;
        var command = module.Commands.FirstOrDefault(c => c.CommandTypeId == commandType);
        if (command is null) return false;
        if (commandType is DefenseCommandTypes.Enable or DefenseCommandTypes.Disable)
            return command.Target == "none" && defense.AutoEnabled != (commandType == DefenseCommandTypes.Enable);
        if (commandType != DefenseCommandTypes.Fire || command.Target != "object" || _selectedFieldId is not null || _selectedPoiId is not null)
            return false;
        return module is { PowerState: "On", OperationalState: "Ready", StructurePoints: > 0, ActiveCommandType: null } &&
            module.DefensePreview is { CanFire: true, TargetTorpedoId: { } target } preview &&
            preview.OwnerObjectId == weapon.OwnerObjectId && preview.ModuleId == weapon.ModuleId && target == _selectedObjectId &&
            snapshot.Objects.Any(o => o.ObjectId == target && !o.IsDestroyed && o.Torpedo is not null);
    }

    private void SendDefenseCommand(WeaponPanelInstance weapon, string commandType)
    {
        if (_handle is null || !IsDefenseCommandEnabled(weapon, commandType)) return;
        var key = (weapon.OwnerObjectId, weapon.ModuleId);
        var submission = new DefenseSubmission
        { TargetId = commandType == DefenseCommandTypes.Fire ? DefenseModule(weapon)!.DefensePreview!.TargetTorpedoId : null };
        _defenseSubmissions[key] = submission;
        _defenseSubmissionErrors.Remove(key);
        try
        {
            submission.SendTask = _handle.SendCommandAsync(weapon.OwnerObjectId, weapon.ModuleId,
                commandType, out submission.CommandId, submission.TargetId).AsTask();
        }
        catch (Exception)
        {
            _defenseSubmissions.Remove(key);
            _defenseSubmissionErrors[key] = "send_failed";
        }
    }

    private string? GetDefenseInstanceStatus(WeaponPanelInstance weapon)
    {
        RefreshDefenseSubmissions();
        var key = (weapon.OwnerObjectId, weapon.ModuleId);
        if (_defenseSubmissions.ContainsKey(key)) return "Sending...";
        if (_defenseSubmissionErrors.TryGetValue(key, out var error)) return error == "send_failed" ? "Send failed" : error;
        return DefenseModule(weapon)?.Defense is { } defense ? DefenseStatusText(defense, DefensePresentationTime) : null;
    }

    private string? GetDefenseInstanceOperatorText(WeaponPanelInstance weapon) => WeaponOperatorText(DefenseModule(weapon)?.Defense?.Operator);
}
