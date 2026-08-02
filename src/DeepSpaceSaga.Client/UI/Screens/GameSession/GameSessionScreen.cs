using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed class GameSessionScreen : IScreen
{
    private readonly SnapshotBuffer _buffer;
    private readonly IMotionPredictor _predictor;
    private readonly SKPaint _backgroundPaint;
    private readonly SKPaint _objectPaint;

    public GameSessionScreen(SnapshotBuffer buffer, IMotionPredictor predictor)
    {
        _buffer = buffer;
        _predictor = predictor;
        _backgroundPaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill };
        _objectPaint = new SKPaint { Color = SKColors.Cyan, Style = SKPaintStyle.Fill, IsAntialias = true };
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

        var snapshot = _buffer.Latest;
        if (snapshot is null)
            return;

        // Predict from snapshot time to now
        long nowMs = Environment.TickCount64;
        long elapsedMs = nowMs - snapshot.GameTimeMs;

        foreach (var obj in snapshot.Objects)
        {
            var predicted = elapsedMs > 0
                ? _predictor.Predict(obj, elapsedMs)
                : obj;

            float sx = (float)predicted.X;
            float sy = height - (float)predicted.Y; // flip Y for screen

            canvas.DrawCircle(sx, sy, 4, _objectPaint);
        }
    }
}
