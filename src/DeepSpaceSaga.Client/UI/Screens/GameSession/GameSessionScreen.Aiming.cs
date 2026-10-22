using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    internal CombatInputSettings CombatInputs { get; }
    internal sealed record PointAimState(ProjectileKind Kind, string ModuleId, string ProfileId);
    internal PointAimState? PointAim { get; private set; }
    internal string? LastPointCommandId { get; private set; }
    private string? _lastPointModuleId;
    private Guid? _pointInputSession;
    private bool _pointInputSessionInitialized;
    private sealed class PointSubmission
    {
        internal string CommandId = "";
        internal Task? SendTask;
        internal ulong StartRevision;
        internal double? StartReadyAt;
        internal double ReloadMs;
        internal HashSet<string> ExistingFlights = new(StringComparer.Ordinal);
    }
    private readonly Dictionary<string, PointSubmission> _pointSubmissions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _pointErrors = new(StringComparer.Ordinal);
    private static ImmutableArray<LaunchProfileSnapshot> PointProfiles(InstalledModuleSnapshot module) =>
        module.LauncherCombat?.Profiles ?? module.Defense?.Profiles ?? [];
    private double PointAimTime => _buffer.LatestPrediction is { } prediction
        ? prediction.BufferedSnapshot.Snapshot.MotionTimeMs + CombatPredictionDelta(prediction) : 0;
    internal string? PointAimFeedback => (PointAim?.ModuleId ?? _lastPointModuleId) is { } moduleId && PointAimModule(moduleId) is { } module
        ? PointWeaponStatus(module) : null;

    private bool _pointAltLeft, _pointAltRight;
    internal void SynchronizeModifiers(bool controlLeft, bool controlRight, bool altLeft = false, bool altRight = false)
    { _isCtrlLeftDown = controlLeft; _isCtrlRightDown = controlRight; _pointAltLeft = altLeft; _pointAltRight = altRight; }

    private InstalledModuleSnapshot? PointAimModule(string id) =>
        _buffer.Presented?.Snapshot is { InstalledModules.IsDefaultOrEmpty: false } snapshot
            ? snapshot.InstalledModules.FirstOrDefault(m => m.ModuleId == id && !PointProfiles(m).IsDefaultOrEmpty) : null;

    private void CancelPointAim() { PointAim = null; CurrentTorpedoAimPreview = null; CurrentCounterAimPreview = null; ResetPointFriendlyConfirmation(); }

    private void RefreshPointAiming()
    {
        var session = _buffer.PresentationSessionId;
        if (!_pointInputSessionInitialized || session != _pointInputSession)
        {
            CancelPointAim(); _pointInputSession = session; _pointInputSessionInitialized = true;
            foreach (var pending in _pointSubmissions.Values) _ = pending.SendTask?.Exception;
            _pointSubmissions.Clear(); _pointErrors.Clear(); LastPointCommandId = null; _lastPointModuleId = null;
        }
        var snapshot = _buffer.Presented?.Snapshot;
        if (snapshot is null || snapshot.ActiveDialogue is not null || snapshot.EffectiveOutcome.State == SessionOutcomeState.Defeated)
            CancelPointAim();
        if (PointAim is { } aim && (PointAimModule(aim.ModuleId) is not { } module ||
            !PointProfiles(module).Any(p => p.ProfileId == aim.ProfileId))) CancelPointAim();
        foreach (var (id, submission) in _pointSubmissions.ToArray())
        {
            if (submission.SendTask is { IsFaulted: true } or { IsCanceled: true })
            {
                _ = submission.SendTask.Exception;
                _pointErrors[id] = "send_failed"; _pointSubmissions.Remove(id); continue;
            }
            if (_buffer.FindCommandAcceptance(submission.CommandId) is { Accepted: false })
            { _pointErrors[id] = "command_rejected"; _pointSubmissions.Remove(id); continue; }
            if (_buffer.FindCommandResult(submission.CommandId) is not { } result || result.Status == CommandResultStatus.Deferred) continue;
            if (result.Status != CommandResultStatus.Executed)
            {
                _pointErrors[id] = result.ReasonCode ?? "command_rejected";
                _pointSubmissions.Remove(id);
            }
            else if (snapshot is not null && snapshot.SnapshotSequence > submission.StartRevision &&
                ((!snapshot.CommandResults.IsDefaultOrEmpty && snapshot.CommandResults.Any(r => r.CommandId == submission.CommandId && r.Status == CommandResultStatus.Executed)) ||
                 snapshot.Objects.Any(o => o.OwnProjectile is { } own && own.OwnerObjectId == result.ObjectId && own.LauncherModuleId == id &&
                     !submission.ExistingFlights.Contains(o.ObjectId) && own.LaunchMotionTimeMs >= result.EffectiveGameTimeMs) ||
                 PointAimModule(id) is { } admittedModule &&
                     (admittedModule.LauncherCombat?.ReadyAtMotionTimeMs ?? admittedModule.Defense?.ReadyAtMotionTimeMs) is { } ready &&
                     ready != submission.StartReadyAt && ready >= result.EffectiveGameTimeMs + submission.ReloadMs ||
                 snapshot.MotionTimeMs > result.EffectiveGameTimeMs + submission.ReloadMs))
                _pointSubmissions.Remove(id);
        }
    }

    private string? PointReadyReason(InstalledModuleSnapshot module)
    {
        if (module.IsDestroyed || (module.StructureMax > 0 ? module.StructureCurrent <= 0 : module.StructurePoints <= 0))
            return CommandReasonCodes.ModuleDestroyed;
        if (module.DisabledUntilMotionTimeMs is { } disabled && disabled > PointAimTime) return CommandReasonCodes.ModuleDisabled;
        if (module.PowerState != "On" || module.OperationalState != "Ready") return CommandReasonCodes.ModuleUnavailable;
        if (module.CommandTypeIds.IsDefaultOrEmpty || !module.CommandTypeIds.Contains(module.LauncherCombat?.Profiles is not null
            ? CombatCommandTypes.Launch : DefenseCommandTypes.Launch)) return CommandReasonCodes.ModuleUnavailable;
        var op = module.Operator ?? module.LauncherCombat?.Operator ?? module.Defense?.Operator;
        if (string.IsNullOrWhiteSpace(op?.CrewId)) return CommandReasonCodes.NoWeaponOperator;
        if ((module.LauncherCombat?.ReadyAtMotionTimeMs ?? module.Defense?.ReadyAtMotionTimeMs) is { } ready && ready > PointAimTime)
            return CommandReasonCodes.LauncherReloading;
        return null;
    }

    private bool EnterPointAim(ProjectileKind kind, string? moduleId = null)
    {
        RefreshPointAiming();
        var snapshot = _buffer.Presented?.Snapshot;
        if (snapshot is null || snapshot.InstalledModules.IsDefaultOrEmpty || snapshot.ActiveDialogue is not null ||
            FindPlayerShipMotion(snapshot) is not { IsDestroyed: false }) return false;
        var modules = snapshot.InstalledModules.Where(m => !PointProfiles(m).IsDefaultOrEmpty &&
            (kind == ProjectileKind.Torpedo ? m.LauncherCombat?.Profiles is not null : m.Defense?.Profiles is not null) && !m.IsDestroyed)
            .Where(m => moduleId is null || m.ModuleId == moduleId)
            .OrderBy(m => m.Position).ThenBy(m => m.ModuleId, StringComparer.Ordinal).ToArray();
        var module = modules.FirstOrDefault(m => !_pointSubmissions.ContainsKey(m.ModuleId) && PointReadyReason(m) is null) ?? modules.FirstOrDefault();
        if (module is null) return false;
        ResetPointFriendlyConfirmation();
        PointAim = new(kind, module.ModuleId, PointProfiles(module)[0].ProfileId);
        _pointErrors.Remove(module.ModuleId); _isPanningMap = false;
        return true;
    }

    private bool HandlePointAimKey(Key key)
    {
        if (key is Key.AltLeft or Key.AltRight)
        { if (key == Key.AltLeft) _pointAltLeft = true; else _pointAltRight = true; return true; }
        RefreshPointAiming();
        if (PointAim is not null && (key == CombatInputs.Cancel || key == Key.Escape)) { CancelPointAim(); return true; }
        if (IsCtrlDown) return false;
        if (key == CombatInputs.TorpedoAim) return EnterPointAim(ProjectileKind.Torpedo);
        if (key == CombatInputs.CountermissileAim) return EnterPointAim(ProjectileKind.Countermissile);
        if (PointAim is not { } aim || PointAimModule(aim.ModuleId) is not { } module) return false;
        int index = CombatInputs.Profiles.IndexOf(key);
        if (index >= 0)
        {
            ResetPointFriendlyConfirmation();
            var profiles = PointProfiles(module);
            if (index < profiles.Length) PointAim = aim with { ProfileId = profiles[index].ProfileId };
            _pointErrors.Remove(module.ModuleId);
            return true;
        }
        return key is Key.Number1 or Key.Number2 or Key.Number3; // aiming never changes speed with these keys
    }

    private bool HandlePointAimWheel(float x, float y, float delta)
    {
        RefreshPointAiming();
        if (!CombatInputs.WheelProfiles || PointAim is not { } aim || !float.IsFinite(delta) || delta == 0 ||
            IsClickOnUiPanel(x / _uiScale, y / _uiScale) || PointAimModule(aim.ModuleId) is not { } module) return false;
        var profiles = PointProfiles(module);
        ResetPointFriendlyConfirmation();
        int index = Enumerable.Range(0, profiles.Length).First(i => profiles[i].ProfileId == aim.ProfileId);
        PointAim = aim with { ProfileId = profiles[(index + (delta > 0 ? 1 : profiles.Length - 1)) % profiles.Length].ProfileId };
        _pointErrors.Remove(module.ModuleId);
        return true;
    }

    private bool HandlePointAimMouse(float x, float y, MouseButton button)
    {
        RefreshPointAiming();
        if (PointAim is not { } aim) return false;
        if (button == CombatInputs.CancelMouse) { CancelPointAim(); return true; }
        if (button != CombatInputs.LaunchMouse || IsClickOnUiPanel(x / _uiScale, y / _uiScale)) return false;
        _mouseX = x; _mouseY = y; _uiMouseX = x / _uiScale; _uiMouseY = y / _uiScale; _hasMousePosition = true;
        if (PointAimModule(aim.ModuleId) is not { } module || _buffer.Presented?.Snapshot is not { } snapshot) return true;
        if (_pointSubmissions.ContainsKey(module.ModuleId)) return true;
        if (PointReadyReason(module) is not null) { _pointErrors.Remove(module.ModuleId); return true; }
        if (_handle is null) { _pointErrors[module.ModuleId] = "connection_unavailable"; return true; }
        var target = _camera.ScreenToWorld(x, y, _viewportW, _viewportH);
        if (aim.Kind == ProjectileKind.Countermissile && BuildCounterAimPreview(target.Item1, target.Item2) is { } counter)
        {
            if (counter.Unreachable) { _pointErrors[module.ModuleId] = "aim_unreachable"; return true; }
            target = (counter.X, counter.Y);
        }
        var player = FindPlayerShip(_renderStates);
        double? range = module.LauncherCombat?.RangeKm ?? module.Defense?.RangeKm;
        if (range is not > 0 || !double.IsFinite(range.Value))
        { _pointErrors[module.ModuleId] = CommandReasonCodes.ModuleUnavailable; return true; }
        if (player is null || !double.IsFinite(target.Item1) || !double.IsFinite(target.Item2) ||
            double.Hypot(target.Item1 - player.Value.Pose.X, target.Item2 - player.Value.Pose.Y) / 10 > range.Value)
        { _pointErrors[module.ModuleId] = CommandReasonCodes.LaunchOutOfRange; return true; }
        if (aim.Kind == ProjectileKind.Torpedo && !ConfirmPointFriendlyFire(aim, target.Item1, target.Item2)) return true;
        var submission = new PointSubmission
        {
            StartRevision = snapshot.SnapshotSequence,
            StartReadyAt = module.LauncherCombat?.ReadyAtMotionTimeMs ?? module.Defense?.ReadyAtMotionTimeMs,
            ReloadMs = module.LauncherCombat?.ReloadDurationMs ?? module.Defense?.ReloadDurationMs ?? 0,
            ExistingFlights = snapshot.Objects.Where(o => o.OwnProjectile?.LauncherModuleId == module.ModuleId).Select(o => o.ObjectId).ToHashSet(StringComparer.Ordinal)
        };
        _pointSubmissions[module.ModuleId] = submission;
        _pointErrors.Remove(module.ModuleId);
        try
        {
            submission.SendTask = _handle.SendPointLaunchAsync(snapshot.PlayerShipObjectId!, module.ModuleId, aim.Kind,
                target.Item1, target.Item2, aim.ProfileId, out submission.CommandId).AsTask();
            LastPointCommandId = submission.CommandId;
            _lastPointModuleId = module.ModuleId;
            if (aim.Kind == ProjectileKind.Torpedo) CancelPointAim();
        }
        catch (Exception)
        { _pointSubmissions.Remove(module.ModuleId); _pointErrors[module.ModuleId] = "send_failed"; }
        return true;
    }

    private string PointWeaponStatus(InstalledModuleSnapshot module)
    {
        if (_pointSubmissions.TryGetValue(module.ModuleId, out var pending)) return
            _buffer.FindCommandAcceptance(pending.CommandId) is { Accepted: true } || _buffer.FindCommandResult(pending.CommandId) is { Status: CommandResultStatus.Executed }
                ? CombatText("Accepted", "Command accepted") : CombatText("Sending", "Sending...");
        if (_pointErrors.TryGetValue(module.ModuleId, out var error))
        {
            if (error == CommandReasonCodes.LauncherReloading &&
                (module.LauncherCombat?.ReadyAtMotionTimeMs ?? module.Defense?.ReadyAtMotionTimeMs) is { } ready && ready <= PointAimTime)
                _pointErrors.Remove(module.ModuleId);
            else return PointLaunchReason(error);
        }
        if (PointReadyReason(module) is { } reason) return reason == CommandReasonCodes.LauncherReloading
            ? CombatText("Reloading", "Reloading") : PointLaunchReason(reason);
        return CombatText("Ready", "Ready");
    }

    internal static string PointLaunchReason(string reason) => reason == "aim_unreachable" ? CombatText("Aim.Unreachable", "Unreachable") :
        CombatText("Reason." + reason, CombatText("CommandRejected", "Command rejected"));

    private InstalledModuleSnapshot? PointPanelModule(WeaponPanelInstance weapon) =>
        _buffer.Presented?.Snapshot.PlayerShipObjectId == weapon.OwnerObjectId ? PointAimModule(weapon.ModuleId) : null;
    private bool IsPointWeaponCommandEnabled(WeaponPanelInstance weapon, string commandType)
    {
        if (PointPanelModule(weapon) is not { } module) return false;
        if (_buffer.Presented?.Snapshot is not { ActiveDialogue: null } snapshot || FindPlayerShipMotion(snapshot) is not { IsDestroyed: false }) return false;
        if (commandType is CombatCommandTypes.Launch or DefenseCommandTypes.Launch) return !module.IsDestroyed;
        return commandType == CombatCommandTypes.Detonate && _selectedObjectId is { } id && snapshot.Objects.Any(o =>
            o.ObjectId == id && o.OwnProjectile is { Kind: ProjectileKind.Torpedo } point && point.LauncherModuleId == module.ModuleId && point.OwnerObjectId == weapon.OwnerObjectId);
    }
    private void SendPointWeaponCommand(WeaponPanelInstance weapon, string commandType)
    {
        if (PointPanelModule(weapon) is not { } module) return;
        if (!IsPointWeaponCommandEnabled(weapon, commandType)) return;
        if (commandType == CombatCommandTypes.Detonate)
        { if (_handle is not null) _ = _handle.SendCommandAsync(weapon.OwnerObjectId, weapon.ModuleId, commandType, _selectedObjectId); return; }
        EnterPointAim(module.LauncherCombat?.Profiles is not null ? ProjectileKind.Torpedo : ProjectileKind.Countermissile, module.ModuleId);
    }
    private string? GetPointWeaponStatus(WeaponPanelInstance weapon)
    {
        if (PointPanelModule(weapon) is not { } module) return null;
        string status = PointWeaponStatus(module);
        return status == CombatText("Reloading", "Reloading")
            ? $"{status} {PointReloadRealSeconds(module):0.0} {CombatText("Seconds", "с")}" : status;
    }
    private double PointReloadRealSeconds(InstalledModuleSnapshot module)
    {
        double ready = module.LauncherCombat?.ReadyAtMotionTimeMs ?? module.Defense?.ReadyAtMotionTimeMs ?? 0;
        return PanelTime.RealSeconds((ready - PointAimTime) / 1000, PanelDisplaySpeed);
    }

    private SimulationSpeed PanelDisplaySpeed
    {
        get
        {
            if (_buffer.CurrentSpeed != SimulationSpeed.Speed0) _lastNonPauseSpeed = _buffer.CurrentSpeed;
            return PanelTime.EffectiveSpeed(_buffer.CurrentSpeed, _lastNonPauseSpeed);
        }
    }
    private string? PointWeaponButtonLabel(WeaponPanelInstance weapon, string command)
    {
        if (PointPanelModule(weapon) is not { } module) return null;
        return command == CombatCommandTypes.Detonate ? CombatText("Detonate", "Подрыв") :
            $"{CombatText("Aim", "Прицел")} [{(module.LauncherCombat?.Profiles is not null ? CombatInputs.TorpedoAim : CombatInputs.CountermissileAim)}]";
    }
    private string? GetPointWeaponOperator(WeaponPanelInstance weapon) => PointPanelModule(weapon) is { } module
        ? PointOperatorLabel(module.Operator ?? module.LauncherCombat?.Operator ?? module.Defense?.Operator) : null;
    private static string PointOperatorLabel(WeaponOperatorSnapshot? op) => op is null ? CombatText("NoOperator", "Нет оператора") :
        $"{op.DisplayName} · {CombatText("Accuracy", "Точность")} {op.Skill}";
    private IReadOnlyList<string> GetPointWeaponDetails(WeaponPanelInstance weapon)
    {
        if (PointPanelModule(weapon) is not { } module) return [];
        var lines = new List<string>();
        var profiles = PointProfiles(module);
        string? current = PointAim?.ModuleId == module.ModuleId ? PointAim.ProfileId : profiles.FirstOrDefault()?.ProfileId;
        foreach (var profile in profiles)
            lines.Add($"{(profile.ProfileId == current ? "> " : "")}{CombatText("Profile." + profile.ProfileId, profile.ProfileId)} · {profile.SpeedKmS:0.##} {CombatText("KilometersPerSecond", "")} · R {profile.BlastRadiusKm:0.##} {CombatText("Kilometers", "")}");
        double? scatter = module.LauncherCombat?.ScatterPercent ?? module.Defense?.ScatterPercent;
        double? effective = module.LauncherCombat?.EffectiveScatterPercent ?? module.Defense?.EffectiveScatterPercent;
        lines.Add($"{CombatText("Scatter", "Разброс")}: {(scatter is { } value ? (value * 100).ToString("0.##") + "%" : "—")} · σ {(effective is { } sigma ? (sigma * 100).ToString("0.##") + "%" : "—")}");
        return lines;
    }

    private void DrawPointAimFeedback(SKCanvas canvas)
    {
        RefreshPointAiming();
        if (PointAim is not { } aim) return;
        var styles = CombatSettings.PointCombat;
        DrawTorpedoAimPreview(canvas);
        DrawCounterAimPreview(canvas);
        var rect = AvailableMapRect();
        float x = _hasMousePosition ? _mouseX : rect.MidX;
        float y = _hasMousePosition ? _mouseY : rect.Top + (float)styles.TextSizePx * 2;
        if (CurrentCounterAimPreview is { } counter)
        { var point = _camera.WorldToScreen(counter.X, counter.Y, _viewportW, _viewportH); x = point.X; y = point.Y; }
        if (_hasMousePosition && IsClickOnUiPanel(_uiMouseX, _uiMouseY)) return;
        using var line = new SKPaint { Color = styles.AimReticle, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = (float)styles.LineWidthPx };
        float radius = (float)styles.ReticleRadiusPx;
        canvas.DrawLine(x - radius, y, x + radius, y, line); canvas.DrawLine(x, y - radius, x, y + radius, line);
        using var text = new SKPaint { Color = styles.ForeignProfileText, IsAntialias = true, TextSize = (float)styles.TextSizePx };
        canvas.DrawText($"{CombatText("Profile." + aim.ProfileId, aim.ProfileId)} · {PointAimFeedback}", x + radius, y + radius + text.TextSize, text);
    }
}
