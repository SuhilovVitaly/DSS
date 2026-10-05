using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>Samples confirmed analytic geometry for display only; never changes flight state.</summary>
internal static class CombatTrajectoryProjector
{
    internal sealed record Geometry(
        List<FutureTrajectoryPoint> Prediction,
        List<FutureTrajectoryPoint> Target,
        FutureTrajectoryPoint? Intercept);

    internal static Geometry Project(TorpedoSnapshot flight, ObjectMotionSnapshot? target,
        IMotionPredictor predictor, CameraState camera, int width, int height)
    {
        var prediction = new List<FutureTrajectoryPoint>();
        var targetPath = new List<FutureTrajectoryPoint>();
        var route = flight.Route;
        if (route.Segments.IsDefaultOrEmpty) return new(prediction, targetPath, null);
        double duration = route.Segments.Sum(s => s.DurationMs);
        double elapsed = route.ElapsedMs;
        bool intercept = route.HasIntercept && duration >= elapsed && target is not null;
        SampleRoute(route, elapsed, Math.Max(elapsed, duration), camera, prediction);
        if (!intercept)
        {
            // No lifetime/physical range is invented. Extend the last confirmed tangent
            // only to the viewport; the Engine will continue to guide the actual projectile.
            var end = TorpedoGuidanceMath.PredictPose(route, Math.Max(elapsed, duration));
            TrajectoryViewportGeometry.ExtendToEdge(prediction, end.Direction, camera, width, height);
            return new(prediction, targetPath, null);
        }

        double remaining = duration - elapsed;
        var encounter = prediction[^1];
        var targetEnd = PredictTarget(target!, remaining, predictor);
        // A manoeuvring target can invalidate the last confirmed linear interception.
        // Do not advertise a meeting that shared motion no longer predicts.
        if (Math.Abs(targetEnd.X - encounter.X) + Math.Abs(targetEnd.Y - encounter.Y) > 1e-3)
            return new(prediction, targetPath, null);
        if (target!.RenderObjectType != SpaceObjectType.UnknownSpaceObject)
        {
            int samples = LinearMotionPredictor.IsLinear(target) ? 1 : 256;
            for (int i = 0; i <= samples; i++) targetPath.Add(PredictTarget(target, remaining * i / samples, predictor));
        }
        return new(prediction, targetPath, encounter);
    }

    private static FutureTrajectoryPoint PredictTarget(ObjectMotionSnapshot target, double ms, IMotionPredictor predictor)
    {
        // The shared predictor accepts integer milliseconds; interpolate the sub-ms endpoint.
        long whole = (long)Math.Min(long.MaxValue - 1d, Math.Floor(ms));
        var a = predictor.Predict(target, whole);
        double fraction = ms - whole;
        if (fraction <= 0) return new(a.X, a.Y);
        var b = predictor.Predict(target, whole + 1);
        return new(a.X + (b.X - a.X) * fraction, a.Y + (b.Y - a.Y) * fraction);
    }

    private static void SampleRoute(TorpedoRoute route, double from, double to, CameraState camera, List<FutureTrajectoryPoint> points)
    {
        if (route.Segments.IsDefaultOrEmpty || to < from) return;
        double cursor = 0;
        foreach (var segment in route.Segments)
        {
            double start = Math.Max(from, cursor), end = Math.Min(to, cursor + segment.DurationMs);
            if (end >= start) SampleSegment(segment, start - cursor, end - cursor, camera, points);
            cursor += segment.DurationMs;
        }
        if (to > cursor || points.Count == 0)
        {
            double start = Math.Max(from, cursor);
            var pose = TorpedoGuidanceMath.PredictPose(route, start);
            SampleSegment(new(pose.X, pose.Y, pose.Direction, route.Segments[^1].SpeedKmS, 0, Math.Max(0, to - start)),
                0, Math.Max(0, to - start), camera, points);
        }
    }

    private static void SampleSegment(TorpedoRouteSegment segment, double from, double to,
        CameraState camera, List<FutureTrajectoryPoint> points)
    {
        int count = 1;
        if (segment.AngularVelocityDegPerSec != 0)
        {
            double omega = Math.Abs(segment.AngularVelocityDegPerSec) * Math.PI / 180;
            double radiusPx = segment.SpeedKmS * 10 / omega * camera.PixelsPerWorldUnit;
            double step = 2 * Math.Acos(Math.Clamp(1 - .5 / Math.Max(.5, radiusPx), -1, 1));
            double angle = omega * (to - from) / 1000;
            count = (int)Math.Clamp(Math.Ceiling(angle / Math.Max(.001, Math.Min(.1, step))), 1, 4096);
        }
        for (int i = 0; i <= count; i++)
        {
            var p = TorpedoGuidanceMath.PredictSegment(segment, from + (to - from) * i / count);
            var point = new FutureTrajectoryPoint(p.X, p.Y);
            if (points.Count == 0 || points[^1] != point) points.Add(point);
        }
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
