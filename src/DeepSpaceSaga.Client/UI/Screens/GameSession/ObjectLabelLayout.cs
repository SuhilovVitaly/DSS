using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Geometry helper for object labels on the tactical map.
/// </summary>
internal static class ObjectLabelLayout
{
    public const float LabelOffsetPx = 45f;
    public const float MinPlaqueWidth = 120f;
    public const float PlaqueHeight = 18f;
    public const float StripeHeight = 3f;
    public const float StatusSquareSize = 8f;
    public const float TextPaddingX = 12f;
    public const float TextPaddingY = 3f;
    public const float StatusTextGap = 6f;

    /// <summary>
    /// Maps object direction to label angle.
    ///   0 ≤ dir ≤ 180 → 315°
    ///   180 &lt; dir ≤ 270 → 45°
    ///   270 &lt; dir &lt; 360 → 135°
    /// </summary>
    public static float GetLabelAngle(double directionDegrees)
    {
        double d = directionDegrees % 360;
        if (d < 0) d += 360;

        if (d >= 0 && d <= 180)
            return 315f;
        if (d > 180 && d <= 270)
            return 45f;
        // d > 270
        return 135f;
    }

    /// <summary>
    /// Compute the screen position for the label plaque given the object's
    /// screen position and the label angle.
    /// </summary>
    public static SKPoint ComputeLabelOrigin(SKPoint objectScreen, float angleDegrees)
    {
        double rad = angleDegrees * Math.PI / 180.0;
        float dx = (float)(Math.Cos(rad) * LabelOffsetPx);
        float dy = -(float)(Math.Sin(rad) * LabelOffsetPx);
        return new SKPoint(objectScreen.X + dx, objectScreen.Y + dy);
    }

    /// <summary>
    /// Clamp a point so the label stays within the viewport.
    /// </summary>
    public static SKPoint ClampToViewport(SKPoint origin, SKSize labelSize, SKSize viewport)
    {
        float x = Math.Clamp(origin.X, 2f, viewport.Width - labelSize.Width - 2f);
        float y = Math.Clamp(origin.Y, 2f, viewport.Height - labelSize.Height - 2f);
        return new SKPoint(x, y);
    }

    /// <summary>
    /// Compute the full label geometry for an object: plaque rectangle,
    /// leader-line endpoint, status-square rectangle, and text origin.
    /// </summary>
    public static ObjectLabelGeometry Create(
        SKPoint objectScreen,
        double directionDegrees,
        float textWidth,
        SKSize viewport)
    {
        float angle = GetLabelAngle(directionDegrees);

        float plaqueW = Math.Max(MinPlaqueWidth,
            TextPaddingX + StatusSquareSize + StatusTextGap + textWidth + TextPaddingX);
        float plaqueH = PlaqueHeight;
        var plaqueSize = new SKSize(plaqueW, plaqueH);

        SKPoint origin = ComputeLabelOrigin(objectScreen, angle);
        origin = ClampToViewport(origin, plaqueSize, viewport);

        var plaqueRect = new SKRect(origin.X, origin.Y,
            origin.X + plaqueW, origin.Y + plaqueH);

        // Leader line endpoint — nearest point on bottom edge (not always center)
        var leaderEndPoint = GetLeaderEndPoint(objectScreen, plaqueRect);

        // Status square — starts at TextPaddingX from left edge
        float sqX = plaqueRect.Left + TextPaddingX;
        float sqY = plaqueRect.Top + (plaqueH - StatusSquareSize) / 2f;
        var statusRect = new SKRect(sqX, sqY,
            sqX + StatusSquareSize, sqY + StatusSquareSize);

        // Text origin (Y is anchor; renderer adds TextSize for baseline)
        float textX = statusRect.Right + StatusTextGap;
        float textY = plaqueRect.Top + TextPaddingY;
        var textOrigin = new SKPoint(textX, textY);

        return new ObjectLabelGeometry(plaqueRect, leaderEndPoint, statusRect, textOrigin);
    }

    /// <summary>
    /// Compute the leader-line endpoint: the corner of the plaque
    /// (top-left, top-right, bottom-left or bottom-right) that is closest
    /// to the object's screen point by Euclidean distance.
    /// </summary>
    public static SKPoint GetLeaderEndPoint(SKPoint objectScreen, SKRect plaqueRect)
    {
        var topLeft = new SKPoint(plaqueRect.Left, plaqueRect.Top);
        var topRight = new SKPoint(plaqueRect.Right, plaqueRect.Top);
        var bottomLeft = new SKPoint(plaqueRect.Left, plaqueRect.Bottom);
        var bottomRight = new SKPoint(plaqueRect.Right, plaqueRect.Bottom);

        var nearest = topLeft;
        double bestDistanceSquared = DistanceSquared(objectScreen, topLeft);

        if (DistanceSquared(objectScreen, topRight) < bestDistanceSquared)
        {
            nearest = topRight;
            bestDistanceSquared = DistanceSquared(objectScreen, topRight);
        }

        if (DistanceSquared(objectScreen, bottomLeft) < bestDistanceSquared)
        {
            nearest = bottomLeft;
            bestDistanceSquared = DistanceSquared(objectScreen, bottomLeft);
        }

        if (DistanceSquared(objectScreen, bottomRight) < bestDistanceSquared)
        {
            nearest = bottomRight;
        }

        return nearest;
    }

    private static double DistanceSquared(SKPoint a, SKPoint b)
    {
        double dx = a.X - b.X;
        double dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
}
