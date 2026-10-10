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
            var color = data.Kind switch { "Radiation" => new SKColor(220, 192, 100), "Dust" => new SKColor(150, 165, 200), _ => new SKColor(185, 160, 145) };
            canvas.Save(); canvas.ClipRect(SKRect.Create(width, height));
            paint.Style = SKPaintStyle.Fill; paint.Color = color.WithAlpha((byte)(12 + data.Intensity * 22));
            canvas.DrawPath(path, paint);
            canvas.Save(); canvas.ClipPath(path);
            paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = 1; paint.Color = color.WithAlpha(95);
            if (data.Kind == "Radiation")
            {
                for (int i = 1; i <= 3; i++) canvas.DrawCircle(x, y, inner + (outer - inner) * i / 4, paint);
            }
            else if (data.Kind == "Dust")
            {
                float phase = data.DecorationSeed % 20;
                for (float start = -height + phase; start <= width; start += 20)
                    canvas.DrawLine(start, 0, start + height, height, paint);
            }
            else
            {
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
            }
            canvas.Restore();
            paint.Style = SKPaintStyle.Stroke; paint.StrokeWidth = data.Id == selectedId ? 2 : 1;
            paint.Color = color.WithAlpha(data.Id == selectedId ? (byte)220 : (byte)90); canvas.DrawPath(path, paint);
            canvas.Restore();
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
