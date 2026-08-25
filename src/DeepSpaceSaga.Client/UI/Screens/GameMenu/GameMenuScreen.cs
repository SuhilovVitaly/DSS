using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameMenu;

public sealed class GameMenuScreen : IScreen
{
    private int _screenWidth;
    private int _screenHeight;

    private GameMenuButton _hoveredButton = GameMenuButton.None;
    private GameMenuButton _pressedButton = GameMenuButton.None;

    private readonly string _title;
    private readonly string _version;
    private readonly string _resume;
    private readonly string _save;
    private readonly string _load;
    private readonly string _settings;
    private readonly string _mainMenu;
    private readonly string _escResume;

    public GameMenuScreen()
    {
        XenonStyle.Preload();
        XenonWindowChrome.Preload();
        GenericButtonTypeA.Preload();

        _title = GameInfo.Title;
        _version = GameInfo.Version;
        _resume = Localization.Get("GameMenu.Resume");
        _save = Localization.Get("GameMenu.Save");
        _load = Localization.Get("GameMenu.Load");
        _settings = Localization.Get("GameMenu.Settings");
        _mainMenu = Localization.Get("GameMenu.MainMenu");
        _escResume = Localization.Get("GameMenu.EscResume");
    }

    public void OnActivated()
    {
        _hoveredButton = GameMenuButton.None;
        _pressedButton = GameMenuButton.None;
    }

    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key)
    {
        return key == Key.Escape ? ScreenEvent.Resume : ScreenEvent.None;
    }

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        var hit = GameMenuLayout.HitTest(x, y, _screenWidth, _screenHeight);

        if (!IsEnabled(hit))
            return ScreenEvent.None;

        _pressedButton = hit;

        return hit switch
        {
            GameMenuButton.Resume => ScreenEvent.Resume,
            GameMenuButton.Save => ScreenEvent.OpenSaveWindow,
            GameMenuButton.Load => ScreenEvent.OpenLoadWindow,
            GameMenuButton.MainMenu => ScreenEvent.MainMenu,
            _ => ScreenEvent.None
        };
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call sites/tests.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    public bool OnMouseMove(float x, float y)
    {
        var hit = GameMenuLayout.HitTest(x, y, _screenWidth, _screenHeight);
        _hoveredButton = IsEnabled(hit) ? hit : GameMenuButton.None;
        return IsEnabled(hit);
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;
        XenonWindowChrome.Draw(
            canvas,
            GameMenuLayout.PanelRect(width, height),
            GameMenuLayout.FooterRect(width, height),
            _title,
            _version,
            _escResume);

        DrawButton(canvas, width, height, _resume, GameMenuButton.Resume);
        DrawButton(canvas, width, height, _save, GameMenuButton.Save);
        DrawButton(canvas, width, height, _load, GameMenuButton.Load);
        DrawButton(canvas, width, height, _settings, GameMenuButton.Settings);
        DrawButton(canvas, width, height, _mainMenu, GameMenuButton.MainMenu);
        _pressedButton = GameMenuButton.None;
    }

    private static bool IsEnabled(GameMenuButton button) =>
        button is GameMenuButton.Resume or GameMenuButton.Save or GameMenuButton.Load
            or GameMenuButton.MainMenu;

    private ButtonState GetState(GameMenuButton id)
    {
        if (!IsEnabled(id)) return ButtonState.Disabled;
        if (_pressedButton == id) return ButtonState.Pressed;
        if (_hoveredButton == id) return ButtonState.Hovered;
        return ButtonState.Normal;
    }

    private void DrawButton(SKCanvas canvas, int width, int height, string text, GameMenuButton id)
    {
        GenericButtonTypeA.Draw(canvas, GameMenuLayout.ButtonRect(id, width, height), text, GetState(id));
    }
}
