using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal static class SolarSystemLayerRenderer
{
    internal static void Draw(SKCanvas canvas, SolarSystemMapSnapshot map, CameraState camera, SKRect viewport)
    {
        var (cx, cy) = camera.WorldToScreen(0, 0, (int)viewport.Width, (int)viewport.Height);
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        canvas.Save();
        canvas.ClipRect(viewport);
        paint.Color = new SKColor(145, 160, 170, 90);
        foreach (var belt in map.Belts)
        {
            Ring(belt.InnerRadius);
            Ring(belt.OuterRadius);
        }
        paint.Color = new SKColor(105, 135, 160, 45);
        foreach (var orbit in map.Orbits)
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
