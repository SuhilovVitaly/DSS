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

    /// <summary>Bounded manual flight: the marker is the first 500 m contact, not a later
    /// centre rendezvous. Unknown solutions show only the confirmed short pursuit segment.</summary>
    internal static Geometry ProjectManual(CountermeasurePlan plan, TorpedoRoute target, long now,
        long lifetime, CameraState camera)
    {
        var points = new List<FutureTrajectoryPoint>();
        var targetPoints = new List<FutureTrajectoryPoint>();
        double expires = now + (double)lifetime;
        double? encounter = plan.PredictedEncounterMotionTimeMs is { } contact && contact >= now && contact <= expires
            ? contact : null;
        double end = encounter ?? Math.Min(expires, plan.Route.StartMotionTimeMs + plan.Route.Segments.Sum(s => s.DurationMs));
        SampleRoute(plan.Route, Math.Max(0, now - plan.Route.StartMotionTimeMs), end - plan.Route.StartMotionTimeMs, camera, points);
        if (encounter is null || points.Count == 0) return new(points, targetPoints, null);
        SampleRoute(target, now - target.StartMotionTimeMs, encounter.Value - target.StartMotionTimeMs, camera, targetPoints);
        return new(points, targetPoints, points[^1]);
    }

    internal static Geometry Project(TorpedoSnapshot flight, ObjectMotionSnapshot? target,
        IMotionPredictor predictor, CameraState camera, int width, int height)
    {
        var prediction = new List<FutureTrajectoryPoint>();
        var targetPath = new List<FutureTrajectoryPoint>();
        var route = flight.Route;
        if (route.Segments.IsDefaultOrEmpty) return new(prediction, targetPath, null);
        double duration = route.Segments.Sum(s => s.DurationMs);
        double elapsed = route.ElapsedMs;
        double remaining = duration - elapsed;
        bool intercept = route.HasIntercept && remaining >= 0 && target is { IsDestroyed: false };
        // An AI segment is only confirmed until its next decision boundary. The
        // predictor clamps there; a coincident frozen endpoint is not an intercept.
        if (target?.HostileCombat is { } hostile)
            intercept &= remaining <= hostile.Segment.DurationMs - hostile.Segment.ElapsedMs;
        SampleRoute(route, elapsed, Math.Max(elapsed, duration), camera, prediction);
        var encounter = prediction[^1];
        if (intercept)
        {
            var targetEnd = PredictTarget(target!, remaining, predictor);
            // A manoeuvre can invalidate the confirmed constant-velocity aim point.
            intercept = Math.Abs(targetEnd.X - encounter.X) + Math.Abs(targetEnd.Y - encounter.Y) <= 1e-3;
        }
        if (!intercept)
        {
            // All unconfirmed encounters use the same display: a continuation to the
            // viewport, without a target connector or hit marker. The rejected aim
            // point must not look like the end of the projectile's flight.
            var end = TorpedoGuidanceMath.PredictPose(route, Math.Max(elapsed, duration));
            TrajectoryViewportGeometry.ExtendToEdge(prediction, end.Direction, camera, width, height);
            return new(prediction, targetPath, null);
        }

        if (target!.RenderObjectType != SpaceObjectType.UnknownSpaceObject)
        {
            int samples = LinearMotionPredictor.IsLinear(target) ? 1 : 256;
            for (int i = 0; i <= samples; i++) targetPath.Add(PredictTarget(target, remaining * i / samples, predictor));
            // Collapse only the accepted numerical tolerance so both lines and the
            // marker share exactly one endpoint at every zoom level.
            targetPath[^1] = encounter;
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
