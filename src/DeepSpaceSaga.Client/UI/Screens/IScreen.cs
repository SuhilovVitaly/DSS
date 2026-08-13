using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens;

public interface IScreen
{
    void Render(SKCanvas canvas, int width, int height);
    ScreenEvent OnMouseDown(float x, float y, MouseButton button);
    void OnMouseUp(float x, float y) { }
    bool OnMouseMove(float x, float y);
    ScreenEvent OnMouseWheel(float x, float y, float delta);
    ScreenEvent OnKeyDown(Key key);
    void OnKeyUp(Key key) { }
    void OnActivated();
    void OnDeactivated();
}

public enum ScreenEvent
{
    None,
    NewGame,
    Exit,
    OpenGameMenu,
    Resume,
    MainMenu,
    QuickSave,
    QuickLoad,
    OpenSettings,
    CloseSettings
}
