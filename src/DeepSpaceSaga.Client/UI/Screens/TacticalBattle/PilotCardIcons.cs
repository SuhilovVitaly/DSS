using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal static class PilotCardIcons
{
    internal static void StatIcon(SKCanvas canvas, string kind, SKPoint center, SKColor color)
    {
        using var path = new SKPath();
        switch (kind)
        {
            case "armor":
                path.MoveTo(0, -4.6f);
                path.LineTo(4.6f, -3);
                path.LineTo(4.2f, .6f);
                path.QuadTo(3.2f, 3.6f, 0, 4.6f);
                path.QuadTo(-3.2f, 3.6f, -4.2f, .6f);
                path.LineTo(-4.6f, -3);
                break;
            case "hull":
                path.MoveTo(0, -4.6f);
                path.LineTo(4.6f, 4.6f);
                path.LineTo(0, 2.4f);
                path.LineTo(-4.6f, 4.6f);
                break;
            case "mind":
                path.MoveTo(-4.6f, 0);
                path.QuadTo(0, -5.6f, 4.6f, 0);
                path.QuadTo(0, 5.6f, -4.6f, 0);
                break;
            default:
                return;
        }

        path.Close();
        using var paint = new SKPaint
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.3f,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };
        path.Offset(center.X, center.Y);
        canvas.DrawPath(path, paint);
        if (kind == "mind")
        {
            paint.Style = SKPaintStyle.Fill;
            canvas.DrawCircle(center, 1.5f, paint);
        }
    }
}
