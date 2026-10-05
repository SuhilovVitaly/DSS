using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal sealed class SolarSystemLayerRenderer
{
    internal void Draw(SKCanvas canvas, SolarSystemMapSnapshot map, CameraState camera, SKRect viewport, bool showOrbits = true)
    {
        PrepareMap(map);
        var (cx, cy) = camera.WorldToScreen(0, 0, (int)viewport.Width, (int)viewport.Height);
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        canvas.Save();
        canvas.ClipRect(viewport);
        paint.Color = new SKColor(145, 160, 170, 90);
        foreach (var belt in map.Belts)
        {
            Ring(belt.InnerRadius);
            Ring(belt.OuterRadius);
            DrawDecoration(canvas, belt, camera, viewport);
        }
        paint.Color = new SKColor(105, 135, 160, 45);
        foreach (var orbit in showOrbits ? map.Orbits : [])
        {
            float a = (float)(orbit.Elements.SemiMajorAxis * camera.PixelsPerWorldUnit);
            float b = (float)(orbit.Elements.SemiMinorAxis * camera.PixelsPerWorldUnit);
            if (float.IsFinite(a) && float.IsFinite(b)) canvas.DrawOval(cx, cy, a, b, paint);
        }
        paint.Color = new SKColor(255, 210, 100);
        paint.StrokeWidth = 2;
        if (viewport.Contains(cx, cy)) canvas.DrawCircle(cx, cy, 8, paint);
        canvas.Restore();

        void Ring(double radius)
        {
            float pixels = (float)(radius * camera.PixelsPerWorldUnit);
            if (float.IsFinite(pixels)) canvas.DrawCircle(cx, cy, pixels, paint);
        }
    }

    private readonly Dictionary<(BeltMapData Belt, int Lod), (double X, double Y)[]> _decoration = new();
    private SolarSystemMapSnapshot? _map;
    internal int CachedPatterns => _decoration.Count;

    private void PrepareMap(SolarSystemMapSnapshot map)
    {
        if (ReferenceEquals(map, _map)) return;
        foreach (var key in _decoration.Keys.Where(k => !map.Belts.Contains(k.Belt)).ToArray()) _decoration.Remove(key);
        _map = map;
    }

    private void DrawDecoration(SKCanvas canvas, BeltMapData belt, CameraState camera, SKRect viewport)
    {
        int lod = (belt.OuterRadius - belt.InnerRadius) * camera.PixelsPerWorldUnit < 12 ? 0 : 1;
        var samples = Decoration(belt, lod);
        using var paint = new SKPaint { IsAntialias = false, Color = new SKColor(150, 160, 165, 80), StrokeWidth = 1 };
        foreach (var (x, y) in samples)
        {
            var (sx, sy) = camera.WorldToScreen(x, y, (int)viewport.Width, (int)viewport.Height);
            if (viewport.Contains(sx, sy)) canvas.DrawPoint(sx, sy, paint);
        }
    }

    internal (double X, double Y)[] Decoration(BeltMapData belt, int lod)
    {
        if (_decoration.TryGetValue((belt, lod), out var cached)) return cached;
        // A bounded visual sample budget cannot allocate arbitrary entity-sized worlds.
        int count = Math.Min(Math.Max(0, belt.DecorationSamples), 65536) / (lod == 0 ? 4 : 1);
        var points = new (double X, double Y)[count];
        ulong state = belt.DecorationSeed;
        double Next()
        {
            unchecked
            {
                ulong z = (state += 0x9E3779B97F4A7C15UL);
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return ((z ^ (z >> 31)) >> 11) * (1.0 / 9007199254740992.0);
            }
        }
        double rotation = Next() * Math.Tau;
        for (int i = 0; i < count; i++)
        {
            double angle = rotation + (i % 5) * Math.Tau / 5 + Next() * 0.7;
            double radius = belt.InnerRadius + (belt.OuterRadius - belt.InnerRadius) * Next();
            points[i] = (radius * Math.Sin(angle), -radius * Math.Cos(angle));
        }
        _decoration[(belt, lod)] = points;
        return points;
    }

    internal static void DrawPlanet(SKCanvas canvas, PlanetMapData planet, double x, double y,
        CameraState camera, int width, int height)
    {
        var (sx, sy) = camera.WorldToScreen(x, y, width, height);
        float radius = (float)Math.Clamp(planet.VisualRadius * camera.PixelsPerWorldUnit, 16, 24);
        if (sx < -radius || sy < -radius || sx > width + radius || sy > height + radius) return;
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2,
            Color = planet.Kind switch { "Icy" => new SKColor(140, 220, 245), "Gas" => new SKColor(225, 175, 120), _ => new SKColor(185, 180, 170) }
        };
        canvas.DrawCircle(sx, sy, radius, paint);
        if (planet.Kind == "Gas") canvas.DrawOval(sx, sy, radius * 1.5f, radius * 0.35f, paint);
        if (planet.Kind == "Icy") canvas.DrawLine(sx - radius, sy, sx + radius, sy, paint);
    }
}
