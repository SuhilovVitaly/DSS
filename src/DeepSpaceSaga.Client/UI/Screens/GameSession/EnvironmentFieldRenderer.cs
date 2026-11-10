using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal sealed class EnvironmentFieldRenderer
{
    internal const string Notice = "Информационное поле; игровые эффекты отсутствуют";
    internal sealed record Geometry(EnvironmentFieldData Data, double X, double Y)
    {
        internal bool Contains(double x, double y)
        {
            double dx = x - X, dy = y - Y, radius = double.Hypot(dx, dy);
            if (radius < Data.InnerRadius - 1e-6 || radius > Data.OuterRadius + 1e-6) return false;
            if (radius == 0 || Data.SweepDegrees == 360) return true;
            double angle = (Math.Atan2(dx, -dy) * 180 / Math.PI - Data.StartAngleDegrees + 720) % 360;
            return angle <= Data.SweepDegrees + 1e-6 || 360 - angle <= 1e-6;
        }
    }

    private readonly Dictionary<(string Id, ulong Seed, int Count), (double Radius, double Angle)[]> _patterns = new();
    internal int DecorationSamples { get; set; } = 64;
    internal int CachedPatternCount => _patterns.Count;

    internal static IReadOnlyList<Geometry> Resolve(AuthoritativeSnapshot snapshot, IEnumerable<ObjectMotionSnapshot> poses, long motionTimeMs)
    {
        if (snapshot.AiMap is not { } map || map.Fields.IsDefaultOrEmpty) return [];
        var objects = poses.ToDictionary(p => p.ObjectId, StringComparer.OrdinalIgnoreCase);
        var result = new List<Geometry>();
        foreach (var field in map.Fields.OrderBy(f => f.Id, StringComparer.Ordinal))
        {
            ObjectMotionSnapshot? anchor = null;
            if (field.AnchorKind == "Parent") objects.TryGetValue(field.ParentObjectId!, out anchor);
            else if (field.AnchorKind == "Orbit" && field.Orbit is { } orbit)
                anchor = OrbitalMotionMath.At(new(field.Id, 0, 0, 0, 0), orbit, motionTimeMs);
            else if (field.AnchorKind == "Sun")
                anchor = objects.Values.FirstOrDefault(p => p.ObjectType == "Sun" || p.RenderObjectType == "Sun");
            if (anchor is not null) result.Add(new(field, anchor.X + field.OffsetX, anchor.Y + field.OffsetY));
        }
        return result;
    }

    internal void Draw(SKCanvas canvas, IReadOnlyList<Geometry> fields, CameraState camera, int width, int height, string? selectedId)
    {
        int count = Math.Clamp(DecorationSamples, 1, 256);
        var keys = fields.Where(f => f.Data.Kind == "Debris").Select(f => (f.Data.Id, f.Data.DecorationSeed, count)).ToHashSet();
        foreach (var stale in _patterns.Keys.Where(k => !keys.Contains(k)).ToArray()) _patterns.Remove(stale);
        using var paint = new SKPaint { IsAntialias = true };
        foreach (var field in fields)
        {
            var data = field.Data;
            var (x, y) = camera.WorldToScreen(field.X, field.Y, width, height);
            double radius = data.OuterRadius * camera.PixelsPerWorldUnit;
            if (!float.IsFinite(x) || !float.IsFinite(y) || !double.IsFinite(radius) || radius <= 0 || radius > float.MaxValue ||
                x + radius < 0 || y + radius < 0 || x - radius > width || y - radius > height) continue;
            float outer = (float)radius, inner = (float)(data.InnerRadius * camera.PixelsPerWorldUnit);
            using var path = Sector(x, y, inner, outer, data.StartAngleDegrees, data.SweepDegrees);
            var (fill, border) = FieldColors(data.Kind, data.Intensity);
            canvas.Save(); canvas.ClipRect(SKRect.Create(width, height));
            if (data.Kind is "Dust" or "Radiation")
            {
                canvas.Save();
                canvas.ClipPath(path, antialias: true);
                DrawCloudField(canvas, data, x, y, inner, outer, width, height);
                canvas.Restore();
                if (data.Id == selectedId)
                {
                    paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = 2;
                    paint.Color = border; canvas.DrawPath(path, paint);
                }
                canvas.Restore();
                continue;
            }
            paint.Style = SKPaintStyle.Fill; paint.Color = fill;
            canvas.DrawPath(path, paint);
            canvas.Save(); canvas.ClipPath(path);
            paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = 1; paint.Color = border;
            var key = (data.Id, data.DecorationSeed, count);
            if (!_patterns.TryGetValue(key, out var pattern)) _patterns[key] = pattern = Pattern(data.DecorationSeed, count);
            foreach (var sample in pattern)
            {
                double ratio = data.InnerRadius / data.OuterRadius;
                double r = data.OuterRadius * Math.Sqrt(ratio * ratio + (1 - ratio * ratio) * sample.Radius);
                double angle = (data.StartAngleDegrees + data.SweepDegrees * sample.Angle) * Math.PI / 180;
                var p = camera.WorldToScreen(field.X + r * Math.Sin(angle), field.Y - r * Math.Cos(angle), width, height);
                if (p.X < -3 || p.X > width + 3 || p.Y < -3 || p.Y > height + 3) continue;
                canvas.DrawLine(p.X - 2, p.Y + 2, p.X, p.Y - 2, paint);
                canvas.DrawLine(p.X, p.Y - 2, p.X + 2, p.Y + 2, paint);
                canvas.DrawLine(p.X - 2, p.Y + 2, p.X + 2, p.Y + 2, paint);
            }
            canvas.Restore();
            paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = data.Id == selectedId ? 2 : 1;
            paint.Color = border; canvas.DrawPath(path, paint);
            canvas.Restore();
        }
    }

    internal static (SKColor Fill, SKColor Border) FieldColors(string kind, double intensity)
    {
        var color = kind switch
        {
            "Radiation" => AnomalyFieldLayer.HeatRamp[^1].Color,
            "Dust" => AnomalyFieldLayer.DustRamp[^1].Color,
            _ => new SKColor(169, 150, 133)
        };
        double t = double.IsFinite(intensity) ? Math.Clamp(intensity, 0, 1) : 0;
        double alpha = kind == "Radiation" ? 6 + 6 * t : 5 + 5 * t;
        return (color.WithAlpha(TacticalMapPalette.Alpha(alpha)),
            color.WithAlpha(TacticalMapPalette.Alpha(kind == "Radiation" ? 25 : 20)));
    }

    private static void DrawCloudField(SKCanvas canvas, EnvironmentFieldData field, float x, float y,
        float inner, float outer, int width, int height)
    {
        double thickness = outer - (double)inner;
        if (thickness <= 0) return;
        bool radiation = field.Kind == "Radiation";
        bool hasCore = radiation && inner == 0 && field.SweepDegrees == 360;
        double middle = inner + thickness * (hasCore ? .35 : .5);
        double sweep = field.SweepDegrees * Math.PI / 180;
        int count = hasCore ? 9 : (int)Math.Clamp(Math.Ceiling(middle * sweep / thickness), 1, 512);
        double step = sweep / count;
        double start = (field.StartAngleDegrees - 90) * Math.PI / 180;
        double intensity = double.IsFinite(field.Intensity) ? Math.Clamp(field.Intensity, 0, 1) : 0;

        // Convert the battle's logarithmic density ramp to radial stops for a Gaussian clump.
        var ramp = radiation ? AnomalyFieldLayer.HeatRamp : AnomalyFieldLayer.DustRamp;
        var colors = new SKColor[ramp.Length];
        var stops = new float[ramp.Length];
        double opacity = 255 * (.45 + .45 * intensity);
        for (int i = 0; i < ramp.Length - 1; i++)
        {
            var (density, color) = ramp[^(i + 1)];
            stops[i] = (float)(Math.Sqrt(-2 * Math.Log(Math.Pow(2, density) - 1)) / 3);
            colors[i] = color.WithAlpha((byte)Math.Round(opacity * (radiation ? 1 : Math.Sqrt(density))));
        }
        // Black is neutral for Screen blending. The radiation ramp changes color only.
        colors[^1] = radiation ? SKColors.Black.WithAlpha(colors[0].Alpha) : SKColors.Transparent;
        stops[^1] = 1;
        using var shader = SKShader.CreateRadialGradient(SKPoint.Empty, 1, colors, stops, SKShaderTileMode.Clamp);
        using var paint = new SKPaint { IsAntialias = true, Shader = shader, BlendMode = SKBlendMode.Screen };
        if (hasCore) DrawClump(x, y, 0, outer, outer);

        ulong seed = field.DecorationSeed;
        for (int i = 0; i < count; i++)
        {
            double angle = start + step * (i + .3 + .4 * Next());
            double centerRadius = middle + thickness * (Next() - .5) * .20;
            double radialRadius = thickness * ((hasCore ? .19 : .28) + .16 * Next());
            double tangentRadius = Math.Max(radialRadius, middle * step * (.48 + .26 * Next()));
            var (sin, cos) = Math.SinCos(angle);
            DrawClump(x + centerRadius * cos, y + centerRadius * sin, angle, radialRadius, tangentRadius);
        }

        void DrawClump(double cx, double cy, double angle, double radialRadius, double tangentRadius)
        {
            double extent = Math.Max(radialRadius, tangentRadius);
            if (cx + extent < 0 || cy + extent < 0 || cx - extent > width || cy - extent > height) return;
            canvas.Save();
            canvas.Translate((float)cx, (float)cy);
            canvas.RotateDegrees((float)(angle * 180 / Math.PI));
            canvas.Scale((float)radialRadius, (float)tangentRadius);
            canvas.DrawCircle(0, 0, 1, paint);
            canvas.Restore();
        }

        double Next()
        {
            unchecked { seed = seed * 6364136223846793005UL + 1442695040888963407UL; }
            return (seed >> 11) / 9007199254740992.0;
        }
    }

    private static (double Radius, double Angle)[] Pattern(ulong seed, int count)
    {
        // Presentation-only deterministic decoration; never consumes an Engine RNG stream.
        double Next() { unchecked { seed = seed * 6364136223846793005UL + 1442695040888963407UL; } return (seed >> 11) / 9007199254740992.0; }
        return Enumerable.Range(0, count).Select(_ => (Next(), Next())).ToArray();
    }

    private static SKPath Sector(float x, float y, float inner, float outer, double start, double sweep)
    {
        var path = new SKPath { FillType = SKPathFillType.EvenOdd };
        if (sweep == 360)
        {
            path.AddCircle(x, y, outer); if (inner > 0) path.AddCircle(x, y, inner);
            return path;
        }
        var outside = new SKRect(x - outer, y - outer, x + outer, y + outer);
        float angle = (float)(start - 90), end = (float)(start + sweep - 90);
        path.MoveTo(x + inner * MathF.Cos(angle * MathF.PI / 180), y + inner * MathF.Sin(angle * MathF.PI / 180));
        path.LineTo(x + outer * MathF.Cos(angle * MathF.PI / 180), y + outer * MathF.Sin(angle * MathF.PI / 180));
        path.ArcTo(outside, angle, (float)sweep, false);
        path.LineTo(x + inner * MathF.Cos(end * MathF.PI / 180), y + inner * MathF.Sin(end * MathF.PI / 180));
        if (inner > 0) path.ArcTo(new(x - inner, y - inner, x + inner, y + inner), end, (float)-sweep, false);
        path.Close(); return path;
    }
}
