using System.Globalization;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    internal sealed record PointProjectileVisual(string Profile, double? PhysicalRadiusPx, double? CoreRadiusPx,
        SKPoint? DetonationCenter, double? RemainingRealSeconds, bool HasMinimumMarker, string Label, SKRect LabelBounds = default);
    internal readonly Dictionary<string, PointProjectileVisual> PointProjectileVisuals = new(StringComparer.Ordinal);

    private void DrawPointProjectile(SKCanvas canvas, ObjectRenderState state, SnapshotPrediction? prediction)
    {
        if (state.Source.ObservedProjectile is not null)
        {
            DrawObservedProjectile(canvas, state);
            return;
        }
        var geometry = PreparedFlight(state.Source.ObjectId, prediction);
        if (geometry is null) return;
        AlignFlightStart(geometry, state);
        _combatTrajectories[state.Source.ObjectId] = geometry;
        var own = state.Source.OwnProjectile;
        var observed = state.Source.ObservedProjectile;
        var styles = CombatSettings.PointCombat;
        var kind = own?.Kind ?? observed!.Kind;
        string profile = own?.ProfileId ?? observed!.ProfileId;
        using var line = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = (float)styles.LineWidthPx, IsAntialias = true };
        using var dash = SKPathEffect.CreateDash([(float)styles.DashLengthPx, (float)styles.DashGapPx], 0);
        using var path = new SKPath();
        if (own is not null)
        {
            line.Color = kind == ProjectileKind.Torpedo ? CombatSettings.Trail : CombatSettings.CountermeasureTrail;
            CombatTrajectoryProjector.BuildPath(path, [new(own.StartX, own.StartY), new(state.Pose.X, state.Pose.Y)], _camera, _viewportW, _viewportH);
            canvas.DrawPath(path, line);
        }
        line.Color = own is null ? styles.ForeignTrace : kind == ProjectileKind.Torpedo ? CombatSettings.Prediction : CombatSettings.CountermeasurePrediction;
        using var observedDash = own is null && ObservedThreatVisuals.TryGetValue(state.Source.ObjectId, out var threatVisual) && threatVisual.Dash is { } pattern
            ? SKPathEffect.CreateDash(pattern, 0) : null;
        if (own is null && ObservedThreatVisuals.TryGetValue(state.Source.ObjectId, out var classified))
        { line.Color = classified.Color; line.StrokeWidth = classified.WidthPx; line.PathEffect = observedDash; }
        else line.PathEffect = dash;
        CombatTrajectoryProjector.BuildPath(path, geometry.Prediction, _camera, _viewportW, _viewportH);
        canvas.DrawPath(path, line);
        line.PathEffect = null;

        SKPoint? center = null;
        double? radius = null, coreRadius = null, seconds = null;
        bool minimum = false;
        if (own is not null && geometry.Detonation is { } end)
        {
            var screen = _camera.WorldToScreen(end.X, end.Y, _viewportW, _viewportH);
            double pixels = own.BlastRadiusKm * 10 * _camera.PixelsPerWorldUnit;
            if (double.IsFinite(pixels) && pixels > 0 && pixels < float.MaxValue)
            {
                center = new(screen.X, screen.Y);
                radius = pixels;
                coreRadius = kind == ProjectileKind.Torpedo ? pixels * own.CoreFraction : 0;
                minimum = pixels < styles.MinimumBlastMarkerDiameterPx;
                DrawPointBlast(canvas, center.Value, pixels, own.CoreFraction, kind, 1, line);
                if (minimum)
                {
                    float half = (float)(styles.MinimumBlastMarkerDiameterPx / 2);
                    canvas.DrawRect(center.Value.X - half, center.Value.Y - half, half * 2, half * 2, line);
                }
            }
            double multiplier = prediction?.CurrentSpeed.MotionTimeMultiplier() ?? .25;
            seconds = geometry.DetonationEtaSeconds / (multiplier > 0 ? multiplier : .25);
        }
        string label = CombatText("Profile." + profile, profile) + (seconds is { } remaining
            ? " · " + remaining.ToString("0.0", CultureInfo.InvariantCulture) + " " + CombatText("Seconds", "s") : string.Empty);
        var marker = _camera.WorldToScreen(state.Pose.X, state.Pose.Y, _viewportW, _viewportH);
        var anchor = center ?? new SKPoint(marker.X, marker.Y);
        // Map labels are physical screen pixels; DPI scaling belongs to the UI overlay pass.
        using var text = new SKPaint { Color = styles.ForeignProfileText, TextSize = (float)styles.TextSizePx, IsAntialias = true };
        var labelBounds = SKRect.Empty;
        if (anchor.X >= 0 && anchor.Y >= 0 && anchor.X <= _viewportW && anchor.Y <= _viewportH)
        {
            float width = text.MeasureText(label);
            float labelX = Math.Clamp(anchor.X + (float)styles.ReticleRadiusPx, 4, Math.Max(4, _viewportW - width - 4));
            float baseline = Math.Clamp(anchor.Y - (float)styles.ReticleRadiusPx, 4 - text.FontMetrics.Ascent,
                Math.Max(4 - text.FontMetrics.Ascent, _viewportH - text.FontMetrics.Descent - 4));
            labelBounds = new(labelX, baseline + text.FontMetrics.Ascent, labelX + width, baseline + text.FontMetrics.Descent);
            canvas.DrawText(label, labelX, baseline, text);
        }
        PointProjectileVisuals[state.Source.ObjectId] = new(profile, radius, coreRadius, center, seconds, minimum, label, labelBounds);
    }

    private readonly ObservedProjectileTrail _observedProjectileTrail = new();
    internal ObservedProjectileTrail ObservedTrails => _observedProjectileTrail;

    internal static double ObservedTailPixels(double speedKmS, double ppu) =>
        Math.Clamp(speedKmS * 10 * ppu * .2, 0, 18);

    private void DrawObservedProjectile(SKCanvas canvas, ObjectRenderState state)
    {
        var observed = state.Source.ObservedProjectile!;
        using var path = new SKPath();
        using var line = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true,
            Color = CombatSettings.PointCombat.ForeignTrace
        };
        CombatTrajectoryProjector.BuildPath(path, _observedProjectileTrail.Get(state.Source.ObjectId), _camera, _viewportW, _viewportH);
        canvas.DrawPath(path, line);
        double length = ObservedTailPixels(observed.SpeedKmS, _camera.PixelsPerWorldUnit) / _camera.PixelsPerWorldUnit;
        double norm = double.Hypot(observed.DirectionX, observed.DirectionY);
        if (norm > 0 && length > 0)
        {
            line.Color = ObservedThreatVisuals.TryGetValue(state.Source.ObjectId, out var threat) ? threat.Color : CombatSettings.Torpedo;
            CombatTrajectoryProjector.BuildPath(path, [new(state.Pose.X - observed.DirectionX / norm * length,
                state.Pose.Y - observed.DirectionY / norm * length), new(state.Pose.X, state.Pose.Y)], _camera, _viewportW, _viewportH);
            canvas.DrawPath(path, line);
        }
        _combatTrajectories[state.Source.ObjectId] = new([], [], null) { IsObserved = true, Kind = observed.Kind };
        PointProjectileVisuals[state.Source.ObjectId] = new(observed.ProfileId, null, null, null, null, false, string.Empty);
    }

    private void DrawPointBlast(SKCanvas canvas, SKPoint center, double radius, double coreFraction,
        ProjectileKind kind, double alphaFraction, SKPaint line)
    {
        if (center.X + radius < 0 || center.Y + radius < 0 || center.X - radius > _viewportW || center.Y - radius > _viewportH) return;
        var styles = CombatSettings.PointCombat;
        bool counter = kind == ProjectileKind.Countermissile;
        using var fill = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        SKColor Fade(SKColor color) => color.WithAlpha((byte)Math.Round(color.Alpha * Math.Clamp(alphaFraction, 0, 1)));
        fill.Color = Fade(counter ? styles.CounterBlastFill : styles.BlastFill);
        canvas.DrawCircle(center, (float)radius, fill);
        if (!counter)
        {
            fill.Color = Fade(styles.BlastCoreFill);
            canvas.DrawCircle(center, (float)(radius * coreFraction), fill);
        }
        line.Color = Fade(counter ? styles.CounterBlastStroke : styles.BlastStroke);
        canvas.DrawCircle(center, (float)radius, line);
    }

    private void DrawPointEffects(SKCanvas canvas, long now)
    {
        var styles = CombatSettings.PointCombat;
        using var line = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = (float)styles.LineWidthPx, IsAntialias = true };
        foreach (var (entry, started) in _combatEffects.PointEffects)
        {
            double progress = _combatEffects.PointProgress(entry, started, now);
            var screen = _camera.WorldToScreen(entry.X, entry.Y, _viewportW, _viewportH);
            var center = new SKPoint(screen.X, screen.Y);
            if (entry.Type == CombatEventType.ShotDown)
            {
                line.Color = styles.ShotDown.WithAlpha((byte)Math.Round(styles.ShotDown.Alpha * (1 - progress)));
                float half = (float)styles.ReticleRadiusPx;
                canvas.DrawLine(center.X - half, center.Y - half, center.X + half, center.Y + half, line);
                canvas.DrawLine(center.X - half, center.Y + half, center.X + half, center.Y - half, line);
            }
            else if (entry.BlastRadiusKm is { } physicalRadius && physicalRadius > 0)
            {
                double radius = physicalRadius * 10 * _camera.PixelsPerWorldUnit;
                if (double.IsFinite(radius) && radius < float.MaxValue)
                    DrawPointBlast(canvas, center, radius, 0, entry.ProjectileKind ?? ProjectileKind.Torpedo, 1 - progress, line);
            }
        }
    }
}

