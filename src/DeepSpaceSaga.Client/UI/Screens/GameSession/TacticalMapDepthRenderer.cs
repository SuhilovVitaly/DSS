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
    private const long ActiveReticleRotationPeriodMs = 3_500;
    private const long SelectedReticleRotationPeriodMs = 9_000;
    private const long FlamePulsePeriodMs = 180;
    private const float FocusIndicatorCornerRadius = 26f;
    private const float FocusIndicatorBracketArmLength = 9f;
    private const float FocusIndicatorTickGap = 4f;
    private const float FocusIndicatorTickLength = 14f;

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

    private readonly SKPaint _focusIndicatorPaint = new()
    {
        Color = new SKColor(75, 75, 75),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.5f,
        IsAntialias = true
    };
    private readonly SKPath _focusIndicatorPath = new();

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

    private readonly SKPaint _engineFlameOuterPaint = new()
    {
        Color = new SKColor(255, 90, 20, 140),
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };
    private readonly SKPaint _engineFlameInnerPaint = new()
    {
        Color = new SKColor(255, 220, 120, 200),
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };
    private readonly SKPath _engineFlamePath = new();

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

    private readonly SKPaint _interceptPointShadowPaint = new()
    {
        Color = new SKColor(18, 16, 12, 220),
        Style = SKPaintStyle.Fill,
        IsAntialias = true
    };
    private readonly SKPaint _interceptPointRingPaint = new()
    {
        Color = new SKColor(255, 150, 60, 170),
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1f,
        IsAntialias = true
    };
    private readonly SKPaint _interceptPointCorePaint = new()
    {
        Color = new SKColor(255, 150, 60, 235),
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
    /// Draws the camera-focus indicator: a tactical targeting reticle made of 4
    /// small L-shaped corner brackets (opening inward) plus 4 axis tick marks on
    /// the cardinal directions (sitting just outside the brackets, with a gap
    /// between them), marking the exact point the camera is focused on (always
    /// the viewport center). The exact center point is deliberately left
    /// unpainted — a large open area in the middle, unlike a solid crosshair.
    /// Drawn unconditionally, regardless of whether the camera is on a free map
    /// point or following an object.
    /// </summary>
    public void DrawFocusIndicator(SKCanvas canvas, float centerX, float centerY)
    {
        const float r = FocusIndicatorCornerRadius;
        const float arm = FocusIndicatorBracketArmLength;
        const float tickStart = FocusIndicatorCornerRadius + FocusIndicatorTickGap;
        const float tickEnd = tickStart + FocusIndicatorTickLength;

        _focusIndicatorPath.Reset();

        // Corner brackets: each pair of arms meets at its corner point and runs
        // toward the center along the two edges of the implied square.
        AddCornerBracket(_focusIndicatorPath, centerX - r, centerY - r, arm, arm);
        AddCornerBracket(_focusIndicatorPath, centerX + r, centerY - r, -arm, arm);
        AddCornerBracket(_focusIndicatorPath, centerX - r, centerY + r, arm, -arm);
        AddCornerBracket(_focusIndicatorPath, centerX + r, centerY + r, -arm, -arm);

        // Axis tick marks: short segments starting just outside the corner
        // radius (leaving a real gap) and extending further outward.
        _focusIndicatorPath.MoveTo(centerX, centerY - tickStart);
        _focusIndicatorPath.LineTo(centerX, centerY - tickEnd);
        _focusIndicatorPath.MoveTo(centerX, centerY + tickStart);
        _focusIndicatorPath.LineTo(centerX, centerY + tickEnd);
        _focusIndicatorPath.MoveTo(centerX - tickStart, centerY);
        _focusIndicatorPath.LineTo(centerX - tickEnd, centerY);
        _focusIndicatorPath.MoveTo(centerX + tickStart, centerY);
        _focusIndicatorPath.LineTo(centerX + tickEnd, centerY);

        canvas.DrawPath(_focusIndicatorPath, _focusIndicatorPaint);
    }

    /// <summary>
    /// Adds one L-shaped corner bracket to <paramref name="path"/>: two segments
    /// meeting at (cornerX, cornerY), one running horizontally by
    /// <paramref name="dx"/> and one running vertically by <paramref name="dy"/>
    /// — both pointing back toward the center (inward) from the corner.
    /// </summary>
    private static void AddCornerBracket(SKPath path, float cornerX, float cornerY, float dx, float dy)
    {
        path.MoveTo(cornerX + dx, cornerY);
        path.LineTo(cornerX, cornerY);
        path.LineTo(cornerX, cornerY + dy);
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
        float markerRadius,
        long uiTimeMs = 0)
    {
        DrawObjectReticle(canvas, centerX, centerY, markerRadius, new SKColor(226, 232, 238), GetSelectedReticleRotationDegrees(uiTimeMs));
    }

    /// <summary>
    /// Draws the same soft reticle for the object currently under the pointer,
    /// tinted orange to distinguish the transient active state from selection.
    /// </summary>
    public void DrawActiveObjectReticle(
        SKCanvas canvas,
        float centerX,
        float centerY,
        float markerRadius,
        long uiTimeMs = 0)
    {
        DrawObjectReticle(canvas, centerX, centerY, markerRadius, new SKColor(255, 165, 0), GetActiveReticleRotationDegrees(uiTimeMs));
    }

    private void DrawObjectReticle(
        SKCanvas canvas,
        float centerX,
        float centerY,
        float markerRadius,
        SKColor color,
        float rotationDegrees)
    {
        _selectionGlowPaint.Color = new SKColor(color.Red, color.Green, color.Blue, 42);
        _selectionPaint.Color = new SKColor(color.Red, color.Green, color.Blue, 125);

        float ringRadius = (markerRadius + 3.5f) * 2f;
        float crossExtent = ringRadius + 4f;

        DrawReticleGeometry(canvas, centerX, centerY, ringRadius, crossExtent, rotationDegrees, _selectionGlowPaint);
        DrawReticleGeometry(canvas, centerX, centerY, ringRadius, crossExtent, rotationDegrees, _selectionPaint);
    }

    internal static float GetActiveReticleRotationDegrees(long uiTimeMs)
    {
        return GetReticleRotationDegrees(uiTimeMs, ActiveReticleRotationPeriodMs, 1f);
    }

    internal static float GetSelectedReticleRotationDegrees(long uiTimeMs)
    {
        return GetReticleRotationDegrees(uiTimeMs, SelectedReticleRotationPeriodMs, -1f);
    }

    private static float GetReticleRotationDegrees(
        long uiTimeMs,
        long rotationPeriodMs,
        float direction)
    {
        long normalizedTimeMs = Math.Max(0L, uiTimeMs) % rotationPeriodMs;
        return direction * normalizedTimeMs * 360f / rotationPeriodMs;
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

    /// <summary>
    /// Draws an animated engine-thrust flame behind the player ship, visible
    /// only while the engine is actively accelerating. Flat-fill (no shaders
    /// or blur), directional along the ship's heading, with a deterministic
    /// sine-based flicker keyed to <paramref name="uiTimeMs"/> — mirrors the
    /// same normalization idiom as the reticle rotation animation. Must be
    /// drawn before (behind) <see cref="DrawPlayerShipGlyph"/> so the ship's
    /// own triangle sits on top of the flame's base.
    /// </summary>
    public void DrawEngineFlame(
        SKCanvas canvas,
        float shipX,
        float shipY,
        double directionDegrees,
        float radius,
        long uiTimeMs)
    {
        double radians = directionDegrees * Math.PI / 180.0;
        float dx = (float)Math.Sin(radians);
        float dy = -(float)Math.Cos(radians);
        float rx = dy;
        float ry = -dx;

        long normalizedTimeMs = Math.Max(0L, uiTimeMs) % FlamePulsePeriodMs;
        // Wide swing (0.6..1.0, a 40% range) — at the marker's real ~5px radius, a
        // subtle flicker (e.g. the original 0.75..1.0) is smoothed away by
        // antialiasing and reads as static rather than animated.
        float flicker = 0.8f + 0.2f * (float)Math.Sin(2.0 * Math.PI * normalizedTimeMs / FlamePulsePeriodMs);

        float baseX = shipX - dx * radius * 0.7f;
        float baseY = shipY - dy * radius * 0.7f;

        // Base length extends well past the ship glyph itself (radius*2.2/1.5 vs.
        // the glyph's own radius*1.0 nose) so the flame reads as a distinct shape
        // rather than a sliver mostly hidden behind the ship at small marker sizes.
        // Half-width exceeds the ship glyph's own back-edge half-width (radius*5/7
        // ≈ radius*0.714) so the flame visibly flares past the ship's silhouette
        // on both sides instead of being fully covered by its opaque fill.
        DrawFlameLayer(canvas, baseX, baseY, dx, dy, rx, ry, radius * 0.9f, radius * 2.2f * flicker, _engineFlameOuterPaint);
        DrawFlameLayer(canvas, baseX, baseY, dx, dy, rx, ry, radius * 0.55f, radius * 1.5f * flicker, _engineFlameInnerPaint);
    }

    private void DrawFlameLayer(
        SKCanvas canvas,
        float baseX,
        float baseY,
        float dx,
        float dy,
        float rx,
        float ry,
        float halfWidth,
        float length,
        SKPaint paint)
    {
        var left = new SKPoint(baseX - rx * halfWidth, baseY - ry * halfWidth);
        var right = new SKPoint(baseX + rx * halfWidth, baseY + ry * halfWidth);
        var tip = new SKPoint(baseX - dx * length, baseY - dy * length);

        _engineFlamePath.Reset();
        _engineFlamePath.MoveTo(left);
        _engineFlamePath.LineTo(right);
        _engineFlamePath.LineTo(tip);
        _engineFlamePath.Close();

        canvas.DrawPath(_engineFlamePath, paint);
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

    /// <summary>
    /// Marks the rendezvous point of a confirmed intercept-solve fly-through curve
    /// (story-20260829-210641.md) — the pose the ship's Dubins curve was built to
    /// arrive at exactly, distinct from <see cref="DrawNavigationTarget"/>'s live
    /// (continuously re-baked) aim point, which drifts away from the curve's actual
    /// destination while a multi-cycle curve is still being flown.
    /// </summary>
    public void DrawInterceptPoint(SKCanvas canvas, float centerX, float centerY)
    {
        canvas.DrawCircle(centerX + 1f, centerY + 1f, 3.5f, _interceptPointShadowPaint);
        canvas.DrawCircle(centerX, centerY, 3.5f, _interceptPointRingPaint);
        canvas.DrawCircle(centerX, centerY, 1.75f, _interceptPointCorePaint);
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

    private static void DrawReticleGeometry(
        SKCanvas canvas,
        float centerX,
        float centerY,
        float ringRadius,
        float crossExtent,
        float rotationDegrees,
        SKPaint paint)
    {
        double startRadians = rotationDegrees * Math.PI / 180.0;

        for (int arm = 0; arm < 4; arm++)
        {
            double angleRadians = startRadians + arm * Math.PI / 2.0;
            float dx = (float)Math.Cos(angleRadians);
            float dy = (float)Math.Sin(angleRadians);

            canvas.DrawLine(centerX + dx * crossExtent, centerY + dy * crossExtent, centerX + dx * ringRadius, centerY + dy * ringRadius, paint);
        }

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
