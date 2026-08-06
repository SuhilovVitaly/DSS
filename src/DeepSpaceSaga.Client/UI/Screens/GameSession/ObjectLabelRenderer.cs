using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Draws compact object labels on the tactical map:
/// leader line → dark plaque → bottom accent stripe → status square → text.
/// </summary>
internal sealed class ObjectLabelRenderer
{
    private const string UnknownLabel = "Unknown Celestial Object";

    private readonly SKPaint _leaderLinePaint;
    private readonly SKPaint _plaqueBgPaint;
    private readonly SKPaint _plaqueBorderPaint;
    private readonly SKPaint _textPaint;
    private readonly SKPaint _unknownTextPaint;
    private readonly SKPaint _statusSquarePaint;
    private readonly SKPaint _stripePaint;

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
            var predicted = renderStates[i].Predicted;
            var (objSx, objSy) = camera.WorldToScreen(predicted.X, predicted.Y, viewportW, viewportH);
            var objectScreen = new SKPoint(objSx, objSy);

            string label = predicted.DisplayName ?? UnknownLabel;
            bool isUnknown = predicted.DisplayName is null;
            float textWidth = (isUnknown ? _unknownTextPaint : _textPaint).MeasureText(label);
            var geometry = ObjectLabelLayout.Create(objectScreen, predicted.Direction, textWidth,
                new SKSize(viewportW, viewportH));

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
        long gameTimeMs,
        SimulationSpeed speed,
        int viewportW,
        int viewportH,
        CameraState camera)
    {
        for (int i = 0; i < renderStates.Count; i++)
        {
            var state = renderStates[i];
            var predicted = state.Predicted;
            var (objSx, objSy) = camera.WorldToScreen(predicted.X, predicted.Y, viewportW, viewportH);
            var objectScreen = new SKPoint(objSx, objSy);

            SKColor objectColor = state.IsPlayerShip
                ? SpaceMapColorResolver.PlayerShipColor
                : SpaceMapColorResolver.GetColor(predicted.ObjectType, predicted.RelationToPlayer);

            string label = predicted.DisplayName ?? UnknownLabel;
            bool isUnknown = predicted.DisplayName is null;
            float textWidth = (isUnknown ? _unknownTextPaint : _textPaint).MeasureText(label);
            var geometry = ObjectLabelLayout.Create(objectScreen, predicted.Direction, textWidth,
                new SKSize(viewportW, viewportH));

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

            // Status square — drawn only during the visible blink phase,
            // with the original object color (no brightness shift)
            if (StatusSquareAnimator.IsStatusSquareVisible(gameTimeMs, speed))
            {
                _statusSquarePaint.Color = objectColor;
                canvas.DrawRect(geometry.StatusRect, _statusSquarePaint);
            }

            // Text
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
