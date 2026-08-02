using SkiaSharp;

namespace DeepSpaceSaga.Client;

public interface IScreen
{
    void Render(SKCanvas canvas, int width, int height);
    ScreenEvent OnMouseDown(float x, float y);
    void OnMouseMove(float x, float y);
    void OnActivated();
    void OnDeactivated();
}

public enum ScreenEvent
{
    None,
    NewGame,
    Exit
}
