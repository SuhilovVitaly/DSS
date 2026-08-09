using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Draws compact object labels on the tactical map:
/// leader line → dark plaque → bottom accent stripe → status square → text.
/// Uses orbit-based layout and per-object smoothing for plaque position.
/// </summary>
internal sealed class ObjectLabelRenderer
{
    private readonly SKPaint _leaderLinePaint;
    private readonly SKPaint _plaqueBgPaint;
    private readonly SKPaint _plaqueBorderPaint;
    private readonly SKPaint _textPaint;
    private readonly SKPaint _unknownTextPaint;
    private readonly SKPaint _statusSquarePaint;
    private readonly SKPaint _stripePaint;

    /// <summary>Per-object smoothed visible positions.</summary>
    private readonly ObjectLabelSmoother _smoother = new();

    /// <summary>
    /// Geometries computed during the last <see cref="ComputeGeometries"/> call,
    /// keyed by object ID — reused between leader and plaque passes so both see
    /// the same smoothed position.
    /// </summary>
    private readonly Dictionary<string, ObjectLabelGeometry> _geometries = new(StringComparer.Ordinal);

    /// <summary>Active object IDs from the current frame.</summary>
    private readonly HashSet<string> _activeIds = new(StringComparer.Ordinal);

    public ObjectLabelRenderer()
    {
        var typeface = SKTypeface.FromFamilyName("Consolas") ?? SKTypeface.Default;

        _leaderLinePaint = new SKPaint
        {
            Color = new SKColor(32, 32, 32),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            IsAntialias = true
        };

        _plaqueBgPaint = new SKPaint
        {
            Color = new SKColor(22, 22, 22),
            Style = SKPaintStyle.Fill
        };

        _plaqueBorderPaint = new SKPaint
        {
            Color = new SKColor(32, 32, 32),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f
        };

        _textPaint = new SKPaint
        {
            Color = new SKColor(200, 200, 200),
            TextSize = 12f,
            IsAntialias = true,
            Typeface = typeface
        };

        _unknownTextPaint = new SKPaint
        {
            Color = new SKColor(136, 136, 136),
            TextSize = 12f,
            IsAntialias = true,
            Typeface = typeface
        };

        _statusSquarePaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        _stripePaint = new SKPaint
        {
            Style = SKPaintStyle.Fill
        };
    }

    /// <summary>
    /// Compute smoothed label geometries for all visible objects.
    /// Must be called once per frame before DrawLeaders/DrawPlaques.
    /// </summary>
    public void ComputeGeometries(
        IReadOnlyList<ObjectRenderState> renderStates,
        double deltaSeconds,
        int viewportW,
        int viewportH,
        CameraState camera,
        bool resetSmoothing = false)
    {
        _geometries.Clear();
        _activeIds.Clear();

        if (resetSmoothing)
            _smoother.ResetAll();

        var viewport = new SKSize(viewportW, viewportH);

        for (int i = 0; i < renderStates.Count; i++)
        {
            var state = renderStates[i];
            var predicted = state.Predicted;
            string objectId = predicted.ObjectId;

            var (objSx, objSy) = camera.WorldToScreen(predicted.X, predicted.Y, viewportW, viewportH);

            // Visibility filter: skip objects whose marker/glyph is fully outside viewport.
            // Marker radius from the shared policy (player ship included) so the
            // viewport culling matches the drawn marker size.
            float markerRadius = TacticalMapMarkerPolicy.GetMarkerRadiusPx(
                state.IsPlayerShip ? SpaceObjectType.PlayerShip : predicted.RenderObjectType);
            if (objSx < -markerRadius || objSx > viewportW + markerRadius ||
                objSy < -markerRadius || objSy > viewportH + markerRadius)
                continue;

            _activeIds.Add(objectId);
            var objectScreen = new SKPoint(objSx, objSy);

            string label = ObjectLabelText.Build(predicted.RenderObjectType, predicted.DisplayName, predicted.ObjectId);
            bool isUnknown = predicted.RenderObjectType == SpaceObjectType.UnknownSpaceObject;
            float textWidth = (isUnknown ? _unknownTextPaint : _textPaint).MeasureText(label);

            // Target geometry from orbit layout (no smoothing).
            var targetGeom = ObjectLabelLayout.Create(objectScreen, predicted.Direction, textWidth,
                viewport, markerRadius);

            // Apply smoothing to get the visible plaque position.
            SKRect visiblePlaque = _smoother.Update(
                objectId,
                targetGeom.PlaqueRect,
                targetGeom.PlaqueCenter,
                deltaSeconds,
                viewportW,
                viewportH,
                reset: resetSmoothing);

            // Leader endpoint — always bottom-left corner of the visible plaque.
            var leaderEndPoint = new SKPoint(visiblePlaque.Left, visiblePlaque.Bottom);

            // Recompute status rect and text origin relative to the visible plaque.
            float sqX = visiblePlaque.Left + ObjectLabelLayout.TextPaddingX + ObjectLabelLayout.ContentOffsetX;
            float sqY = visiblePlaque.Top + (visiblePlaque.Height - ObjectLabelLayout.StatusSquareSize) / 2f
                        + ObjectLabelLayout.StatusOffsetY;
            var statusRect = new SKRect(sqX, sqY,
                sqX + ObjectLabelLayout.StatusSquareSize, sqY + ObjectLabelLayout.StatusSquareSize);

            float textX = statusRect.Right + ObjectLabelLayout.StatusTextGap;
            float textY = visiblePlaque.Top + ObjectLabelLayout.TextPaddingY + ObjectLabelLayout.TextOffsetY;

            _geometries[objectId] = new ObjectLabelGeometry(
                visiblePlaque, leaderEndPoint, statusRect, new SKPoint(textX, textY),
                targetGeom.PlaqueCenter);
        }

        _smoother.RemoveStaleExcept(_activeIds);
    }

