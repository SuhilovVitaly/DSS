using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed class GameSessionScreen : IScreen
{
    private readonly SnapshotBuffer _buffer;
    private readonly IMotionPredictor _predictor;
    private readonly ClientGameClock _clock;
    private readonly SKPaint _backgroundPaint;
    private readonly SKPaint _objectPaint;
    private readonly SKPaint _centerPaint;

    public GameSessionScreen(SnapshotBuffer buffer, IMotionPredictor predictor, ClientGameClock clock)
    {
        _buffer = buffer;
        _predictor = predictor;
        _clock = clock;
        _backgroundPaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill };
        _objectPaint = new SKPaint { Color = SKColors.Cyan, Style = SKPaintStyle.Fill, IsAntialias = true };
        _centerPaint = new SKPaint { Color = new SKColor(40, 40, 40), Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
    }

    public void OnActivated() { }
    public void OnDeactivated() { }

    public ScreenEvent OnMouseDown(float x, float y) => ScreenEvent.None;
    public bool OnMouseMove(float x, float y) => false;

    public ScreenEvent OnKeyDown(Key key)
    {
        return key == Key.Escape ? ScreenEvent.OpenGameMenu : ScreenEvent.None;
    }

    public void Render(SKCanvas canvas, int width, int height)
    {
        canvas.DrawRect(0, 0, width, height, _backgroundPaint);

        // Center-based transform: Sun at screen center
        float cx = width / 2f;
        float cy = height / 2f;

        // Crosshair at center (Sun position)
        canvas.DrawLine(cx - 10, cy, cx + 10, cy, _centerPaint);
        canvas.DrawLine(cx, cy - 10, cx, cy + 10, _centerPaint);

        var snapshot = _buffer.Latest;
        if (snapshot is null)
            return;

        long predictionDelta = _clock.PredictionDeltaMs;

        foreach (var obj in snapshot.Objects)
        {
            var predicted = predictionDelta > 0
                ? _predictor.Predict(obj, predictionDelta)
                : obj;

            // Center-based: world coords map directly
            // DSS convention: 0°=up (negative Y), screen Y increases downward
            // So world -Y maps upward on screen naturally
            float sx = cx + (float)predicted.X;
            float sy = cy + (float)predicted.Y;

            canvas.DrawCircle(sx, sy, 4, _objectPaint);
        }
    }
}
