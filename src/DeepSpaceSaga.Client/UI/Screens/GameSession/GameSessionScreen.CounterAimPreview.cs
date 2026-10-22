using System.Globalization;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    internal sealed record CounterAimPreview(double StartX, double StartY, double X, double Y, double RadiusKm,
        double PhysicalSeconds, double RealSeconds, string? SnappedObjectId, double? ArrivalRealSeconds,
        double? SpareRealSeconds, bool Late, bool Unreachable, int SalvoCount, bool OwnTorpedoWarning,
        BlastThreatResult? Threat, IReadOnlyList<string> InterceptionsEnroute);
    internal CounterAimPreview? CurrentCounterAimPreview { get; private set; }
    internal SKRect CounterAimLabelBounds { get; private set; }

    private CounterAimPreview? BuildCounterAimPreview(double x, double y)
    {
        if (PointAim is not { Kind: ProjectileKind.Countermissile } aim || PointAimModule(aim.ModuleId) is not { } module ||
            FindPlayerShip(_renderStates) is not { } player ||
            PointProfiles(module).FirstOrDefault(p => p.ProfileId == aim.ProfileId) is not { } profile) return null;
        double multiplier = _buffer.LatestPrediction?.CurrentSpeed.MotionTimeMultiplier() ?? .25;
        return ProjectCounterAim(player.Pose.ToSnapshot(), _renderStates.Where(s => !s.Source.IsDestroyed &&
            (s.Source.OwnProjectile is not null || s.Source.ObservedProjectile is not null)).Select(s => s.Pose.ToSnapshot()).ToArray(),
            profile, module.Defense!.RangeKm, x, y, _camera.PixelsPerWorldUnit, _pointAltLeft || _pointAltRight,
            PointAimTime, multiplier > 0 ? multiplier : .25);
    }

    internal static CounterAimPreview ProjectCounterAim(ObjectMotionSnapshot player, IReadOnlyList<ObjectMotionSnapshot> displayed,
        LaunchProfileSnapshot profile, double rangeKm, double x, double y, double ppu, bool alt, double nowMotionMs, double multiplier)
    {
        var foreign = displayed.Where(o => o.ObservedProjectile is not null && !o.IsDestroyed).OrderBy(o => o.ObjectId, StringComparer.Ordinal).ToArray();
        ObjectMotionSnapshot? snapped = null;
        double closestPx = 12;
        if (!alt)
            foreach (var row in foreign)
            {
                if (row.ObservedProjectile is not { Kind: ProjectileKind.Torpedo } observed) continue;
                var direction = Unit(observed);
                double along = Math.Max(0, (x - row.X) * direction.X + (y - row.Y) * direction.Y);
                double sx = row.X + along * direction.X, sy = row.Y + along * direction.Y;
                double distancePx = double.Hypot(x - sx, y - sy) * ppu;
                if (distancePx > closestPx || snapped is not null && distancePx == closestPx) continue;
                snapped = row; closestPx = distancePx;
            }
        // Re-project once after selection: candidates must all be compared to the original cursor.
        if (snapped is not null)
        {
            var direction = Unit(snapped.ObservedProjectile!);
            double along = Math.Max(0, (x - snapped.X) * direction.X + (y - snapped.Y) * direction.Y);
            x = snapped.X + direction.X * along; y = snapped.Y + direction.Y * along;
        }
        double distanceKm = double.Hypot(x - player.X, y - player.Y) / 10;
        double seconds = distanceKm / profile.SpeedKmS;
        double playerRadians = player.Direction * Math.PI / 180;
        var threats = foreign.Where(o => o.ObservedProjectile!.Kind == ProjectileKind.Torpedo).Select(o =>
        {
            var observed = o.ObservedProjectile!;
            var threat = BlastThreatMath.Evaluate(player.X, player.Y, player.SpeedKmS * Math.Sin(playerRadians), -player.SpeedKmS * Math.Cos(playerRadians),
                o.X, o.Y, observed.DirectionX, observed.DirectionY, observed.SpeedKmS, observed.BlastRadiusKm);
            return (Row: o, Threat: threat);
        }).ToArray();
        var selected = snapped is not null ? threats.First(t => t.Row.ObjectId == snapped.ObjectId) :
            threats.Where(t => t.Threat.IsThreat).OrderBy(t => t.Threat.EntryAfterSeconds).ThenBy(t => t.Row.ObjectId, StringComparer.Ordinal).FirstOrDefault();
        double? arrival = null;
        if (snapped?.ObservedProjectile is { SpeedKmS: > 0 } target)
            arrival = double.Hypot(x - snapped.X, y - snapped.Y) / (target.SpeedKmS * 10);
        double? spare = selected.Threat?.EntryAfterSeconds is { } deadline ? deadline - seconds : null;
        bool late = spare < 0 || snapped?.ObservedProjectile is { SpeedKmS: > 0 } moving &&
            arrival is { } arrives && seconds > arrives + profile.BlastRadiusKm / moving.SpeedKmS;
        int count = 0;
        foreach (var row in foreign)
        {
            var point = ObservedAt(row, seconds);
            if (double.Hypot(point.X - x, point.Y - y) / 10 <= profile.BlastRadiusKm) count++;
        }
        var ownRows = displayed.Where(o => !o.IsDestroyed && o.ObservedProjectile is null && o.OwnProjectile?.OwnerObjectId == player.ObjectId).ToArray();
        bool ownWarning = ownRows.Any(o => o.OwnProjectile is { Kind: ProjectileKind.Torpedo } own &&
            own.DetonationMotionTimeMs >= nowMotionMs + seconds * 1000 && OwnAt(o, seconds, nowMotionMs) is var position &&
            double.Hypot(position.X - x, position.Y - y) / 10 <= profile.BlastRadiusKm);
        var enroute = PredictPointInterceptions(displayed, player.ObjectId, nowMotionMs);
        bool missedSnapped = snapped is not null && ObservedAt(snapped, seconds) is var snappedAt &&
            double.Hypot(snappedAt.X - x, snappedAt.Y - y) / 10 > profile.BlastRadiusKm;
        bool unreachable = !double.IsFinite(seconds) || distanceKm > rangeKm || !late && missedSnapped;
        return new(player.X, player.Y, x, y, profile.BlastRadiusKm, seconds, seconds / multiplier, snapped?.ObjectId,
            arrival / multiplier, spare / multiplier, late, unreachable, count, ownWarning, selected.Threat, enroute);
    }

    internal static IReadOnlyList<string> PredictPointInterceptions(IReadOnlyList<ObjectMotionSnapshot> displayed, string playerId, double nowMotionMs)
    {
        var ownFlights = displayed.Where(o => !o.IsDestroyed && o.ObservedProjectile is null && o.OwnProjectile is { Kind: ProjectileKind.Countermissile } own &&
            own.OwnerObjectId == playerId && own.DetonationMotionTimeMs >= nowMotionMs).ToArray();
        var result = new List<string>();
        foreach (var row in displayed.Where(o => !o.IsDestroyed && o.ObservedProjectile is { Kind: ProjectileKind.Torpedo }).OrderBy(o => o.ObjectId, StringComparer.Ordinal))
            foreach (var ownRow in ownFlights)
            {
                var own = ownRow.OwnProjectile!;
                var future = ObservedAt(row, (own.DetonationMotionTimeMs - nowMotionMs) / 1000);
                if (double.Hypot(future.X - own.DetonationX, future.Y - own.DetonationY) / 10 > own.BlastRadiusKm) continue;
                result.Add(row.ObjectId); break;
            }
        return result;
    }

    private static (double X, double Y) Unit(ObservedProjectileSnapshot observed)
    {
        double length = double.Hypot(observed.DirectionX, observed.DirectionY);
        return length > 0 ? (observed.DirectionX / length, observed.DirectionY / length) : (0, 0);
    }
    private static (double X, double Y) ObservedAt(ObjectMotionSnapshot row, double seconds)
    {
        var observed = row.ObservedProjectile!; var direction = Unit(observed);
        return (row.X + direction.X * observed.SpeedKmS * seconds * 10, row.Y + direction.Y * observed.SpeedKmS * seconds * 10);
    }
    private static (double X, double Y) OwnAt(ObjectMotionSnapshot row, double seconds, double nowMotionMs)
    {
        var own = row.OwnProjectile!;
        double span = own.DetonationMotionTimeMs - own.LaunchMotionTimeMs;
        double progress = span <= 0 ? 1 : (nowMotionMs + seconds * 1000 - own.LaunchMotionTimeMs) / span;
        return BlastMath.PositionAt(own.StartX, own.StartY, own.DetonationX, own.DetonationY, progress);
    }

    private void DrawCounterAimPreview(SKCanvas canvas)
    {
        CurrentCounterAimPreview = null; CounterAimLabelBounds = SKRect.Empty;
        if (!_hasMousePosition || IsClickOnUiPanel(_uiMouseX, _uiMouseY)) return;
        var target = _camera.ScreenToWorld(_mouseX, _mouseY, _viewportW, _viewportH);
        if (BuildCounterAimPreview(target.Item1, target.Item2) is not { } preview) return;
        CurrentCounterAimPreview = preview;
        var styles = CombatSettings.PointCombat;
        var start = _camera.WorldToScreen(preview.StartX, preview.StartY, _viewportW, _viewportH);
        var end = _camera.WorldToScreen(preview.X, preview.Y, _viewportW, _viewportH);
        using var line = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = (float)styles.LineWidthPx,
            Color = preview.Late || preview.Unreachable ? styles.Danger : styles.Safe
        };
        canvas.DrawLine(start.X, start.Y, end.X, end.Y, line);
        double radius = preview.RadiusKm * 10 * _camera.PixelsPerWorldUnit;
        if (double.IsFinite(radius) && radius < float.MaxValue) DrawPointBlast(canvas, new(end.X, end.Y), radius, 0, ProjectileKind.Countermissile, .7, line);
        canvas.DrawCircle(end.X, end.Y, (float)styles.ReticleRadiusPx, line);
        if (preview.Threat is { DangerStartX: { } ax, DangerStartY: { } ay })
        {
            var a = _camera.WorldToScreen(ax, ay, _viewportW, _viewportH);
            line.Color = styles.Danger; line.StrokeWidth = (float)styles.LineWidthPx * 2;
            if (preview.Threat is { DangerEndX: { } bx, DangerEndY: { } by })
            { var b = _camera.WorldToScreen(bx, by, _viewportW, _viewportH); canvas.DrawLine(a.X, a.Y, b.X, b.Y, line); }
            canvas.DrawCircle(a.X, a.Y, (float)styles.ReticleRadiusPx, line);
        }
        using var text = new SKPaint { IsAntialias = true, TextSize = (float)styles.TextSizePx, Color = styles.ForeignProfileText };
        string Number(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);
        string label = $"{CombatText("ETA", "")} {Number(preview.RealSeconds)} {CombatText("Seconds", "s")} · ×{preview.SalvoCount}";
        if (preview.ArrivalRealSeconds is { } arrival) label += " · " + CombatText("Aim.ArrivesIn", "Arrives in") + $" {Number(arrival)} {CombatText("Seconds", "s")}";
        if (preview.SpareRealSeconds is { } spare) label += " · " + CombatText("Aim.Spare", "Spare") + $" {Number(spare)} {CombatText("Seconds", "s")}";
        if (preview.Late) label += " · " + CombatText("Aim.Late", "Too late");
        if (preview.Unreachable) label += " · " + CombatText("Aim.Unreachable", "Unreachable");
        if (preview.OwnTorpedoWarning) label += " · " + CombatText("Aim.OwnTorpedo", "Will hit own torpedo");
        CounterAimLabelBounds = DrawPointAimLabel(canvas, label, new(end.X, end.Y), text);
        foreach (string id in preview.InterceptionsEnroute)
        {
            var row = _renderStates.First(s => s.Source.ObjectId == id);
            var position = _camera.WorldToScreen(row.Pose.X, row.Pose.Y, _viewportW, _viewportH);
            DrawPointAimLabel(canvas, "◇ " + CombatText("Aim.InterceptionEnroute", "Interception en route (prediction)"), new(position.X, position.Y), text);
        }
    }
}
