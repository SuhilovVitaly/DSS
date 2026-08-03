using SkiaSharp;

namespace DeepSpaceSaga.Client.UI;

/// <summary>
/// Draws an adaptive world-coordinate grid aligned to world units.
/// Pure client-side rendering — never touches the Engine.
/// </summary>
public sealed class GridRenderer
{
    private const double HighDetailScaleThreshold = 5.0;
    private const double LowDetailScaleThreshold = 0.1;
    private const double MinGridStepPixels = 10.0;

    private static readonly int[] HighDetailCandidates = { 1, 5, 10, 100, 1000, 10000 };
    private static readonly int[] NormalDetailCandidates = { 10, 100, 1000, 10000 };
    private static readonly int[] LowDetailCandidates = { 1000, 10000 };

    private static readonly (int WorldStep, SKColor Color)[] AllLevels =
    {
        (1,     new SKColor(8, 8, 8)),
        (5,     new SKColor(10, 10, 10)),
        (10,    new SKColor(12, 12, 12)),
        (100,   new SKColor(22, 22, 22)),
        (1000,  new SKColor(42, 42, 42)),
        (10000, new SKColor(62, 62, 62)),
    };

    private readonly SKPaint _backgroundPaint;
    private readonly SKPaint _axisPaint;
    private readonly Dictionary<int, SKPaint> _levelPaints;

    public GridRenderer()
    {
        _backgroundPaint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Fill,
        };

        _axisPaint = new SKPaint
        {
            Color = new SKColor(90, 90, 90),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f,
            IsAntialias = true,
        };

        _levelPaints = new Dictionary<int, SKPaint>();
        foreach (var (step, color) in AllLevels)
        {
            _levelPaints[step] = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f,
                IsAntialias = true,
            };
        }
    }

    /// <summary>
    /// Draw the adaptive world grid for the current camera state and viewport.
    /// </summary>
    public void Draw(SKCanvas canvas, CameraState camera, int viewportWidth, int viewportHeight)
    {
        // 1. Clear background
        canvas.DrawRect(0, 0, viewportWidth, viewportHeight, _backgroundPaint);

        if (viewportWidth <= 0 || viewportHeight <= 0)
            return;

        // 2. Determine world bounds of the viewport
        var (worldLeft, worldTop) = camera.ScreenToWorld(0, 0, viewportWidth, viewportHeight);
        var (worldRight, worldBottom) = camera.ScreenToWorld(viewportWidth, viewportHeight, viewportWidth, viewportHeight);

        double pixelsPerUnit = camera.PixelsPerWorldUnit;

        // 3. Draw grid levels from smallest to largest (larger steps paint over smaller)
        var candidates = GetDetailModeCandidates(pixelsPerUnit);
        foreach (int worldStep in candidates)
        {
            if (worldStep * pixelsPerUnit < MinGridStepPixels)
                continue;

            if (!_levelPaints.TryGetValue(worldStep, out var paint))
                continue;

            DrawLevel(canvas, worldStep, worldLeft, worldRight, worldTop, worldBottom,
                      pixelsPerUnit, camera, viewportWidth, viewportHeight, paint);
        }

        // 5. Draw world axes (X=0 and Y=0) on top
        DrawWorldAxes(canvas, camera, viewportWidth, viewportHeight, worldLeft, worldRight, worldTop, worldBottom);
    }

    /// <summary>
    /// Select which grid levels should be drawn for the given scale.
    /// Levels are filtered by detail mode AND minimum screen pixel spacing.
    /// </summary>
    public static IReadOnlyList<int> GetEligibleLevels(double pixelsPerWorldUnit)
    {
        var candidates = GetDetailModeCandidates(pixelsPerWorldUnit);
        var result = new List<int>(candidates.Length);

        foreach (int worldStep in candidates)
        {
            double screenStepPixels = worldStep * pixelsPerWorldUnit;
            if (screenStepPixels >= MinGridStepPixels)
                result.Add(worldStep);
        }

        return result;
    }

    /// <summary>
    /// Select candidate world steps based on the detail mode (high / normal / low).
    /// Returns a reference to a static cached array — zero allocations.
    /// This does NOT filter by screen pixel spacing.
    /// </summary>
    public static int[] GetDetailModeCandidates(double pixelsPerWorldUnit)
    {
        if (pixelsPerWorldUnit >= HighDetailScaleThreshold)
            return HighDetailCandidates;

        if (pixelsPerWorldUnit <= LowDetailScaleThreshold)
            return LowDetailCandidates;

        return NormalDetailCandidates;
    }

    /// <summary>
    /// Compute the first world coordinate that is a multiple of worldStep
    /// and is less than or equal to worldBound.
    /// Works correctly for negative coordinates.
    /// </summary>
    public static double ComputeFirstWorldLine(double worldBound, double worldStep)
    {
        return Math.Floor(worldBound / worldStep) * worldStep;
    }

    private static void DrawLevel(
        SKCanvas canvas,
        int worldStep,
        double worldLeft, double worldRight,
        double worldTop, double worldBottom,
        double pixelsPerUnit,
        CameraState camera,
        int viewportWidth, int viewportHeight,
        SKPaint paint)
    {
        // Vertical lines (along X axis)
        double firstX = ComputeFirstWorldLine(worldLeft, worldStep);
        for (double wx = firstX; wx <= worldRight + worldStep; wx += worldStep)
        {
            var (sx, _) = camera.WorldToScreen(wx, 0, viewportWidth, viewportHeight);
            canvas.DrawLine(sx, 0, sx, viewportHeight, paint);
        }

        // Horizontal lines (along Y axis)
        double firstY = ComputeFirstWorldLine(worldTop, worldStep);
        for (double wy = firstY; wy <= worldBottom + worldStep; wy += worldStep)
        {
            var (_, sy) = camera.WorldToScreen(0, wy, viewportWidth, viewportHeight);
            canvas.DrawLine(0, sy, viewportWidth, sy, paint);
        }
    }

    private void DrawWorldAxes(
        SKCanvas canvas,
        CameraState camera,
        int viewportWidth, int viewportHeight,
        double worldLeft, double worldRight,
        double worldTop, double worldBottom)
    {
        // X-axis (Y = 0) — horizontal line through world origin
        if (worldTop <= 0 && worldBottom >= 0)
        {
            var (_, sy) = camera.WorldToScreen(0, 0, viewportWidth, viewportHeight);
            canvas.DrawLine(0, sy, viewportWidth, sy, _axisPaint);
        }

        // Y-axis (X = 0) — vertical line through world origin
        if (worldLeft <= 0 && worldRight >= 0)
        {
            var (sx, _) = camera.WorldToScreen(0, 0, viewportWidth, viewportHeight);
            canvas.DrawLine(sx, 0, sx, viewportHeight, _axisPaint);
        }
    }
}
