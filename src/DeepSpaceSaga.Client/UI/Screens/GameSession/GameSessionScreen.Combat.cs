using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private readonly CombatJournalPanel _combatJournalPanel = new();
    private readonly HashSet<string> _combatPoseObjectIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, CombatTrajectoryProjector.Geometry> _combatTrajectories = new(StringComparer.Ordinal);
    internal IReadOnlyDictionary<string, CombatTrajectoryProjector.Geometry> CombatTrajectories => _combatTrajectories;
    private readonly CombatEffectStore _combatEffects;
    internal CombatEffectStore CombatEffects => _combatEffects;
    internal string? HoveredCommandTypeId => _commandsPanel.HoveredCommandTypeId;
    internal static string CombatText(string key, string fallback) => Localization.Combat(key);
    internal static string DefenseReasonText(string reason) => CombatText("Reason." + reason, CombatText("CommandRejected", "Command rejected"));

    private static long CombatPredictionDelta(SnapshotPrediction prediction) =>
        prediction.CurrentSpeed == SimulationSpeed.Speed0 && prediction.BufferedSnapshot.Snapshot.CurrentSpeed == SimulationSpeed.Speed0
            ? 0 : prediction.EffectivePredictionDeltaMs;

    private long DefensePresentationTime => _buffer.LatestPrediction is { } prediction
        ? prediction.BufferedSnapshot.Snapshot.MotionTimeMs + CombatPredictionDelta(prediction)
        : _buffer.Presented?.Snapshot.MotionTimeMs ?? 0;
    private void UpdateCombatPoseObjects(AuthoritativeSnapshot snapshot)
    {
        _combatPoseObjectIds.Clear();
        foreach (var obj in snapshot.Objects)
            if (obj.OwnProjectile is not null || obj.ObservedProjectile is not null) _combatPoseObjectIds.Add(obj.ObjectId);
    }

    private void DrawCombatTrajectories(SKCanvas canvas, SnapshotPrediction? prediction)
    {
        _combatTrajectories.Clear(); PointProjectileVisuals.Clear();
        foreach (var state in _renderStates)
            if (state.Source.OwnProjectile is not null || state.Source.ObservedProjectile is not null) DrawPointProjectile(canvas, state, prediction);
    }

    private void AlignFlightStart(CombatTrajectoryProjector.Geometry geometry, ObjectRenderState state)
    {
        if (geometry.Prediction.Count > 0) geometry.Prediction[0] = new(state.Pose.X, state.Pose.Y);
        else geometry.Prediction.Add(new(state.Pose.X, state.Pose.Y));
    }

    private void DrawCombatEffects(SKCanvas canvas, BufferedSnapshot? buffered, long now)
    {
        _combatEffects.PointStyles = CombatSettings.PointCombat;
        _combatEffects.ReceiveJournal(buffered?.Snapshot.CombatJournal ?? default, buffered?.ReceivedAtTimestamp ?? now);
        DrawPointEffects(canvas, now); DrawPointHitFeedback(canvas);
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
    internal const float CombatMarkerRadius = 2.5f;
    private void UpdateCombatImportance()
    {
        _combatImportantIds.Clear();
        foreach (var state in _renderStates)
            if (state.Source.OwnProjectile is not null || state.Source.ObservedProjectile is not null || ObjectLabelRenderer.HasSelectableShipLabel(state.Source))
                _combatImportantIds.Add(state.Source.ObjectId);
    }
    internal static bool HasCombatMarker(ObjectMotionSnapshot source) => source.OwnProjectile is not null ||
        source.ObservedProjectile is not null || source.RenderObjectType == SpaceObjectType.Wreck;
    private void DrawCombatMarker(SKCanvas canvas, ObjectRenderState state, float x, float y)
    {
        _combatCorePaint ??= new SKPaint { IsAntialias = true };
        if (state.Source.OwnProjectile is not null || state.Source.ObservedProjectile is not null)
        {
            var kind = state.Source.OwnProjectile?.Kind ?? state.Source.ObservedProjectile!.Kind;
            _combatCorePaint.Color = kind == ProjectileKind.Countermissile ? CombatSettings.Countermeasure : CombatSettings.Torpedo;
            canvas.DrawCircle(x, y, (float)CombatSettings.PointCombat.ProjectileMarkerRadiusPx, _combatCorePaint); return;
        }
        _combatCorePaint.Color = CombatSettings.Wreck;
        canvas.DrawCircle(x, y, CombatMarkerRadius, _combatCorePaint);
    }
    private void ReleaseCombatPaints() { _combatCorePaint?.Dispose(); _combatCorePaint = null; }
}
