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
        _pressedButton = GameMenuButton.None;

        float pl = GameMenuLayout.PanelLeft(width);
        float pt = GameMenuLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + GameMenuLayout.PanelWidth, pt + GameMenuLayout.PanelHeight);
        MenuStyle.DrawPanel(canvas, panelRect);

        float cx = pl + GameMenuLayout.PanelWidth / 2f;

        canvas.DrawText(GameInfo.Title, cx, pt + MenuLayout.TitleY, MenuStyle.TextTitle);
        canvas.DrawText(GameInfo.Version, cx, pt + MenuLayout.VersionY, MenuStyle.TextVersion);

        DrawButton(canvas, pl, pt, GameMenuLayout.ResumeY, "RESUME", GameMenuButton.Resume);
        DrawButton(canvas, pl, pt, GameMenuLayout.SaveY, "SAVE", GameMenuButton.Save);
        DrawButton(canvas, pl, pt, GameMenuLayout.LoadY, "LOAD", GameMenuButton.Load);
        DrawButton(canvas, pl, pt, GameMenuLayout.SettingsY, "SETTINGS", GameMenuButton.Settings);
        DrawButton(canvas, pl, pt, GameMenuLayout.MainMenuY, "MAIN MENU", GameMenuButton.MainMenu);
    }

    private static bool IsEnabled(GameMenuButton button) =>
        button is GameMenuButton.Resume or GameMenuButton.Save or GameMenuButton.Load or GameMenuButton.MainMenu;

    private ButtonState GetState(GameMenuButton id)
    {
        if (!IsEnabled(id)) return ButtonState.Disabled;
        if (_pressedButton == id) return ButtonState.Pressed;
        if (_hoveredButton == id) return ButtonState.Hovered;
        return ButtonState.Normal;
    }

    private void DrawButton(SKCanvas canvas, float panelLeft, float panelTop,
        float buttonLocalY, string text, GameMenuButton id)
    {
        float bx = panelLeft + (GameMenuLayout.PanelWidth - GameMenuLayout.ButtonWidth) / 2f;
        float by = panelTop + buttonLocalY;
        var rect = new SKRect(bx, by, bx + GameMenuLayout.ButtonWidth, by + GameMenuLayout.ButtonHeight);

        MenuStyle.DrawButton(canvas, rect, text, GetState(id));
    }
}
