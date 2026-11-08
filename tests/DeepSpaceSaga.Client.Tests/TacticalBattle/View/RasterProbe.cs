using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle.View;

internal static class RasterProbe
{
    internal static double Fraction(SKBitmap bitmap, SKRect area, params SKColor[] colors)
    {
        int matches = 0, total = 0;
        for (int y = (int)Math.Ceiling(area.Top * 1.5); y < area.Bottom * 1.5; y++)
            for (int x = (int)Math.Ceiling(area.Left * 1.5); x < area.Right * 1.5; x++)
            {
                if (x < 0 || x >= bitmap.Width || y < 0 || y >= bitmap.Height) continue;
                var pixel = bitmap.GetPixel(x, y);
                if (colors.Any(c => Math.Abs(c.Red - pixel.Red) <= 40 && Math.Abs(c.Green - pixel.Green) <= 40 && Math.Abs(c.Blue - pixel.Blue) <= 40)) matches++;
                total++;
            }
        return total == 0 ? 0 : matches / (double)total;
    }
}