// Stores only positions from the display-admitted scene, never forecasts or future receipts.
internal sealed class ObservedProjectileTrail
{
    private readonly Dictionary<string, List<FutureTrajectoryPoint>> _paths = new(StringComparer.Ordinal);
    private Guid? _session;
    private ulong _sequence;
    private long _motion;
    internal IReadOnlyList<FutureTrajectoryPoint> Get(string id) => _paths.TryGetValue(id, out var path) ? path : [];
    internal void Update(Guid? session, AuthoritativeSnapshot? scene)
    {
        if (scene is null) { _paths.Clear(); return; }
        if (_session != session || scene.SnapshotSequence < _sequence || scene.MotionTimeMs < _motion) _paths.Clear();
        _session = session; _sequence = scene.SnapshotSequence; _motion = scene.MotionTimeMs;
        var ids = scene.Objects.Where(o => o.ObservedProjectile is not null).Select(o => o.ObjectId).ToHashSet(StringComparer.Ordinal);
        foreach (var id in _paths.Keys.Where(id => !ids.Contains(id)).ToArray()) _paths.Remove(id);
        foreach (var obj in scene.Objects.Where(o => o.ObservedProjectile is not null))
        {
            if (!_paths.TryGetValue(obj.ObjectId, out var path)) _paths[obj.ObjectId] = path = [];
            var point = new FutureTrajectoryPoint(obj.X, obj.Y);
            if (path.Count == 0 || path[^1] != point) path.Add(point);
            if (path.Count > 4096) path.RemoveAt(0);
        }
    }
}
