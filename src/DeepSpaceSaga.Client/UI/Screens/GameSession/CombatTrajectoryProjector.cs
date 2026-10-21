using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>Projects prepared conditional paths for display; never changes the confirmed scene.</summary>
internal static class CombatTrajectoryProjector
{
    internal static void BuildOwnPath(ObjectMotionSnapshot own, List<FutureTrajectoryPoint> points)
    {
        points.Clear();
        for (int time = 0; time <= 5000; time += 100)
        {
            var pose = CurrentManeuverForecast.Sample(own, time);
            points.Add(new(pose.X, pose.Y));
        }
    }

    internal sealed record Geometry(
        List<FutureTrajectoryPoint> Prediction,
        List<FutureTrajectoryPoint> Target,
        FutureTrajectoryPoint? Intercept)
    {
        internal CombatForecastStatus Status { get; init; }
        internal double EndMotionTimeMs { get; init; }
        internal double? EtaSeconds { get; init; }
        internal double HorizonSeconds { get; init; }
        internal FutureTrajectoryPoint? Detonation { get; init; }
        internal double? DetonationEtaSeconds { get; init; }
        internal double? BlastRadiusKm { get; init; }
        internal ProjectileKind? Kind { get; init; }
        internal bool IsObserved { get; init; }
    }

    internal static Geometry FromPointForecast(CombatForecastResult result, ObjectMotionSnapshot projectile,
        double now, Geometry? reuse = null)
    {
        var own = projectile.OwnProjectile;
        var observed = projectile.ObservedProjectile;
        if (own is null && observed is null) throw new ArgumentException("Expected a point projectile.", nameof(projectile));
        return FromForecast(result, now, reuse) with
        {
            Detonation = own is null ? null : new(own.DetonationX, own.DetonationY),
            DetonationEtaSeconds = own is null ? null : Math.Max(0, (own.DetonationMotionTimeMs - now) / 1000),
            BlastRadiusKm = own?.BlastRadiusKm ?? observed!.BlastRadiusKm,
            Kind = own?.Kind ?? observed!.Kind,
            IsObserved = own is null
        };
    }

    internal static Geometry FromForecast(CombatForecastResult result, double now, Geometry? reuse = null)
    {
        now = Math.Clamp(now, result.StartMotionTimeMs, result.EndMotionTimeMs);
        var prediction = reuse?.Prediction ?? new List<FutureTrajectoryPoint>();
        var target = reuse?.Target ?? new List<FutureTrajectoryPoint>();
        Slice(result.Projectile, now, prediction);
        Slice(result.Target, now, target);
        bool contact = result.Status == CombatForecastStatus.Contact;
        return new(prediction, target, contact && prediction.Count > 0 ? prediction[^1] : null)
        {
            Status = result.Status,
            EndMotionTimeMs = result.EndMotionTimeMs,
            EtaSeconds = contact ? Math.Max(0, (result.EndMotionTimeMs - now) / 1000) : null,
            HorizonSeconds = Math.Max(0, (result.EndMotionTimeMs - now) / 1000)
        };
    }

    private static void Slice(System.Collections.Immutable.ImmutableArray<CombatForecastPoint> path, double now, List<FutureTrajectoryPoint> points)
    {
        points.Clear();
        if (path.IsEmpty) return;
        int i = 0;
        while (i + 1 < path.Length && path[i + 1].MotionTimeMs <= now) i++;
        var a = path[i];
        if (i + 1 < path.Length && now > a.MotionTimeMs)
        {
            var b = path[i + 1];
            double f = (now - a.MotionTimeMs) / (b.MotionTimeMs - a.MotionTimeMs);
            points.Add(new(a.X + (b.X - a.X) * f, a.Y + (b.Y - a.Y) * f));
            i++;
        }
        for (; i < path.Length; i++) points.Add(new(path[i].X, path[i].Y));
    }

    /// <summary>Clip in double precision before converting to Skia's float coordinates.</summary>
    internal static void BuildPath(SKPath path, IReadOnlyList<FutureTrajectoryPoint> points,
        CameraState camera, int width, int height)
    {
        path.Reset();
        for (int i = 1; i < points.Count; i++)
        {
            double x = width / 2d + (points[i - 1].X - camera.FocusX) * camera.PixelsPerWorldUnit;
            double y = height / 2d + (points[i - 1].Y - camera.FocusY) * camera.PixelsPerWorldUnit;
            double dx = (points[i].X - points[i - 1].X) * camera.PixelsPerWorldUnit;
            double dy = (points[i].Y - points[i - 1].Y) * camera.PixelsPerWorldUnit;
            double enter = 0, exit = 1;
            if (!Clip(-dx, x + 2, ref enter, ref exit) || !Clip(dx, width + 2 - x, ref enter, ref exit) ||
                !Clip(-dy, y + 2, ref enter, ref exit) || !Clip(dy, height + 2 - y, ref enter, ref exit)) continue;
            var a = new SKPoint((float)(x + enter * dx), (float)(y + enter * dy));
            var b = new SKPoint((float)(x + exit * dx), (float)(y + exit * dy));
            if (!float.IsFinite(a.X) || !float.IsFinite(a.Y) || !float.IsFinite(b.X) || !float.IsFinite(b.Y)) continue;
            if (path.IsEmpty || path.LastPoint != a) path.MoveTo(a);
            path.LineTo(b);
        }
    }

    private static bool Clip(double p, double q, ref double enter, ref double exit)
    {
        if (p == 0) return q >= 0;
        double t = q / p;
        if (p < 0) enter = Math.Max(enter, t);
        else exit = Math.Min(exit, t);
        return enter <= exit;
    }
}
