using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal static class AiMapPresentation
{
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
