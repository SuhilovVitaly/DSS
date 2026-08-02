using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens;

public sealed class GameSessionScreen : IScreen
{
    private readonly IGameSessionConnection _connection;
    private readonly SKPaint _backgroundPaint;

    public GameSessionScreen(IGameSessionConnection connection)
    {
        _connection = connection;
        _backgroundPaint = new SKPaint { Color = SKColors.Black, Style = SKPaintStyle.Fill };
    }

    public IGameSessionConnection Connection => _connection;

    public void OnActivated() { }
    public void OnDeactivated() { }

    public ScreenEvent OnMouseDown(float x, float y) => ScreenEvent.None;
    public bool OnMouseMove(float x, float y) => false;

    public void Render(SKCanvas canvas, int width, int height)
    {
        canvas.DrawRect(0, 0, width, height, _backgroundPaint);
    }
}