    /// <summary>
    /// Draw leader lines only — called BEFORE object glyphs so lines go behind ships.
    /// </summary>
    public void DrawLeaders(
        SKCanvas canvas,
        IReadOnlyList<ObjectRenderState> renderStates,
        int viewportW,
        int viewportH,
        CameraState camera)
    {
        for (int i = 0; i < renderStates.Count; i++)
        {
            string objectId = renderStates[i].Predicted.ObjectId;
            if (!_geometries.TryGetValue(objectId, out var geometry))
                continue;

            var predicted = renderStates[i].Predicted;
            var (objSx, objSy) = camera.WorldToScreen(predicted.X, predicted.Y, viewportW, viewportH);

            canvas.DrawLine(objSx, objSy,
                geometry.LeaderEndPoint.X, geometry.LeaderEndPoint.Y,
                _leaderLinePaint);
        }
    }

    /// <summary>
    /// Draw plaque, stripe, status square and text — called AFTER object glyphs
    /// so the plaque UI sits on top.
    /// </summary>
    public void DrawPlaques(
        SKCanvas canvas,
        IReadOnlyList<ObjectRenderState> renderStates,
        long uiTimeMs,
        SimulationSpeed speed,
        int viewportW,
        int viewportH,
        CameraState camera)
    {
        for (int i = 0; i < renderStates.Count; i++)
        {
            var state = renderStates[i];
            string objectId = state.Predicted.ObjectId;
            if (!_geometries.TryGetValue(objectId, out var geometry))
                continue;

            var predicted = state.Predicted;

            SKColor objectColor = state.IsPlayerShip
                ? SpaceMapColorResolver.PlayerShipColor
                : SpaceMapColorResolver.GetColor(predicted.RenderObjectType, predicted.RelationToPlayer);

            // Plaque background + border
            canvas.DrawRect(geometry.PlaqueRect, _plaqueBgPaint);
            canvas.DrawRect(geometry.PlaqueRect, _plaqueBorderPaint);

            // Bottom accent stripe
            var stripeRect = new SKRect(
                geometry.PlaqueRect.Left,
                geometry.PlaqueRect.Bottom - ObjectLabelLayout.StripeHeight,
                geometry.PlaqueRect.Right,
                geometry.PlaqueRect.Bottom);

            byte sr = (byte)((objectColor.Red + 52) / 3);
            byte sg = (byte)((objectColor.Green + 52) / 3);
            byte sb = (byte)((objectColor.Blue + 52) / 3);
            _stripePaint.Color = new SKColor(sr, sg, sb);
            canvas.DrawRect(stripeRect, _stripePaint);

            // Status square — blink driven by real/UI time, not game time
            if (StatusSquareAnimator.IsStatusSquareVisible(uiTimeMs, speed))
            {
                _statusSquarePaint.Color = objectColor;
                canvas.DrawRect(geometry.StatusRect, _statusSquarePaint);
            }

            // Text
            string label = ObjectLabelText.Build(predicted.RenderObjectType, predicted.DisplayName, predicted.ObjectId);
            bool isUnknown = predicted.RenderObjectType == SpaceObjectType.UnknownSpaceObject;
            var textPaint = isUnknown ? _unknownTextPaint : _textPaint;
            if (!isUnknown)
            {
                textPaint.Color = new SKColor(
                    (byte)Math.Min(255, objectColor.Red + 60),
                    (byte)Math.Min(255, objectColor.Green + 60),
                    (byte)Math.Min(255, objectColor.Blue + 60));
            }
            float textY = geometry.TextOrigin.Y + textPaint.TextSize;
            canvas.DrawText(label, geometry.TextOrigin.X, textY, textPaint);
        }
    }
}
