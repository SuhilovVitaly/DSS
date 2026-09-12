using DeepSpaceSaga.Client.UI.Screens.GameSession;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class ObjectTrailGeometryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Dense_curved_history_keeps_geometry_opacity_and_color_transition(bool isShip)
    {
        var points = Enumerable.Range(0, 801)
            .Select(i => new SKPoint(i / 4f, 20 * MathF.Sin(i / 25f))).ToArray();
        var geometry = new ObjectTrailGeometry();
        geometry.Simplify(points, isShip);
        Assert.True(geometry.Segments.Count < 200);
        Assert.Equal(0, geometry.Segments[0].Start);
        Assert.Equal(points.Length - 1, geometry.Segments[^1].End);
        int previous = 0;
        foreach (var (start, end) in geometry.Segments)
        {
            Assert.Equal(previous, start);
            Assert.True(end > start);
            var drawn = GameSessionScreen.GetTrailSegmentColor((float)end / (points.Length - 1), isShip);
            for (int i = start + 1; i <= end; i++)
            {
                Assert.InRange(ObjectTrailGeometry.DistanceSquared(points[i], points[start], points[end]),
                    0, ObjectTrailGeometry.MaxDeviationPixels * ObjectTrailGeometry.MaxDeviationPixels);
                var original = GameSessionScreen.GetTrailSegmentColor((float)i / (points.Length - 1), isShip);
                Assert.Equal(original.Red, drawn.Red);
                Assert.Equal(original.Green, drawn.Green);
                Assert.Equal(original.Blue, drawn.Blue);
                Assert.InRange(drawn.Alpha - original.Alpha, 0, ObjectTrailGeometry.MaxAlphaDifference);
            }
            previous = end;
        }
    }

    [Fact]
    public void Sharp_reversal_is_not_replaced_by_a_zero_length_chord()
    {
        var points = Enumerable.Repeat(new SKPoint(0, 0), 801).ToArray();
        points[4] = new(100, 100);
        var geometry = new ObjectTrailGeometry();
        geometry.Simplify(points, false);
        Assert.Contains(geometry.Segments, segment => segment.End == 4);
        Assert.Contains(geometry.Segments, segment => segment.Start == 4);
        geometry.Simplify(Array.Empty<SKPoint>(), false);
        Assert.Empty(geometry.Segments);
    }
}
