using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    internal CountermeasurePlan? ManualDefensePreviewPlan { get; private set; }
    internal CombatTrajectoryProjector.Geometry? ManualDefensePreviewGeometry { get; private set; }
    internal WeaponPanelInstance? ManualDefensePreviewWeapon { get; private set; }
    internal string? ManualDefensePreviewMessage { get; private set; }
    private (AuthoritativeSnapshot Snapshot, WeaponPanelInstance Weapon)? _manualPreviewKey;

    private WeaponPanelInstance? ManualPreviewWeapon() => _hasMousePosition &&
        !(_panelVisible && _lastPanelRect.Contains(_uiMouseX, _uiMouseY)) &&
        _commandsPanel.EnabledCommandAt(_uiMouseX, _uiMouseY) == DefenseCommandTypes.Fire &&
        _commandsPanel.AllCommandButtons.FirstOrDefault(b => b.CommandTypeId == DefenseCommandTypes.Fire &&
            b.Rect.Contains(_uiMouseX, _uiMouseY)).Weapon is { } weapon && IsDefenseCommandEnabled(weapon, DefenseCommandTypes.Fire)
            ? weapon : null;

    private void DrawManualDefensePreview(SKCanvas canvas, SnapshotPrediction? prediction, bool resized)
    {
        var snapshot = prediction?.BufferedSnapshot.Snapshot;
        var weapon = ManualPreviewWeapon();
        if (resized || snapshot is null || weapon is null || DefenseModule(weapon) is not { Defense: { } defense } module ||
            defense.SpeedKmS is not > 0 || defense.TurnRateDegPerSec is not > 0 || defense.MaxFlightTimeMs is not > 0 ||
            snapshot.Objects.FirstOrDefault(o => o.ObjectId == weapon.OwnerObjectId) is not { } owner ||
            snapshot.Objects.FirstOrDefault(o => o.ObjectId == _selectedObjectId)?.Torpedo is not { } target)
        {
            ManualDefensePreviewPlan = null;
            ManualDefensePreviewGeometry = null;
            ManualDefensePreviewWeapon = null;
            ManualDefensePreviewMessage = null;
            _manualPreviewKey = null;
            return;
        }
        var key = (snapshot, weapon);
        if (_manualPreviewKey != key)
        {
            ManualDefensePreviewPlan = CountermeasureGuidanceMath.PlanManualLaunch(owner, target.Route, snapshot.MotionTimeMs,
                defense.MaxFlightTimeMs.Value, defense.SpeedKmS.Value, defense.TurnRateDegPerSec.Value,
                module.DefensePreview?.FirstTargetContactMotionTimeMs);
            _manualPreviewKey = key;
        }
        ManualDefensePreviewWeapon = weapon;
        ManualDefensePreviewGeometry = CombatTrajectoryProjector.ProjectManual(ManualDefensePreviewPlan!, target.Route,
            snapshot.MotionTimeMs, defense.MaxFlightTimeMs.Value, _camera);
        DrawCombatGeometry(canvas, ManualDefensePreviewGeometry, CombatSettings.Preview, CombatSettings.Preview);
        ManualDefensePreviewMessage = ManualDefensePreviewGeometry.Intercept is null ? CombatText("UnknownIntercept", "Intercept unknown") : null;
        if (ManualDefensePreviewGeometry.Intercept is { } intercept && module.DefensePreview?.ChanceTenths is { } chance)
        {
            var (x, y) = _camera.WorldToScreen(intercept.X, intercept.Y, _viewportW, _viewportH);
            using var text = new SKPaint { Color = CombatSettings.Preview, TextSize = 12, IsAntialias = true };
            canvas.DrawText($"{chance / 10m:0.0}%", x + 8, y - 8, text);
        }
    }

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
        _buffer.Presented?.Snapshot is { InstalledModules.IsDefaultOrEmpty: false } snapshot &&
        snapshot.PlayerShipObjectId == weapon.OwnerObjectId
            ? snapshot.InstalledModules.FirstOrDefault(m => m.ModuleId == weapon.ModuleId && m.Defense is not null) : null;

    private bool IsDefenseCommandEnabled(WeaponPanelInstance weapon, string commandType)
    {
        RefreshDefenseSubmissions();
        var snapshot = _buffer.Presented?.Snapshot;
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
        if (_defenseSubmissions.ContainsKey(key)) return CombatText("Sending", "Sending...");
        if (_defenseSubmissionErrors.TryGetValue(key, out var error)) return error == "send_failed"
            ? CombatText("SendFailed", "Send failed") : DefenseReasonText(error);
        var module = DefenseModule(weapon);
        if (module?.Defense is not { } defense) return null;
        string status = DefenseStatusText(defense, DefensePresentationTime);
        return defense.State == DefenseState.Ready && module.DefensePreview is { CanFire: false, Reason: { } reason }
            ? status + " · " + DefenseReasonText(reason) : status;
    }

    private string? GetDefenseInstanceOperatorText(WeaponPanelInstance weapon) => DefenseModule(weapon)?.Defense?.Operator is { } op
        ? $"{op.DisplayName} · {CombatText("SkillMetadata", "Навык (справочно)")} {op.Skill}"
        : CombatText("NoOperator", "Нет оператора");

    internal static string CombatText(string key, string fallback)
    {
        string fullKey = "Combat." + key;
        string value = Localization.Get(fullKey);
        return value == fullKey ? fallback : value;
    }

    internal static string DefenseReasonText(string reason) =>
        CombatText("Reason." + reason, CombatText("CommandRejected", "Command rejected"));

    internal static string RemainingFlightText(CountermeasureSnapshot flight, double time)
    {
        double? deadline = flight.ExpiresAtMotionTimeMs;
        if (flight.Phase == CountermeasurePhase.MissedCoast && flight.MissExpiresAtMotionTimeMs is { } coast)
            deadline = Math.Min(deadline ?? coast, coast);
        return deadline is { } end ? $"{Math.Max(0, (end - time) / 1000):0.0} " + CombatText("Seconds", "с") : "—";
    }

    private IReadOnlyList<string> GetDefenseInstanceDetails(WeaponPanelInstance weapon)
    {
        var module = DefenseModule(weapon);
        var lines = new List<string>();
        if (module?.Defense is { } defense)
            lines.AddRange(DefenseBurstLines(defense, DefensePresentationTime));
        if (module?.DefensePreview is { TargetTorpedoId: { } target, ChanceTenths: { } chance } preview &&
            target == _selectedObjectId && preview.OwnerObjectId == weapon.OwnerObjectId && preview.ModuleId == weapon.ModuleId)
            lines.Add($"{CombatText("Accuracy", "Точность")} {preview.Accuracy:0.##} − " +
                $"{CombatText("Maneuverability", "манёвренность")} {preview.TargetManeuverability:0.##} → {chance / 10m:0.0}%");
        if (module?.Defense?.ActiveProjectileId is { } id &&
            _buffer.Presented?.Snapshot.Objects.FirstOrDefault(o => o.ObjectId == id)?.Countermeasure is { } flight)
        {
            lines.Add($"{CombatText("FrozenChance", "Шанс активной ПРО")}: {flight.FrozenChanceTenths / 10m:0.0}%");
            lines.Add($"{CombatText("RemainingFlight", "Осталось полёта")}: {RemainingFlightText(flight, DefensePresentationTime)}");
        }
        return lines;
    }

    internal static IReadOnlyList<string> DefenseBurstLines(DefenseSnapshot defense, double motionTimeMs)
    {
        var lines = new List<string>
        {
            $"{CombatText("Barrels", "Стволы")}: {defense.Barrels}"
        };
        if (defense.PendingBurstShots is { IsDefaultOrEmpty: false } pending)
        {
            double due = pending.Min(shot => shot.DueMotionTimeMs);
            lines.Add($"{CombatText("NextBarrel", "Следующий выстрел")}: " +
                $"{Math.Max(0, (due - motionTimeMs) / 1000):0.0} {CombatText("Seconds", "с")}");
        }
        if (defense.ReadyAtMotionTimeMs is { } readyAt && readyAt > motionTimeMs)
            lines.Add($"{CombatText("Reloading", "Перезарядка")}: " +
                $"{(readyAt - motionTimeMs) / 1000:0.0} {CombatText("Seconds", "с")}");
        return lines;
    }
}
