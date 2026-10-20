using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Defeat;

/// <summary>A terminal screen: only Load and Main Menu can leave it.</summary>
public sealed class DefeatScreen : IScreen
{
    private readonly float _uiScale;
    private DefeatLayout _layout;
    private ScreenEvent _hovered;
    private ScreenEvent _focused = ScreenEvent.OpenLoadWindow;
    private readonly CombatEncounterSnapshot? _encounter;
    private float _statsScroll, _statsScrollLimit;

    internal DefeatLayout Layout => _layout;
    internal float StatsScrollOffset => _statsScroll;
    internal IReadOnlyList<string> StatsLines => _encounter is null ? [] : CombatSummaryPanel.FormatStats(_encounter);

    public DefeatScreen(float uiScale = 1, CombatEncounterSnapshot? encounter = null)
    { _uiScale = float.IsFinite(uiScale) && uiScale > 0 ? uiScale : 1; _encounter = encounter; }

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

    public ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        if (_layout.Stats.Contains(x / _uiScale, y / _uiScale) && float.IsFinite(delta) && delta != 0)
            _statsScroll = Math.Clamp(_statsScroll - Math.Sign(delta) * 24, 0, _statsScrollLimit);
        return ScreenEvent.None;
    }

    public void Render(SKCanvas canvas, int width, int height)
    {
        _layout = DefeatLayout.Create((int)(width / _uiScale), (int)(height / _uiScale), _encounter is not null);
        using var shade = new SKPaint { Color = new SKColor(0, 0, 0, 220) };
        canvas.DrawRect(0, 0, width, height, shade);
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
        string text = Localization.Get("Combat.Defeat");
        float measured = title.MeasureText(text), available = _layout.Panel.Width - 48 * _layout.Scale;
        if (measured > available) title.TextSize *= available / measured;
        canvas.DrawText(text, _layout.Panel.MidX, _layout.Panel.Top + 70 * _layout.Scale, title);
        if (_encounter is not null)
        {
            _statsScrollLimit = CombatSummaryPanel.DrawStats(canvas, _layout.Stats, StatsLines, Math.Max(8, 12 * _layout.Scale), _statsScroll);
            _statsScroll = Math.Clamp(_statsScroll, 0, _statsScrollLimit);
        }
        Draw(_layout.Load, "Combat.Defeat.Load", ScreenEvent.OpenLoadWindow);
        Draw(_layout.MainMenu, "Combat.Defeat.MainMenu", ScreenEvent.MainMenu);
        canvas.Restore();

        void Draw(SKRect rect, string key, ScreenEvent action)
        {
            GenericButtonTypeA.Draw(canvas, rect, Localization.Get(key),
                action == _focused || action == _hovered ? ButtonState.Hovered : ButtonState.Normal);
        }
    }
}
