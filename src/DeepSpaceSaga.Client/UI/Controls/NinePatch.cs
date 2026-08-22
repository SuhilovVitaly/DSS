using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Controls;

/// <summary>
/// Nine-slice image stretching for small bordered panel textures (like
/// <c>micro-panel.png</c>), split into a 3×3 grid of source samples that get blitted
/// into an arbitrarily sized destination rect:
/// <list type="bullet">
/// <item>the four <paramref name="corner"/>×<paramref name="corner"/> corners are cut
/// straight from the source's own corners and drawn unscaled, so a rounded or
/// transparent corner keeps its exact shape at any panel size;</item>
/// <item>each edge is a <paramref name="corner"/>×<paramref name="corner"/> sample taken
/// from the middle of that edge (not the leftover strip between corners) — deliberately
/// a fixed clean sample rather than a proportional stretch, so any decoration sitting
/// near the corners (rivets, connector notches, etc.) is never itself stretched into a
/// smear when the panel grows longer than the source image;</item>
/// <item>the interior between the corners is stretched both ways to fill the center.</item>
/// </list>
/// This is how one small border texture becomes panels of any length and height without
/// redrawing the artwork per size — see <see cref="ScenarioSelect.ScenarioSelectScreen"/>.
/// </summary>
public static class NinePatch
{
    /// <summary>
    /// Draws <paramref name="source"/> into <paramref name="dest"/> using
    /// <paramref name="corner"/>×<paramref name="corner"/> source pixels for each corner
    /// and edge sample (clamped down if the source or destination is too small to fit
    /// two full corners along an axis). <paramref name="paint"/>, if given, is passed
    /// through to every blit — e.g. a paint whose Color.Alpha is less than 255 dims the
    /// whole draw uniformly (used for a disabled-looking button).
    /// </summary>
    public static void Draw(SKCanvas canvas, SKBitmap source, SKRect dest, float corner, SKPaint? paint = null)
    {
        float sw = source.Width;
        float sh = source.Height;

        float c = Math.Min(corner, Math.Min(sw, sh) / 2f);
        float dc = Math.Min(c, Math.Min(dest.Width, dest.Height) / 2f);
        if (c <= 0 || dc <= 0)
            return;

        float midX = sw / 2f;
        float midY = sh / 2f;
        float half = c / 2f;

        // Source samples: real corners, plus one fixed c×c sample from the middle of
        // each edge (never the variable-length leftover strip between corners).
        var srcTopLeft = new SKRect(0, 0, c, c);
        var srcTopRight = new SKRect(sw - c, 0, sw, c);
        var srcBottomLeft = new SKRect(0, sh - c, c, sh);
        var srcBottomRight = new SKRect(sw - c, sh - c, sw, sh);

        var srcTop = new SKRect(midX - half, 0, midX + half, c);
        var srcBottom = new SKRect(midX - half, sh - c, midX + half, sh);
        var srcLeft = new SKRect(0, midY - half, c, midY + half);
        var srcRight = new SKRect(sw - c, midY - half, sw, midY + half);

        var srcCenter = new SKRect(c, c, Math.Max(c, sw - c), Math.Max(c, sh - c));

        // Destination: corners at native (clamped) size in each of the four corners;
        // edges and center stretch to fill whatever space is left between them.
        float dLeft = dest.Left, dRight = dest.Right, dTop = dest.Top, dBottom = dest.Bottom;
        float dInnerLeft = dLeft + dc, dInnerRight = dRight - dc;
        float dInnerTop = dTop + dc, dInnerBottom = dBottom - dc;

        Blit(canvas, source, srcTopLeft, new SKRect(dLeft, dTop, dInnerLeft, dInnerTop), paint);
        Blit(canvas, source, srcTopRight, new SKRect(dInnerRight, dTop, dRight, dInnerTop), paint);
        Blit(canvas, source, srcBottomLeft, new SKRect(dLeft, dInnerBottom, dInnerLeft, dBottom), paint);
        Blit(canvas, source, srcBottomRight, new SKRect(dInnerRight, dInnerBottom, dRight, dBottom), paint);

        Blit(canvas, source, srcTop, new SKRect(dInnerLeft, dTop, dInnerRight, dInnerTop), paint);
        Blit(canvas, source, srcBottom, new SKRect(dInnerLeft, dInnerBottom, dInnerRight, dBottom), paint);
        Blit(canvas, source, srcLeft, new SKRect(dLeft, dInnerTop, dInnerLeft, dInnerBottom), paint);
        Blit(canvas, source, srcRight, new SKRect(dInnerRight, dInnerTop, dRight, dInnerBottom), paint);

        Blit(canvas, source, srcCenter, new SKRect(dInnerLeft, dInnerTop, dInnerRight, dInnerBottom), paint);
    }

    private static void Blit(SKCanvas canvas, SKBitmap source, SKRect src, SKRect dst, SKPaint? paint)
    {
        if (dst.Width <= 0 || dst.Height <= 0 || src.Width <= 0 || src.Height <= 0)
            return;

        canvas.DrawBitmap(source, src, dst, paint);
    }
}
