using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Screen-space detail reduction only; recorded history is never changed.
/// Every omitted point stays within 0.25 px of its chord and opacity differs
/// by at most 3/255. Colour transitions and both endpoints remain exact.
/// </summary>
internal sealed class ObjectTrailGeometry
{
    internal const float MaxDeviationPixels = .25f;
    internal const int MaxAlphaDifference = 3;
    private readonly List<SKPoint> _projected = new(256);
    private readonly Stack<(int Start, int End)> _pending = new();
    internal List<(int Start, int End)> Segments { get; } = new(128);
    internal IReadOnlyList<SKPoint> Points => _projected;

    internal void Build(IReadOnlyList<ObjectTrailPoint> points, CameraState camera,
        int width, int height, bool isShip)
    {
        _projected.Clear();
        for (int i = 0; i < points.Count; i++)
        {
            var (x, y) = camera.WorldToScreen(points[i].X, points[i].Y, width, height);
            _projected.Add(new(x, y));
        }
        Simplify(_projected, isShip);
    }

    internal void Simplify(IReadOnlyList<SKPoint> points, bool isShip)
    {
        Segments.Clear();
        _pending.Clear();
        for (int start = 0; start < points.Count - 1;)
        {
            int end = start + 1;
            var firstColor = GameSessionScreen.GetTrailSegmentColor((float)end / (points.Count - 1), isShip);
            while (end + 1 < points.Count)
            {
                var color = GameSessionScreen.GetTrailSegmentColor((float)(end + 1) / (points.Count - 1), isShip);
                if (color.Red != firstColor.Red || color.Alpha - firstColor.Alpha > MaxAlphaDifference)
                    break;
                end++;
            }

            _pending.Push((start, end));
            while (_pending.TryPop(out var range))
            {
                float maxError = MaxDeviationPixels * MaxDeviationPixels;
                int split = -1;
                for (int i = range.Start + 1; i < range.End; i++)
                {
                    float error = DistanceSquared(points[i], points[range.Start], points[range.End]);
                    if (error > maxError) { maxError = error; split = i; }
                }
                if (split < 0)
                    Segments.Add(range);
                else
                {
                    _pending.Push((split, range.End));
                    _pending.Push((range.Start, split));
                }
            }
            start = end;
        }
    }

    internal static float DistanceSquared(SKPoint point, SKPoint from, SKPoint to)
    {
        float dx = to.X - from.X, dy = to.Y - from.Y;
        float lengthSq = dx * dx + dy * dy;
        float t = lengthSq > 0 ? Math.Clamp(((point.X - from.X) * dx + (point.Y - from.Y) * dy) / lengthSq, 0, 1) : 0;
        float ex = point.X - (from.X + t * dx), ey = point.Y - (from.Y + t * dy);
        return ex * ex + ey * ey;
    }
}
