using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Demo;

internal static class PirateGroupsPresentation
{
    internal sealed record Marker(string Id, double X, double Y, string Label, bool Confirmed);

    internal static IReadOnlyList<Marker> Markers(DemoEncountersSnapshot? encounters) =>
        encounters is null || encounters.Groups.IsDefaultOrEmpty ? [] : encounters.Groups.Select(group =>
        {
            string type = !group.IsDetected || group.Signatures.IsDefaultOrEmpty ? Localization.Get("Demo.Pirate.Unknown")
                : string.Join(" + ", group.Signatures.Select(s => Localization.Get(s.DisplayNameKey)));
            string status = Localization.Get(group.Status switch
            {
                PirateGroupStatuses.Captured => "Demo.Pirate.Captured",
                PirateGroupStatuses.DebugVisible => "Demo.Pirate.DebugVisible",
                _ => "Demo.Pirate.Observed"
            });
            string count = group.IsDetected && group.ApproximateDroneCount is { } approximate
                ? " · " + string.Format(Localization.Get("Demo.Pirate.ApproximateDrones"), approximate) : "";
            return new Marker(group.Id, group.X, group.Y, status + ": " + type + count, group.IsDetected);
        }).ToArray();

    internal static void Draw(SKCanvas canvas, DemoEncountersSnapshot? encounters, CameraState camera, int width, int height)
    {
        using var paint = new SKPaint { IsAntialias = true, TextSize = 14, StrokeWidth = 2 };
        foreach (var marker in Markers(encounters))
        {
            var (x, y) = camera.WorldToScreen(marker.X, marker.Y, width, height);
            if (!float.IsFinite(x) || !float.IsFinite(y) || x < -20 || y < -20 || x > width + 20 || y > height + 20) continue;
            paint.Color = marker.Confirmed ? new SKColor(236, 111, 92) : new SKColor(150, 150, 150);
            paint.Style = SKPaintStyle.Stroke;
            using var path = new SKPath();
            path.MoveTo(x, y - 9); path.LineTo(x + 9, y); path.LineTo(x, y + 9); path.LineTo(x - 9, y); path.Close();
            canvas.DrawPath(path, paint);
            paint.Style = SKPaintStyle.Fill;
            string label = marker.Label;
            while (label.Length > 1 && paint.MeasureText(label) > width - 24) label = label[..^1];
            float left = Math.Clamp(x + 14, 8, Math.Max(8, width - paint.MeasureText(label) - 8));
            float top = Math.Clamp(y - 14, 20, Math.Max(20, height - 8));
            canvas.DrawText(label, left, top, paint);
        }
    }
}
