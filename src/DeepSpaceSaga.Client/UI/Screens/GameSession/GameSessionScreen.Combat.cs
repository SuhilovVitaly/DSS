using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    internal TorpedoRoute? LaunchPreviewRoute { get; private set; }
    internal CombatTrajectoryProjector.Geometry? LaunchPreviewGeometry { get; private set; }
    private (ObjectMotionSnapshot Owner, ObjectMotionSnapshot Target, LauncherCombatSnapshot Launcher, long Time)? _launchPreviewKey;
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
            if (obj.Torpedo is not { } flight) continue;
            _combatPoseObjectIds.Add(obj.ObjectId);
            _combatPoseObjectIds.Add(flight.TargetObjectId);
        }
        if (IsLaunchPreviewRequested())
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
        if (viewportResized || prediction is null || !IsLaunchPreviewRequested() ||
            FindLauncher()?.LauncherCombat is not { } launcher ||
            FindPlayerShip(_renderStates) is not { } ownerState ||
            FindRenderStateById(_selectedObjectId) is not { } targetState)
        {
            ClearLaunchPreview();
            return;
        }

        // Guidance uses confirmed motion, never the visual correction or frozen pose.
        // A confirmed paused snapshot fixes physical time even if the presentation
        // buffer retained an earlier extrapolation lead for ordinary map objects.
        long delta = CombatPredictionDelta(prediction);
        var owner = _predictor.Predict(ownerState.Source, delta);
        var target = _predictor.Predict(targetState.Source, delta);
        long time = prediction.BufferedSnapshot.Snapshot.MotionTimeMs + delta;
        var key = (owner, target, launcher, time);
        if (_launchPreviewKey != key)
        {
            LaunchPreviewRoute = TorpedoGuidanceMath.Plan(owner, target, launcher.SpeedKmS, launcher.TurnRateDegPerSec, time);
            _launchPreviewKey = key;
        }
        // A value-only proposed flight feeds the same display sampler. No world object,
        // command, receipt, RNG or launcher state is created or changed here.
        var proposed = new TorpedoSnapshot(owner.ObjectId, FindLauncher()!.ModuleId, target.ObjectId, time,
            launcher.SpeedKmS, launcher.TurnRateDegPerSec, launcher.Damage, 0, LaunchPreviewRoute!, []);
        LaunchPreviewGeometry = CombatTrajectoryProjector.Project(proposed, target, _predictor, _camera, _viewportW, _viewportH);
        DrawCombatGeometry(canvas, LaunchPreviewGeometry, CombatSettings.Preview, CombatSettings.Preview, CombatSettings.Preview);
    }

    private readonly Dictionary<string, CombatTrajectoryProjector.Geometry> _combatTrajectories = new(StringComparer.Ordinal);
    internal IReadOnlyDictionary<string, CombatTrajectoryProjector.Geometry> CombatTrajectories => _combatTrajectories;
    private SKPaint? _combatTrajectoryPaint;
    private SKPathEffect? _combatDash;
    private SKPath? _combatTrajectoryPath;

    private void DrawCombatTrajectories(SKCanvas canvas, BufferedSnapshot? buffered)
    {
        _combatTrajectories.Clear();
        _combatEffects.Receive(buffered?.Snapshot.CombatImpacts ?? default, buffered?.ReceivedAtTimestamp);
        foreach (var state in _renderStates)
        {
            if (state.Source.RenderObjectType != SpaceObjectType.Missile || state.Pose.Motion.Torpedo is not { } flight) continue;
            var target = FindRenderStateById(flight.TargetObjectId)?.Source;
            if (target is not null && buffered is not null)
            {
                long delta = Math.Max(0, flight.Route.StartMotionTimeMs + (long)flight.Route.ElapsedMs - buffered.Snapshot.MotionTimeMs);
                target = _predictor.Predict(target, delta);
            }
            var geometry = CombatTrajectoryProjector.Project(flight, target, _predictor, _camera, _viewportW, _viewportH);
            _combatTrajectories[state.Source.ObjectId] = geometry;
            DrawCombatGeometry(canvas, geometry, CombatSettings.Trail, CombatSettings.Prediction, CombatSettings.Intercept);
        }
        foreach (var effect in _combatEffects.Active)
        {
            var geometry = new CombatTrajectoryProjector.Geometry(
                CombatTrajectoryProjector.History(effect.Impact.FinalTrail, _camera), [], [], null);
            _combatTrajectories[effect.Impact.TorpedoObjectId] = geometry;
            DrawCombatGeometry(canvas, geometry, CombatSettings.Trail, CombatSettings.Prediction, CombatSettings.Intercept);
        }
    }

    private void DrawCombatGeometry(SKCanvas canvas, CombatTrajectoryProjector.Geometry geometry,
        SKColor trail, SKColor prediction, SKColor intercept)
    {
        _combatTrajectoryPaint ??= new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f, IsAntialias = true };
        _combatDash ??= SKPathEffect.CreateDash([6f, 4f], 0);
        _combatTrajectoryPath ??= new SKPath();
        DrawPath(geometry.Travelled, trail, false);
        DrawPath(geometry.Prediction, prediction, true);
        DrawPath(geometry.Target, prediction, true);
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

        void DrawPath(IReadOnlyList<FutureTrajectoryPoint> points, SKColor color, bool dashed)
        {
            _combatTrajectoryPaint.Color = color;
            _combatTrajectoryPaint.PathEffect = dashed ? _combatDash : null;
            CombatTrajectoryProjector.BuildPath(_combatTrajectoryPath, points, _camera, _viewportW, _viewportH);
            canvas.DrawPath(_combatTrajectoryPath, _combatTrajectoryPaint);
        }
    }

    private TorpedoInspectionData? BuildTorpedoInspection(ObjectRenderState state)
    {
        if (state.Source is not { RenderObjectType: SpaceObjectType.Missile, Torpedo: { } flight }) return null;
        var predicted = state.Pose.Motion.Torpedo ?? flight;
        // Route elapsed is advanced by the shared predictor. Camera reconciliation
        // changes only X/Y/heading; neither it nor pixels can change travelled distance.
        double extraMs = Math.Max(0, predicted.Route.ElapsedMs - flight.Route.ElapsedMs);
        double travelledKm = flight.DistanceTravelledWorldUnits / 10 + flight.SpeedKmS * extraMs / 1000;
        double? etaSeconds = predicted.Route.HasIntercept && flight.PredictedImpactMotionTimeMs is { } impact
            ? Math.Max(0, ((double)impact - predicted.Route.StartMotionTimeMs - predicted.Route.ElapsedMs) / 1000)
            : null;
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
        if (buffered is not null)
            _combatEffects.Receive(buffered.Snapshot.CombatImpacts, buffered.ReceivedAtTimestamp);
        else _combatEffects.Receive(default);
        if (_combatEffects.Active.Count == 0) return;
        _combatExplosionPaint ??= new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 2f, IsAntialias = true };
        foreach (var effect in _combatEffects.Active)
        {
            var (x, y) = _camera.WorldToScreen(effect.Impact.X, effect.Impact.Y, _viewportW, _viewportH);
            float radius = effect.RadiusPx(now);
            if (radius <= 0 || x < -52 || y < -52 || x > _viewportW + 52 || y > _viewportH + 52) continue;
            _combatExplosionPaint.Color = CombatSettings.Explosion.WithAlpha(effect.Alpha(now, CombatSettings.Explosion.Alpha));
            canvas.DrawCircle(x, y, radius, _combatExplosionPaint);
        }
    }
    private readonly HashSet<string> _combatImportantIds = new(StringComparer.Ordinal);
    private SKPaint? _combatCorePaint;
    private SKPaint? _combatHaloPaint;
    private SKMaskFilter? _combatBlur;
    internal const float CombatMarkerRadius = 2.5f;

    private void UpdateCombatImportance()
    {
        _combatImportantIds.Clear();
        foreach (var state in _renderStates)
        {
            if (ObjectLabelRenderer.HasHullBar(state.Source)) _combatImportantIds.Add(state.Source.ObjectId);
            if (state.Source.Torpedo is not { } torpedo) continue;
            _combatImportantIds.Add(state.Source.ObjectId);
            _combatImportantIds.Add(torpedo.TargetObjectId);
        }
    }

    internal static bool HasCombatMarker(ObjectMotionSnapshot source) =>
        source.RenderObjectType == SpaceObjectType.Wreck ||
        source is { RenderObjectType: SpaceObjectType.Missile, Torpedo: not null };

    private void DrawCombatMarker(SKCanvas canvas, ObjectRenderState state, float x, float y)
    {
        _combatCorePaint ??= new SKPaint { IsAntialias = true };
        if (state.Source.Torpedo is not null)
        {
            _combatBlur ??= SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 2f);
            _combatHaloPaint ??= new SKPaint { IsAntialias = true, MaskFilter = _combatBlur };
            _combatHaloPaint.Color = CombatSettings.Torpedo;
            canvas.DrawCircle(x, y, CombatMarkerRadius, _combatHaloPaint);
            _combatCorePaint.Color = CombatSettings.Torpedo;
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
