using SkiaSharp;

namespace DeepSpaceSaga.Client.UI;

/// <summary>World-aligned nested 5×5 cells with continuous density-based color and bounded work.</summary>
public sealed class GridRenderer
{
    public const int Subdivision = 5;
    private readonly TacticalMapSettings _settings;
    private readonly SKPaint _paint = new() { Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
    public GridRenderer(TacticalMapSettings? settings = null) => _settings = (settings ?? new()).Validate();
    public static double ComputeFirstWorldLine(double worldBound, double worldStep) => Math.Floor(worldBound / worldStep) * worldStep;

    public static IReadOnlyList<double> GetEligibleLevels(double pixelsPerWorldUnit,
        TacticalMapSettings? settings = null, int viewportExtent = 1920)
    {
        var s = settings ?? new TacticalMapSettings();
        return GetEligibleLevels(pixelsPerWorldUnit, s, viewportExtent, s.GridBaseCellPixels / s.MaximumPpu);
    }

    private static IReadOnlyList<double> GetEligibleLevels(double pixelsPerWorldUnit, TacticalMapSettings s,
        int viewportExtent, double baseStep)
    {
        if (!double.IsFinite(pixelsPerWorldUnit) || pixelsPerWorldUnit <= 0) return Array.Empty<double>();
        // Jump straight to the first visible ancestor, even at astronomical zoom levels.
        double exponent = Math.Max(0, Math.Ceiling(
            (Math.Log(s.GridMinimumPixels) - Math.Log(baseStep) - Math.Log(pixelsPerWorldUnit)) / Math.Log(Subdivision)));
        double step = baseStep * Math.Pow(Subdivision, exponent);
        var levels = new List<double>(6);
        double upperPixels = Math.Max(s.GridBaseCellPixels, viewportExtent) * Subdivision;
        while (double.IsFinite(step))
        {
            levels.Add(step);
            // Ancestors larger than the viewport have the same saturated color. Their
            // boundaries are already present in this last level, so no further work is needed.
            if (step * pixelsPerWorldUnit >= upperPixels) break;
            step *= Subdivision;
        }
        return levels;
    }

    public static double LevelOpacity(double step, double ppu, TacticalMapSettings settings) =>
        SmoothStep((step * ppu - settings.GridMinimumPixels) / settings.GridFadePixels);

    public static SKColor LevelColor(double step, double ppu, TacticalMapSettings settings) =>
        LevelColor(step, ppu, settings, SKColors.Black);

    private static SKColor LevelColor(double step, double ppu, TacticalMapSettings settings, SKColor background)
    {
        double pixels = step * ppu;
        double fullVisibility = settings.GridMinimumPixels + settings.GridFadePixels;
        double majorPixels = settings.GridBaseCellPixels * Subdivision;
        double weight = SmoothStep(Math.Log(Math.Max(pixels, fullVisibility) / fullVisibility) /
            Math.Log(majorPixels / fullVisibility));
        double opacity = LevelOpacity(step, ppu, settings);
        // Premix against the map background. Shared parent boundaries must not
        // brighten by accumulating several translucent copies of the same line.
        byte Channel(byte baseChannel, double fine, double major) =>
            (byte)Math.Min(255, baseChannel + Math.Round((fine + (major - fine) * weight) * opacity));
        return new SKColor(Channel(background.Red, 18, 35), Channel(background.Green, 23, 44), Channel(background.Blue, 28, 52));
    }

    private static double SmoothStep(double value)
    {
        double t = Math.Clamp(value, 0, 1);
        return t * t * (3 - 2 * t);
    }

    public void Draw(SKCanvas canvas, CameraState camera, int viewportWidth, int viewportHeight)
    {
        canvas.Clear(SKColors.Black);
        DrawLines(canvas, camera.FocusX, camera.FocusY, camera.PixelsPerWorldUnit, viewportWidth, viewportHeight,
            _settings, _paint, SKColors.Black, _settings.GridBaseCellPixels / _settings.MaximumPpu);
    }

    internal static void DrawLines(SKCanvas canvas, double focusX, double focusY, double ppu,
        int viewportWidth, int viewportHeight, TacticalMapSettings settings, SKPaint paint, SKColor background, double baseStep, Func<SKColor, SKColor>? style = null)
    {
        if (viewportWidth <= 0 || viewportHeight <= 0) return;
        double halfWidth = viewportWidth * 0.5, halfHeight = viewportHeight * 0.5;
        double left = focusX - halfWidth / ppu, top = focusY - halfHeight / ppu;
        double right = focusX + halfWidth / ppu, bottom = focusY + halfHeight / ppu;
        var levels = GetEligibleLevels(ppu, settings, Math.Max(viewportWidth, viewportHeight), baseStep);
        for (int level = 0; level < levels.Count; level++)
        {
            double step = levels[level];
            if (LevelOpacity(step, ppu, settings) <= 0) continue;
            var color = LevelColor(step, ppu, settings, background);
            paint.Color = style?.Invoke(color) ?? color;
            double firstX = Math.Floor(left / step), firstY = Math.Floor(top / step);
            int nx = Math.Min(4096, (int)Math.Ceiling((right - left) / step) + 2);
            int ny = Math.Min(4096, (int)Math.Ceiling((bottom - top) / step) + 2);
            bool hasParent = level + 1 < levels.Count;
            for (int i = 0; i < nx; i++)
            {
                double index = firstX + i;
                if (hasParent && index % Subdivision == 0) continue;
                float x = (float)(halfWidth + (index * step - focusX) * ppu);
                if (x >= 0 && x <= viewportWidth) canvas.DrawLine(x, 0, x, viewportHeight, paint);
            }
            for (int i = 0; i < ny; i++)
            {
                double index = firstY + i;
                if (hasParent && index % Subdivision == 0) continue;
                float y = (float)(halfHeight + (index * step - focusY) * ppu);
                if (y >= 0 && y <= viewportHeight) canvas.DrawLine(0, y, viewportWidth, y, paint);
            }
        }
    }
}
