using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Geometry helper for object labels on the tactical map.
/// Places the label plaque on an orbit ring behind the object
/// relative to its direction of motion, with smooth continuous
/// positioning — no hard sector thresholds.
/// </summary>
internal static class ObjectLabelLayout
{
    public const float MinPlaqueWidth = 120f;
    public const float PlaqueHeight = 18f;
    public const float StripeHeight = 3f;
    public const float StatusSquareSize = 8f;
    public const float TextPaddingX = 12f;
    public const float TextPaddingY = 3f;
    public const float StatusTextGap = 6f;

    /// <summary>Horizontal offset applied to status square and text inside the plaque.</summary>
    public const float ContentOffsetX = -8f;

    /// <summary>Vertical offset applied to status square and text inside the plaque.</summary>
    public const float ContentOffsetY = -3f;

    /// <summary>Default marker radius for non-player objects (circle).</summary>
    public const float DefaultMarkerRadius = 4f;

    /// <summary>Extra margin beyond marker edge before plaque may sit.</summary>
    public const float SafeMarginPx = 8f;

    /// <summary>Extra distance from safe-area edge to plaque center.</summary>
    private const float OrbitExtraPx = 6f;

    /// <summary>Viewport margin for clamp.</summary>
    private const float ViewportMargin = 2f;

    /// <summary>
    /// Compute the full label geometry for an object.
    /// Plaque is placed behind the object relative to its direction of motion,
    /// then clamped to the viewport while preserving the rear-half-plane constraint
    /// whenever possible.
    /// </summary>
    public static ObjectLabelGeometry Create(
        SKPoint objectScreen,
        double directionDegrees,
        float textWidth,
        SKSize viewport,
        float markerRadius = DefaultMarkerRadius)
    {
        float plaqueW = Math.Max(MinPlaqueWidth,
            TextPaddingX + StatusSquareSize + StatusTextGap + textWidth + TextPaddingX);
        float plaqueH = PlaqueHeight;
        var plaqueSize = new SKSize(plaqueW, plaqueH);

        float safeRadius = markerRadius + SafeMarginPx;

        // Target plaque center — behind the object on the orbit ring.
        SKPoint targetCenter = ComputePlaqueCenter(objectScreen, directionDegrees, plaqueSize, safeRadius);

        // Plaque rect at target position.
        var plaqueRect = RectFromCenter(targetCenter, plaqueW, plaqueH);

        // Clamp into viewport, trying to stay in the rear half-plane.
        plaqueRect = ClampWithRearConstraint(plaqueRect, objectScreen, directionDegrees, viewport);

        // Leader line endpoint — always bottom-left corner of the plaque.
        var leaderEndPoint = new SKPoint(plaqueRect.Left, plaqueRect.Bottom);

        // Status square — with content offset applied.
        float sqX = plaqueRect.Left + TextPaddingX + ContentOffsetX;
        float sqY = plaqueRect.Top + (plaqueH - StatusSquareSize) / 2f + ContentOffsetY;
        var statusRect = new SKRect(sqX, sqY,
            sqX + StatusSquareSize, sqY + StatusSquareSize);

        // Text origin — with same content offset.
        float textX = statusRect.Right + StatusTextGap;
        float textY = plaqueRect.Top + TextPaddingY + ContentOffsetY;
        var textOrigin = new SKPoint(textX, textY);

        // Plaque center (for smoothing interpolation by renderer).
        var plaqueCenter = new SKPoint(plaqueRect.MidX, plaqueRect.MidY);

        return new ObjectLabelGeometry(plaqueRect, leaderEndPoint, statusRect, textOrigin, plaqueCenter);
    }

    /// <summary>
    /// Compute the target plaque center on the orbit ring behind the object.
    /// Direction 0° = up, 90° = right (clockwise). The rear vector points
    /// opposite to the forward direction.
    /// Uses a circular orbit whose radius is based on the plaque half-diagonal
    /// so the plaque cannot intersect the safe area at any angle.
    /// </summary>
    public static SKPoint ComputePlaqueCenter(
        SKPoint objectScreen,
        double directionDegrees,
        SKSize plaqueSize,
        float safeRadius)
    {
        double rad = directionDegrees * Math.PI / 180.0;

        // Forward vector in screen coordinates.
        float fx = (float)Math.Sin(rad);
        float fy = -(float)Math.Cos(rad);

        // Rear vector = opposite of forward.
        float rx = -fx;
        float ry = -fy;

        // Half-diagonal of the plaque — maximum distance from its center
        // to any point on its perimeter. Using this with a circular orbit
        // guarantees the safe area is respected at all angles.
        float halfDiag = MathF.Sqrt(
            plaqueSize.Width * plaqueSize.Width / 4f +
            plaqueSize.Height * plaqueSize.Height / 4f);
        float orbitRadius = halfDiag + safeRadius + OrbitExtraPx;

        return new SKPoint(
            objectScreen.X + rx * orbitRadius,
            objectScreen.Y + ry * orbitRadius);
    }

    /// <summary>
    /// Build an SKRect from a center point and dimensions.
    /// </summary>
    private static SKRect RectFromCenter(SKPoint center, float width, float height)
    {
        float halfW = width / 2f;
        float halfH = height / 2f;
        return new SKRect(center.X - halfW, center.Y - halfH,
            center.X + halfW, center.Y + halfH);
    }

