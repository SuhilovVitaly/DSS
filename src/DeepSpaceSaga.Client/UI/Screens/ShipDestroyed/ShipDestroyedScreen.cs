using DeepSpaceSaga.Client.UI.Controls;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.ShipDestroyed;

/// <summary>A terminal screen after the player ship is destroyed: only Load and Main Menu can leave it.</summary>
public sealed class ShipDestroyedScreen : IScreen
{
    private readonly float _uiScale;
    private ShipDestroyedLayout _layout;
    private ScreenEvent _hovered;
    private ScreenEvent _focused = ScreenEvent.OpenLoadWindow;

    internal ShipDestroyedLayout Layout => _layout;
    internal ScreenEvent Focused => _focused;

    public ShipDestroyedScreen(float uiScale = 1) => _uiScale = float.IsFinite(uiScale) && uiScale > 0 ? uiScale : 1;

    public bool IsOpaque => true;
    public void OnActivated() { _hovered = ScreenEvent.None; _focused = ScreenEvent.OpenLoadWindow; }
    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key)
    {
        if (key is Key.Tab or Key.Down or Key.Up or Key.Left or Key.Right)
            _focused = _focused == ScreenEvent.OpenLoadWindow ? ScreenEvent.MainMenu : ScreenEvent.OpenLoadWindow;
        return key is Key.Enter or Key.Space ? _focused : ScreenEvent.None;
    }

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button) =>
        button == MouseButton.Left ? _layout.Hit(x / _uiScale, y / _uiScale) : ScreenEvent.None;

    public bool OnMouseMove(float x, float y)
    {
        _hovered = _layout.Hit(x / _uiScale, y / _uiScale);
        if (_hovered != ScreenEvent.None) _focused = _hovered;
        return _hovered != ScreenEvent.None;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;

    public void Render(SKCanvas canvas, int width, int height)
    {
        _layout = ShipDestroyedLayout.Create((int)(width / _uiScale), (int)(height / _uiScale));
        canvas.Clear(SKColors.Black);
        canvas.Save();
        canvas.Scale(_uiScale);
        GenericWindowTypeA.Draw(canvas, _layout.Panel);
        using var title = new SKPaint
        {
            Color = MenuStyle.ColorText,
            TextSize = 26 * _layout.Scale,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            Typeface = MenuStyle.TextTitle.Typeface
        };
        string text = Localization.Get("ShipDestroyed.Title");
        float measured = title.MeasureText(text), available = _layout.Panel.Width - 48 * _layout.Scale;
        if (measured > available) title.TextSize *= available / measured;
        canvas.DrawText(text, _layout.Panel.MidX, _layout.Panel.Top + 70 * _layout.Scale, title);
        Draw(_layout.Load, "ShipDestroyed.Load", ScreenEvent.OpenLoadWindow);
        Draw(_layout.MainMenu, "ShipDestroyed.MainMenu", ScreenEvent.MainMenu);
        canvas.Restore();

        void Draw(SKRect rect, string key, ScreenEvent action) =>
            GenericButtonTypeA.Draw(canvas, rect, Localization.Get(key),
                action == _focused || action == _hovered ? ButtonState.Hovered : ButtonState.Normal);
    }
}

/// <summary>Logical coordinates; the window applies the configured interface scale.</summary>
internal readonly record struct ShipDestroyedLayout(SKRect Panel, SKRect Load, SKRect MainMenu, float Scale)
{
    internal static ShipDestroyedLayout Create(int width, int height)
    {
        float scale = Math.Max(.01f, Math.Min(1, Math.Min((width - 16) / 500f, (height - 16) / 280f)));
        float left = (width - 500 * scale) / 2, top = (height - 280 * scale) / 2;
        SKRect Rect(float x, float y, float w, float h) =>
            new(left + x * scale, top + y * scale, left + (x + w) * scale, top + (y + h) * scale);
        return new(Rect(0, 0, 500, 280), Rect(40, 110, 420, 56), Rect(40, 182, 420, 56), scale);
    }

    internal ScreenEvent Hit(float x, float y) => Load.Contains(x, y) ? ScreenEvent.OpenLoadWindow :
        MainMenu.Contains(x, y) ? ScreenEvent.MainMenu : ScreenEvent.None;
}
