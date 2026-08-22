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
    /// <summary>Printable-character input, forwarded from Silk.NET's <c>IKeyboard.KeyChar</c>.</summary>
    void OnTextInput(char c) { }
    void OnActivated();
    void OnDeactivated();
}

public enum ScreenEvent
{
    None,
    NewGame,
    ScenarioSelected,
    Exit,
    OpenGameMenu,
    Resume,
    MainMenu,
    QuickSave,
    QuickLoad,
    OpenSettings,
    CloseSettings,
    OpenSaveWindow,
    CloseSaveWindow,
    OpenLoadWindow,
    CloseLoadWindow,
    LoadSlotRequested,
    OpenFinance,
    CloseFinance,
    OpenShip,
    CloseShip
}
