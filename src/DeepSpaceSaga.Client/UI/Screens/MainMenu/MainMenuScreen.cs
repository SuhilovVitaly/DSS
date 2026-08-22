using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.MainMenu;

public sealed class MainMenuScreen : IScreen
{
    private int _screenWidth;
    private int _screenHeight;

    private MenuButton _hoveredButton = MenuButton.None;
    private MenuButton _pressedButton = MenuButton.None;

    /// <summary>
    /// Panel background at the exact 500×550 panel size. Loaded once and shared by
    /// every MainMenuScreen instance; falls back to MenuStyle.DrawPanel's plain fill
    /// if the file is missing.
    /// </summary>
    private static readonly SKBitmap? BackgroundImage =
        LoadImage("Images/UI/window-background-500x550.png");

    private static SKBitmap? LoadImage(string path)
    {
        try { return File.Exists(path) ? SKBitmap.Decode(path) : null; }
        catch { return null; }
    }

    /// <summary>True if the background PNG file was found and decoded at startup.</summary>
    internal static bool HasLoadedBackground => BackgroundImage is not null;

    public void OnActivated()
    {
        _hoveredButton = MenuButton.None;
        _pressedButton = MenuButton.None;
    }

    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key) => ScreenEvent.None;

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        var hit = MenuLayout.HitTest(x, y, _screenWidth, _screenHeight);

        if (hit == MenuButton.NewGame || hit == MenuButton.Load || hit == MenuButton.Settings || hit == MenuButton.Exit)
            _pressedButton = hit;

        return hit switch
        {
            MenuButton.NewGame => ScreenEvent.NewGame,
            MenuButton.Load => ScreenEvent.OpenLoadWindow,
            MenuButton.Settings => ScreenEvent.OpenSettings,
            MenuButton.Exit => ScreenEvent.Exit,
            _ => ScreenEvent.None
        };
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call sites/tests.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    public bool OnMouseMove(float x, float y)
    {
        var hit = MenuLayout.HitTest(x, y, _screenWidth, _screenHeight);
        _hoveredButton = hit;
        return hit == MenuButton.NewGame || hit == MenuButton.Load || hit == MenuButton.Settings || hit == MenuButton.Exit;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;
        _pressedButton = MenuButton.None;

        MenuStyle.DrawBackground(canvas, width, height);

        float pl = MenuLayout.PanelLeft(width);
        float pt = MenuLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + MenuLayout.PanelWidth, pt + MenuLayout.PanelHeight);
        if (BackgroundImage is not null)
            canvas.DrawBitmap(BackgroundImage, panelRect);
        else
            MenuStyle.DrawPanel(canvas, panelRect);

        float cx = pl + MenuLayout.PanelWidth / 2f;

        canvas.DrawText(GameInfo.Title, cx, pt + MenuLayout.TitleY, MenuStyle.TextTitle);
        canvas.DrawText(GameInfo.Version, cx, pt + MenuLayout.VersionY, MenuStyle.TextVersion);

        DrawButton(canvas, pl, pt, MenuLayout.NewGameY, "NEW GAME", MenuButton.NewGame);
        DrawButton(canvas, pl, pt, MenuLayout.LoadY, "LOAD", MenuButton.Load);
        DrawButton(canvas, pl, pt, MenuLayout.SettingsY, "SETTINGS", MenuButton.Settings);
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
        float bx = panelLeft + (MenuLayout.PanelWidth - MenuLayout.ButtonWidth) / 2f;
        float by = panelTop + buttonLocalY;
        var rect = new SKRect(bx, by, bx + MenuLayout.ButtonWidth, by + MenuLayout.ButtonHeight);

        ImageButton.Draw(canvas, rect, text, GetState(id, active: true));
    }
}
