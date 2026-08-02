using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens;

public sealed class MainMenuScreen : IScreen
{
    private int _screenWidth;
    private int _screenHeight;

    private MenuButton _hoveredButton = MenuButton.None;
    private MenuButton _pressedButton = MenuButton.None;

    public void OnActivated()
    {
        _hoveredButton = MenuButton.None;
        _pressedButton = MenuButton.None;
    }

    public void OnDeactivated() { }

    public ScreenEvent OnMouseDown(float x, float y)
    {
        var hit = MenuLayout.HitTest(x, y, _screenWidth, _screenHeight);

        if (hit == MenuButton.NewGame || hit == MenuButton.Exit)
            _pressedButton = hit;

        return hit switch
        {
            MenuButton.NewGame => ScreenEvent.NewGame,
            MenuButton.Exit => ScreenEvent.Exit,
            _ => ScreenEvent.None
        };
    }

    public bool OnMouseMove(float x, float y)
    {
        var hit = MenuLayout.HitTest(x, y, _screenWidth, _screenHeight);
        _hoveredButton = (hit == MenuButton.Load) ? MenuButton.None : hit;
        return hit == MenuButton.NewGame || hit == MenuButton.Exit;
    }

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;
        _pressedButton = MenuButton.None;

        MenuStyle.DrawBackground(canvas, width, height);

        float pl = MenuLayout.PanelLeft(width);
        float pt = MenuLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + MenuLayout.PanelWidth, pt + MenuLayout.PanelHeight);
        MenuStyle.DrawPanel(canvas, panelRect);

        float cx = pl + MenuLayout.PanelWidth / 2f;

        canvas.DrawText("Deep Space Saga", cx, pt + MenuLayout.TitleY, MenuStyle.TextTitle);

        float versionY = pt + MenuLayout.TitleY + MenuLayout.TitleToVersionGap + MenuLayout.VersionFontSize;
        canvas.DrawText("Version 1.0.0", cx, versionY, MenuStyle.TextVersion);

        DrawButton(canvas, pl, pt, MenuLayout.NewGameY, "NEW GAME", MenuButton.NewGame);
        DrawButton(canvas, pl, pt, MenuLayout.LoadY, "LOAD", MenuButton.Load);
        DrawButton(canvas, pl, pt, MenuLayout.ExitY, "EXIT", MenuButton.Exit);
    }

    private ButtonState GetState(MenuButton id, bool active)
    {
        if (!active) return ButtonState.Disabled;
        if (_pressedButton == id) return ButtonState.Pressed;
        if (_hoveredButton == id) return ButtonState.Hovered;
        return ButtonState.Normal;
    }

    private void DrawButton(SKCanvas canvas, float panelLeft, float panelTop,
        float buttonLocalY, string text, MenuButton id)
    {
        float bx = panelLeft + (MenuLayout.PanelWidth - MenuStyle.ButtonWidth) / 2f;
        float by = panelTop + buttonLocalY;
        var rect = new SKRect(bx, by, bx + MenuStyle.ButtonWidth, by + MenuStyle.ButtonHeight);

        bool active = id != MenuButton.Load;
        MenuStyle.DrawButton(canvas, rect, text, GetState(id, active));
    }
}
