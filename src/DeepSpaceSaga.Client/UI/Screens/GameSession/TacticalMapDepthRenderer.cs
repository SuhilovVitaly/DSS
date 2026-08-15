using DeepSpaceSaga.Client.UI;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Client-only pseudo-3D presentation for tactical-map markers and prediction
/// trajectories. The lighting direction is fixed in screen space (upper-left),
/// so camera pan, zoom, and object heading never change the visual light source.
/// </summary>
internal sealed class TacticalMapDepthRenderer
{
    private const float LightOffsetX = -0.45f;
    private const float LightOffsetY = -0.45f;
    private const float TrajectoryShadowOffset = 1.25f;

    private readonly SKPaint _markerShadowPaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };
    private readonly SKPaint _markerBasePaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };
    private readonly SKPaint _markerShadePaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };
    private readonly SKPaint _markerHighlightPaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };
    private readonly SKPaint _markerRimPaint = new()
    {
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1f,
        IsAntialias = true
    };

    private readonly SKPaint _glintHaloPaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };
    private readonly SKPaint _glintCorePaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };

    private readonly SKPaint _selectionGlowPaint = new()
    {
        Color = new SKColor(220, 228, 236, 42),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 2f,
        StrokeCap = SKStrokeCap.Round,
        IsAntialias = true,
        MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 2.2f)
    };
    private readonly SKPaint _selectionPaint = new()
    {
        Color = new SKColor(226, 232, 238, 125),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 0.8f,
        StrokeCap = SKStrokeCap.Round,
        IsAntialias = true
    };

    /// <summary>
    /// Radius-multiplier/alpha pairs for the glint halo, largest and faintest
    /// first, so each subsequent layer paints a smaller and brighter ring on
    /// top of the previous one for a smooth continuous glow (no banding).
    /// </summary>
    private static readonly (float RadiusMultiplier, byte Alpha)[] GlintHaloLayers =
    [
        (2.6f, 12),
        (2.2f, 22),
        (1.8f, 38),
        (1.5f, 60),
        (1.25f, 90),
        (1.05f, 130)
    ];

    private readonly SKPaint _glyphShadowPaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };
    private readonly SKPaint _glyphBasePaint = new() { Style = SKPaintStyle.Fill, IsAntialias = true };
    private readonly SKPaint _glyphHighlightPaint = new()
    {
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1f,
        IsAntialias = true,
        StrokeJoin = SKStrokeJoin.Round
    };
    private readonly SKPaint _glyphRimPaint = new()
    {
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.25f,
        IsAntialias = true,
        StrokeJoin = SKStrokeJoin.Round
    };

    private readonly SKPath _trajectoryPath = new();
    private readonly TrajectoryPaintSet _futureTrajectoryPaints = new(
        edgeColor: new SKColor(24, 24, 24, 235),
        bodyColor: new SKColor(76, 76, 76, 220),
        highlightColor: new SKColor(158, 158, 158, 210));
    private readonly TrajectoryPaintSet _navigationTrajectoryPaints = new(
        edgeColor: new SKColor(32, 30, 26, 240),
        bodyColor: new SKColor(100, 92, 72, 230),
        highlightColor: new SKColor(198, 184, 142, 220));

    private readonly SKPaint _navigationTargetShadowPaint = new()
    {
        Color = new SKColor(30, 26, 18, 220),
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };
    private readonly SKPaint _navigationTargetPaint = new()
    {
        Color = new SKColor(198, 184, 142, 235),
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };

    public void DrawSphericalMarker(
        SKCanvas canvas,
        float centerX,
        float centerY,
        float radius,
        SKColor baseColor)
    {
        float depthOffset = Math.Clamp(radius * 0.14f, 0.75f, 2.5f);

        _markerShadowPaint.Color = ScaleColor(baseColor, 0.18f, 220);
        canvas.DrawCircle(centerX + depthOffset, centerY + depthOffset, radius, _markerShadowPaint);

        _markerBasePaint.Color = baseColor;
        canvas.DrawCircle(centerX, centerY, radius, _markerBasePaint);

        _markerShadePaint.Color = ScaleColor(baseColor, 0.38f, 185);
        canvas.DrawCircle(
            centerX + radius * 0.18f,
            centerY + radius * 0.18f,
            radius * 0.84f,
            _markerShadePaint);

        _markerHighlightPaint.Color = MixWithWhite(baseColor, 0.72f, 225);
        canvas.DrawCircle(
            centerX - radius * 0.28f,
            centerY - radius * 0.28f,
            Math.Max(0.9f, radius * 0.19f),
            _markerHighlightPaint);

        _markerRimPaint.Color = ScaleColor(baseColor, 0.28f, 235);
        canvas.DrawCircle(centerX, centerY, radius, _markerRimPaint);
    }

    /// <summary>
    /// Draws a soft glinting point of light (bright core + fading, symmetric
    /// halo) for distant/unresolved sensor contacts — unknown objects and
    /// asteroids. Unlike <see cref="DrawSphericalMarker"/>, every layer is
    /// centered on (centerX, centerY) with no offset, so the result is
    /// direction-symmetric: no directional lighting, streak, or ray.
    /// </summary>
    public void DrawGlintMarker(
        SKCanvas canvas,
        float centerX,
        float centerY,
        float radius,
        SKColor baseColor)
    {
        foreach ((float radiusMultiplier, byte alpha) in GlintHaloLayers)
        {
            _glintHaloPaint.Color = MixWithWhite(baseColor, 0.55f, alpha);
            canvas.DrawCircle(centerX, centerY, radius * radiusMultiplier, _glintHaloPaint);
        }

        _glintCorePaint.Color = MixWithWhite(baseColor, 0.8f, 250);
        canvas.DrawCircle(centerX, centerY, Math.Max(0.9f, radius * 0.45f), _glintCorePaint);
    }

    /// <summary>
    /// Draws the selected-object reticle in screen space: a faint circular ring
    /// with four short sight lines that approach it only from the outside. The
    /// lines stop at the ring and never cross the selected object.
    /// </summary>
    public void DrawSelectionReticle(
        SKCanvas canvas,
        float centerX,
        float centerY,
        float markerRadius)
    {
        DrawObjectReticle(canvas, centerX, centerY, markerRadius, new SKColor(226, 232, 238));
    }

    /// <summary>
    /// Draws the same soft reticle for the object currently under the pointer,
    /// tinted orange to distinguish the transient active state from selection.
    /// </summary>
    public void DrawActiveObjectReticle(
        SKCanvas canvas,
        float centerX,
        float centerY,
        float markerRadius)
    {
        DrawObjectReticle(canvas, centerX, centerY, markerRadius, new SKColor(255, 165, 0));
    }

    private void DrawObjectReticle(
        SKCanvas canvas,
        float centerX,
        float centerY,
        float markerRadius,
        SKColor color)
    {
        _selectionGlowPaint.Color = new SKColor(color.Red, color.Green, color.Blue, 42);
        _selectionPaint.Color = new SKColor(color.Red, color.Green, color.Blue, 125);

        float ringRadius = (markerRadius + 3.5f) * 2f;
        float crossExtent = ringRadius + 4f;

        DrawSelectionGeometry(canvas, centerX, centerY, ringRadius, crossExtent, _selectionGlowPaint);
        DrawSelectionGeometry(canvas, centerX, centerY, ringRadius, crossExtent, _selectionPaint);
    }

    public void DrawPlayerShipGlyph(
        SKCanvas canvas,
        SKPath glyphPath,
        float markerRadius,
        SKColor baseColor)
    {
        float depthOffset = Math.Clamp(markerRadius * 0.22f, 1f, 2f);

        _glyphShadowPaint.Color = ScaleColor(baseColor, 0.16f, 225);
        DrawTranslatedPath(canvas, glyphPath, depthOffset, depthOffset, _glyphShadowPaint);

        _glyphBasePaint.Color = baseColor;
        canvas.DrawPath(glyphPath, _glyphBasePaint);

        _glyphHighlightPaint.Color = MixWithWhite(baseColor, 0.58f, 210);
        DrawTranslatedPath(canvas, glyphPath, LightOffsetX, LightOffsetY, _glyphHighlightPaint);

        _glyphRimPaint.Color = ScaleColor(baseColor, 0.3f, 240);
        canvas.DrawPath(glyphPath, _glyphRimPaint);
    }

    public void DrawFutureTrajectory(
        SKCanvas canvas,
        IReadOnlyList<FutureTrajectoryPoint> points,
        CameraState camera,
        int viewportWidth,
        int viewportHeight)
    {
        DrawTrajectory(
            canvas,
            points,
            camera,
            viewportWidth,
            viewportHeight,
            _futureTrajectoryPaints);
    }

    public void DrawNavigationTrajectory(
        SKCanvas canvas,
        IReadOnlyList<FutureTrajectoryPoint> points,
        CameraState camera,
        int viewportWidth,
        int viewportHeight)
    {
        DrawTrajectory(
            canvas,
            points,
            camera,
            viewportWidth,
            viewportHeight,
            _navigationTrajectoryPaints);
    }

    public void DrawNavigationTarget(SKCanvas canvas, float centerX, float centerY)
    {
        canvas.DrawCircle(centerX + 1f, centerY + 1f, 2.25f, _navigationTargetShadowPaint);
        canvas.DrawCircle(centerX, centerY, 1.5f, _navigationTargetPaint);
    }

    private void DrawTrajectory(
        SKCanvas canvas,
        IReadOnlyList<FutureTrajectoryPoint> points,
        CameraState camera,
        int viewportWidth,
        int viewportHeight,
        TrajectoryPaintSet paints)
    {
        if (points.Count < 2)
            return;

        _trajectoryPath.Reset();
        var first = points[0];
        var (firstX, firstY) = camera.WorldToScreen(first.X, first.Y, viewportWidth, viewportHeight);
        _trajectoryPath.MoveTo(firstX, firstY);

        for (int i = 1; i < points.Count; i++)
        {
            var point = points[i];
            var (screenX, screenY) = camera.WorldToScreen(point.X, point.Y, viewportWidth, viewportHeight);
            _trajectoryPath.LineTo(screenX, screenY);
        }

        DrawTranslatedPath(
            canvas,
            _trajectoryPath,
            TrajectoryShadowOffset,
            TrajectoryShadowOffset,
            paints.Shadow);
        canvas.DrawPath(_trajectoryPath, paints.Edge);
        canvas.DrawPath(_trajectoryPath, paints.Body);
        DrawTranslatedPath(canvas, _trajectoryPath, LightOffsetX, LightOffsetY, paints.Highlight);
    }

    private static void DrawTranslatedPath(
        SKCanvas canvas,
        SKPath path,
        float offsetX,
        float offsetY,
        SKPaint paint)
    {
        canvas.Save();
        canvas.Translate(offsetX, offsetY);
        canvas.DrawPath(path, paint);
        canvas.Restore();
    }

    private static void DrawSelectionGeometry(
        SKCanvas canvas,
        float centerX,
        float centerY,
        float ringRadius,
        float crossExtent,
        SKPaint paint)
    {
        canvas.DrawLine(centerX - crossExtent, centerY, centerX - ringRadius, centerY, paint);
        canvas.DrawLine(centerX + ringRadius, centerY, centerX + crossExtent, centerY, paint);
        canvas.DrawLine(centerX, centerY - crossExtent, centerX, centerY - ringRadius, paint);
        canvas.DrawLine(centerX, centerY + ringRadius, centerX, centerY + crossExtent, paint);
        canvas.DrawCircle(centerX, centerY, ringRadius, paint);
    }

    private static SKColor ScaleColor(SKColor color, float scale, byte alpha)
    {
        return new SKColor(
            (byte)Math.Clamp((int)Math.Round(color.Red * scale), 0, 255),
            (byte)Math.Clamp((int)Math.Round(color.Green * scale), 0, 255),
            (byte)Math.Clamp((int)Math.Round(color.Blue * scale), 0, 255),
            alpha);
    }

    private static SKColor MixWithWhite(SKColor color, float whiteAmount, byte alpha)
    {
        float baseAmount = 1f - whiteAmount;
        return new SKColor(
            (byte)Math.Clamp((int)Math.Round(color.Red * baseAmount + 255 * whiteAmount), 0, 255),
            (byte)Math.Clamp((int)Math.Round(color.Green * baseAmount + 255 * whiteAmount), 0, 255),
            (byte)Math.Clamp((int)Math.Round(color.Blue * baseAmount + 255 * whiteAmount), 0, 255),
            alpha);
    }

    private sealed class TrajectoryPaintSet
    {
        public TrajectoryPaintSet(SKColor edgeColor, SKColor bodyColor, SKColor highlightColor)
        {
            Shadow = CreateStrokePaint(new SKColor(6, 6, 6, 235), 5.5f);
            Edge = CreateStrokePaint(edgeColor, 4f);
            Body = CreateStrokePaint(bodyColor, 2.25f);
            Highlight = CreateStrokePaint(highlightColor, 0.8f);
        }

        public SKPaint Shadow { get; }
        public SKPaint Edge { get; }
        public SKPaint Body { get; }
        public SKPaint Highlight { get; }

        private static SKPaint CreateStrokePaint(SKColor color, float strokeWidth)
        {
            return new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = strokeWidth,
                StrokeCap = SKStrokeCap.Round,
                StrokeJoin = SKStrokeJoin.Round,
                IsAntialias = true
            };
        }
    }
}
