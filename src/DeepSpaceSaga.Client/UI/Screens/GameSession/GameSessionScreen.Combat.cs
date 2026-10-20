using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private string? _pendingFireModeId;
    private string? _pendingFireModeCommandId;
    private Task? _fireModeSendTask;
    private static readonly string[] FireModeOrder = ["standard", "maneuverability", "overheat"];

    private void RefreshFireModeSubmission()
    {
        if (_pendingFireModeId is null) return;
        var snapshot = _buffer.Presented?.Snapshot;
        if (FindLauncher()?.LauncherCombat?.ModeId == _pendingFireModeId ||
            _pendingFireModeCommandId is { } id && _buffer.FindCommandResult(id) is
            { Status: CommandResultStatus.Rejected or CommandResultStatus.Failed or CommandResultStatus.Cancelled } ||
            _fireModeSendTask is { IsFaulted: true } or { IsCanceled: true })
        {
            _ = _fireModeSendTask?.Exception;
            _pendingFireModeId = null;
            _pendingFireModeCommandId = null;
            _fireModeSendTask = null;
        }
    }

    private bool IsFireModeEnabled()
    {
        RefreshFireModeSubmission();
        return _pendingFireModeId is null && _handle is not null &&
            _buffer.Presented?.Snapshot is { ActiveDialogue: null } snapshot &&
            FindPlayerShipMotion(snapshot) is { IsDestroyed: false } &&
            FindLauncher() is
            {
                PowerState: "On", OperationalState: "Ready", StructurePoints: > 0,
                LauncherCombat: { IsBrokenUntilDock: false }
            } launcher &&
            launcher.Commands.Any(c => c.CommandTypeId == TorpedoLauncherCommandTypes.SetMode);
    }

    private void SendNextFireMode()
    {
        if (!IsFireModeEnabled()) return;
        var launcher = FindLauncher()!;
        string current = launcher.LauncherCombat!.ModeId;
        int index = Array.IndexOf(FireModeOrder, current);
        string next = FireModeOrder[(index + 1) % FireModeOrder.Length];
        _pendingFireModeId = next;
        try
        {
            _fireModeSendTask = _handle!.SendFireModeAsync(_buffer.Presented!.Snapshot.PlayerShipObjectId!,
                launcher.ModuleId, next, out var id).AsTask();
            _pendingFireModeCommandId = id;
        }
        catch (Exception)
        {
            _pendingFireModeId = null;
            _pendingFireModeCommandId = null;
            _fireModeSendTask = null;
        }
    }

    internal static string FireModeName(string modeId) => modeId switch
    {
        "standard" => CombatText("Mode.Standard", "Стандарт"),
        "maneuverability" => CombatText("Mode.Maneuverability", "Манёвренность"),
        "overheat" => CombatText("Mode.Overheat", "Перегрев"),
        _ => modeId
    };

    internal static string LauncherModeStatus(LauncherCombatSnapshot launcher, double motionTimeMs)
    {
        string mode = FireModeName(launcher.ModeId);
        if (launcher.IsBrokenUntilDock) return mode + " · " + CombatText("BrokenUntilDock", "Сломан до дока");
        if (launcher.ReadyAtMotionTimeMs is { } readyAt && readyAt > motionTimeMs)
            return mode + " · " + CombatText("Reloading", "Перезарядка") +
                $" {Math.Max(0, (readyAt - motionTimeMs) / 1000):0.0} " + CombatText("Seconds", "с");
        return mode + " · " + CombatText("Ready", "Готов");
    }

    private readonly CombatJournalPanel _combatJournalPanel = new();
    internal TorpedoRoute? LaunchPreviewRoute { get; private set; }
    internal CombatTrajectoryProjector.Geometry? LaunchPreviewGeometry { get; private set; }
    private (ObjectMotionSnapshot Owner, ObjectMotionSnapshot Target, LauncherCombatSnapshot Launcher, long Time, long? Lifetime)? _launchPreviewKey;
    private readonly HashSet<string> _combatPoseObjectIds = new(StringComparer.Ordinal);

    private bool IsLaunchPreviewRequested() => _hasMousePosition &&
        !(_panelVisible && _lastPanelRect.Contains(_uiMouseX, _uiMouseY)) &&
        _commandsPanel.EnabledCommandAt(_uiMouseX, _uiMouseY) == CombatCommandTypes.Fire && IsTorpedoFireEnabled();

    private static long CombatPredictionDelta(SnapshotPrediction prediction) =>
        prediction.CurrentSpeed == SimulationSpeed.Speed0 && prediction.BufferedSnapshot.Snapshot.CurrentSpeed == SimulationSpeed.Speed0
            ? 0 : prediction.EffectivePredictionDeltaMs;

    private void UpdateCombatPoseObjects(AuthoritativeSnapshot snapshot)
    {
        _combatPoseObjectIds.Clear();
        foreach (var obj in snapshot.Objects)
        {
            if (obj.OwnProjectile is not null || obj.ObservedProjectile is not null)
                _combatPoseObjectIds.Add(obj.ObjectId);
            if (obj.Countermeasure is { } defenseFlight)
            {
                _combatPoseObjectIds.Add(obj.ObjectId);
                _combatPoseObjectIds.Add(defenseFlight.TargetTorpedoId);
            }
            if (obj.Torpedo is not { } flight) continue;
            _combatPoseObjectIds.Add(obj.ObjectId);
            _combatPoseObjectIds.Add(flight.TargetObjectId);
        }
        if (IsLaunchPreviewRequested() || ManualPreviewWeapon() is not null)
        {
            if (snapshot.PlayerShipObjectId is { } playerId) _combatPoseObjectIds.Add(playerId);
            if (_selectedObjectId is { } targetId) _combatPoseObjectIds.Add(targetId);
        }
    }

    private void ClearLaunchPreview()
    {
        LaunchPreviewRoute = null;
        LaunchPreviewGeometry = null;
        _launchPreviewKey = null;
    }

    private void DrawLaunchPreview(SKCanvas canvas, SnapshotPrediction? prediction, bool viewportResized)
    {
        DrawManualDefensePreview(canvas, prediction, viewportResized);
        if (prediction is null || LaunchPreviewGeometry is null) return;
        DrawCombatGeometry(canvas, LaunchPreviewGeometry, CombatSettings.Preview, CombatSettings.Preview, _selectedObjectId);
    }

    private readonly Dictionary<string, CombatTrajectoryProjector.Geometry> _combatTrajectories = new(StringComparer.Ordinal);
    internal IReadOnlyDictionary<string, CombatTrajectoryProjector.Geometry> CombatTrajectories => _combatTrajectories;
    private SKPaint? _combatTrajectoryPaint;
    private SKPathEffect? _combatDash;
    private SKPath? _combatTrajectoryPath;

    private void DrawCombatTrajectories(SKCanvas canvas, SnapshotPrediction? prediction)
    {
        var buffered = prediction?.BufferedSnapshot;
        _combatTrajectories.Clear();
        PointProjectileVisuals.Clear();
        _combatEffects.Receive(buffered?.Snapshot.CombatImpacts ?? default, buffered?.ReceivedAtTimestamp);
        foreach (var state in _renderStates)
        {
            if (state.Source.OwnProjectile is not null || state.Source.ObservedProjectile is not null)
            {
                DrawPointProjectile(canvas, state, prediction);
                continue;
            }
            if (state.Pose.Motion.Torpedo is null) continue;
            var geometry = PreparedFlight(state.Source.ObjectId, prediction);
            if (geometry is null) continue;
            AlignFlightStart(geometry, state);
            _combatTrajectories[state.Source.ObjectId] = geometry;
            DrawCombatGeometry(canvas, geometry, CombatSettings.Prediction, CombatSettings.Intercept, state.Source.Torpedo?.TargetObjectId);
        }
    }

    private void AlignFlightStart(CombatTrajectoryProjector.Geometry geometry, ObjectRenderState state)
    {
        if (geometry.Prediction.Count > 0) geometry.Prediction[0] = new(state.Pose.X, state.Pose.Y);
        else geometry.Prediction.Add(new(state.Pose.X, state.Pose.Y));
        string? targetId = state.Source.Torpedo?.TargetObjectId ?? state.Source.Countermeasure?.TargetTorpedoId;
        if (geometry.Target.Count > 0 && FindRenderStateById(targetId) is { } target)
            geometry.Target[0] = new(target.Pose.X, target.Pose.Y);
    }

    private void DrawCombatGeometry(SKCanvas canvas, CombatTrajectoryProjector.Geometry geometry,
        SKColor prediction, SKColor intercept, string? targetId = null)
    {
        _combatTrajectoryPaint ??= new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f, IsAntialias = true };
        _combatDash ??= SKPathEffect.CreateDash([6f, 4f], 0);
        _combatTrajectoryPath ??= new SKPath();
        DrawPath(geometry.Prediction, prediction, true);
        var target = FindRenderStateById(targetId);
        DrawPath(geometry.Target, CombatTargetPathColor(target?.Source, target?.IsPlayerShip == true), true);
        _combatTrajectoryPaint.PathEffect = null;
        _combatTrajectoryPaint.Color = intercept;
        if (geometry.Intercept is { } point)
        {
            var (x, y) = _camera.WorldToScreen(point.X, point.Y, _viewportW, _viewportH);
            if (x >= -6 && y >= -6 && x <= _viewportW + 6 && y <= _viewportH + 6)
            {
                canvas.DrawLine(x - 5, y - 5, x + 5, y + 5, _combatTrajectoryPaint);
                canvas.DrawLine(x - 5, y + 5, x + 5, y - 5, _combatTrajectoryPaint);
            }
        }

        // Both centres are physical positions. Never snap the target onto the projectile.
        if (geometry.Intercept is not null && geometry.Target.Count > 0)
        {
            var q = geometry.Target[^1];
            var (qx, qy) = _camera.WorldToScreen(q.X, q.Y, _viewportW, _viewportH);
            canvas.DrawCircle(qx, qy, 3, _combatTrajectoryPaint);
        }
        if (geometry.Prediction.Count > 0)
        {
            var last = geometry.Prediction[^1];
            var (x, y) = _camera.WorldToScreen(last.X, last.Y, _viewportW, _viewportH);
            bool endpointVisible = x >= 0 && y >= 0 && x <= _viewportW && y <= _viewportH;
            if (!endpointVisible)
            {
                var first = geometry.Prediction[0];
                (x, y) = _camera.WorldToScreen(first.X, first.Y, _viewportW, _viewportH);
            }
            if (x >= 0 && y >= 0 && x <= _viewportW && y <= _viewportH)
            {
                using var text = new SKPaint { Color = CombatSettings.DefenseText, TextSize = 12, IsAntialias = true };
                string label = geometry.EtaSeconds is { } eta
                    ? $"{CombatText("Forecast.Conditional", "Current maneuver")} · {eta:0.0} {CombatText("Seconds", "s")}"
                    : CombatText("Forecast." + geometry.Status, "Forecast limited") +
                        $" · {geometry.HorizonSeconds:0.0} {CombatText("Seconds", "s")}";
                if (geometry.Intercept is null && endpointVisible) canvas.DrawRect(x - 3, y - 3, 6, 6, _combatTrajectoryPaint);
                canvas.DrawText(label, x + 8, y + 16, text);
            }
        }

        void DrawPath(IReadOnlyList<FutureTrajectoryPoint> points, SKColor color, bool dashed)
        {
            _combatTrajectoryPaint.Color = color;
            _combatTrajectoryPaint.PathEffect = dashed ? _combatDash : null;
            CombatTrajectoryProjector.BuildPath(_combatTrajectoryPath, points, _camera, _viewportW, _viewportH);
            canvas.DrawPath(_combatTrajectoryPath, _combatTrajectoryPaint);
        }
    }

    internal SKColor CombatTargetPathColor(ObjectMotionSnapshot? target, bool isPlayer = false)
    {
        var color = target?.RenderObjectType switch
        {
            SpaceObjectType.Missile => CombatSettings.Torpedo,
            SpaceObjectType.Countermeasure => CombatSettings.Countermeasure,
            SpaceObjectType.Wreck => CombatSettings.Wreck,
            _ => isPlayer ? SpaceMapColorResolver.PlayerShipColor :
                SpaceMapColorResolver.GetColor(target?.RenderObjectType, target?.RelationToPlayer)
        };
        return color.WithAlpha(77);
    }

    private TorpedoInspectionData? BuildTorpedoInspection(ObjectRenderState state)
    {
        if (state.Source is not { RenderObjectType: SpaceObjectType.Missile, Torpedo: { } flight }) return null;
        var predicted = state.Pose.Motion.Torpedo ?? flight;
        // Route elapsed is advanced by the shared predictor. Camera reconciliation
        // changes only X/Y/heading; neither it nor pixels can change travelled distance.
        double extraMs = Math.Max(0, predicted.Route.ElapsedMs - flight.Route.ElapsedMs);
        double travelledKm = flight.DistanceTravelledWorldUnits / 10 + flight.SpeedKmS * extraMs / 1000;
        double? etaSeconds = _combatTrajectories.TryGetValue(state.Source.ObjectId, out var geometry)
            ? geometry.EtaSeconds : null;
        var target = FindRenderStateById(flight.TargetObjectId)?.Source;
        string targetLabel = target is { RenderObjectType: not null and not SpaceObjectType.UnknownSpaceObject }
            && !string.IsNullOrWhiteSpace(target.DisplayName) ? target.DisplayName : flight.TargetObjectId;
        return new(targetLabel, travelledKm, etaSeconds, flight.HitChancePercent);
    }
    private readonly CombatEffectStore _combatEffects;
    private SKPaint? _combatExplosionPaint;
    internal CombatEffectStore CombatEffects => _combatEffects;

    private void DrawCombatEffects(SKCanvas canvas, BufferedSnapshot? buffered, long now)
    {
        _combatEffects.PointStyles = CombatSettings.PointCombat;
        _combatEffects.ReceiveJournal(buffered?.Snapshot.CombatJournal ?? default, buffered?.ReceivedAtTimestamp ?? now);
        DrawPointEffects(canvas, now);
        DrawPointHitFeedback(canvas);
        if (buffered is not null)
            _combatEffects.Receive(buffered.Snapshot.CombatImpacts, buffered.ReceivedAtTimestamp);
        else _combatEffects.Receive(default);
        if (_combatEffects.Active.Count == 0) return;
        _combatExplosionPaint ??= new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 2f, IsAntialias = true };
        foreach (var effect in _combatEffects.Active)
        {
            var (x, y) = _camera.WorldToScreen(effect.Impact.X, effect.Impact.Y, _viewportW, _viewportH);
            // Historical torpedo data remains in the effect store; its trail stays hidden.
            float radius = effect.RadiusPx(now);
            if (radius <= 0 || x < -52 || y < -52 || x > _viewportW + 52 || y > _viewportH + 52) continue;
            _combatExplosionPaint.Color = CombatSettings.Explosion.WithAlpha(effect.Alpha(now, CombatSettings.Explosion.Alpha));
            canvas.DrawCircle(x, y, radius, _combatExplosionPaint);
        }
    }
    private readonly HashSet<string> _combatImportantIds = new(StringComparer.Ordinal);
    internal sealed record PointFeedbackLabel(SKRect Bounds, string Text);
    internal readonly Dictionary<string, PointFeedbackLabel> EnemyStatusBadges = new(StringComparer.Ordinal);
    internal readonly Dictionary<long, PointFeedbackLabel> PointHitLabels = new();

    private void DrawPointHitFeedback(SKCanvas canvas)
    {
        EnemyStatusBadges.Clear(); PointHitLabels.Clear();
        var available = AvailableMapRect();
        if (available.Width <= 0 || available.Height <= 0) return;
        var styles = CombatSettings.PointCombat;
        using var text = new SKPaint { Color = styles.ForeignProfileText, TextSize = (float)styles.TextSizePx, IsAntialias = true };
        using var background = new SKPaint { Color = new SKColor(0, 0, 0, 200) };
        double multiplier = _buffer.CurrentSpeed.MotionTimeMultiplier();
        if (multiplier <= 0) multiplier = .25;
        foreach (var state in _renderStates)
        {
            if (state.Source is not { RelationToPlayer: PlayerRelation.Enemy, EnemyStatus: { } status } ||
                state.Source.RenderObjectType is not (SpaceObjectType.NpcShip or SpaceObjectType.Wreck)) continue;
            var point = _camera.WorldToScreen(state.Pose.X, state.Pose.Y, _viewportW, _viewportH);
            if (point.X < 0 || point.Y < 0 || point.X > _viewportW || point.Y > _viewportH) continue;
            var parts = new List<string>();
            if (status.Destroyed) parts.Add(CombatText("EnemyDestroyed", "Уничтожен"));
            else
            {
                if (status.Damaged) parts.Add(CombatText("EnemyDamaged", "Повреждён"));
                if (status.ImmobilizedUntilMotionMs is { } until && until > DefensePresentationTime)
                    parts.Add($"{CombatText("EnemyImmobilized", "Обездвижен")} {(until - DefensePresentationTime) / 1000d / multiplier:0.0} {CombatText("Seconds", "с")}");
            }
            if (parts.Count == 0) continue;
            float x = point.X + 8, y = point.Y + 12;
            if (_labelRenderer.Geometries.TryGetValue(state.Source.ObjectId, out var plaque))
            { x = plaque.PlaqueRect.Left; y = plaque.PlaqueRect.Bottom + 4; }
            EnemyStatusBadges[state.Source.ObjectId] = Draw(string.Join(" · ", parts), x, y);
        }
        var offsets = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (entry, _) in _combatEffects.HitResults)
        {
            if (entry.HitResult is not { } result) continue;
            var point = _camera.WorldToScreen(entry.X, entry.Y, _viewportW, _viewportH);
            var target = _renderStates.FirstOrDefault(s => s.Source.ObjectId == entry.TargetObjectId &&
                s.Source.RenderObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip or SpaceObjectType.Wreck);
            if (target.Source is not null) point = _camera.WorldToScreen(target.Pose.X, target.Pose.Y, _viewportW, _viewportH);
            if (point.X < 0 || point.Y < 0 || point.X > _viewportW || point.Y > _viewportH) continue;
            string value = result switch
            {
                ProjectileHitResult.Core => CombatText("Hit.Core", "Ядро"),
                ProjectileHitResult.Edge => CombatText("Hit.Edge", "Край"),
                ProjectileHitResult.Miss => CombatText("Hit.Miss", "Мимо"),
                ProjectileHitResult.ShotDown => CombatText("Hit.ShotDown", "Сбита"),
                _ => "—"
            };
            string key = entry.TargetObjectId.Length > 0 ? entry.TargetObjectId : entry.ProjectileObjectId;
            int offset = offsets.GetValueOrDefault(key); offsets[key] = offset + 1;
            PointHitLabels[entry.EventId] = Draw(value, point.X + 10, point.Y - 24 - offset * (text.TextSize + 6));
        }

        PointFeedbackLabel Draw(string value, float x, float y)
        {
            while (value.Length > 1 && text.MeasureText(value) + 8 > available.Width) value = value[..^1];
            float width = Math.Min(available.Width, text.MeasureText(value) + 8);
            float height = Math.Min(available.Height, text.TextSize + 6);
            x = Math.Clamp(x, available.Left, available.Right - width);
            y = Math.Clamp(y, available.Top, available.Bottom - height);
            var bounds = new SKRect(x, y, x + width, y + height);
            canvas.Save(); canvas.ClipRect(bounds); canvas.DrawRect(bounds, background);
            canvas.DrawText(value, x + 4, y + text.TextSize, text); canvas.Restore();
            return new(bounds, value);
        }
    }
    private SKPaint? _combatCorePaint;
    private SKPaint? _combatHaloPaint;
    private SKMaskFilter? _combatBlur;
    internal const float CombatMarkerRadius = 2.5f;

    private void UpdateCombatImportance()
    {
        _combatImportantIds.Clear();
        foreach (var state in _renderStates)
        {
            if (state.Source.OwnProjectile is not null || state.Source.ObservedProjectile is not null)
                _combatImportantIds.Add(state.Source.ObjectId);
            if (ObjectLabelRenderer.HasSelectableShipLabel(state.Source)) _combatImportantIds.Add(state.Source.ObjectId);
            if (state.Source.Countermeasure is { } defenseFlight)
            {
                _combatImportantIds.Add(state.Source.ObjectId);
                _combatImportantIds.Add(defenseFlight.TargetTorpedoId);
            }
            if (state.Source.Torpedo is not { } torpedo) continue;
            _combatImportantIds.Add(state.Source.ObjectId);
            _combatImportantIds.Add(torpedo.TargetObjectId);
        }
    }

    internal static bool HasCombatMarker(ObjectMotionSnapshot source) =>
        source.OwnProjectile is not null || source.ObservedProjectile is not null || source.RenderObjectType == SpaceObjectType.Wreck ||
        source is { RenderObjectType: SpaceObjectType.Missile, Torpedo: not null } or
        { RenderObjectType: SpaceObjectType.Countermeasure, Countermeasure: not null };

    private void DrawCombatMarker(SKCanvas canvas, ObjectRenderState state, float x, float y)
    {
        _combatCorePaint ??= new SKPaint { IsAntialias = true };
        if (state.Source.OwnProjectile is not null || state.Source.ObservedProjectile is not null)
        {
            var kind = state.Source.OwnProjectile?.Kind ?? state.Source.ObservedProjectile!.Kind;
            _combatCorePaint.Color = kind == ProjectileKind.Countermissile ? CombatSettings.Countermeasure : CombatSettings.Torpedo;
            canvas.DrawCircle(x, y, (float)CombatSettings.PointCombat.ProjectileMarkerRadiusPx, _combatCorePaint);
            return;
        }
        if (state.Source.Torpedo is not null || state.Source.Countermeasure is not null)
        {
            _combatBlur ??= SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 2f);
            _combatHaloPaint ??= new SKPaint { IsAntialias = true, MaskFilter = _combatBlur };
            _combatHaloPaint.Color = state.Source.Countermeasure is null ? CombatSettings.Torpedo : CombatSettings.Countermeasure;
            canvas.DrawCircle(x, y, CombatMarkerRadius, _combatHaloPaint);
            _combatCorePaint.Color = _combatHaloPaint.Color;
        }
        else _combatCorePaint.Color = CombatSettings.Wreck;
        canvas.DrawCircle(x, y, CombatMarkerRadius, _combatCorePaint);
    }

    private void ReleaseCombatPaints()
    {
        _combatTrajectoryPaint?.Dispose();
        _combatTrajectoryPaint = null;
        _combatDash?.Dispose();
        _combatDash = null;
        _combatTrajectoryPath?.Dispose();
        _combatTrajectoryPath = null;
        _combatExplosionPaint?.Dispose();
        _combatExplosionPaint = null;
        _combatHaloPaint?.Dispose();
        _combatCorePaint?.Dispose();
        _combatBlur?.Dispose();
        _combatHaloPaint = null;
        _combatCorePaint = null;
        _combatBlur = null;
    }

    private bool _torpedoSubmitPending;
    private string? _pendingTorpedoCommandId;
    private Task? _torpedoSendTask;
    private bool _torpedoSendFailed;
    private string? _pendingTorpedoTargetId;
    private bool _torpedoRejectedOutOfRange;
    private string? _torpedoRejectionReason;

    private void RefreshTorpedoSubmission()
    {
        if (_torpedoSendTask is { IsFaulted: true }) _ = _torpedoSendTask.Exception;
        if (!_torpedoSubmitPending) return;
        var result = _pendingTorpedoCommandId is { } id ? _buffer.FindCommandResult(id) : null;
        if (result is { Status: not CommandResultStatus.Deferred })
        {
            _torpedoRejectionReason = result.Status == CommandResultStatus.Rejected ? result.ReasonCode : null;
            _torpedoRejectedOutOfRange = result.Status == CommandResultStatus.Rejected && result.ReasonCode == "target_out_of_range";
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
        var snapshot = _buffer.Presented!.Snapshot;
        string moduleId = FindLauncher()!.ModuleId;
        string? targetId = _selectedObjectId;
        _pendingTorpedoTargetId = targetId;
        _torpedoRejectedOutOfRange = false;
        _torpedoRejectionReason = null;
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

    private bool IsSelfDestructEnabled()
    {
        RefreshTorpedoSubmission();
        return !_torpedoSubmitPending && _buffer.Presented?.Snapshot.ActiveDialogue is null &&
            FindLauncher() is { LauncherCombat: { } combat } launcher &&
            !combat.ActiveTorpedoIds.IsDefaultOrEmpty &&
            launcher.Commands.Any(c => c.CommandTypeId == CombatCommandTypes.SelfDestruct);
    }

    private void SendSelfDestruct()
    {
        if (_handle is null || !IsSelfDestructEnabled()) return;
        var launcher = FindLauncher()!;
        var snapshot = _buffer.Presented!.Snapshot;
        var activeIds = launcher.LauncherCombat!.ActiveTorpedoIds;
        string targetId = _selectedObjectId is { } selected && activeIds.Contains(selected)
            ? selected : activeIds[0];
        _pendingTorpedoTargetId = targetId;
        _torpedoRejectedOutOfRange = false;
        _torpedoSubmitPending = true;
        _torpedoSendFailed = false;
        _pendingTorpedoCommandId = null;
        _torpedoSendTask = null;
        try
        {
            _torpedoSendTask = _handle.SendCommandAsync(snapshot.PlayerShipObjectId!, launcher.ModuleId,
                CombatCommandTypes.SelfDestruct, out _pendingTorpedoCommandId, targetId).AsTask();
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
        var modules = _buffer.Presented?.Snapshot.InstalledModules;
        return moduleId is null || modules is null || modules.Value.IsDefaultOrEmpty
            ? null : modules.Value.FirstOrDefault(m => m.ModuleId == moduleId);
    }

    private bool IsTorpedoFireEnabled()
    {
        RefreshTorpedoSubmission();
        if (_torpedoSubmitPending) return false;
        var snapshot = _buffer.Presented?.Snapshot;
        if (snapshot is null || snapshot.ActiveDialogue is not null ||
            FindPlayerShipMotion(snapshot) is not { IsDestroyed: false } ||
            _selectedObjectId is null || _selectedObjectId == snapshot.PlayerShipObjectId)
            return false;
        return snapshot.Objects.Any(o => o.ObjectId == _selectedObjectId && !o.IsDestroyed && o.RenderObjectType != SpaceObjectType.Countermeasure && o.Countermeasure is null) &&
            FindLauncher() is
            {
                PowerState: "On", OperationalState: "Ready", StructurePoints: > 0,
                ActiveCommandType: null, LauncherCombat.Operator: not null, LauncherCombat.IsBrokenUntilDock: false
            } launcher && !launcher.Commands.IsDefaultOrEmpty &&
            (launcher.LauncherCombat!.ReadyAtMotionTimeMs is null ||
             launcher.LauncherCombat.ReadyAtMotionTimeMs <= snapshot.MotionTimeMs) &&
            launcher.Commands.Any(c => c.CommandTypeId == CombatCommandTypes.Fire && c.Target == "object") &&
            IsSelectedTargetInTorpedoRange(snapshot, launcher.LauncherCombat!);
    }

    private bool IsSelectedTargetInTorpedoRange(AuthoritativeSnapshot snapshot, LauncherCombatSnapshot launcher) =>
        launcher.RangeKm is { } range && FindPlayerShipMotion(snapshot) is { } owner &&
        snapshot.Objects.FirstOrDefault(o => o.ObjectId == _selectedObjectId) is { } target &&
        CombatRangeMath.IsWithinRange(owner, target, range);

    private string ReadyLauncherStatus(LauncherCombatSnapshot launcher)
    {
        string status = LauncherModeStatus(launcher, _buffer.Presented?.Snapshot.MotionTimeMs ?? 0);
        if (_torpedoRejectedOutOfRange && _selectedObjectId == _pendingTorpedoTargetId)
            return status + " · Пуск отклонён: цель вне дальности";
        if (launcher.RangeKm is not { } range || !double.IsFinite(range) || range <= 0) return status + " · Дальность неизвестна";
        if (_buffer.Presented?.Snapshot is { } snapshot && _selectedObjectId is not null &&
            _selectedObjectId != snapshot.PlayerShipObjectId && snapshot.Objects.Any(o => o.ObjectId == _selectedObjectId) &&
            !IsSelectedTargetInTorpedoRange(snapshot, launcher))
            return status + $" · Вне дальности ({range:0.##} км)";
        return status;
    }

    private InstalledModuleSnapshot? FindDefense() => _buffer.Presented?.Snapshot is { InstalledModules.IsDefaultOrEmpty: false } snapshot
        ? snapshot.InstalledModules.FirstOrDefault(m => m.Defense is not null) : null;
    private bool IsDefenseToggleEnabled(string commandType)
    {
        var snapshot = _buffer.Presented?.Snapshot;
        return snapshot is not null && snapshot.ActiveDialogue is null && FindPlayerShipMotion(snapshot) is { IsDestroyed: false } &&
            FindDefense() is { Defense: { } defense } module && !module.Commands.IsDefaultOrEmpty && module.Commands.Any(c => c.CommandTypeId == commandType) &&
            defense.AutoEnabled != (commandType == DefenseCommandTypes.Enable);
    }
    private long DefensePresentationTime => _buffer.LatestPrediction is { } prediction
        ? prediction.BufferedSnapshot.Snapshot.MotionTimeMs + CombatPredictionDelta(prediction)
        : _buffer.Presented?.Snapshot.MotionTimeMs ?? 0;
    private string? GetDefenseStatus() => FindDefense()?.Defense is { } defense
        ? DefenseStatusText(defense, DefensePresentationTime) : null;
    internal static string WeaponOperatorText(WeaponOperatorSnapshot? op) => op is null ? CombatText("NoOperator", "Нет оператора") :
        $"{op.DisplayName} · {CombatText("SkillMetadata", "Навык (справочно)")} {op.Skill}";
    private string? GetWeaponOperatorText(string panel) => panel switch
    {
        "Torpedo Launcher" => WeaponOperatorText(FindLauncher()?.LauncherCombat?.Operator),
        "Countermeasure Launcher" => WeaponOperatorText(FindDefense()?.Defense?.Operator),
        _ => null
    };
    internal static string DefenseStatusText(DefenseSnapshot defense, double time) =>
        (defense.AutoEnabled ? CombatText("AutoOn", "Авто ВКЛ") : CombatText("AutoOff", "Авто ВЫКЛ")) + " · " + (defense.State switch
        {
            DefenseState.NoOperator => CombatText("NoOperator", "Нет оператора"),
            DefenseState.Guiding => CombatText("Guiding", "Наведение"),
            DefenseState.Reloading => CombatText("Reloading", "Перезарядка") + $" {Math.Max(0, (defense.ReloadDueMotionTimeMs!.Value - time) / 1000):0.0} " + CombatText("Seconds", "с"),
            _ => CombatText("Ready", "Готов")
        });

    private string? GetLauncherStatus()
    {
        RefreshTorpedoSubmission();
        if (_torpedoSubmitPending) return _pendingTorpedoCommandId is { } id &&
            _buffer.FindCommandAcceptance(id) is { Accepted: true } ? CombatText("Accepted", "Команда принята") : "Sending...";
        if (_torpedoRejectionReason is { } reason && !_torpedoRejectedOutOfRange) return CombatText("Reason." + reason, reason == CommandReasonCodes.TargetDestroyed ? "Цель уже уничтожена" : reason);
        if (_torpedoSendFailed) return "Send failed";
        RefreshFireModeSubmission();
        if (_pendingFireModeId is not null) return CombatText("Sending", "Отправка...");
        return FindLauncher() switch
        {
            { LauncherCombat.IsBrokenUntilDock: true, LauncherCombat: { } launcher } =>
                LauncherModeStatus(launcher, _buffer.Presented?.Snapshot.MotionTimeMs ?? 0),
            { LauncherCombat.Operator: null } => "Нет оператора",
            { PowerState: "On", OperationalState: "Ready", StructurePoints: > 0, LauncherCombat: { } launcher } => ReadyLauncherStatus(launcher),
            { LauncherCombat: not null } => "Unavailable",
            _ => null
        };
    }
}
