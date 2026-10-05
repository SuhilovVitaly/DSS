using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    internal readonly Dictionary<string, CombatTrajectoryProjector.Geometry> DefenseGeometry = new(StringComparer.Ordinal);
    private void DrawCountermeasureTrajectories(SKCanvas canvas)
    {
        DefenseGeometry.Clear();
        foreach (var state in _renderStates)
        {
            if (state.Source.RenderObjectType != SpaceObjectType.Countermeasure || state.Pose.Motion.Countermeasure is not { } flight) continue;
            using var paint = new SKPaint
            {
                Color = CombatSettings.CountermeasureTrail,
                StrokeWidth = 1.5f,
                Style = SKPaintStyle.Stroke,
                IsAntialias = true
            };
            using var path = new SKPath();
            var points = new List<FutureTrajectoryPoint>();
            if (!flight.Trail.IsDefaultOrEmpty)
                foreach (var segment in flight.Trail) Sample(segment.Segment, 0, segment.Segment.DurationMs);
            double cursor = 0;
            double historyEnd = flight.Trail.IsDefaultOrEmpty ? flight.LaunchMotionTimeMs :
                flight.Trail[^1].StartMotionTimeMs + flight.Trail[^1].Segment.DurationMs;
            foreach (var segment in flight.Route.Segments)
            {
                double start = Math.Max(0, historyEnd - flight.Route.StartMotionTimeMs - cursor);
                double end = Math.Min(segment.DurationMs, flight.Route.ElapsedMs - cursor);
                if (end >= start) Sample(segment, start, end);
                cursor += segment.DurationMs;
            }
            var routeEnd = TorpedoGuidanceMath.PredictPose(flight.Route, flight.Route.ElapsedMs);
            points.Add(new(routeEnd.X, routeEnd.Y));
            CombatTrajectoryProjector.BuildPath(path, points, _camera, _viewportW, _viewportH);
            canvas.DrawPath(path, paint);
            if (flight.Phase != CountermeasurePhase.Guiding) continue;
            var target = _renderStates.Where(s => s.Source.ObjectId == flight.TargetTorpedoId).Select(s => s.Pose.Motion).FirstOrDefault();
            var adapter = new TorpedoSnapshot(flight.OwnerObjectId, flight.LauncherModuleId, flight.TargetTorpedoId,
                flight.LaunchMotionTimeMs, state.Pose.Motion.SpeedKmS, 90, 0, 0, flight.Route, flight.Trail);
            var geometry = CombatTrajectoryProjector.Project(adapter, target, _predictor, _camera, _viewportW, _viewportH);
            DefenseGeometry[state.Source.ObjectId] = geometry;
            DrawCombatGeometry(canvas, geometry, CombatSettings.CountermeasurePrediction, CombatSettings.CountermeasureIntercept);

            void Sample(TorpedoRouteSegment segment, double from, double to)
            {
                int count = segment.AngularVelocityDegPerSec == 0 ? 1 : (int)Math.Clamp(Math.Ceiling((to - from) / 20), 1, 2048);
                for (int i = 0; i <= count; i++)
                {
                    var p = TorpedoGuidanceMath.PredictSegment(segment, from + (to - from) * i / count);
                    points.Add(new(p.X, p.Y));
                }
            }
        }
    }

    internal readonly Dictionary<string, (SKPoint Projectile, SKPoint? Encounter, string Chance)> DefenseChanceLabels = new(StringComparer.Ordinal);
    internal string? DefenseTooltip { get; private set; }
    internal float? SelectedDefenseRadiusPx { get; private set; }
    private void DrawDefenseAnnotations(SKCanvas canvas)
    {
        DefenseTooltip = null;
        SelectedDefenseRadiusPx = null;
        DefenseChanceLabels.Clear();
        using var text = new SKPaint { Color = CombatSettings.DefenseText, TextSize = 12f, IsAntialias = true };
        using var line = new SKPaint { Color = CombatSettings.DefenseRange, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
        foreach (var state in _renderStates)
        {
            var (x, y) = _camera.WorldToScreen(state.Pose.X, state.Pose.Y, _viewportW, _viewportH);
            if (state.Source.RenderObjectType == SpaceObjectType.NpcShip && state.Source.Defense is { } defense)
            {
                if (state.Source.ObjectId == _selectedObjectId)
                {
                    float radius = (float)(defense.RangeKm * 10 * _camera.PixelsPerWorldUnit);
                    SelectedDefenseRadiusPx = radius;
                    canvas.DrawCircle(x, y, radius, line);
                }
                if (_labelRenderer.Geometries.TryGetValue(state.Source.ObjectId, out var label))
                    canvas.DrawText(DefenseStatusText(defense, DefensePresentationTime), label.PlaqueRect.Left, label.PlaqueRect.Bottom + 22, text);
            }
            if (state.Source.RenderObjectType != SpaceObjectType.Countermeasure || state.Source.Countermeasure is not { } flight) continue;
            string chance = $"{flight.FrozenChanceTenths / 10m:0.0}%";
            canvas.DrawText(chance, x + 8, y - 8, text);
            SKPoint? marker = null;
            if (DefenseGeometry.TryGetValue(state.Source.ObjectId, out var geometry) && geometry.Intercept is { } encounter)
            {
                var (mx, my) = _camera.WorldToScreen(encounter.X, encounter.Y, _viewportW, _viewportH);
                marker = new(mx, my);
                canvas.DrawText(chance, mx + 8, my - 8, text);
                if (_hasMousePosition && Math.Abs(_mouseX - mx) <= 10 && Math.Abs(_mouseY - my) <= 10)
                    DefenseTooltip = string.Join("\n", ObjectInfoPanel.CountermeasureLines(flight).Select(l => $"{l.Label}: {l.Value}"));
            }
            DefenseChanceLabels[state.Source.ObjectId] = (new(x, y), marker, chance);
        }
        if (DefenseTooltip is not null)
        {
            string[] lines = DefenseTooltip.Split('\n');
            float width = Math.Min(_viewportW - 16, Math.Max(260, lines.Max(l => text.MeasureText(l)) + 20));
            float left = Math.Clamp(_mouseX + 16, 8, Math.Max(8, _viewportW - width - 8));
            float top = Math.Clamp(_mouseY + 16, 8, Math.Max(8, _viewportH - lines.Length * 18 - 16));
            using var background = new SKPaint { Color = new SKColor(8, 25, 36, 245) };
            canvas.DrawRect(left, top, width, lines.Length * 18 + 12, background);
            for (int i = 0; i < lines.Length; i++) canvas.DrawText(lines[i], left + 8, top + 18 + i * 18, text);
        }
    }

    private void DrawCountermeasureResults(SKCanvas canvas, BufferedSnapshot? buffered)
    {
        _combatEffects.ReceiveJournal(buffered?.Snapshot.CombatJournal ?? default, buffered?.ReceivedAtTimestamp ?? _timestampProvider());
        using var paint = new SKPaint { Color = CombatSettings.DefenseText, IsAntialias = true };
        using var font = new SKFont(SKTypeface.Default, 14);
        foreach (var (entry, _) in _combatEffects.Results)
        {
            var (x, y) = _camera.WorldToScreen(entry.X, entry.Y, _viewportW, _viewportH);
            canvas.DrawText(entry.Type == CombatEventType.Intercept ? "Перехват" : "Промах", x + 10, y - 10, font, paint);
        }
    }
}
