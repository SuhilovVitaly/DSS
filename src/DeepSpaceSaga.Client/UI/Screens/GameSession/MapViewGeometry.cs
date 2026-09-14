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
    internal static SKRect FreeViewport(int width, int height, IReadOnlyList<SKRect> obstacles)
    {
        var xs = new List<float> { 0, width };
        var ys = new List<float> { 0, height };
        foreach (var r in obstacles)
        {
            xs.Add(Math.Clamp(r.Left, 0, width)); xs.Add(Math.Clamp(r.Right, 0, width));
            ys.Add(Math.Clamp(r.Top, 0, height)); ys.Add(Math.Clamp(r.Bottom, 0, height));
        }
        xs.Sort(); ys.Sort();
        SKRect best = new(0, 0, width, height);
        float area = 0;
        for (int l = 0; l < xs.Count - 1; l++)
        for (int r = l + 1; r < xs.Count; r++)
        for (int t = 0; t < ys.Count - 1; t++)
        for (int b = t + 1; b < ys.Count; b++)
        {
            var candidate = new SKRect(xs[l], ys[t], xs[r], ys[b]);
            float a = candidate.Width * candidate.Height;
            if (a <= area || obstacles.Any(o => candidate.IntersectsWith(o))) continue;
            best = candidate; area = a;
        }
        return best;
    }

    internal static void Fit(CameraState camera, MapWorldBounds bounds, SKRect available, int width, int height, TacticalMapSettings settings)
    {
        if (!bounds.HasValue || width <= 0 || height <= 0) return;
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
