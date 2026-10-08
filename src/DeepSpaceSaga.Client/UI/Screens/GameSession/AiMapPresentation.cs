using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal static class AiMapPresentation
{
    internal const string PoiNotice = "Исследование пока недоступно";
    internal sealed record PoiGeometry(PointOfInterestData Data, double X, double Y);

    internal static IReadOnlyList<PoiGeometry> Points(AuthoritativeSnapshot snapshot, IEnumerable<ObjectMotionSnapshot> poses, long motionTimeMs)
    {
        if (snapshot.AiMap is not { } map || map.PointsOfInterest.IsDefaultOrEmpty) return [];
        var objects = poses.ToDictionary(p => p.ObjectId, StringComparer.OrdinalIgnoreCase);
        var result = new List<PoiGeometry>();
        foreach (var point in map.PointsOfInterest.OrderBy(p => p.ObjectId, StringComparer.Ordinal))
        {
            ObjectMotionSnapshot? anchor = null;
            if (point.ParentObjectId is { } parent) objects.TryGetValue(parent, out anchor);
            else if (point.Orbit is { } orbit) anchor = OrbitalMotionMath.At(new(point.ObjectId, 0, 0, 0, 0), orbit, motionTimeMs);
            if (anchor is not null) result.Add(new(point, anchor.X + point.OffsetX, anchor.Y + point.OffsetY));
        }
        return result;
    }

    internal static void DrawPoints(SKCanvas canvas, IReadOnlyList<PoiGeometry> points, CameraState camera, int width, int height, string? selectedId)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = new SKColor(150, 215, 210), Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
        foreach (var point in points)
        {
            var (x, y) = camera.WorldToScreen(point.X, point.Y, width, height);
            if (!float.IsFinite(x) || !float.IsFinite(y) || x < -30 || x > width + 30 || y < -30 || y > height + 30) continue;
            using var path = new SKPath();
            for (int i = 0; i < 6; i++)
            {
                float dx = 9 * (float)Math.Cos(i * Math.PI / 3), dy = 9 * (float)Math.Sin(i * Math.PI / 3);
                if (i == 0) path.MoveTo(x + dx, y + dy); else path.LineTo(x + dx, y + dy);
            }
            path.Close(); paint.Style = SKPaintStyle.Stroke; canvas.DrawPath(path, paint);
            if (point.Data.ObjectId == selectedId) canvas.DrawCircle(x, y, 14, paint);
            paint.Style = SKPaintStyle.Fill; paint.TextSize = 12; canvas.DrawText("POI", x + 13, y - 10, paint);
        }
    }

    internal const string TerritoryNotice = "Территория ИИ; патрули будут добавлены позднее";
    internal sealed record TerritoryGeometry(TerritoryMapData Data, double X, double Y,
        double DefenceRadiusWorld, double PatrolRadiusWorld);

    internal static TerritoryMapData? Territory(AuthoritativeSnapshot? snapshot, string id)
    {
        var rows = snapshot?.AiMap?.Territories ?? default;
        return rows.IsDefaultOrEmpty ? null : rows.FirstOrDefault(t => string.Equals(t.BaseObjectId, id, StringComparison.OrdinalIgnoreCase));
    }

    internal static IReadOnlyList<TerritoryGeometry> Territories(AuthoritativeSnapshot snapshot, IEnumerable<ObjectMotionSnapshot> poses)
    {
        if (snapshot.AiMap is not { } map || map.Territories.IsDefaultOrEmpty) return [];
        var byId = poses.ToDictionary(p => p.ObjectId, StringComparer.OrdinalIgnoreCase);
        return map.Territories.Where(t => byId.ContainsKey(t.BaseObjectId)).Select(t =>
            new TerritoryGeometry(t, byId[t.BaseObjectId].X, byId[t.BaseObjectId].Y, t.DefenceRadiusKm * 10, t.PatrolRadiusKm * 10)).ToArray();
    }

    internal static void DrawTerritories(SKCanvas canvas, AuthoritativeSnapshot snapshot, IEnumerable<ObjectMotionSnapshot> poses,
        CameraState camera, int width, int height, string? selectedId)
    {
        var rows = Territories(snapshot, poses);
        if (rows.Count == 0) return;
        using var outer = new SKPath { FillType = SKPathFillType.Winding };
        using var inner = new SKPath { FillType = SKPathFillType.Winding };
        var contours = new List<(float X, float Y, float Inner, float Outer, bool Selected)>();
        foreach (var row in rows)
        {
            var (x, y) = camera.WorldToScreen(row.X, row.Y, width, height);
            double outerRadius = row.PatrolRadiusWorld * camera.PixelsPerWorldUnit;
            double innerRadius = row.DefenceRadiusWorld * camera.PixelsPerWorldUnit;
            if (!float.IsFinite(x) || !float.IsFinite(y) || !double.IsFinite(outerRadius) || outerRadius <= 0 ||
                x + outerRadius < 0 || y + outerRadius < 0 || x - outerRadius > width || y - outerRadius > height) continue;
            AddCircle(outer, x, y, outerRadius, width, height);
            AddCircle(inner, x, y, innerRadius, width, height);
            if (outerRadius <= 1e8) contours.Add((x, y, (float)innerRadius, (float)outerRadius, row.Data.BaseObjectId == selectedId));
        }
        using var paint = new SKPaint { IsAntialias = true, Color = new SKColor(255, 98, 80, 24) };
        canvas.Save(); canvas.ClipRect(SKRect.Create(width, height));
        // One winding fill per radius class: overlap does not compound opacity.
        canvas.DrawPath(outer, paint); paint.Color = new SKColor(255, 98, 80, 35); canvas.DrawPath(inner, paint);
        paint.Style = SKPaintStyle.Stroke;
        foreach (var c in contours)
        {
            paint.Color = new SKColor(255, 128, 104, c.Selected ? (byte)230 : (byte)100);
            paint.StrokeWidth = c.Selected ? 2 : 1;
            canvas.DrawCircle(c.X, c.Y, c.Outer, paint);
            canvas.DrawCircle(c.X, c.Y, c.Inner, paint);
        }
        canvas.Restore();
    }

    private static void AddCircle(SKPath path, float x, float y, double radius, int width, int height)
    {
        if (radius <= 1e8) path.AddCircle(x, y, (float)radius);
        else if (Math.Max(Math.Abs(x), Math.Abs(x - width)) + Math.Max(Math.Abs(y), Math.Abs(y - height)) < radius)
            path.AddRect(SKRect.Create(width, height));
    }

    internal static AiBaseMapData? Base(AuthoritativeSnapshot? snapshot, string? id)
    {
        var bases = snapshot?.AiMap?.Bases ?? default;
        return bases.IsDefaultOrEmpty ? null : bases.FirstOrDefault(b => string.Equals(b.ObjectId, id, StringComparison.OrdinalIgnoreCase));
    }

    internal static void DrawBase(SKCanvas canvas, AiBaseMapData data, float x, float y, float radius)
    {
        using var paint = new SKPaint { IsAntialias = true, Color = new SKColor(255, 112, 96), StrokeWidth = 2, Style = SKPaintStyle.Stroke };
        float r = Math.Max(radius, 8);
        using var path = new SKPath();
        path.MoveTo(x, y - r); path.LineTo(x + r, y); path.LineTo(x, y + r); path.LineTo(x - r, y); path.Close();
        canvas.DrawPath(path, paint);
        if (data.BaseType == "Planetary") canvas.DrawCircle(x, y, r * 0.45f, paint);
        else { canvas.DrawLine(x - r * 0.45f, y, x + r * 0.45f, y, paint); canvas.DrawLine(x, y - r * 0.45f, x, y + r * 0.45f, paint); }
        paint.Style = SKPaintStyle.Fill; paint.TextSize = 12;
        canvas.DrawText("AI", x + r + 4, y - r, paint);
    }
}
