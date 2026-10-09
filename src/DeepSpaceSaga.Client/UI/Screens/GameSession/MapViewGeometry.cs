using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal struct MapWorldBounds
{
    internal bool HasValue;
    internal double MinX, MinY, MaxX, MaxY;
    internal void Include(double x, double y)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y)) return;
        if (!HasValue) { MinX = MaxX = x; MinY = MaxY = y; HasValue = true; }
        else { MinX = Math.Min(MinX, x); MinY = Math.Min(MinY, y); MaxX = Math.Max(MaxX, x); MaxY = Math.Max(MaxY, y); }
    }
}

internal static class MapViewGeometry
{
    internal static SKRect FreeViewport(int width, int height, IReadOnlyList<SKRect> obstacles) =>
        FreeViewport(width, height, obstacles, out _, out _);

    internal static SKRect FreeViewport(int width, int height, IReadOnlyList<SKRect> obstacles,
        out int candidates, out int obstacleChecks)
    {
        candidates = obstacleChecks = 0;
        if (width <= 0 || height <= 0) return SKRect.Empty;
        var clipped = new List<SKRect>();
        var xs = new SortedSet<float> { 0, width };
        foreach (var obstacle in obstacles)
        {
            if (!float.IsFinite(obstacle.Left) || !float.IsFinite(obstacle.Top) ||
                !float.IsFinite(obstacle.Right) || !float.IsFinite(obstacle.Bottom)) continue;
            var r = new SKRect(Math.Clamp(obstacle.Left, 0, width), Math.Clamp(obstacle.Top, 0, height),
                Math.Clamp(obstacle.Right, 0, width), Math.Clamp(obstacle.Bottom, 0, height));
            if (r.Width <= 0 || r.Height <= 0) continue;
            clipped.Add(r); xs.Add(r.Left); xs.Add(r.Right);
        }
        clipped.Sort((a, b) => a.Top != b.Top ? a.Top.CompareTo(b.Top) : a.Bottom.CompareTo(b.Bottom));
        var edges = xs.ToArray();
        SKRect best = SKRect.Empty;
        double bestArea = 0;
        int considered = 0;
        void Consider(float left, float top, float right, float bottom)
        {
            if (bottom <= top) return;
            considered++;
            var r = new SKRect(left, top, right, bottom);
            double area = (double)r.Width * r.Height;
            if (area > bestArea || area == bestArea && Before(r, best)) { best = r; bestArea = area; }
        }
        for (int l = 0; l < edges.Length - 1; l++)
            for (int r = l + 1; r < edges.Length; r++)
            {
                float cursor = 0;
                foreach (var obstacle in clipped)
                {
                    obstacleChecks++;
                    if (obstacle.Right <= edges[l] || obstacle.Left >= edges[r]) continue;
                    Consider(edges[l], cursor, edges[r], obstacle.Top);
                    cursor = Math.Max(cursor, obstacle.Bottom);
                    if (cursor >= height) break;
                }
                Consider(edges[l], cursor, edges[r], height);
            }
        candidates = considered;
        return best;
    }

    private static bool Before(SKRect a, SKRect b) => a.Top < b.Top || a.Top == b.Top &&
        (a.Left < b.Left || a.Left == b.Left && (a.Bottom < b.Bottom || a.Bottom == b.Bottom && a.Right < b.Right));
    internal static void Fit(CameraState camera, MapWorldBounds bounds, SKRect available, int width, int height, TacticalMapSettings settings)
    {
        if (!bounds.HasValue || width <= 0 || height <= 0 || available.IsEmpty) return;
        float padding = (float)Math.Min(settings.FitPaddingPixels, Math.Min(available.Width, available.Height) / 4);
        available.Inflate(-padding, -padding);
        double ppu = Math.Min(Math.Max(1, available.Width) / Math.Max(1, bounds.MaxX - bounds.MinX),
            Math.Max(1, available.Height) / Math.Max(1, bounds.MaxY - bounds.MinY));
        ppu = Math.Clamp(ppu, settings.MinimumPpu, settings.MaximumPpu);
        camera.SetZoom(ppu);
        camera.SetFocus(bounds.MinX + (bounds.MaxX - bounds.MinX) / 2 - (available.MidX - width / 2.0) / ppu,
            bounds.MinY + (bounds.MaxY - bounds.MinY) / 2 - (available.MidY - height / 2.0) / ppu);
    }
}
