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

    /// <summary>Same size/color/alignment as <see cref="MenuStyle.TextTitle"/>, but in
    /// Humaroid — a local copy rather than mutating the shared paint, which other screens
    /// (GameMenu, Trade, Station, ...) also draw their own titles with.</summary>
    private static readonly SKPaint _titleTextPaint = new()
    {
        Color = MenuStyle.ColorText,
        TextSize = MenuStyle.TitleFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Center,
        Typeface = MenuStyle.TypefaceHumaroid
    };

    /// <summary>Same size/color/alignment as <see cref="MenuStyle.TextVersion"/>, but in Humaroid.</summary>
    private static readonly SKPaint _versionTextPaint = new()
    {
        Color = MenuStyle.ColorText,
        TextSize = MenuStyle.VersionFontSize,
        IsAntialias = true,
        TextAlign = SKTextAlign.Center,
        Typeface = MenuStyle.TypefaceHumaroid
    };

    public MainMenuScreen()
    {
        GenericWindowTypeA.Preload();
        GenericButtonTypeA.Preload();
    }

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
        DrawWindowShell(canvas, MenuLayout.PanelRect(width, height));

        float cx = pl + MenuLayout.PanelWidth / 2f;

        canvas.DrawText(GameInfo.Title, cx, pt + MenuLayout.TitleY, _titleTextPaint);
        canvas.DrawText(GameInfo.Version, cx, pt + MenuLayout.VersionY, _versionTextPaint);

        DrawButton(canvas, pl, pt, MenuLayout.NewGameY, Localization.Get("MainMenu.NewGame"), MenuButton.NewGame);
        DrawButton(canvas, pl, pt, MenuLayout.LoadY, Localization.Get("MainMenu.Load"), MenuButton.Load);
        DrawButton(canvas, pl, pt, MenuLayout.SettingsY, Localization.Get("MainMenu.Settings"), MenuButton.Settings);
        DrawButton(canvas, pl, pt, MenuLayout.ExitY, Localization.Get("MainMenu.Exit"), MenuButton.Exit);
    }

    internal static void DrawWindowShell(SKCanvas canvas, SKRect bounds) =>
        GenericWindowTypeA.Draw(canvas, bounds);

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

        GenericButtonTypeA.Draw(canvas, rect, text, GetState(id, active: true));
    }
}
