using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Motion;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed class GameSessionScreen : IScreen
{
    private readonly SnapshotBuffer _buffer;
    private readonly IMotionPredictor _predictor;
    private readonly CameraState _camera;
    private readonly GridRenderer _grid;
    private readonly SKPaint _objectPaint;
    private readonly SKPaint _centerPaint;
    private readonly SKPaint _markerPaint;
    private int _viewportW;
    private int _viewportH;

    public GameSessionScreen(SnapshotBuffer buffer, IMotionPredictor predictor)
    {
        _buffer = buffer;
        _predictor = predictor;

        // Fixed camera: center of viewport = World(10000, 10000)
        _camera = new CameraState(focusX: 10000, focusY: 10000, pixelsPerWorldUnit: 1.0);
        _grid = new GridRenderer();

        _objectPaint = new SKPaint { Color = SKColors.Cyan, Style = SKPaintStyle.Fill, IsAntialias = true };
        _centerPaint = new SKPaint { Color = new SKColor(40, 40, 40), Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
        _markerPaint = new SKPaint { Color = new SKColor(220, 220, 60), Style = SKPaintStyle.Fill, IsAntialias = true };
    }

    public void OnActivated() { }
    public void OnDeactivated() { }

    public ScreenEvent OnMouseDown(float x, float y)
    {
        // Left click: set new camera focus at clicked world position
        var (worldX, worldY) = _camera.ScreenToWorld(x, y, _viewportW, _viewportH);
        _camera.SetFocus(worldX, worldY);
        return ScreenEvent.None;
    }
    public bool OnMouseMove(float x, float y) => false;

    public ScreenEvent OnKeyDown(Key key)
    {
        return key == Key.Escape ? ScreenEvent.OpenGameMenu : ScreenEvent.None;
    }

    public void Render(SKCanvas canvas, int width, int height)
    {
        _viewportW = width;
        _viewportH = height;

        // 1. Draw adaptive world grid (clears background internally)
        _grid.Draw(canvas, _camera, width, height);

        // 2. Static marker at World(10000, 10000) — reference point, radius 20 px
        var (mx, my) = _camera.WorldToScreen(10000, 10000, width, height);
        canvas.DrawCircle(mx, my, 20, _markerPaint);

        // 3. Crosshair at viewport center
        float cx = width / 2f;
        float cy = height / 2f;
        canvas.DrawLine(cx - 10, cy, cx + 10, cy, _centerPaint);
        canvas.DrawLine(cx, cy - 10, cx, cy + 10, _centerPaint);

        // 4. Engine objects rendered through the same camera transform
        var buffered = _buffer.Latest;
        if (buffered is null)
            return;

        long predictionDelta = buffered.PredictionDeltaMs;

        // During pause (Speed0), do not predict forward — objects stay at snapshot position.
        // Use the client-side authoritative speed tracker (updated immediately on speed change)
        // rather than the snapshot speed (which lags by up to 1 second).
        bool isPaused = _buffer.CurrentSpeed == Contracts.SimulationSpeed.Speed0;
        long effectiveDelta = isPaused ? 0 : predictionDelta;

        foreach (var obj in buffered.Snapshot.Objects)
        {
            var predicted = effectiveDelta > 0
                ? _predictor.Predict(obj, effectiveDelta)
                : obj;

            var (sx, sy) = _camera.WorldToScreen(predicted.X, predicted.Y, width, height);

            canvas.DrawCircle(sx, sy, 4, _objectPaint);
        }
    }
}
