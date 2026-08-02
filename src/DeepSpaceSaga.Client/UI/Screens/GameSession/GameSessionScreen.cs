using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed class GameSessionScreen : IScreen
{
    private readonly SnapshotBuffer _buffer;
    private readonly SKPaint _backgroundPaint;
    private readonly SKPaint _objectPaint;

    public GameSessionScreen(SnapshotBuffer buffer)
    {
        _buffer = buffer;
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

        // Render objects from the latest authoritative snapshot.
        // No direct access to Engine or IGameSessionConnection.
        var snapshot = _buffer.Latest;
        if (snapshot is null)
            return;

        foreach (var obj in snapshot.Objects)
        {
            // Simple world-to-screen: 1 unit = 100 m, center on Sun
            float sx = (float)obj.X;
            float sy = height - (float)obj.Y; // flip Y for screen

            // Draw object as a small circle
            canvas.DrawCircle(sx, sy, 4, _objectPaint);
        }
    }
}
