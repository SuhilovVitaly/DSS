using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;
namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    internal readonly Dictionary<string, CombatTrajectoryProjector.Geometry> DefenseGeometry = new(StringComparer.Ordinal);
    private void DrawCountermeasureTrajectories(SKCanvas canvas, SnapshotPrediction? prediction)
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
            DrawCountermeasureTrail(canvas, points, paint);
            var geometry = PreparedFlight(state.Source.ObjectId, prediction);
            if (geometry is null) continue;
            AlignFlightStart(geometry, state);
            DefenseGeometry[state.Source.ObjectId] = geometry;
            DrawCombatGeometry(canvas, geometry, CombatSettings.CountermeasurePrediction, CombatSettings.CountermeasureIntercept, flight.TargetTorpedoId);

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
    internal SKColor CountermeasureTrailColor(double fraction) =>
        CombatSettings.CountermeasureTrail.WithAlpha((byte)Math.Floor(
            CombatSettings.CountermeasureTrail.Alpha * (.25 + .75 * Math.Clamp(fraction, 0, 1)) + .5));

    private void DrawCountermeasureTrail(SKCanvas canvas, IReadOnlyList<FutureTrajectoryPoint> points, SKPaint paint)
    {
        double total = 0;
        for (int i = 1; i < points.Count; i++) total += Distance(points[i - 1], points[i]);
        double travelled = 0;
        for (int i = 1; i < points.Count; i++)
        {
            double next = travelled + Distance(points[i - 1], points[i]);
            var a = _camera.WorldToScreen(points[i - 1].X, points[i - 1].Y, _viewportW, _viewportH);
            var b = _camera.WorldToScreen(points[i].X, points[i].Y, _viewportW, _viewportH);
            using var shader = SKShader.CreateLinearGradient(new SKPoint(a.X, a.Y), new SKPoint(b.X, b.Y),
                [CountermeasureTrailColor(total > 0 ? travelled / total : 0),
                 CountermeasureTrailColor(total > 0 ? next / total : 1)], null, SKShaderTileMode.Clamp);
            paint.Color = SKColors.White;
            paint.Shader = shader;
            canvas.DrawLine(a.X, a.Y, b.X, b.Y, paint);
            paint.Shader = null;
            travelled = next;
        }

        static double Distance(FutureTrajectoryPoint a, FutureTrajectoryPoint b) =>
            Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    }

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
        using var text = new SKPaint { Color = CombatSettings.DefenseText, TextSize = 12f, IsAntialias = true };
        using var line = new SKPaint { Color = CombatSettings.DefenseRange, Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
        DrawHoveredWeaponRanges(canvas, line);
        foreach (var state in _renderStates)
        {
            if (state.Source.RenderObjectType == SpaceObjectType.NpcShip && state.Source.Defense is { } defense &&
                _labelRenderer.Geometries.TryGetValue(state.Source.ObjectId, out var label))
                canvas.DrawText(DefenseStatusText(defense, DefensePresentationTime), label.PlaqueRect.Left, label.PlaqueRect.Bottom + 22, text);
            if (state.Source.Countermeasure is { Phase: CountermeasurePhase.Guiding } flight &&
                DefenseGeometry.TryGetValue(state.Source.ObjectId, out var geometry) && geometry.Intercept is { } encounter)
            {
                var marker = _camera.WorldToScreen(encounter.X, encounter.Y, _viewportW, _viewportH);
                if (marker.X < 0 || marker.Y < 0 || marker.X > _viewportW || marker.Y > _viewportH) continue;
                var projectile = _camera.WorldToScreen(state.Pose.X, state.Pose.Y, _viewportW, _viewportH);
                string chance = $"{flight.FrozenChanceTenths / 10m:0.0}%";
                canvas.DrawText(chance, marker.X + 8, marker.Y - 8, text);
                DefenseChanceLabels[state.Source.ObjectId] = (new(projectile.X, projectile.Y), new(marker.X, marker.Y), chance);
            }
        }
    }

    private void DrawHoveredWeaponRanges(SKCanvas canvas, SKPaint paint)
    {
        if (FindPlayerShip(_renderStates) is not { } player || player.Source.IsDestroyed) return;
        var (x, y) = _camera.WorldToScreen(player.Pose.X, player.Pose.Y, _viewportW, _viewportH);
        string? hoveredModule = null;
        if (_hasMousePosition && !(_panelVisible && _lastPanelRect.Contains(_uiMouseX, _uiMouseY)) &&
            _commandsPanel.PanelAt(_uiMouseX, _uiMouseY) is { } panel)
            hoveredModule = panel.Weapon?.ModuleId ?? (panel.Name == "Torpedo Launcher" ? FindLauncher()?.ModuleId : null);
        var modules = _buffer.Presented?.Snapshot.InstalledModules ?? default;
        if (modules.IsDefault) return;
        static bool IsPointWeapon(InstalledModuleSnapshot module) =>
            module.LauncherCombat?.Profiles is { IsDefaultOrEmpty: false } || module.Defense?.Profiles is { IsDefaultOrEmpty: false };
        _commandsPanel.InitializePointWeaponRanges(_buffer.PresentationSessionId, modules.Where(IsPointWeapon).Select(m => m.ModuleId));
        foreach (var module in modules)
        {
            if (IsPointWeapon(module))
            {
                if (!_commandsPanel.IsWeaponRangeVisible(module.ModuleId)) continue;
                if (module.LauncherCombat?.RangeKm is { } pointRange) Ring(module.ModuleId, "torpedo", pointRange, false, true);
                else if (module.Defense is { } pointDefense) Ring(module.ModuleId, "counter", pointDefense.RangeKm, false, true);
                continue;
            }
            if (module.ModuleId != hoveredModule && !_commandsPanel.IsWeaponRangeVisible(module.ModuleId)) continue;
            if (module.Defense is { } defense)
            {
                Ring(module.ModuleId, "auto", defense.RangeKm, false);
                if (defense.ManualRangeKm is { } manual) Ring(module.ModuleId, "manual", manual, true);
            }
            else if (module.LauncherCombat?.RangeKm is { } range) Ring(module.ModuleId, "torpedo", range, false);
        }

        void Ring(string module, string kind, double range, bool dashed, bool point = false)
        {
            double pixels = range * 10 * _camera.PixelsPerWorldUnit;
            if (!double.IsFinite(pixels) || pixels <= 0 || pixels > float.MaxValue) return;
            using var effect = dashed ? SKPathEffect.CreateDash([6, 4], 0) : null;
            paint.Color = CombatSettings.RangeColor(kind);
            paint.StrokeWidth = point ? (float)CombatSettings.PointCombat.LineWidthPx : 1;
            paint.PathEffect = effect;
            canvas.DrawCircle(x, y, (float)pixels, paint);
            paint.PathEffect = null;
            WeaponRangeRings.Add(new(module, kind, range, new(x, y), (float)pixels));
        }
    }

    private void DrawCountermeasureResults(SKCanvas canvas, BufferedSnapshot? buffered)
    {
        _combatEffects.ReceiveJournal(buffered?.Snapshot.CombatJournal ?? default, buffered?.ReceivedAtTimestamp ?? _timestampProvider());
    }
}