    /// <summary>
    /// Clamp the plaque rect into the viewport. After clamping,
    /// if the plaque ended up in the forward half-plane (wrong side
    /// of the object), nudge it back to the nearest valid rear position.
    /// </summary>
    private static SKRect ClampWithRearConstraint(
        SKRect plaque,
        SKPoint objectScreen,
        double directionDegrees,
        SKSize viewport)
    {
        float pw = plaque.Width;
        float ph = plaque.Height;

        float maxCenterX = viewport.Width - ViewportMargin - pw / 2f;
        float maxCenterY = viewport.Height - ViewportMargin - ph / 2f;
        float minCenterX = ViewportMargin + pw / 2f;
        float minCenterY = ViewportMargin + ph / 2f;

        double rad = directionDegrees * Math.PI / 180.0;
        float fx = (float)Math.Sin(rad);
        float fy = -(float)Math.Cos(rad);
        float rx = -fx;
        float ry = -fy;

        float cx = plaque.MidX;
        float cy = plaque.MidY;

        // 1. Clamp the ideal position.
        cx = Math.Clamp(cx, minCenterX, maxCenterX);
        cy = Math.Clamp(cy, minCenterY, maxCenterY);

        // 2. Check if clamped position is behind the object.
        float dx = cx - objectScreen.X;
        float dy = cy - objectScreen.Y;
        float dotForward = dx * fx + dy * fy;

        if (dotForward <= 0)
            return RectFromCenter(new SKPoint(cx, cy), pw, ph);

        // 3. Clamped into forward half-plane — try rear ray intersection
        //    with the viewport boundary to find a behind-the-object position.
        //    Parameter t along rear ray: P(t) = objectScreen + rear * t.
        bool found = TryIntersectRearWithViewport(
            objectScreen, rx, ry,
            minCenterX, minCenterY, maxCenterX, maxCenterY,
            out float bestCx, out float bestCy);

        if (found)
        {
            float bdx = bestCx - objectScreen.X;
            float bdy = bestCy - objectScreen.Y;
            if (bdx * fx + bdy * fy <= 0)
                return RectFromCenter(new SKPoint(bestCx, bestCy), pw, ph);
        }

        // 4. Fallback: use the viewport corner farthest in the rear direction.
        float cornerDot = float.MinValue;
        float fallbackCx = cx;
        float fallbackCy = cy;
        TestCorner(minCenterX, minCenterY, objectScreen, rx, ry, ref fallbackCx, ref fallbackCy, ref cornerDot);
        TestCorner(maxCenterX, minCenterY, objectScreen, rx, ry, ref fallbackCx, ref fallbackCy, ref cornerDot);
        TestCorner(minCenterX, maxCenterY, objectScreen, rx, ry, ref fallbackCx, ref fallbackCy, ref cornerDot);
        TestCorner(maxCenterX, maxCenterY, objectScreen, rx, ry, ref fallbackCx, ref fallbackCy, ref cornerDot);

        return RectFromCenter(new SKPoint(fallbackCx, fallbackCy), pw, ph);
    }

    /// <summary>
    /// Find the intersection of the rear ray with the viewport boundary
    /// that is farthest from the object (i.e. best rear position).
    /// </summary>
    private static bool TryIntersectRearWithViewport(
        SKPoint origin, float rx, float ry,
        float minX, float minY, float maxX, float maxY,
        out float cx, out float cy)
    {
        cx = cy = 0;
        float bestT = float.MinValue;

        // Intersect with each viewport edge: x=minX, x=maxX, y=minY, y=maxY.
        TryIntersect(origin, rx, ry, minX, minY, maxX, maxY, 1, 0, minX, ref cx, ref cy, ref bestT);
        TryIntersect(origin, rx, ry, minX, minY, maxX, maxY, 1, 0, maxX, ref cx, ref cy, ref bestT);
        TryIntersect(origin, rx, ry, minX, minY, maxX, maxY, 0, 1, minY, ref cx, ref cy, ref bestT);
        TryIntersect(origin, rx, ry, minX, minY, maxX, maxY, 0, 1, maxY, ref cx, ref cy, ref bestT);

        return bestT > 0;
    }

    /// <summary>
    /// Intersect rear ray with a line: nx*x + ny*y = c.
    /// Ray: (origin.X + rx*t, origin.Y + ry*t). Only t > 0 is valid (rear direction).
    /// If the intersection is within the viewport bounds and farther than previous best, use it.
    /// </summary>
    private static void TryIntersect(
        SKPoint origin, float rx, float ry,
        float minX, float minY, float maxX, float maxY,
        float nx, float ny, float c,
        ref float bestCx, ref float bestCy, ref float bestT)
    {
        float denom = nx * rx + ny * ry;
        if (Math.Abs(denom) < 1e-9f)
            return; // parallel

        // Ray: P = origin + rear*t. Line: nx*x + ny*y = c.
        // Substituting: t = (c - nx*origin.X - ny*origin.Y) / denom.
        float t = (c - nx * origin.X - ny * origin.Y) / denom;
        if (t <= 0)
            return;

        float ix = origin.X + rx * t;
        float iy = origin.Y + ry * t;

        // Check within viewport bounds (with small tolerance).
        if (ix < minX - 0.1f || ix > maxX + 0.1f || iy < minY - 0.1f || iy > maxY + 0.1f)
            return;

        if (t > bestT)
        {
            bestT = t;
            bestCx = Math.Clamp(ix, minX, maxX);
            bestCy = Math.Clamp(iy, minY, maxY);
        }
    }

    private static void TestCorner(
        float cx, float cy,
        SKPoint origin, float rx, float ry,
        ref float bestCx, ref float bestCy, ref float bestDot)
    {
        float dx = cx - origin.X;
        float dy = cy - origin.Y;
        float dot = dx * rx + dy * ry; // dot with rear = how far behind
        if (dot > bestDot)
        {
            bestDot = dot;
            bestCx = cx;
            bestCy = cy;
        }
    }

}
