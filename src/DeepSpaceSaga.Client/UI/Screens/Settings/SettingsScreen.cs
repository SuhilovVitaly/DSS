using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Settings;

public sealed class SettingsScreen : IScreen
{
    private int _screenWidth;
    private int _screenHeight;

    private SettingsButton _hoveredButton = SettingsButton.None;
    private SettingsButton _pressedButton = SettingsButton.None;

    private static readonly SKPaint _dimPaint = new()
    {
        Color = new SKColor(0, 0, 0, 160),
        Style = SKPaintStyle.Fill
    };

    public void OnActivated()
    {
        _hoveredButton = SettingsButton.None;
        _pressedButton = SettingsButton.None;
    }

    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key)
    {
        return key == Key.Escape ? ScreenEvent.CloseSettings : ScreenEvent.None;
    }

    public ScreenEvent OnMouseDown(float x, float y)
    {
        var hit = SettingsLayout.HitTest(x, y, _screenWidth, _screenHeight);

        if (hit == SettingsButton.None)
            return ScreenEvent.None;

        _pressedButton = hit;

        return hit == SettingsButton.Exit ? ScreenEvent.CloseSettings : ScreenEvent.None;
    }

    public bool OnMouseMove(float x, float y)
    {
        var hit = SettingsLayout.HitTest(x, y, _screenWidth, _screenHeight);
        _hoveredButton = hit;
        return hit != SettingsButton.None;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;
        _pressedButton = SettingsButton.None;

        // Dim background (overlay effect — underlying screen renders behind this)
        canvas.DrawRect(0, 0, width, height, _dimPaint);

        float pl = SettingsLayout.PanelLeft(width);
        float pt = SettingsLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + SettingsLayout.PanelWidth, pt + SettingsLayout.PanelHeight);
        MenuStyle.DrawPanel(canvas, panelRect);

        float cx = pl + SettingsLayout.PanelWidth / 2f;

        canvas.DrawText("SETTINGS", cx, pt + SettingsLayout.TitleY, MenuStyle.TextTitle);

        DrawButton(canvas, pl, pt, SettingsLayout.ExitY, "EXIT", SettingsButton.Exit);
    }

    private ButtonState GetState(SettingsButton id)
    {
        if (_pressedButton == id) return ButtonState.Pressed;
        if (_hoveredButton == id) return ButtonState.Hovered;
        return ButtonState.Normal;
    }

    private void DrawButton(SKCanvas canvas, float panelLeft, float panelTop,
        float buttonLocalY, string text, SettingsButton id)
    {
        float bx = panelLeft + (SettingsLayout.PanelWidth - SettingsLayout.ButtonWidth) / 2f;
        float by = panelTop + buttonLocalY;
        var rect = new SKRect(bx, by, bx + SettingsLayout.ButtonWidth, by + SettingsLayout.ButtonHeight);

        MenuStyle.DrawButton(canvas, rect, text, GetState(id));
    }
}
