using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Demo;

internal static class BountySearchAreaPresentation
{
    internal static IReadOnlyList<BountyOrder> Areas(TargetBountySnapshot? bounties) => bounties?.Orders.Where(b =>
        b.Tracking && b.Eligibility == BountyEligibility.TargetAvailable && b.SearchArea is { Radius: > 0 } a &&
        double.IsFinite(a.X) && double.IsFinite(a.Y) && double.IsFinite(a.Radius)).ToArray() ?? [];
    internal static void Draw(SKCanvas canvas, TargetBountySnapshot? bounties, CameraState camera, int width, int height)
    {
        using var fill = new SKPaint { IsAntialias = true, Color = new(115, 154, 230, 22) };
        using var stroke = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2, Color = new(115, 154, 230, 150) };
        using var text = new SKPaint { IsAntialias = true, TextSize = 13, Color = new(160, 190, 245) };
        int labelRow = 0;
        canvas.Save(); canvas.ClipRect(new(0, 0, width, height));
        foreach (var order in Areas(bounties))
        {
            var area = order.SearchArea!;
            var (x, y) = camera.WorldToScreen(area.X, area.Y, width, height);
            float radius = (float)(area.Radius * camera.PixelsPerWorldUnit);
            if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(radius) || radius <= 0 || x + radius < 0 || y + radius < 0 || x - radius > width || y - radius > height) continue;
            canvas.DrawCircle(x, y, radius, fill); canvas.DrawCircle(x, y, radius, stroke);
            string label = string.Format(Localization.Get("bounty.map.area_label"), order.TargetGroupId, order.RewardTokens);
            while (label.Length > 1 && text.MeasureText(label) > width - 32) label = label[..^1];
            float left = Math.Clamp(x - radius, 16, Math.Max(16, width - text.MeasureText(label) - 16));
            float top = Math.Clamp(y - radius + 20 + labelRow++ * 20, 24, Math.Max(24, height - 24));
            canvas.DrawText(label, left, top, text);
        }
        canvas.Restore();
    }
}
