using SkiaSharp;

namespace DeepSpaceSaga.Client.UI;

/// <summary>World-aligned 1/2/5 grid at any scale, with bounded work and continuous opacity.</summary>
public sealed class GridRenderer
{
    private readonly TacticalMapSettings _settings;
    private readonly SKPaint _paint = new() { Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = true };
    public GridRenderer(TacticalMapSettings? settings = null) => _settings = (settings ?? new()).Validate();
    public static double ComputeFirstWorldLine(double worldBound, double worldStep) => Math.Floor(worldBound / worldStep) * worldStep;

    public static IReadOnlyList<double> GetEligibleLevels(double pixelsPerWorldUnit, TacticalMapSettings? settings = null)
    {
        var s = settings ?? new TacticalMapSettings();
        if (!double.IsFinite(pixelsPerWorldUnit) || pixelsPerWorldUnit <= 0) return Array.Empty<double>();
        double power = Math.Pow(10, Math.Floor(Math.Log10(s.GridMinimumPixels / pixelsPerWorldUnit)));
        var levels = new List<double>(6);
        for (int decade = 0; decade < 3; decade++, power *= 10)
            foreach (double m in s.GridMantissas)
            {
                double step = power * m, pixels = step * pixelsPerWorldUnit;
                if (pixels >= s.GridMinimumPixels && pixels <= s.GridMinimumPixels * 20) levels.Add(step);
            }
        return levels;
    }

    public static double LevelOpacity(double step, double ppu, TacticalMapSettings settings)
    {
        double pixels = step * ppu;
        double fadeIn = Math.Clamp((pixels - settings.GridMinimumPixels) / settings.GridFadePixels, 0, 1);
        double fadeOut = Math.Clamp((settings.GridMinimumPixels * 20 - pixels) / (settings.GridMinimumPixels * 5), 0, 1);
        return fadeIn * fadeOut;
    }

    public void Draw(SKCanvas canvas, CameraState camera, int viewportWidth, int viewportHeight)
    {
        canvas.Clear(SKColors.Black);
        if (viewportWidth <= 0 || viewportHeight <= 0) return;
        var (left, top) = camera.ScreenToWorld(0, 0, viewportWidth, viewportHeight);
        var (right, bottom) = camera.ScreenToWorld(viewportWidth, viewportHeight, viewportWidth, viewportHeight);
        foreach (double step in GetEligibleLevels(camera.PixelsPerWorldUnit, _settings))
        {
            _paint.Color = new SKColor(32, 32, 32, (byte)(255 * LevelOpacity(step, camera.PixelsPerWorldUnit, _settings)));
            double startX = ComputeFirstWorldLine(left, step), startY = ComputeFirstWorldLine(top, step);
            int nx = Math.Min(4096, (int)Math.Ceiling((right - left) / step) + 2);
            int ny = Math.Min(4096, (int)Math.Ceiling((bottom - top) / step) + 2);
            for (int i = 0; i < nx; i++)
            {
                var (x, _) = camera.WorldToScreen(startX + i * step, 0, viewportWidth, viewportHeight);
                canvas.DrawLine(x, 0, x, viewportHeight, _paint);
            }
            for (int i = 0; i < ny; i++)
            {
                var (_, y) = camera.WorldToScreen(0, startY + i * step, viewportWidth, viewportHeight);
                canvas.DrawLine(0, y, viewportWidth, y, _paint);
            }
        }
        _paint.Color = new SKColor(44, 44, 44);
        var origin = camera.WorldToScreen(0, 0, viewportWidth, viewportHeight);
        if (left <= 0 && right >= 0) canvas.DrawLine(origin.X, 0, origin.X, viewportHeight, _paint);
        if (top <= 0 && bottom >= 0) canvas.DrawLine(0, origin.Y, viewportWidth, origin.Y, _paint);
    }
}
