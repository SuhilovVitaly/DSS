namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>Analytic continuation of straight motion; cost is independent of zoom and flight time.</summary>
internal static class TrajectoryViewportGeometry
{
    internal static void ExtendToEdge(List<FutureTrajectoryPoint> points, double direction,
        CameraState camera, int width, int height)
    {
        if (points.Count == 0 || width <= 0 || height <= 0 || !double.IsFinite(direction)) return;
        var origin = points[^1];
        double angle = direction * Math.PI / 180;
        double dx = Math.Sin(angle), dy = -Math.Cos(angle);
        var min = camera.ScreenToWorld(0, 0, width, height);
        var max = camera.ScreenToWorld(width, height, width, height);
        double enter = 0, exit = double.PositiveInfinity;
        if (!ClipAxis(origin.X, dx, min.X, max.X, ref enter, ref exit) ||
            !ClipAxis(origin.Y, dy, min.Y, max.Y, ref enter, ref exit) ||
            !double.IsFinite(exit) || exit <= 0) return;
        points.Add(new(origin.X + exit * dx, origin.Y + exit * dy));
    }

    private static bool ClipAxis(double origin, double direction, double min, double max, ref double enter, ref double exit)
    {
        if (Math.Abs(direction) < 1e-12) return origin >= min && origin <= max;
        double a = (min - origin) / direction, b = (max - origin) / direction;
        enter = Math.Max(enter, Math.Min(a, b));
        exit = Math.Min(exit, Math.Max(a, b));
        return exit >= enter;
    }
}
