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
    internal sealed record FlightMapLabel(SKRect Bounds, string Text);
    internal readonly Dictionary<string, FlightMapLabel> DefenseFlightLabels = new(StringComparer.Ordinal);
    internal sealed record WeaponRangeRing(string ModuleId, string Kind, double RangeKm, SKPoint Center, float RadiusPx);
    internal readonly List<WeaponRangeRing> WeaponRangeRings = new();
    internal string? DefenseTooltip { get; private set; }
    private void DrawDefenseAnnotations(SKCanvas canvas)
    {
        DefenseTooltip = null;
        DefenseChanceLabels.Clear();
        DefenseFlightLabels.Clear();
        WeaponRangeRings.Clear();
        var occupied = new List<SKRect>();
        using var text = new SKPaint { Color = CombatSettings.DefenseText, TextSize = 12f, IsAntialias = true };
        using var line = new SKPaint { Color = CombatSettings.DefenseRange, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
        DrawHoveredWeaponRanges(canvas, line);
        foreach (var state in _renderStates)
        {
            var (x, y) = _camera.WorldToScreen(state.Pose.X, state.Pose.Y, _viewportW, _viewportH);
            if (state.Source.RenderObjectType == SpaceObjectType.NpcShip && state.Source.Defense is { } defense)
            {
                if (_labelRenderer.Geometries.TryGetValue(state.Source.ObjectId, out var label))
                    canvas.DrawText(DefenseStatusText(defense, DefensePresentationTime), label.PlaqueRect.Left, label.PlaqueRect.Bottom + 22, text);
            }
            if (state.Source.RenderObjectType != SpaceObjectType.Countermeasure || state.Source.Countermeasure is not { } flight) continue;
            if (x < 0 || y < 0 || x > _viewportW || y > _viewportH) continue;
            string chance = $"{flight.FrozenChanceTenths / 10m:0.0}%";
            string remaining = RemainingFlightText(flight, DefensePresentationTime);
            var labelLines = WrapFlightLabel($"{state.Source.ObjectId} · {flight.LauncherModuleId}\n{chance} · {CombatText("RemainingFlight", "Осталось полёта")}: {remaining}", text);
            var bounds = PlaceFlightLabel(x + 8, y - 8, labelLines, text, occupied);
            occupied.Add(bounds);
            DefenseFlightLabels[state.Source.ObjectId] = new(bounds, string.Join("\n", labelLines));
            for (int i = 0; i < labelLines.Count; i++) canvas.DrawText(labelLines[i], bounds.Left, bounds.Top + 12 + 16 * i, text);
            SKPoint? marker = null;
            if (DefenseGeometry.TryGetValue(state.Source.ObjectId, out var geometry) && geometry.Intercept is { } encounter)
            {
                var (mx, my) = _camera.WorldToScreen(encounter.X, encounter.Y, _viewportW, _viewportH);
                marker = new(mx, my);
                if (mx >= 0 && my >= 0 && mx <= _viewportW && my <= _viewportH)
                {
                    var markerBounds = PlaceFlightLabel(mx + 8, my - 8, [chance], text, occupied);
                    occupied.Add(markerBounds);
                    canvas.DrawText(chance, markerBounds.Left, markerBounds.Top + 12, text);
                }
                if (_hasMousePosition && Math.Abs(_mouseX - mx) <= 10 && Math.Abs(_mouseY - my) <= 10)
                    DefenseTooltip = string.Join("\n", ObjectInfoPanel.CountermeasureLines(state.Pose.Motion.Countermeasure ?? flight).Select(l => $"{l.Label}: {l.Value}"));
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

    private void DrawHoveredWeaponRanges(SKCanvas canvas, SKPaint paint)
    {
        if (!_hasMousePosition || _panelVisible && _lastPanelRect.Contains(_uiMouseX, _uiMouseY) ||
            _commandsPanel.PanelAt(_uiMouseX, _uiMouseY) is not { } panel ||
            FindPlayerShip(_renderStates) is not { } player || player.Source.IsDestroyed) return;
        var (x, y) = _camera.WorldToScreen(player.Pose.X, player.Pose.Y, _viewportW, _viewportH);
        if (panel.Weapon is { } weapon && DefenseModule(weapon)?.Defense is { } defense)
        {
            Ring(weapon.ModuleId, "auto", defense.RangeKm, false);
            if (defense.ManualRangeKm is { } manual) Ring(weapon.ModuleId, "manual", manual, true);
        }
        else if (panel.Name == "Torpedo Launcher" && FindLauncher() is { LauncherCombat.RangeKm: { } range } launcher)
            Ring(launcher.ModuleId, "torpedo", range, false);

        void Ring(string module, string kind, double range, bool dashed)
        {
            double pixels = range * 10 * _camera.PixelsPerWorldUnit;
            if (!double.IsFinite(pixels) || pixels <= 0 || pixels > float.MaxValue) return;
            using var effect = dashed ? SKPathEffect.CreateDash([6, 4], 0) : null;
            paint.PathEffect = effect;
            canvas.DrawCircle(x, y, (float)pixels, paint);
            paint.PathEffect = null;
            WeaponRangeRings.Add(new(module, kind, range, new(x, y), (float)pixels));
        }
    }

    private List<string> WrapFlightLabel(string value, SKPaint paint)
    {
        float width = Math.Max(1, Math.Min(340, _viewportW - 16));
        var lines = new List<string>();
        foreach (string source in value.Split('\n'))
        {
            string rest = source;
            while (paint.MeasureText(rest) > width)
            {
                int length = Math.Max(1, (int)paint.BreakText(rest, width));
                int space = rest.LastIndexOf(' ', length - 1, length);
                if (space > 0) length = space;
                lines.Add(rest[..length].TrimEnd());
                rest = rest[length..].TrimStart();
            }
            lines.Add(rest);
        }
        return lines;
    }

    private SKRect PlaceFlightLabel(float x, float y, IReadOnlyList<string> lines, SKPaint paint, List<SKRect> occupied)
    {
        float width = lines.Max(l => paint.MeasureText(l)), height = lines.Count * 16;
        float left = Math.Clamp(x, 8, Math.Max(8, _viewportW - width - 8));
        float top = Math.Clamp(y, 8, Math.Max(8, _viewportH - height - 8));
        var result = new SKRect(left, top, left + width, top + height);
        foreach (float candidate in new[] { top }.Concat(occupied.SelectMany(r => new[] { r.Bottom + 4, r.Top - height - 4 })))
        {
            if (candidate < 8 || candidate + height > _viewportH - 8) continue;
            var bounds = new SKRect(left, candidate, left + width, candidate + height);
            if (occupied.Any(r => r.IntersectsWith(bounds))) continue;
            return bounds;
        }
        return result;
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
